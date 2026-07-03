using System;
using DiceDungeon.Core.Characters;

namespace DiceDungeon.Core.Meta
{
    /// <summary>
    /// 영구 저장 프로필 (마을 메타 성장의 상태).
    /// public 필드만 사용 — Unity JsonUtility·System.Text.Json 양쪽에서 그대로 직렬화 가능.
    /// 스키마 변경 시 Version을 올리고 마이그레이션을 추가한다 (03-기술설계 2.4).
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public int Version = 1;
        public int Soulstones;
        public int TotalRuns;
        public int BestFloor;

        // 훈련소: 스탯별 강화 레벨
        public int TrainHpLevel;
        public int TrainAtkLevel;
        public int TrainDefLevel;

        // 유물 레벨 (RelicId 순서)
        public int[] RelicLevels = new int[RelicCatalog.Count];

        // 캐릭터 해금 (기사는 기본)
        public bool RogueUnlocked;
        public bool MageUnlocked;
        public bool ClericUnlocked;

        public const int TrainHpPerLevel = 25;
        public const int TrainAtkPerLevel = 3;
        public const int TrainDefPerLevel = 2;
        public const int CharacterUnlockCost = 300;

        /// <summary>훈련 비용: 60 × 1.6^레벨 (지수 소프트캡, 02-시스템설계 4.2). (v3) 50×1.5에서 상향.</summary>
        public static int TrainCost(int currentLevel) => (int)Math.Round(60 * Math.Pow(1.6, currentLevel));

        public bool TryTrain(ref int level)
        {
            int cost = TrainCost(level);
            if (Soulstones < cost) return false;
            Soulstones -= cost;
            level++;
            return true;
        }

        public bool TryUpgradeRelic(RelicId id)
        {
            int lv = RelicLevels[(int)id];
            if (lv >= RelicCatalog.MaxLevel) return false;
            int cost = RelicCatalog.UpgradeCost(lv);
            if (Soulstones < cost) return false;
            Soulstones -= cost;
            RelicLevels[(int)id] = lv + 1;
            return true;
        }

        public bool TryUnlock(CharacterId id)
        {
            if (id == CharacterId.Knight || IsUnlocked(id)) return false;
            if (Soulstones < CharacterUnlockCost) return false;
            Soulstones -= CharacterUnlockCost;
            switch (id)
            {
                case CharacterId.Rogue: RogueUnlocked = true; break;
                case CharacterId.Mage: MageUnlocked = true; break;
                case CharacterId.Cleric: ClericUnlocked = true; break;
            }
            return true;
        }

        public bool IsUnlocked(CharacterId id)
        {
            switch (id)
            {
                case CharacterId.Knight: return true;
                case CharacterId.Rogue: return RogueUnlocked;
                case CharacterId.Mage: return MageUnlocked;
                case CharacterId.Cleric: return ClericUnlocked;
                default: return false;
            }
        }

        public RelicModifiers Modifiers() => RelicModifiers.From(RelicLevels);

        public int TrainedHp => TrainHpLevel * TrainHpPerLevel;
        public int TrainedAtk => TrainAtkLevel * TrainAtkPerLevel;
        public int TrainedDef => TrainDefLevel * TrainDefPerLevel;
    }
}
