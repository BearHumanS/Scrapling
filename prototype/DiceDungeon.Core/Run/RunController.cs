using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Board;
using DiceDungeon.Core.Characters;
using DiceDungeon.Core.Data;
using DiceDungeon.Core.Meta;
using DiceDungeon.Core.Progression;

namespace DiceDungeon.Core.Run
{
    /// <summary>런 시작 조건: 선택 캐릭터 + 마을 메타 성장(훈련소·유물)의 집계.</summary>
    public sealed class RunConfig
    {
        public CharacterId Character = CharacterId.Knight;
        /// <summary>전직 자동 선택 (시뮬레이션): true = A경로(공격), false = B경로(유틸).</summary>
        public bool PreferPathA = true;
        public RelicModifiers Mods = new RelicModifiers();

        // 훈련소 보정 (PlayerProfile.Trained* 를 넣는다)
        public int BonusHp { get; set; }
        public int BonusAtk { get; set; }
        public int BonusDef { get; set; }

        public static RunConfig From(PlayerProfile profile, CharacterId character)
            => new RunConfig
            {
                Character = character,
                Mods = profile.Modifiers(),
                BonusHp = profile.TrainedHp,
                BonusAtk = profile.TrainedAtk,
                BonusDef = profile.TrainedDef,
            };
    }

    public sealed class RunResult
    {
        public int FloorsCleared { get; set; }
        public int DeathFloor { get; set; }   // 0 = 사망하지 않음 (귀환 또는 완주)
        public bool ClearedFinalFloor { get; set; }
        public int Gold { get; set; }
        public int Soulstones { get; set; }
        public int MonstersKilled { get; set; }
        public int DiceRolls { get; set; }
        public bool ReviveUsed { get; set; }
    }

    /// <summary>런 중 의사결정. 실제 게임에서는 UI 입력, 시뮬레이션에서는 휴리스틱.</summary>
    public interface IPlayerPolicy
    {
        /// <summary>층 클리어 후: true = 하강, false = 귀환 (01-게임기획서 5).</summary>
        bool Descend(int nextFloor, double hpRatio);
        /// <summary>전투 직전 포션 사용 여부.</summary>
        bool UsePotion(double hpRatio, int potions);
        /// <summary>출발점 통과 시: true = 보스 입장, false = 한 바퀴 더 (위험도 +1) — 심연의 부름 (v7).</summary>
        bool EnterBoss(int floor, int lapsDone, double hpRatio);
    }

    /// <summary>기본 휴리스틱: HP 여유가 있으면 하강, 위험하면 포션, 여유 있으면 최대 2바퀴.</summary>
    public sealed class GreedyPolicy : IPlayerPolicy
    {
        private readonly double _descendHpThreshold;
        private readonly int _maxLaps;

        public GreedyPolicy(double descendHpThreshold = 0.35, int maxLaps = 2)
        {
            _descendHpThreshold = descendHpThreshold;
            _maxLaps = maxLaps;
        }

        public bool Descend(int nextFloor, double hpRatio) => hpRatio >= _descendHpThreshold;
        public bool UsePotion(double hpRatio, int potions) => potions > 0 && hpRatio < 0.45;
        public bool EnterBoss(int floor, int lapsDone, double hpRatio)
            => lapsDone >= _maxLaps || hpRatio < 0.65;
    }

    /// <summary>
    /// 런 전체 오케스트레이션: 층 생성 → 주사위 → 이동 → 타일 처리 → 한 바퀴 → 보스 → 하강/귀환.
    /// 순수 C#이므로 단위 테스트와 대량 시뮬레이션(밸런스 검증)에 그대로 사용한다.
    /// </summary>
    public sealed class RunController
    {
        private readonly Rng _rng;
        private readonly IPlayerPolicy _policy;
        private readonly RunConfig _config;
        private readonly CharacterClass _character;
        private readonly CharacterSheet _sheet;
        private readonly Unit _player;
        private List<Skill> _skills;
        private readonly RunResult _result = new RunResult();

        private int _gold;
        private int _potions;
        private int _gearCount;
        private bool _reviveLeft;
        private readonly EquipmentLoadout _loadout = new EquipmentLoadout();
        private readonly ConsumableBag _bag = new ConsumableBag { Charms = 1 }; // 첫 런부터 조작 경험

        public RunController(int seed, RunConfig config, IPlayerPolicy policy)
        {
            _rng = new Rng(seed);
            _policy = policy;
            _config = config;
            _character = CharacterClass.Get(config.Character);
            _sheet = new CharacterSheet(config.Character);
            _sheet.AutoLearnSkills(); // 시작 스킬 1개
            _player = _sheet.BuildUnit(
                config.BonusHp + config.Mods.BonusHp,
                config.BonusAtk + config.Mods.BonusAtk,
                config.BonusDef + config.Mods.BonusDef,
                config.Mods.BonusCrit);
            _skills = _sheet.EquipSkills();
            _gold = config.Mods.StartGold;
            _potions = 2 + config.Mods.StartPotions;
            _reviveLeft = _character.RevivePerRun;
        }

        private void RefreshPlayer()
        {
            _sheet.Refresh(_player,
                _config.BonusHp + _config.Mods.BonusHp,
                _config.BonusAtk + _config.Mods.BonusAtk,
                _config.BonusDef + _config.Mods.BonusDef,
                _config.Mods.BonusCrit);
        }

        public RunResult Play()
        {
            for (int floor = 1; floor <= Balance.FinalFloor; floor++)
            {
                if (!PlayFloor(floor))
                {
                    _result.DeathFloor = floor;
                    break;
                }

                _result.FloorsCleared = floor;
                _player.HealRatio(Balance.FloorClearHealRatio); // 층 클리어 회복
                if (floor == Balance.FinalFloor)
                {
                    _result.ClearedFinalFloor = true;
                    break;
                }
                if (!_policy.Descend(floor + 1, _player.HpRatio))
                    break; // 자발 귀환: 결산 100% 회수
            }

            _result.Gold = _gold;
            // v8: 사망 페널티는 킬 보상에만 — 도달층 보상은 보존 (감사 A1)
            double stones = Balance.SoulstonesForRun(
                                _result.FloorsCleared, _result.MonstersKilled, _result.DeathFloor > 0)
                            * _config.Mods.SoulstoneMult;
            _result.Soulstones = (int)stones;
            return _result;
        }

        /// <returns>false = 사망.</returns>
        private bool PlayFloor(int floor)
        {
            var tiles = FloorGenerator.Generate(floor, _rng.Derive());
            var dice = new DiceRoller(_rng.Derive());
            int position = 0;
            int capturedThisFloor = 0;
            int laps = 0;
            _extraLaps = 0;
            bool rerollLeft = _character.FloorReroll;

            while (true)
            {
                // 홀짝 부적: 원하는 타일(쉼터/내 땅/상점)의 거리 홀짝에 맞춰 굴림을 강제
                // — 보드 이동을 "구경"에서 "결정"으로 바꾸는 소모품 (01-게임기획서 4.3)
                var roll = RollWithCharmPolicy(dice, tiles, position, floor);
                _result.DiceRolls++;

                // 도적 패시브: 층당 1회, 낮은 눈이면 무료 리롤
                if (rerollLeft && roll.Sum <= 4)
                {
                    rerollLeft = false;
                    roll = dice.Roll();
                    _result.DiceRolls++;
                }

                // 주사위 장비 효과 (운명 조작 — 01-게임기획서 4.3)
                roll = DiceModRules.Apply(roll, _loadout.ActiveDiceMod, dice, _rng);

                int next = position + roll.Sum;
                bool passedStart = next >= Balance.BoardSize;
                position = next % Balance.BoardSize;

                if (passedStart)
                {
                    // 심연의 부름 (v7): 완주 보너스는 랩마다, 보스 입장은 선택.
                    // 한 바퀴 더 = 내 땅 회복·파밍 기회, 대신 위험도 +1 (몬스터·보스 강화)
                    laps++;
                    AddGold((int)(Balance.LapBonusGold(floor)
                                  * (1 + Balance.LapRewardMult * _extraLaps)
                                  * _config.Mods.LapGoldMult));
                    if (_loadout.HasEffect(UniqueEffect.LapFullHeal))
                        _player.Heal(_player.MaxHp); // 전설: 완주 시 전체 회복

                    if (_policy.EnterBoss(floor, laps, _player.HpRatio))
                        return FightBoss(floor, roll, capturedThisFloor);

                    _extraLaps++; // 계속 돈다 — 이후 생성되는 몬스터가 강해진다
                }

                if (!ResolveTile(tiles[position], floor, roll, ref capturedThisFloor))
                    return false;
            }
        }

        /// <summary>이번 층에서 선택한 추가 랩 수 — 몬스터 위험도 배율의 근거.</summary>
        private int _extraLaps;

        private double DangerMult => 1 + Balance.LapDangerMult * _extraLaps;

        /// <summary>부적 사용 판단 (시뮬레이션 정책 — UI 게임에서는 플레이어 버튼).</summary>
        private DiceResult RollWithCharmPolicy(DiceRoller dice, IReadOnlyList<Tile> tiles, int position, int floor)
        {
            if (_bag.Charms > 0)
            {
                int wantDist = DesirableDistance(tiles, position, floor);
                if (wantDist > 0 && _bag.TryUse(ConsumableType.ParityCharm))
                    return dice.RollWithParity(even: wantDist % 2 == 0);
            }
            return dice.Roll();
        }

        /// <summary>지금 가고 싶은 타일까지의 거리 (2~12, 랩 넘어가는 칸 제외). 0 = 없음.</summary>
        private int DesirableDistance(IReadOnlyList<Tile> tiles, int position, int floor)
        {
            for (int d = 2; d <= 12; d++)
            {
                if (position + d >= Balance.BoardSize) break; // 랩(보스)은 부적 대상 아님
                var t = tiles[position + d];
                bool wantHeal = _player.HpRatio < 0.5
                                && (t.Type == TileType.Rest || (t.Type == TileType.Battle && t.Captured));
                bool wantShop = _gold >= Balance.GearPrice(floor) && t.Type == TileType.Shop;
                if (wantHeal || wantShop) return d;
            }
            return 0;
        }

        private bool ResolveTile(Tile tile, int floor, DiceResult roll, ref int captured)
        {
            switch (tile.Type)
            {
                case TileType.Battle:
                case TileType.Elite:
                    if (tile.Captured)
                    {
                        // 내 땅: 회복 (브루마블의 "내 땅" 변형)
                        _player.HealRatio(Balance.CapturedTileHealRatio + _config.Mods.CaptureHealBonus);
                        return true;
                    }
                    bool elite = tile.Type == TileType.Elite;
                    if (!Fight(MakeMonsters(floor, elite), roll, captured, floor, out bool fled)) return false;
                    if (fled) return true; // 연막탄 도주: 보상·점령 없음
                    tile.Captured = true;
                    captured++;
                    AddGold(Balance.BattleGold(floor) * (elite ? 2 : 1));
                    if (elite) AddGear(floor);
                    return true;

                case TileType.Treasure:
                    if (_rng.Chance(Balance.MimicChance))
                        return Fight(MakeMonsters(floor, elite: false), roll, captured, floor, out _); // 미믹 기습
                    double v2 = _rng.NextDouble();
                    if (v2 < 0.40) AddGear(floor);
                    else if (v2 < 0.75) AddGold(Balance.TreasureGold(floor));
                    else _bag.Add((ConsumableType)_rng.Next(0, 3)); // 소모품 드롭
                    return true;

                case TileType.Shop:
                    ShopVisit(floor);
                    return true;

                case TileType.Rest:
                    _player.HealRatio(Balance.RestHealRatio * (_character.DoubleRestHeal ? 2 : 1)
                                      + _config.Mods.RestHealBonus);
                    return true;

                case TileType.Trap:
                {
                    double avoid = Balance.TrapAvoidChance + _config.Mods.TrapAvoidBonus
                                   + (_loadout.HasEffect(UniqueEffect.TrapWard) ? 0.25 : 0);
                    if (!_rng.Chance(avoid))
                        _player.TakeDamage((int)(_player.MaxHp * Balance.TrapDamageRatio));
                    return _player.IsAlive;
                }

                case TileType.Event:
                    ResolveEvent(floor);
                    return _player.IsAlive;

                default: // Start 등
                    return true;
            }
        }

        /// <summary>이벤트 선택지 시스템 (깊이 v5): 스탯 판정 리스크/리워드 — 재주·운의 전투 외 용도.</summary>
        private void ResolveEvent(int floor)
        {
            var def = EventCatalog.Roll(floor, _rng);
            int idx = EventCatalog.PickByExpectedValue(def, _sheet.Stats, _player.HpRatio, _gold);
            var choice = def.Choices[idx];

            double chance = choice.SuccessChance(_sheet.Stats);
            bool success = _rng.Chance(chance);
            // 마법사 패시브: 실패한 판정을 한 번 다시 굴림 (선택지 +1의 변형)
            if (!success && _character.BestEventChoice)
                success = _rng.Chance(chance);

            ApplyOutcome(success ? choice.Success : choice.Fail, floor);
        }

        private void ApplyOutcome(EventOutcome o, int floor)
        {
            if (o.Gold > 0) AddGold(o.Gold);
            else if (o.Gold < 0) _gold = Math.Max(0, _gold + o.Gold);
            if (o.HpRatio > 0) _player.HealRatio(o.HpRatio);
            else if (o.HpRatio < 0) _player.TakeDamage((int)(_player.MaxHp * -o.HpRatio));
            if (o.Gear) AddGear(floor);
            if (o.Item != null) _bag.Add(o.Item.Value);
            if (o.Xp > 0) ApplyXp(o.Xp);
        }

        private void ShopVisit(int floor)
        {
            double discount = 1.0 - _config.Mods.ShopDiscount;
            int potionPrice = (int)(Balance.PotionPrice(floor) * discount);
            int gearPrice = (int)(Balance.GearPrice(floor) * discount);

            if (_player.HpRatio < 0.6 && _gold >= potionPrice)
            {
                _gold -= potionPrice;
                _potions++;
            }
            // 부적이 없으면 하나 확보 (조작 수단 유지)
            int charmPrice = (int)(ConsumableBag.Price(ConsumableType.ParityCharm, floor) * discount);
            if (_bag.Charms == 0 && _gold >= charmPrice)
            {
                _gold -= charmPrice;
                _bag.Add(ConsumableType.ParityCharm);
            }
            while (_gold >= gearPrice)
            {
                _gold -= gearPrice;
                AddGear(floor);
            }
        }

        /// <summary>장비 획득: 슬롯 4종·등급 5단계 (02-시스템설계 3.1). 하위품은 자동 매각.</summary>
        private void AddGear(int floor)
        {
            _gearCount++;
            var item = EquipmentFactory.Generate(floor, _rng);
            if (_loadout.TryEquip(item))
            {
                _loadout.ApplyTo(_sheet);
                RefreshPlayer();
            }
            else
            {
                AddGold(10 + floor * 4); // 매각
            }
        }

        /// <summary>골드 획득 단일 지점 — 전설 '골드 탐지' 효과 적용.</summary>
        private void AddGold(int amount)
        {
            if (_loadout.HasEffect(UniqueEffect.GoldFind))
                amount = (int)(amount * 1.25);
            _gold += amount;
        }

        private List<Unit> MakeMonsters(int floor, bool elite)
        {
            int hp = (int)(Balance.MonsterHp(floor) * DangerMult);
            int atk = (int)(Balance.MonsterAtk(floor) * DangerMult);
            int def = Balance.MonsterDef(floor);
            var element = ElementTable.FloorElement(floor); // 층 테마 = 방어 속성
            if (elite)
                return new List<Unit> { new Unit("Elite", (int)(hp * 1.8), (int)(atk * 1.3), def, 9) { Element = element } };

            // 튜토리얼 구간(1~2층)은 1마리 고정, 이후 1~2마리
            int count = floor <= Balance.SoloMonsterFloors ? 1 : _rng.Next(1, 3);
            var list = new List<Unit>();
            for (int i = 0; i < count; i++)
            {
                var trait = TraitRules.Roll(floor, _rng); // 몬스터 개성 (깊이 v5)
                var mob = new Unit(trait == MonsterTrait.None ? "Mob" : TraitRules.Name(trait), hp, atk, def, 8)
                { Element = element, Trait = trait };
                if (trait == MonsterTrait.Shielded) mob.ShieldedHitsLeft = 2;
                list.Add(mob);
            }
            return list;
        }

        private bool FightBoss(int floor, DiceResult roll, int captured)
        {
            var boss = new Unit(
                "Boss",
                (int)(Balance.MonsterHp(floor) * Balance.BossHpMult * DangerMult),
                (int)(Balance.MonsterAtk(floor) * Balance.BossAtkMult * DangerMult),
                Balance.MonsterDef(floor), 9)
            {
                Element = ElementTable.FloorElement(floor),
                Trait = floor >= 10 ? MonsterTrait.Enrage : MonsterTrait.None, // 심층 보스: 광폭화
            };
            return Fight(new List<Unit> { boss }, roll, captured, floor, out _, isBoss: true);
        }

        private bool Fight(List<Unit> monsters, DiceResult roll, int captured, int floor, out bool fled, bool isBoss = false)
        {
            fled = false;

            // 연막탄: 죽을 판이면 전투 자체를 회피 (보스 제외, 보상 없음)
            if (!isBoss && _player.HpRatio < 0.20 && _potions == 0
                && _bag.TryUse(ConsumableType.Smoke))
            {
                fled = true;
                return true;
            }

            if (_policy.UsePotion(_player.HpRatio, _potions))
            {
                _potions--;
                _player.HealRatio(Balance.PotionHealRatio);
            }

            // 폭탄: 정예·보스전 개시 일격
            if ((isBoss || monsters.Any(m => m.Name == "Elite")) && _bag.TryUse(ConsumableType.Bomb))
            {
                int boom = ConsumableBag.BombDamage(floor);
                foreach (var m in monsters)
                {
                    m.TakeDamage(boom);
                    if (!m.IsAlive) _result.MonstersKilled++;
                }
                monsters.RemoveAll(m => !m.IsAlive);
                if (monsters.Count == 0) { GainBattleXp(1, floor, isBoss); return true; }
            }

            // 보드-전투 연동 (02-시스템설계 1.3):
            // 더블 = 선제공격, 럭키세븐 = 크리 +15%, 점령 칸 실드 (기사 ×2, 가디언 요새화 ×3)
            int perTile = 2 * (_character.DoubleCaptureShield ? 2 : 1)
                            * (_sheet.HasCaptureShieldTriple ? 3 : 1);
            var options = new BattleOptions
            {
                CritBonus = roll.IsLuckySeven ? 0.15 : 0,
                PlayerFirst = roll.IsDouble,
                StartShield = captured * perTile
                              + (int)(_player.MaxHp * _character.BarrierRatio), // 마법사: 마력 방벽
                Skills = _skills,
                ReviveAvailable = _reviveLeft,
                ReviveRatio = _sheet.HasReviveBoost ? 0.7 : 0.5,
                BurnOnHit = _sheet.BurnOnHitChance,
            };
            var result = BattleSimulator.Fight(_player, monsters, _rng.Derive(), options);

            if (result.ReviveUsed)
            {
                _reviveLeft = false;
                _result.ReviveUsed = true;
            }
            _result.MonstersKilled += result.MonstersKilled;
            if (!result.PlayerWon) return false;

            GainBattleXp(result.MonstersKilled, floor, isBoss);
            return true;
        }

        /// <summary>경험치 → 레벨업(스탯 자동 배분·스킬 학습) → 전직 (07-심화시스템 5).</summary>
        private void GainBattleXp(int kills, int floor, bool isBoss)
            => ApplyXp(kills * Balance.MonsterXp(floor) * (isBoss ? Balance.BossXpMult : 1));

        private void ApplyXp(int xp)
        {
            if (_sheet.GainXp(xp) <= 0) return;

            if (_sheet.CanJobChange)
            {
                var (a, b) = JobCatalog.PathsFor(_config.Character);
                _sheet.JobChange(_config.PreferPathA ? a : b);
            }
            _sheet.AutoAllocateStats();
            _sheet.AutoLearnSkills();
            RefreshPlayer();
            _skills = _sheet.EquipSkills();
        }
    }
}
