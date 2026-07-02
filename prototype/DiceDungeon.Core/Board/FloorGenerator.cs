using System.Collections.Generic;
using DiceDungeon.Core.Data;

namespace DiceDungeon.Core.Board
{
    /// <summary>
    /// 층 번호 + RNG → 24칸 보드 생성.
    /// 규칙: 0번은 항상 출발, 상점 1개·쉼터 1개 보장, 나머지는 비율 기반 랜덤 (01-게임기획서 4.1).
    /// </summary>
    public static class FloorGenerator
    {
        public static IReadOnlyList<Tile> Generate(int floor, Rng rng)
        {
            int size = Balance.BoardSize;
            var types = new TileType[size];
            types[0] = TileType.Start;

            // 보장 배치: 상점, 쉼터를 서로 다른 비출발 칸에
            int shopIndex = rng.Next(1, size);
            int restIndex;
            do { restIndex = rng.Next(1, size); } while (restIndex == shopIndex);
            types[shopIndex] = TileType.Shop;
            types[restIndex] = TileType.Rest;

            for (int i = 1; i < size; i++)
            {
                if (i == shopIndex || i == restIndex) continue;
                types[i] = RollTileType(rng);
            }

            var tiles = new List<Tile>(size);
            for (int i = 0; i < size; i++)
                tiles.Add(new Tile(i, types[i]));
            return tiles;
        }

        private static TileType RollTileType(Rng rng)
        {
            double v = rng.NextDouble();
            double acc = Balance.TileBattleRatio;
            if (v < acc) return TileType.Battle;
            acc += Balance.TileEliteRatio;
            if (v < acc) return TileType.Elite;
            acc += Balance.TileTreasureRatio;
            if (v < acc) return TileType.Treasure;
            acc += Balance.TileEventRatio;
            if (v < acc) return TileType.Event;
            acc += Balance.TileTrapRatio;
            if (v < acc) return TileType.Trap;
            return TileType.Battle; // 잔여 확률은 전투로
        }
    }
}
