using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Sistema de Controles Táctiles en Pantalla (Virtual Touch Controls) para dispositivos móviles / Android:
    /// - Cruceta D-Pad táctil arcade (Izquierda, Derecha, Salto, Agacharse)
    /// - Botonera arcade Street Fighter (Puño Ligero, Patada Ligera, Puño Fuerte, Patada Fuerte, Hadouken Especial)
    /// - Botón de Pausa / Menú táctil
    /// - Soporte multitáctil independiente para moverte y golpear al mismo tiempo con ambos pulgares.
    /// - Activación automática en Android/iOS o manual con tecla [T] en PC/Editor.
    /// - Uso de CanvasGroup para transiciones fluidas sin desactivar GameObjects ni pausar scripts.
    /// </summary>
    public class VirtualTouchControls : MonoBehaviour
    {
        public static VirtualTouchControls Instance { get; private set; }

        public static bool IsLeftPressed { get; set; }
        public static bool IsRightPressed { get; set; }
        public static bool IsDownPressed { get; set; }
        public static bool IsJumpPressed { get; set; }
        public static bool IsPunchPressed { get; set; }
        public static bool IsKickPressed { get; set; }
        public static bool IsHeavyPunchPressed { get; set; }
        public static bool IsHeavyKickPressed { get; set; }
        public static bool IsSpecialPressed { get; set; }
        public static bool IsGrabPressed { get; set; }
        public static bool IsPausePressed { get; set; }
        public static bool IsExitTrainingPressed { get; set; }

        private static bool jumpConsumed = false;
        private static bool punchConsumed = false;
        private static bool kickConsumed = false;
        private static bool heavyPunchConsumed = false;
        private static bool heavyKickConsumed = false;
        private static bool specialConsumed = false;
        private static bool grabConsumed = false;
        private static bool pauseConsumed = false;
        private static bool exitTrainingConsumed = false;

        public static bool isTouchControlsEnabled { get; private set; } = true;

        [Header("Contenedores UI")]
        public GameObject touchRootPanel;
        public CanvasGroup touchCanvasGroup;
        public GameObject exitTrainingTouchButton;

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

            // En dispositivos móviles (Android/iOS) siempre activado por defecto
            bool isMobile = Application.isMobilePlatform || Input.touchSupported;
            isTouchControlsEnabled = isMobile || PlayerPrefs.GetInt("FightFace_TouchEnabled", isMobile ? 1 : 0) == 1;

            if (touchRootPanel == null)
            {
                touchRootPanel = gameObject;
            }

            if (touchCanvasGroup == null && touchRootPanel != null)
            {
                touchCanvasGroup = touchRootPanel.GetComponent<CanvasGroup>();
                if (touchCanvasGroup == null)
                {
                    touchCanvasGroup = touchRootPanel.AddComponent<CanvasGroup>();
                }
            }

            // En dispositivos móviles asegurar 60 FPS y pantalla siempre encendida
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }

        private void Update()
        {
            // Atajo en PC [T] o [F7] para activar/desactivar controles táctiles en cualquier momento
            if (FightInput.GetKeyT())
            {
                ToggleTouchControls();
            }

            // Salir de entrenamiento si se presiona el botón táctil de salir
            if (ConsumeExitTrainingPressed())
            {
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ExitTrainingToSelectMenu();
                }
            }

            // Atajo táctil de pausa / menú
            if (ConsumePausePressed())
            {
                if (BattleUI.Instance != null)
                {
                    if (BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
                    {
                        BattleUI.Instance.ExitTrainingToSelectMenu();
                    }
                    else
                    {
                        BattleUI.Instance.ToggleCharacterSelectMenu();
                    }
                }
            }

            // Visibilidad del botón flotante para finalizar entrenamiento en móvil
            bool inTraining = BattleManager.Instance != null && BattleManager.Instance.isTrainingMode;
            if (exitTrainingTouchButton != null && exitTrainingTouchButton.activeSelf != inTraining)
            {
                exitTrainingTouchButton.SetActive(inTraining);
            }

            // Visibilidad dinámica mediante CanvasGroup (NUNCA desactiva el GameObject)
            if (touchCanvasGroup != null)
            {
                bool isSelectActive = CharacterSelectUI.Instance != null && CharacterSelectUI.Instance.gameObject.activeInHierarchy;
                bool isWebcamActive = WebcamCaptureManager.Instance != null && WebcamCaptureManager.Instance.gameObject.activeInHierarchy;
                bool isWinnerActive = BattleUI.Instance != null && BattleUI.Instance.winnerModalPanel != null && BattleUI.Instance.winnerModalPanel.activeInHierarchy;
                bool isCelebrationActive = TournamentCelebrationUI.Instance != null && TournamentCelebrationUI.Instance.IsActive;
                bool isLobbyActive = NetworkLobbyUI.Instance != null && NetworkLobbyUI.Instance.IsLobbyOpen();

                // Controles visibles en combate normal o entrenamiento mientras no haya menús modales superpuestos
                bool inArena = !isSelectActive && !isWebcamActive && !isWinnerActive && !isCelebrationActive && !isLobbyActive;
                bool shouldShow = isTouchControlsEnabled && inArena;

                float targetAlpha = shouldShow ? 1f : 0f;
                if (!Mathf.Approximately(touchCanvasGroup.alpha, targetAlpha))
                {
                    touchCanvasGroup.alpha = targetAlpha;
                    touchCanvasGroup.interactable = shouldShow;
                    touchCanvasGroup.blocksRaycasts = shouldShow;
                }
            }
        }

        public void ToggleTouchControls()
        {
            SetTouchControlsEnabled(!isTouchControlsEnabled);
        }

        public void SetTouchControlsEnabled(bool enabled)
        {
            isTouchControlsEnabled = enabled;
            PlayerPrefs.SetInt("FightFace_TouchEnabled", isTouchControlsEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (touchCanvasGroup != null)
            {
                touchCanvasGroup.alpha = isTouchControlsEnabled ? 1f : 0f;
                touchCanvasGroup.interactable = isTouchControlsEnabled;
                touchCanvasGroup.blocksRaycasts = isTouchControlsEnabled;
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner(isTouchControlsEnabled ? "📱 CONTROLES TÁCTILES: ACTIVADOS" : "📱 CONTROLES TÁCTILES: DESACTIVADOS", 1.5f);
            }

            Debug.Log($"[VirtualTouch] 📱 Controles táctiles: {(isTouchControlsEnabled ? "ON" : "OFF")}");
        }

        // ================== CONSUMIDORES DE PULSACIÓN (FRAME-TRIGGER) ==================

        public static bool ConsumeJumpPressed()
        {
            if (IsJumpPressed && !jumpConsumed)
            {
                jumpConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumePunchPressed()
        {
            if (IsPunchPressed && !punchConsumed)
            {
                punchConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumeKickPressed()
        {
            if (IsKickPressed && !kickConsumed)
            {
                kickConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumeHeavyPunchPressed()
        {
            if (IsHeavyPunchPressed && !heavyPunchConsumed)
            {
                heavyPunchConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumeHeavyKickPressed()
        {
            if (IsHeavyKickPressed && !heavyKickConsumed)
            {
                heavyKickConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumeSpecialPressed()
        {
            if (IsSpecialPressed && !specialConsumed)
            {
                specialConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumePausePressed()
        {
            if (IsPausePressed && !pauseConsumed)
            {
                pauseConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumeGrabPressed()
        {
            if (IsGrabPressed && !grabConsumed)
            {
                grabConsumed = true;
                return true;
            }
            return false;
        }

        public static bool ConsumeExitTrainingPressed()
        {
            if (IsExitTrainingPressed && !exitTrainingConsumed)
            {
                exitTrainingConsumed = true;
                return true;
            }
            return false;
        }

        // ================== ACCIONES DE BOTONES TÁCTILES ==================

        public static void SetTouchAction(TouchAction action, bool isPressed)
        {
            switch (action)
            {
                case TouchAction.Left:
                    IsLeftPressed = isPressed;
                    break;
                case TouchAction.Right:
                    IsRightPressed = isPressed;
                    break;
                case TouchAction.Down:
                    IsDownPressed = isPressed;
                    break;
                case TouchAction.Jump:
                    IsJumpPressed = isPressed;
                    if (!isPressed) jumpConsumed = false;
                    break;
                case TouchAction.Punch:
                    IsPunchPressed = isPressed;
                    if (!isPressed) punchConsumed = false;
                    break;
                case TouchAction.Kick:
                    IsKickPressed = isPressed;
                    if (!isPressed) kickConsumed = false;
                    break;
                case TouchAction.HeavyPunch:
                    IsHeavyPunchPressed = isPressed;
                    if (!isPressed) heavyPunchConsumed = false;
                    break;
                case TouchAction.HeavyKick:
                    IsHeavyKickPressed = isPressed;
                    if (!isPressed) heavyKickConsumed = false;
                    break;
                case TouchAction.Special:
                    IsSpecialPressed = isPressed;
                    if (!isPressed) specialConsumed = false;
                    break;
                case TouchAction.Grab:
                    IsGrabPressed = isPressed;
                    if (!isPressed) grabConsumed = false;
                    break;
                case TouchAction.Pause:
                    IsPausePressed = isPressed;
                    if (!isPressed) pauseConsumed = false;
                    break;
                case TouchAction.ExitTraining:
                    IsExitTrainingPressed = isPressed;
                    if (!isPressed) exitTrainingConsumed = false;
                    break;
            }
        }
    }

    public enum TouchAction
    {
        Left,
        Right,
        Down,
        Jump,
        Punch,
        Kick,
        HeavyPunch,
        HeavyKick,
        Special,
        Grab,
        Pause,
        ExitTraining
    }

    /// <summary>
    /// Componente de botón táctil multitáctil altamente responsivo estilo Arcade.
    /// Responde a IPointerDownHandler, IPointerUpHandler, IPointerExitHandler e IPointerEnterHandler para control táctil fluido.
    /// </summary>
    public class TouchButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        public TouchAction action;
        private Image buttonImage;
        private Color normalColor;
        private Color pressedColor;
        private Vector3 originalScale;

        private void Awake()
        {
            buttonImage = GetComponent<Image>();
            if (buttonImage != null)
            {
                normalColor = buttonImage.color;
                pressedColor = new Color(
                    Mathf.Min(normalColor.r * 1.4f, 1f),
                    Mathf.Min(normalColor.g * 1.4f, 1f),
                    Mathf.Min(normalColor.b * 1.4f, 1f),
                    Mathf.Min(normalColor.a * 1.4f, 1f)
                );
            }
            originalScale = transform.localScale;

            // Desactivar raycastTarget en todos los Text hijos para que el toque lo reciba directamente el Image del botón
            foreach (Text t in GetComponentsInChildren<Text>(true))
            {
                t.raycastTarget = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            VirtualTouchControls.SetTouchAction(action, true);
            transform.localScale = originalScale * 0.90f; // Efecto de presión física
            if (buttonImage != null) buttonImage.color = pressedColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            VirtualTouchControls.SetTouchAction(action, false);
            transform.localScale = originalScale;
            if (buttonImage != null) buttonImage.color = normalColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            VirtualTouchControls.SetTouchAction(action, false);
            transform.localScale = originalScale;
            if (buttonImage != null) buttonImage.color = normalColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.dragging || eventData.eligibleForClick)
            {
                VirtualTouchControls.SetTouchAction(action, true);
                transform.localScale = originalScale * 0.90f;
                if (buttonImage != null) buttonImage.color = pressedColor;
            }
        }

        private void OnDisable()
        {
            VirtualTouchControls.SetTouchAction(action, false);
            transform.localScale = originalScale;
            if (buttonImage != null) buttonImage.color = normalColor;
        }
    }
}
