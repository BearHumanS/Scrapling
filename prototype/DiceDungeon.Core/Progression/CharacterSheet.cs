using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Characters;
using DiceDungeon.Core.Data;

namespace DiceDungeon.Core.Progression
{
    /// <summary>
    /// 런 중 캐릭터의 성장 상태: 레벨·경험치·스탯 배분·스킬북·전직·장비 누계.
    /// Unit(전투 수치)은 이 시트에서 파생된다 — Refresh()가 유일한 재계산 지점.
    /// UI 게임에서는 배분/학습/전직이 플레이어 입력, 시뮬레이션에서는 Auto* 메서드.
    /// </summary>
    public sealed class CharacterSheet
    {
        public CharacterId Job { get; }
        public JobPath Advanced { get; private set; }
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public StatBlock Stats { get; }
        public SkillBook Book { get; } = new SkillBook();
        public int UnspentStatPoints { get; private set; }

        // 장비 누계 (런 내 파밍)
        public int GearAtk, GearDef, GearHp;

        private int _biasCursor;

        public CharacterSheet(CharacterId job)
        {
            Job = job;
            Stats = JobCatalog.BaseStats(job);
            Book.Points = 1; // 시작 스킬 1개
        }

        public static int XpToLevel(int level) => (int)(14 * Math.Pow(level, 1.25));

        /// <summary>경험치 획득 → 오른 레벨 수 반환 (레벨당 스탯 +3, 스킬 +1).</summary>
        public int GainXp(int amount)
        {
            Xp += amount;
            int gained = 0;
            while (Xp >= XpToLevel(Level))
            {
                Xp -= XpToLevel(Level);
                Level++;
                gained++;
                UnspentStatPoints += 3;
                Book.Points++;
            }
            return gained;
        }

        public bool CanJobChange => Level >= JobCatalog.JobChangeLevel && Advanced == null;

        public void JobChange(JobPath path)
        {
            if (!CanJobChange || path.Parent != Job) return;
            Advanced = path;
            Stats.Str += path.BonusStats.Str;
            Stats.Agi += path.BonusStats.Agi;
            Stats.Vit += path.BonusStats.Vit;
            Stats.Int += path.BonusStats.Int;
            Stats.Dex += path.BonusStats.Dex;
            Stats.Luk += path.BonusStats.Luk;
        }

        public IEnumerable<SkillDef> SkillPool => JobCatalog.SkillPool(Job, Advanced);

        /// <summary>수동 스탯 배분 (UI용). 성공 시 true.</summary>
        public bool SpendStatPoint(StatId id)
        {
            if (UnspentStatPoints <= 0) return false;
            Stats.Add(id, 1);
            UnspentStatPoints--;
            return true;
        }

        // ---------- 시뮬레이션용 자동 정책 (07-심화시스템 6) ----------

        /// <summary>스탯 자동 배분: 직업 편향 리스트 순환.</summary>
        public void AutoAllocateStats()
        {
            var bias = Advanced?.AllocationBias ?? JobCatalog.BaseBias(Job);
            while (UnspentStatPoints > 0)
            {
                Stats.Add(bias[_biasCursor % bias.Count], 1);
                _biasCursor++;
                UnspentStatPoints--;
            }
        }

        /// <summary>스킬 자동 학습: 2차 트리 우선, 선행 충족되는 것부터.</summary>
        public void AutoLearnSkills()
        {
            bool learned = true;
            while (learned && Book.Points > 0)
            {
                learned = false;
                var ordered = (Advanced != null ? Advanced.Tree.Concat(JobCatalog.BaseTree(Job))
                                                : JobCatalog.BaseTree(Job).AsEnumerable());
                foreach (var def in ordered)
                {
                    if (Book.CanLearn(def))
                    {
                        Book.Learn(def);
                        learned = true;
                        break;
                    }
                }
            }
        }

        // ---------- Unit 파생 ----------

        /// <summary>
        /// 시트 → Unit 재계산. maxHp 증가분만큼 현재 HP도 증가 (레벨업 회복 체감).
        /// bonus*: 훈련소·유물 등 런 외부 보정.
        /// </summary>
        public void Refresh(Unit unit, int bonusHp, int bonusAtk, int bonusDef, double bonusCrit)
        {
            var cls = CharacterClass.Get(Job);
            var pool = SkillPool.ToList();

            int newMaxHp = Stats.DeriveMaxHp() + GearHp + bonusHp;
            int hpGain = newMaxHp - unit.MaxHp;
            unit.MaxHp = newMaxHp;
            if (hpGain > 0) unit.Heal(hpGain);
            else unit.Hp = Math.Min(unit.Hp, unit.MaxHp);

            unit.Atk = Stats.DeriveAtk() + GearAtk + bonusAtk
                       + (int)Book.PassiveSum(pool, PassiveType.AtkFlat);
            unit.Matk = Stats.DeriveMatk() + GearAtk + bonusAtk
                        + (int)Book.PassiveSum(pool, PassiveType.MatkFlat);
            unit.Def = Stats.DeriveDef() + GearDef + bonusDef
                       + (int)Book.PassiveSum(pool, PassiveType.DefFlat);
            unit.Mdef = Stats.DeriveMdef() + GearDef / 2 + bonusDef / 2;
            unit.Spd = Stats.DeriveSpd();
            unit.CritChance = Stats.DeriveCrit() + bonusCrit
                              + Book.PassiveSum(pool, PassiveType.CritPct);
            unit.Evasion = Stats.DeriveEvasion() + cls.Evasion
                           + Book.PassiveSum(pool, PassiveType.EvadePct);
            unit.Accuracy = Stats.DeriveAccuracy();
        }

        public Unit BuildUnit(int bonusHp, int bonusAtk, int bonusDef, double bonusCrit)
        {
            var name = Advanced?.Name ?? CharacterClass.Get(Job).Name;
            var unit = new Unit(name, 1, 1, 1, 1);
            unit.MaxHp = 0; // Refresh가 전량 회복 처리하도록
            unit.Hp = 0;
            Refresh(unit, bonusHp, bonusAtk, bonusDef, bonusCrit);
            unit.Hp = unit.MaxHp;
            return unit;
        }

        public List<Skill> EquipSkills() => Book.EquipActives(SkillPool, JobCatalog.SkillSlots);

        public double BurnOnHitChance => Book.PassiveSum(SkillPool, PassiveType.BurnOnHitPct);
        public bool HasCaptureShieldTriple => Book.PassiveSum(SkillPool, PassiveType.CaptureShieldTriple) > 0;
        public bool HasReviveBoost => Book.PassiveSum(SkillPool, PassiveType.ReviveBoost) > 0;
    }
}
