namespace DiceDungeon.Core.Board
{
    public enum TileType
    {
        Start,     // 출발(게이트): 통과 시 한 바퀴 카운트 + 보너스 골드
        Battle,    // 일반 전투: 승리 시 칸 점령
        Elite,     // 정예 전투: 2배 보상 + 장비 확정
        Treasure,  // 보물: 골드/장비, 낮은 확률로 미믹
        Shop,      // 상점
        Rest,      // 쉼터: 회복
        Trap,      // 함정: 즉발 피해 (민첩 판정 회피)
        Event      // 랜덤 이벤트 (?)
    }

    public sealed class Tile
    {
        public int Index { get; }
        public TileType Type { get; }

        /// <summary>전투 칸을 승리해 점령했는가. 점령 칸 재방문 시 회복 제공 (브루마블의 "내 땅").</summary>
        public bool Captured { get; set; }

        public Tile(int index, TileType type)
        {
            Index = index;
            Type = type;
        }
    }
}
