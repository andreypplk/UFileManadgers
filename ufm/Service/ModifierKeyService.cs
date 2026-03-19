using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Input;
using System;
using System.Diagnostics;

namespace ufm
{
    public class ModifierKeyService : IModifierKeyService
    {
        #region Поля

        private bool _isCtrlPressed = false;
        private bool _isShiftPressed = false;
        private bool _isAltPressed = false;

        #endregion

        #region Свойства

        public bool IsCtrlPressed => _isCtrlPressed;
        public bool IsShiftPressed => _isShiftPressed;
        public bool IsAltPressed => _isAltPressed;

        #endregion

        #region Публичные методы

        public void UpdateKeyState(VirtualKey key, bool isPressed)
        {
            switch (key)
            {
                case VirtualKey.Control:
                case VirtualKey.LeftControl:
                case VirtualKey.RightControl:
                    _isCtrlPressed = isPressed;
                    break;
                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                case VirtualKey.RightShift:
                    _isShiftPressed = isPressed;
                    break;
                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                case VirtualKey.RightMenu:
                    _isAltPressed = isPressed;
                    break;
            }
        }

        public void UpdateKeyStateFromCore()
        {
            try
            {
                var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
                var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
                var altState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);

                _isCtrlPressed = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
                _isShiftPressed = shiftState.HasFlag(CoreVirtualKeyStates.Down);
                _isAltPressed = altState.HasFlag(CoreVirtualKeyStates.Down);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModifierKeyService] Error updating modifier keys: {ex}");

                var coreWindow = CoreWindow.GetForCurrentThread();
                if (coreWindow != null)
                {
                    _isCtrlPressed = coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                    _isShiftPressed = coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                    _isAltPressed = coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
                }
            }
        }

        public (bool ctrl, bool shift, bool alt) GetCurrentState()
        {
            return (_isCtrlPressed, _isShiftPressed, _isAltPressed);
        }

        #endregion
    }
}