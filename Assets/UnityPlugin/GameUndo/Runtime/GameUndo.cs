using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityPlugin.Bridge;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UnityPlugin.GameUndo
{
    public class GameUndo : Singleton<GameUndo>
    {
        public const int DEFAULT_UNDO_SIZE = 128;

        static Config _defaultConfig = new Config
        {
            undoSize = DEFAULT_UNDO_SIZE,
            useInput = true,
        };
        public static Config DefaultConfig
        {
            get => _defaultConfig;
            set
            {
                _defaultConfig = value;
                if (HasInstance())
                {
                    _instance.undoSize = _defaultConfig.undoSize;
                    _instance.useInput = _defaultConfig.useInput;
                }
            }
        }

        public struct UndoItem
        {
            public string name;
            public Action redoAction;
            public object context;
        }

        [SerializeField, Range(32, 128)] int undoSize = _defaultConfig.undoSize;
        [SerializeField,] bool useInput = _defaultConfig.useInput;

        List<UndoItem> _undoList = new List<UndoItem>();
#if UNITY_EDITOR
        public List<UndoItem> UndoList { get => _undoList; }
#endif

        public static event Action<string, object> OnUndoBefore;
        public static event Action<string, object> OnUndoAfter;

        public static void Push(Action redoAction, object context, string name = null)
        {
            Instance?.PushInner(redoAction, context, name);
        }

        public static void Pop()
        {
            Instance?.PopInner();
        }

        #region Mono

        protected override void OnAwake()
        {
            base.OnAwake();

            if (useInput)
            {
                InputBridge.CheckDefaultInputModule();
            }
        }

        void Update()
        {
            if (useInput && CheckInput())
            {
                PopInner();
            }
        }

        #endregion

        #region Implement

        void PushInner(Action redoAction, object context, string name = null)
        {
            var item = new UndoItem
            {
                name = name,
                redoAction = redoAction,
                context = context,
            };

            if (string.IsNullOrEmpty(name))
            {
                var sb = UnityGenericPool<StringBuilder>.Get();
                sb.Append(context).Append(' ').Append(redoAction);
                item.name = sb.ToString();
                UnityGenericPool<StringBuilder>.Release(sb);
            }

            _undoList.Add(item);

            if (_undoList.Count > undoSize)
            {
                _undoList.RemoveRange(0, _undoList.Count - undoSize);
            }
        }

        void PopInner()
        {
            var index = _undoList.Count - 1;
            if (index < 0) return;

            var item = _undoList[index];

            OnUndoBefore?.Invoke(item.name, item.context);

            try
            {
                item.redoAction?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            _undoList.RemoveAt(index);

            OnUndoAfter?.Invoke(item.name, item.context);
        }

        bool CheckInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.zKey.wasReleasedThisFrame)
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed;
#else
                return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
#endif
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyUp(KeyCode.Z))
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
#else
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
            }
#endif
            return false;
        }

        #endregion
    }
}