using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Menú de Selección de Luchadores estilo Street Fighter II:
    /// - Cuadrícula arcade superior de selección de personajes con cursores 1P (Rojo) y 2P/CPU (Azul).
    /// - Navegación fluida por teclado (A/D para 1P, Flechas Izq/Der para 2P, Enter/Espacio para Pelear).
    /// - Panel de perfil izquierdo (1P) y derecho (2P) con tarjeta de arte completa, estadísticas (Fuerza, Velocidad, etc.)
    ///   y visor del rostro personalizado ovalado erguido.
    /// - Conexión directa a la webcam para personalizar rostros en cualquier momento.
    /// - Compatible al 100% con el Nuevo Input System de Unity 6 (cero excepciones de Input).
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        public static CharacterSelectUI Instance { get; private set; }

        public static readonly FighterBodyType[] Roster = {
            FighterBodyType.Gordo,
            FighterBodyType.Flaco,
            FighterBodyType.Musculoso,
            FighterBodyType.Mujer,
            FighterBodyType.Mujer2,
            FighterBodyType.DosCabezas,
            FighterBodyType.Playero,
            FighterBodyType.Oficinista,
            FighterBodyType.Pijamero
        };

        public enum GameMode
        {
            VsAI = 0,
            VsHuman = 1,
            Training = 2
        }

        [Header("Selecciones Activas")]
        public FighterBodyType p1BodyType = FighterBodyType.Musculoso;
        public FighterBodyType p2BodyType = FighterBodyType.Gordo;
        public bool p2IsAI = true;
        public GameMode currentGameMode = GameMode.VsAI;

        [Header("P1 UI")]
        public Image p1FighterCardImage;
        public Image p1HeadPreviewImage;
        public Text p1NameText;
        public InputField p1NameInputField;
        public Text p1DescText;
        public Text p1StatsText;
        public Button p1WebcamButton;
        public Button p1UploadFileButton;
        public Button[] p1BodyButtons;
        public Image[] p1FaceThumbs;        // [0]=Normal, [1]=Dolor, [2]=Rabia, [3]=Ganador, [4]=KO
        public Button[] p1FaceSlotButtons;

        [Header("P2 UI")]
        public Image p2FighterCardImage;
        public Image p2HeadPreviewImage;
        public Text p2NameText;
        public InputField p2NameInputField;
        public Text p2DescText;
        public Text p2StatsText;
        public Text p2AILabelText;
        public Button p2AIToggleButton;
        public Button p2ModeCpuButton;
        public Button p2ModeHumanButton;
        public Image p2ModeCpuBg;
        public Image p2ModeHumanBg;
        public Text p2ModeCpuText;
        public Text p2ModeHumanText;
        public Outline p2ModeCpuOutline;
        public Outline p2ModeHumanOutline;
        public Button p2WebcamButton;
        public Button p2UploadFileButton;
        public Button[] p2BodyButtons;
        public Image[] p2FaceThumbs;        // [0]=Normal, [1]=Dolor, [2]=Rabia, [3]=Ganador, [4]=KO
        public Button[] p2FaceSlotButtons;

        [Header("Modal de Confirmación Pre-Pelea")]
        public GameObject preFightModal;
        public bool hasDismissedPreFightPrompt = false;

        [Header("Roster Arcade Grid (Estilo Street Fighter II)")]
        public Button[] rosterSlotButtons;
        public Image[] rosterThumbImages;
        public GameObject[] rosterP1Badges;
        public GameObject[] rosterP2Badges;
        public Outline[] rosterOutlines;

        [Header("Botones Principales")]
        public Button fightButton;
        public Button trainingButton;
        public Button tournamentButton;

        [Header("Control de Música Opcional")]
        public Button musicToggleButton;
        public Text musicToggleText;
        public Image musicToggleImage;

        [Header("Control de Volumen de Voces")]
        public Button voiceVolumeButton;
        public Text voiceVolumeText;
        public Image voiceVolumeImage;

        [Header("Selector de Escenarios")]
        public StageType selectedStage = StageType.Auto;
        public Image stagePreviewImage;
        public Text stageNameText;
        public Text stageDescText;
        public Button stagePrevButton;
        public Button stageNextButton;

        private void Awake()
        {
            Instance = this;
        }

        private float lastMusicToggleTime = 0f;
        private float lastVoiceVolumeTime = 0f;

        public void OnToggleMusicClicked()
        {
            if (Time.unscaledTime - lastMusicToggleTime < 0.25f) return;
            lastMusicToggleTime = Time.unscaledTime;

            if (FightAudioManager.Instance != null)
            {
                FightAudioManager.Instance.ToggleMusic();
                UpdateMusicToggleVisuals();
            }
        }

        public void OnCycleVoiceVolumeClicked()
        {
            if (Time.unscaledTime - lastVoiceVolumeTime < 0.20f) return;
            lastVoiceVolumeTime = Time.unscaledTime;

            if (FightAudioManager.Instance != null)
            {
                FightAudioManager.Instance.CycleVoiceVolume();
                UpdateVoiceVolumeVisuals();
            }
        }

        public void UpdateMusicToggleVisuals()
        {
            bool isEnabled = FightAudioManager.Instance != null && FightAudioManager.Instance.isMusicEnabled;
            if (musicToggleText != null)
            {
                musicToggleText.text = isEnabled ? "🎵 MÚSICA [M]: SÍ" : "🔇 MÚSICA [M]: NO";
                musicToggleText.color = isEnabled ? Color.white : new Color(1f, 0.85f, 0.85f);
            }
            if (musicToggleImage != null)
            {
                musicToggleImage.color = isEnabled ? new Color(0.15f, 0.58f, 0.90f) : new Color(0.50f, 0.18f, 0.18f);
            }
        }

        public void UpdateVoiceVolumeVisuals()
        {
            if (FightAudioManager.Instance != null && voiceVolumeText != null)
            {
                voiceVolumeText.text = FightAudioManager.Instance.GetVoiceVolumeText();
                float mult = FightAudioManager.Instance.voiceVolumeMultiplier;
                if (voiceVolumeImage != null)
                {
                    if (mult >= 2.0f)
                    {
                        voiceVolumeImage.color = new Color(0.85f, 0.45f, 0.10f); // Naranja fuerte
                    }
                    else if (mult >= 1.5f)
                    {
                        voiceVolumeImage.color = new Color(0.20f, 0.65f, 0.35f); // Verde óptimo
                    }
                    else if (mult >= 1.0f)
                    {
                        voiceVolumeImage.color = new Color(0.18f, 0.52f, 0.75f); // Azul estándar
                    }
                    else
                    {
                        voiceVolumeImage.color = new Color(0.40f, 0.40f, 0.45f); // Tenue
                    }
                }
            }
        }

        private void Start()
        {
            RefreshP1UI();
            RefreshP2UI();
            RefreshRosterHighlights();
            RefreshStageUI();

            if (stagePrevButton != null)
            {
                stagePrevButton.onClick.AddListener(OnStagePrevClicked);
            }

            if (stageNextButton != null)
            {
                stageNextButton.onClick.AddListener(OnStageNextClicked);
            }

            if (fightButton != null)
            {
                fightButton.onClick.AddListener(OnStartFight);
            }

            if (tournamentButton != null)
            {
                tournamentButton.onClick.AddListener(OnStartTournament);
            }

            if (trainingButton != null)
            {
                trainingButton.onClick.AddListener(OnStartTrainingMode);
            }

            UpdateMusicToggleVisuals();
            UpdateVoiceVolumeVisuals();

            if (p2AIToggleButton != null)
            {
                p2AIToggleButton.onClick.AddListener(ToggleP2AI);
            }

            if (p2ModeCpuButton != null)
            {
                p2ModeCpuButton.onClick.AddListener(() => SetP2ControllerMode(true));
            }

            if (p2ModeHumanButton != null)
            {
                p2ModeHumanButton.onClick.AddListener(() => SetP2ControllerMode(false));
            }
            UpdateP2ControllerModeUI();

            if (p1WebcamButton != null)
            {
                p1WebcamButton.onClick.AddListener(() => OpenP1Webcam(FaceType.Base));
            }

            if (p2WebcamButton != null)
            {
                p2WebcamButton.onClick.AddListener(() => OpenP2Webcam(FaceType.Base));
            }

            if (p1UploadFileButton != null)
            {
                p1UploadFileButton.onClick.AddListener(OnP1UploadPhoto);
            }

            if (p2UploadFileButton != null)
            {
                p2UploadFileButton.onClick.AddListener(OnP2UploadPhoto);
            }

            if (p1NameInputField != null)
            {
                p1NameInputField.onValueChanged.AddListener(OnP1NameChanged);
            }

            if (p2NameInputField != null)
            {
                p2NameInputField.onValueChanged.AddListener(OnP2NameChanged);
            }

            // Enlazar botones de las 5 caras P1
            if (p1FaceSlotButtons != null)
            {
                FaceType[] types = { FaceType.Base, FaceType.Dolor, FaceType.Enojo, FaceType.Ganador, FaceType.KO };
                for (int i = 0; i < p1FaceSlotButtons.Length; i++)
                {
                    FaceType em = (i < types.Length) ? types[i] : FaceType.Base;
                    FaceType capturedEm = em;
                    p1FaceSlotButtons[i].onClick.AddListener(() => OpenP1Webcam(capturedEm));
                }
            }

            // Enlazar botones de las 5 caras P2
            if (p2FaceSlotButtons != null)
            {
                FaceType[] types = { FaceType.Base, FaceType.Dolor, FaceType.Enojo, FaceType.Ganador, FaceType.KO };
                for (int i = 0; i < p2FaceSlotButtons.Length; i++)
                {
                    FaceType em = (i < types.Length) ? types[i] : FaceType.Base;
                    FaceType capturedEm = em;
                    p2FaceSlotButtons[i].onClick.AddListener(() => OpenP2Webcam(capturedEm));
                }
            }

            // Enlazar clics de slots del roster
            if (rosterSlotButtons != null)
            {
                for (int i = 0; i < rosterSlotButtons.Length && i < Roster.Length; i++)
                {
                    int idx = i;
                    rosterSlotButtons[i].onClick.AddListener(() => OnRosterSlotClicked(idx));
                }
            }
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;

            // Confirmar y empezar pelea (Enter / Espacio)
            if (FightInput.GetConfirm())
            {
                OnStartFight();
                return;
            }

            // Atajo rápido: Tecla [F2] o [E] inicia Modo Entrenamiento
            if (FightInput.GetTrainingStart())
            {
                OnStartTrainingMode();
                return;
            }

            // Selector de Escenarios con [Arriba / Abajo] o [RePág / AvPág]
            if (FightInput.GetStagePrev())
            {
                OnStagePrevClicked();
            }
            else if (FightInput.GetStageNext())
            {
                OnStageNextClicked();
            }

            // Navegación P1 con A / D
            if (FightInput.GetP1SelectPrev())
            {
                CycleP1(-1);
            }
            else if (FightInput.GetP1SelectNext())
            {
                CycleP1(1);
            }

            // Navegación P2 con Flechas Izquierda / Derecha
            if (FightInput.GetP2SelectPrev())
            {
                CycleP2(-1);
            }
            else if (FightInput.GetP2SelectNext())
            {
                CycleP2(1);
            }

            // Atajos rápidos
            if (FightInput.GetTournamentStart())
            {
                OnStartTournament();
            }

            if (FightInput.GetToggleMusic())
            {
                OnToggleMusicClicked();
            }

            if (FightInput.GetToggleVoiceVolume())
            {
                OnCycleVoiceVolumeClicked();
            }

            if (FightInput.GetToggleCustomizer())
            {
                OpenP1Webcam();
            }
        }

        public void CycleP1(int delta)
        {
            int curIdx = Array.IndexOf(Roster, p1BodyType);
            if (curIdx < 0) curIdx = 0;
            curIdx = (curIdx + delta + Roster.Length) % Roster.Length;
            SelectP1Body(Roster[curIdx]);
        }

        public void CycleP2(int delta)
        {
            int curIdx = Array.IndexOf(Roster, p2BodyType);
            if (curIdx < 0) curIdx = 0;
            curIdx = (curIdx + delta + Roster.Length) % Roster.Length;
            SelectP2Body(Roster[curIdx]);
        }

        public void OnRosterSlotClicked(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < Roster.Length)
            {
                // Clic selecciona para P1 por defecto, o si ya tiene a este, alterna P2
                if (p1BodyType != Roster[slotIndex])
                {
                    SelectP1Body(Roster[slotIndex]);
                }
                else
                {
                    SelectP2Body(Roster[slotIndex]);
                }
            }
        }

        public void SelectP1Body(FighterBodyType body)
        {
            p1BodyType = body;
            int fId = (int)body + 1;
            FaceProfile profile = FaceLoader.LoadProfileForFighter(fId, FaceLoader.GetFighterDisplayName(body));
            if (BattleManager.Instance != null && BattleManager.Instance.player1 != null)
            {
                BattleManager.Instance.p1BodyType = body;
                BattleManager.Instance.player1.fighterName = FaceLoader.GetFighterDisplayName(body);
                if (BattleManager.Instance.player1.faceController != null)
                {
                    BattleManager.Instance.player1.faceController.SetProfile(profile);
                }
                if (BattleManager.Instance.player1.bodyController != null)
                {
                    BattleManager.Instance.player1.bodyController.SetClassicBody(body);
                }
            }
            RefreshP1UI();
            RefreshRosterHighlights();
            RefreshStageUI();
        }

        public void SelectP2Body(FighterBodyType body)
        {
            p2BodyType = body;
            int fId = (int)body + 1;
            FaceProfile profile = FaceLoader.LoadProfileForFighter(fId, FaceLoader.GetFighterDisplayName(body));
            if (BattleManager.Instance != null && BattleManager.Instance.player2 != null)
            {
                BattleManager.Instance.p2BodyType = body;
                BattleManager.Instance.player2.fighterName = (currentGameMode == GameMode.Training ? "DUMMY: " : "") + FaceLoader.GetFighterDisplayName(body);
                if (BattleManager.Instance.player2.faceController != null)
                {
                    BattleManager.Instance.player2.faceController.SetProfile(profile);
                }
                if (BattleManager.Instance.player2.bodyController != null)
                {
                    BattleManager.Instance.player2.bodyController.SetClassicBody(body);
                }
            }
            RefreshP2UI();
            RefreshRosterHighlights();
            RefreshStageUI();
        }

        public void SetP2ControllerMode(bool isAI)
        {
            p2IsAI = isAI;
            currentGameMode = isAI ? GameMode.VsAI : GameMode.VsHuman;
            UpdateP2ControllerModeUI();
            RefreshRosterHighlights();
            RefreshP2UI();
        }

        public void ToggleP2AI()
        {
            SetP2ControllerMode(!p2IsAI);
        }

        public void UpdateP2ControllerModeUI()
        {
            if (p2ModeCpuBg != null)
            {
                p2ModeCpuBg.color = p2IsAI ? new Color(0.12f, 0.52f, 0.95f, 1f) : new Color(0.11f, 0.15f, 0.23f, 0.90f);
            }
            if (p2ModeCpuOutline != null)
            {
                p2ModeCpuOutline.effectColor = p2IsAI ? new Color(1f, 0.88f, 0.2f, 1f) : new Color(0.25f, 0.35f, 0.50f, 0.5f);
                p2ModeCpuOutline.effectDistance = p2IsAI ? new Vector2(2f, 2f) : new Vector2(1f, 1f);
            }
            if (p2ModeCpuText != null)
            {
                p2ModeCpuText.color = p2IsAI ? Color.white : new Color(0.65f, 0.75f, 0.85f);
                p2ModeCpuText.fontStyle = p2IsAI ? FontStyle.Bold : FontStyle.Normal;
            }

            if (p2ModeHumanBg != null)
            {
                p2ModeHumanBg.color = !p2IsAI ? new Color(0.12f, 0.70f, 0.40f, 1f) : new Color(0.11f, 0.15f, 0.23f, 0.90f);
            }
            if (p2ModeHumanOutline != null)
            {
                p2ModeHumanOutline.effectColor = !p2IsAI ? new Color(1f, 0.88f, 0.2f, 1f) : new Color(0.25f, 0.35f, 0.50f, 0.5f);
                p2ModeHumanOutline.effectDistance = !p2IsAI ? new Vector2(2f, 2f) : new Vector2(1f, 1f);
            }
            if (p2ModeHumanText != null)
            {
                p2ModeHumanText.color = !p2IsAI ? Color.white : new Color(0.65f, 0.75f, 0.85f);
                p2ModeHumanText.fontStyle = !p2IsAI ? FontStyle.Bold : FontStyle.Normal;
            }

            if (p2AILabelText != null)
            {
                p2AILabelText.text = p2IsAI ? "🤖 MODO: CPU (IA)" : "👥 MODO: 2P HUMANO";
            }
        }

        public void OpenP1Webcam()
        {
            OpenP1Webcam(FaceType.Base);
        }

        public void OpenP1Webcam(FaceType emotion)
        {
            int fId = (int)p1BodyType + 1;
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.OpenFaceCustomizer(fId, emotion);
            }
            else if (WebcamCaptureManager.Instance != null)
            {
                WebcamCaptureManager.Instance.SetTargetFighter(fId);
                WebcamCaptureManager.Instance.SetGuideForEmotion(emotion);
                WebcamCaptureManager.Instance.StartWebcam();
            }
        }

        public void OpenP2Webcam()
        {
            OpenP2Webcam(FaceType.Base);
        }

        public void OpenP2Webcam(FaceType emotion)
        {
            int fId = (int)p2BodyType + 1;
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.OpenFaceCustomizer(fId, emotion);
            }
            else if (WebcamCaptureManager.Instance != null)
            {
                WebcamCaptureManager.Instance.SetTargetFighter(fId);
                WebcamCaptureManager.Instance.SetGuideForEmotion(emotion);
                WebcamCaptureManager.Instance.StartWebcam();
            }
        }

        public void OnStagePrevClicked()
        {
            var values = (StageType[])System.Enum.GetValues(typeof(StageType));
            int idx = System.Array.IndexOf(values, selectedStage);
            idx = (idx - 1 + values.Length) % values.Length;
            selectedStage = values[idx];
            RefreshStageUI();
        }

        public void OnStageNextClicked()
        {
            var values = (StageType[])System.Enum.GetValues(typeof(StageType));
            int idx = System.Array.IndexOf(values, selectedStage);
            idx = (idx + 1) % values.Length;
            selectedStage = values[idx];
            RefreshStageUI();
        }

        public void RefreshStageUI()
        {
            if (stageNameText != null)
            {
                stageNameText.text = FaceLoader.GetStageDisplayName(selectedStage, p2BodyType);
            }
            if (stageDescText != null)
            {
                stageDescText.text = FaceLoader.GetStageDescription(selectedStage, p2BodyType);
            }
            if (stagePreviewImage != null)
            {
                Sprite sp = FaceLoader.LoadStageSprite(selectedStage, p2BodyType);
                var outl = stagePreviewImage.GetComponent<Outline>();
                if (sp != null)
                {
                    stagePreviewImage.sprite = sp;
                    stagePreviewImage.color = Color.white;
                    stagePreviewImage.enabled = true;
                    if (outl != null) outl.enabled = true;
                }
                else
                {
                    stagePreviewImage.sprite = null;
                    stagePreviewImage.color = new Color(0, 0, 0, 0);
                    stagePreviewImage.enabled = false;
                    if (outl != null) outl.enabled = false;
                }
            }
        }

        public void OnStartFight()
        {
            if (preFightModal != null && !hasDismissedPreFightPrompt)
            {
                preFightModal.SetActive(true);
                return;
            }
            ExecuteStartFight();
        }

        public void ExecuteStartFight()
        {
            if (preFightModal != null) preFightModal.SetActive(false);
            currentGameMode = p2IsAI ? GameMode.VsAI : GameMode.VsHuman;
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StartFightWithCharacters(p1BodyType, p2BodyType, p2IsAI, false, selectedStage);
            }
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.HideCharacterSelectMenu();
            }
            gameObject.SetActive(false);
        }

        public void OnP1NameChanged(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;
            int fId = (int)p1BodyType + 1;
            FaceLoader.SaveFighterName(fId, newName);
            if (BattleManager.Instance != null && BattleManager.Instance.player1 != null)
            {
                BattleManager.Instance.player1.fighterName = newName;
            }
            if (TournamentManager.Instance != null)
            {
                var td = TournamentManager.Instance.GetFighterData(fId);
                if (td != null) td.name = newName;
            }
            RefreshStageUI();
        }

        public void OnP2NameChanged(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;
            int fId = (int)p2BodyType + 1;
            FaceLoader.SaveFighterName(fId, newName);
            if (BattleManager.Instance != null && BattleManager.Instance.player2 != null)
            {
                BattleManager.Instance.player2.fighterName = newName;
            }
            if (TournamentManager.Instance != null)
            {
                var td = TournamentManager.Instance.GetFighterData(fId);
                if (td != null) td.name = newName;
            }
            RefreshStageUI();
        }

        public void OnP1UploadPhoto()
        {
            int fId = (int)p1BodyType + 1;
            string chosen = PhotoFileImporter.PromptSelectImageFile($"Subir foto de rostro para {FaceLoader.GetFighterDisplayName(p1BodyType)}");
            if (!string.IsNullOrEmpty(chosen))
            {
                PhotoFileImporter.ImportAndApplyPhoto(chosen, fId, FaceType.Base);
                RefreshP1UI();
            }
        }

        public void OnP2UploadPhoto()
        {
            int fId = (int)p2BodyType + 1;
            string chosen = PhotoFileImporter.PromptSelectImageFile($"Subir foto de rostro para {FaceLoader.GetFighterDisplayName(p2BodyType)}");
            if (!string.IsNullOrEmpty(chosen))
            {
                PhotoFileImporter.ImportAndApplyPhoto(chosen, fId, FaceType.Base);
                RefreshP2UI();
            }
        }

        public void OnStartTrainingMode()
        {
            currentGameMode = GameMode.Training;
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StartFightWithCharacters(p1BodyType, p2BodyType, false, training: true, stage: selectedStage);
            }
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.HideCharacterSelectMenu();
            }
            gameObject.SetActive(false);
        }

        public void OnStartTournament()
        {
            if (TournamentManager.Instance != null)
            {
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.HideCharacterSelectMenu();
                }
                gameObject.SetActive(false);
                TournamentManager.Instance.StartNewTournament((int)p1BodyType + 1);
            }
        }

        public void RefreshP1UI()
        {
            int fId = (int)p1BodyType + 1;
            if (p1FighterCardImage != null)
            {
                Sprite bodySpr = FaceLoader.LoadFighterCardSprite(p1BodyType);
                p1FighterCardImage.sprite = bodySpr;
                p1FighterCardImage.enabled = (bodySpr != null);
                p1FighterCardImage.color = (bodySpr != null) ? Color.white : new Color(0, 0, 0, 0);
            }
            if (p1NameInputField != null)
            {
                p1NameInputField.text = FaceLoader.GetFighterDisplayName(p1BodyType);
            }
            else if (p1NameText != null)
            {
                p1NameText.text = FaceLoader.GetFighterDisplayName(p1BodyType).ToUpper();
            }
            if (p1DescText != null)
            {
                p1DescText.text = FaceLoader.GetFighterDescription(p1BodyType);
            }
            if (p1StatsText != null)
            {
                p1StatsText.text = GetStatsFormatted(p1BodyType);
            }

            FaceProfile prof = FaceLoader.LoadProfileForFighter(fId, FaceLoader.GetFighterDisplayName(p1BodyType));
            if (prof != null)
            {
                if (p1HeadPreviewImage != null)
                {
                    p1HeadPreviewImage.transform.localScale = Vector3.one;
                    Sprite baseSp = prof.GetSprite(FaceType.Base);
                    p1HeadPreviewImage.sprite = baseSp;
                    p1HeadPreviewImage.enabled = (baseSp != null);
                    p1HeadPreviewImage.color = (baseSp != null) ? Color.white : new Color(1, 1, 1, 0);
                    p1HeadPreviewImage.rectTransform.anchoredPosition = Vector2.zero;
                    p1HeadPreviewImage.rectTransform.sizeDelta = new Vector2(75f, 85f);
                }

                if (p1FaceThumbs != null)
                {
                    FaceType[] types = { FaceType.Base, FaceType.Dolor, FaceType.Enojo, FaceType.Ganador, FaceType.KO };
                    for (int t = 0; t < Mathf.Min(p1FaceThumbs.Length, types.Length); t++)
                    {
                        if (p1FaceThumbs[t] != null)
                        {
                            p1FaceThumbs[t].sprite = prof.GetSprite(types[t]);
                            p1FaceThumbs[t].color = (p1FaceThumbs[t].sprite != null) ? Color.white : new Color(0.1f, 0.1f, 0.2f, 0.4f);
                        }
                    }
                }
            }
        }

        public void RefreshP2UI()
        {
            int fId = (int)p2BodyType + 1;
            if (p2FighterCardImage != null)
            {
                Sprite bodySpr = FaceLoader.LoadFighterCardSprite(p2BodyType);
                p2FighterCardImage.sprite = bodySpr;
                p2FighterCardImage.enabled = (bodySpr != null);
                p2FighterCardImage.color = (bodySpr != null) ? Color.white : new Color(0, 0, 0, 0);
            }
            if (p2NameInputField != null)
            {
                p2NameInputField.text = FaceLoader.GetFighterDisplayName(p2BodyType);
            }
            else if (p2NameText != null)
            {
                string prefix = (currentGameMode == GameMode.Training) ? "DUMMY: " : "";
                p2NameText.text = prefix + FaceLoader.GetFighterDisplayName(p2BodyType).ToUpper();
            }
            if (p2DescText != null)
            {
                p2DescText.text = (currentGameMode == GameMode.Training) 
                    ? "Dummy de prueba: Recibe golpes, muestra daño y combos, y regenera su salud automáticamente."
                    : FaceLoader.GetFighterDescription(p2BodyType);
            }
            if (p2StatsText != null)
            {
                p2StatsText.text = GetStatsFormatted(p2BodyType);
            }
            if (p2AILabelText != null)
            {
                if (currentGameMode == GameMode.Training)
                {
                    p2AILabelText.text = "🥋 MODO: ENTRENAMIENTO";
                }
                else
                {
                    p2AILabelText.text = p2IsAI ? "🤖 MODO: CPU (IA)" : "🎮 MODO: 2P HUMANO";
                }
            }

            FaceProfile prof = FaceLoader.LoadProfileForFighter(fId, FaceLoader.GetFighterDisplayName(p2BodyType));
            if (prof != null)
            {
                if (p2HeadPreviewImage != null)
                {
                    p2HeadPreviewImage.transform.localScale = Vector3.one;
                    Sprite baseSp = prof.GetSprite(FaceType.Base);
                    p2HeadPreviewImage.sprite = baseSp;
                    p2HeadPreviewImage.enabled = (baseSp != null);
                    p2HeadPreviewImage.color = (baseSp != null) ? Color.white : new Color(1, 1, 1, 0);
                    p2HeadPreviewImage.rectTransform.anchoredPosition = Vector2.zero;
                    p2HeadPreviewImage.rectTransform.sizeDelta = new Vector2(75f, 85f);
                }

                if (p2FaceThumbs != null)
                {
                    FaceType[] types = { FaceType.Base, FaceType.Dolor, FaceType.Enojo, FaceType.Ganador, FaceType.KO };
                    for (int t = 0; t < Mathf.Min(p2FaceThumbs.Length, types.Length); t++)
                    {
                        if (p2FaceThumbs[t] != null)
                        {
                            p2FaceThumbs[t].sprite = prof.GetSprite(types[t]);
                            p2FaceThumbs[t].color = (p2FaceThumbs[t].sprite != null) ? Color.white : new Color(0.1f, 0.1f, 0.2f, 0.4f);
                        }
                    }
                }
            }
            RefreshStageUI();
        }

        public void RefreshRosterThumbnails()
        {
            if (rosterThumbImages == null) return;
            for (int i = 0; i < rosterThumbImages.Length && i < Roster.Length; i++)
            {
                if (rosterThumbImages[i] == null) continue;
                FighterBodyType b = Roster[i];
                int fId = (int)b + 1;
                FaceProfile prof = FaceLoader.LoadProfileForFighter(fId, FaceLoader.GetFighterDisplayName(b));
                if (prof != null && prof.faceBase != null)
                {
                    rosterThumbImages[i].sprite = prof.faceBase;
                }
                else
                {
                    rosterThumbImages[i].sprite = FaceLoader.LoadFighterThumbSprite(b);
                }
            }
        }

        public void RefreshRosterHighlights()
        {
            RefreshRosterThumbnails();
            for (int i = 0; i < Roster.Length; i++)
            {
                bool isP1 = (Roster[i] == p1BodyType);
                bool isP2 = (Roster[i] == p2BodyType);

                if (rosterP1Badges != null && i < rosterP1Badges.Length && rosterP1Badges[i] != null)
                {
                    rosterP1Badges[i].SetActive(isP1);
                }

                if (rosterP2Badges != null && i < rosterP2Badges.Length && rosterP2Badges[i] != null)
                {
                    rosterP2Badges[i].SetActive(isP2);
                    var txt = rosterP2Badges[i].GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.text = (currentGameMode == GameMode.Training) ? "DUMMY" : (p2IsAI ? "CPU" : "2P");
                    }
                }

                if (rosterOutlines != null && i < rosterOutlines.Length && rosterOutlines[i] != null)
                {
                    if (isP1 && isP2)
                    {
                        rosterOutlines[i].enabled = true;
                        rosterOutlines[i].effectColor = new Color(0.9f, 0.4f, 1f); // Morado fusión P1+P2
                        rosterOutlines[i].effectDistance = new Vector2(4, 4);
                    }
                    else if (isP1)
                    {
                        rosterOutlines[i].enabled = true;
                        rosterOutlines[i].effectColor = new Color(1f, 0.25f, 0.1f); // Rojo neón P1
                        rosterOutlines[i].effectDistance = new Vector2(4, 4);
                    }
                    else if (isP2)
                    {
                        rosterOutlines[i].enabled = true;
                        rosterOutlines[i].effectColor = new Color(0.1f, 0.65f, 1f); // Azul neón P2
                        rosterOutlines[i].effectDistance = new Vector2(4, 4);
                    }
                    else
                    {
                        rosterOutlines[i].enabled = true;
                        rosterOutlines[i].effectColor = new Color(0.3f, 0.35f, 0.45f, 0.7f);
                        rosterOutlines[i].effectDistance = new Vector2(2, 2);
                    }
                }
            }
        }

        public static string GetStatsFormatted(FighterBodyType body)
        {
            switch (body)
            {
                case FighterBodyType.Gordo:
                    return "FUERZA:    ■■■■■■■■■□  9\n" +
                           "VELOCIDAD: ■■■■□□□□□□  4\n" +
                           "DEFENSA:   ■■■■■■■■■■  10\n" +
                           "TÉCNICA:   ■■■■■□□□□□  5";
                case FighterBodyType.Flaco:
                    return "FUERZA:    ■■■■■□□□□□  5\n" +
                           "VELOCIDAD: ■■■■■■■■■■  10\n" +
                           "DEFENSA:   ■■■■□□□□□□  4\n" +
                           "TÉCNICA:   ■■■■■■■■■□  9";
                case FighterBodyType.Musculoso:
                    return "FUERZA:    ■■■■■■■■■□  9\n" +
                           "VELOCIDAD: ■■■■■■■□□□  7\n" +
                           "DEFENSA:   ■■■■■■■■□□  8\n" +
                           "TÉCNICA:   ■■■■■■■□□□  7";
                case FighterBodyType.Mujer:
                    return "FUERZA:    ■■■■■■□□□□  6\n" +
                           "VELOCIDAD: ■■■■■■■■■□  9\n" +
                           "DEFENSA:   ■■■■■□□□□□  5\n" +
                           "TÉCNICA:   ■■■■■■■■■□  9";
                case FighterBodyType.Mujer2:
                    return "FUERZA:    ■■■■■■■□□□  7\n" +
                           "VELOCIDAD: ■■■■■■■■■■  10\n" +
                           "DEFENSA:   ■■■■■■□□□□  6\n" +
                           "TÉCNICA:   ■■■■■■■■■■  10";
                case FighterBodyType.DosCabezas:
                    return "FUERZA:    ■■■■■■■■□□  8\n" +
                           "VELOCIDAD: ■■■■■■□□□□  6\n" +
                           "DEFENSA:   ■■■■■■■□□□  7\n" +
                           "TÉCNICA:   ■■■■■■□□□□  6";
                case FighterBodyType.Playero:
                    return "FUERZA:    ■■■■■■■□□□  7\n" +
                           "VELOCIDAD: ■■■■■■■■□□  8\n" +
                           "DEFENSA:   ■■■■■■■□□□  7\n" +
                           "TÉCNICA:   ■■■■■■■■□□  8";
                case FighterBodyType.Oficinista:
                    return "FUERZA:    ■■■■■■■■■□  9\n" +
                           "VELOCIDAD: ■■■■■■■■□□  8\n" +
                           "DEFENSA:   ■■■■■■□□□□  6\n" +
                           "TÉCNICA:   ■■■■■■■□□□  7";
                case FighterBodyType.Pijamero:
                    return "FUERZA:    ■■■■■■□□□□  6\n" +
                           "VELOCIDAD: ■■■■■■■■■□  9\n" +
                           "DEFENSA:   ■■■■■■■■□□  8\n" +
                           "TÉCNICA:   ■■■■■■■■■□  9";
                default:
                    return "";
            }
        }

        private void OnEnable()
        {
            RefreshP1UI();
            RefreshP2UI();
            RefreshRosterHighlights();
            UpdateMusicToggleVisuals();
            UpdateVoiceVolumeVisuals();
            UpdateP2ControllerModeUI();
        }
    }
}
