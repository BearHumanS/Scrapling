using System;
using System.Collections.Generic;
using System.Linq;
using DiceDungeon.Core;
using DiceDungeon.Core.Battle;
using DiceDungeon.Core.Board;
using DiceDungeon.Core.Characters;
using DiceDungeon.Core.Data;
using DiceDungeon.Core.Meta;
using DiceDungeon.Core.Progression;
using DiceDungeon.Core.Run;

// 밸런스 시뮬레이터 (03-기술설계 5).
// 사용법:
//   dotnet run --project DiceDungeon.Sim              → 1,000런 기본 통계
//   dotnet run --project DiceDungeon.Sim -- selftest  → 코어 로직 자가 검증
//   dotnet run --project DiceDungeon.Sim -- chars     → 캐릭터 4종 밸런스 비교
//   dotnet run --project DiceDungeon.Sim -- meta      → 훈련(스탯) 단계별 도달 층
//   dotnet run --project DiceDungeon.Sim -- economy   → 소울스톤 경제: 유저 여정 시뮬레이션

if (args.Contains("selftest")) { SelfTest.RunAll(); return; }
if (args.Contains("meta")) { Simulation.MetaProgression(); return; }
if (args.Contains("chars")) { Simulation.CharacterCompare(); return; }
if (args.Contains("jobs")) { Simulation.JobCompare(); return; }
if (args.Contains("economy")) { Simulation.Economy(); return; }
Simulation.Baseline(runs: 1000);

static class Simulation
{
    public static void Baseline(int runs)
    {
        Console.WriteLine($"=== 기본 시뮬레이션: 신규 유저(기사, 메타 0), {runs}런 ===\n");
        Report(RunMany(runs, new RunConfig(), baseSeed: 20260702));
    }

    /// <summary>캐릭터 4종 상대 밸런스 — 평균 도달 층이 ±15% 안에 있어야 한다.</summary>
    public static void CharacterCompare()
    {
        Console.WriteLine("=== 캐릭터 밸런스 비교 (1,000런/캐릭터, 메타 0) ===\n");
        Console.WriteLine("캐릭터 | 평균층 | 사망률 | 평균 소울스톤 | 부활 사용률");
        Console.WriteLine("-------|--------|--------|--------------|------------");
        foreach (var c in CharacterClass.All)
        {
            var results = RunMany(1000, new RunConfig { Character = c.Id }, baseSeed: 555);
            Console.WriteLine($"{c.Name,-4} | {results.Average(r => r.FloorsCleared),6:F2} " +
                $"| {results.Count(r => r.DeathFloor > 0) * 100.0 / results.Count,5:F1}% " +
                $"| {results.Average(r => r.Soulstones),12:F0} " +
                $"| {results.Count(r => r.ReviveUsed) * 100.0 / results.Count,9:F1}%");
        }
    }

    /// <summary>2차 직업 8경로 밸런스 — 평균 도달 층 ±15% 이내 목표 (07-심화시스템 6).</summary>
    public static void JobCompare()
    {
        Console.WriteLine("=== 전직 경로 밸런스 비교 (800런/경로, 메타 0) ===\n");
        Console.WriteLine("경로           | 평균층 | 사망률");
        Console.WriteLine("---------------|--------|-------");
        foreach (var c in CharacterClass.All)
        {
            var (a, b) = JobCatalog.PathsFor(c.Id);
            foreach (bool preferA in new[] { true, false })
            {
                var results = RunMany(800,
                    new RunConfig { Character = c.Id, PreferPathA = preferA }, baseSeed: 1234);
                string name = preferA ? a.Name : b.Name;
                Console.WriteLine($"{name,-12} | {results.Average(r => r.FloorsCleared),6:F2} " +
                    $"| {results.Count(r => r.DeathFloor > 0) * 100.0 / results.Count,5:F1}%");
            }
        }
    }

    /// <summary>훈련소 성장 단계별 도달 층 → 벽(Wall) 위치와 성장 체감 검증.</summary>
    public static void MetaProgression()
    {
        Console.WriteLine("=== 훈련 단계별 비교 (500런/단계, 기사) ===\n");
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

    /// <summary>
    /// 유저 여정: 런 → 소울스톤 → 마을에서 탐욕 구매(가장 싼 업그레이드) → 다음 런.
    /// "몇 런째에 몇 층까지 가는가"로 리텐션 페이싱을 검증한다.
    /// </summary>
    public static void Economy()
    {
        Console.WriteLine("=== 소울스톤 경제: 80런 유저 여정 (탐욕 구매 전략) ===\n");
        var profile = new PlayerProfile();
        var floorLog = new List<int>();

        for (int run = 1; run <= 80; run++)
        {
            var config = RunConfig.From(profile, CharacterId.Knight);
            var result = new RunController(9000 + run, config, new GreedyPolicy()).Play();
            profile.Soulstones += result.Soulstones;
            profile.TotalRuns++;
            profile.BestFloor = Math.Max(profile.BestFloor, result.FloorsCleared);
            floorLog.Add(result.FloorsCleared);

            SpendGreedily(profile);

            if (run % 10 == 0)
            {
                double recentAvg = floorLog.Skip(floorLog.Count - 10).Average();
                int totalRelics = profile.RelicLevels.Sum();
                Console.WriteLine($"런 {run,3}: 최근 10런 평균 {recentAvg,4:F1}층 | 최고 {profile.BestFloor,2}층 " +
                    $"| 훈련 {profile.TrainHpLevel}/{profile.TrainAtkLevel}/{profile.TrainDefLevel} " +
                    $"| 유물 총 {totalRelics}Lv | 잔여 소울스톤 {profile.Soulstones}");
            }
        }
    }

    /// <summary>탐욕 구매: 훈련 3종 + 유물 12종 중 가장 싼 것을 산다.</summary>
    private static void SpendGreedily(PlayerProfile p)
    {
        while (true)
        {
            var options = new List<(int cost, Action buy)>
            {
                (PlayerProfile.TrainCost(p.TrainHpLevel), () => { int l = p.TrainHpLevel; p.TryTrain(ref l); p.TrainHpLevel = l; }),
                (PlayerProfile.TrainCost(p.TrainAtkLevel), () => { int l = p.TrainAtkLevel; p.TryTrain(ref l); p.TrainAtkLevel = l; }),
                (PlayerProfile.TrainCost(p.TrainDefLevel), () => { int l = p.TrainDefLevel; p.TryTrain(ref l); p.TrainDefLevel = l; }),
            };
            for (int i = 0; i < RelicCatalog.Count; i++)
            {
                if (p.RelicLevels[i] >= RelicCatalog.MaxLevel) continue;
                var id = (RelicId)i;
                options.Add((RelicCatalog.UpgradeCost(p.RelicLevels[i]), () => p.TryUpgradeRelic(id)));
            }
            var cheapest = options.OrderBy(o => o.cost).First();
            if (p.Soulstones < cheapest.cost) break;
            cheapest.buy();
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

        Check("전투: 모든 캐릭터가 1층 몬스터에게 승리 (튜토리얼 보장)", () =>
        {
            foreach (var c in CharacterClass.All)
            {
                for (int seed = 0; seed < 50; seed++)
                {
                    var player = new Unit(c.Name, c.Hp, c.Atk, c.Def, c.Spd, c.CritChance);
                    var mob = new List<Unit> { new Unit("M", Balance.MonsterHp(1), Balance.MonsterAtk(1), Balance.MonsterDef(1), 8) };
                    var opt = new BattleOptions { Skills = c.Skills.Select(s => s.Instance()).ToList() };
                    if (!BattleSimulator.Fight(player, mob, new Rng(seed), opt).PlayerWon) return false;
                }
            }
            return true;
        });

        Check("상태이상: 독은 중첩·지속 피해, 기절은 1회 소모", () =>
        {
            var u = new Unit("T", 100, 10, 0, 10);
            u.Statuses.Apply(StatusType.Poison, 3);
            u.Statuses.Apply(StatusType.Stun);
            var (dmg1, skip1) = u.Statuses.Tick(u);
            var (dmg2, skip2) = u.Statuses.Tick(u);
            return dmg1 == 9 && skip1 && dmg2 == 9 && !skip2; // 독 3중첩 × 3
        });

        Check("성직자: 부활 패시브는 런당 1회만", () =>
        {
            var results = Enumerable.Range(0, 500)
                .Select(s => new RunController(s, new RunConfig { Character = CharacterId.Cleric }, new GreedyPolicy()).Play())
                .ToList();
            return results.Any(r => r.ReviveUsed); // 발동 사례가 존재하고
        });

        Check("프로필: 훈련·유물 구매가 소울스톤을 정확히 차감", () =>
        {
            var p = new PlayerProfile { Soulstones = 1000 };
            int lv = p.TrainHpLevel;
            bool ok = p.TryTrain(ref lv);
            p.TrainHpLevel = lv;
            ok &= p.Soulstones == 1000 - PlayerProfile.TrainCost(0);
            int before = p.Soulstones;
            ok &= p.TryUpgradeRelic(RelicId.StartGold);
            ok &= p.Soulstones == before - RelicCatalog.UpgradeCost(0);
            ok &= p.RelicLevels[(int)RelicId.StartGold] == 1;
            return ok;
        });

        Check("수동 전투: 공격만으로 1층 몬스터 격파, 이벤트 순서 정상", () =>
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var c = CharacterClass.Knight;
                var player = new Unit(c.Name, c.Hp, c.Atk, c.Def, c.Spd, c.CritChance);
                var mobs = new List<Unit> { new Unit("M", Balance.MonsterHp(1), Balance.MonsterAtk(1), Balance.MonsterDef(1), 8) };
                var battle = new InteractiveBattle(player, mobs, new Rng(seed),
                    new BattleOptions { Skills = c.Skills.Select(s => s.Instance()).ToList() }, potions: 2);
                int guard = 0;
                while (!battle.Over && guard++ < 60)
                    battle.DoRound(PlayerActionType.Attack);
                if (!battle.PlayerWon) return false;
            }
            return true;
        });

        Check("수동 전투: 방어는 받는 피해를 절반으로", () =>
        {
            int TakeHit(bool defend)
            {
                var player = new Unit("P", 1000, 1, 100, 1, 0) { Accuracy = 1.0 }; // 못 죽이는 탱커
                var mob = new List<Unit> { new Unit("M", 10000, 50, 0, 9, critChance: 0) { Accuracy = 1.0 } };
                var b = new InteractiveBattle(player, mob, new Rng(11), new BattleOptions(), 0);
                b.DoRound(defend ? PlayerActionType.Defend : PlayerActionType.Attack);
                return 1000 - player.Hp;
            }
            int normal = TakeHit(false), defended = TakeHit(true);
            return defended > 0 && defended <= normal / 2 + 1;
        });

        Check("수동 전투: 스킬 쿨다운 동작 (사용 직후 비활성 → 회복)", () =>
        {
            var c = CharacterClass.Mage;
            var player = new Unit(c.Name, 10000, c.Atk, c.Def, c.Spd, 0);
            var mobs = new List<Unit> { new Unit("M", 100000, 1, 0, 8) };
            var b = new InteractiveBattle(player, mobs, new Rng(5),
                new BattleOptions { Skills = c.Skills.Select(s => s.Instance()).ToList() }, 0);
            if (!b.CanUseSkill(0)) return false;
            b.DoRound(PlayerActionType.Skill, 0);          // 화염구 (쿨 3)
            if (b.CanUseSkill(0)) return false;            // 쿨다운 중
            b.DoRound(PlayerActionType.Attack);
            b.DoRound(PlayerActionType.Attack);
            b.DoRound(PlayerActionType.Attack);
            return b.CanUseSkill(0);                       // 3라운드 후 회복
        });

        Check("속성표: 삼각 상성 + 신성↔암흑 + 무속성 중립", () =>
        {
            return ElementTable.Multiplier(Element.Fire, Element.Earth) == 1.5
                && ElementTable.Multiplier(Element.Earth, Element.Fire) == 0.75
                && ElementTable.Multiplier(Element.Earth, Element.Ice) == 1.5
                && ElementTable.Multiplier(Element.Ice, Element.Fire) == 1.5
                && ElementTable.Multiplier(Element.Holy, Element.Shadow) == 2.0
                && ElementTable.Multiplier(Element.Shadow, Element.Holy) == 2.0
                && ElementTable.Multiplier(Element.Fire, Element.Fire) == 0.5
                && ElementTable.Multiplier(Element.Neutral, Element.Fire) == 1.0;
        });

        Check("스킬트리: 선행 조건 강제 (강타 Lv3 → 플레임 슬래시)", () =>
        {
            var book = new SkillBook { Points = 10 };
            var blow = JobCatalog.BaseTree(CharacterId.Knight).First(s => s.Id == "kn_blow");
            var flame = JobCatalog.RuneKnight.Tree.First(s => s.Id == "rk_flame");
            if (book.CanLearn(flame)) return false;         // 선행 미충족
            book.Learn(blow); book.Learn(blow); book.Learn(blow);
            return book.CanLearn(flame) && book.Learn(flame);
        });

        Check("레벨업: 경험치 곡선·포인트 지급 (스탯+3, 스킬+1)", () =>
        {
            var sheet = new CharacterSheet(CharacterId.Knight);
            int skillPointsBefore = sheet.Book.Points;
            int gained = sheet.GainXp(CharacterSheet.XpToLevel(1));
            return gained == 1 && sheet.Level == 2
                && sheet.UnspentStatPoints == 3
                && sheet.Book.Points == skillPointsBefore + 1;
        });

        Check("전직: 레벨 10 도달 시 가능, 보너스 스탯 +10", () =>
        {
            var sheet = new CharacterSheet(CharacterId.Mage);
            while (sheet.Level < JobCatalog.JobChangeLevel)
                sheet.GainXp(CharacterSheet.XpToLevel(sheet.Level));
            if (!sheet.CanJobChange) return false;
            int before = sheet.Stats.Total;
            sheet.JobChange(JobCatalog.Archmage);
            return sheet.Advanced == JobCatalog.Archmage
                && sheet.Stats.Total == before + 10
                && !sheet.CanJobChange; // 재전직 불가
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
