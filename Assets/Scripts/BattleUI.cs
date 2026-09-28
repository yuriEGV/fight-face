using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Interfaz gráfica de combate completa:
    /// - Barras de vida con interpolación suave.
    /// - Miniaturas de rostros reactivas a las 4 emociones en tiempo real.
    /// - Temporizador estilo arcade 99s.
    /// - Banners de ¡K.O.! y Victoria.
    /// - Panel de personalización de caras y webcam.
    /// - Guía interactiva de controles en pantalla.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        public static BattleUI Instance { get; private set; }

        [Header("Jugador 1")]
        public Image p1HealthFill;
        public Image p1HealthGhost;
        public Image p1FacePortrait;
        public Text p1NameText;
        public Text p1HealthText;

        [Header("Jugador 2")]
        public Image p2HealthFill;
        public Image p2HealthGhost;
        public Image p2FacePortrait;
        public Text p2NameText;
        public Text p2HealthText;

        [Header("Temporizador y Mensajes")]
        public Text timerText;
        public Text centerBannerText;
        public GameObject centerBannerPanel;

        [Header("Paneles y Modales")]
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
                    if (BattleManager.Instance != null)
                        BattleManager.Instance.RestartMatch();
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

            if (centerBannerPanel != null)
            {
                centerBannerPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Atajos de teclado rápidos
            if (FightInput.GetRestart())
            {
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

            // Suavizado de barras de vida usando unscaledDeltaTime (inmune a pausas de Hitstop)
            if (p1HealthFill != null)
            {
                p1HealthFill.fillAmount = Mathf.Lerp(p1HealthFill.fillAmount, p1TargetFill, Time.unscaledDeltaTime * 14f);
            }
            if (p1HealthGhost != null)
            {
                p1HealthGhost.fillAmount = Mathf.Lerp(p1HealthGhost.fillAmount, p1TargetFill, Time.unscaledDeltaTime * 4f);
            }

            if (p2HealthFill != null)
            {
                p2HealthFill.fillAmount = Mathf.Lerp(p2HealthFill.fillAmount, p2TargetFill, Time.unscaledDeltaTime * 14f);
            }
            if (p2HealthGhost != null)
            {
                p2HealthGhost.fillAmount = Mathf.Lerp(p2HealthGhost.fillAmount, p2TargetFill, Time.unscaledDeltaTime * 4f);
            }

            // Actualizar retratos de caras en tiempo real
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
