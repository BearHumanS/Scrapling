using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Board;
using DiceDungeon.Core.Data;
using DiceDungeon.Core.Run;

// 밸런스 시뮬레이터 (03-기술설계 5).
// 사용법:
//   dotnet run --project DiceDungeon.Sim             → 1,000런 시뮬레이션 통계
//   dotnet run --project DiceDungeon.Sim -- selftest → 코어 로직 자가 검증
//   dotnet run --project DiceDungeon.Sim -- meta     → 메타 성장 단계별 도달 층 비교

if (args.Contains("selftest")) { SelfTest.RunAll(); return; }
if (args.Contains("meta")) { Simulation.MetaProgression(); return; }
Simulation.Baseline(runs: 1000);

static class Simulation
{
    public static void Baseline(int runs)
    {
        Console.WriteLine($"=== 기본 시뮬레이션: 신규 유저(메타 성장 0), {runs}런 ===\n");
        Report(RunMany(runs, new RunConfig(), baseSeed: 20260702));
    }

    /// <summary>훈련소/유물 성장 단계별로 평균 도달 층이 어떻게 움직이는지 → 벽(Wall) 위치 검증.</summary>
    public static void MetaProgression()
    {
        Console.WriteLine("=== 메타 성장 단계별 비교 (500런/단계) ===\n");
        Console.WriteLine("단계 | 보너스(HP/공/방) | 평균층 | 15층 클리어율");
        Console.WriteLine("-----|------------------|--------|--------------");
        for (int stage = 0; stage <= 10; stage++)
        {
            var config = new RunConfig { BonusHp = stage * 25, BonusAtk = stage * 3, BonusDef = stage * 2 };
            var results = RunMany(500, config, baseSeed: 777 + stage);
            double avgFloor = results.Average(r => r.FloorsCleared);
            double clearRate = results.Count(r => r.ClearedFinalFloor) * 100.0 / results.Count;
            Console.WriteLine($"{stage,4} | +{config.BonusHp}/{config.BonusAtk}/{config.BonusDef,-10} | {avgFloor,6:F1} | {clearRate,6:F1}%");
        }
    }

    private static List<RunResult> RunMany(int runs, RunConfig config, int baseSeed)
    {
        var results = new List<RunResult>(runs);
        for (int i = 0; i < runs; i++)
            results.Add(new RunController(baseSeed + i, config, new GreedyPolicy()).Play());
        return results;
    }

    private static void Report(List<RunResult> results)
    {
        int n = results.Count;
        Console.WriteLine($"평균 도달 층      : {results.Average(r => r.FloorsCleared):F2}");
        Console.WriteLine($"15층 클리어율     : {results.Count(r => r.ClearedFinalFloor) * 100.0 / n:F1}%");
        Console.WriteLine($"사망률            : {results.Count(r => r.DeathFloor > 0) * 100.0 / n:F1}%");
        Console.WriteLine($"평균 소울스톤     : {results.Average(r => r.Soulstones):F0}");
        Console.WriteLine($"평균 주사위 횟수  : {results.Average(r => r.DiceRolls):F1} (≒ 세션 길이 프록시)");
        Console.WriteLine($"평균 처치 수      : {results.Average(r => r.MonstersKilled):F1}");

        Console.WriteLine("\n층별 사망 분포 (벽 위치 확인):");
        var deaths = results.Where(r => r.DeathFloor > 0).GroupBy(r => r.DeathFloor)
                            .ToDictionary(g => g.Key, g => g.Count());
        for (int f = 1; f <= Balance.FinalFloor; f++)
        {
            int d = deaths.TryGetValue(f, out var c) ? c : 0;
            Console.WriteLine($"  {f,2}층: {new string('#', d * 60 / Math.Max(1, n / 10))} {d * 100.0 / n:F1}%");
        }
    }
}

static class SelfTest
{
    public static void RunAll()
    {
        Check("같은 시드는 같은 결과 (결정론)", () =>
        {
            var a = new RunController(42, new RunConfig(), new GreedyPolicy()).Play();
            var b = new RunController(42, new RunConfig(), new GreedyPolicy()).Play();
            return a.FloorsCleared == b.FloorsCleared && a.Gold == b.Gold
                && a.Soulstones == b.Soulstones && a.DiceRolls == b.DiceRolls;
        });

        Check("층 생성: 24칸, 출발 1개, 상점·쉼터 보장", () =>
        {
            var tiles = FloorGenerator.Generate(1, new Rng(7));
            return tiles.Count == Balance.BoardSize
                && tiles[0].Type == TileType.Start
                && tiles.Count(t => t.Type == TileType.Shop) == 1
                && tiles.Count(t => t.Type == TileType.Rest) >= 1;
        });

        Check("주사위: 1만 회 굴려 2~12 범위, 홀짝 부적 동작", () =>
        {
            var roller = new DiceRoller(new Rng(1));
            for (int i = 0; i < 10000; i++)
            {
                var r = roller.Roll();
                if (r.Sum < 2 || r.Sum > 12) return false;
            }
            for (int i = 0; i < 100; i++)
                if (roller.RollWithParity(even: true).Sum % 2 != 0) return false;
            return true;
        });

        Check("전투: 압도적 강자가 승리", () =>
        {
            var strong = new Unit("S", 1000, 100, 50, 10);
            var weak = new List<Unit> { new Unit("W", 10, 1, 0, 5) };
            return BattleSimulator.Fight(strong, weak, new Rng(3)).PlayerWon;
        });

        Check("전투: 1층 몬스터에게 초기 스탯으로 승리 (튜토리얼 보장)", () =>
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var player = new Unit("P", Balance.PlayerHp, Balance.PlayerAtk,
                                      Balance.PlayerDef, Balance.PlayerSpd, Balance.PlayerCritChance);
                var mob = new List<Unit> { new Unit("M", Balance.MonsterHp(1), Balance.MonsterAtk(1), Balance.MonsterDef(1), 8) };
                if (!BattleSimulator.Fight(player, mob, new Rng(seed)).PlayerWon) return false;
            }
            return true;
        });

        Check("사망 시 소울스톤 페널티 적용", () =>
        {
            var died = Enumerable.Range(0, 2000)
                .Select(s => new RunController(s, new RunConfig(), new GreedyPolicy(descendHpThreshold: 0)).Play())
                .FirstOrDefault(r => r.DeathFloor > 0);
            if (died == null) return false;
            int full = Balance.SoulstonesForRun(died.FloorsCleared, died.MonstersKilled);
            return died.Soulstones == (int)(full * Balance.DeathSoulstonePenalty);
        });

        Console.WriteLine($"\n{_passed}/{_total} 통과");
        if (_passed != _total) Environment.Exit(1);
    }

    private static int _total, _passed;

    private static void Check(string name, Func<bool> test)
    {
        _total++;
        bool ok;
        try { ok = test(); }
        catch (Exception e) { Console.WriteLine($"[실패] {name} — 예외: {e.Message}"); return; }
        if (ok) { _passed++; Console.WriteLine($"[통과] {name}"); }
        else Console.WriteLine($"[실패] {name}");
    }
}
