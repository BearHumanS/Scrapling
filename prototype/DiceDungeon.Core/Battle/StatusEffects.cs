using System;
using System.Collections.Generic;

namespace DiceDungeon.Core.Battle
{
    public enum StatusType
    {
        Burn,   // 턴 시작마다 최대 HP 3% 피해, 2턴
        Poison, // 중첩형: 턴 시작마다 중첩 × 2 고정 피해, 3턴
        Freeze, // 스피드 -3, 2턴 (본편: 주사위 눈 -1 기믹과 연동 예정)
        Stun    // 다음 행동 1회 상실
    }

    /// <summary>유닛 하나에 걸린 상태이상 집합. 턴 시작 시 Tick으로 처리.</summary>
    public sealed class StatusSet
    {
        private sealed class Entry
        {
            public int Duration;
            public int Stacks;
        }

        private readonly Dictionary<StatusType, Entry> _entries = new Dictionary<StatusType, Entry>();

        public bool Has(StatusType type) => _entries.ContainsKey(type);

        public void Apply(StatusType type, int stacks = 1)
        {
            if (!_entries.TryGetValue(type, out var e))
            {
                e = new Entry();
                _entries[type] = e;
            }
            e.Duration = DefaultDuration(type);
            e.Stacks += stacks;
        }

        /// <summary>턴 시작 처리. 반환: (도트 피해, 행동 불가 여부).</summary>
        public (int damage, bool skipTurn) Tick(Unit owner)
        {
            int damage = 0;
            bool skip = false;

            // 도트는 고정 피해 (v4: 최대 HP 비례는 깊은 층에서 무한 스케일링되어 폐기)
            if (_entries.TryGetValue(StatusType.Burn, out var burn))
                damage += 4 * burn.Stacks;
            if (_entries.TryGetValue(StatusType.Poison, out var poison))
                damage += 3 * poison.Stacks;
            if (_entries.ContainsKey(StatusType.Stun))
            {
                skip = true;
                _entries.Remove(StatusType.Stun); // 기절은 1회 소모
            }

            // 지속시간 감소
            var expired = new List<StatusType>();
            foreach (var kv in _entries)
            {
                kv.Value.Duration--;
                if (kv.Value.Duration <= 0) expired.Add(kv.Key);
            }
            foreach (var t in expired) _entries.Remove(t);

            return (damage, skip);
        }

        public int SpeedPenalty => Has(StatusType.Freeze) ? 3 : 0;

        private static int DefaultDuration(StatusType type)
        {
            switch (type)
            {
                case StatusType.Burn: return 2;
                case StatusType.Poison: return 3;
                case StatusType.Freeze: return 2;
                case StatusType.Stun: return 1;
                default: return 1;
            }
        }
    }
}
