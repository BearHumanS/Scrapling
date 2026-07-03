using System;
using DiceDungeon.Core.Board;

namespace DiceDungeon.Core.Progression
{
    public enum EquipSlot { Weapon, Armor, Accessory, Dice }

    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>주사위 장비 효과 (01-게임기획서 4.3 — 운을 조작하는 재미의 핵심).</summary>
    public enum DiceMod
    {
        None,
        NoOnes,    // 1이 나온 눈을 한 번 다시 굴림
        Balanced,  // 합이 4 이하면 다시 굴림 (1회)
        Heavy,     // 합 -1 (최소 2), 대신 이 장비에 공격력 보너스
        Lucky      // 합이 6·8이면 30% 확률로 7로 보정 (럭키세븐 시너지)
    }

    /// <summary>전설 등급 고유 효과 (02-시스템설계 3.1).</summary>
    public enum UniqueEffect
    {
        None,
        LapFullHeal,   // 완주 시 HP 전체 회복
        GoldFind,      // 골드 획득 +25%
        TrapWard       // 함정 회피 +25%p
    }

    /// <summary>장비 인스턴스 (런 내 파밍, 슬롯당 1개 장착).</summary>
    public sealed class Equipment
    {
        public EquipSlot Slot { get; }
        public Rarity Rarity { get; }
        public string Name { get; }
        public int Atk { get; }
        public int Def { get; }
        public int Hp { get; }
        public double Crit { get; }
        public DiceMod DiceMod { get; }
        public UniqueEffect Effect { get; }

        public Equipment(EquipSlot slot, Rarity rarity, string name,
                         int atk = 0, int def = 0, int hp = 0, double crit = 0,
                         DiceMod diceMod = DiceMod.None, UniqueEffect effect = UniqueEffect.None)
        {
            Slot = slot; Rarity = rarity; Name = name;
            Atk = atk; Def = def; Hp = hp; Crit = crit;
            DiceMod = diceMod; Effect = effect;
        }

        /// <summary>시뮬레이션/자동 장착용 비교 점수.</summary>
        public double Score => Atk * 3 + Def * 2.5 + Hp * 0.35 + Crit * 250
                               + (DiceMod != DiceMod.None ? 8 : 0)
                               + (Effect != UniqueEffect.None ? 15 : 0);
    }

    public static class EquipmentFactory
    {
        private static readonly string[] RarityNames = { "낡은", "쓸만한", "정교한", "영웅의", "전설의" };
        private static readonly double[] RarityMult = { 1.0, 1.35, 1.8, 2.4, 3.2 };

        /// <summary>층 비례 등급 확률: 깊을수록 상급 (전설은 5층부터).</summary>
        public static Rarity RollRarity(int floor, Rng rng)
        {
            double v = rng.NextDouble();
            double legendary = floor >= 5 ? 0.02 + floor * 0.004 : 0;
            double epic = 0.05 + floor * 0.008;
            double rare = 0.15 + floor * 0.01;
            double uncommon = 0.30;
            if (v < legendary) return Rarity.Legendary;
            if (v < legendary + epic) return Rarity.Epic;
            if (v < legendary + epic + rare) return Rarity.Rare;
            if (v < legendary + epic + rare + uncommon) return Rarity.Uncommon;
            return Rarity.Common;
        }

        public static Equipment Generate(int floor, Rng rng)
        {
            var slot = (EquipSlot)rng.Next(0, 4);
            var rarity = RollRarity(floor, rng);
            double scale = (1 + 0.22 * (floor - 1)) * RarityMult[(int)rarity];
            string prefix = RarityNames[(int)rarity];
            var effect = rarity == Rarity.Legendary ? RollEffect(rng) : UniqueEffect.None;

            switch (slot)
            {
                case EquipSlot.Weapon:
                    return new Equipment(slot, rarity, $"{prefix} 검",
                        atk: Math.Max(1, (int)(3 * scale)), effect: effect);
                case EquipSlot.Armor:
                    return new Equipment(slot, rarity, $"{prefix} 갑옷",
                        def: Math.Max(1, (int)(2 * scale)), hp: (int)(14 * scale), effect: effect);
                case EquipSlot.Accessory:
                    return new Equipment(slot, rarity, $"{prefix} 반지",
                        hp: (int)(8 * scale), crit: 0.015 * RarityMult[(int)rarity], effect: effect);
                default: // 주사위: 등급이 오를수록 좋은 조작 효과
                    return DiceEquip(rarity, prefix, scale, effect);
            }
        }

        private static Equipment DiceEquip(Rarity rarity, string prefix, double scale, UniqueEffect effect)
        {
            switch (rarity)
            {
                case Rarity.Common:
                    return new Equipment(EquipSlot.Dice, rarity, $"{prefix} 주사위", hp: (int)(5 * scale));
                case Rarity.Uncommon:
                    return new Equipment(EquipSlot.Dice, rarity, "균형 주사위", diceMod: DiceMod.Balanced);
                case Rarity.Rare:
                    return new Equipment(EquipSlot.Dice, rarity, "정직한 주사위", diceMod: DiceMod.NoOnes);
                case Rarity.Epic:
                    return new Equipment(EquipSlot.Dice, rarity, "무거운 주사위",
                        atk: (int)(4 * scale), diceMod: DiceMod.Heavy);
                default:
                    return new Equipment(EquipSlot.Dice, rarity, "운명의 주사위",
                        diceMod: DiceMod.Lucky, effect: effect);
            }
        }

        private static UniqueEffect RollEffect(Rng rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return UniqueEffect.LapFullHeal;
                case 1: return UniqueEffect.GoldFind;
                default: return UniqueEffect.TrapWard;
            }
        }
    }

    /// <summary>
    /// 장착 상태 (슬롯당 1개). RunController(시뮬)와 Unity GameManager가 공용.
    /// 합계는 CharacterSheet.Gear*에 반영해 Refresh 한 곳에서만 재계산되게 한다.
    /// </summary>
    public sealed class EquipmentLoadout
    {
        private readonly Equipment[] _slots = new Equipment[4];

        public Equipment Get(EquipSlot slot) => _slots[(int)slot];

        /// <summary>점수가 더 높으면 교체 장착. 반환: 장착 여부.</summary>
        public bool TryEquip(Equipment item)
        {
            var cur = _slots[(int)item.Slot];
            if (cur != null && cur.Score >= item.Score) return false;
            _slots[(int)item.Slot] = item;
            return true;
        }

        public int TotalAtk { get { int s = 0; foreach (var e in _slots) if (e != null) s += e.Atk; return s; } }
        public int TotalDef { get { int s = 0; foreach (var e in _slots) if (e != null) s += e.Def; return s; } }
        public int TotalHp { get { int s = 0; foreach (var e in _slots) if (e != null) s += e.Hp; return s; } }
        public double TotalCrit { get { double s = 0; foreach (var e in _slots) if (e != null) s += e.Crit; return s; } }

        public DiceMod ActiveDiceMod => _slots[(int)EquipSlot.Dice]?.DiceMod ?? DiceMod.None;

        public bool HasEffect(UniqueEffect effect)
        {
            foreach (var e in _slots)
                if (e != null && e.Effect == effect) return true;
            return false;
        }

        /// <summary>시트에 장비 합계 반영 (호출 후 sheet.Refresh 필요).</summary>
        public void ApplyTo(CharacterSheet sheet)
        {
            sheet.GearAtk = TotalAtk;
            sheet.GearDef = TotalDef;
            sheet.GearHp = TotalHp;
            sheet.GearCrit = TotalCrit;
        }
    }

    /// <summary>주사위 장비 효과 적용 (보드 이동 전 단계).</summary>
    public static class DiceModRules
    {
        public static DiceResult Apply(DiceResult roll, DiceMod mod, DiceRoller roller, Rng rng)
        {
            switch (mod)
            {
                case DiceMod.NoOnes:
                {
                    int d1 = roll.Die1 == 1 ? rng.Next(1, 7) : roll.Die1;
                    int d2 = roll.Die2 == 1 ? rng.Next(1, 7) : roll.Die2;
                    return new DiceResult(d1, d2);
                }
                case DiceMod.Balanced:
                    return roll.Sum <= 4 ? roller.Roll() : roll;
                case DiceMod.Heavy:
                {
                    // 합 -1 (최소 2): 큰 눈 하나를 줄인다
                    if (roll.Sum <= 2) return roll;
                    return roll.Die1 >= roll.Die2
                        ? new DiceResult(Math.Max(1, roll.Die1 - 1), roll.Die2)
                        : new DiceResult(roll.Die1, Math.Max(1, roll.Die2 - 1));
                }
                case DiceMod.Lucky:
                    if ((roll.Sum == 6 || roll.Sum == 8) && rng.Chance(0.3))
                        return roll.Sum == 6
                            ? new DiceResult(roll.Die1, roll.Die2 + 1)
                            : new DiceResult(roll.Die1, roll.Die2 - 1);
                    return roll;
                default:
                    return roll;
            }
        }
    }
}
