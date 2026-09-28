using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Gestiona la captura de rostros en tiempo real mediante la Webcam,
    /// el guardado automático de las 4 fotos en disco (Base, Enojo, Dolor, KO)
    /// y la asignación directa al luchador seleccionado.
    /// </summary>
    public class WebcamCaptureManager : MonoBehaviour
    {
        public static WebcamCaptureManager Instance { get; private set; }

        [Header("UI de Previsualización")]
        public RawImage cameraPreviewUI;
        public AspectRatioFitter previewAspectRatio;

        [Header("Miniaturas de las 4 Caras Capturadas")]
        public Image previewThumbBase;
        public Image previewThumbAngry;
        public Image previewThumbHurt;
        public Image previewThumbKO;

        [Header("Luchador Objetivo")]
        public int targetPlayerId = 1; // 1 = Jugador 1, 2 = Jugador 2

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
            UpdateSaveFolderPath();
            // Intentar cargar fotos existentes si ya fueron capturadas previamente
            if (Directory.Exists(saveFolderPath))
            {
                activeProfile.LoadFromDirectory(saveFolderPath);
                UpdateThumbnailPreviews();
            }
        }

        public void SetTargetPlayer(int playerId)
        {
            targetPlayerId = playerId;
            UpdateSaveFolderPath();

            // Cargar perfil del jugador objetivo
            activeProfile = new FaceProfile();
            if (Directory.Exists(saveFolderPath))
            {
                activeProfile.LoadFromDirectory(saveFolderPath);
            }
            else
            {
                // Cargar perfil cómico por defecto
                activeProfile = FaceLoader.CreateDefaultProceduralProfile(targetPlayerId);
            }
            UpdateThumbnailPreviews();
        }

        private void UpdateSaveFolderPath()
        {
            saveFolderPath = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{targetPlayerId}");
        }

        /// <summary>
        /// Inicia el feed en vivo de la cámara web.
        /// </summary>
        public void StartWebcam()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("[WebcamCapture] No se detectó ninguna cámara web conectada al equipo.");
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

            Debug.Log($"[WebcamCapture] Cámara iniciada: {webcamTexture.deviceName}");
        }

        /// <summary>
        /// Detiene la cámara web para liberar recursos.
        /// </summary>
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
        /// Captura el fotograma actual de la webcam para una de las 4 emociones:
        /// Base, Enojo, Dolor o KO.
        /// </summary>
        public void CaptureCurrentFrameAs(FaceType emotion)
        {
            Texture2D snapshot = null;

            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                // Capturar desde webcam
                snapshot = CaptureSquareFromWebcam(webcamTexture);
            }
            else
            {
                // Si la webcam no está activa, generar o usar textura cómica para permitir pruebas
                Debug.LogWarning("[WebcamCapture] Webcam inactiva. Generando fotograma de prueba.");
                snapshot = FaceLoader.CreateDefaultProceduralProfile(targetPlayerId).GetSprite(emotion).texture;
            }

            if (snapshot == null) return;

            Sprite newSprite = FaceLoader.CreateSpriteFromTexture(snapshot);
            activeProfile.SetSprite(emotion, newSprite);

            // Guardar automáticamente la foto en el disco
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }

            string filename = GetFilenameForEmotion(emotion);
            string fullPath = Path.Combine(saveFolderPath, filename);
            FaceLoader.SaveTextureToFile(snapshot, fullPath);

            Debug.Log($"[WebcamCapture] ¡Foto {emotion} capturada y guardada en {fullPath}!");

            UpdateThumbnailPreviews();
            ApplyToTargetFighter();
        }

        private string GetFilenameForEmotion(FaceType emotion)
        {
            switch (emotion)
            {
                case FaceType.Base: return "Foto_Base.png";
                case FaceType.Enojo: return "Foto_Enojo.png"; // ¡4ta cara!
                case FaceType.Dolor: return "Foto_Dolor.png";
                case FaceType.KO: return "Foto_KO.png";
                default: return "Foto_Base.png";
            }
        }

        /// <summary>
        /// Extrae un recorte cuadrado centrado de la cámara web para enfocar el rostro.
        /// </summary>
        private Texture2D CaptureSquareFromWebcam(WebCamTexture cam)
        {
            int w = cam.width;
            int h = cam.height;
            int size = Mathf.Min(w, h);
            int xOffset = (w - size) / 2;
            int yOffset = (h - size) / 2;

            Texture2D rawFrame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = cam.GetPixels32();
            rawFrame.SetPixels32(pixels);
            rawFrame.Apply();

            // Extraer el cuadrado
            Color[] squarePixels = rawFrame.GetPixels(xOffset, yOffset, size, size);
            Texture2D squareTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            squareTex.SetPixels(squarePixels);
            squareTex.Apply();

            Destroy(rawFrame);
            return squareTex;
        }

        /// <summary>
        /// Aplica el perfil facial capturado al luchador activo en el combate.
        /// </summary>
        public void ApplyToTargetFighter()
        {
            if (BattleManager.Instance != null)
            {
                FighterController fighter = targetPlayerId == 1 ? BattleManager.Instance.player1 : BattleManager.Instance.player2;
                if (fighter != null && fighter.faceController != null)
                {
                    fighter.faceController.SetProfile(activeProfile);
                }
            }
        }

        /// <summary>
        /// Actualiza los recuadros de preview en la UI del creador.
        /// </summary>
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
