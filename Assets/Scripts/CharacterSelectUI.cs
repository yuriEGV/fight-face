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
            FighterBodyType.DosCabezas
        };

        [Header("Selecciones Activas")]
        public FighterBodyType p1BodyType = FighterBodyType.Musculoso;
        public FighterBodyType p2BodyType = FighterBodyType.Gordo;
        public bool p2IsAI = true;

        [Header("P1 UI")]
        public Image p1FighterCardImage;
        public Image p1HeadPreviewImage;
        public Text p1NameText;
        public Text p1DescText;
        public Text p1StatsText;
        public Button p1WebcamButton;
        public Button[] p1BodyButtons;

        [Header("P2 UI")]
        public Image p2FighterCardImage;
        public Image p2HeadPreviewImage;
        public Text p2NameText;
        public Text p2DescText;
        public Text p2StatsText;
        public Text p2AILabelText;
        public Button p2AIToggleButton;
        public Button p2WebcamButton;
        public Button[] p2BodyButtons;

        [Header("Roster Arcade Grid (Estilo Street Fighter II)")]
        public Button[] rosterSlotButtons;
        public Image[] rosterThumbImages;
        public GameObject[] rosterP1Badges;
        public GameObject[] rosterP2Badges;
        public Outline[] rosterOutlines;

        [Header("Botones Principales")]
        public Button fightButton;
        public Button tournamentButton;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            RefreshP1UI();
            RefreshP2UI();
            RefreshRosterHighlights();

            if (fightButton != null)
            {
                fightButton.onClick.AddListener(OnStartFight);
            }

            if (p2AIToggleButton != null)
            {
                p2AIToggleButton.onClick.AddListener(ToggleP2AI);
            }

            if (p1WebcamButton != null)
            {
                p1WebcamButton.onClick.AddListener(OpenP1Webcam);
            }

            if (p2WebcamButton != null)
            {
                p2WebcamButton.onClick.AddListener(OpenP2Webcam);
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
                if (BattleUI.Instance != null) BattleUI.Instance.ToggleTournamentMenu();
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
            RefreshP1UI();
            RefreshRosterHighlights();
            if (BattleManager.Instance != null && BattleManager.Instance.player1 != null)
            {
                if (BattleManager.Instance.player1.bodyController != null)
                {
                    BattleManager.Instance.player1.bodyController.SetClassicBody(body);
                }
            }
        }

        public void SelectP2Body(FighterBodyType body)
        {
            p2BodyType = body;
            RefreshP2UI();
            RefreshRosterHighlights();
            if (BattleManager.Instance != null && BattleManager.Instance.player2 != null)
            {
                if (BattleManager.Instance.player2.bodyController != null)
                {
                    BattleManager.Instance.player2.bodyController.SetClassicBody(body);
                }
            }
        }

        public void ToggleP2AI()
        {
            p2IsAI = !p2IsAI;
            RefreshP2UI();
            RefreshRosterHighlights();
        }

        public void OpenP1Webcam()
        {
            if (WebcamCaptureManager.Instance != null)
            {
                WebcamCaptureManager.Instance.SetTargetFighter(1);
            }
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ToggleFaceCustomizer();
            }
        }

        public void OpenP2Webcam()
        {
            if (WebcamCaptureManager.Instance != null)
            {
                WebcamCaptureManager.Instance.SetTargetFighter(2);
            }
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ToggleFaceCustomizer();
            }
        }

        public void OnStartFight()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StartFightWithCharacters(p1BodyType, p2BodyType, p2IsAI);
            }
            gameObject.SetActive(false);
        }

        public void RefreshP1UI()
        {
            if (p1FighterCardImage != null)
            {
                p1FighterCardImage.sprite = FaceLoader.LoadFighterCardSprite(p1BodyType);
            }
            if (p1NameText != null)
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
            if (p1HeadPreviewImage != null)
            {
                p1HeadPreviewImage.transform.localScale = Vector3.one;
                if (BattleManager.Instance != null && BattleManager.Instance.player1 != null)
                {
                    var p1Face = BattleManager.Instance.player1.faceController;
                    if (p1Face != null && p1Face.Profile != null)
                    {
                        p1HeadPreviewImage.sprite = p1Face.Profile.GetSprite(FaceType.Base);
                    }
                }
            }
        }

        public void RefreshP2UI()
        {
            if (p2FighterCardImage != null)
            {
                p2FighterCardImage.sprite = FaceLoader.LoadFighterCardSprite(p2BodyType);
            }
            if (p2NameText != null)
            {
                p2NameText.text = FaceLoader.GetFighterDisplayName(p2BodyType).ToUpper();
            }
            if (p2DescText != null)
            {
                p2DescText.text = FaceLoader.GetFighterDescription(p2BodyType);
            }
            if (p2StatsText != null)
            {
                p2StatsText.text = GetStatsFormatted(p2BodyType);
            }
            if (p2AILabelText != null)
            {
                p2AILabelText.text = p2IsAI ? "🤖 MODO: CPU (IA)" : "🎮 MODO: 2P HUMANO";
            }
            if (p2HeadPreviewImage != null)
            {
                p2HeadPreviewImage.transform.localScale = Vector3.one;
                if (BattleManager.Instance != null && BattleManager.Instance.player2 != null)
                {
                    var p2Face = BattleManager.Instance.player2.faceController;
                    if (p2Face != null && p2Face.Profile != null)
                    {
                        p2HeadPreviewImage.sprite = p2Face.Profile.GetSprite(FaceType.Base);
                    }
                }
            }
        }

        public void RefreshRosterHighlights()
        {
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
                        txt.text = p2IsAI ? "CPU" : "2P";
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
                case FighterBodyType.DosCabezas:
                    return "FUERZA:    ■■■■■■■■□□  8\n" +
                           "VELOCIDAD: ■■■■■■□□□□  6\n" +
                           "DEFENSA:   ■■■■■■■□□□  7\n" +
                           "TÉCNICA:   ■■■■■■□□□□  6";
                default:
                    return "";
            }
        }

        private void OnEnable()
        {
            RefreshP1UI();
            RefreshP2UI();
            RefreshRosterHighlights();
        }
    }
}
