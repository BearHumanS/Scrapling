using System;
using DiceDungeon.Core.Progression;

namespace DiceDungeon.Core.Run
{
    /// <summary>이벤트 선택의 결과 묶음 (데이터 주도 — 01-게임기획서 "리스크/리워드 선택지").</summary>
    public sealed class EventOutcome
    {
        public int Gold;
        public double HpRatio;      // +회복 / -피해 (최대 HP 비례)
        public bool Gear;
        public ConsumableType? Item;
        public int Xp;

        public static readonly EventOutcome Nothing = new EventOutcome();
    }

    /// <summary>선택지: 스탯 판정이 있으면 성공/실패 분기 (재주·운의 전투 외 용도).</summary>
    public sealed class EventChoice
    {
        public string Desc = "";
        public StatId? Check;
        public int Dc;              // 난이도 (스탯 값과 비교)
        public EventOutcome Success = EventOutcome.Nothing;
        public EventOutcome Fail = EventOutcome.Nothing;

        /// <summary>판정 성공 확률: 50% ± 스탯-난이도 차이당 4.5%p (25~95% 클램프).</summary>
        public double SuccessChance(StatBlock stats)
        {
            if (Check == null) return 1.0;
            double c = 0.5 + 0.045 * (stats.Get(Check.Value) - Dc);
            return Math.Max(0.25, Math.Min(0.95, c));
        }
    }

    public sealed class EventDef
    {
        public string Name = "";
        public EventChoice[] Choices = Array.Empty<EventChoice>();
    }

    /// <summary>이벤트 카탈로그 8종 (본편 30종의 대표 유형 — 콘텐츠 추가는 여기만).</summary>
    public static class EventCatalog
    {
        public static EventDef Roll(int floor, Rng rng)
        {
            var all = Build(floor);
            return all[rng.Next(0, all.Length)];
        }

        public static EventDef[] Build(int floor)
        {
            int g = 20 + floor * 10; // 층 비례 골드 기준값
            return new[]
            {
                new EventDef
                {
                    Name = "무너진 제단",
                    Choices = new[]
                    {
                        new EventChoice { Desc = "기도한다 (운)", Check = StatId.Luk, Dc = 8,
                            Success = new EventOutcome { HpRatio = 0.30, Gold = g / 2 },
                            Fail = new EventOutcome { HpRatio = -0.15 } },
                        new EventChoice { Desc = "지나친다" },
                    }
                },
                new EventDef
                {
                    Name = "잠긴 보물상자",
                    Choices = new[]
                    {
                        new EventChoice { Desc = "자물쇠를 딴다 (재주)", Check = StatId.Dex, Dc = 9,
                            Success = new EventOutcome { Gear = true },
                            Fail = new EventOutcome { HpRatio = -0.12 } },
                        new EventChoice { Desc = "부순다 (힘)", Check = StatId.Str, Dc = 10,
                            Success = new EventOutcome { Gold = g },
                            Fail = new EventOutcome { HpRatio = -0.06 } },
                        new EventChoice { Desc = "내버려둔다" },
                    }
                },
                new EventDef
                {
                    Name = "떠도는 상인",
                    Choices = new[]
                    {
                        new EventChoice { Desc = $"부적을 산다 (-{g}G)",
                            Success = new EventOutcome { Gold = -g, Item = ConsumableType.ParityCharm } },
                        new EventChoice { Desc = "지나친다" },
                    }
                },
                new EventDef
                {
                    Name = "수상한 고기",
                    Choices = new[]
                    {
                        new EventChoice { Desc = "먹는다 (체력)", Check = StatId.Vit, Dc = 9,
                            Success = new EventOutcome { HpRatio = 0.40 },
                            Fail = new EventOutcome { HpRatio = -0.10 } },
                        new EventChoice { Desc = "버린다" },
                    }
                },
                new EventDef
                {
                    Name = "고대 서고",
                    Choices = new[]
                    {
                        new EventChoice { Desc = "해독한다 (지능)", Check = StatId.Int, Dc = 9,
                            Success = new EventOutcome { Xp = 15 + floor * 5 },
                            Fail = EventOutcome.Nothing },
                        new EventChoice { Desc = "태워서 몸을 녹인다",
                            Success = new EventOutcome { HpRatio = 0.10 } },
                    }
                },
                new EventDef
                {
                    Name = "해골 도박사",
                    Choices = new[]
                    {
                        new EventChoice { Desc = $"{g}G를 건다 (운)", Check = StatId.Luk, Dc = 10,
                            Success = new EventOutcome { Gold = g * 2 },
                            Fail = new EventOutcome { Gold = -g } },
                        new EventChoice { Desc = "거절한다" },
                    }
                },
                new EventDef
                {
                    Name = "갇힌 모험가",
                    Choices = new[]
                    {
                        new EventChoice { Desc = "잔해를 치운다 (힘)", Check = StatId.Str, Dc = 9,
                            Success = new EventOutcome { Gold = g, Item = ConsumableType.Bomb },
                            Fail = new EventOutcome { HpRatio = -0.10 } },
                        new EventChoice { Desc = "못 본 척한다" },
                    }
                },
                new EventDef
                {
                    Name = "검은 웅덩이",
                    Choices = new[]
                    {
                        new EventChoice { Desc = "손을 넣는다 (운)", Check = StatId.Luk, Dc = 11,
                            Success = new EventOutcome { Gear = true },
                            Fail = new EventOutcome { HpRatio = -0.18 } },
                        new EventChoice { Desc = "연막탄 재료만 뜬다",
                            Success = new EventOutcome { Item = ConsumableType.Smoke } },
                    }
                },
            };
        }

        /// <summary>시뮬레이션용 기대값 선택: HP·골드·아이템을 단일 효용으로 환산.</summary>
        public static int PickByExpectedValue(EventDef def, StatBlock stats, double hpRatio, int gold)
        {
            int best = 0;
            double bestEv = double.MinValue;
            for (int i = 0; i < def.Choices.Length; i++)
            {
                var c = def.Choices[i];
                double p = c.SuccessChance(stats);
                double ev = p * Utility(c.Success, hpRatio, gold) + (1 - p) * Utility(c.Fail, hpRatio, gold);
                if (ev > bestEv) { bestEv = ev; best = i; }
            }
            return best;
        }

        private static double Utility(EventOutcome o, double hpRatio, int gold)
        {
            double u = o.Gold * 0.01 + o.Xp * 0.02 + (o.Gear ? 1.2 : 0) + (o.Item != null ? 0.8 : 0);
            // HP 가치는 낮을수록 비쌈 / 골드 지출은 잔고 없으면 불가
            u += o.HpRatio * (o.HpRatio < 0 ? (hpRatio < 0.4 ? 6.0 : 2.5) : (hpRatio < 0.6 ? 4.0 : 1.0));
            if (o.Gold < 0 && gold < -o.Gold) u = double.MinValue; // 못 사는 선택지
            return u;
        }
    }
}
