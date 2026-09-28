using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Controlador de la Pantalla de Selección de Luchadores:
    /// - Permite elegir el cuerpo para P1 y P2 entre los 5 personajes clásicos:
    ///   El Gordo, El Flaco, El Musculoso, La Mujer, El Dos Cabezas.
    /// - Muestra tarjetas de presentación y descripción de estilo de pelea.
    /// - Permite abrir la captura de webcam para personalizar el rostro de P1 o P2 con recorte ovalado.
    /// - Permite alternar P2 entre Jugador Humano o CPU.
    /// - Botón "¡A PELEAR!" para iniciar el combate en el escenario tradicional.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        public static CharacterSelectUI Instance { get; private set; }

        [Header("Selecciones Activas")]
        public FighterBodyType p1BodyType = FighterBodyType.Musculoso;
        public FighterBodyType p2BodyType = FighterBodyType.Gordo;
        public bool p2IsAI = true;

        [Header("P1 UI")]
        public Image p1FighterCardImage;
        public Image p1HeadPreviewImage;
        public Text p1NameText;
        public Text p1DescText;
        public Button p1WebcamButton;
        public Button[] p1BodyButtons;

        [Header("P2 UI")]
        public Image p2FighterCardImage;
        public Image p2HeadPreviewImage;
        public Text p2NameText;
        public Text p2DescText;
        public Text p2AILabelText;
        public Button p2AIToggleButton;
        public Button p2WebcamButton;
        public Button[] p2BodyButtons;

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
        }

        private void Update()
        {
            if (gameObject.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    OnStartFight();
                }
            }
        }

        public void SelectP1Body(FighterBodyType body)
        {
            p1BodyType = body;
            RefreshP1UI();
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
                p1NameText.text = FaceLoader.GetFighterDisplayName(p1BodyType);
            }
            if (p1DescText != null)
            {
                p1DescText.text = FaceLoader.GetFighterDescription(p1BodyType);
            }
            if (p1HeadPreviewImage != null && BattleManager.Instance != null && BattleManager.Instance.player1 != null)
            {
                var p1Face = BattleManager.Instance.player1.faceController;
                if (p1Face != null && p1Face.Profile != null)
                {
                    p1HeadPreviewImage.sprite = p1Face.Profile.GetSprite(FaceType.Base);
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
                p2NameText.text = FaceLoader.GetFighterDisplayName(p2BodyType);
            }
            if (p2DescText != null)
            {
                p2DescText.text = FaceLoader.GetFighterDescription(p2BodyType);
            }
            if (p2AILabelText != null)
            {
                p2AILabelText.text = p2IsAI ? "Control: 🤖 CPU (IA)" : "Control: 🎮 Jugador 2";
            }
            if (p2HeadPreviewImage != null && BattleManager.Instance != null && BattleManager.Instance.player2 != null)
            {
                var p2Face = BattleManager.Instance.player2.faceController;
                if (p2Face != null && p2Face.Profile != null)
                {
                    p2HeadPreviewImage.sprite = p2Face.Profile.GetSprite(FaceType.Base);
                }
            }
        }

        private void OnEnable()
        {
            RefreshP1UI();
            RefreshP2UI();
        }
    }
}
