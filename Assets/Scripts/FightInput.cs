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
        // Asignación flexible de mandos USB / Joysticks para jugar 2 personas en 1 PC con 1 teclado y 1 joystick
        public static bool IsGamepad1AssignedToP2 = false;

        public static bool ToggleGamepadAssignment()
        {
            IsGamepad1AssignedToP2 = !IsGamepad1AssignedToP2;
            return IsGamepad1AssignedToP2;
        }

#if ENABLE_INPUT_SYSTEM
        public static Gamepad GetGamepad1()
        {
            if (IsGamepad1AssignedToP2)
            {
                if (Gamepad.all.Count > 1) return Gamepad.all[1];
                return null;
            }
            if (Gamepad.all.Count > 0) return Gamepad.all[0];
            return Gamepad.current;
        }

        public static Gamepad GetGamepad2()
        {
            if (IsGamepad1AssignedToP2)
            {
                if (Gamepad.all.Count > 0) return Gamepad.all[0];
                return Gamepad.current;
            }
            if (Gamepad.all.Count > 1) return Gamepad.all[1];
            return null;
        }
#endif

        // ----------------- MÉTODOS DIRECTOS (RAW) DE TECLADO / GAMEPAD -----------------
        public static bool GetP1DownRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.sKey.isPressed) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.down.isPressed || pad.leftStick.down.isPressed || pad.leftStick.y.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.S);
#endif
        }

        public static bool GetP1LeftRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.aKey.isPressed) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.left.isPressed || pad.leftStick.left.isPressed || pad.leftStick.x.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.A);
#endif
        }

        public static bool GetP1RightRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.dKey.isPressed) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.right.isPressed || pad.leftStick.right.isPressed || pad.leftStick.x.ReadValue() > 0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.D);
#endif
        }

        public static bool GetP1JumpRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.wKey.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame || pad.leftStick.y.ReadValue() > 0.65f) return true;
            }
            return false;
#else
            return Input.GetKeyDown(KeyCode.W);
#endif
        }

        public static bool GetP1PunchRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.buttonWest.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool GetP1KickRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.gKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool GetP1HeavyPunchRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.rKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.buttonNorth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.J);
#endif
        }

        public static bool GetP1HeavyKickRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.tKey.wasPressedThisFrame || kb.uKey.wasPressedThisFrame)) return true;
            var pad = GetGamepad1();
            if (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.U);
#endif
        }

        public static bool GetP1SpecialRaw()
        {
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

        public static bool GetP1GrabRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame) return true;
            var pad = GetGamepad1();
            if (pad != null && pad.leftShoulder.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.H);
#endif
        }

        public static bool GetP2DownRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.downArrowKey.isPressed) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.down.isPressed || pad.leftStick.down.isPressed || pad.leftStick.y.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.DownArrow);
#endif
        }

        public static bool GetP2LeftRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.leftArrowKey.isPressed) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.left.isPressed || pad.leftStick.left.isPressed || pad.leftStick.x.ReadValue() < -0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.LeftArrow);
#endif
        }

        public static bool GetP2RightRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.rightArrowKey.isPressed) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.right.isPressed || pad.leftStick.right.isPressed || pad.leftStick.x.ReadValue() > 0.35f) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.RightArrow);
#endif
        }

        public static bool GetP2JumpRaw()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.upArrowKey.wasPressedThisFrame) return true;
            var pad = GetGamepad2();
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame || pad.leftStick.y.ReadValue() > 0.65f) return true;
            }
            return false;
#else
            return Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        public static bool GetP2PunchRaw()
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

        public static bool GetP2KickRaw()
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

        public static bool GetP2HeavyPunchRaw()
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

        public static bool GetP2HeavyKickRaw()
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

        public static bool GetP2SpecialRaw()
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

        public static bool GetP2GrabRaw()
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

        // ----------------- JUGADOR 1 (Entradas de juego activas) -----------------
        public static bool GetP1Down()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.IsDownPressed) return true;
            return GetP1DownRaw();
        }

        public static bool GetP1Left()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.IsLeftPressed) return true;
            return GetP1LeftRaw();
        }

        public static bool GetP1Right()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.IsRightPressed) return true;
            return GetP1RightRaw();
        }

        public static bool GetP1Jump()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.ConsumeJumpPressed()) return true;
            return GetP1JumpRaw();
        }

        public static bool GetP1Punch()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.ConsumePunchPressed()) return true;
            return GetP1PunchRaw();
        }

        public static bool GetP1Kick()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.ConsumeKickPressed()) return true;
            return GetP1KickRaw();
        }

        public static bool GetP1HeavyPunch()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.ConsumeHeavyPunchPressed()) return true;
            return GetP1HeavyPunchRaw();
        }

        public static bool GetP1HeavyKick()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.ConsumeHeavyKickPressed()) return true;
            return GetP1HeavyKickRaw();
        }

        public static bool GetP1Special()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.ConsumeSpecialPressed()) return true;
            return GetP1SpecialRaw();
        }

        public static bool GetP1Grab()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive && NetworkFightManager.Instance.IsClient)
            {
                return false;
            }
            if (VirtualTouchControls.ConsumeGrabPressed()) return true;
            return GetP1GrabRaw();
        }

        // ----------------- JUGADOR 2 (Entradas de juego activas) -----------------
        public static bool GetP2Down()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Down) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.IsDownPressed) return true;
                    return GetP1DownRaw() || GetP2DownRaw();
                }
            }
            return GetP2DownRaw();
        }

        public static bool GetP2Left()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Left) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.IsLeftPressed) return true;
                    return GetP1LeftRaw() || GetP2LeftRaw();
                }
            }
            return GetP2LeftRaw();
        }

        public static bool GetP2Right()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Right) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.IsRightPressed) return true;
                    return GetP1RightRaw() || GetP2RightRaw();
                }
            }
            return GetP2RightRaw();
        }

        public static bool GetP2Jump()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Jump) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.ConsumeJumpPressed()) return true;
                    return GetP1JumpRaw() || GetP2JumpRaw();
                }
            }
            return GetP2JumpRaw();
        }

        public static bool GetP2Punch()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Punch) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.ConsumePunchPressed()) return true;
                    return GetP1PunchRaw() || GetP2PunchRaw();
                }
            }
            return GetP2PunchRaw();
        }

        public static bool GetP2Kick()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Kick) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.ConsumeKickPressed()) return true;
                    return GetP1KickRaw() || GetP2KickRaw();
                }
            }
            return GetP2KickRaw();
        }

        public static bool GetP2HeavyPunch()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2HeavyPunch) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.ConsumeHeavyPunchPressed()) return true;
                    return GetP1HeavyPunchRaw() || GetP2HeavyPunchRaw();
                }
            }
            return GetP2HeavyPunchRaw();
        }

        public static bool GetP2HeavyKick()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2HeavyKick) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.ConsumeHeavyKickPressed()) return true;
                    return GetP1HeavyKickRaw() || GetP2HeavyKickRaw();
                }
            }
            return GetP2HeavyKickRaw();
        }

        public static bool GetP2Special()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Special) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.ConsumeSpecialPressed()) return true;
                    return GetP1SpecialRaw() || GetP2SpecialRaw();
                }
            }
            return GetP2SpecialRaw();
        }

        public static bool GetP2Grab()
        {
            if (NetworkFightManager.Instance != null && NetworkFightManager.Instance.IsNetworkMatchActive)
            {
                if (NetworkFightManager.Instance.IsHost && NetworkFightManager.Instance.RemoteP2Grab) return true;
                if (NetworkFightManager.Instance.IsClient)
                {
                    if (VirtualTouchControls.ConsumeGrabPressed()) return true;
                    return GetP1GrabRaw() || GetP2GrabRaw();
                }
            }
            return GetP2GrabRaw();
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

        public static bool GetToggleWifiLobby()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.nKey.wasPressedThisFrame || kb.f8Key.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.F8);
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
