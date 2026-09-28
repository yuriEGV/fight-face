using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FightFace
{
    /// <summary>
    /// Wrapper de entrada para soportar el Nuevo Input System de Unity 6
    /// de manera limpia y sin fricción.
    /// </summary>
    public static class FightInput
    {
        // ----------------- JUGADOR 1 (WASD + F / G / Espacio) -----------------
        public static bool GetP1Left()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.aKey.isPressed;
#else
            return Input.GetKey(KeyCode.A);
#endif
        }

        public static bool GetP1Right()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.dKey.isPressed;
#else
            return Input.GetKey(KeyCode.D);
#endif
        }

        public static bool GetP1Jump()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.wKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.W);
#endif
        }

        public static bool GetP1Punch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool GetP1Kick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.gKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool GetP1HeavyPunch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.rKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.J);
#endif
        }

        public static bool GetP1HeavyKick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.tKey.wasPressedThisFrame || kb.uKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.U);
#endif
        }

        public static bool GetP1Special()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.qKey.wasPressedThisFrame || kb.yKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Y);
#endif
        }

        public static bool GetP1Grab()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.hKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.H);
#endif
        }

        public static bool GetP1SelectPrev()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.aKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.A);
#endif
        }

        public static bool GetP1SelectNext()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.dKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.D);
#endif
        }

        // ----------------- JUGADOR 2 (Flechas + L / K / P / O / I / NumPad) -----------------
        public static bool GetP2Left()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.leftArrowKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftArrow);
#endif
        }

        public static bool GetP2Right()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.rightArrowKey.isPressed;
#else
            return Input.GetKey(KeyCode.RightArrow);
#endif
        }

        public static bool GetP2Jump()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.upArrowKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        public static bool GetP2Punch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.lKey.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Keypad1);
#endif
        }

        public static bool GetP2Kick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.kKey.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Keypad2);
#endif
        }

        public static bool GetP2HeavyPunch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.pKey.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Keypad4);
#endif
        }

        public static bool GetP2HeavyKick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.oKey.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Keypad5);
#endif
        }

        public static bool GetP2Special()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.iKey.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Keypad6);
#endif
        }

        public static bool GetP2Grab()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.numpad3Key.wasPressedThisFrame || kb.semicolonKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Semicolon);
#endif
        }

        public static bool GetP2SelectPrev()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.leftArrowKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.LeftArrow);
#endif
        }

        public static bool GetP2SelectNext()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.rightArrowKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.RightArrow);
#endif
        }

        // ----------------- CONTROLES GENERALES -----------------
        public static bool GetConfirm()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool GetRestart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        public static bool GetTournamentStart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.tKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.T);
#endif
        }

        public static bool GetToggleCustomizer()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.cKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        public static bool GetMenuToggle()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M);
#endif
        }
    }
}
