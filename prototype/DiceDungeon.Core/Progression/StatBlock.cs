using System;

namespace DiceDungeon.Core.Progression
{
    public enum StatId { Str, Agi, Vit, Int, Dex, Luk }

    /// <summary>
    /// 6스탯 (07-심화시스템 1, RO 오마주). 레벨업당 3포인트 배분.
    /// 파생 수치 공식은 전부 여기 상수로 — 밸런스 조정 지점 단일화.
    /// </summary>
    public sealed class StatBlock
    {
        public int Str, Agi, Vit, Int, Dex, Luk;

        public StatBlock() { }

        public StatBlock(int str, int agi, int vit, int intel, int dex, int luk)
        {
            Str = str; Agi = agi; Vit = vit; Int = intel; Dex = dex; Luk = luk;
        }

        public int Get(StatId id)
        {
            switch (id)
            {
                case StatId.Str: return Str;
                case StatId.Agi: return Agi;
                case StatId.Vit: return Vit;
                case StatId.Int: return Int;
                case StatId.Dex: return Dex;
                default: return Luk;
            }
        }

        public void Add(StatId id, int amount)
        {
            switch (id)
            {
                case StatId.Str: Str += amount; break;
                case StatId.Agi: Agi += amount; break;
                case StatId.Vit: Vit += amount; break;
                case StatId.Int: Int += amount; break;
                case StatId.Dex: Dex += amount; break;
                default: Luk += amount; break;
            }
        }

        public int Total => Str + Agi + Vit + Int + Dex + Luk;

        // 파생 수치 (스탯 1당 효과)
        public int DeriveMaxHp() => 70 + Vit * 8;
        public int DeriveAtk() => 8 + Str;
        public int DeriveMatk() => 8 + (int)(Int * 1.2);
        public int DeriveDef() => 2 + (int)(Vit * 0.3);
        public int DeriveMdef() => (int)(Int * 0.5);
        public int DeriveSpd() => 8 + (int)(Agi * 0.5);
        public double DeriveCrit() => 0.05 + Luk * 0.004;
        public double DeriveEvasion() => Agi * 0.003;
        public double DeriveAccuracy() => 0.90 + Dex * 0.008;
    }
}
