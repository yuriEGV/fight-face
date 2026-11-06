using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace FightFace
{
    /// <summary>
    /// Motor de audio procedural arcade estilo Street Fighter II:
    /// - Genera clips de audio 16-bit retro en memoria (AudioClip.Create) sin depender de archivos externos.
    /// - Golpes ligeros, pesados, patadas, bloqueos, whoosh de ataque, Hadouken, K.O. y campana de ring.
    /// - Ambiente callejero continuo y modulado para dar atmósfera viva a cada pelea.
    /// - Reproductor de Música de Fondo (BGM) en bucle con ajuste dinámico de volumen (más suave en peleas).
    /// </summary>
    public class FightAudioManager : MonoBehaviour
    {
        public static FightAudioManager Instance { get; private set; }

        private const int SampleRate = 44100;

        [Header("Música de Fondo (BGM)")]
        private AudioSource musicSource;
        private AudioClip bgmClip;
        public bool isMusicEnabled { get; private set; } = true;
        public float menuMusicVolume = 0.35f;   // Volumen agradable no invasivo en menús
        public float fightMusicVolume = 0.12f;  // Volumen suave en peleas para que resalten los golpes

        [Header("Voces de Luchadores")]
        public float voiceVolumeMultiplier { get; private set; } = 1.5f;
        public static readonly float[] VoiceVolumeLevels = new float[] { 1.0f, 1.5f, 2.0f, 2.5f, 0.75f };

        private AudioSource sfxSource;
        private AudioSource sfxHeavySource;
        private AudioSource ambientSource;
        private AudioSource voiceSource;

        private AudioClip clipHitLight;
        private AudioClip clipHitHeavy;
        private AudioClip clipKick;
        private AudioClip clipBlock;
        private AudioClip clipWhoosh;
        private AudioClip clipHadouken;
        private AudioClip clipHadoukenHit;
        private AudioClip clipKO;
        private AudioClip clipRoundBell;
        private AudioClip clipAmbience;
        private AudioClip clipButtonClick;
        private AudioClip clipButtonConfirm;

        private AudioClip clipVoiceAttackDefault;
        private AudioClip clipVoiceSpecialDefault;
        private AudioClip clipVoiceHurtDefault;
        private AudioClip clipVoiceVictoryDefault;
        private AudioClip clipVoiceKODefault;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudio()
        {
            isMusicEnabled = PlayerPrefs.GetInt("FightFace_MusicEnabled", 1) == 1;
            voiceVolumeMultiplier = PlayerPrefs.GetFloat("FightFace_VoiceVolume", 1.5f);

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D sonido estéreo directo

            sfxHeavySource = gameObject.AddComponent<AudioSource>();
            sfxHeavySource.playOnAwake = false;
            sfxHeavySource.spatialBlend = 0f;

            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.volume = 0.14f;
            ambientSource.spatialBlend = 0f;

            // Fuente de Música de Fondo (BGM)
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = menuMusicVolume;
            musicSource.spatialBlend = 0f;

            // Generar clips en memoria
            clipHitLight = GenerateHitLightClip();
            clipHitHeavy = GenerateHitHeavyClip();
            clipKick = GenerateKickClip();
            clipBlock = GenerateBlockClip();
            clipWhoosh = GenerateWhooshClip();
            clipHadouken = GenerateHadoukenClip();
            clipHadoukenHit = GenerateHadoukenHitClip();
            clipKO = GenerateKOClip();
            clipRoundBell = GenerateRoundBellClip();
            clipAmbience = GenerateAmbienceClip();
            clipButtonClick = GenerateButtonClickClip();
            clipButtonConfirm = GenerateButtonConfirmClip();

            clipVoiceAttackDefault = GenerateProceduralAttackVoice();
            clipVoiceSpecialDefault = GenerateProceduralSpecialVoice();
            clipVoiceHurtDefault = GenerateProceduralHurtVoice();
            clipVoiceVictoryDefault = GenerateProceduralVictoryVoice();
            clipVoiceKODefault = GenerateProceduralKOVoice();

            // Iniciar ambiente continuo
            ambientSource.clip = clipAmbience;
            ambientSource.Play();

            // Cargar y reproducir música de fondo si está activada
            StartCoroutine(LoadAndPlayBackgroundMusicRoutine());
        }

        private void Update()
        {
            if (!isMusicEnabled)
            {
                if (musicSource != null && musicSource.isPlaying) musicSource.Stop();
                return;
            }

            if (musicSource == null || !musicSource.isPlaying) return;

            // En peleas activas se reduce el volumen para que resalten los golpes y efectos
            bool inFight = BattleManager.Instance != null && BattleManager.Instance.IsMatchActive;
            if (CharacterSelectUI.Instance != null && CharacterSelectUI.Instance.gameObject.activeInHierarchy)
            {
                inFight = false;
            }

            float targetVol = inFight ? fightMusicVolume : menuMusicVolume;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVol, Time.unscaledDeltaTime * 0.35f);
        }

        private IEnumerator LoadAndPlayBackgroundMusicRoutine()
        {
            // 1. Intentar cargar desde Resources
            bgmClip = Resources.Load<AudioClip>("Music/Pepper_Kid_en_papa_pepper");
            if (bgmClip != null)
            {
                musicSource.clip = bgmClip;
                if (isMusicEnabled)
                {
                    musicSource.volume = menuMusicVolume;
                    musicSource.Play();
                }
                Debug.Log("[FightAudio] 🎵 Música de fondo 'Pepper Kid en papa pepper' cargada desde Resources.");
                yield break;
            }

            // 2. Cargar en caliente desde archivo .mp3 mediante UnityWebRequestMultimedia
            string[] possiblePaths = {
                Path.Combine(Application.dataPath, "Music", "Pepper_Kid_en_papa_pepper.mp3"),
                Path.Combine(Application.dataPath, "Resources", "Music", "Pepper_Kid_en_papa_pepper.mp3"),
                @"C:\Users\maritimo13\Downloads\Pepper Kid en papa pepper.mp3"
            };

            string validPath = null;
            foreach (var p in possiblePaths)
            {
                if (File.Exists(p))
                {
                    validPath = p;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(validPath))
            {
                string url = "file:///" + validPath.Replace("\\", "/");
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
                {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        bgmClip = DownloadHandlerAudioClip.GetContent(www);
                        if (bgmClip != null)
                        {
                            bgmClip.name = "Pepper_Kid_en_papa_pepper";
                            musicSource.clip = bgmClip;
                            if (isMusicEnabled)
                            {
                                musicSource.volume = menuMusicVolume;
                                musicSource.Play();
                            }
                            Debug.Log($"[FightAudio] 🎵 Música de fondo cargada exitosamente desde: {validPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[FightAudio] No se pudo cargar el audio vía UnityWebRequest: {www.error}");
                    }
                }
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            isMusicEnabled = enabled;
            PlayerPrefs.SetInt("FightFace_MusicEnabled", isMusicEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (musicSource != null)
            {
                if (isMusicEnabled)
                {
                    if (bgmClip != null && !musicSource.isPlaying)
                    {
                        musicSource.volume = menuMusicVolume;
                        musicSource.Play();
                    }
                }
                else
                {
                    musicSource.Stop();
                }
            }
            Debug.Log($"[FightAudio] 🎵 Música {(isMusicEnabled ? "ACTIVADA" : "DESACTIVADA")}");
        }

        public bool ToggleMusic()
        {
            SetMusicEnabled(!isMusicEnabled);
            return isMusicEnabled;
        }

        public void SetMusicVolume(float volume)
        {
            menuMusicVolume = volume;
            fightMusicVolume = volume * 0.35f;
            if (musicSource != null)
            {
                musicSource.volume = volume;
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (isMusicEnabled && musicSource != null && !musicSource.isPlaying) musicSource.UnPause();
        }

        // ================== REPRODUCCIÓN PÚBLICA ==================

        public void PlayHitLight()
        {
            if (sfxSource != null && clipHitLight != null)
            {
                sfxSource.pitch = UnityEngine.Random.Range(0.95f, 1.08f);
                sfxSource.PlayOneShot(clipHitLight, 0.85f);
            }
        }

        public void PlayHitHeavy()
        {
            if (sfxHeavySource != null && clipHitHeavy != null)
            {
                sfxHeavySource.pitch = UnityEngine.Random.Range(0.92f, 1.04f);
                sfxHeavySource.PlayOneShot(clipHitHeavy, 1.0f);
            }
        }

        public void PlayKick(bool isHeavy = false)
        {
            if (isHeavy)
            {
                PlayHitHeavy();
                return;
            }

            if (sfxSource != null && clipKick != null)
            {
                sfxSource.pitch = UnityEngine.Random.Range(0.96f, 1.06f);
                sfxSource.PlayOneShot(clipKick, 0.90f);
            }
        }

        public void PlayBlock()
        {
            if (sfxSource != null && clipBlock != null)
            {
                sfxSource.pitch = UnityEngine.Random.Range(0.98f, 1.05f);
                sfxSource.PlayOneShot(clipBlock, 0.90f);
            }
        }

        public void PlayWhoosh()
        {
            if (sfxSource != null && clipWhoosh != null)
            {
                sfxSource.pitch = UnityEngine.Random.Range(0.90f, 1.15f);
                sfxSource.PlayOneShot(clipWhoosh, 0.60f);
            }
        }

        public void PlayHadouken()
        {
            if (sfxHeavySource != null && clipHadouken != null)
            {
                sfxHeavySource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                sfxHeavySource.PlayOneShot(clipHadouken, 0.95f);
            }
        }

        public void PlayHadoukenHit()
        {
            if (sfxHeavySource != null && clipHadoukenHit != null)
            {
                sfxHeavySource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                sfxHeavySource.PlayOneShot(clipHadoukenHit, 1.0f);
            }
        }

        public void PlayKO()
        {
            if (sfxHeavySource != null && clipKO != null)
            {
                sfxHeavySource.pitch = 1.0f;
                sfxHeavySource.PlayOneShot(clipKO, 1.1f);
            }
        }

        public void PlayRoundBell()
        {
            if (sfxHeavySource != null && clipRoundBell != null)
            {
                sfxHeavySource.pitch = 1.0f;
                sfxHeavySource.PlayOneShot(clipRoundBell, 0.95f);
            }
        }

        public void PlayButtonClick(float volume = 0.85f)
        {
            if (sfxSource != null && clipButtonClick != null)
            {
                sfxSource.pitch = UnityEngine.Random.Range(0.96f, 1.05f);
                sfxSource.PlayOneShot(clipButtonClick, volume);
            }
        }

        public void PlayButtonConfirm(float volume = 1f)
        {
            if (sfxHeavySource != null && clipButtonConfirm != null)
            {
                sfxHeavySource.pitch = 1.0f;
                sfxHeavySource.PlayOneShot(clipButtonConfirm, volume);
            }
        }

        public void SetVoiceVolumeMultiplier(float multiplier)
        {
            voiceVolumeMultiplier = Mathf.Clamp(multiplier, 0.25f, 3.0f);
            PlayerPrefs.SetFloat("FightFace_VoiceVolume", voiceVolumeMultiplier);
            PlayerPrefs.Save();
        }

        public float CycleVoiceVolume()
        {
            int currentIndex = -1;
            for (int i = 0; i < VoiceVolumeLevels.Length; i++)
            {
                if (Mathf.Abs(voiceVolumeMultiplier - VoiceVolumeLevels[i]) < 0.05f)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % VoiceVolumeLevels.Length;
            SetVoiceVolumeMultiplier(VoiceVolumeLevels[nextIndex]);

            // Reproducir un grito de prueba de inmediato con el nuevo volumen
            PlayProceduralVoice(VoiceType.Attack, 1.0f, 1.0f);

            Debug.Log($"[FightAudio] 🗣️ Volumen de voces ajustado a {Mathf.RoundToInt(voiceVolumeMultiplier * 100f)}%");
            return voiceVolumeMultiplier;
        }

        public string GetVoiceVolumeText()
        {
            return $"🗣️ VOCES [V]: {Mathf.RoundToInt(voiceVolumeMultiplier * 100f)}%";
        }

        public void PlayFighterVoice(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip != null && voiceSource != null)
            {
                voiceSource.pitch = pitch;
                float finalVolume = Mathf.Clamp(volume * voiceVolumeMultiplier, 0f, 3.0f);
                voiceSource.PlayOneShot(clip, finalVolume);
            }
        }

        public void PlayProceduralVoice(VoiceType type, float volume = 1f, float pitch = 1f)
        {
            AudioClip target = null;
            switch (type)
            {
                case VoiceType.Attack: target = clipVoiceAttackDefault; break;
                case VoiceType.Special: target = clipVoiceSpecialDefault; break;
                case VoiceType.Hurt: target = clipVoiceHurtDefault; break;
                case VoiceType.Victory: target = clipVoiceVictoryDefault; break;
                case VoiceType.KO: target = clipVoiceKODefault; break;
            }

            if (target != null && voiceSource != null)
            {
                voiceSource.pitch = pitch;
                float finalVolume = Mathf.Clamp(volume * voiceVolumeMultiplier, 0f, 3.0f);
                voiceSource.PlayOneShot(target, finalVolume);
            }
        }

        // ================== GENERADORES PROCEDURALES ==================

        private AudioClip GenerateHitLightClip()
        {
            float duration = 0.12f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-32f * t);
                // Golpe con frecuencia descendente (260Hz -> 80Hz) + ruido blanco suave
                float freq = Mathf.Lerp(260f, 80f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                data[i] = Mathf.Clamp((tone + noise) * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_HitLight", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateHitHeavyClip()
        {
            float duration = 0.28f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-16f * t);
                // Sub-grave contundente (140Hz -> 40Hz)
                float freq = Mathf.Lerp(140f, 40f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                // Crujido de impacto distorsionado
                float crunch = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-28f * t);
                float sample = (tone * 1.3f + crunch * 0.8f) * env;
                // Saturación arcade suave (soft clipping)
                data[i] = Mathf.Clamp(Mathf.Sin(sample * 1.4f), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_HitHeavy", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateKickClip()
        {
            float duration = 0.16f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-24f * t);
                // Golpe de aire rápido y seco
                float freq = Mathf.Lerp(320f, 65f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float thud = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                data[i] = Mathf.Clamp((tone + thud) * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Kick", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateBlockClip()
        {
            float duration = 0.14f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-36f * t);
                // Timbre metálico de guardia / bloqueo
                float ring1 = Mathf.Sin(2f * Mathf.PI * 980f * t);
                float ring2 = Mathf.Sin(2f * Mathf.PI * 1420f * t) * 0.6f;
                float thud = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.5f;
                data[i] = Mathf.Clamp((ring1 + ring2 + thud) * env * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Block", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateWhooshClip()
        {
            float duration = 0.14f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                // Envolvente de campana rápida (sube y baja)
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                float noise = UnityEngine.Random.value * 2f - 1f;
                data[i] = Mathf.Clamp(noise * env * 0.55f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Whoosh", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateHadoukenClip()
        {
            float duration = 0.42f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(progress, 0.7f));

                // Frecuencia de plasma que sube rápidamente
                float mod = Mathf.Sin(2f * Mathf.PI * 40f * t) * 35f;
                float carrierFreq = Mathf.Lerp(180f, 520f, progress) + mod;
                float wave = Mathf.Sin(2f * Mathf.PI * carrierFreq * t);
                float sizzle = (UnityEngine.Random.value * 2f - 1f) * 0.4f;

                data[i] = Mathf.Clamp((wave + sizzle) * env * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Hadouken", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateHadoukenHitClip()
        {
            float duration = 0.32f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-14f * t);
                float bass = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 40f, t / duration) * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-20f * t);
                data[i] = Mathf.Clamp((bass * 1.1f + noise * 0.9f) * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_HadoukenHit", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateKOClip()
        {
            float duration = 1.10f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-4.5f * t);
                // Caída de baja frecuencia profunda y dramática de K.O.
                float f = Mathf.Lerp(110f, 28f, t / duration);
                float sub = Mathf.Sin(2f * Mathf.PI * f * t);
                float boomNoise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-12f * t) * 0.7f;
                float mixed = (sub * 1.2f + boomNoise) * env;
                data[i] = Mathf.Clamp(Mathf.Sin(mixed * 1.5f), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_KO", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateRoundBellClip()
        {
            float duration = 0.95f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            // Dos campanazos clásicos de boxeo (t=0s y t=0.24s)
            float strike2Time = 0.24f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float bell = 0f;

                // Primer campanazo
                if (t >= 0f)
                {
                    float dt1 = t;
                    float env1 = Mathf.Exp(-7f * dt1);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 1380f * dt1) * 0.6f + Mathf.Sin(2f * Mathf.PI * 2760f * dt1) * 0.35f;
                    bell += tone1 * env1;
                }

                // Segundo campanazo
                if (t >= strike2Time)
                {
                    float dt2 = t - strike2Time;
                    float env2 = Mathf.Exp(-6f * dt2);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 1380f * dt2) * 0.75f + Mathf.Sin(2f * Mathf.PI * 2760f * dt2) * 0.40f;
                    bell += tone2 * env2;
                }

                data[i] = Mathf.Clamp(bell * 0.75f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_RoundBell", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateAmbienceClip()
        {
            float duration = 4.0f; // bucle de 4 segundos
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            float lastNoise = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                // Ruido filtrado marrón suave (hum de ciudad / calle nocturna)
                float white = UnityEngine.Random.value * 2f - 1f;
                lastNoise = (lastNoise + (0.025f * white)) / 1.025f;

                // Murmullo sutil modulado
                float crowdLFO = 0.6f + Mathf.Sin(2f * Mathf.PI * 0.4f * t) * 0.25f;
                float hum = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.15f;

                // Suavizar extremos para bucle sin clics
                float fade = Mathf.Clamp01(Mathf.Sin(Mathf.PI * (t / duration)));

                data[i] = Mathf.Clamp((lastNoise * 0.75f + hum) * crowdLFO * fade * 0.35f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Ambience_StreetLoop", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateButtonClickClip()
        {
            float duration = 0.045f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-75f * t);
                // Beep retro arcade ascendente (700Hz -> 1400Hz) con micro-impacto
                float freq = Mathf.Lerp(700f, 1400f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float click = (i < 30) ? 0.5f : 0f;
                data[i] = Mathf.Clamp((tone * 0.85f + click) * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_ButtonClick", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateButtonConfirmClip()
        {
            float duration = 0.24f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-16f * t);
                // Acorde arcade confirmatorio ascendente (Do -> Sol: 523Hz -> 784Hz)
                float freq = t < 0.08f ? 523.25f : 783.99f;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.35f;
                data[i] = Mathf.Clamp((tone + sub) * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_ButtonConfirm", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ================== VOCES PROCEDURALES ARCADE ==================

        private AudioClip GenerateProceduralAttackVoice()
        {
            float duration = 0.28f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                float freq = Mathf.Lerp(210f, 135f, t / duration);

                // Formantes de voz "HAH!"
                float tone1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * (freq * 3.5f) * t) * 0.45f;
                float tone3 = Mathf.Sin(2f * Mathf.PI * (freq * 5.8f) * t) * 0.25f;
                float breath = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-18f * t) * 0.2f;

                data[i] = Mathf.Clamp((tone1 + tone2 + tone3 + breath) * env * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Voice_Procedural_Attack", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateProceduralSpecialVoice()
        {
            float duration = 0.52f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                float freq = (t < 0.22f) ? Mathf.Lerp(140f, 280f, t / 0.22f) : Mathf.Lerp(280f, 210f, (t - 0.22f) / 0.30f);

                // Grito potente "HAA-DOU!" con resonancia
                float voice = Mathf.Sin(2f * Mathf.PI * freq * t);
                float harmonic = Mathf.Sin(2f * Mathf.PI * (freq * 2.8f) * t) * 0.55f;
                float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.35f;

                data[i] = Mathf.Clamp((voice + harmonic + sub) * env * 0.90f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Voice_Procedural_Special", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateProceduralHurtVoice()
        {
            float duration = 0.22f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-12f * t);
                float freq = Mathf.Lerp(250f, 95f, t / duration);

                // Quejido sordo "UGH!"
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float distortion = Mathf.Clamp(tone * 1.8f, -0.8f, 0.8f);

                data[i] = Mathf.Clamp(distortion * env * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Voice_Procedural_Hurt", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateProceduralVictoryVoice()
        {
            float duration = 0.58f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Clamp01(Mathf.Sin(Mathf.PI * (t / duration)));
                float note = (t < 0.18f) ? 310f : (t < 0.36f ? 390f : 520f);

                // Tono alegre y celebratorio
                float tone = Mathf.Sin(2f * Mathf.PI * note * t);
                float chime = Mathf.Sin(2f * Mathf.PI * (note * 2f) * t) * 0.35f;

                data[i] = Mathf.Clamp((tone + chime) * env * 0.80f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Voice_Procedural_Victory", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateProceduralKOVoice()
        {
            float duration = 0.80f;
            int totalSamples = (int)(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-3.5f * t);
                float tremolo = 1f + 0.3f * Mathf.Sin(2f * Mathf.PI * 7f * t);
                float freq = Mathf.Lerp(175f, 60f, t / duration);

                // Gemido dramático descendente "GWAAAH..."
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float grit = Mathf.Sin(2f * Mathf.PI * (freq * 2.2f) * t) * 0.4f;

                data[i] = Mathf.Clamp((tone + grit) * env * tremolo * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Voice_Procedural_KO", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
