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
            if (kb != null) return kb.aKey.isPressed;
#endif
            return Input.GetKey(KeyCode.A);
        }

        public static bool GetP1Right()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.dKey.isPressed;
#endif
            return Input.GetKey(KeyCode.D);
        }

        public static bool GetP1Jump()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.wKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.W);
        }

        public static bool GetP1Punch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space);
        }

        public static bool GetP1Kick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.gKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.E);
        }

        public static bool GetP1HeavyPunch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.rKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.J);
        }

        public static bool GetP1HeavyKick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.tKey.wasPressedThisFrame || kb.uKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.U);
        }

        public static bool GetP1Special()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.qKey.wasPressedThisFrame || kb.yKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Y);
        }

        public static bool GetP1Grab()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.hKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.H);
        }

        // ----------------- JUGADOR 2 (Flechas + L / K / P / O / I / NumPad) -----------------
        public static bool GetP2Left()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.leftArrowKey.isPressed;
#endif
            return Input.GetKey(KeyCode.LeftArrow);
        }

        public static bool GetP2Right()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.rightArrowKey.isPressed;
#endif
            return Input.GetKey(KeyCode.RightArrow);
        }

        public static bool GetP2Jump()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.upArrowKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.UpArrow);
        }

        public static bool GetP2Punch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.lKey.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Keypad1);
        }

        public static bool GetP2Kick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.kKey.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Keypad2);
        }

        public static bool GetP2HeavyPunch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.pKey.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Keypad4);
        }

        public static bool GetP2HeavyKick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.oKey.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Keypad5);
        }

        public static bool GetP2Special()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.iKey.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Keypad6);
        }

        public static bool GetP2Grab()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.numpad3Key.wasPressedThisFrame || kb.semicolonKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Semicolon);
        }

        // ----------------- CONTROLES GENERALES -----------------
        public static bool GetRestart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.rKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.R);
        }

        public static bool GetTournamentStart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.tKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.T);
        }

        public static bool GetToggleCustomizer()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.cKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Tab);
        }

        public static bool GetMenuToggle()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.escapeKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M);
        }
    }
}
