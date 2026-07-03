using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Battle;

namespace DiceDungeon.Core.Progression
{
    /// <summary>학습형 패시브 효과 종류 (07-심화시스템 3). 값은 레벨당 누적.</summary>
    public enum PassiveType
    {
        None,
        AtkFlat,            // 물리 공격 +값/Lv
        MatkFlat,           // 마법 공격 +값/Lv
        DefFlat,            // 방어 +값/Lv
        CritPct,            // 크리 확률 +값/Lv (0.01 = 1%p)
        EvadePct,           // 회피 +값/Lv
        BurnOnHitPct,       // 기본 공격 시 화상 확률 +값/Lv
        CaptureShieldTriple,// 점령 칸 실드 3배 (Lv1 고정)
        ReviveBoost         // 부활 시 HP 70% (Lv1 고정)
    }

    /// <summary>
    /// 스킬 정의 (불변 데이터). 직업·스킬 추가는 이 정의만 늘리면 된다 — 코드 수정 없음.
    /// 액티브: 계수/속성/상태이상/광역/회복/실드. 패시브: PassiveType.
    /// </summary>
    public sealed class SkillDef
    {
        public string Id { get; }
        public string Name { get; }
        public int MaxLevel { get; }
        public int Cooldown { get; }
        public bool Magic { get; }
        public Element Element { get; }
        public double BaseCoef { get; }
        public double CoefPerLevel { get; }
        public bool Aoe { get; }
        public StatusType? Applies { get; }
        public int StacksPerLevel { get; }
        public double HealBase { get; }
        public double HealPerLevel { get; }
        public double ShieldRatio { get; }       // 시전 시 최대 HP 비례 실드 획득
        public PassiveType Passive { get; }
        public double PassiveValue { get; }
        public string PrereqId { get; }          // 선행 스킬 (없으면 "")
        public int PrereqLevel { get; }

        public bool IsPassive => Passive != PassiveType.None;

        public SkillDef(string id, string name, int maxLevel, int cooldown = 0,
                        bool magic = false, Element element = Element.Neutral,
                        double coef = 0, double coefPerLv = 0, bool aoe = false,
                        StatusType? applies = null, int stacksPerLv = 0,
                        double healBase = 0, double healPerLv = 0, double shieldRatio = 0,
                        PassiveType passive = PassiveType.None, double passiveValue = 0,
                        string prereqId = "", int prereqLevel = 0)
        {
            Id = id; Name = name; MaxLevel = maxLevel; Cooldown = cooldown;
            Magic = magic; Element = element;
            BaseCoef = coef; CoefPerLevel = coefPerLv; Aoe = aoe;
            Applies = applies; StacksPerLevel = stacksPerLv;
            HealBase = healBase; HealPerLevel = healPerLv; ShieldRatio = shieldRatio;
            Passive = passive; PassiveValue = passiveValue;
            PrereqId = prereqId; PrereqLevel = prereqLevel;
        }

        /// <summary>해당 레벨의 전투용 인스턴스 생성 (액티브 전용).</summary>
        public Skill Instantiate(int level)
        {
            int stacks = Math.Max(1, StacksPerLevel * level);
            return new Skill(Name, Cooldown,
                coef: BaseCoef + CoefPerLevel * (level - 1),
                aoe: Aoe, applies: Applies, applyStacks: stacks,
                healRatio: HealBase + HealPerLevel * (level - 1),
                magic: Magic, element: Element, shieldRatio: ShieldRatio);
        }
    }

    /// <summary>
    /// 런 중 학습 상태: 배운 스킬 레벨 + 남은 포인트. 선행 조건을 강제한다.
    /// </summary>
    public sealed class SkillBook
    {
        private readonly Dictionary<string, int> _learned = new Dictionary<string, int>();
        public int Points { get; set; }

        public int LevelOf(string id) => _learned.TryGetValue(id, out var lv) ? lv : 0;

        public bool CanLearn(SkillDef def)
        {
            if (Points <= 0) return false;
            if (LevelOf(def.Id) >= def.MaxLevel) return false;
            if (def.PrereqId.Length > 0 && LevelOf(def.PrereqId) < def.PrereqLevel) return false;
            return true;
        }

        public bool Learn(SkillDef def)
        {
            if (!CanLearn(def)) return false;
            Points--;
            _learned[def.Id] = LevelOf(def.Id) + 1;
            return true;
        }

        /// <summary>배운 액티브 스킬을 전투 인스턴스로 (장착 슬롯 수만큼, 계수 높은 순).</summary>
        public List<Skill> EquipActives(IEnumerable<SkillDef> pool, int slots)
        {
            return pool.Where(d => !d.IsPassive && LevelOf(d.Id) > 0)
                       .OrderByDescending(d => (d.BaseCoef + d.CoefPerLevel * (LevelOf(d.Id) - 1)) * (d.Aoe ? 1.3 : 1)
                                               + (d.HealBase + d.HealPerLevel * (LevelOf(d.Id) - 1)) * 2)
                       .Take(slots)
                       .Select(d => d.Instantiate(LevelOf(d.Id)))
                       .ToList();
        }

        /// <summary>배운 패시브의 누적 값 합계.</summary>
        public double PassiveSum(IEnumerable<SkillDef> pool, PassiveType type)
            => pool.Where(d => d.Passive == type && LevelOf(d.Id) > 0)
                   .Sum(d => d.PassiveValue * LevelOf(d.Id));
    }
}
