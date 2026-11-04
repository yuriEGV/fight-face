using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FightFace
{
    /// <summary>
    /// Wrapper de entrada unificado para Unity 6:
    /// Soporte dual simultáneo para Teclado y Joysticks / Gamepads (Xbox, PlayStation, Mandos USB y Arcade Sticks).
    /// </summary>
    public static class FightInput
    {
#if ENABLE_INPUT_SYSTEM
        public static Gamepad GetGamepad1()
        {
            if (Gamepad.all.Count > 0) return Gamepad.all[0];
            return Gamepad.current;
        }

        public static Gamepad GetGamepad2()
        {
            if (Gamepad.all.Count > 1) return Gamepad.all[1];
            return null;
        }
#endif

        // ----------------- JUGADOR 1 (Teclado: WASD + F / G / R / T / Q / H | Joystick: D-Pad / Stick + A/B/X/Y/LB/RB/RT | Touch Móvil) -----------------
        public static bool GetP1Down()
        {
            if (VirtualTouchControls.IsDownPressed) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.sKey.isPressed) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.down.isPressed) return true;
                if (pad.leftStick.down.isPressed) return true;
                if (pad.leftStick.y.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.S);
#endif
        }

        public static bool GetP1Left()
        {
            if (VirtualTouchControls.IsLeftPressed) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.aKey.isPressed) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.left.isPressed) return true;
                if (pad.leftStick.left.isPressed) return true;
                if (pad.leftStick.x.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.A);
#endif
        }

        public static bool GetP1Right()
        {
            if (VirtualTouchControls.IsRightPressed) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.dKey.isPressed) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.right.isPressed) return true;
                if (pad.leftStick.right.isPressed) return true;
                if (pad.leftStick.x.ReadValue() > 0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.D);
#endif
        }

        public static bool GetP1Jump()
        {
            if (VirtualTouchControls.ConsumeJumpPressed()) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.wKey.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame) return true;
                if (pad.leftStick.up.wasPressedThisFrame) return true;
                if (pad.leftStick.y.ReadValue() > 0.65f) return true;
            }
            return false;
#else
            return Input.GetKeyDown(KeyCode.W);
#endif
        }

        public static bool GetP1Punch()
        {
            if (VirtualTouchControls.ConsumePunchPressed()) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.buttonWest.wasPressedThisFrame) return true; // X en Xbox / Cuadrado en PS
            return false;
#else
            return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool GetP1Kick()
        {
            if (VirtualTouchControls.ConsumeKickPressed()) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.gKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true; // A en Xbox / Cruz en PS
            return false;
#else
            return Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool GetP1HeavyPunch()
        {
            if (VirtualTouchControls.ConsumeHeavyPunchPressed()) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.rKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.buttonNorth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)) return true; // Y / RB en Xbox / Triángulo / R1 en PS
            return false;
#else
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.J);
#endif
        }

        public static bool GetP1HeavyKick()
        {
            if (VirtualTouchControls.ConsumeHeavyKickPressed()) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.tKey.wasPressedThisFrame || kb.uKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame)) return true; // B / RT en Xbox / Círculo / R2 en PS
            return false;
#else
            return Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.U);
#endif
        }

        public static bool GetP1Special()
        {
            if (VirtualTouchControls.ConsumeSpecialPressed()) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.qKey.wasPressedThisFrame || kb.yKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.leftTrigger.wasPressedThisFrame || (pad.leftShoulder.isPressed && pad.rightShoulder.isPressed))) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Y);
#endif
        }

        public static bool GetP1Grab()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.leftShoulder.wasPressedThisFrame) return true; // LB en Xbox / L1 en PS
            return false;
#else
            return Input.GetKeyDown(KeyCode.H);
#endif
        }

        public static bool GetP1SelectPrev()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.aKey.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.A);
#endif
        }

        public static bool GetP1SelectNext()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.dKey.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.D);
#endif
        }

        // ----------------- JUGADOR 2 (Teclado: Flechas + L / K / P / O / I | Joystick 2 si está conectado) -----------------
        public static bool GetP2Down()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.downArrowKey.isPressed) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.down.isPressed) return true;
                if (pad.leftStick.down.isPressed) return true;
                if (pad.leftStick.y.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.DownArrow);
#endif
        }

        public static bool GetP2Left()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.leftArrowKey.isPressed) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.left.isPressed) return true;
                if (pad.leftStick.left.isPressed) return true;
                if (pad.leftStick.x.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.LeftArrow);
#endif
        }

        public static bool GetP2Right()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.rightArrowKey.isPressed) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.right.isPressed) return true;
                if (pad.leftStick.right.isPressed) return true;
                if (pad.leftStick.x.ReadValue() > 0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.RightArrow);
#endif
        }

        public static bool GetP2Jump()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.upArrowKey.wasPressedThisFrame) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame) return true;
                if (pad.leftStick.up.wasPressedThisFrame) return true;
                if (pad.leftStick.y.ReadValue() > 0.65f) return true;
            }
            return false;
#else
            return Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        public static bool GetP2Punch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.lKey.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)) return true;
            var pad = GetGamepad2();
            if (pad != null && pad.buttonWest.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Keypad1);
#endif
        }

        public static bool GetP2Kick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.kKey.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)) return true;
            var pad = GetGamepad2();
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Keypad2);
#endif
        }

        public static bool GetP2HeavyPunch()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.pKey.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)) return true;
            var pad = GetGamepad2();
            if (pad != null && (pad.buttonNorth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Keypad4);
#endif
        }

        public static bool GetP2HeavyKick()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.oKey.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)) return true;
            var pad = GetGamepad2();
            if (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Keypad5);
#endif
        }

        public static bool GetP2Special()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.iKey.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame)) return true;
            var pad = GetGamepad2();
            if (pad != null && (pad.rightTrigger.wasPressedThisFrame || pad.leftTrigger.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Keypad6);
#endif
        }

        public static bool GetP2Grab()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.numpad3Key.wasPressedThisFrame || kb.semicolonKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad2();
            if (pad != null && pad.leftShoulder.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Semicolon);
#endif
        }

        public static bool GetP2SelectPrev()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.leftArrowKey.wasPressedThisFrame) return true;
            var pad = GetGamepad2();
            if (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.LeftArrow);
#endif
        }

        public static bool GetP2SelectNext()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.rightArrowKey.wasPressedThisFrame) return true;
            var pad = GetGamepad2();
            if (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.RightArrow);
#endif
        }

        // ----------------- CONTROLES GENERALES -----------------
        public static bool GetConfirm()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool GetRestart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.f5Key.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame || (kb.rKey.wasPressedThisFrame && (kb.ctrlKey.isPressed || kb.altKey.isPressed)))) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.selectButton.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.Backspace) || (Input.GetKeyDown(KeyCode.R) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)));
#endif
        }

        public static bool GetTrainingReset()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.f1Key.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.selectButton.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Backspace);
#endif
        }

        public static bool GetTournamentStart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.buttonNorth.wasPressedThisFrame) return true; // Y en Xbox / Triángulo en PS
            return false;
#else
            return Input.GetKeyDown(KeyCode.F3);
#endif
        }

        public static bool GetTrainingStart()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.f2Key.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.buttonWest.wasPressedThisFrame) return true; // X en Xbox / Cuadrado en PS
            return false;
#else
            return Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool GetStagePrev()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.pageUpKey.wasPressedThisFrame || kb.leftBracketKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.leftShoulder.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.LeftBracket);
#endif
        }

        public static bool GetStageNext()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.pageDownKey.wasPressedThisFrame || kb.rightBracketKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.rightShoulder.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.RightBracket);
#endif
        }

        public static bool GetToggleCustomizer()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }

        public static bool GetMenuToggle()
        {
            if (VirtualTouchControls.ConsumePausePressed()) return true;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.startButton.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool GetToggleMusic()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.mKey.wasPressedThisFrame || kb.f4Key.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.leftStickButton.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.F4);
#endif
        }

        public static bool GetToggleVoiceVolume()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.vKey.wasPressedThisFrame || kb.f6Key.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.rightStickButton.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.F6);
#endif
        }

        public static bool GetKeyT()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.tKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.T);
#endif
        }
    }
}
