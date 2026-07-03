using System;

namespace DiceDungeon.Core.Meta
{
    public enum RelicId
    {
        StartGold,      // 시작 골드 +30/Lv
        StartPotion,    // 시작 포션 +1/Lv (최대 3)
        RestHeal,       // 쉼터 회복 +5%p/Lv
        CaptureHeal,    // 점령 칸 회복 +3%p/Lv
        TrapSense,      // 함정 회피 +8%p/Lv
        AtkCore,        // 공격 +2/Lv
        DefCore,        // 방어 +1/Lv
        VitalCore,      // 최대 HP +15/Lv
        LuckyCharm,     // 크리 확률 +2%p/Lv
        Bargain,        // 상점 가격 -5%/Lv
        SoulHarvest,    // 소울스톤 획득 +8%/Lv
        BossTrophy      // 보스(랩) 보너스 골드 +25%/Lv
    }

    /// <summary>유물 카탈로그: 출시 스코프 12종 × 5레벨 (02-시스템설계 3.3).</summary>
    public static class RelicCatalog
    {
        public const int Count = 12;
        public const int MaxLevel = 5;

        /// <summary>레벨업 비용 (소울스톤): 60 × 1.75^현재레벨 — 지수 싱크 (02-시스템설계 4.2).
        /// (v3) 40×1.6에서 상향 — 경제 시뮬레이션에서 60런 내 전 유물 만렙 도달해 싱크 소진.</summary>
        public static int UpgradeCost(int currentLevel)
            => (int)Math.Round(60 * Math.Pow(1.75, currentLevel));

        public static string Name(RelicId id)
        {
            switch (id)
            {
                case RelicId.StartGold: return "여행자의 지갑";
                case RelicId.StartPotion: return "연금술 가방";
                case RelicId.RestHeal: return "따뜻한 담요";
                case RelicId.CaptureHeal: return "정착민의 깃발";
                case RelicId.TrapSense: return "고양이의 감각";
                case RelicId.AtkCore: return "힘의 결정";
                case RelicId.DefCore: return "수호의 결정";
                case RelicId.VitalCore: return "생명의 결정";
                case RelicId.LuckyCharm: return "네잎클로버";
                case RelicId.Bargain: return "상인의 인장";
                case RelicId.SoulHarvest: return "영혼 수확기";
                case RelicId.BossTrophy: return "왕관 수집가";
                default: return id.ToString();
            }
        }
    }

    /// <summary>유물 레벨 배열 → 런에 적용되는 보정치 집계. RunController가 소비한다.</summary>
    public sealed class RelicModifiers
    {
        public int StartGold;
        public int StartPotions;
        public double RestHealBonus;      // 가산 %p
        public double CaptureHealBonus;   // 가산 %p
        public double TrapAvoidBonus;     // 가산 %p
        public int BonusAtk;
        public int BonusDef;
        public int BonusHp;
        public double BonusCrit;
        public double ShopDiscount;       // 0.05 = 5% 할인
        public double SoulstoneMult = 1.0;
        public double LapGoldMult = 1.0;

        public static RelicModifiers From(int[] relicLevels)
        {
            var m = new RelicModifiers();
            if (relicLevels == null) return m;
            for (int i = 0; i < relicLevels.Length && i < RelicCatalog.Count; i++)
            {
                int lv = relicLevels[i];
                if (lv <= 0) continue;
                switch ((RelicId)i)
                {
                    case RelicId.StartGold: m.StartGold += 30 * lv; break;
                    case RelicId.StartPotion: m.StartPotions += Math.Min(lv, 3); break;
                    case RelicId.RestHeal: m.RestHealBonus += 0.05 * lv; break;
                    case RelicId.CaptureHeal: m.CaptureHealBonus += 0.03 * lv; break;
                    case RelicId.TrapSense: m.TrapAvoidBonus += 0.08 * lv; break;
                    case RelicId.AtkCore: m.BonusAtk += 2 * lv; break;
                    case RelicId.DefCore: m.BonusDef += 1 * lv; break;
                    case RelicId.VitalCore: m.BonusHp += 15 * lv; break;
                    case RelicId.LuckyCharm: m.BonusCrit += 0.02 * lv; break;
                    case RelicId.Bargain: m.ShopDiscount += 0.05 * lv; break;
                    case RelicId.SoulHarvest: m.SoulstoneMult += 0.08 * lv; break;
                    case RelicId.BossTrophy: m.LapGoldMult += 0.25 * lv; break;
                }
            }
            return m;
        }
    }
}
