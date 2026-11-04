using System;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Perfil de voces de batalla de un luchador:
    /// Contiene los 5 clips de voz personalizados grabados con micrófono o los fallbacks arcade.
    /// </summary>
    [Serializable]
    public class FighterVoiceProfile
    {
        public int fighterId = 1;
        public string fighterName = "Luchador";
        public int headIndex = 0; // 0 = Cabeza 1, 1 = Cabeza 2 (Hombre-Máquina)

        public AudioClip voiceAttack;
        public AudioClip voiceSpecial;
        public AudioClip voiceHurt;
        public AudioClip voiceVictory;
        public AudioClip voiceKO;

        public AudioClip GetVoice(VoiceType type)
        {
            switch (type)
            {
                case VoiceType.Attack: return voiceAttack;
                case VoiceType.Special: return voiceSpecial;
                case VoiceType.Hurt: return voiceHurt;
                case VoiceType.Victory: return voiceVictory;
                case VoiceType.KO: return voiceKO;
                default: return null;
            }
        }

        public void SetVoice(VoiceType type, AudioClip clip)
        {
            switch (type)
            {
                case VoiceType.Attack: voiceAttack = clip; break;
                case VoiceType.Special: voiceSpecial = clip; break;
                case VoiceType.Hurt: voiceHurt = clip; break;
                case VoiceType.Victory: voiceVictory = clip; break;
                case VoiceType.KO: voiceKO = clip; break;
            }
        }

        public bool HasCustomVoice(VoiceType type)
        {
            return GetVoice(type) != null;
        }

        public static string GetFileName(VoiceType type)
        {
            switch (type)
            {
                case VoiceType.Attack: return "Voz_Ataque.wav";
                case VoiceType.Special: return "Voz_Especial.wav";
                case VoiceType.Hurt: return "Voz_Dolor.wav";
                case VoiceType.Victory: return "Voz_Victoria.wav";
                case VoiceType.KO: return "Voz_Derrota.wav";
                default: return "Voz.wav";
            }
        }

        public bool LoadFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return false;

            bool anyLoaded = false;

            string pAttack = Path.Combine(directoryPath, GetFileName(VoiceType.Attack));
            string pSpecial = Path.Combine(directoryPath, GetFileName(VoiceType.Special));
            string pHurt = Path.Combine(directoryPath, GetFileName(VoiceType.Hurt));
            string pVictory = Path.Combine(directoryPath, GetFileName(VoiceType.Victory));
            string pKO = Path.Combine(directoryPath, GetFileName(VoiceType.KO));

            if (File.Exists(pAttack))
            {
                voiceAttack = WavUtility.LoadFromFile(pAttack, $"Voice_Attack_{fighterId}_{headIndex}");
                if (voiceAttack != null) anyLoaded = true;
            }

            if (File.Exists(pSpecial))
            {
                voiceSpecial = WavUtility.LoadFromFile(pSpecial, $"Voice_Special_{fighterId}_{headIndex}");
                if (voiceSpecial != null) anyLoaded = true;
            }

            if (File.Exists(pHurt))
            {
                voiceHurt = WavUtility.LoadFromFile(pHurt, $"Voice_Hurt_{fighterId}_{headIndex}");
                if (voiceHurt != null) anyLoaded = true;
            }

            if (File.Exists(pVictory))
            {
                voiceVictory = WavUtility.LoadFromFile(pVictory, $"Voice_Victory_{fighterId}_{headIndex}");
                if (voiceVictory != null) anyLoaded = true;
            }

            if (File.Exists(pKO))
            {
                voiceKO = WavUtility.LoadFromFile(pKO, $"Voice_KO_{fighterId}_{headIndex}");
                if (voiceKO != null) anyLoaded = true;
            }

            return anyLoaded;
        }

        public void SaveVoice(VoiceType type, AudioClip clip, string directoryPath)
        {
            if (clip == null) return;
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            SetVoice(type, clip);
            string filePath = Path.Combine(directoryPath, GetFileName(type));
            WavUtility.SaveToFile(clip, filePath);
            Debug.Log($"[FighterVoiceProfile] Voz {type} guardada en: {filePath}");
        }

        public void DeleteVoice(VoiceType type, string directoryPath)
        {
            SetVoice(type, null);
            string filePath = Path.Combine(directoryPath, GetFileName(type));
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Debug.Log($"[FighterVoiceProfile] Voz {type} eliminada: {filePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FighterVoiceProfile] No se pudo borrar {filePath}: {ex.Message}");
                }
            }
        }
    }
}
