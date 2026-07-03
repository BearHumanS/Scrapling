namespace DiceDungeon.Core.Battle
{
    /// <summary>6속성 (07-심화시스템 4). 무기 기본 공격은 무속성, 스킬이 속성을 가진다.</summary>
    public enum Element { Neutral, Fire, Ice, Earth, Holy, Shadow }

    public static class ElementTable
    {
        // [공격속성, 방어속성] 배율.
        // 삼각: 화염>대지>냉기>화염 (1.5 / 역방향 0.75), 동속성 0.5, 신성↔암흑 상호 2.0.
        private static readonly double[,] Matrix =
        {
            //          무     화염   냉기   대지   신성   암흑
            /*무*/   { 1.0,  1.0,  1.0,  1.0,  1.0,  1.0 },
            /*화염*/ { 1.0,  0.5,  0.75, 1.5,  1.0,  1.0 },
            /*냉기*/ { 1.0,  1.5,  0.5,  0.75, 1.0,  1.0 },
            /*대지*/ { 1.0,  0.75, 1.5,  0.5,  1.0,  1.0 },
            /*신성*/ { 1.0,  1.0,  1.0,  1.0,  0.5,  2.0 },
            /*암흑*/ { 1.0,  1.0,  1.0,  1.0,  2.0,  0.5 },
        };

        public static double Multiplier(Element attack, Element defense)
            => Matrix[(int)attack, (int)defense];

        /// <summary>몬스터 방어 속성 = 층 테마 (01-게임기획서 5와 일치).</summary>
        public static Element FloorElement(int floor)
        {
            if (floor <= 3) return Element.Shadow;  // 지하묘지 (언데드)
            if (floor <= 6) return Element.Earth;   // 곰팡이 동굴
            if (floor <= 9) return Element.Fire;    // 용암 대장간
            if (floor <= 12) return Element.Ice;    // 얼어붙은 성채
            return Element.Shadow;                  // 심연
        }
    }
}
