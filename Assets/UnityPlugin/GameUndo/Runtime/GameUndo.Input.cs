using System;
using UnityEngine;
using UnityPlugin.Bridge;

namespace UnityPlugin.GameUndo
{
    public partial class GameUndo : Singleton<GameUndo>
    {
        InputBridge.KeyButton _keyZ;
        InputBridge.KeyButton _keyY;
        InputBridge.KeyButton _keyLeftShift;
        InputBridge.KeyButton _keyRightShift;
        InputBridge.KeyButton _keyLeftCtrl;
        InputBridge.KeyButton _keyRightCtrl;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        InputBridge.KeyButton _keyLeftCommand;
        InputBridge.KeyButton _keyRightCommand;
#endif

#if UNITY_EDITOR
        InputBridge.KeyButton _keyLeftAlt;
        InputBridge.KeyButton _keyRightAlt;
#endif

        void InitInput()
        {
            InputBridge.CheckDefaultInputModule();

            _keyZ = InputBridge.GetKeyButton(KeyCode.Z);
            _keyY = InputBridge.GetKeyButton(KeyCode.Y);
            _keyLeftShift = InputBridge.GetKeyButton(KeyCode.LeftShift);
            _keyRightShift = InputBridge.GetKeyButton(KeyCode.RightShift);
            _keyLeftCtrl = InputBridge.GetKeyButton(KeyCode.LeftControl);
            _keyRightCtrl = InputBridge.GetKeyButton(KeyCode.RightControl);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            _keyLeftCommand = InputBridge.GetKeyButton(KeyCode.LeftCommand);
            _keyRightCommand = InputBridge.GetKeyButton(KeyCode.RightCommand);
#endif
#if UNITY_EDITOR
            _keyLeftAlt = InputBridge.GetKeyButton(KeyCode.LeftAlt);
            _keyRightAlt = InputBridge.GetKeyButton(KeyCode.RightAlt);
#endif
        }

        void UpdateInput()
        {
            if (disableRecord) return;
            try
            {
                if (config.useInput)
                {
                    if (_keyZ.WasPressedThisFrame())
                    {
                        if (IsMainModifierKeyPressed())
                        {
                            if (IsShiftPressed()) RedoInner();
                            else UndoInner();
                        }
                    }
                    else if (_keyY.WasPressedThisFrame())
                    {
                        if (IsMainModifierKeyPressed()) RedoInner();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                config.useInput = false;
            }
        }

        bool IsMainModifierKeyPressed()
        {
#if UNITY_EDITOR
            if (config.useEditorInput)
            {
                return _keyLeftAlt.IsPressed() || _keyRightAlt.IsPressed();
            }
#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return _keyLeftCommand.IsPressed() || _keyRightCommand.IsPressed();
#else
            return _keyLeftCtrl.IsPressed() || _keyRightCtrl.IsPressed();
#endif
        }

        bool IsShiftPressed()
        {
            return _keyLeftShift.IsPressed() || _keyRightShift.IsPressed();
        }
    }
}
