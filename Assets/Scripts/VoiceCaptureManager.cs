using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Gestiona la grabación y gestión de voces de batalla de los luchadores usando el micrófono:
    /// 1. 💥 Ataque: Grito al golpear fuerte ("¡Toma!", "¡Muere!")
    /// 2. 🌀 Especial: Grito al disparar Hadouken ("¡Hadouken!", "¡Fuego!")
    /// 3. 😣 Dolor: Quejido al recibir daño / Stun ("¡Ayyy!", "¡Ouch!")
    /// 4. 👑 Victoria: Frase de triunfo al noquear ("¡Soy el mejor!", "¡Victoria!")
    /// 5. 💀 Derrota: Grito agónico de K.O. ("¡Nooooo!", "¡Maldición!")
    /// </summary>
    public class VoiceCaptureManager : MonoBehaviour
    {
        public static VoiceCaptureManager Instance { get; private set; }

        [Header("Estado de Luchador")]
        public int targetFighterId = 1;
        public int currentHeadIndex = 0; // 0 = Cabeza 1, 1 = Cabeza 2 (Hombre-Máquina)
        public Text fighterTitleText;
        public Text micStatusText;
        public Text recordingStatusBadge;

        [Header("Slots de UI de Voz")]
        public VoiceSlotUI slotAttack;
        public VoiceSlotUI slotSpecial;
        public VoiceSlotUI slotHurt;
        public VoiceSlotUI slotVictory;
        public VoiceSlotUI slotKO;

        private FighterVoiceProfile activeProfile;
        private string saveFolderPath;
        private string activeMicDevice;
        private AudioClip micRecordingClip;
        private Coroutine activeRecordingRoutine;
        private bool isRecording = false;
        private VoiceType currentRecordingType;

        public bool IsRecording => isRecording;
        public FighterVoiceProfile CurrentProfile => activeProfile;

        [Serializable]
        public class VoiceSlotUI
        {
            public VoiceType type;
            public Text labelText;
            public Text statusText;
            public Button recordButton;
            public Button playButton;
            public Button deleteButton;
            public Image recordButtonBg;
            public Text recordButtonText;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            CheckMicrophoneDevices();
        }

        public void CheckMicrophoneDevices()
        {
            if (Microphone.devices != null && Microphone.devices.Length > 0)
            {
                activeMicDevice = Microphone.devices[0];
                if (micStatusText != null)
                {
                    micStatusText.text = $"🎤 Micrófono: <b>{activeMicDevice}</b> <color=#44FF44>● CONECTADO</color>";
                }
            }
            else
            {
                activeMicDevice = null;
                if (micStatusText != null)
                {
                    micStatusText.text = "⚠️ Micrófono: <color=#FFAA00>NO DETECTADO</color> (se usarán gritos arcade procedurales)";
                }
            }
        }

        public void SetTargetFighter(int fighterId, int headIndex = 0)
        {
            targetFighterId = fighterId;
            currentHeadIndex = headIndex;
            UpdateSaveFolderPath();
            RefreshActiveProfile();
            UpdateAllSlotVisuals();
        }

        public void SetHeadIndex(int headIndex)
        {
            currentHeadIndex = headIndex;
            UpdateSaveFolderPath();
            RefreshActiveProfile();
            UpdateAllSlotVisuals();
        }

        private void UpdateSaveFolderPath()
        {
            string suffix = currentHeadIndex > 0 ? $"_Head{currentHeadIndex + 1}" : "";
            saveFolderPath = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{targetFighterId}{suffix}");
        }

        private void RefreshActiveProfile()
        {
            string fighterName = GetFighterDefaultName(targetFighterId);
            activeProfile = VoiceLoader.LoadProfileForFighter(targetFighterId, fighterName, currentHeadIndex);

            if (fighterTitleText != null)
            {
                string headTag = (targetFighterId == 6) ? $" - <b>[CABEZA {currentHeadIndex + 1}]</b>" : "";
                fighterTitleText.text = $"Estudio de Voz: <b>Luchador {targetFighterId} ({fighterName})</b>{headTag}";
            }
        }

        public void ToggleRecording(VoiceType type, float maxDuration = 2.0f)
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording(type, maxDuration);
            }
        }

        public void StartRecording(VoiceType type, float maxDuration = 2.0f)
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            }
#endif

            if (isRecording) StopRecording();

            CheckMicrophoneDevices();
            if (string.IsNullOrEmpty(activeMicDevice))
            {
                if (recordingStatusBadge != null)
                {
                    recordingStatusBadge.text = "⚠️ Conecta un micrófono a tu equipo para grabar.";
                    recordingStatusBadge.color = new Color(1f, 0.4f, 0.4f);
                }
                return;
            }

            currentRecordingType = type;
            isRecording = true;

            int sampleRate = 44100;
            micRecordingClip = Microphone.Start(activeMicDevice, false, Mathf.CeilToInt(maxDuration), sampleRate);

            if (activeRecordingRoutine != null) StopCoroutine(activeRecordingRoutine);
            activeRecordingRoutine = StartCoroutine(RecordingCountdownRoutine(type, maxDuration));

            UpdateRecordButtonVisual(type, true);
        }

        private IEnumerator RecordingCountdownRoutine(VoiceType type, float maxDuration)
        {
            float elapsed = 0f;
            while (elapsed < maxDuration && isRecording)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0f, maxDuration - elapsed);

                if (recordingStatusBadge != null)
                {
                    bool blink = ((int)(elapsed * 4f)) % 2 == 0;
                    string dot = blink ? "🔴" : "⭕";
                    recordingStatusBadge.text = $"{dot} <b>GRABANDO {type.ToString().ToUpper()}...</b> {remaining:F1}s (¡Habla fuerte al mic!)";
                    recordingStatusBadge.color = blink ? new Color(1f, 0.25f, 0.25f) : new Color(1f, 0.8f, 0.2f);
                }

                yield return null;
            }

            if (isRecording)
            {
                StopRecording();
            }
        }

        public void StopRecording()
        {
            if (!isRecording) return;
            isRecording = false;

            if (activeRecordingRoutine != null)
            {
                StopCoroutine(activeRecordingRoutine);
                activeRecordingRoutine = null;
            }

            int lastPos = 0;
            if (!string.IsNullOrEmpty(activeMicDevice) && Microphone.IsRecording(activeMicDevice))
            {
                lastPos = Microphone.GetPosition(activeMicDevice);
                Microphone.End(activeMicDevice);
            }

            if (micRecordingClip != null)
            {
                if (lastPos <= 0 || lastPos > micRecordingClip.samples)
                {
                    lastPos = micRecordingClip.samples;
                }

                // Recortar audio al punto exacto donde se detuvo
                AudioClip trimmed = WavUtility.TrimAudioClip(micRecordingClip, lastPos);
                trimmed.name = $"Voice_{currentRecordingType}_{targetFighterId}_{currentHeadIndex}";

                // Guardar en el perfil y en disco
                if (activeProfile == null) RefreshActiveProfile();
                activeProfile.SaveVoice(currentRecordingType, trimmed, saveFolderPath);

                // Limpiar caché de VoiceLoader para que las peleas recojan el nuevo audio
                VoiceLoader.ClearCacheForFighter(targetFighterId, currentHeadIndex);

                // Escucha previa automática para confirmar
                PlayPreview(currentRecordingType);

                if (recordingStatusBadge != null)
                {
                    recordingStatusBadge.text = $"✅ ¡Voz de <b>{currentRecordingType}</b> guardada con éxito!";
                    recordingStatusBadge.color = new Color(0.2f, 1f, 0.4f);
                }
            }

            UpdateRecordButtonVisual(currentRecordingType, false);
            UpdateAllSlotVisuals();
        }

        public void PlayPreview(VoiceType type)
        {
            if (activeProfile == null) RefreshActiveProfile();

            AudioClip clip = activeProfile.GetVoice(type);
            if (clip != null)
            {
                if (FightAudioManager.Instance != null)
                {
                    FightAudioManager.Instance.PlayFighterVoice(clip, 1.0f, 1.0f);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, Vector3.zero);
                }
            }
            else
            {
                // Reproducir el grito procedural arcade por defecto
                if (FightAudioManager.Instance != null)
                {
                    FightAudioManager.Instance.PlayProceduralVoice(type, 1.0f, 1.0f);
                }
            }
        }

        public void DeleteVoice(VoiceType type)
        {
            if (activeProfile == null) RefreshActiveProfile();

            activeProfile.DeleteVoice(type, saveFolderPath);
            VoiceLoader.ClearCacheForFighter(targetFighterId, currentHeadIndex);

            UpdateAllSlotVisuals();

            if (recordingStatusBadge != null)
            {
                recordingStatusBadge.text = $"🗑️ Voz de {type} restablecida al grito arcade por defecto.";
                recordingStatusBadge.color = new Color(0.8f, 0.85f, 1f);
            }
        }

        public void UpdateAllSlotVisuals()
        {
            UpdateSlot(slotAttack, VoiceType.Attack, "1. 💥 GOLPE FUERTE / ATAQUE", "¡Toma! / ¡Muere!");
            UpdateSlot(slotSpecial, VoiceType.Special, "2. 🌀 HADOUKEN / ESPECIAL", "¡Hadouken! / ¡Fuego!");
            UpdateSlot(slotHurt, VoiceType.Hurt, "3. 😣 DOLOR / DAÑO", "¡Ayyy! / ¡Ouch!");
            UpdateSlot(slotVictory, VoiceType.Victory, "4. 👑 VICTORIA / CELEBRACIÓN", "¡Soy el mejor! / ¡Gané!");
            UpdateSlot(slotKO, VoiceType.KO, "5. 💀 DERROTA / K.O.", "¡Nooooo! / ¡Maldición!");
        }

        private void UpdateSlot(VoiceSlotUI slot, VoiceType type, string title, string hint)
        {
            if (slot == null) return;

            slot.type = type;
            if (slot.labelText != null)
            {
                slot.labelText.text = $"<b>{title}</b>\n<size=11><color=#99BBEE>Ejemplo: {hint}</color></size>";
            }

            bool hasCustom = activeProfile != null && activeProfile.HasCustomVoice(type);

            if (slot.statusText != null)
            {
                if (hasCustom)
                {
                    AudioClip c = activeProfile.GetVoice(type);
                    float dur = c != null ? c.length : 0f;
                    slot.statusText.text = $"<color=#33FF66>● Grabado ({dur:F1}s)</color>";
                }
                else
                {
                    slot.statusText.text = "<color=#AAAAAA>○ Arcade Default</color>";
                }
            }

            if (slot.deleteButton != null)
            {
                slot.deleteButton.interactable = hasCustom;
            }

            UpdateRecordButtonVisual(type, isRecording && currentRecordingType == type);
        }

        private void UpdateRecordButtonVisual(VoiceType type, bool isThisRecording)
        {
            VoiceSlotUI slot = GetSlot(type);
            if (slot == null) return;

            if (slot.recordButtonBg != null)
            {
                slot.recordButtonBg.color = isThisRecording ? new Color(1f, 0.2f, 0.2f) : new Color(0.85f, 0.25f, 0.2f);
            }

            if (slot.recordButtonText != null)
            {
                slot.recordButtonText.text = isThisRecording ? "⏹️ DETENER" : "🔴 GRABAR";
            }
        }

        private VoiceSlotUI GetSlot(VoiceType type)
        {
            switch (type)
            {
                case VoiceType.Attack: return slotAttack;
                case VoiceType.Special: return slotSpecial;
                case VoiceType.Hurt: return slotHurt;
                case VoiceType.Victory: return slotVictory;
                case VoiceType.KO: return slotKO;
                default: return null;
            }
        }

        private string GetFighterDefaultName(int id)
        {
            string[] names = {
                "El Gordo",
                "El Flaco",
                "El Musculoso",
                "La Mujer",
                "La Guerrera",
                "Hombre-Máquina"
            };
            int idx = Mathf.Clamp(id - 1, 0, names.Length - 1);
            return names[idx];
        }
    }
}
