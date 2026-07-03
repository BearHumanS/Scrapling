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
        }

        public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);

        public void Heal(int amount) => Hp = Math.Min(MaxHp, Hp + amount);

        public void HealRatio(double ratio) => Heal((int)Math.Round(MaxHp * ratio));
    }
}
