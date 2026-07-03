using System;

namespace DiceDungeon.Core.Battle
{
    /// <summary>
    /// 스킬 정의 (데이터 주도 — 본편에서는 JSON 테이블로 이전).
    /// 효과는 계수/상태이상/광역/회복의 조합으로 기술한다.
    /// </summary>
    public sealed class Skill
    {
        public string Name { get; }
        public int Cooldown { get; }
        /// <summary>공격 계수 (0이면 비공격 스킬).</summary>
        public double Coef { get; }
        public bool Aoe { get; }
        public StatusType? Applies { get; }
        public int ApplyStacks { get; }
        /// <summary>최대 HP 비례 회복 (회복 스킬).</summary>
        public double HealRatio { get; }
        /// <summary>마법 여부 (마법공격/마법방어 사용) — 07-심화시스템 1.</summary>
        public bool Magic { get; }
        /// <summary>공격 속성 (07-심화시스템 4).</summary>
        public Element Element { get; }
        /// <summary>시전 시 최대 HP 비례 실드 획득.</summary>
        public double ShieldRatio { get; }

        public int CurrentCooldown { get; set; }
        public bool Ready => CurrentCooldown <= 0;

        public Skill(string name, int cooldown, double coef = 0, bool aoe = false,
                     StatusType? applies = null, int applyStacks = 1, double healRatio = 0,
                     bool magic = false, Element element = Element.Neutral, double shieldRatio = 0)
        {
            Name = name;
            Cooldown = cooldown;
            Coef = coef;
            Aoe = aoe;
            Applies = applies;
            ApplyStacks = applyStacks;
            HealRatio = healRatio;
            Magic = magic;
            Element = element;
            ShieldRatio = shieldRatio;
        }

        /// <summary>런 시작 시 쿨다운 초기화된 사본 (정의 객체는 불변으로 유지).</summary>
        public Skill Instance() => new Skill(Name, Cooldown, Coef, Aoe, Applies, ApplyStacks, HealRatio, Magic, Element, ShieldRatio);
    }
}
