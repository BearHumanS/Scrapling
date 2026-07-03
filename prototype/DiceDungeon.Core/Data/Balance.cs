using System;

namespace DiceDungeon.Core.Data
{
    /// <summary>
    /// 초기 밸런스 상수. 프로토타입 단계에서는 코드 상수로 두고,
    /// Phase 2에서 구글 시트 → JSON 파이프라인으로 이전한다 (03-기술설계 참조).
    /// 기준 수치는 02-시스템설계의 층별 테이블(1층 HP30/공5 → 15층 HP1500/공90)을 지수 보간.
    /// </summary>
    public static class Balance
    {
        public const int BoardSize = 24;
        public const int FinalFloor = 15;

        // 플레이어 초기 스탯
        // (v2) 시뮬레이션 결과 초안(HP100/공10)은 1층 사망률 16%로 과도 → 공격 상향
        public const int PlayerHp = 100;
        public const int PlayerAtk = 12;
        public const int PlayerDef = 5;
        public const int PlayerSpd = 10;
        public const double PlayerCritChance = 0.10;
        public const double CritMultiplier = 1.5;

        // 몬스터 층별 스케일링
        // (v3) 스킬 시스템 도입으로 플레이어 파워 급등 → 지수 재상향 (v2: 1.28/1.19)
        public static int MonsterHp(int floor) => (int)Math.Round(32 * Math.Pow(1.34, floor - 1));
        public static int MonsterAtk(int floor) => (int)Math.Round(5 * Math.Pow(1.23, floor - 1));
        public static int MonsterDef(int floor) => (int)Math.Round(2 * Math.Pow(1.20, floor - 1));

        /// <summary>1~2층은 몬스터 1마리 고정 (튜토리얼 구간 보호).</summary>
        public const int SoloMonsterFloors = 2;

        // 보스 = 해당 층 몬스터의 배율 (v2: 3.0/1.5 → 2.5/1.2, 초반 보스 벽 완화)
        public const double BossHpMult = 2.5;
        public const double BossAtkMult = 1.2;

        // 층 클리어 보상: 회복 (하강 결정을 실질적 선택지로 만들기 위함)
        public const double FloorClearHealRatio = 0.35;

        // 경험치 (07-심화시스템 5): 처치당, 보스 배율
        public static int MonsterXp(int floor) => 5 + floor * 3;
        public const int BossXpMult = 5;

        // 타일 생성 비율 (01-게임기획서 4.1)
        public const double TileBattleRatio = 0.50;
        public const double TileEliteRatio = 0.08;
        public const double TileTreasureRatio = 0.15;
        public const double TileEventRatio = 0.12;
        public const double TileTrapRatio = 0.10;
        // 상점 1개, 쉼터 1개는 층당 보장 배치. 나머지는 위 비율로 채움.

        // 타일 효과
        public const double CapturedTileHealRatio = 0.15; // 점령 칸 재방문 회복
        public const double RestHealRatio = 0.30;
        public const double TrapDamageRatio = 0.10;       // 최대 HP 비례
        public const double TrapAvoidChance = 0.30;       // 민첩 판정 (프로토타입은 고정 확률)
        public const double MimicChance = 0.08;           // 보물 칸 미믹

        // 보상 (층 비례)
        public static int BattleGold(int floor) => 12 + floor * 6;
        public static int TreasureGold(int floor) => 20 + floor * 10;
        public static int LapBonusGold(int floor) => 40 + floor * 15;

        // 장비 성장치 (v2: 깊은 층 장비일수록 강함 — 하강의 리워드)
        public static int GearAtkBonus(int floor) => 2 + floor / 2;
        public static int GearDefBonus(int floor) => 1 + floor / 4;
        public static int GearHpBonus(int floor) => 10 + floor * 3;

        // 상점
        public static int PotionPrice(int floor) => 30 + floor * 10;
        public static int GearPrice(int floor) => 60 + floor * 25;
        public const double PotionHealRatio = 0.40;

        // 런 결산: 소울스톤 = 클리어 층수 가중 합
        public static int SoulstonesForRun(int floorsCleared, int monstersKilled)
            => floorsCleared * 10 + monstersKilled;
        public const double DeathSoulstonePenalty = 0.7; // 사망 시 70%만 회수
    }
}
