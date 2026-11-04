using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

        [Header("Miniaturas de las 5 Caras")]
        public Image previewThumbBase;     // 1. Normal
        public Image previewThumbHurt;     // 2. Dolor
        public Image previewThumbAngry;    // 3. Rabia
        public Image previewThumbWinner;   // 4. Ganador
        public Image previewThumbKO;       // 5. Perdedor (K.O.)

        [Header("Luchador Activo y Nombre")]
        public int targetFighterId = 1;
        public int currentHeadIndex = 0;   // 0 = Cabeza 1 (Izquierda), 1 = Cabeza 2 (Derecha)
        public Text targetFighterLabel;
        public InputField fighterNameInput;

        [Header("Selector de Doble Cabeza (Hombre-Máquina / Dos Cabezas)")]
        public GameObject headSelectorRow;
        public Button head1ToggleButton;
        public Button head2ToggleButton;
        public Text head1ButtonText;
        public Text head2ButtonText;

        [Header("Guía Visual")]
        public Text guideTitleText;
        public Text guideDescriptionText;

        [Header("Orientación y Zoom")]
        public bool flipVertical = false; // Por defecto FALSE en Windows para que las fotos queden perfectamente derechas
        public bool flipHorizontal = false;
        public float faceZoom = 1.0f;     // Escala de encuadre en el visor
        public Text flipStatusText;

        [Header("Manipulación y Encuadre de Foto")]
        public float panX = 0f;
        public float panY = 0f;
        public Texture2D activeRawTexture;
        public string activeRawFilePath;
        public FaceType currentEditingEmotion = FaceType.Base;

        private WebCamTexture webcamTexture;
        private FaceProfile activeProfile;
        private string saveFolderPath;

        public FaceProfile CurrentProfile => activeProfile;
        public FaceProfile ActiveProfile => activeProfile;
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

            if (head1ToggleButton != null)
            {
                head1ToggleButton.onClick.AddListener(() => SetHeadIndex(0));
            }

            if (head2ToggleButton != null)
            {
                head2ToggleButton.onClick.AddListener(() => SetHeadIndex(1));
            }
        }

        public void SetHeadIndex(int index)
        {
            currentHeadIndex = index;
            UpdateSaveFolderPath();
            RefreshActiveProfile();
            UpdateHeadToggleVisuals();
            UpdateThumbnailPreviews();
            ApplyToActiveFighters();

            if (VoiceCaptureManager.Instance != null)
            {
                VoiceCaptureManager.Instance.SetTargetFighter(targetFighterId, currentHeadIndex);
            }
        }

        public void SetTargetFighter(int fighterId)
        {
            targetFighterId = fighterId;
            if (targetFighterId != 6)
            {
                currentHeadIndex = 0;
            }

            if (headSelectorRow != null)
            {
                headSelectorRow.SetActive(targetFighterId == 6);
            }

            UpdateSaveFolderPath();
            RefreshActiveProfile();
            UpdateHeadToggleVisuals();

            SetGuideForEmotion(FaceType.Base);
            UpdateThumbnailPreviews();
            UpdateFlipStatusText();

            if (VoiceCaptureManager.Instance != null)
            {
                VoiceCaptureManager.Instance.SetTargetFighter(targetFighterId, currentHeadIndex);
            }
        }

        public void RefreshActiveProfile()
        {
            activeProfile = new FaceProfile();
            activeProfile.fighterId = targetFighterId;
            activeProfile.fighterName = GetFighterDefaultName(targetFighterId);

            string suffix = currentHeadIndex > 0 ? $"_Head{currentHeadIndex + 1}" : "";
            string dirJugador = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{targetFighterId}{suffix}");
            string dirLuchador = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}{suffix}");

            if (Directory.Exists(dirJugador) && activeProfile.LoadFromDirectory(dirJugador))
            {
                Debug.Log($"[WebcamCapture] Fotos cargadas desde Jugador_{targetFighterId}{suffix}");
            }
            else if (Directory.Exists(dirLuchador) && activeProfile.LoadFromDirectory(dirLuchador))
            {
                Debug.Log($"[WebcamCapture] Fotos cargadas desde Luchador_{targetFighterId}{suffix}");
            }
            else
            {
                activeProfile = FaceLoader.CreateDefaultProceduralProfile(targetFighterId + (currentHeadIndex * 10));
                activeProfile.fighterName = GetFighterDefaultName(targetFighterId);
            }

            if (targetFighterLabel != null)
            {
                string headTag = (targetFighterId == 6) ? $" - <b>[CABEZA {currentHeadIndex + 1}]</b>" : "";
                targetFighterLabel.text = $"Editando: <b>Luchador {targetFighterId} ({activeProfile.fighterName})</b>{headTag}";
            }
            if (fighterNameInput != null)
            {
                fighterNameInput.text = activeProfile.fighterName;
            }

            // Si la cámara no está activa, mostrar la foto normal en el visor principal
            if (!IsCameraRunning && cameraPreviewUI != null && activeProfile != null)
            {
                Sprite baseSp = activeProfile.GetSprite(FaceType.Base);
                if (baseSp != null && baseSp.texture != null)
                {
                    cameraPreviewUI.texture = baseSp.texture;
                    cameraPreviewUI.enabled = true;
                }
            }
        }

        private void UpdateHeadToggleVisuals()
        {
            if (head1ToggleButton != null)
            {
                var img = head1ToggleButton.GetComponent<Image>();
                if (img != null) img.color = (currentHeadIndex == 0) ? new Color(0.2f, 0.85f, 0.4f) : new Color(0.25f, 0.28f, 0.35f);
            }
            if (head2ToggleButton != null)
            {
                var img = head2ToggleButton.GetComponent<Image>();
                if (img != null) img.color = (currentHeadIndex == 1) ? new Color(0.2f, 0.85f, 0.4f) : new Color(0.25f, 0.28f, 0.35f);
            }
        }

        public void OnFighterNameChanged(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;
            if (activeProfile != null)
            {
                activeProfile.fighterName = newName;
            }
            FaceLoader.SaveFighterName(targetFighterId, newName);
            if (TournamentManager.Instance != null)
            {
                var td = TournamentManager.Instance.GetFighterData(targetFighterId);
                if (td != null) td.name = newName;
            }
            if (targetFighterLabel != null)
            {
                string headTag = (targetFighterId == 6) ? $" - <b>[CABEZA {currentHeadIndex + 1}]</b>" : "";
                targetFighterLabel.text = $"Editando: <b>Luchador {targetFighterId} ({newName})</b>{headTag}";
            }
            if (CharacterSelectUI.Instance != null)
            {
                CharacterSelectUI.Instance.RefreshP1UI();
                CharacterSelectUI.Instance.RefreshP2UI();
            }
        }

        public string GetFighterDefaultName(int id)
        {
            FighterBodyType b = (FighterBodyType)Mathf.Clamp(id - 1, 0, 5);
            return FaceLoader.GetFighterDisplayName(b);
        }

        public void UploadPhotoFromFile(FaceType emotion)
        {
            string chosenFile = PhotoFileImporter.PromptSelectImageFile($"Selecciona una foto para {emotion} (Luchador {targetFighterId})");
            if (!string.IsNullOrEmpty(chosenFile))
            {
                Sprite newSprite = PhotoFileImporter.ImportAndApplyPhoto(chosenFile, targetFighterId, emotion, currentHeadIndex, faceZoom);
                if (newSprite != null)
                {
                    if (activeProfile != null)
                    {
                        activeProfile.SetSprite(emotion, newSprite);
                    }
                    if (cameraPreviewUI != null && newSprite.texture != null)
                    {
                        cameraPreviewUI.texture = newSprite.texture;
                        cameraPreviewUI.enabled = true;
                    }
                    UpdateThumbnailPreviews();
                    ApplyToActiveFighters();

                    if (guideTitleText != null)
                    {
                        string headTag = (targetFighterId == 6) ? $" [CABEZA {currentHeadIndex + 1}]" : "";
                        guideTitleText.text = $"✅ ¡FOTO {emotion.ToString().ToUpper()}{headTag} ADJUNTADA CON ÉXITO!";
                    }
                }
                else
                {
                    if (guideTitleText != null)
                    {
                        guideTitleText.text = $"⚠️ NO SE PUDO CARGAR LA IMAGEN SELECCIONADA";
                    }
                }
            }
        }

        public void UploadBodyPhotoFromFile()
        {
            string chosenFile = PhotoFileImporter.PromptSelectImageFile($"Selecciona foto de CUERPO ENTERO para Luchador {targetFighterId}");
            if (!string.IsNullOrEmpty(chosenFile))
            {
                Sprite bodySprite = PhotoFileImporter.ImportAndApplyBodyPhoto(chosenFile, targetFighterId);
                if (bodySprite != null)
                {
                    if (guideTitleText != null)
                    {
                        guideTitleText.text = $"✅ ¡FOTO DE CUERPO ADJUNTADA CON ÉXITO!";
                    }
                }
                else
                {
                    if (guideTitleText != null)
                    {
                        guideTitleText.text = $"⚠️ NO SE PUDO CARGAR LA FOTO DE CUERPO";
                    }
                }
            }
        }

        public void OpenFighterFolder()
        {
            PhotoFileImporter.OpenFighterPhotosInExplorer(targetFighterId, currentHeadIndex);
        }

        private void UpdateSaveFolderPath()
        {
            string suffix = currentHeadIndex > 0 ? $"_Head{currentHeadIndex + 1}" : "";
            saveFolderPath = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{targetFighterId}{suffix}");
        }

        public void ToggleFlipVertical()
        {
            flipVertical = !flipVertical;
            ApplyTransformToPreview();
            RecropAndApply();
            Debug.Log($"[WebcamCapture] Invertir Vertical: {flipVertical}");
        }

        public void ToggleFlipHorizontal()
        {
            flipHorizontal = !flipHorizontal;
            ApplyTransformToPreview();
            RecropAndApply();
            Debug.Log($"[WebcamCapture] Espejo Horizontal: {flipHorizontal}");
        }

        public void ZoomIn()
        {
            faceZoom = Mathf.Min(4.5f, faceZoom + 0.15f);
            ApplyTransformToPreview();
            RecropAndApply();
        }

        public void ZoomOut()
        {
            faceZoom = Mathf.Max(0.5f, faceZoom - 0.15f);
            ApplyTransformToPreview();
            RecropAndApply();
        }

        private void UpdateFlipStatusText()
        {
            if (flipStatusText != null)
            {
                flipStatusText.text = $"Giro: {(flipVertical ? "180°" : "0°")} | Espejo: {(flipHorizontal ? "ON" : "OFF")} | Zoom: {faceZoom:F1}x | Pos: ({panX:+0.00;-0.00}, {panY:+0.00;-0.00})";
            }
        }

        public void SetActiveRawTexture(Texture2D rawTex, FaceType emotion, string filePath = "")
        {
            activeRawTexture = rawTex;
            currentEditingEmotion = emotion;
            activeRawFilePath = filePath;
            panX = 0f;
            panY = 0f;
            faceZoom = 1.0f;
            if (cameraPreviewUI != null && rawTex != null)
            {
                cameraPreviewUI.texture = rawTex;
                cameraPreviewUI.enabled = true;
            }
            ApplyTransformToPreview();
            UpdateFlipStatusText();
        }

        public void ApplyTransformToPreview()
        {
            if (cameraPreviewUI != null)
            {
                float uvW = 1.0f / Mathf.Max(0.2f, faceZoom);
                float uvH = 1.0f / Mathf.Max(0.2f, faceZoom);
                float uvX = (0.5f - uvW * 0.5f) + panX;
                float uvY = (0.535f - uvH * 0.5f) + panY;

                if (flipHorizontal)
                {
                    uvX += uvW;
                    uvW = -uvW;
                }
                if (flipVertical)
                {
                    uvY += uvH;
                    uvH = -uvH;
                }

                cameraPreviewUI.uvRect = new Rect(uvX, uvY, uvW, uvH);
            }
            UpdateFlipStatusText();
        }

        public void PanLeft(float amount = 0.04f)
        {
            panX += amount;
            ApplyTransformToPreview();
            RecropAndApply();
        }

        public void PanRight(float amount = 0.04f)
        {
            panX -= amount;
            ApplyTransformToPreview();
            RecropAndApply();
        }

        public void PanUp(float amount = 0.04f)
        {
            panY -= amount;
            ApplyTransformToPreview();
            RecropAndApply();
        }

        public void PanDown(float amount = 0.04f)
        {
            panY += amount;
            ApplyTransformToPreview();
            RecropAndApply();
        }

        public void ResetPanAndZoom()
        {
            panX = 0f;
            panY = 0f;
            faceZoom = 1.0f;
            flipHorizontal = false;
            flipVertical = false;
            ApplyTransformToPreview();
            RecropAndApply();
        }

        public void OnPanDelta(Vector2 delta)
        {
            panX += delta.x;
            panY += delta.y;
            ApplyTransformToPreview();
        }

        public void RecropAndApply()
        {
            string filename = GetFilenameForEmotion(currentEditingEmotion);
            if (activeRawTexture == null)
            {
                string rawPath = Path.Combine(saveFolderPath, $"Raw_{filename}");
                if (!File.Exists(rawPath))
                {
                    string suffix = currentHeadIndex > 0 ? $"_Head{currentHeadIndex + 1}" : "";
                    rawPath = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}{suffix}", $"Raw_{filename}");
                }

                if (File.Exists(rawPath))
                {
                    byte[] bytes = File.ReadAllBytes(rawPath);
                    activeRawTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    activeRawTexture.LoadImage(bytes);
                }
            }

            if (activeRawTexture == null) return;

            Texture2D cropped = FaceLoader.MaskAsOvalHead(activeRawTexture, flipVertical, flipHorizontal, faceZoom, panX, panY);
            if (cropped == null) return;

            Sprite newSprite = FaceLoader.CreateSpriteFromTexture(cropped);
            if (activeProfile != null)
            {
                activeProfile.SetSprite(currentEditingEmotion, newSprite);
            }

            if (!Directory.Exists(saveFolderPath)) Directory.CreateDirectory(saveFolderPath);
            string fullPath = Path.Combine(saveFolderPath, filename);
            FaceLoader.SaveTextureToFile(cropped, fullPath);

            if (currentEditingEmotion == FaceType.Enojo)
            {
                FaceLoader.SaveTextureToFile(cropped, Path.Combine(saveFolderPath, "Foto_Rabia.png"));
                FaceLoader.SaveTextureToFile(cropped, Path.Combine(saveFolderPath, "Foto_Enojo.png"));
            }
            else if (currentEditingEmotion == FaceType.Ganador)
            {
                FaceLoader.SaveTextureToFile(cropped, Path.Combine(saveFolderPath, "Foto_Ganador.png"));
            }
            else if (currentEditingEmotion == FaceType.KO)
            {
                FaceLoader.SaveTextureToFile(cropped, Path.Combine(saveFolderPath, "Foto_KO.png"));
                FaceLoader.SaveTextureToFile(cropped, Path.Combine(saveFolderPath, "Foto_Perdedor.png"));
            }

            string headSuffix = currentHeadIndex > 0 ? $"_Head{currentHeadIndex + 1}" : "";
            string altDir = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}{headSuffix}");
            if (!Directory.Exists(altDir)) Directory.CreateDirectory(altDir);
            FaceLoader.SaveTextureToFile(cropped, Path.Combine(altDir, filename));
            if (currentEditingEmotion == FaceType.KO) FaceLoader.SaveTextureToFile(cropped, Path.Combine(altDir, "Foto_Perdedor.png"));

            UpdateThumbnailPreviews();
            ApplyToActiveFighters();

            if (CharacterSelectUI.Instance != null)
            {
                CharacterSelectUI.Instance.RefreshP1UI();
                CharacterSelectUI.Instance.RefreshP2UI();
            }
        }

        public void SetGuideForEmotion(FaceType emotion)
        {
            currentEditingEmotion = emotion;
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
                    guideTitleText.text = "4. 📷 FOTO GANADOR (VICTORIA)";
                    guideDescriptionText.text = "Gran sonrisa o celebración triunfal • Actitud de campeón para la pantalla de victoria";
                    break;
                case FaceType.KO:
                    guideTitleText.text = "5. 📷 FOTO PERDEDOR (K.O. / DERROTA)";
                    guideDescriptionText.text = "Expresión de derrota o noqueado • Ojos cerrados o mareados • Boca abierta de K.O.";
                    break;
            }

            // Si la cámara no está activa, cargar la foto raw o la foto de la emoción seleccionada en el visor
            if (!IsCameraRunning && cameraPreviewUI != null)
            {
                string filename = GetFilenameForEmotion(emotion);
                string rawPath = Path.Combine(saveFolderPath, $"Raw_{filename}");
                if (!File.Exists(rawPath))
                {
                    string suffix = currentHeadIndex > 0 ? $"_Head{currentHeadIndex + 1}" : "";
                    rawPath = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}{suffix}", $"Raw_{filename}");
                }

                if (File.Exists(rawPath))
                {
                    byte[] bytes = File.ReadAllBytes(rawPath);
                    activeRawTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    activeRawTexture.LoadImage(bytes);
                    cameraPreviewUI.texture = activeRawTexture;
                    cameraPreviewUI.enabled = true;
                    ApplyTransformToPreview();
                }
                else if (activeProfile != null)
                {
                    Sprite emSp = activeProfile.GetSprite(emotion);
                    if (emSp != null && emSp.texture != null)
                    {
                        activeRawTexture = null;
                        cameraPreviewUI.texture = emSp.texture;
                        cameraPreviewUI.uvRect = new Rect(0, 0, 1, 1);
                        cameraPreviewUI.enabled = true;
                    }
                }
            }
        }

        public void StartWebcam()
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }
#endif

            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("[WebcamCapture] No se detectó ninguna cámara web.");
                return;
            }

            if (webcamTexture == null)
            {
                WebCamDevice device = WebCamTexture.devices[0];
                for (int i = 0; i < WebCamTexture.devices.Length; i++)
                {
                    if (WebCamTexture.devices[i].isFrontFacing)
                    {
                        device = WebCamTexture.devices[i];
                        break;
                    }
                }
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

        // Métodos de captura directos para los 5 botones
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

        public void CaptureFotoPerdedor()
        {
            SetGuideForEmotion(FaceType.KO);
            CaptureCurrentFrameAs(FaceType.KO);
        }

        public void CaptureCurrentFrameAs(FaceType emotion)
        {
            currentEditingEmotion = emotion;
            Texture2D snapshot = null;

            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                snapshot = CaptureStickerFromWebcam(webcamTexture);
            }
            else if (activeRawTexture != null)
            {
                snapshot = FaceLoader.MaskAsOvalHead(activeRawTexture, flipVertical, flipHorizontal, faceZoom, panX, panY);
            }
            else
            {
                Debug.LogWarning("[WebcamCapture] Webcam inactiva y sin foto Raw. Generando fotograma de muestra.");
                snapshot = FaceLoader.CreateDefaultProceduralProfile(targetFighterId + (currentHeadIndex * 10)).GetSprite(emotion).texture;
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
            }
            else if (emotion == FaceType.KO)
            {
                FaceLoader.SaveTextureToFile(snapshot, Path.Combine(saveFolderPath, "Foto_KO.png"));
                FaceLoader.SaveTextureToFile(snapshot, Path.Combine(saveFolderPath, "Foto_Perdedor.png"));
            }

            string suffix = currentHeadIndex > 0 ? $"_Head{currentHeadIndex + 1}" : "";
            string altDir = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{targetFighterId}{suffix}");
            if (!Directory.Exists(altDir)) Directory.CreateDirectory(altDir);
            FaceLoader.SaveTextureToFile(snapshot, Path.Combine(altDir, filename));
            if (emotion == FaceType.KO) FaceLoader.SaveTextureToFile(snapshot, Path.Combine(altDir, "Foto_Perdedor.png"));

            Debug.Log($"[WebcamCapture] Foto {emotion} guardada en: {fullPath}");

            if (guideTitleText != null)
            {
                string headTag = (targetFighterId == 6) ? $" [CABEZA {currentHeadIndex + 1}]" : "";
                guideTitleText.text = $"✅ ¡FOTO {emotion.ToString().ToUpper()}{headTag} GUARDADA!";
            }

            UpdateThumbnailPreviews();
            ApplyToActiveFighters();

            if (CharacterSelectUI.Instance != null)
            {
                CharacterSelectUI.Instance.RefreshP1UI();
                CharacterSelectUI.Instance.RefreshP2UI();
            }
        }

        private string GetFilenameForEmotion(FaceType emotion)
        {
            switch (emotion)
            {
                case FaceType.Base: return "Foto_Base.png";
                case FaceType.Dolor: return "Foto_Dolor.png";
                case FaceType.Enojo: return "Foto_Rabia.png";
                case FaceType.Ganador: return "Foto_Ganador.png";
                case FaceType.KO: return "Foto_Perdedor.png";
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

            Texture2D sticker = FaceLoader.MaskAsOvalHead(rawFrame, shouldFlipY, flipHorizontal, faceZoom, panX, panY);
            Destroy(rawFrame);
            return sticker;
        }

        public void ApplyToActiveFighters()
        {
            if (BattleManager.Instance != null)
            {
                bool isP1Match = (BattleManager.Instance.player1 != null) && 
                    ((int)BattleManager.Instance.p1BodyType + 1 == targetFighterId);
                
                if (isP1Match)
                {
                    if (targetFighterId == 6 && currentHeadIndex == 1)
                    {
                        if (BattleManager.Instance.player1.faceControllerRight != null)
                        {
                            BattleManager.Instance.player1.faceControllerRight.SetProfile(activeProfile);
                        }
                    }
                    else
                    {
                        if (BattleManager.Instance.player1.faceController != null)
                        {
                            BattleManager.Instance.player1.faceController.SetProfile(activeProfile);
                        }
                        BattleManager.Instance.player1.fighterName = activeProfile.fighterName;
                    }
                }

                bool isP2Match = (BattleManager.Instance.player2 != null) && 
                    ((int)BattleManager.Instance.p2BodyType + 1 == targetFighterId);
                
                if (isP2Match)
                {
                    if (targetFighterId == 6 && currentHeadIndex == 1)
                    {
                        if (BattleManager.Instance.player2.faceControllerRight != null)
                        {
                            BattleManager.Instance.player2.faceControllerRight.SetProfile(activeProfile);
                        }
                    }
                    else
                    {
                        if (BattleManager.Instance.player2.faceController != null)
                        {
                            BattleManager.Instance.player2.faceController.SetProfile(activeProfile);
                        }
                        BattleManager.Instance.player2.fighterName = activeProfile.fighterName;
                    }
                }
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.RefreshFacePortraits();
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
            {
                previewThumbBase.sprite = activeProfile.faceBase;
                previewThumbBase.color = Color.white;
            }

            if (previewThumbHurt != null && activeProfile.faceHurt != null)
            {
                previewThumbHurt.sprite = activeProfile.faceHurt;
                previewThumbHurt.color = Color.white;
            }

            if (previewThumbAngry != null && activeProfile.faceAngry != null)
            {
                previewThumbAngry.sprite = activeProfile.faceAngry;
                previewThumbAngry.color = Color.white;
            }

            Sprite winnerSprite = activeProfile.faceWinner != null ? activeProfile.faceWinner : activeProfile.GetSprite(FaceType.Ganador);
            if (previewThumbWinner != null && winnerSprite != null)
            {
                previewThumbWinner.sprite = winnerSprite;
                previewThumbWinner.color = Color.white;
            }

            Sprite koSprite = activeProfile.faceKO != null ? activeProfile.faceKO : activeProfile.GetSprite(FaceType.KO);
            if (previewThumbKO != null && koSprite != null)
            {
                previewThumbKO.sprite = koSprite;
                previewThumbKO.color = Color.white;
            }
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

    /// <summary>
    /// Componente interactivo que permite arrastrar la foto con el ratón en el visor
    /// y hacer zoom suave con la rueda del ratón (scroll wheel).
    /// </summary>
    public class PhotoDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public WebcamCaptureManager manager;

        public void OnDrag(PointerEventData eventData)
        {
            if (manager == null) return;
            var rect = GetComponent<RectTransform>();
            if (rect != null && rect.rect.width > 0 && rect.rect.height > 0)
            {
                // Arrastrar el ratón mueve el encuadre en tiempo real
                float dx = -eventData.delta.x / (rect.rect.width * Mathf.Max(0.5f, manager.faceZoom));
                float dy = -eventData.delta.y / (rect.rect.height * Mathf.Max(0.5f, manager.faceZoom));
                manager.OnPanDelta(new Vector2(dx, dy));
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (manager == null) return;
            manager.RecropAndApply();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (manager == null) return;
            if (eventData.scrollDelta.y > 0)
            {
                manager.ZoomIn();
            }
            else if (eventData.scrollDelta.y < 0)
            {
                manager.ZoomOut();
            }
        }
    }
}
