using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Data;

namespace DiceDungeon.Core.Battle
{
    public sealed class BattleResult
    {
        public bool PlayerWon { get; set; }
        public int Rounds { get; set; }
        public int MonstersKilled { get; set; }
    }

    /// <summary>
    /// 속도 기반 턴제 자동 전투 (순수 C#, 엔진 비의존).
    /// 피해 공식: ATK × 100/(100+DEF) × 크리배율 (02-시스템설계 1.2).
    /// Presentation은 이 시뮬레이터가 뱉는 결과를 받아 연출만 재생한다.
    /// </summary>
    public static class BattleSimulator
    {
        private const int MaxRounds = 50; // 무한 전투 방지 (교착 시 패배 처리)

        /// <param name="critBonus">럭키세븐 등 보드 연동 크리 보너스.</param>
        /// <param name="playerFirst">더블 선제공격 여부 (보드-전투 연동).</param>
        /// <param name="startShield">점령 칸 수 비례 시작 실드.</param>
        public static BattleResult Fight(
            Unit player, List<Unit> monsters, Rng rng,
            double critBonus = 0, bool playerFirst = false, int startShield = 0)
        {
            var result = new BattleResult();
            int shield = startShield;

            for (int round = 1; round <= MaxRounds; round++)
            {
                result.Rounds = round;
                foreach (var actor in TurnOrder(player, monsters, playerFirst && round == 1))
                {
                    if (!actor.IsAlive) continue;

                    if (actor == player)
                    {
                        var target = monsters.Where(m => m.IsAlive)
                                             .OrderBy(m => m.Hp).FirstOrDefault();
                        if (target == null) break;
                        target.TakeDamage(Damage(player, target, rng, critBonus));
                        if (!target.IsAlive) result.MonstersKilled++;
                    }
                    else
                    {
                        int dmg = Damage(actor, player, rng, 0);
                        int absorbed = Math.Min(shield, dmg);
                        shield -= absorbed;
                        player.TakeDamage(dmg - absorbed);
                        if (!player.IsAlive)
                        {
                            result.PlayerWon = false;
                            return result;
                        }
                    }
                }

                if (monsters.All(m => !m.IsAlive))
                {
                    result.PlayerWon = true;
                    return result;
                }
            }

            result.PlayerWon = false; // 교착 = 패배 취급 (밸런스 경보용)
            return result;
        }

        private static IEnumerable<Unit> TurnOrder(Unit player, List<Unit> monsters, bool forcePlayerFirst)
        {
            var all = new List<Unit>(monsters) { player };
            if (forcePlayerFirst)
                return all.OrderByDescending(u => u == player ? int.MaxValue : u.Spd);
            return all.OrderByDescending(u => u.Spd);
        }

        private static int Damage(Unit attacker, Unit defender, Rng rng, double critBonus)
        {
            double dmg = attacker.Atk * (100.0 / (100.0 + defender.Def));
            if (rng.Chance(attacker.CritChance + critBonus))
                dmg *= Balance.CritMultiplier;
            return Math.Max(1, (int)Math.Round(dmg));
        }
    }
}
