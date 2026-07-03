using System;

namespace DiceDungeon.Core.Battle
{
    public sealed class Unit
    {
        public string Name { get; }
        public int MaxHp { get; set; }
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spd { get; set; }
        public double CritChance { get; set; }
        /// <summary>마법 공격/방어 (07-심화시스템 1). 기본값: 물리와 동일/절반.</summary>
        public int Matk { get; set; }
        public int Mdef { get; set; }
        /// <summary>회피·명중 (AGI/DEX 파생 + 패시브).</summary>
        public double Evasion { get; set; }
        public double Accuracy { get; set; } = 0.9;
        /// <summary>방어 속성 (몬스터: 층 테마, 플레이어: 무속성).</summary>
        public Element Element { get; set; } = Element.Neutral;

        public bool IsAlive => Hp > 0;
        public double HpRatio => MaxHp <= 0 ? 0 : (double)Hp / MaxHp;
        public StatusSet Statuses { get; } = new StatusSet();
        public int EffectiveSpd => Math.Max(1, Spd - Statuses.SpeedPenalty);

        public Unit(string name, int hp, int atk, int def, int spd, double critChance = 0.05)
        {
            Name = name;
            MaxHp = hp;
            Hp = hp;
            Atk = atk;
            Def = def;
            Spd = spd;
            CritChance = critChance;
            Matk = atk;
            Mdef = def;
        }

        public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);

        public void Heal(int amount) => Hp = Math.Min(MaxHp, Hp + amount);

        public void HealRatio(double ratio) => Heal((int)Math.Round(MaxHp * ratio));
    }
}
