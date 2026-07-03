using System.IO;
using DiceDungeon.Core.Meta;
using UnityEngine;

namespace DiceDungeon.Game
{
    /// <summary>
    /// PlayerProfile 저장/로드 (03-기술설계 2.4의 최소 구현).
    /// 프로토타입: 평문 JSON. 출시 전 AES 암호화 + 체크섬 + 이중 슬롯으로 확장한다.
    /// </summary>
    public static class SaveService
    {
        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, "profile.json");

        public static PlayerProfile LoadOrCreate()
        {
            try
            {
                if (File.Exists(Path))
                {
                    var profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(Path));
                    if (profile != null) return Migrate(profile);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"프로필 로드 실패, 새로 생성: {e.Message}");
            }
            return new PlayerProfile();
        }

        public static void Save(PlayerProfile profile)
        {
            File.WriteAllText(Path, JsonUtility.ToJson(profile, prettyPrint: true));
        }

        private static PlayerProfile Migrate(PlayerProfile p)
        {
            // 스키마 버전 마이그레이션 체인 (Version 1이 최초)
            if (p.RelicLevels == null || p.RelicLevels.Length < RelicCatalog.Count)
            {
                var levels = new int[RelicCatalog.Count];
                if (p.RelicLevels != null)
                    for (int i = 0; i < p.RelicLevels.Length; i++) levels[i] = p.RelicLevels[i];
                p.RelicLevels = levels;
            }
            return p;
        }
    }
}
