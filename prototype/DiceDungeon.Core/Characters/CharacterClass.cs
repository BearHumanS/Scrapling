using System.Collections.Generic;
using DiceDungeon.Core.Battle;

namespace DiceDungeon.Core.Characters
{
    public enum CharacterId { Knight, Rogue, Mage, Cleric }

    /// <summary>
    /// 캐릭터 클래스 정의 (02-시스템설계 2.2).
    /// 고유 패시브는 플래그로 노출하고 RunController/BattleSimulator가 해석한다.
    /// </summary>
    public sealed class CharacterClass
    {
        public CharacterId Id { get; }
        public string Name { get; }
        public int Hp { get; }
        public int Atk { get; }
        public int Def { get; }
        public int Spd { get; }
        public double CritChance { get; }
        public IReadOnlyList<Skill> Skills { get; }

        // 고유 패시브
        public bool DoubleCaptureShield { get; }   // 기사: 점령 칸 실드 2배
        public bool FloorReroll { get; }           // 도적: 층당 1회 주사위 리롤
        public double Evasion { get; }             // 도적: 회피 확률
        public bool BestEventChoice { get; }       // 마법사: 이벤트 선택지 +1 (최선 선택)
        public double BarrierRatio { get; }        // 마법사: 전투 시작 마력 방벽 (최대 HP 비례 실드)
        public bool DoubleRestHeal { get; }        // 성직자: 쉼터 회복 2배
        public bool RevivePerRun { get; }          // 성직자: 런당 1회 자동 부활

        private CharacterClass(CharacterId id, string name, int hp, int atk, int def, int spd,
                               double crit, List<Skill> skills,
                               bool captureShield = false, bool reroll = false,
                               bool bestEvent = false, bool restHeal = false, bool revive = false,
                               double evasion = 0, double barrier = 0)
        {
            Id = id; Name = name; Hp = hp; Atk = atk; Def = def; Spd = spd;
            CritChance = crit; Skills = skills;
            DoubleCaptureShield = captureShield;
            FloorReroll = reroll;
            BestEventChoice = bestEvent;
            DoubleRestHeal = restHeal;
            RevivePerRun = revive;
            Evasion = evasion;
            BarrierRatio = barrier;
        }

        public static readonly CharacterClass Knight = new CharacterClass(
            CharacterId.Knight, "기사", hp: 120, atk: 11, def: 7, spd: 9, crit: 0.08,
            new List<Skill>
            {
                new Skill("방패치기", cooldown: 3, coef: 1.2, applies: StatusType.Stun),
                new Skill("강타", cooldown: 4, coef: 1.7),
                new Skill("성역", cooldown: 5, healRatio: 0.20),
            },
            captureShield: true);

        public static readonly CharacterClass Rogue = new CharacterClass(
            CharacterId.Rogue, "도적", hp: 105, atk: 16, def: 5, spd: 13, crit: 0.25,
            new List<Skill>
            {
                new Skill("급습", cooldown: 3, coef: 1.5),
                new Skill("독칼", cooldown: 2, coef: 0.8, applies: StatusType.Poison, applyStacks: 3),
                new Skill("연속베기", cooldown: 4, coef: 1.9),
            },
            reroll: true, evasion: 0.25);

        public static readonly CharacterClass Mage = new CharacterClass(
            CharacterId.Mage, "마법사", hp: 95, atk: 18, def: 4, spd: 10, crit: 0.10,
            new List<Skill>
            {
                new Skill("화염구", cooldown: 3, coef: 1.3, aoe: true, applies: StatusType.Burn),
                new Skill("빙결탄", cooldown: 2, coef: 1.0, applies: StatusType.Freeze),
                new Skill("마력 폭발", cooldown: 5, coef: 2.3),
            },
            bestEvent: true, barrier: 0.25);

        public static readonly CharacterClass Cleric = new CharacterClass(
            CharacterId.Cleric, "성직자", hp: 110, atk: 10, def: 6, spd: 8, crit: 0.08,
            new List<Skill>
            {
                new Skill("신성타격", cooldown: 2, coef: 1.3),
                new Skill("축복", cooldown: 4, healRatio: 0.25),
                new Skill("심판", cooldown: 5, coef: 1.9),
            },
            restHeal: true, revive: true);

        public static CharacterClass Get(CharacterId id)
        {
            switch (id)
            {
                case CharacterId.Rogue: return Rogue;
                case CharacterId.Mage: return Mage;
                case CharacterId.Cleric: return Cleric;
                default: return Knight;
            }
        }

        public static readonly CharacterClass[] All = { Knight, Rogue, Mage, Cleric };
    }
}
