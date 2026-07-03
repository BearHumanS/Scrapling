using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Characters;

namespace DiceDungeon.Core.Progression
{
    public enum JobPathId
    {
        None,          // 아직 1차
        RuneKnight, Guardian,       // 기사
        Assassin, ShadowDancer,     // 도적
        Archmage, FrostWeaver,      // 마법사
        Inquisitor, HighPriest      // 성직자
    }

    /// <summary>2차 직업 정의: 전직 보너스 스탯 + 전용 스킬트리 (07-심화시스템 2).</summary>
    public sealed class JobPath
    {
        public JobPathId Id { get; }
        public string Name { get; }
        public CharacterId Parent { get; }
        public StatBlock BonusStats { get; }             // 전직 시 +10 (편향 분포)
        public IReadOnlyList<SkillDef> Tree { get; }
        public IReadOnlyList<StatId> AllocationBias { get; } // 시뮬레이션 자동 배분 순환

        public JobPath(JobPathId id, string name, CharacterId parent, StatBlock bonus,
                       List<SkillDef> tree, StatId[] bias)
        {
            Id = id; Name = name; Parent = parent; BonusStats = bonus; Tree = tree;
            AllocationBias = bias;
        }
    }

    /// <summary>
    /// 직업·스킬트리 카탈로그 (전부 데이터 — 콘텐츠 추가 시 여기만 늘린다).
    /// 1차 4종 트리(액티브 3 + 패시브 2) + 2차 8종 트리(3종씩, 선행 조건 포함).
    /// </summary>
    public static class JobCatalog
    {
        public const int JobChangeLevel = 10;
        public const int SkillSlots = 3;

        // ---------- 1차 직업 초기 스탯 (합 30) 및 배분 편향 ----------
        public static StatBlock BaseStats(CharacterId id)
        {
            switch (id)
            {
                case CharacterId.Rogue: return new StatBlock(6, 9, 4, 2, 4, 5);
                case CharacterId.Mage: return new StatBlock(2, 4, 4, 11, 5, 4);
                case CharacterId.Cleric: return new StatBlock(4, 3, 7, 8, 4, 4);
                default: return new StatBlock(9, 3, 9, 2, 4, 3); // 기사
            }
        }

        public static StatId[] BaseBias(CharacterId id)
        {
            switch (id)
            {
                case CharacterId.Rogue: return new[] { StatId.Agi, StatId.Str, StatId.Luk };
                case CharacterId.Mage: return new[] { StatId.Int, StatId.Vit, StatId.Dex };
                case CharacterId.Cleric: return new[] { StatId.Int, StatId.Vit, StatId.Str };
                default: return new[] { StatId.Str, StatId.Vit, StatId.Str };
            }
        }

        // ---------- 1차 스킬트리 ----------
        private static readonly List<SkillDef> KnightTree = new List<SkillDef>
        {
            new SkillDef("kn_bash", "방패치기", 5, cooldown: 3, coef: 1.0, coefPerLv: 0.12, applies: StatusType.Stun, stacksPerLv: 1),
            new SkillDef("kn_blow", "강타", 5, cooldown: 4, coef: 1.4, coefPerLv: 0.18),
            new SkillDef("kn_sanct", "성역", 5, cooldown: 5, healBase: 0.12, healPerLv: 0.03),
            new SkillDef("kn_wall", "철벽", 3, passive: PassiveType.DefFlat, passiveValue: 2),
            new SkillDef("kn_charge", "돌진", 3, cooldown: 3, coef: 1.1, coefPerLv: 0.10),
        };

        private static readonly List<SkillDef> RogueTree = new List<SkillDef>
        {
            new SkillDef("rg_ambush", "급습", 5, cooldown: 3, coef: 1.3, coefPerLv: 0.15),
            new SkillDef("rg_poison", "독칼", 5, cooldown: 2, coef: 0.7, coefPerLv: 0.08, applies: StatusType.Poison, stacksPerLv: 2),
            new SkillDef("rg_flurry", "연속베기", 5, cooldown: 4, coef: 1.6, coefPerLv: 0.20),
            new SkillDef("rg_sharp", "칼갈기", 3, passive: PassiveType.CritPct, passiveValue: 0.03),
            new SkillDef("rg_step", "경공", 3, passive: PassiveType.EvadePct, passiveValue: 0.03),
        };

        private static readonly List<SkillDef> MageTree = new List<SkillDef>
        {
            new SkillDef("mg_fire", "화염구", 5, cooldown: 3, magic: true, element: Element.Fire, coef: 1.1, coefPerLv: 0.14, aoe: true, applies: StatusType.Burn, stacksPerLv: 1),
            new SkillDef("mg_ice", "빙결탄", 5, cooldown: 2, magic: true, element: Element.Ice, coef: 0.9, coefPerLv: 0.11, applies: StatusType.Freeze, stacksPerLv: 1),
            new SkillDef("mg_blast", "마력 폭발", 5, cooldown: 5, magic: true, coef: 2.0, coefPerLv: 0.25),
            new SkillDef("mg_medit", "명상", 3, passive: PassiveType.MatkFlat, passiveValue: 3),
            new SkillDef("mg_focus", "집중", 3, passive: PassiveType.CritPct, passiveValue: 0.02),
        };

        private static readonly List<SkillDef> ClericTree = new List<SkillDef>
        {
            new SkillDef("cl_smite", "신성타격", 5, cooldown: 2, magic: true, element: Element.Holy, coef: 1.1, coefPerLv: 0.12),
            new SkillDef("cl_bless", "축복", 5, cooldown: 4, healBase: 0.18, healPerLv: 0.04),
            new SkillDef("cl_judge", "심판", 5, cooldown: 5, magic: true, element: Element.Holy, coef: 1.6, coefPerLv: 0.20),
            new SkillDef("cl_faith", "신앙", 3, passive: PassiveType.MatkFlat, passiveValue: 2),
            new SkillDef("cl_guard", "가호", 3, passive: PassiveType.DefFlat, passiveValue: 1.5),
        };

        // ---------- 2차 직업 (전직 보너스 +10, 트리에 선행 조건) ----------
        public static readonly JobPath RuneKnight = new JobPath(
            JobPathId.RuneKnight, "룬 나이트", CharacterId.Knight,
            new StatBlock(6, 0, 2, 0, 0, 2),
            new List<SkillDef>
            {
                new SkillDef("rk_flame", "플레임 슬래시", 5, cooldown: 3, element: Element.Fire, coef: 1.6, coefPerLv: 0.22, prereqId: "kn_blow", prereqLevel: 3),
                new SkillDef("rk_ignite", "발화", 3, passive: PassiveType.BurnOnHitPct, passiveValue: 0.10, prereqId: "rk_flame", prereqLevel: 1),
                new SkillDef("rk_rage", "전장의 함성", 3, passive: PassiveType.AtkFlat, passiveValue: 3),
            },
            new[] { StatId.Str, StatId.Str, StatId.Vit, StatId.Luk });

        public static readonly JobPath Guardian = new JobPath(
            JobPathId.Guardian, "가디언", CharacterId.Knight,
            new StatBlock(2, 0, 6, 0, 0, 2),
            new List<SkillDef>
            {
                new SkillDef("gd_oath", "수호의 맹세", 5, cooldown: 4, element: Element.Holy, shieldRatio: 0.12, prereqId: "kn_wall", prereqLevel: 2),
                new SkillDef("gd_fort", "요새화", 1, passive: PassiveType.CaptureShieldTriple, passiveValue: 1, prereqId: "gd_oath", prereqLevel: 1),
                new SkillDef("gd_smite", "응징의 방패", 5, cooldown: 3, coef: 1.3, coefPerLv: 0.15, applies: StatusType.Stun, stacksPerLv: 1),
            },
            new[] { StatId.Vit, StatId.Vit, StatId.Str, StatId.Dex });

        public static readonly JobPath Assassin = new JobPath(
            JobPathId.Assassin, "어쌔신", CharacterId.Rogue,
            new StatBlock(4, 3, 0, 0, 0, 3),
            new List<SkillDef>
            {
                new SkillDef("as_venom", "맹독 인장", 5, cooldown: 3, coef: 1.0, coefPerLv: 0.10, applies: StatusType.Poison, stacksPerLv: 3, prereqId: "rg_poison", prereqLevel: 3),
                new SkillDef("as_back", "백스탭", 5, cooldown: 4, coef: 2.0, coefPerLv: 0.25, prereqId: "rg_ambush", prereqLevel: 2),
                new SkillDef("as_lethal", "치명", 3, passive: PassiveType.CritPct, passiveValue: 0.04),
            },
            new[] { StatId.Luk, StatId.Str, StatId.Agi, StatId.Agi });

        public static readonly JobPath ShadowDancer = new JobPath(
            JobPathId.ShadowDancer, "섀도 댄서", CharacterId.Rogue,
            new StatBlock(0, 6, 2, 0, 0, 2),
            new List<SkillDef>
            {
                new SkillDef("sd_bolt", "그림자 칼날", 5, cooldown: 3, element: Element.Shadow, coef: 1.4, coefPerLv: 0.18, prereqId: "rg_flurry", prereqLevel: 2),
                new SkillDef("sd_veil", "장막", 3, passive: PassiveType.EvadePct, passiveValue: 0.05),
                new SkillDef("sd_dance", "환영무", 5, cooldown: 5, coef: 1.2, coefPerLv: 0.15, aoe: true),
            },
            new[] { StatId.Agi, StatId.Agi, StatId.Luk, StatId.Str });

        public static readonly JobPath Archmage = new JobPath(
            JobPathId.Archmage, "아크메이지", CharacterId.Mage,
            new StatBlock(0, 0, 2, 6, 2, 0),
            new List<SkillDef>
            {
                new SkillDef("am_meteor", "메테오", 5, cooldown: 5, magic: true, element: Element.Fire, coef: 2.2, coefPerLv: 0.30, aoe: true, applies: StatusType.Burn, stacksPerLv: 2, prereqId: "mg_fire", prereqLevel: 3),
                new SkillDef("am_surge", "마력 격류", 3, passive: PassiveType.MatkFlat, passiveValue: 4),
                new SkillDef("am_quake", "대지 붕괴", 5, cooldown: 4, magic: true, element: Element.Earth, coef: 1.7, coefPerLv: 0.20),
            },
            new[] { StatId.Int, StatId.Int, StatId.Dex, StatId.Vit });

        public static readonly JobPath FrostWeaver = new JobPath(
            JobPathId.FrostWeaver, "서리술사", CharacterId.Mage,
            new StatBlock(0, 2, 4, 4, 0, 0),
            new List<SkillDef>
            {
                new SkillDef("fw_nova", "프로스트 노바", 5, cooldown: 4, magic: true, element: Element.Ice, coef: 1.5, coefPerLv: 0.18, aoe: true, applies: StatusType.Freeze, stacksPerLv: 1, prereqId: "mg_ice", prereqLevel: 3),
                new SkillDef("fw_armor", "얼음 갑주", 5, cooldown: 5, shieldRatio: 0.15),
                new SkillDef("fw_deep", "심빙", 3, passive: PassiveType.MatkFlat, passiveValue: 3),
            },
            new[] { StatId.Int, StatId.Vit, StatId.Int, StatId.Agi });

        public static readonly JobPath Inquisitor = new JobPath(
            JobPathId.Inquisitor, "인퀴지터", CharacterId.Cleric,
            new StatBlock(3, 0, 0, 4, 0, 3),
            new List<SkillDef>
            {
                new SkillDef("iq_wrath", "천벌", 5, cooldown: 4, magic: true, element: Element.Holy, coef: 2.0, coefPerLv: 0.25, prereqId: "cl_judge", prereqLevel: 3),
                new SkillDef("iq_zeal", "광신", 3, passive: PassiveType.CritPct, passiveValue: 0.03),
                new SkillDef("iq_purge", "정화의 불꽃", 5, cooldown: 3, magic: true, element: Element.Fire, coef: 1.3, coefPerLv: 0.15, aoe: true),
            },
            new[] { StatId.Int, StatId.Luk, StatId.Int, StatId.Str });

        public static readonly JobPath HighPriest = new JobPath(
            JobPathId.HighPriest, "하이프리스트", CharacterId.Cleric,
            new StatBlock(0, 0, 4, 4, 0, 2),
            new List<SkillDef>
            {
                new SkillDef("hp_heal", "대치유", 5, cooldown: 4, healBase: 0.25, healPerLv: 0.05, prereqId: "cl_bless", prereqLevel: 3),
                new SkillDef("hp_resur", "소생의 빛", 1, passive: PassiveType.ReviveBoost, passiveValue: 1),
                new SkillDef("hp_ray", "성광", 5, cooldown: 3, magic: true, element: Element.Holy, coef: 1.4, coefPerLv: 0.16),
            },
            new[] { StatId.Int, StatId.Vit, StatId.Int, StatId.Vit });

        public static readonly JobPath[] AllPaths =
            { RuneKnight, Guardian, Assassin, ShadowDancer, Archmage, FrostWeaver, Inquisitor, HighPriest };

        public static IReadOnlyList<SkillDef> BaseTree(CharacterId id)
        {
            switch (id)
            {
                case CharacterId.Rogue: return RogueTree;
                case CharacterId.Mage: return MageTree;
                case CharacterId.Cleric: return ClericTree;
                default: return KnightTree;
            }
        }

        public static (JobPath a, JobPath b) PathsFor(CharacterId id)
        {
            var paths = AllPaths.Where(p => p.Parent == id).ToArray();
            return (paths[0], paths[1]);
        }

        /// <summary>현재 직업 상태의 전체 스킬 풀 (1차 트리 + 전직했다면 2차 트리).</summary>
        public static IEnumerable<SkillDef> SkillPool(CharacterId baseJob, JobPath advanced)
        {
            foreach (var s in BaseTree(baseJob)) yield return s;
            if (advanced != null)
                foreach (var s in advanced.Tree) yield return s;
        }
    }
}
