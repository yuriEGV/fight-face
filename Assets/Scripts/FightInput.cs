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

        // ----------------- JUGADOR 2 (Flechas + L / K) -----------------
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

        // ----------------- CONTROLES GENERALES -----------------
        public static bool GetRestart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.rKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.R);
        }

        public static bool GetToggleCustomizer()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) return kb.cKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Tab);
        }
    }
}
