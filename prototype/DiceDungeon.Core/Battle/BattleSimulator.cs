using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Data;

namespace DiceDungeon.Core.Battle
{
    public sealed class BattleOptions
    {
        public double CritBonus;          // 럭키세븐 등 보드 연동
        public bool PlayerFirst;          // 더블 선제공격
        public int StartShield;           // 점령 칸 실드
        public List<Skill> Skills = new List<Skill>(); // 플레이어 스킬 (인스턴스, 쿨다운 상태 유지)
        public bool ReviveAvailable;      // 성직자 패시브 (런당 1회)
        public double Evasion;            // 도적 패시브: 회피 확률
    }

    public sealed class BattleResult
    {
        public bool PlayerWon { get; set; }
        public int Rounds { get; set; }
        public int MonstersKilled { get; set; }
        public bool ReviveUsed { get; set; }
    }

    /// <summary>
    /// 속도 기반 턴제 자동 전투 (순수 C#, 엔진 비의존).
    /// 피해 공식: ATK × 계수 × 100/(100+DEF) × 크리배율 (02-시스템설계 1.2).
    /// 플레이어 AI: 회복 스킬(HP 50% 미만) > 최고 계수 공격 스킬 > 기본 공격.
    /// Presentation은 이 시뮬레이터의 결과를 받아 연출만 재생한다.
    /// </summary>
    public static class BattleSimulator
    {
        private const int MaxRounds = 50; // 무한 전투 방지 (교착 시 패배 처리)

        public static BattleResult Fight(Unit player, List<Unit> monsters, Rng rng, BattleOptions? options = null)
        {
            options = options ?? new BattleOptions();
            var result = new BattleResult();
            int shield = options.StartShield;
            var skills = options.Skills ?? new List<Skill>();
            bool reviveLeft = options.ReviveAvailable;

            for (int round = 1; round <= MaxRounds; round++)
            {
                result.Rounds = round;
                foreach (var actor in TurnOrder(player, monsters, options.PlayerFirst && round == 1))
                {
                    if (!actor.IsAlive) continue;

                    var (dot, skip) = actor.Statuses.Tick(actor);
                    if (dot > 0)
                    {
                        actor.TakeDamage(dot);
                        if (!actor.IsAlive)
                        {
                            if (actor != player) { result.MonstersKilled++; continue; }
                            if (!TryRevive(player, ref reviveLeft, result)) return Lost(result);
                        }
                    }
                    if (skip) continue;

                    if (actor == player)
                    {
                        PlayerAct(player, monsters, skills, rng, options.CritBonus, result);
                    }
                    else
                    {
                        if (options.Evasion > 0 && rng.Chance(options.Evasion)) continue; // 회피
                        int dmg = Damage(actor, player, 1.0, rng, 0);
                        int absorbed = Math.Min(shield, dmg);
                        shield -= absorbed;
                        player.TakeDamage(dmg - absorbed);
                        if (!player.IsAlive && !TryRevive(player, ref reviveLeft, result))
                            return Lost(result);
                    }
                }

                foreach (var s in skills)
                    if (s.CurrentCooldown > 0) s.CurrentCooldown--;

                if (monsters.All(m => !m.IsAlive))
                {
                    result.PlayerWon = true;
                    return result;
                }
            }

            return Lost(result); // 교착 = 패배 취급 (밸런스 경보용)
        }

        private static void PlayerAct(Unit player, List<Unit> monsters, List<Skill> skills,
                                      Rng rng, double critBonus, BattleResult result)
        {
            // 1) 위험하면 회복 스킬
            var healSkill = skills.FirstOrDefault(s => s.Ready && s.HealRatio > 0);
            if (healSkill != null && player.HpRatio < 0.5)
            {
                player.HealRatio(healSkill.HealRatio);
                healSkill.CurrentCooldown = healSkill.Cooldown;
                return;
            }

            // 2) 최고 계수 공격 스킬
            var attackSkill = skills.Where(s => s.Ready && s.Coef > 0)
                                    .OrderByDescending(s => s.Coef * (s.Aoe ? 1.5 : 1))
                                    .FirstOrDefault();
            double coef = attackSkill?.Coef ?? 1.0;
            bool aoe = attackSkill?.Aoe ?? false;
            if (attackSkill != null) attackSkill.CurrentCooldown = attackSkill.Cooldown;

            var targets = aoe
                ? monsters.Where(m => m.IsAlive).ToList()
                : monsters.Where(m => m.IsAlive).OrderBy(m => m.Hp).Take(1).ToList();

            foreach (var target in targets)
            {
                target.TakeDamage(Damage(player, target, coef, rng, critBonus));
                if (attackSkill?.Applies != null && target.IsAlive)
                    target.Statuses.Apply(attackSkill.Applies.Value, attackSkill.ApplyStacks);
                if (!target.IsAlive) result.MonstersKilled++;
            }
        }

        private static bool TryRevive(Unit player, ref bool reviveLeft, BattleResult result)
        {
            if (!reviveLeft) return false;
            reviveLeft = false;
            result.ReviveUsed = true;
            player.Hp = player.MaxHp / 2; // 부활: HP 50%
            return true;
        }

        private static BattleResult Lost(BattleResult result)
        {
            result.PlayerWon = false;
            return result;
        }

        private static IEnumerable<Unit> TurnOrder(Unit player, List<Unit> monsters, bool forcePlayerFirst)
        {
            var all = new List<Unit>(monsters) { player };
            if (forcePlayerFirst)
                return all.OrderByDescending(u => u == player ? int.MaxValue : u.EffectiveSpd);
            return all.OrderByDescending(u => u.EffectiveSpd);
        }

        private static int Damage(Unit attacker, Unit defender, double coef, Rng rng, double critBonus)
        {
            double dmg = attacker.Atk * coef * (100.0 / (100.0 + defender.Def));
            if (rng.Chance(attacker.CritChance + critBonus))
                dmg *= Balance.CritMultiplier;
            return Math.Max(1, (int)Math.Round(dmg));
        }
    }
}
