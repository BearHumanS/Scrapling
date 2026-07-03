using System;

namespace DiceDungeon.Core.Battle
{
    /// <summary>
    /// 통합 피해 판정 (BattleSimulator·InteractiveBattle 공용) — 07-심화시스템.
    /// 명중/회피 → 물리·마법 분기 → 속성 배율 → 크리티컬.
    /// </summary>
    public static class DamageCalc
    {
        public static int Compute(Unit attacker, Unit defender, Skill skill, Rng rng,
                                  double critBonus, out bool crit, out bool miss)
        {
            crit = false;

            double hitChance = Math.Max(0.5, Math.Min(1.0, attacker.Accuracy - defender.Evasion));
            miss = !rng.Chance(hitChance);
            if (miss) return 0;

            bool magic = skill != null && skill.Magic;
            double atkStat = magic ? attacker.Matk : attacker.Atk;
            double defStat = magic ? defender.Mdef : defender.Def;
            double coef = skill != null && skill.Coef > 0 ? skill.Coef : 1.0;
            var element = skill != null ? skill.Element : Element.Neutral;

            double dmg = atkStat * coef * (100.0 / (100.0 + defStat))
                         * ElementTable.Multiplier(element, defender.Element);

            // 콤보 시너지: 빙결된 대상은 크리티컬 +25%p (스킬 순서에 의미 부여)
            if (defender.Statuses.Has(StatusType.Freeze)) critBonus += 0.25;

            crit = rng.Chance(attacker.CritChance + critBonus);
            if (crit) dmg *= Data.Balance.CritMultiplier;

            return Math.Max(1, (int)Math.Round(dmg));
        }
    }
}
