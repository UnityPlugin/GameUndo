using System;
using UnityEngine;
using UnityPlugin.Bridge;

namespace UnityPlugin.GameUndo
{
    public partial class GameUndo : Singleton<GameUndo>
    {
        [SerializeField,] bool useInput = _defaultConfig.useInput;

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
        }

        void UpdateInput()
        {
            try
            {
                if (useInput)
                {
                    if (_keyZ.WasPressedThisFrame())
                    {
                        if (IsCtrlPressed())
                        {
                            if (IsShiftPressed()) RedoInner();
                            else UndoInner();
                        }
                    }
                    else if (_keyY.WasPressedThisFrame())
                    {
                        if (IsCtrlPressed()) RedoInner();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                useInput = false;
            }
        }

        bool IsCtrlPressed()
        {
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
