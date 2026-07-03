namespace DiceDungeon.Core.Battle
{
    /// <summary>
    /// 몬스터 개성 (깊이 v5): "누구를 먼저 잡을까"라는 결정을 만든다.
    /// 3층부터 일반 몬스터에 35% 확률로 1개 부여. 보스는 10층+에서 광폭화 고정.
    /// </summary>
    public enum MonsterTrait
    {
        None,
        Healer,    // 동료 HP 60% 미만이면 공격 대신 12% 회복 → 먼저 잡아야 함
        Enrage,    // HP 50% 미만이면 공격력 ×1.4 → 빨리 끝내거나 기절로 봉쇄
        Shielded,  // 처음 받는 2회 타격 피해 절반 → 광역·도트가 유리
        Venomous,  // 공격 시 플레이어에게 중독 1중첩 → 장기전 금지
        Explosive  // 죽을 때 자기 공격력 ×1.5 피해 → 마지막에 잡거나 원거리 처리(본편)
    }

    public static class TraitRules
    {
        public const int MinFloor = 3;
        public const double Chance = 0.35;

        public static MonsterTrait Roll(int floor, Rng rng)
        {
            if (floor < MinFloor || !rng.Chance(Chance)) return MonsterTrait.None;
            return (MonsterTrait)rng.Next(1, 6);
        }

        public static string Name(MonsterTrait trait)
        {
            switch (trait)
            {
                case MonsterTrait.Healer: return "치유사";
                case MonsterTrait.Enrage: return "광폭";
                case MonsterTrait.Shielded: return "장갑";
                case MonsterTrait.Venomous: return "맹독";
                case MonsterTrait.Explosive: return "폭발";
                default: return "";
            }
        }
    }
}
