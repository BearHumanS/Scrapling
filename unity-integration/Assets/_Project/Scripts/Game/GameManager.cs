using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Board;
using DiceDungeon.Core.Characters;
using DiceDungeon.Core.Data;
using DiceDungeon.Core.Meta;
using DiceDungeon.Core.Run;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDungeon.Game
{
    /// <summary>
    /// 런 상태머신 (Presentation): 주사위 → 이동 → 타일 → 보스 → 하강/귀환 → 결산.
    /// 게임 규칙 판정은 전부 Core를 호출하고, 여기서는 입력 대기와 연출 순서만 관리한다.
    /// Core의 RunController는 시뮬레이션(자동 정책)용이고, 이 클래스가 그 대화형 버전이다 —
    /// 규칙 수치를 바꿀 때는 Core(Balance.cs)만 바꾸면 양쪽에 동시 적용된다.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        // Bootstrap이 배선
        public BoardView Board;
        public Text FloorText, GoldText, SoulText, HpText, DiceText, LogText;
        public Image HpFill;
        public Button RollButton;
        public GameObject TownPanel, RunPanel, ChoicePanel, ResultPanel;
        public Text TownInfoText, ResultText;
        public System.Action RefreshTownUi;

        private PlayerProfile _profile;
        private CharacterId _selected = CharacterId.Knight;

        // 런 상태
        private Rng _rng;
        private DiceRoller _dice;
        private CharacterClass _character;
        private RelicModifiers _mods;
        private Unit _player;
        private List<Skill> _skills;
        private IReadOnlyList<Tile> _tiles;
        private int _floor, _position, _gold, _potions, _capturedThisFloor, _monstersKilled;
        private bool _reviveLeft, _rerollLeft;
        private readonly Queue<string> _log = new Queue<string>();

        public PlayerProfile Profile => _profile;
        public CharacterId Selected => _selected;

        private void Awake()
        {
            _profile = SaveService.LoadOrCreate();
        }

        public void SelectOrUnlock(CharacterId id)
        {
            if (_profile.IsUnlocked(id)) _selected = id;
            else if (_profile.TryUnlock(id))
            {
                _selected = id;
                SaveService.Save(_profile);
            }
            RefreshTownUi?.Invoke();
        }

        public void Train(int stat) // 0=HP 1=ATK 2=DEF
        {
            bool ok = false;
            switch (stat)
            {
                case 0: ok = _profile.TryTrain(ref _profile.TrainHpLevel); break;
                case 1: ok = _profile.TryTrain(ref _profile.TrainAtkLevel); break;
                case 2: ok = _profile.TryTrain(ref _profile.TrainDefLevel); break;
            }
            if (ok) SaveService.Save(_profile);
            RefreshTownUi?.Invoke();
        }

        /// <summary>유물: 가장 싼 것 자동 구매 (스캐폴드 단순화 — 본편은 개별 상점 UI).</summary>
        public void BuyCheapestRelic()
        {
            var candidates = Enumerable.Range(0, RelicCatalog.Count)
                .Where(i => _profile.RelicLevels[i] < RelicCatalog.MaxLevel)
                .OrderBy(i => RelicCatalog.UpgradeCost(_profile.RelicLevels[i]))
                .ToList();
            if (candidates.Count > 0 && _profile.TryUpgradeRelic((RelicId)candidates[0]))
                SaveService.Save(_profile);
            RefreshTownUi?.Invoke();
        }

        public void StartRun()
        {
            var config = RunConfig.From(_profile, _selected);
            _character = CharacterClass.Get(_selected);
            _mods = config.Mods;
            _player = new Unit(_character.Name,
                _character.Hp + config.BonusHp + _mods.BonusHp,
                _character.Atk + config.BonusAtk + _mods.BonusAtk,
                _character.Def + config.BonusDef + _mods.BonusDef,
                _character.Spd,
                _character.CritChance + _mods.BonusCrit);
            _skills = _character.Skills.Select(s => s.Instance()).ToList();
            _rng = new Rng(System.Environment.TickCount);
            _gold = _mods.StartGold;
            _potions = 2 + _mods.StartPotions;
            _reviveLeft = _character.RevivePerRun;
            _monstersKilled = 0;
            _floor = 0;

            TownPanel.SetActive(false);
            RunPanel.SetActive(true);
            NextFloor();
        }

        private void NextFloor()
        {
            _floor++;
            _position = 0;
            _capturedThisFloor = 0;
            _rerollLeft = _character.FloorReroll;
            _tiles = FloorGenerator.Generate(_floor, _rng.Derive());
            _dice = new DiceRoller(_rng.Derive());
            Board.ShowFloor(_tiles);
            Log($"— {_floor}층 시작 —");
            RefreshHud();
            RollButton.interactable = true;
        }

        public void OnRollPressed()
        {
            RollButton.interactable = false;
            StartCoroutine(TurnRoutine());
        }

        private IEnumerator TurnRoutine()
        {
            // 주사위 연출: 선판정 후 눈만 플리커 (03-기술설계 2.5)
            var roll = _dice.Roll();
            if (_rerollLeft && roll.Sum <= 4)
            {
                _rerollLeft = false;
                roll = _dice.Roll();
                Log("도적의 감: 낮은 눈을 다시 굴렸다!");
            }
            for (float t = 0; t < 0.5f; t += 0.06f)
            {
                DiceText.text = $"⚀ {_rng.Next(1, 7)} + {_rng.Next(1, 7)}";
                yield return new WaitForSeconds(0.06f);
            }
            DiceText.text = $"🎲 {roll.Die1} + {roll.Die2} = {roll.Sum}" +
                            (roll.IsDouble ? " 더블!" : roll.IsLuckySeven ? " 럭키세븐!" : "");

            int target = _position + roll.Sum;
            bool lap = target >= Balance.BoardSize;
            yield return Board.MoveToken(_position, roll.Sum);
            _position = target % Balance.BoardSize;

            if (lap)
            {
                int bonus = (int)(Balance.LapBonusGold(_floor) * _mods.LapGoldMult);
                _gold += bonus;
                Log($"한 바퀴 완주! +{bonus}G — 보스 게이트가 열린다");
                RefreshHud();
                yield return BattleRoutine(BossMonsters(), roll, isBoss: true);
                if (_player.IsAlive) yield return FloorClearRoutine();
                yield break;
            }

            yield return ResolveTileRoutine(_tiles[_position], roll);

            if (_player != null && _player.IsAlive)
                RollButton.interactable = true;
        }

        private IEnumerator ResolveTileRoutine(Tile tile, DiceResult roll)
        {
            switch (tile.Type)
            {
                case TileType.Battle:
                case TileType.Elite:
                    if (tile.Captured)
                    {
                        _player.HealRatio(Balance.CapturedTileHealRatio + _mods.CaptureHealBonus);
                        Log("내 땅이다 — 잠시 쉬며 회복");
                        break;
                    }
                    bool elite = tile.Type == TileType.Elite;
                    yield return BattleRoutine(MakeMonsters(elite), roll, isBoss: false);
                    if (!_player.IsAlive) yield break;
                    tile.Captured = true;
                    _capturedThisFloor++;
                    Board.Paint(_position, tile);
                    int gold = Balance.BattleGold(_floor) * (elite ? 2 : 1);
                    _gold += gold;
                    Log($"칸 점령! +{gold}G");
                    if (elite) AddGear();
                    break;

                case TileType.Treasure:
                    if (_rng.Chance(Balance.MimicChance))
                    {
                        Log("미믹이다!");
                        yield return BattleRoutine(MakeMonsters(false), roll, isBoss: false);
                    }
                    else if (_rng.Chance(0.5)) AddGear();
                    else { int g = Balance.TreasureGold(_floor); _gold += g; Log($"보물 상자 +{g}G"); }
                    break;

                case TileType.Shop:
                    ShopVisit();
                    break;

                case TileType.Rest:
                    _player.HealRatio(Balance.RestHealRatio * (_character.DoubleRestHeal ? 2 : 1) + _mods.RestHealBonus);
                    Log("모닥불에서 휴식했다");
                    break;

                case TileType.Trap:
                    if (_rng.Chance(Balance.TrapAvoidChance + _mods.TrapAvoidBonus))
                        Log("함정을 피했다!");
                    else
                    {
                        int dmg = (int)(_player.MaxHp * Balance.TrapDamageRatio);
                        _player.TakeDamage(dmg);
                        Log($"함정! -{dmg} HP");
                    }
                    if (!_player.IsAlive) yield return EndRunRoutine(died: true);
                    break;

                case TileType.Event:
                    ResolveEvent();
                    if (!_player.IsAlive) yield return EndRunRoutine(died: true);
                    break;
            }
            RefreshHud();
        }

        private IEnumerator BattleRoutine(List<Unit> monsters, DiceResult roll, bool isBoss)
        {
            if (_potions > 0 && _player.HpRatio < 0.45)
            {
                _potions--;
                _player.HealRatio(Balance.PotionHealRatio);
                Log("포션을 마셨다");
            }

            var options = new BattleOptions
            {
                CritBonus = roll.IsLuckySeven ? 0.15 : 0,
                PlayerFirst = roll.IsDouble,
                StartShield = _capturedThisFloor * (_character.DoubleCaptureShield ? 4 : 2)
                              + (int)(_player.MaxHp * _character.BarrierRatio),
                Skills = _skills,
                ReviveAvailable = _reviveLeft,
                Evasion = _character.Evasion,
            };

            int hpBefore = _player.Hp;
            var result = BattleSimulator.Fight(_player, monsters, _rng.Derive(), options);
            _monstersKilled += result.MonstersKilled;
            if (result.ReviveUsed) { _reviveLeft = false; Log("빛이 감싸며 부활했다!"); }

            // 결과 재생 (Week 3에서 수동 입력으로 교체 예정)
            string name = isBoss ? "보스" : "몬스터";
            Log($"{name}과 전투! ({result.Rounds}라운드, HP {hpBefore}→{_player.Hp})");
            yield return LerpHpBar(hpBefore, _player.Hp);

            if (!result.PlayerWon)
                yield return EndRunRoutine(died: true);
        }

        private IEnumerator FloorClearRoutine()
        {
            _player.HealRatio(Balance.FloorClearHealRatio);
            RefreshHud();
            if (_floor >= Balance.FinalFloor)
            {
                Log("심연의 끝에 도달했다!");
                yield return EndRunRoutine(died: false);
                yield break;
            }
            ChoicePanel.SetActive(true); // 하강/귀환 버튼은 Bootstrap이 배선
        }

        public void OnDescend()
        {
            ChoicePanel.SetActive(false);
            NextFloor();
        }

        public void OnRetreat()
        {
            ChoicePanel.SetActive(false);
            StartCoroutine(EndRunRoutine(died: false));
        }

        private IEnumerator EndRunRoutine(bool died)
        {
            int floors = died ? _floor - 1 : _floor;
            floors = Mathf.Max(0, floors);
            double stones = Balance.SoulstonesForRun(floors, _monstersKilled) * _mods.SoulstoneMult;
            if (died) stones *= Balance.DeathSoulstonePenalty;
            int earned = (int)stones;

            _profile.Soulstones += earned;
            _profile.TotalRuns++;
            _profile.BestFloor = Mathf.Max(_profile.BestFloor, floors);
            SaveService.Save(_profile);

            ResultText.text = (died ? "쓰러졌다...\n" : "귀환 성공!\n") +
                $"도달: {floors}층\n처치: {_monstersKilled}\n골드: {_gold}\n소울스톤 +{earned}";
            yield return new WaitForSeconds(0.6f);
            RunPanel.SetActive(false);
            ResultPanel.SetActive(true);
        }

        public void BackToTown()
        {
            ResultPanel.SetActive(false);
            TownPanel.SetActive(true);
            RefreshTownUi?.Invoke();
        }

        // --- 내부 헬퍼 ---

        private void ShopVisit()
        {
            double discount = 1.0 - _mods.ShopDiscount;
            int potionPrice = (int)(Balance.PotionPrice(_floor) * discount);
            int gearPrice = (int)(Balance.GearPrice(_floor) * discount);
            var bought = new List<string>();
            if (_player.HpRatio < 0.6 && _gold >= potionPrice)
            {
                _gold -= potionPrice; _potions++;
                bought.Add("포션");
            }
            while (_gold >= gearPrice)
            {
                _gold -= gearPrice;
                AddGear();
                bought.Add("장비");
            }
            Log(bought.Count > 0 ? $"상점: {string.Join(", ", bought)} 구매" : "상점: 살 게 없다...");
        }

        private void ResolveEvent()
        {
            double v = _rng.NextDouble();
            if (_character.BestEventChoice && v >= 0.40 && v < 0.75)
                v = _rng.NextDouble();
            if (v < 0.40) { int g = Balance.TreasureGold(_floor); _gold += g; Log($"행운의 이벤트 +{g}G"); }
            else if (v < 0.75)
            {
                int dmg = (int)(_player.MaxHp * 0.08);
                _player.TakeDamage(dmg);
                Log($"불길한 이벤트 -{dmg} HP");
            }
            else AddGear();
        }

        private void AddGear()
        {
            _player.Atk += Balance.GearAtkBonus(_floor);
            _player.Def += Balance.GearDefBonus(_floor);
            int hp = Balance.GearHpBonus(_floor);
            _player.MaxHp += hp;
            _player.Heal(hp);
            Log("장비 획득! 강해졌다");
        }

        private List<Unit> MakeMonsters(bool elite)
        {
            int hp = Balance.MonsterHp(_floor);
            int atk = Balance.MonsterAtk(_floor);
            int def = Balance.MonsterDef(_floor);
            if (elite)
                return new List<Unit> { new Unit("Elite", (int)(hp * 1.8), (int)(atk * 1.3), def, 9) };
            int count = _floor <= Balance.SoloMonsterFloors ? 1 : _rng.Next(1, 3);
            var list = new List<Unit>();
            for (int i = 0; i < count; i++) list.Add(new Unit("Mob", hp, atk, def, 8));
            return list;
        }

        private List<Unit> BossMonsters() => new List<Unit>
        {
            new Unit("Boss",
                (int)(Balance.MonsterHp(_floor) * Balance.BossHpMult),
                (int)(Balance.MonsterAtk(_floor) * Balance.BossAtkMult),
                Balance.MonsterDef(_floor), 9)
        };

        private IEnumerator LerpHpBar(int from, int to)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.4f)
            {
                float hp = Mathf.Lerp(from, to, t);
                UiFactory.SetRatio(HpFill, hp / _player.MaxHp);
                yield return null;
            }
            RefreshHud();
        }

        private void RefreshHud()
        {
            FloorText.text = $"{_floor}층";
            GoldText.text = $"{_gold}G";
            SoulText.text = $"◆{_profile.Soulstones}";
            HpText.text = $"{_player.Hp}/{_player.MaxHp}  물약{_potions}";
            UiFactory.SetRatio(HpFill, (float)_player.HpRatio);
        }

        private void Log(string message)
        {
            _log.Enqueue(message);
            while (_log.Count > 3) _log.Dequeue();
            LogText.text = string.Join("\n", _log);
        }
    }
}
