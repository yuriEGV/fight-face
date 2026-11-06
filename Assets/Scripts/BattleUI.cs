using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Interfaz gráfica de combate estilo Street Fighter II:
    /// - Barras de vida estilo retro arcade amarillas con ghosting rojo.
    /// - Barras de Stun (aturdimiento) dinámicas.
    /// - Indicadores RAGE READY pulsantes al caer bajo el 20% de HP.
    /// - Retratos faciales reactivos en tiempo real con las fotos del jugador.
    /// - Temporizador central y logotipo K.O.
    /// - Notificaciones de combo dinámicas (🔥 X HITS! 💥 Y DAMAGE).
    /// - Pantalla Modal de Victoria K.O. con la FOTO GANADOR (Foto 4).
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        public static BattleUI Instance { get; private set; }

        [Header("Jugador 1")]
        public Image p1HealthFill;
        public Image p1HealthGhost;
        public Image p1StunFill;
        public GameObject p1StunBadge;
        public GameObject p1RageBadge;
        public Image p1FacePortrait;
        public Text p1NameText;
        public Text p1HealthText;

        [Header("Jugador 2")]
        public Image p2HealthFill;
        public Image p2HealthGhost;
        public Image p2StunFill;
        public GameObject p2StunBadge;
        public GameObject p2RageBadge;
        public Image p2FacePortrait;
        public Text p2NameText;
        public Text p2HealthText;

        [Header("Temporizador y Banners Arcade")]
        public Text timerText;
        public Text centerBannerText;
        public GameObject centerBannerPanel;

        [Header("Contador de Combos")]
        public GameObject comboPanel;
        public Text comboHitsText;
        public Text comboDamageText;
        private Coroutine hideComboCoroutine;

        [Header("Pantalla Modal de Victoria (Winner Screen)")]
        public GameObject winnerModalPanel;
        public Image winnerPortraitImage;
        public Image loserPortraitImage;
        public Text winnerTitleText;
        public Text winnerNameText;
        public Text loserNameText;
        public Text winnerStatsText;
        public Button winnerRematchButton;
        public Button winnerSelectButton;
        private float winnerModalOpenTime = 0f;

        [Header("Paneles y Navegación")]
        public GameObject characterSelectPanel;
        public GameObject faceCustomizerPanel;
        public GameObject controlsGuidePanel;
        public Button rematchButton;
        public Button openCustomizerButton;
        public Button closeCustomizerButton;
        public Button tournamentButton;
        public Button openSelectMenuButton;

        [Header("Modo Entrenamiento UI")]
        public GameObject trainingOverlayPanel;
        public Text trainingBadgeText;
        public Text trainingMoveText;

        [Header("Marcadores de Rondas Street Fighter (V-Markers)")]
        public Image[] p1RoundMarkers;
        public Image[] p2RoundMarkers;

        private bool wasSelectMenuOpenBeforeWebcam = false;

        private float p1TargetFill = 1f;
        private float p2TargetFill = 1f;
        private float p1TargetStun = 0f;
        private float p2TargetStun = 0f;

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
        }

        private void Start()
        {
            if (rematchButton != null)
            {
                rematchButton.onClick.AddListener(() =>
                {
                    if (winnerModalPanel != null) winnerModalPanel.SetActive(false);
                    if (BattleManager.Instance != null) BattleManager.Instance.RestartMatch();
                });
            }

            if (winnerRematchButton != null)
            {
                winnerRematchButton.onClick.AddListener(() =>
                {
                    if (winnerModalPanel != null) winnerModalPanel.SetActive(false);
                    if (BattleManager.Instance != null) BattleManager.Instance.RestartMatch();
                });
            }

            if (winnerSelectButton != null)
            {
                winnerSelectButton.onClick.AddListener(() =>
                {
                    if (winnerModalPanel != null) winnerModalPanel.SetActive(false);
                    ShowCharacterSelectMenu();
                });
            }

            if (tournamentButton != null)
            {
                tournamentButton.onClick.AddListener(() =>
                {
                    if (TournamentManager.Instance != null)
                        TournamentManager.Instance.StartNewTournament(1);
                });
            }

            if (openCustomizerButton != null)
            {
                openCustomizerButton.onClick.AddListener(ToggleFaceCustomizer);
            }

            if (closeCustomizerButton != null)
            {
                closeCustomizerButton.onClick.AddListener(CloseFaceCustomizer);
            }

            if (openSelectMenuButton != null)
            {
                openSelectMenuButton.onClick.AddListener(ToggleCharacterSelectMenu);
            }

            // Reproducir sonido arcade en todos los botones de la interfaz de batalla
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                btn.onClick.AddListener(() => FightAudioManager.Instance?.PlayButtonClick());
            }

            if (centerBannerPanel != null) centerBannerPanel.SetActive(false);
            if (comboPanel != null) comboPanel.SetActive(false);
            if (winnerModalPanel != null) winnerModalPanel.SetActive(false);
            if (p1StunBadge != null) p1StunBadge.SetActive(false);
            if (p2StunBadge != null) p2StunBadge.SetActive(false);
            if (p1RageBadge != null) p1RageBadge.SetActive(false);
            if (p2RageBadge != null) p2RageBadge.SetActive(false);
        }

        private void Update()
        {
            HandleInput();

            // Suavizado de barras de vida y stun
            if (p1HealthFill != null)
                p1HealthFill.fillAmount = Mathf.Lerp(p1HealthFill.fillAmount, p1TargetFill, Time.unscaledDeltaTime * 14f);
            if (p1HealthGhost != null)
                p1HealthGhost.fillAmount = Mathf.Lerp(p1HealthGhost.fillAmount, p1TargetFill, Time.unscaledDeltaTime * 4f);

            if (p2HealthFill != null)
                p2HealthFill.fillAmount = Mathf.Lerp(p2HealthFill.fillAmount, p2TargetFill, Time.unscaledDeltaTime * 14f);
            if (p2HealthGhost != null)
                p2HealthGhost.fillAmount = Mathf.Lerp(p2HealthGhost.fillAmount, p2TargetFill, Time.unscaledDeltaTime * 4f);

            if (p1StunFill != null)
                p1StunFill.fillAmount = Mathf.Lerp(p1StunFill.fillAmount, p1TargetStun, Time.unscaledDeltaTime * 10f);
            if (p2StunFill != null)
                p2StunFill.fillAmount = Mathf.Lerp(p2StunFill.fillAmount, p2TargetStun, Time.unscaledDeltaTime * 10f);

            // Actualizar retratos
            UpdateFacePortraits();
        }

        private void HandleInput()
        {
            // 1. Pantalla de Victoria K.O.: Altiro a Selección de Personajes con Confirmar (ENTER / ESPACIO) o M / ESC
            if (winnerModalPanel != null && winnerModalPanel.activeSelf)
            {
                // Buffer de seguridad de 0.8s para evitar que botones presionados durante el combate cierren la pantalla inmediatamente
                if (Time.unscaledTime - winnerModalOpenTime > 0.8f)
                {
                    if (FightInput.GetConfirm() || FightInput.GetMenuToggle())
                    {
                        winnerModalPanel.SetActive(false);
                        ShowCharacterSelectMenu();
                    }
                    else if (FightInput.GetRestart())
                    {
                        winnerModalPanel.SetActive(false);
                        if (BattleManager.Instance != null) BattleManager.Instance.RestartMatch();
                    }
                }
                return;
            }

            // 2. Si el creador de rostros está abierto
            if (faceCustomizerPanel != null && faceCustomizerPanel.activeSelf)
            {
                if (FightInput.GetMenuToggle() || FightInput.GetToggleCustomizer())
                    ToggleFaceCustomizer();
                return;
            }

            // 3. Si el menú de selección de personajes está abierto, CharacterSelectUI maneja los inputs
            if (characterSelectPanel != null && characterSelectPanel.activeSelf)
                return;

            // 4. Durante el combate libre en el ring
            if (FightInput.GetRestart())
            {
                if (BattleManager.Instance != null) BattleManager.Instance.RestartMatch();
            }
            else if (FightInput.GetMenuToggle())
            {
                if (BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
                {
                    ExitTrainingToSelectMenu();
                }
                else
                {
                    ToggleCharacterSelectMenu();
                }
            }
            else if (FightInput.GetToggleCustomizer())
            {
                ToggleFaceCustomizer();
            }
            else if (FightInput.GetTournamentStart())
            {
                ToggleTournamentMenu();
            }
            else if (FightInput.GetToggleMusic())
            {
                if (FightAudioManager.Instance != null)
                {
                    bool isEnabled = FightAudioManager.Instance.ToggleMusic();
                    ShowBanner(isEnabled ? "🎵 MÚSICA ACTIVADA" : "🔇 MÚSICA DESACTIVADA", 1.8f);
                }
            }
            else if (FightInput.GetToggleVoiceVolume())
            {
                if (FightAudioManager.Instance != null)
                {
                    float mult = FightAudioManager.Instance.CycleVoiceVolume();
                    ShowBanner($"🗣️ VOLUMEN VOCES: {Mathf.RoundToInt(mult * 100f)}%", 1.8f);
                }
            }
            else if (FightInput.GetKeyT())
            {
                if (VirtualTouchControls.Instance != null)
                {
                    VirtualTouchControls.Instance.ToggleTouchControls();
                }
            }
            else if (FightInput.GetToggleWifiLobby())
            {
                if (NetworkLobbyUI.Instance != null)
                {
                    NetworkLobbyUI.Instance.ToggleLobby();
                }
            }
        }

        private void UpdateFacePortraits()
        {
            if (BattleManager.Instance == null) return;

            // Retrato P1
            if (p1FacePortrait != null && BattleManager.Instance.player1 != null)
            {
                var p1Face = BattleManager.Instance.player1.faceController;
                if (p1Face != null && p1Face.Profile != null)
                {
                    p1FacePortrait.sprite = p1Face.Profile.GetSprite(p1Face.CurrentEmotion);
                }
            }

            // Retrato P2
            if (p2FacePortrait != null && BattleManager.Instance.player2 != null)
            {
                var p2Face = BattleManager.Instance.player2.faceController;
                if (p2Face != null && p2Face.Profile != null)
                {
                    p2FacePortrait.sprite = p2Face.Profile.GetSprite(p2Face.CurrentEmotion);
                }
            }
        }

        public void UpdateHealth(int playerId, int current, int max)
        {
            float fill = Mathf.Clamp01((float)current / Mathf.Max(1, max));
            if (playerId == 1)
            {
                p1TargetFill = fill;
                if (p1HealthText != null) p1HealthText.text = $"{Mathf.Max(0, current)} / {max}";
            }
            else
            {
                p2TargetFill = fill;
                if (p2HealthText != null) p2HealthText.text = $"{Mathf.Max(0, current)} / {max}";
            }
        }

        public void UpdateStun(int playerId, float current, float max)
        {
            float fill = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (playerId == 1) p1TargetStun = fill;
            else p2TargetStun = fill;
        }

        public void ShowStunBadge(int playerId, bool active)
        {
            if (playerId == 1 && p1StunBadge != null) p1StunBadge.SetActive(active);
            else if (playerId == 2 && p2StunBadge != null) p2StunBadge.SetActive(active);
        }

        public void SetRageBadge(int playerId, bool active)
        {
            if (playerId == 1 && p1RageBadge != null) p1RageBadge.SetActive(active);
            else if (playerId == 2 && p2RageBadge != null) p2RageBadge.SetActive(active);
        }

        public void ShowComboNotification(int attackerPlayerId, int hits, int damage)
        {
            if (trainingOverlayPanel != null && trainingOverlayPanel.activeSelf && trainingMoveText != null)
            {
                trainingMoveText.text = $"💥 COMBO: <color=#FFDD00>{hits} GOLPES</color>  |  DAÑO TOTAL: <color=#FF4444>{damage}</color>";
            }

            if (comboPanel == null || comboHitsText == null || comboDamageText == null) return;

            comboHitsText.text = $"🔥 {hits} HITS!";
            comboDamageText.text = $"💥 {damage} DAMAGE";
            comboPanel.SetActive(true);

            if (hideComboCoroutine != null) StopCoroutine(hideComboCoroutine);
            hideComboCoroutine = StartCoroutine(HideComboRoutine(1.3f));
        }

        private IEnumerator HideComboRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (comboPanel != null) comboPanel.SetActive(false);
            hideComboCoroutine = null;
        }

        public void UpdateTimerString(string text)
        {
            if (timerText != null)
            {
                timerText.text = text;
            }
        }

        public void SetTrainingModeUI(bool active)
        {
            if (openSelectMenuButton != null)
            {
                var txt = openSelectMenuButton.GetComponentInChildren<Text>();
                var img = openSelectMenuButton.GetComponent<Image>();
                if (active)
                {
                    if (txt != null) txt.text = "🛑 SALIR [ESC]";
                    if (img != null) img.color = new Color(0.95f, 0.15f, 0.15f);
                }
                else
                {
                    if (txt != null) txt.text = "🥊 Menú [ESC]";
                    if (img != null) img.color = new Color(0.95f, 0.25f, 0.35f);
                }
            }

            if (active)
            {
                if (trainingOverlayPanel == null)
                {
                    BuildTrainingOverlay();
                }
                if (trainingOverlayPanel != null)
                {
                    trainingOverlayPanel.SetActive(true);
                }
                ResetTrainingComboFeedback();
            }
            else
            {
                if (trainingOverlayPanel != null)
                {
                    trainingOverlayPanel.SetActive(false);
                }
            }
        }

        public void ShowTrainingMove(string moveName, int damage)
        {
            if (trainingMoveText != null)
            {
                trainingMoveText.text = $"ÚLTIMO GOLPE: <color=#FFE600>{moveName.ToUpper()}</color>  |  DAÑO BASE: <color=#FF5555>{damage}</color>";
            }
        }

        public void ResetTrainingComboFeedback()
        {
            if (trainingMoveText != null)
            {
                trainingMoveText.text = "PROBANDO GOLPES: [F] Jab | [R] Cross | [G] Patada Baja | [T] Patada Alta | [Q] Especial";
            }
        }

        private void BuildTrainingOverlay()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas == null) return;

            trainingOverlayPanel = new GameObject("TrainingOverlayPanel");
            trainingOverlayPanel.transform.SetParent(canvas.transform, false);
            var rect = trainingOverlayPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.10f, 0.74f);
            rect.anchorMax = new Vector2(0.90f, 0.86f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = trainingOverlayPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.94f);
            var outline = trainingOverlayPanel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.75f, 0.1f, 0.9f);
            outline.effectDistance = new Vector2(2, 2);

            // Botón Izquierdo: Reiniciar Posición [F1]
            GameObject rstBtnObj = new GameObject("Btn_ResetPos");
            rstBtnObj.transform.SetParent(trainingOverlayPanel.transform, false);
            var rbRect = rstBtnObj.AddComponent<RectTransform>();
            rbRect.anchorMin = new Vector2(0.015f, 0.12f);
            rbRect.anchorMax = new Vector2(0.19f, 0.88f);
            rbRect.offsetMin = Vector2.zero;
            rbRect.offsetMax = Vector2.zero;
            var rbImg = rstBtnObj.AddComponent<Image>();
            rbImg.color = new Color(0.18f, 0.40f, 0.85f);
            var rbOutline = rstBtnObj.AddComponent<Outline>();
            rbOutline.effectColor = new Color(0.4f, 0.7f, 1f);
            rbOutline.effectDistance = new Vector2(1, 1);
            var rbBtn = rstBtnObj.AddComponent<Button>();
            rbBtn.onClick.AddListener(() => {
                if (BattleManager.Instance != null) BattleManager.Instance.ResetTrainingPositions();
            });

            GameObject rbTxtObj = new GameObject("Text");
            rbTxtObj.transform.SetParent(rstBtnObj.transform, false);
            var rbTRect = rbTxtObj.AddComponent<RectTransform>();
            rbTRect.anchorMin = Vector2.zero;
            rbTRect.anchorMax = Vector2.one;
            rbTRect.sizeDelta = Vector2.zero;
            var rbTxt = rbTxtObj.AddComponent<Text>();
            rbTxt.text = "🔄 REINICIAR [F1]";
            rbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rbTxt.fontSize = 11;
            rbTxt.fontStyle = FontStyle.Bold;
            rbTxt.alignment = TextAnchor.MiddleCenter;
            rbTxt.color = Color.white;
            rbTxt.raycastTarget = false;

            // Título Badge Central
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(trainingOverlayPanel.transform, false);
            var bRect = badgeObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.20f, 0.48f);
            bRect.anchorMax = new Vector2(0.76f, 0.98f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            trainingBadgeText = badgeObj.AddComponent<Text>();
            trainingBadgeText.text = "🥋 MODO ENTRENAMIENTO  •  PRÁCTICA Y COMBOS";
            trainingBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            trainingBadgeText.fontSize = 13;
            trainingBadgeText.fontStyle = FontStyle.Bold;
            trainingBadgeText.alignment = TextAnchor.MiddleCenter;
            trainingBadgeText.color = new Color(1f, 0.85f, 0.2f);
            trainingBadgeText.raycastTarget = false;

            // Move & Damage readout
            GameObject moveObj = new GameObject("MoveText");
            moveObj.transform.SetParent(trainingOverlayPanel.transform, false);
            var mRect = moveObj.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.20f, 0.02f);
            mRect.anchorMax = new Vector2(0.76f, 0.50f);
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;
            trainingMoveText = moveObj.AddComponent<Text>();
            trainingMoveText.text = "Golpea al muñeco dummy para practicar todos tus movimientos";
            trainingMoveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            trainingMoveText.fontSize = 12;
            trainingMoveText.fontStyle = FontStyle.Normal;
            trainingMoveText.alignment = TextAnchor.MiddleCenter;
            trainingMoveText.color = Color.white;
            trainingMoveText.raycastTarget = false;

            // Botón Derecho: Salir al Menú [M / ESC]
            GameObject exitBtnObj = new GameObject("Btn_ExitTraining");
            exitBtnObj.transform.SetParent(trainingOverlayPanel.transform, false);
            var ebRect = exitBtnObj.AddComponent<RectTransform>();
            ebRect.anchorMin = new Vector2(0.77f, 0.12f);
            ebRect.anchorMax = new Vector2(0.985f, 0.88f);
            ebRect.offsetMin = Vector2.zero;
            ebRect.offsetMax = Vector2.zero;
            var ebImg = exitBtnObj.AddComponent<Image>();
            ebImg.color = new Color(0.95f, 0.20f, 0.15f);
            var ebOutline = exitBtnObj.AddComponent<Outline>();
            ebOutline.effectColor = new Color(1f, 0.85f, 0.2f);
            ebOutline.effectDistance = new Vector2(2, 2);
            var ebBtn = exitBtnObj.AddComponent<Button>();
            ebBtn.onClick.AddListener(ExitTrainingToSelectMenu);

            GameObject ebTxtObj = new GameObject("Text");
            ebTxtObj.transform.SetParent(exitBtnObj.transform, false);
            var ebTRect = ebTxtObj.AddComponent<RectTransform>();
            ebTRect.anchorMin = Vector2.zero;
            ebTRect.anchorMax = Vector2.one;
            ebTRect.sizeDelta = Vector2.zero;
            var ebTxt = ebTxtObj.AddComponent<Text>();
            ebTxt.text = "🛑 FINALIZAR\n[ESC / TOQUE]";
            ebTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ebTxt.fontSize = 12;
            ebTxt.fontStyle = FontStyle.Bold;
            ebTxt.alignment = TextAnchor.MiddleCenter;
            ebTxt.color = Color.white;
            ebTxt.raycastTarget = false;
        }

        public void ExitTrainingToSelectMenu()
        {
            if (trainingOverlayPanel != null) trainingOverlayPanel.SetActive(false);
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.isTrainingMode = false;
                BattleManager.Instance.isMatchActive = false;
            }
            if (openSelectMenuButton != null)
            {
                var txt = openSelectMenuButton.GetComponentInChildren<Text>();
                var img = openSelectMenuButton.GetComponent<Image>();
                if (txt != null) txt.text = "🥊 Menú [ESC]";
                if (img != null) img.color = new Color(0.95f, 0.25f, 0.35f);
            }
            ShowCharacterSelectMenu();
        }

        /// <summary>
        /// Muestra la pantalla oficial de ganador usando la FOTO GANADOR (Foto 4).
        /// Sirve de celebración arcade y separación definitiva de combate para elegir otro personaje.
        /// </summary>
        public void ShowWinnerScreen(FighterController winner, FighterController loser)
        {
            if (winnerModalPanel == null) return;

            winnerModalPanel.transform.SetAsLastSibling();
            winnerModalPanel.SetActive(true);
            winnerModalOpenTime = Time.unscaledTime;

            if (winnerTitleText != null) winnerTitleText.text = "🏆 ★ ¡VICTORIA TOTAL! ★ 🏆";
            if (winnerNameText != null && winner != null) winnerNameText.text = $"👑 {winner.fighterName.ToUpper()} ES EL CAMPEÓN 👑";

            if (winnerStatsText != null && winner != null)
            {
                int healthPct = Mathf.RoundToInt(winner.HealthPercent * 100f);
                int maxHits = winner.maxComboHits > 0 ? winner.maxComboHits : 1;
                winnerStatsText.text = $"❤️ {healthPct}% SALUD RESTANTE   •   🔥 MAX COMBO: {maxHits} HITS\n✨ ¡2 RONDAS GANADAS SEGUIDAS! ELIGE TU PRÓXIMO COMBATE ✨";
            }

            // Foto Ganador
            if (winnerPortraitImage != null && winner != null && winner.faceController != null)
            {
                var prof = winner.faceController.Profile;
                if (prof != null)
                {
                    Sprite winSprite = prof.faceWinner != null ? prof.faceWinner : prof.GetSprite(FaceType.Ganador);
                    if (winSprite != null) winnerPortraitImage.sprite = winSprite;
                }
            }

            // Foto Perdedor (K.O.)
            if (loserPortraitImage != null && loser != null && loser.faceController != null)
            {
                var loserProf = loser.faceController.Profile;
                if (loserProf != null)
                {
                    Sprite koSprite = loserProf.faceKO != null ? loserProf.faceKO : loserProf.GetSprite(FaceType.KO);
                    if (koSprite != null) loserPortraitImage.sprite = koSprite;
                }
            }
            if (loserNameText != null && loser != null)
            {
                loserNameText.text = $"DERROTADO: <b>{loser.fighterName.ToUpper()}</b>";
            }
        }



        public void UpdateTimer(int seconds)
        {
            if (timerText != null)
            {
                timerText.text = seconds.ToString();
            }
        }

        public void ShowBanner(string message, float duration = 2.5f)
        {
            if (centerBannerPanel != null && centerBannerText != null)
            {
                centerBannerText.text = message;
                centerBannerPanel.SetActive(true);
                StopCoroutine("HideBannerAfterDelay");
                StartCoroutine(HideBannerAfterDelay(duration));
            }
        }

        private IEnumerator HideBannerAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (centerBannerPanel != null)
            {
                centerBannerPanel.SetActive(false);
            }
        }

        public void ShowCharacterSelectMenu()
        {
            if (trainingOverlayPanel != null)
            {
                trainingOverlayPanel.SetActive(false);
            }

            if (characterSelectPanel != null)
            {
                characterSelectPanel.transform.SetAsLastSibling();
                characterSelectPanel.SetActive(true);
                Time.timeScale = 0f;
                if (CharacterSelectUI.Instance != null)
                {
                    CharacterSelectUI.Instance.RefreshP1UI();
                    CharacterSelectUI.Instance.RefreshP2UI();
                    CharacterSelectUI.Instance.RefreshStageUI();
                }
            }
        }

        public void HideCharacterSelectMenu()
        {
            if (characterSelectPanel != null)
            {
                characterSelectPanel.SetActive(false);
                Time.timeScale = 1f;
            }
        }

        public void ToggleCharacterSelectMenu()
        {
            if (characterSelectPanel != null)
            {
                bool active = !characterSelectPanel.activeSelf;
                if (active) ShowCharacterSelectMenu();
                else HideCharacterSelectMenu();
            }
        }

        public void ToggleTournamentMenu()
        {
            if (TournamentManager.Instance != null)
            {
                int p1Id = 1;
                if (BattleManager.Instance != null && BattleManager.Instance.player1 != null)
                {
                    p1Id = (int)BattleManager.Instance.p1BodyType + 1;
                }
                if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
                TournamentManager.Instance.StartNewTournament(p1Id);
            }
        }

        public void RefreshFacePortraits()
        {
            UpdateFacePortraits();
        }

        public void UpdateRoundWins(int p1Wins, int p2Wins)
        {
            if (p1RoundMarkers != null)
            {
                for (int i = 0; i < p1RoundMarkers.Length; i++)
                {
                    if (p1RoundMarkers[i] != null)
                    {
                        bool won = i < p1Wins;
                        p1RoundMarkers[i].color = won ? new Color(1f, 0.88f, 0.1f, 1f) : new Color(0.2f, 0.22f, 0.3f, 0.6f);
                    }
                }
            }

            if (p2RoundMarkers != null)
            {
                for (int i = 0; i < p2RoundMarkers.Length; i++)
                {
                    if (p2RoundMarkers[i] != null)
                    {
                        bool won = i < p2Wins;
                        p2RoundMarkers[i].color = won ? new Color(1f, 0.88f, 0.1f, 1f) : new Color(0.2f, 0.22f, 0.3f, 0.6f);
                    }
                }
            }
        }

        public void OpenFaceCustomizer(int targetFighterId = -1, FaceType targetEmotion = FaceType.Base)
        {
            if (faceCustomizerPanel == null) return;

            wasSelectMenuOpenBeforeWebcam = (characterSelectPanel != null && characterSelectPanel.activeSelf);
            if (wasSelectMenuOpenBeforeWebcam)
            {
                characterSelectPanel.SetActive(false);
            }

            faceCustomizerPanel.transform.SetAsLastSibling();
            faceCustomizerPanel.SetActive(true);
            Time.timeScale = 0f; // Pausa para tomar fotos con calma

            if (WebcamCaptureManager.Instance != null)
            {
                if (targetFighterId > 0)
                {
                    WebcamCaptureManager.Instance.SetTargetFighter(targetFighterId);
                }
                WebcamCaptureManager.Instance.SetGuideForEmotion(targetEmotion);
                WebcamCaptureManager.Instance.StartWebcam();
            }
        }

        public void CloseFaceCustomizer()
        {
            if (faceCustomizerPanel == null) return;

            faceCustomizerPanel.SetActive(false);

            if (WebcamCaptureManager.Instance != null)
            {
                WebcamCaptureManager.Instance.StopWebcam();
            }

            RefreshFacePortraits();

            if (wasSelectMenuOpenBeforeWebcam)
            {
                wasSelectMenuOpenBeforeWebcam = false;
                ShowCharacterSelectMenu();
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        public void ToggleFaceCustomizer()
        {
            if (faceCustomizerPanel == null) return;
            if (faceCustomizerPanel.activeSelf)
            {
                CloseFaceCustomizer();
            }
            else
            {
                OpenFaceCustomizer();
            }
        }
    }
}
