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
        public Text winnerTitleText;
        public Text winnerNameText;
        public Text winnerStatsText;
        public Button winnerRematchButton;
        public Button winnerSelectButton;

        [Header("Paneles y Navegación")]
        public GameObject characterSelectPanel;
        public GameObject faceCustomizerPanel;
        public GameObject controlsGuidePanel;
        public Button rematchButton;
        public Button openCustomizerButton;
        public Button closeCustomizerButton;
        public Button tournamentButton;
        public Button openSelectMenuButton;

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
                closeCustomizerButton.onClick.AddListener(ToggleFaceCustomizer);
            }

            if (openSelectMenuButton != null)
            {
                openSelectMenuButton.onClick.AddListener(ToggleCharacterSelectMenu);
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
            // Atajos de teclado
            if (FightInput.GetRestart())
            {
                if (winnerModalPanel != null && winnerModalPanel.activeSelf)
                    winnerModalPanel.SetActive(false);
                if (BattleManager.Instance != null)
                    BattleManager.Instance.RestartMatch();
            }

            if (FightInput.GetTournamentStart())
            {
                if (TournamentManager.Instance != null)
                    TournamentManager.Instance.StartNewTournament(1);
            }

            if (FightInput.GetToggleCustomizer())
            {
                ToggleFaceCustomizer();
            }

            if (FightInput.GetMenuToggle())
            {
                ToggleCharacterSelectMenu();
            }

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

        /// <summary>
        /// Muestra la pantalla oficial de ganador usando la FOTO GANADOR (Foto 4).
        /// </summary>
        public void ShowWinnerScreen(FighterController winner, FighterController loser)
        {
            if (winnerModalPanel == null) return;

            winnerModalPanel.SetActive(true);

            if (winnerTitleText != null) winnerTitleText.text = "🏆 WINNER 🏆";
            if (winnerNameText != null && winner != null) winnerNameText.text = winner.fighterName.ToUpper();

            if (winnerStatsText != null && winner != null)
            {
                int healthPct = Mathf.RoundToInt(winner.HealthPercent * 100f);
                int maxHits = winner.maxComboHits > 0 ? winner.maxComboHits : 1;
                winnerStatsText.text = $"❤️ {healthPct}% SALUD RESTANTE\n🔥 MAX COMBO: {maxHits} HITS";
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
            if (characterSelectPanel != null)
            {
                characterSelectPanel.SetActive(true);
            }
        }

        public void HideCharacterSelectMenu()
        {
            if (characterSelectPanel != null)
            {
                characterSelectPanel.SetActive(false);
            }
        }

        public void ToggleCharacterSelectMenu()
        {
            if (characterSelectPanel != null)
            {
                bool active = !characterSelectPanel.activeSelf;
                characterSelectPanel.SetActive(active);
            }
        }

        public void RefreshFacePortraits()
        {
            UpdateFacePortraits();
        }

        public void ToggleFaceCustomizer()
        {
            if (faceCustomizerPanel == null) return;

            bool isActive = !faceCustomizerPanel.activeSelf;
            faceCustomizerPanel.SetActive(isActive);

            if (isActive)
            {
                if (WebcamCaptureManager.Instance != null)
                    WebcamCaptureManager.Instance.StartWebcam();
            }
            else
            {
                if (WebcamCaptureManager.Instance != null)
                    WebcamCaptureManager.Instance.StopWebcam();
                RefreshFacePortraits();
            }
        }
    }
}
