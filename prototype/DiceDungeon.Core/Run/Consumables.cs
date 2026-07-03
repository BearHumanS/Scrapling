using DiceDungeon.Core.Board;

namespace DiceDungeon.Core.Run
{
    /// <summary>
    /// 소모품 (01-게임기획서 3.2) — "운을 조작하는" 코어 차별 요소.
    /// 홀짝 부적이 보드 이동을 구경거리에서 의사결정으로 바꾼다.
    /// </summary>
    public enum ConsumableType
    {
        ParityCharm, // 홀/짝 부적: 이번 굴림의 합을 원하는 홀짝으로 강제
        Bomb,        // 폭탄: 전투 시작 시 적 전체에 고정 피해
        Smoke        // 연막탄: 전투 하나를 회피 (보상 없음)
    }

    public sealed class ConsumableBag
    {
        public int Charms;
        public int Bombs;
        public int Smokes;

        public int Count(ConsumableType type)
        {
            switch (type)
            {
                case ConsumableType.ParityCharm: return Charms;
                case ConsumableType.Bomb: return Bombs;
                default: return Smokes;
            }
        }

        public void Add(ConsumableType type, int amount = 1)
        {
            switch (type)
            {
                case ConsumableType.ParityCharm: Charms += amount; break;
                case ConsumableType.Bomb: Bombs += amount; break;
                default: Smokes += amount; break;
            }
        }

        public bool TryUse(ConsumableType type)
        {
            if (Count(type) <= 0) return false;
            Add(type, -1);
            return true;
        }

        /// <summary>폭탄 피해: 층 비례 고정 (02-시스템설계 3.2).</summary>
        public static int BombDamage(int floor) => 25 + floor * 8;

        public static int Price(ConsumableType type, int floor)
        {
            switch (type)
            {
                case ConsumableType.ParityCharm: return 25 + floor * 8;
                case ConsumableType.Bomb: return 30 + floor * 10;
                default: return 20 + floor * 6;
            }
        }
    }
}
