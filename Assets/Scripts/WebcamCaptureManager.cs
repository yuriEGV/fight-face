using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Gestiona la captura de rostros con Webcam según la estructura completa:
    /// 1. 📷 FOTO NORMAL: Mirando directamente a cámara, boca relajada.
    /// 2. 📷 FOTO DOLOR: Expresión de dolor, rostro ligeramente contraído (se activa al recibir daño fuerte / Stun).
    /// 3. 📷 FOTO RABIA: Expresión agresiva, ceño fruncido (se activa en estado RAGE y ataques especiales).
    /// 4. 📷 FOTO GANADOR: Sonrisa / celebración de victoria (se muestra en la pantalla de WINNER al noquear).
    /// </summary>
    public class WebcamCaptureManager : MonoBehaviour
    {
        public static WebcamCaptureManager Instance { get; private set; }

        [Header("UI de Previsualización")]
        public RawImage cameraPreviewUI;

        [Header("Miniaturas de las 4 Caras")]
        public Image previewThumbBase;     // 1. Normal
        public Image previewThumbHurt;     // 2. Dolor
        public Image previewThumbAngry;    // 3. Rabia
        public Image previewThumbWinner;   // 4. Ganador
        public Image previewThumbKO;       // KO (opcional o compatibilidad)

        [Header("Luchador Activo y Nombre")]
        public int targetFighterId = 1;
        public Text targetFighterLabel;
        public InputField fighterNameInput;

        [Header("Guía Visual")]
        public Text guideTitleText;
        public Text guideDescriptionText;

        [Header("Orientación y Zoom")]
        public bool flipVertical = false; // Por defecto FALSE en Windows para que las fotos queden perfectamente derechas
        public bool flipHorizontal = false;
        public float faceZoom = 1.90f;    // Zoom óptimo cerrado para encuadrar directamente el rostro sin paredes ni fondo
        public Text flipStatusText;

        private WebCamTexture webcamTexture;
        private FaceProfile activeProfile;
        private string saveFolderPath;

        public FaceProfile CurrentProfile => activeProfile;
        public bool IsCameraRunning => webcamTexture != null && webcamTexture.isPlaying;

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

            activeProfile = new FaceProfile();
            UpdateSaveFolderPath();
        }

        private void Start()
        {
            SetTargetFighter(1);

            if (fighterNameInput != null)
            {
                fighterNameInput.onValueChanged.AddListener(OnFighterNameChanged);
            }
        }

        public void SetTargetFighter(int fighterId)
        {
            targetFighterId = fighterId;
            UpdateSaveFolderPath();

            activeProfile = new FaceProfile();
            activeProfile.fighterId = targetFighterId;
            activeProfile.fighterName = GetFighterDefaultName(targetFighterId);

            string dirJugador = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{targetFighterId}");
            string dirLuchador = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}");

            if (Directory.Exists(dirJugador) && activeProfile.LoadFromDirectory(dirJugador))
            {
                Debug.Log($"[WebcamCapture] Fotos cargadas desde Jugador_{targetFighterId}");
            }
            else if (Directory.Exists(dirLuchador) && activeProfile.LoadFromDirectory(dirLuchador))
            {
                Debug.Log($"[WebcamCapture] Fotos cargadas desde Luchador_{targetFighterId}");
            }
            else
            {
                activeProfile = FaceLoader.CreateDefaultProceduralProfile(targetFighterId);
                activeProfile.fighterName = GetFighterDefaultName(targetFighterId);
            }

            if (targetFighterLabel != null)
            {
                targetFighterLabel.text = $"Editando: <b>Luchador {targetFighterId} ({activeProfile.fighterName})</b>";
            }
            if (fighterNameInput != null)
            {
                fighterNameInput.text = activeProfile.fighterName;
            }

            SetGuideForEmotion(FaceType.Base);
            UpdateThumbnailPreviews();
            UpdateFlipStatusText();
        }

        public void OnFighterNameChanged(string newName)
        {
            if (activeProfile != null && !string.IsNullOrEmpty(newName))
            {
                activeProfile.fighterName = newName;
                if (targetFighterLabel != null)
                {
                    targetFighterLabel.text = $"Editando: <b>Luchador {targetFighterId} ({newName})</b>";
                }
            }
        }

        public string GetFighterDefaultName(int id)
        {
            string[] names = {
                "El Gordo",
                "El Flaco",
                "El Musculoso",
                "La Mujer",
                "La Guerrera",
                "El Dos Cabezas"
            };
            int idx = Mathf.Clamp(id - 1, 0, names.Length - 1);
            return names[idx];
        }

        private void UpdateSaveFolderPath()
        {
            saveFolderPath = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{targetFighterId}");
        }

        public void ToggleFlipVertical()
        {
            flipVertical = !flipVertical;
            UpdateFlipStatusText();
            Debug.Log($"[WebcamCapture] Invertir Vertical: {flipVertical}");
        }

        public void ToggleFlipHorizontal()
        {
            flipHorizontal = !flipHorizontal;
            UpdateFlipStatusText();
            Debug.Log($"[WebcamCapture] Espejo Horizontal: {flipHorizontal}");
        }

        public void ZoomIn()
        {
            faceZoom = Mathf.Min(2.5f, faceZoom + 0.15f);
            UpdateFlipStatusText();
        }

        public void ZoomOut()
        {
            faceZoom = Mathf.Max(1.0f, faceZoom - 0.15f);
            UpdateFlipStatusText();
        }

        private void UpdateFlipStatusText()
        {
            if (flipStatusText != null)
            {
                flipStatusText.text = $"Giro: {(flipVertical ? "180°" : "0°")} | Espejo: {(flipHorizontal ? "ON" : "OFF")} | Zoom: {faceZoom:F1}x";
            }
        }

        public void SetGuideForEmotion(FaceType emotion)
        {
            if (guideTitleText == null || guideDescriptionText == null) return;

            switch (emotion)
            {
                case FaceType.Base:
                    guideTitleText.text = "1. 📷 FOTO NORMAL";
                    guideDescriptionText.text = "Mirando directamente a la cámara • Boca relajada • Rostro centrado en el círculo verde";
                    break;
                case FaceType.Dolor:
                    guideTitleText.text = "2. 📷 FOTO DOLOR";
                    guideDescriptionText.text = "Expresión de dolor o golpe • Rostro ligeramente contraído • Ojos apretados";
                    break;
                case FaceType.Enojo:
                    guideTitleText.text = "3. 📷 FOTO RABIA (RAGE / SUPER)";
                    guideDescriptionText.text = "Expresión agresiva • Ceño fruncido • Boca gritando o enseñando los dientes";
                    break;
                case FaceType.Ganador:
                case FaceType.KO:
                    guideTitleText.text = "4. 📷 FOTO GANADOR (VICTORIA)";
                    guideDescriptionText.text = "Gran sonrisa o celebración triunfal • Actitud de campeón para la pantalla de victoria";
                    break;
            }
        }

        public void StartWebcam()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("[WebcamCapture] No se detectó ninguna cámara web.");
                return;
            }

            if (webcamTexture == null)
            {
                WebCamDevice device = WebCamTexture.devices[0];
                webcamTexture = new WebCamTexture(device.name, 640, 480, 30);
            }

            if (!webcamTexture.isPlaying)
            {
                webcamTexture.Play();
            }

            if (cameraPreviewUI != null)
            {
                cameraPreviewUI.texture = webcamTexture;
                cameraPreviewUI.enabled = true;
            }
        }

        public void StopWebcam()
        {
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }

            if (cameraPreviewUI != null)
            {
                cameraPreviewUI.enabled = false;
            }
        }

        // Métodos de captura directos para los 4 botones
        public void CaptureFotoNormal()
        {
            SetGuideForEmotion(FaceType.Base);
            CaptureCurrentFrameAs(FaceType.Base);
        }

        public void CaptureFotoDolor()
        {
            SetGuideForEmotion(FaceType.Dolor);
            CaptureCurrentFrameAs(FaceType.Dolor);
        }

        public void CaptureFotoRabia()
        {
            SetGuideForEmotion(FaceType.Enojo);
            CaptureCurrentFrameAs(FaceType.Enojo);
        }

        public void CaptureFotoGanador()
        {
            SetGuideForEmotion(FaceType.Ganador);
            CaptureCurrentFrameAs(FaceType.Ganador);
        }

        public void CaptureCurrentFrameAs(FaceType emotion)
        {
            Texture2D snapshot = null;

            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                snapshot = CaptureStickerFromWebcam(webcamTexture);
            }
            else
            {
                Debug.LogWarning("[WebcamCapture] Webcam inactiva. Generando fotograma de muestra.");
                snapshot = FaceLoader.CreateDefaultProceduralProfile(targetFighterId).GetSprite(emotion).texture;
            }

            if (snapshot == null) return;

            Sprite newSprite = FaceLoader.CreateSpriteFromTexture(snapshot);
            activeProfile.SetSprite(emotion, newSprite);

            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }

            string filename = GetFilenameForEmotion(emotion);
            string fullPath = Path.Combine(saveFolderPath, filename);
            FaceLoader.SaveTextureToFile(snapshot, fullPath);

            // Guardar también con nombres alternativos de compatibilidad
            if (emotion == FaceType.Enojo)
            {
                FaceLoader.SaveTextureToFile(snapshot, Path.Combine(saveFolderPath, "Foto_Rabia.png"));
                FaceLoader.SaveTextureToFile(snapshot, Path.Combine(saveFolderPath, "Foto_Enojo.png"));
            }
            else if (emotion == FaceType.Ganador)
            {
                FaceLoader.SaveTextureToFile(snapshot, Path.Combine(saveFolderPath, "Foto_Ganador.png"));
                FaceLoader.SaveTextureToFile(snapshot, Path.Combine(saveFolderPath, "Foto_KO.png"));
            }

            string altDir = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}");
            if (!Directory.Exists(altDir)) Directory.CreateDirectory(altDir);
            FaceLoader.SaveTextureToFile(snapshot, Path.Combine(altDir, filename));

            Debug.Log($"[WebcamCapture] Foto {emotion} guardada erguida en: {fullPath}");

            UpdateThumbnailPreviews();
            ApplyToActiveFighters();
        }

        private string GetFilenameForEmotion(FaceType emotion)
        {
            switch (emotion)
            {
                case FaceType.Base: return "Foto_Base.png";
                case FaceType.Dolor: return "Foto_Dolor.png";
                case FaceType.Enojo: return "Foto_Rabia.png";
                case FaceType.Ganador: return "Foto_Ganador.png";
                case FaceType.KO: return "Foto_KO.png";
                default: return "Foto_Base.png";
            }
        }

        private Texture2D CaptureStickerFromWebcam(WebCamTexture cam)
        {
            int w = cam.width;
            int h = cam.height;

            Texture2D rawFrame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = cam.GetPixels32();
            rawFrame.SetPixels32(pixels);
            rawFrame.Apply();

            // En Windows, flipVertical = false garantiza fotos erguidas y derechas
            bool shouldFlipY = flipVertical;

            Texture2D sticker = FaceLoader.MaskAsOvalHead(rawFrame, shouldFlipY, flipHorizontal, faceZoom);
            Destroy(rawFrame);
            return sticker;
        }

        public void ApplyToActiveFighters()
        {
            if (BattleManager.Instance != null)
            {
                bool isP1Match = (BattleManager.Instance.player1 != null) && 
                    ((int)BattleManager.Instance.p1BodyType + 1 == targetFighterId || BattleManager.Instance.player1.playerId == targetFighterId);
                
                if (isP1Match)
                {
                    if (BattleManager.Instance.player1.faceController != null)
                    {
                        BattleManager.Instance.player1.faceController.SetProfile(activeProfile);
                    }
                    BattleManager.Instance.player1.fighterName = activeProfile.fighterName;
                }

                bool isP2Match = (BattleManager.Instance.player2 != null) && 
                    ((int)BattleManager.Instance.p2BodyType + 1 == targetFighterId || BattleManager.Instance.player2.playerId == targetFighterId);
                
                if (isP2Match)
                {
                    if (BattleManager.Instance.player2.faceController != null)
                    {
                        BattleManager.Instance.player2.faceController.SetProfile(activeProfile);
                    }
                    BattleManager.Instance.player2.fighterName = activeProfile.fighterName;
                }
            }

            if (CharacterSelectUI.Instance != null)
            {
                CharacterSelectUI.Instance.RefreshP1UI();
                CharacterSelectUI.Instance.RefreshP2UI();
            }
        }

        public void UpdateThumbnailPreviews()
        {
            if (previewThumbBase != null && activeProfile.faceBase != null)
                previewThumbBase.sprite = activeProfile.faceBase;

            if (previewThumbHurt != null && activeProfile.faceHurt != null)
                previewThumbHurt.sprite = activeProfile.faceHurt;

            if (previewThumbAngry != null && activeProfile.faceAngry != null)
                previewThumbAngry.sprite = activeProfile.faceAngry;

            Sprite winnerSprite = activeProfile.faceWinner != null ? activeProfile.faceWinner : activeProfile.faceKO;
            if (previewThumbWinner != null && winnerSprite != null)
                previewThumbWinner.sprite = winnerSprite;

            if (previewThumbKO != null && activeProfile.faceKO != null)
                previewThumbKO.sprite = activeProfile.faceKO;
        }

        private void OnDisable()
        {
            StopWebcam();
        }

        private void OnDestroy()
        {
            StopWebcam();
        }
    }
}
