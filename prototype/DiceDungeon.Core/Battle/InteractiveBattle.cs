using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Data;

namespace DiceDungeon.Core.Battle
{
    public enum PlayerActionType { Attack, Defend, Potion, Skill }

    public enum BattleEventType
    {
        PlayerHit, MonsterHit, Miss, PlayerMiss, ShieldAbsorb, ShieldGain, Heal, StatusApplied,
        StatusTick, Stunned, MonsterDied, PlayerDied, Revived, Victory, Defeat
    }

    /// <summary>Presentation이 순서대로 재생할 전투 사건 하나.</summary>
    public readonly struct BattleEvent
    {
        public BattleEventType Type { get; }
        public Unit? Actor { get; }
        public Unit? Target { get; }
        public int Amount { get; }
        public bool Crit { get; }

        public BattleEvent(BattleEventType type, Unit? actor = null, Unit? target = null, int amount = 0, bool crit = false)
        {
            Type = type; Actor = actor; Target = target; Amount = amount; Crit = crit;
        }
    }

    /// <summary>
    /// 수동 전투 (05-백로그 Week 3): UI가 플레이어 행동을 넣으면
    /// 그 행동 + 몬스터 반격까지 한 라운드를 판정하고 이벤트 목록을 돌려준다.
    /// BattleSimulator(자동)와 같은 규칙·수치를 쓰되 입력 대기형이다.
    /// 엔진 비의존 — Sim의 selftest로 검증하고 Unity BattleView가 소비한다.
    /// </summary>
    public sealed class InteractiveBattle
    {
        public Unit Player { get; }
        public IReadOnlyList<Unit> Monsters => _monsters;
        public bool Over { get; private set; }
        public bool PlayerWon { get; private set; }
        public int MonstersKilled { get; private set; }
        public bool ReviveUsed { get; private set; }
        public int Potions { get; private set; }
        public int Shield { get; private set; }
        public IReadOnlyList<Skill> Skills => _skills;

        private readonly List<Unit> _monsters;
        private readonly Rng _rng;
        private readonly BattleOptions _options;
        private readonly List<Skill> _skills;
        private bool _defending;
        private bool _reviveLeft;
        private bool _firstRound = true;

        public InteractiveBattle(Unit player, List<Unit> monsters, Rng rng, BattleOptions options, int potions)
        {
            Player = player;
            _monsters = monsters;
            _rng = rng;
            _options = options ?? new BattleOptions();
            _skills = _options.Skills ?? new List<Skill>();
            _reviveLeft = _options.ReviveAvailable;
            Potions = potions;
            Shield = _options.StartShield;
        }

        public bool CanUseSkill(int index) =>
            index >= 0 && index < _skills.Count && _skills[index].Ready && !Over;

        /// <summary>
        /// 플레이어 행동 1회 + 몬스터 전원의 반격 = 한 라운드.
        /// 반환된 이벤트를 순서대로 연출하면 된다.
        /// </summary>
        public List<BattleEvent> DoRound(PlayerActionType action, int skillIndex = -1)
        {
            var events = new List<BattleEvent>();
            if (Over) return events;
            _defending = false;

            // 플레이어 턴 시작: 상태이상 (본편: 플레이어도 디버프 가능. 프로토타입은 몬스터만 걸림)
            switch (action)
            {
                case PlayerActionType.Attack:
                    PlayerStrike(events, null);
                    break;

                case PlayerActionType.Defend:
                    _defending = true; // 이번 라운드 받는 피해 50%↓ (02-시스템설계 1.1)
                    break;

                case PlayerActionType.Potion:
                    if (Potions > 0)
                    {
                        Potions--;
                        int heal = (int)(Player.MaxHp * Balance.PotionHealRatio);
                        Player.Heal(heal);
                        events.Add(new BattleEvent(BattleEventType.Heal, Player, Player, heal));
                    }
                    break;

                case PlayerActionType.Skill:
                    if (!CanUseSkill(skillIndex)) break;
                    var skill = _skills[skillIndex];
                    skill.CurrentCooldown = skill.Cooldown;
                    if (skill.HealRatio > 0 || skill.ShieldRatio > 0)
                    {
                        if (skill.HealRatio > 0)
                        {
                            int heal = (int)(Player.MaxHp * skill.HealRatio);
                            Player.Heal(heal);
                            events.Add(new BattleEvent(BattleEventType.Heal, Player, Player, heal));
                        }
                        if (skill.ShieldRatio > 0)
                        {
                            int gain = (int)(Player.MaxHp * skill.ShieldRatio);
                            Shield += gain;
                            events.Add(new BattleEvent(BattleEventType.ShieldGain, Player, Player, gain));
                        }
                    }
                    else
                    {
                        PlayerStrike(events, skill);
                    }
                    break;
            }

            if (CheckVictory(events)) return events;

            // 몬스터 턴 (첫 라운드 더블 선제공격이면 몬스터는 이번 라운드 행동 불가)
            bool monstersSkip = _firstRound && _options.PlayerFirst;
            _firstRound = false;
            if (!monstersSkip)
            {
                foreach (var m in _monsters.Where(m => m.IsAlive).ToList())
                {
                    var (dot, skip) = m.Statuses.Tick(m);
                    if (dot > 0)
                    {
                        m.TakeDamage(dot);
                        events.Add(new BattleEvent(BattleEventType.StatusTick, m, m, dot));
                        if (!m.IsAlive)
                        {
                            MonstersKilled++;
                            events.Add(new BattleEvent(BattleEventType.MonsterDied, m, m));
                            continue;
                        }
                    }
                    if (skip)
                    {
                        events.Add(new BattleEvent(BattleEventType.Stunned, m, m));
                        continue;
                    }

                    int dmg = DamageCalc.Compute(m, Player, null, _rng, 0, out _, out bool monsterMiss);
                    if (monsterMiss)
                    {
                        events.Add(new BattleEvent(BattleEventType.Miss, m, Player));
                        continue;
                    }
                    if (_defending) dmg = Math.Max(1, dmg / 2);
                    int absorbed = Math.Min(Shield, dmg);
                    if (absorbed > 0)
                    {
                        Shield -= absorbed;
                        events.Add(new BattleEvent(BattleEventType.ShieldAbsorb, m, Player, absorbed));
                    }
                    Player.TakeDamage(dmg - absorbed);
                    events.Add(new BattleEvent(BattleEventType.MonsterHit, m, Player, dmg - absorbed));

                    if (!Player.IsAlive)
                    {
                        if (_reviveLeft)
                        {
                            _reviveLeft = false;
                            ReviveUsed = true;
                            Player.Hp = (int)(Player.MaxHp * _options.ReviveRatio);
                            events.Add(new BattleEvent(BattleEventType.Revived, Player, Player, Player.Hp));
                        }
                        else
                        {
                            Over = true;
                            PlayerWon = false;
                            events.Add(new BattleEvent(BattleEventType.PlayerDied, Player, Player));
                            events.Add(new BattleEvent(BattleEventType.Defeat));
                            return events;
                        }
                    }
                }
            }

            // 라운드 종료: 쿨다운 감소
            foreach (var s in _skills)
                if (s.CurrentCooldown > 0) s.CurrentCooldown--;

            CheckVictory(events);
            return events;
        }

        private void PlayerStrike(List<BattleEvent> events, Skill skill)
        {
            bool aoe = skill?.Aoe ?? false;
            var targets = aoe
                ? _monsters.Where(m => m.IsAlive).ToList()
                : _monsters.Where(m => m.IsAlive).OrderBy(m => m.Hp).Take(1).ToList();

            foreach (var t in targets)
            {
                int dmg = DamageCalc.Compute(Player, t, skill, _rng, _options.CritBonus, out bool crit, out bool miss);
                if (miss)
                {
                    events.Add(new BattleEvent(BattleEventType.PlayerMiss, Player, t));
                    continue;
                }
                t.TakeDamage(dmg);
                events.Add(new BattleEvent(BattleEventType.PlayerHit, Player, t, dmg, crit));
                if (t.IsAlive)
                {
                    if (skill?.Applies != null)
                    {
                        t.Statuses.Apply(skill.Applies.Value, Math.Max(1, skill.ApplyStacks));
                        events.Add(new BattleEvent(BattleEventType.StatusApplied, Player, t));
                    }
                    else if (_options.BurnOnHit > 0 && _rng.Chance(_options.BurnOnHit))
                    {
                        t.Statuses.Apply(StatusType.Burn);
                        events.Add(new BattleEvent(BattleEventType.StatusApplied, Player, t));
                    }
                }
                if (!t.IsAlive)
                {
                    MonstersKilled++;
                    events.Add(new BattleEvent(BattleEventType.MonsterDied, Player, t));
                }
            }
        }

        private bool CheckVictory(List<BattleEvent> events)
        {
            if (!Over && _monsters.All(m => !m.IsAlive))
            {
                Over = true;
                PlayerWon = true;
                events.Add(new BattleEvent(BattleEventType.Victory));
            }
            return Over;
        }

    }
}
