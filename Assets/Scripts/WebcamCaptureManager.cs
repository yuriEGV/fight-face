using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Gestiona la captura de rostros con Webcam:
    /// - Soporte para Jugador 1, Jugador 2 y todos los luchadores del Campeonato (1 a 8).
    /// - Recorte automático ovalado con borde cómic (Sticker).
    /// - Corrección de orientación (Invertir verticalmente / Espejo horizontal).
    /// - Persistencia de las 4 fotos (Base, Enojo, Dolor, KO).
    /// </summary>
    public class WebcamCaptureManager : MonoBehaviour
    {
        public static WebcamCaptureManager Instance { get; private set; }

        [Header("UI de Previsualización")]
        public RawImage cameraPreviewUI;

        [Header("Miniaturas de las 4 Caras")]
        public Image previewThumbBase;
        public Image previewThumbAngry;
        public Image previewThumbHurt;
        public Image previewThumbKO;

        [Header("Luchador Activo")]
        public int targetFighterId = 1;
        public Text targetFighterLabel;

        [Header("Orientación y Zoom")]
        public bool flipVertical = true; // Por defecto TRUE en Windows DirectX para que las fotos queden derechas
        public bool flipHorizontal = false;
        public float faceZoom = 1.45f; // Zoom por defecto para encuadrar directamente el rostro sin espacios en blanco
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

            UpdateThumbnailPreviews();
            UpdateFlipStatusText();
        }

        public string GetFighterDefaultName(int id)
        {
            string[] names = {
                "Panchito 'El Bravo'",
                "Rocky 'El Furioso'",
                "Don Ramón 'El Pájaro'",
                "La Máscara 'El Titán'",
                "La Furia 'Relámpago'",
                "Míster K.O.",
                "El Fantasma",
                "El Jefe Final"
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
            Debug.Log($"[WebcamCapture] Zoom: {faceZoom:F2}x");
        }

        public void ZoomOut()
        {
            faceZoom = Mathf.Max(1.0f, faceZoom - 0.15f);
            UpdateFlipStatusText();
            Debug.Log($"[WebcamCapture] Zoom: {faceZoom:F2}x");
        }

        private void UpdateFlipStatusText()
        {
            if (flipStatusText != null)
            {
                flipStatusText.text = $"Giro: {(flipVertical ? "180°" : "0°")} | Espejo: {(flipHorizontal ? "ON" : "OFF")} | Zoom: {faceZoom:F1}x";
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

        /// <summary>
        /// Captura el fotograma actual de la webcam para una emoción y lo recorta como cabeza ovalada cómica.
        /// </summary>
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

            string altDir = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}");
            if (!Directory.Exists(altDir)) Directory.CreateDirectory(altDir);
            FaceLoader.SaveTextureToFile(snapshot, Path.Combine(altDir, filename));

            Debug.Log($"[WebcamCapture] Foto {emotion} capturada y guardada en {fullPath}");

            UpdateThumbnailPreviews();
            ApplyToActiveFighters();
        }

        private string GetFilenameForEmotion(FaceType emotion)
        {
            switch (emotion)
            {
                case FaceType.Base: return "Foto_Base.png";
                case FaceType.Enojo: return "Foto_Enojo.png";
                case FaceType.Dolor: return "Foto_Dolor.png";
                case FaceType.KO: return "Foto_KO.png";
                default: return "Foto_Base.png";
            }
        }

        /// <summary>
        /// Captura desde la webcam y aplica el recorte ovalado con borde blanco para sticker.
        /// </summary>
        private Texture2D CaptureStickerFromWebcam(WebCamTexture cam)
        {
            int w = cam.width;
            int h = cam.height;

            Texture2D rawFrame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = cam.GetPixels32();
            rawFrame.SetPixels32(pixels);
            rawFrame.Apply();

            // Detectar si la webcam está invertida por hardware
            bool shouldFlipY = flipVertical ^ cam.videoVerticallyMirrored;

            // Recortar en forma de óvalo con zoom cerrado y borde sticker limpio
            Texture2D sticker = FaceLoader.MaskAsOvalHead(rawFrame, shouldFlipY, flipHorizontal, faceZoom);

            Destroy(rawFrame);
            return sticker;
        }

        /// <summary>
        /// Aplica el perfil facial capturado al luchador activo si está en combate.
        /// </summary>
        public void ApplyToActiveFighters()
        {
            if (BattleManager.Instance != null)
            {
                if (BattleManager.Instance.player1 != null && BattleManager.Instance.player1.playerId == targetFighterId)
                {
                    BattleManager.Instance.player1.faceController.SetProfile(activeProfile);
                }
                else if (BattleManager.Instance.player2 != null && BattleManager.Instance.player2.playerId == targetFighterId)
                {
                    BattleManager.Instance.player2.faceController.SetProfile(activeProfile);
                }
            }
        }

        public void UpdateThumbnailPreviews()
        {
            if (previewThumbBase != null && activeProfile.faceBase != null)
                previewThumbBase.sprite = activeProfile.faceBase;

            if (previewThumbAngry != null && activeProfile.faceAngry != null)
                previewThumbAngry.sprite = activeProfile.faceAngry;

            if (previewThumbHurt != null && activeProfile.faceHurt != null)
                previewThumbHurt.sprite = activeProfile.faceHurt;

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
