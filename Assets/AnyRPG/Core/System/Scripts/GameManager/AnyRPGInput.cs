using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace AnyRPG {
    /// <summary>
    /// Minimal compatibility layer for Unity's new Input System.
    /// Replace legacy UnityEngine.Input calls while keeping existing call sites stable.
    /// </summary>
    public static class AnyRPGInput {
        private const float TriggerPressThreshold = 0.5f;
        private static float lastLeftTriggerValue = 0f;
        private static float lastRightTriggerValue = 0f;

        public static Vector3 mousePosition {
            get {
                Vector2 position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                return new Vector3(position.x, position.y, 0f);
            }
        }

        public static bool anyKeyDown {
            get {
                if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) {
                    return true;
                }
                if (Gamepad.current != null) {
                    if (Gamepad.current.buttonSouth.wasPressedThisFrame
                        || Gamepad.current.buttonEast.wasPressedThisFrame
                        || Gamepad.current.buttonWest.wasPressedThisFrame
                        || Gamepad.current.buttonNorth.wasPressedThisFrame
                        || Gamepad.current.startButton.wasPressedThisFrame
                        || Gamepad.current.selectButton.wasPressedThisFrame
                        || Gamepad.current.leftShoulder.wasPressedThisFrame
                        || Gamepad.current.rightShoulder.wasPressedThisFrame
                        || Gamepad.current.leftStickButton.wasPressedThisFrame
                        || Gamepad.current.rightStickButton.wasPressedThisFrame
                        || Gamepad.current.dpad.up.wasPressedThisFrame
                        || Gamepad.current.dpad.down.wasPressedThisFrame
                        || Gamepad.current.dpad.left.wasPressedThisFrame
                        || Gamepad.current.dpad.right.wasPressedThisFrame) {
                        return true;
                    }
                }
                if (Mouse.current != null) {
                    return Mouse.current.leftButton.wasPressedThisFrame
                        || Mouse.current.rightButton.wasPressedThisFrame
                        || Mouse.current.middleButton.wasPressedThisFrame;
                }
                return false;
            }
        }

        public static float GetAxis(string axisName) {
            switch (axisName) {
                case "Mouse ScrollWheel":
                    return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
                case "Mouse X":
                    return Mouse.current != null ? Mouse.current.delta.ReadValue().x : 0f;
                case "Mouse Y":
                    return Mouse.current != null ? Mouse.current.delta.ReadValue().y : 0f;
                case "LeftAnalogHorizontal":
                    return CombineAxis(Gamepad.current != null ? Gamepad.current.leftStick.x.ReadValue() : 0f,
                        GetKeyboardAxis(Key.A, Key.D, Key.LeftArrow, Key.RightArrow));
                case "LeftAnalogVertical":
                    return CombineAxis(Gamepad.current != null ? Gamepad.current.leftStick.y.ReadValue() : 0f,
                        GetKeyboardAxis(Key.S, Key.W, Key.DownArrow, Key.UpArrow));
                case "RightAnalogHorizontal":
                    return Gamepad.current != null ? Gamepad.current.rightStick.x.ReadValue() : 0f;
                case "RightAnalogVertical":
                    return Gamepad.current != null ? Gamepad.current.rightStick.y.ReadValue() : 0f;
                case "LT":
                    return Gamepad.current != null ? Gamepad.current.leftTrigger.ReadValue() : 0f;
                case "RT":
                    return Gamepad.current != null ? Gamepad.current.rightTrigger.ReadValue() : 0f;
                case "D-Pad Horizontal":
                    return Gamepad.current != null ? Gamepad.current.dpad.x.ReadValue() : 0f;
                case "D-Pad Vertical":
                    return Gamepad.current != null ? Gamepad.current.dpad.y.ReadValue() : 0f;
                default:
                    return 0f;
            }
        }

        public static bool GetKey(KeyCode keyCode) {
            return GetKeyState(keyCode, KeyPressState.Pressed);
        }

        public static bool GetKeyDown(KeyCode keyCode) {
            return GetKeyState(keyCode, KeyPressState.Down);
        }

        public static bool GetKeyUp(KeyCode keyCode) {
            return GetKeyState(keyCode, KeyPressState.Up);
        }

        public static bool GetKey(string keyName) {
            if (TryParseJoystickButton(keyName, out int buttonIndex)) {
                return ReadGamepadButton(buttonIndex, KeyPressState.Pressed);
            }
            return false;
        }

        public static bool GetMouseButton(int button) {
            return GetMouseButtonState(button, KeyPressState.Pressed);
        }

        public static bool GetMouseButtonDown(int button) {
            return GetMouseButtonState(button, KeyPressState.Down);
        }

        public static bool GetMouseButtonUp(int button) {
            return GetMouseButtonState(button, KeyPressState.Up);
        }

        private static float CombineAxis(float gamepadValue, float keyboardValue) {
            if (Mathf.Abs(gamepadValue) >= Mathf.Abs(keyboardValue)) {
                return gamepadValue;
            }
            return keyboardValue;
        }

        private static float GetKeyboardAxis(Key negativePrimary, Key positivePrimary, Key negativeAlt, Key positiveAlt) {
            float value = 0f;
            if (Keyboard.current == null) {
                return value;
            }
            if (Keyboard.current[negativePrimary].isPressed || Keyboard.current[negativeAlt].isPressed) {
                value -= 1f;
            }
            if (Keyboard.current[positivePrimary].isPressed || Keyboard.current[positiveAlt].isPressed) {
                value += 1f;
            }
            return Mathf.Clamp(value, -1f, 1f);
        }

        private static bool GetKeyState(KeyCode keyCode, KeyPressState state) {
            if (keyCode == KeyCode.None) {
                return false;
            }
            if (TryReadMouseButton(keyCode, state, out bool mouseResult)) {
                return mouseResult;
            }
            if (TryReadGamepadButton(keyCode, state, out bool gamepadResult)) {
                return gamepadResult;
            }
            if (Keyboard.current == null) {
                return false;
            }
            if (!TryMapKey(keyCode, out Key key) || key == Key.None) {
                return false;
            }
            KeyControl keyControl;
            try {
                keyControl = Keyboard.current[key];
            } catch (System.ArgumentOutOfRangeException) {
                return false;
            }
            if (keyControl == null) {
                return false;
            }
            switch (state) {
                case KeyPressState.Down:
                    return keyControl.wasPressedThisFrame;
                case KeyPressState.Up:
                    return keyControl.wasReleasedThisFrame;
                default:
                    return keyControl.isPressed;
            }
        }

        private static bool GetMouseButtonState(int button, KeyPressState state) {
            if (Mouse.current == null) {
                return false;
            }
            ButtonControl control = null;
            switch (button) {
                case 0:
                    control = Mouse.current.leftButton;
                    break;
                case 1:
                    control = Mouse.current.rightButton;
                    break;
                case 2:
                    control = Mouse.current.middleButton;
                    break;
                default:
                    return false;
            }
            switch (state) {
                case KeyPressState.Down:
                    return control.wasPressedThisFrame;
                case KeyPressState.Up:
                    return control.wasReleasedThisFrame;
                default:
                    return control.isPressed;
            }
        }

        private static bool TryReadMouseButton(KeyCode keyCode, KeyPressState state, out bool result) {
            result = false;
            switch (keyCode) {
                case KeyCode.Mouse0:
                    result = GetMouseButtonState(0, state);
                    return true;
                case KeyCode.Mouse1:
                    result = GetMouseButtonState(1, state);
                    return true;
                case KeyCode.Mouse2:
                    result = GetMouseButtonState(2, state);
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryReadGamepadButton(KeyCode keyCode, KeyPressState state, out bool result) {
            result = false;
            int buttonIndex = -1;
            if (keyCode >= KeyCode.JoystickButton0 && keyCode <= KeyCode.JoystickButton19) {
                buttonIndex = keyCode - KeyCode.JoystickButton0;
            } else if (keyCode >= KeyCode.Joystick1Button0 && keyCode <= KeyCode.Joystick1Button19) {
                buttonIndex = keyCode - KeyCode.Joystick1Button0;
            }
            if (buttonIndex < 0) {
                return false;
            }
            result = ReadGamepadButton(buttonIndex, state);
            return true;
        }

        private static bool ReadGamepadButton(int buttonIndex, KeyPressState state) {
            if (Gamepad.current == null) {
                return false;
            }
            ButtonControl control = null;
            switch (buttonIndex) {
                case 0:
                    control = Gamepad.current.buttonSouth;
                    break;
                case 1:
                    control = Gamepad.current.buttonEast;
                    break;
                case 2:
                    control = Gamepad.current.buttonWest;
                    break;
                case 3:
                    control = Gamepad.current.buttonNorth;
                    break;
                case 4:
                    control = Gamepad.current.leftShoulder;
                    break;
                case 5:
                    control = Gamepad.current.rightShoulder;
                    break;
                case 6:
                    control = Gamepad.current.selectButton;
                    break;
                case 7:
                    control = Gamepad.current.startButton;
                    break;
                case 8:
                    control = Gamepad.current.leftStickButton;
                    break;
                case 9:
                    control = Gamepad.current.rightStickButton;
                    break;
                case 10:
                    control = Gamepad.current.dpad.up;
                    break;
                case 11:
                    control = Gamepad.current.dpad.right;
                    break;
                case 12:
                    control = Gamepad.current.dpad.down;
                    break;
                case 13:
                    control = Gamepad.current.dpad.left;
                    break;
                case 14:
                    return ReadTrigger(Gamepad.current.leftTrigger, true, state);
                case 15:
                    return ReadTrigger(Gamepad.current.rightTrigger, false, state);
                default:
                    return false;
            }
            if (control == null) {
                return false;
            }
            switch (state) {
                case KeyPressState.Down:
                    return control.wasPressedThisFrame;
                case KeyPressState.Up:
                    return control.wasReleasedThisFrame;
                default:
                    return control.isPressed;
            }
        }

        private static bool ReadTrigger(AxisControl trigger, bool isLeft, KeyPressState state) {
            if (trigger == null) {
                return false;
            }
            float value = trigger.ReadValue();
            float previousValue = isLeft ? lastLeftTriggerValue : lastRightTriggerValue;
            if (isLeft) {
                lastLeftTriggerValue = value;
            } else {
                lastRightTriggerValue = value;
            }
            switch (state) {
                case KeyPressState.Down:
                    return value >= TriggerPressThreshold && previousValue < TriggerPressThreshold;
                case KeyPressState.Up:
                    return value < TriggerPressThreshold && previousValue >= TriggerPressThreshold;
                default:
                    return value >= TriggerPressThreshold;
            }
        }

        private static bool TryParseJoystickButton(string keyName, out int buttonIndex) {
            buttonIndex = -1;
            if (string.IsNullOrEmpty(keyName)) {
                return false;
            }
            string trimmed = keyName.Trim().ToLowerInvariant();
            const string prefix = "joystick button ";
            if (!trimmed.StartsWith(prefix, System.StringComparison.Ordinal)) {
                return false;
            }
            string number = trimmed.Substring(prefix.Length);
            return int.TryParse(number, out buttonIndex);
        }

        private static bool TryMapKey(KeyCode keyCode, out Key key) {
            switch (keyCode) {
                case KeyCode.Return:
                    key = Key.Enter;
                    return true;
                case KeyCode.BackQuote:
                    key = Key.Backquote;
                    return true;
                case KeyCode.LeftControl:
                    key = Key.LeftCtrl;
                    return true;
                case KeyCode.RightControl:
                    key = Key.RightCtrl;
                    return true;
                case KeyCode.LeftShift:
                    key = Key.LeftShift;
                    return true;
                case KeyCode.RightShift:
                    key = Key.RightShift;
                    return true;
                case KeyCode.LeftAlt:
                    key = Key.LeftAlt;
                    return true;
                case KeyCode.RightAlt:
                    key = Key.RightAlt;
                    return true;
                case KeyCode.Alpha0:
                    key = Key.Digit0;
                    return true;
                case KeyCode.Alpha1:
                    key = Key.Digit1;
                    return true;
                case KeyCode.Alpha2:
                    key = Key.Digit2;
                    return true;
                case KeyCode.Alpha3:
                    key = Key.Digit3;
                    return true;
                case KeyCode.Alpha4:
                    key = Key.Digit4;
                    return true;
                case KeyCode.Alpha5:
                    key = Key.Digit5;
                    return true;
                case KeyCode.Alpha6:
                    key = Key.Digit6;
                    return true;
                case KeyCode.Alpha7:
                    key = Key.Digit7;
                    return true;
                case KeyCode.Alpha8:
                    key = Key.Digit8;
                    return true;
                case KeyCode.Alpha9:
                    key = Key.Digit9;
                    return true;
                case KeyCode.Keypad0:
                    key = Key.Numpad0;
                    return true;
                case KeyCode.Keypad1:
                    key = Key.Numpad1;
                    return true;
                case KeyCode.Keypad2:
                    key = Key.Numpad2;
                    return true;
                case KeyCode.Keypad3:
                    key = Key.Numpad3;
                    return true;
                case KeyCode.Keypad4:
                    key = Key.Numpad4;
                    return true;
                case KeyCode.Keypad5:
                    key = Key.Numpad5;
                    return true;
                case KeyCode.Keypad6:
                    key = Key.Numpad6;
                    return true;
                case KeyCode.Keypad7:
                    key = Key.Numpad7;
                    return true;
                case KeyCode.Keypad8:
                    key = Key.Numpad8;
                    return true;
                case KeyCode.Keypad9:
                    key = Key.Numpad9;
                    return true;
            }

            if (!System.Enum.TryParse(keyCode.ToString(), out key)) {
                return false;
            }
            return System.Enum.IsDefined(typeof(Key), key) && key != Key.None;
        }

        private enum KeyPressState {
            Pressed,
            Down,
            Up
        }
    }
}
