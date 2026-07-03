using System;

namespace DiceDungeon.Core
{
    /// <summary>
    /// 시드 기반 결정론적 RNG. 층 생성·드롭·이벤트가 모두 이 인스턴스에서 파생되어야
    /// 일일 던전(고정 시드)과 버그 재현이 가능하다. UnityEngine.Random 사용 금지.
    /// </summary>
    public sealed class Rng
    {
        private readonly Random _random;

        public int Seed { get; }

        public Rng(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>[min, max) 범위 정수.</summary>
        public int Next(int min, int max) => _random.Next(min, max);

        /// <summary>[0, 1) 범위 실수.</summary>
        public double NextDouble() => _random.NextDouble();

        public bool Chance(double probability) => _random.NextDouble() < probability;

        /// <summary>하위 시스템용 파생 RNG (스트림 분리).</summary>
        public Rng Derive() => new Rng(_random.Next());
    }
}
