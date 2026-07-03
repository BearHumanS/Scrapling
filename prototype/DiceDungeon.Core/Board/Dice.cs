namespace DiceDungeon.Core.Board
{
    public readonly struct DiceResult
    {
        public int Die1 { get; }
        public int Die2 { get; }
        public int Sum => Die1 + Die2;
        public bool IsDouble => Die1 == Die2;
        /// <summary>합 7 = 럭키세븐: 직후 전투 크리티컬 확률 보너스 (02-시스템설계 1.3).</summary>
        public bool IsLuckySeven => Sum == 7;

        public DiceResult(int die1, int die2)
        {
            Die1 = die1;
            Die2 = die2;
        }
    }

    /// <summary>
    /// 6면체 2개. 결과는 여기서 선판정하고 Presentation은 연출만 재생한다.
    /// 주사위 조작 아이템(홀짝 부적, 고정 주사위)은 이 레이어에서 결과를 변형한다.
    /// </summary>
    public sealed class DiceRoller
    {
        private readonly Rng _rng;

        public DiceRoller(Rng rng)
        {
            _rng = rng;
        }

        public DiceResult Roll() => new DiceResult(_rng.Next(1, 7), _rng.Next(1, 7));

        /// <summary>홀수/짝수 부적: 조건을 만족할 때까지 리롤한 결과를 반환.</summary>
        public DiceResult RollWithParity(bool even)
        {
            while (true)
            {
                var r = Roll();
                if (r.Sum % 2 == (even ? 0 : 1)) return r;
            }
        }
    }
}
