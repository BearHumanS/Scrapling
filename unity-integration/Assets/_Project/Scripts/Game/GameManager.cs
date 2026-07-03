using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Board;
using DiceDungeon.Core.Characters;
using DiceDungeon.Core.Data;
using DiceDungeon.Core.Meta;
using DiceDungeon.Core.Progression;
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
        public BattleView BattleUi;
        public ProgressionPanels Progression;
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
        private CharacterSheet _sheet;
        private RelicModifiers _mods;
        private Unit _player;
        private List<Skill> _skills;
        private IReadOnlyList<Tile> _tiles;
        private int _floor, _position, _gold, _potions, _capturedThisFloor, _monstersKilled;
        private bool _reviveLeft, _rerollLeft;
        private EquipmentLoadout _loadout;
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

        public CharacterSheet Sheet => _sheet;

        public void StartRun()
        {
            var config = RunConfig.From(_profile, _selected);
            _character = CharacterClass.Get(_selected);
            _mods = config.Mods;
            _sheet = new CharacterSheet(_selected);
            _loadout = new EquipmentLoadout();
            _player = _sheet.BuildUnit(
                config.BonusHp + _mods.BonusHp,
                config.BonusAtk + _mods.BonusAtk,
                config.BonusDef + _mods.BonusDef,
                _mods.BonusCrit);
            _skills = _sheet.EquipSkills();
            _rng = new Rng(System.Environment.TickCount);
            _gold = _mods.StartGold;
            _potions = 2 + _mods.StartPotions;
            _reviveLeft = _character.RevivePerRun;
            _monstersKilled = 0;
            _floor = 0;

            TownPanel.SetActive(false);
            RunPanel.SetActive(true);
            NextFloor();
            // 시작 스킬 포인트 1개 → 첫 스킬 선택부터 빌드가 시작된다
            RollButton.interactable = false;
            StartCoroutine(SkillTreeRoutine());
        }

        private void RefreshPlayerFromSheet()
        {
            var config = RunConfig.From(_profile, _selected);
            _sheet.Refresh(_player,
                config.BonusHp + _mods.BonusHp,
                config.BonusAtk + _mods.BonusAtk,
                config.BonusDef + _mods.BonusDef,
                _mods.BonusCrit);
            _skills = _sheet.EquipSkills();
        }

        public void OnSkillTreePressed()
        {
            if (!RollButton.interactable) return; // 이동/전투 중에는 불가
            RollButton.interactable = false;
            StartCoroutine(SkillTreeRoutine());
        }

        private IEnumerator SkillTreeRoutine()
        {
            yield return Progression.RunSkillTree();
            RefreshPlayerFromSheet();
            RefreshHud();
            RollButton.interactable = true;
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
            var modded = DiceModRules.Apply(roll, _loadout.ActiveDiceMod, _dice, _rng);
            if (modded.Sum != roll.Sum) Log("주사위 장비가 운명을 비틀었다");
            roll = modded;
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
                AddGold(bonus);
                if (_loadout.HasEffect(UniqueEffect.LapFullHeal))
                {
                    _player.Heal(_player.MaxHp);
                    Log("전설 장비의 축복 — 완전 회복!");
                }
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
                    AddGold(gold);
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
                    else { int g = Balance.TreasureGold(_floor); AddGold(g); Log($"보물 상자 +{g}G"); }
                    break;

                case TileType.Shop:
                    ShopVisit();
                    break;

                case TileType.Rest:
                    _player.HealRatio(Balance.RestHealRatio * (_character.DoubleRestHeal ? 2 : 1) + _mods.RestHealBonus);
                    Log("모닥불에서 휴식했다");
                    break;

                case TileType.Trap:
                    if (_rng.Chance(Balance.TrapAvoidChance + _mods.TrapAvoidBonus
                                    + (_loadout.HasEffect(UniqueEffect.TrapWard) ? 0.25 : 0)))
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
            int perTile = 2 * (_character.DoubleCaptureShield ? 2 : 1)
                            * (_sheet.HasCaptureShieldTriple ? 3 : 1);
            var options = new BattleOptions
            {
                CritBonus = roll.IsLuckySeven ? 0.15 : 0,
                PlayerFirst = roll.IsDouble,
                StartShield = _capturedThisFloor * perTile
                              + (int)(_player.MaxHp * _character.BarrierRatio),
                Skills = _skills,
                ReviveAvailable = _reviveLeft,
                ReviveRatio = _sheet.HasReviveBoost ? 0.7 : 0.5,
                BurnOnHit = _sheet.BurnOnHitChance,
            };

            // 수동 전투 (Week 3): InteractiveBattle이 판정, BattleView가 입력·연출
            string intro = roll.IsDouble ? "더블! 선제공격 기회"
                         : roll.IsLuckySeven ? "럭키세븐! 크리티컬 +15%" : "적이 나타났다";
            var battle = new InteractiveBattle(_player, monsters, _rng.Derive(), options, _potions);
            yield return BattleUi.RunBattle(battle, isBoss, intro);

            _potions = battle.Potions;
            _monstersKilled += battle.MonstersKilled;
            if (battle.ReviveUsed) { _reviveLeft = false; Log("빛이 감싸며 부활했다!"); }
            RefreshHud();

            if (!battle.PlayerWon)
            {
                yield return EndRunRoutine(died: true);
                yield break;
            }

            // 경험치 → 레벨업(스탯 배분) → 전직 (07-심화시스템 5)
            int xp = battle.MonstersKilled * Balance.MonsterXp(_floor) * (isBoss ? Balance.BossXpMult : 1);
            int gained = _sheet.GainXp(xp);
            if (gained > 0)
            {
                Log($"레벨 업! Lv{_sheet.Level} (+경험치 {xp})");
                if (_sheet.CanJobChange)
                    yield return Progression.RunJobChange();
                yield return Progression.RunLevelUp();
                if (_sheet.Book.Points > 0)
                    yield return Progression.RunSkillTree();
                RefreshPlayerFromSheet();
                RefreshHud();
            }
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
            if (v < 0.40) { int g = Balance.TreasureGold(_floor); AddGold(g); Log($"행운의 이벤트 +{g}G"); }
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
            var item = EquipmentFactory.Generate(_floor, _rng);
            if (_loadout.TryEquip(item))
            {
                _loadout.ApplyTo(_sheet);
                RefreshPlayerFromSheet();
                Log($"[{item.Rarity}] {item.Name} 장착!");
            }
            else
            {
                AddGold(10 + _floor * 4);
                Log($"{item.Name} — 하위품이라 매각 (+{10 + _floor * 4}G)");
            }
        }

        private void AddGold(int amount)
        {
            if (_loadout.HasEffect(UniqueEffect.GoldFind))
                amount = (int)(amount * 1.25);
            _gold += amount;
        }

        private List<Unit> MakeMonsters(bool elite)
        {
            int hp = Balance.MonsterHp(_floor);
            int atk = Balance.MonsterAtk(_floor);
            int def = Balance.MonsterDef(_floor);
            var element = ElementTable.FloorElement(_floor); // 층 테마 = 방어 속성
            if (elite)
                return new List<Unit> { new Unit("Elite", (int)(hp * 1.8), (int)(atk * 1.3), def, 9) { Element = element } };
            int count = _floor <= Balance.SoloMonsterFloors ? 1 : _rng.Next(1, 3);
            var list = new List<Unit>();
            for (int i = 0; i < count; i++) list.Add(new Unit("Mob", hp, atk, def, 8) { Element = element });
            return list;
        }

        private List<Unit> BossMonsters() => new List<Unit>
        {
            new Unit("Boss",
                (int)(Balance.MonsterHp(_floor) * Balance.BossHpMult),
                (int)(Balance.MonsterAtk(_floor) * Balance.BossAtkMult),
                Balance.MonsterDef(_floor), 9)
            { Element = ElementTable.FloorElement(_floor) }
        };

        private void RefreshHud()
        {
            string job = _sheet?.Advanced?.Name ?? _character?.Name ?? "";
            FloorText.text = $"{_floor}층 · {job} Lv{_sheet?.Level ?? 1}";
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
