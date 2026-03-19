using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Input;
using System;
using System.Diagnostics;

namespace ufm
{
    public interface IModifierKeyService
    {
        bool IsCtrlPressed { get; }
        bool IsShiftPressed { get; }
        bool IsAltPressed { get; }

        void UpdateKeyState(VirtualKey key, bool isPressed);
        void UpdateKeyStateFromCore();
        (bool ctrl, bool shift, bool alt) GetCurrentState();
    }
}