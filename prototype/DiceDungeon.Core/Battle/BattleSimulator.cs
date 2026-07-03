using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceDungeon.Core.Battle
{
    public sealed class BattleOptions
    {
        public double CritBonus;          // 럭키세븐 등 보드 연동
        public bool PlayerFirst;          // 더블 선제공격
        public int StartShield;           // 점령 칸 실드
        public List<Skill> Skills = new List<Skill>(); // 플레이어 스킬 (인스턴스, 쿨다운 상태 유지)
        public bool ReviveAvailable;      // 성직자 패시브 (런당 1회)
        public double ReviveRatio = 0.5;  // 부활 HP 비율 (하이프리스트 패시브로 0.7)
        public double BurnOnHit;          // 룬 나이트 패시브: 타격 시 화상 확률
    }

    public sealed class BattleResult
    {
        public bool PlayerWon { get; set; }
        public int Rounds { get; set; }
        public int MonstersKilled { get; set; }
        public bool ReviveUsed { get; set; }
    }

    /// <summary>
    /// 속도 기반 턴제 자동 전투 (순수 C#, 엔진 비의존). 피해 판정은 DamageCalc 공용.
    /// 플레이어 AI: 회복/실드 스킬(HP 50% 미만) > 최고 계수 공격 스킬 > 기본 공격.
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
                            if (!TryRevive(player, options, ref reviveLeft, result)) return Lost(result);
                        }
                    }
                    if (skip) continue;

                    if (actor == player)
                    {
                        PlayerAct(player, monsters, skills, rng, options, ref shield, result);
                    }
                    else
                    {
                        int dmg = DamageCalc.Compute(actor, player, null, rng, 0, out _, out bool miss);
                        if (miss) continue;
                        int absorbed = Math.Min(shield, dmg);
                        shield -= absorbed;
                        player.TakeDamage(dmg - absorbed);
                        if (!player.IsAlive && !TryRevive(player, options, ref reviveLeft, result))
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
                                      Rng rng, BattleOptions options, ref int shield, BattleResult result)
        {
            // 1) 위험하면 회복/실드 스킬
            var sustain = skills.FirstOrDefault(s => s.Ready && (s.HealRatio > 0 || s.ShieldRatio > 0));
            if (sustain != null && player.HpRatio < 0.5)
            {
                if (sustain.HealRatio > 0) player.HealRatio(sustain.HealRatio);
                if (sustain.ShieldRatio > 0) shield += (int)(player.MaxHp * sustain.ShieldRatio);
                sustain.CurrentCooldown = sustain.Cooldown;
                return;
            }

            // 2) 최고 계수 공격 스킬 (속성 유불리는 AI가 모름 — 플레이어 학습 요소)
            var attackSkill = skills.Where(s => s.Ready && s.Coef > 0)
                                    .OrderByDescending(s => s.Coef * (s.Aoe ? 1.5 : 1))
                                    .FirstOrDefault();
            bool aoe = attackSkill?.Aoe ?? false;
            if (attackSkill != null) attackSkill.CurrentCooldown = attackSkill.Cooldown;

            var targets = aoe
                ? monsters.Where(m => m.IsAlive).ToList()
                : monsters.Where(m => m.IsAlive).OrderBy(m => m.Hp).Take(1).ToList();

            foreach (var target in targets)
            {
                int dmg = DamageCalc.Compute(player, target, attackSkill, rng, options.CritBonus, out _, out bool miss);
                if (miss) continue;
                target.TakeDamage(dmg);
                if (target.IsAlive)
                {
                    if (attackSkill?.Applies != null)
                        target.Statuses.Apply(attackSkill.Applies.Value, attackSkill.ApplyStacks);
                    else if (options.BurnOnHit > 0 && rng.Chance(options.BurnOnHit))
                        target.Statuses.Apply(StatusType.Burn); // 발화 패시브
                }
                if (!target.IsAlive) result.MonstersKilled++;
            }
        }

        private static bool TryRevive(Unit player, BattleOptions options, ref bool reviveLeft, BattleResult result)
        {
            if (!reviveLeft) return false;
            reviveLeft = false;
            result.ReviveUsed = true;
            player.Hp = (int)(player.MaxHp * options.ReviveRatio);
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
    }
}
