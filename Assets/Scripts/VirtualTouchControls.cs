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
        public static bool IsPausePressed { get; set; }

        private static bool jumpConsumed = false;
        private static bool punchConsumed = false;
        private static bool kickConsumed = false;
        private static bool heavyPunchConsumed = false;
        private static bool heavyKickConsumed = false;
        private static bool specialConsumed = false;
        private static bool pauseConsumed = false;

        public static bool isTouchControlsEnabled { get; private set; } = true;

        [Header("Contenedor Principal")]
        public GameObject touchRootPanel;

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

            // En dispositivos móviles (Android/iOS) o pantallas táctiles se activa por defecto
            bool isMobile = Application.isMobilePlatform || Input.touchSupported;
            isTouchControlsEnabled = PlayerPrefs.GetInt("FightFace_TouchEnabled", isMobile ? 1 : 0) == 1;

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

            // Visibilidad dinámica: solo se muestran durante el combate activo (no en menús de fotos o selección)
            if (touchRootPanel != null)
            {
                bool inFight = BattleManager.Instance != null && BattleManager.Instance.IsMatchActive;
                bool isSelectActive = CharacterSelectUI.Instance != null && CharacterSelectUI.Instance.gameObject.activeInHierarchy;
                bool isWebcamActive = WebcamCaptureManager.Instance != null && WebcamCaptureManager.Instance.gameObject.activeInHierarchy;

                bool shouldShow = isTouchControlsEnabled && (inFight && !isSelectActive && !isWebcamActive);

                if (touchRootPanel.activeSelf != shouldShow)
                {
                    touchRootPanel.SetActive(shouldShow);
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

            if (touchRootPanel != null)
            {
                touchRootPanel.SetActive(isTouchControlsEnabled);
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
                case TouchAction.Pause:
                    IsPausePressed = isPressed;
                    if (!isPressed) pauseConsumed = false;
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
        Pause
    }

    /// <summary>
    /// Componente de botón táctil multitáctil altamente responsivo estilo Arcade.
    /// Responde a IPointerDownHandler, IPointerUpHandler y IPointerExitHandler para control táctil fluido.
    /// </summary>
    public class TouchButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
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
                    Mathf.Min(normalColor.r * 1.35f, 1f),
                    Mathf.Min(normalColor.g * 1.35f, 1f),
                    Mathf.Min(normalColor.b * 1.35f, 1f),
                    Mathf.Min(normalColor.a * 1.35f, 1f)
                );
            }
            originalScale = transform.localScale;
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

        private void OnDisable()
        {
            VirtualTouchControls.SetTouchAction(action, false);
            transform.localScale = originalScale;
            if (buttonImage != null) buttonImage.color = normalColor;
        }
    }
}
