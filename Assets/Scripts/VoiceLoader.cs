using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Cargador y gestor de perfiles de voz para los luchadores del torneo.
    /// </summary>
    public static class VoiceLoader
    {
        private static readonly Dictionary<string, FighterVoiceProfile> cache = new Dictionary<string, FighterVoiceProfile>();

        public static FighterVoiceProfile LoadProfileForFighter(int id, string defaultName, int headIndex = 0)
        {
            string key = $"{id}_{headIndex}";
            if (cache.TryGetValue(key, out FighterVoiceProfile cached) && cached != null)
            {
                return cached;
            }

            FighterVoiceProfile profile = new FighterVoiceProfile
            {
                fighterId = id,
                fighterName = defaultName,
                headIndex = headIndex
            };

            string suffix = headIndex > 0 ? $"_Head{headIndex + 1}" : "";
            string dirLocal1 = Path.Combine(Application.dataPath, "..", "Luchadores", $"Jugador_{id}{suffix}");
            string dirLocal2 = Path.Combine(Application.dataPath, "..", "Luchadores", $"Luchador_{id}{suffix}");
            string dir1 = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{id}{suffix}");
            string dir2 = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{id}{suffix}");

            if (Directory.Exists(dirLocal1) && profile.LoadFromDirectory(dirLocal1))
            {
                cache[key] = profile;
                return profile;
            }

            if (Directory.Exists(dirLocal2) && profile.LoadFromDirectory(dirLocal2))
            {
                cache[key] = profile;
                return profile;
            }

            if (Directory.Exists(dir1) && profile.LoadFromDirectory(dir1))
            {
                cache[key] = profile;
                return profile;
            }

            if (Directory.Exists(dir2) && profile.LoadFromDirectory(dir2))
            {
                cache[key] = profile;
                return profile;
            }

            cache[key] = profile;
            return profile;
        }

        public static void ClearCacheForFighter(int id, int headIndex = 0)
        {
            string key = $"{id}_{headIndex}";
            if (cache.ContainsKey(key))
            {
                cache.Remove(key);
            }
        }

        public static void ClearAllCache()
        {
            cache.Clear();
        }
    }
}
