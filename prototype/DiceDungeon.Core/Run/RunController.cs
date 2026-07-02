using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Board;
using DiceDungeon.Core.Data;

namespace DiceDungeon.Core.Run
{
    /// <summary>런 시작 조건. 마을 메타 성장(훈련소·유물)이 여기에 반영된다.</summary>
    public sealed class RunConfig
    {
        public int BonusHp { get; set; }
        public int BonusAtk { get; set; }
        public int BonusDef { get; set; }
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
    }

    /// <summary>런 중 의사결정. 실제 게임에서는 UI 입력, 시뮬레이션에서는 휴리스틱.</summary>
    public interface IPlayerPolicy
    {
        /// <summary>층 클리어 후: true = 하강, false = 귀환 (01-게임기획서 5).</summary>
        bool Descend(int nextFloor, double hpRatio);
        /// <summary>전투 직전 포션 사용 여부.</summary>
        bool UsePotion(double hpRatio, int potions);
    }

    /// <summary>기본 휴리스틱: HP 여유가 있으면 하강, 위험하면 포션.</summary>
    public sealed class GreedyPolicy : IPlayerPolicy
    {
        private readonly double _descendHpThreshold;

        public GreedyPolicy(double descendHpThreshold = 0.35)
        {
            _descendHpThreshold = descendHpThreshold;
        }

        public bool Descend(int nextFloor, double hpRatio) => hpRatio >= _descendHpThreshold;
        public bool UsePotion(double hpRatio, int potions) => potions > 0 && hpRatio < 0.45;
    }

    /// <summary>
    /// 런 전체 오케스트레이션: 층 생성 → 주사위 → 이동 → 타일 처리 → 한 바퀴 → 보스 → 하강/귀환.
    /// 순수 C#이므로 단위 테스트와 대량 시뮬레이션(밸런스 검증)에 그대로 사용한다.
    /// </summary>
    public sealed class RunController
    {
        private readonly Rng _rng;
        private readonly IPlayerPolicy _policy;
        private readonly Unit _player;
        private readonly RunResult _result = new RunResult();

        private int _gold;
        private int _potions = 2;
        private int _gearCount;

        public RunController(int seed, RunConfig config, IPlayerPolicy policy)
        {
            _rng = new Rng(seed);
            _policy = policy;
            _player = new Unit(
                "Player",
                Balance.PlayerHp + config.BonusHp,
                Balance.PlayerAtk + config.BonusAtk,
                Balance.PlayerDef + config.BonusDef,
                Balance.PlayerSpd,
                Balance.PlayerCritChance);
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
            int stones = Balance.SoulstonesForRun(_result.FloorsCleared, _result.MonstersKilled);
            if (_result.DeathFloor > 0)
                stones = (int)(stones * Balance.DeathSoulstonePenalty);
            _result.Soulstones = stones;
            return _result;
        }

        /// <returns>false = 사망.</returns>
        private bool PlayFloor(int floor)
        {
            var tiles = FloorGenerator.Generate(floor, _rng.Derive());
            var dice = new DiceRoller(_rng.Derive());
            int position = 0;

            while (true)
            {
                var roll = dice.Roll();
                _result.DiceRolls++;
                int next = position + roll.Sum;
                bool lapComplete = next >= Balance.BoardSize;
                position = next % Balance.BoardSize;

                if (lapComplete)
                {
                    // 완주 보너스 → 보스 게이트 (프로토타입: 완주 즉시 보스전)
                    _gold += Balance.LapBonusGold(floor);
                    return FightBoss(floor, roll);
                }

                if (!ResolveTile(tiles[position], floor, roll))
                    return false;
            }
        }

        private bool ResolveTile(Tile tile, int floor, DiceResult roll)
        {
            switch (tile.Type)
            {
                case TileType.Battle:
                case TileType.Elite:
                    if (tile.Captured)
                    {
                        _player.HealRatio(Balance.CapturedTileHealRatio); // 내 땅: 회복
                        return true;
                    }
                    bool elite = tile.Type == TileType.Elite;
                    if (!Fight(MakeMonsters(floor, elite), roll)) return false;
                    tile.Captured = true;
                    _gold += Balance.BattleGold(floor) * (elite ? 2 : 1);
                    if (elite) AddGear(floor);
                    return true;

                case TileType.Treasure:
                    if (_rng.Chance(Balance.MimicChance))
                        return Fight(MakeMonsters(floor, elite: false), roll); // 미믹 기습
                    if (_rng.Chance(0.5)) AddGear(floor);
                    else _gold += Balance.TreasureGold(floor);
                    return true;

                case TileType.Shop:
                    ShopVisit(floor);
                    return true;

                case TileType.Rest:
                    _player.HealRatio(Balance.RestHealRatio);
                    return true;

                case TileType.Trap:
                    if (!_rng.Chance(Balance.TrapAvoidChance))
                        _player.TakeDamage((int)(_player.MaxHp * Balance.TrapDamageRatio));
                    return _player.IsAlive;

                case TileType.Event:
                    ResolveEvent(floor);
                    return _player.IsAlive;

                default: // Start 등
                    return true;
            }
        }

        private void ResolveEvent(int floor)
        {
            // 프로토타입 이벤트 3종: 횡재 / 손해 / 장비 (본편은 이벤트 30종 테이블화)
            double v = _rng.NextDouble();
            if (v < 0.40) _gold += Balance.TreasureGold(floor);
            else if (v < 0.75) _player.TakeDamage((int)(_player.MaxHp * 0.08));
            else AddGear(floor);
        }

        private void ShopVisit(int floor)
        {
            if (_player.HpRatio < 0.6 && _gold >= Balance.PotionPrice(floor))
            {
                _gold -= Balance.PotionPrice(floor);
                _potions++;
            }
            while (_gold >= Balance.GearPrice(floor))
            {
                _gold -= Balance.GearPrice(floor);
                AddGear(floor);
            }
        }

        private void AddGear(int floor)
        {
            _gearCount++;
            _player.Atk += Balance.GearAtkBonus(floor);
            _player.Def += Balance.GearDefBonus(floor);
            int hp = Balance.GearHpBonus(floor);
            _player.MaxHp += hp;
            _player.Heal(hp);
        }

        private List<Unit> MakeMonsters(int floor, bool elite)
        {
            int hp = Balance.MonsterHp(floor);
            int atk = Balance.MonsterAtk(floor);
            int def = Balance.MonsterDef(floor);
            if (elite)
                return new List<Unit> { new Unit("Elite", (int)(hp * 1.8), (int)(atk * 1.3), def, 9) };

            // 튜토리얼 구간(1~2층)은 1마리 고정, 이후 1~2마리
            int count = floor <= Balance.SoloMonsterFloors ? 1 : _rng.Next(1, 3);
            var list = new List<Unit>();
            for (int i = 0; i < count; i++)
                list.Add(new Unit("Mob", hp, atk, def, 8));
            return list;
        }

        private bool FightBoss(int floor, DiceResult roll)
        {
            var boss = new Unit(
                "Boss",
                (int)(Balance.MonsterHp(floor) * Balance.BossHpMult),
                (int)(Balance.MonsterAtk(floor) * Balance.BossAtkMult),
                Balance.MonsterDef(floor), 9);
            return Fight(new List<Unit> { boss }, roll);
        }

        private bool Fight(List<Unit> monsters, DiceResult roll)
        {
            if (_policy.UsePotion(_player.HpRatio, _potions))
            {
                _potions--;
                _player.HealRatio(Balance.PotionHealRatio);
            }

            // 보드-전투 연동: 더블 = 선제공격, 럭키세븐 = 크리 +15% (02-시스템설계 1.3)
            var result = BattleSimulator.Fight(
                _player, monsters, _rng.Derive(),
                critBonus: roll.IsLuckySeven ? 0.15 : 0,
                playerFirst: roll.IsDouble,
                startShield: 0);

            _result.MonstersKilled += result.MonstersKilled;
            return result.PlayerWon;
        }
    }
}
