using System;
using System.Collections.Generic;
using UnityEngine;
using UnityPlugin.Bridge;

namespace UnityPlugin.GameUndo
{
    public partial class GameUndo : Singleton<GameUndo>
    {
        public delegate void GameUndoCallback(object context, string name, object target);

        public const int DEFAULT_UNDO_SIZE = 128;

        static Config _defaultConfig = new Config
        {
            undoSize = DEFAULT_UNDO_SIZE,
            useInput = true,
            mergeInterval = 3.0f
        };
        public static Config DefaultConfig
        {
            get => _defaultConfig;
            set
            {
                _defaultConfig = value;
                if (HasInstance()) _instance.ApplyConfig();
            }
        }

        [SerializeField, Range(32, 128)] int undoSize = _defaultConfig.undoSize;
        [SerializeField] float mergeInterval = _defaultConfig.mergeInterval;
        [SerializeField] bool stopMerge;

        List<IUndoItem> _undoList = new List<IUndoItem>();

        public static event GameUndoCallback OnUndoBefore;
        public static event GameUndoCallback OnUndoAfter;
        public static event GameUndoCallback OnRedoBefore;
        public static event GameUndoCallback OnRedoAfter;

        int _index = 0;
        float _pushTime;

        #region Static

        static IUndoItem _currentRecord;

        static Dictionary<Type, Stack<IUndoItem>> _undoItemPool;

        static T GetUndoItem<T>() where T : class, IUndoItem, new()
        {
            do
            {
                var type = typeof(T);
                if (_undoItemPool == null || !_undoItemPool.ContainsKey(type)) break;
                if (_undoItemPool[type].TryPop(out var result))
                {
                    return result as T;
                }
            } while (false);

            return new T();
        }

        static void ReleaseUndoItem(IUndoItem item)
        {
            if (item == null) return;

            var type = item.GetType();
            if (_undoItemPool == null) _undoItemPool = new Dictionary<Type, Stack<IUndoItem>>();
            if (!_undoItemPool.TryGetValue(type, out var stack))
            {
                stack = new Stack<IUndoItem>();
                _undoItemPool[type] = stack;
            }
            item.Reset();
            stack.Push(item);
        }

        public static bool SetValue<T>(
            T value,
            Func<object, T> getter,
            Action<object, T> setter,
            object context = null,
            object target = null,
            string name = null,
            bool mergeable = true)
        {
            return SetValue(value, new UndoValueParam<T>
            {
                name = name,
                getter = getter,
                setter = setter,
                context = context,
                target = target,
                mergeable = mergeable,
            });
        }

        public static bool SetValue<T>(T value, UndoValueParam<T> param)
        {
            if (string.IsNullOrEmpty(param.name))
            {
                param.name = $"Set Value";
                param.mergeable = false;
            }

            var record = GetUndoItem<UndoValueItem<T>>();
            record.Setup(param);
            record.DoGet(true);

            try
            {
                param.setter?.Invoke(param.target, value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Instance?.PushInner(record);

            return true;
        }

        public static bool RecordValue<T>(
            Func<object, T> getter,
            Action<object, T> setter,
            object context,
            object target = null,
            string name = null,
            bool mergeable = true)
        {
            return RecordValue(new UndoValueParam<T>
            {
                name = name,
                getter = getter,
                setter = setter,
                context = context,
                target = target,
                mergeable = mergeable,
            });
        }

        public static bool RecordValue<T>(UndoValueParam<T> param)
        {
            if (_currentRecord != null) return false;

            if (string.IsNullOrEmpty(param.name))
            {
                param.name = $"Record Value";
                param.mergeable = false;
            }

            var record = GetUndoItem<UndoValueItem<T>>();
            record.Setup(param);

            _currentRecord = record;

            return true;
        }

        public static bool RecordObject(
            Action<object, DynamicObject> getter,
            Action<object, DynamicObject> setter,
            object context,
            object target,
            string name = null,
            bool mergeable = true)
        {
            return RecordObject(new UndoObjectParam
            {
                name = name,
                getter = getter,
                setter = setter,
                context = context,
                target = target,
                mergeable = mergeable,
            });
        }

        public static bool RecordObject(UndoObjectParam param)
        {
            if (_currentRecord != null) return false;

            if (string.IsNullOrEmpty(param.name))
            {
                param.name = $"Record Object";
                param.mergeable = false;
            }

            var record = GetUndoItem<UndoObjectItem>();
            record.Setup(param);

            _currentRecord = record;

            return true;
        }

        public static bool StopRecord(object context = null)
        {
            if (_currentRecord == null || _currentRecord.Context != context) return false;

            Instance?.PushInner(_currentRecord);
            _currentRecord = null;
            return true;
        }

        public static void StopMerge()
        {
            if (HasInstance()) Instance.stopMerge = true;
        }

        public static void Clear()
        {
            Instance?.ClearInner();
        }

        #endregion

        #region Mono

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplyConfig();
        }

        void Update()
        {
            UpdateInput();
        }

        #endregion

        #region Implement

        void ApplyConfig()
        {
            if (useInput) _instance.InitInput();
            if (undoSize < 32) undoSize = 32;

            if (_undoList.Count > undoSize)
            {
                _undoList.RemoveRange(0, _undoList.Count - undoSize);
            }
        }

        void PushInner(IUndoItem item)
        {
            if (item == null) return;

            if (!item.IsChanged())
            {
                ReleaseUndoItem(item);
                return;
            }

            if (_index < _undoList.Count - 1)
            {
                for (var i = _undoList.Count - 1; i > _index; i--)
                {
                    ReleaseUndoItem(_undoList[i]);
                }
                _undoList.RemoveRange(_index + 1, _undoList.Count - _index - 1);
            }

            try
            {
                var time = Time.realtimeSinceStartup;
                if (!stopMerge)
                {
                    if (time - _pushTime < mergeInterval)
                    {
                        if (_undoList.Count > 0)
                        {
                            var index = _undoList.Count - 1;
                            var last = _undoList[index];
                            if (last.Merge(item))
                            {
                                ReleaseUndoItem(item);
                                if (!last.IsChanged())
                                {
                                    ReleaseUndoItem(last);
                                    _undoList.RemoveAt(index);
                                }
                                _pushTime = time;
                                return;
                            }
                        }
                    }
                }

                item.DoGet(false);
                _index = _undoList.Count;
                _undoList.Add(item);
                _pushTime = time;
                stopMerge = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (_undoList.Count > undoSize)
            {
                for (var i = _undoList.Count - undoSize - 1; i >= 0; i--)
                {
                    ReleaseUndoItem(_undoList[i]);
                }
                _undoList.RemoveRange(0, _undoList.Count - undoSize);
            }
        }

        void ClearInner()
        {
            if (_undoList.Count > 0)
            {
                _undoList.Clear();
            }

            if (_undoItemPool != null && _undoItemPool.Count > 0)
            {
                foreach (var pair in _undoItemPool)
                {
                    pair.Value.Clear();
                }
                _undoItemPool.Clear();
            }

            _index = -1;
        }

        void UndoInner()
        {
            if (_undoList.Count < 1) return;
            if (_index < 0) return;

            if (_index > _undoList.Count - 1) _index = _undoList.Count - 1;

            try
            {
                var item = _undoList[_index];
                OnUndoBefore?.Invoke(item.Context, item.Name, item.Target);
                item.DoSet(true);
                OnUndoAfter?.Invoke(item.Context, item.Name, item.Target);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            _index--;
        }

        void RedoInner()
        {
            if (_undoList.Count < 1) return;
            if (_index >= _undoList.Count - 1) return;

            _index++;
            if (_index < 0) _index = 0;

            try
            {
                var item = _undoList[_index];
                OnRedoBefore?.Invoke(item.Context, item.Name, item.Target);
                item.DoSet(false);
                OnRedoAfter?.Invoke(item.Context, item.Name, item.Target);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #endregion

#if UNITY_EDITOR
        public int UndoCount { get => _undoList.Count; }
        public int UndoIndex { get => _index; }

        public string GetUndoName(int index, bool simpleName = false)
        {
            if (index < 0 || index > _undoList.Count) return null;
            var item = _undoList[index];

            if (simpleName) return item.Name;
            return item.ToString();
        }
#endif
    }
}