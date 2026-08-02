using System;
using System.Collections.Generic;
using UnityEngine;
using UnityPlugin.Bridge;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityPlugin.GameUndo
{
    public partial class GameUndo : Singleton<GameUndo>
    {
        static UndoConfig DEFAULT_CONFIG = new UndoConfig
        {
            undoSize = 128,
            useInput = true,
            mergeInterval = 3.0f,
#if UNITY_EDITOR
            useEditorInput = true,
#endif
        };

        [SerializeField] UndoConfig config = DEFAULT_CONFIG;
        [SerializeField] bool stopMerge = false;
        [SerializeField] bool disableRecord = false;

        List<IUndoItem> _undoList = new List<IUndoItem>();

        int _index = -1;
        float _pushTime;

        #region API

        #region Event

        public delegate void GameUndoCallback(object context, string name, object target);

        public static event GameUndoCallback OnUndoBefore;
        public static event GameUndoCallback OnUndoAfter;
        public static event GameUndoCallback OnRedoBefore;
        public static event GameUndoCallback OnRedoAfter;

        #endregion

        public static int UndoCount { get => HasInstance() ? Instance._undoList.Count : 0; }
        public static int UndoIndex { get => HasInstance() ? Instance._index : -1; }
        public static string CurrentRecord { get => _currentRecord == null ? null : _currentRecord.ToString(); }

        public static UndoConfig Config
        {
            get => HasInstance() ? Instance.config : DEFAULT_CONFIG;
            set
            {
                Instance.config = value;
                Instance.ApplyConfig();
            }
        }

        static IUndoItem _currentRecord;

        public static string GetUndoName(int index, bool simpleName = false)
        {
            if (!HasInstance()) return "";

            var undoList = Instance._undoList;
            if (index < 0 || index > undoList.Count) return "";
            var item = undoList[index];

            if (simpleName) return item.Name;
            return item.ToString();
        }

        #region Set Value

        public static bool SetValue<TValue>(
            TValue value,
            Func<TValue> getter,
            Action<TValue> setter)
        {
            if (Instance.disableRecord) return false;

            return SetValue(value, new UndoValueParam<TValue>
            {
                name = null,
                getter = obj => getter.Invoke(),
                setter = (obj, v) => setter.Invoke(v),
                context = null,
                target = null,
                mergeable = false,
            });
        }

        public static bool SetValue<TTarget, TValue>(
            string name,
            object context,
            TTarget target,
            TValue value,
            Func<TTarget, TValue> getter,
            Action<TTarget, TValue> setter,
            bool mergeable = true) where TTarget : class
        {
            if (Instance.disableRecord) return false;

            return SetValue(value, new UndoValueParam<TValue>
            {
                name = name,
                getter = obj => getter.Invoke((TTarget)obj),
                setter = (obj, v) => setter.Invoke((TTarget)obj, v),
                context = context,
                target = target,
                mergeable = mergeable,
            });
        }

        public static bool SetValue<TValue>(
            string name,
            object context,
            object target,
            TValue value,
            Func<object, TValue> getter,
            Action<object, TValue> setter,
            bool mergeable = true)
        {
            if (Instance.disableRecord) return false;

            return SetValue(value, new UndoValueParam<TValue>
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
            if (Instance.disableRecord) return false;

            if (string.IsNullOrEmpty(param.name))
            {
                param.name = $"Set Value";
                param.mergeable = false;
            }

            var record = GetUndoItem<UndoValueItem<T>>();
            record.Setup(param);

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

        #endregion

        #region Record Value

        public static bool RecordValue<TTarget, TValue>(
            string name,
            object context,
            TTarget target,
            Func<TTarget, TValue> getter,
            Action<TTarget, TValue> setter,
            bool mergeable = true) where TTarget : class
        {
            if (Instance.disableRecord) return false;

            return RecordValue(new UndoValueParam<TValue>
            {
                name = name,
                getter = obj => getter.Invoke((TTarget)obj),
                setter = (obj, v) => setter.Invoke((TTarget)obj, v),
                context = context,
                target = target,
                mergeable = mergeable,
            });
        }

        public static bool RecordValue<TValue>(
            string name,
            object context,
            Func<object, TValue> getter,
            Action<object, TValue> setter,
            bool mergeable = true)
        {
            if (Instance.disableRecord) return false;

            return RecordValue(new UndoValueParam<TValue>
            {
                name = name,
                getter = getter,
                setter = setter,
                context = context,
                target = null,
                mergeable = mergeable,
            });
        }

        public static bool RecordValue<T>(UndoValueParam<T> param)
        {
            if (Instance.disableRecord) return false;

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

        #endregion

        #region Set List

        public static bool SetList<TList, TValue>(
            string name,
            object context,
            TList list,
            TValue refItem,
            Action<TList, TValue> before,
            Action<TList, TValue> after) where TList : class
        {
            if (Instance.disableRecord) return false;

            return SetList(new UndoListParam<TValue>
            {
                name = name,
                before = (obj, item) => before.Invoke((TList)obj, item),
                after = (obj, item) => after.Invoke((TList)obj, item),
                context = context,
                target = list,
                refItem = refItem,
            });
        }

        public static bool SetList<T>(UndoListParam<T> param)
        {
            if (Instance.disableRecord) return false;

            if (string.IsNullOrEmpty(param.name))
            {
                param.name = $"Set List";
            }

            var record = GetUndoItem<UndoListItem<T>>();
            record.Setup(param);

            Instance?.PushInner(record);

            return true;
        }

        #endregion

        #region Record Object

        public static bool RecordObject<TTarget>(
            string name,
            object context,
             TTarget target,
            Action<TTarget, DynamicObject> getter,
            Action<TTarget, DynamicObject> setter,
            bool mergeable = true) where TTarget : class
        {
            if (Instance.disableRecord) return false;

            return RecordObject(new UndoObjectParam
            {
                name = name,
                getter = (obj, data) => getter.Invoke((TTarget)obj, data),
                setter = (obj, data) => setter.Invoke((TTarget)obj, data),
                context = context,
                target = target,
                mergeable = mergeable,
            });
        }

        public static bool RecordObject(
            string name,
            object context,
             object target,
            Action<object, DynamicObject> getter,
            Action<object, DynamicObject> setter,
            bool mergeable = true)
        {
            if (Instance.disableRecord) return false;

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
            if (Instance.disableRecord) return false;

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

        #endregion

        #region Other

        public static void DisableRecord(bool value)
        {
            if (value)
            {
                if (_currentRecord != null)
                {
                    ReleaseUndoItem(_currentRecord);
                    _currentRecord = null;
                }
            }

            if (!HasInstance()) return;
            Instance.disableRecord = value;
        }

        public static bool StopRecord(object context = null)
        {
            if (!HasInstance() || Instance.disableRecord)
            {
                if (_currentRecord != null)
                {
                    ReleaseUndoItem(_currentRecord);
                    _currentRecord = null;
                }
                return false;
            }

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

        #endregion

        #region Utils

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
            if (config.useInput) _instance.InitInput();
            if (config.undoSize < 32) config.undoSize = 32;

            if (_undoList.Count > config.undoSize)
            {
                _undoList.RemoveRange(0, _undoList.Count - config.undoSize);
            }
        }

        void PushInner(IUndoItem item)
        {
            if (disableRecord) return;

            if (item == null) return;
            item.DoGet(false);

            if (item.IsChanged())
            {
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
                    var merged = false;
                    if (!stopMerge)
                    {
                        if (time - _pushTime < config.mergeInterval)
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
                                    merged = true;
                                }
                            }
                        }
                    }

                    if (!merged)
                    {
                        _index = _undoList.Count;
                        _undoList.Add(item);
                    }
                    _pushTime = time;
                    stopMerge = false;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

            }
            else
            {
                ReleaseUndoItem(item);
            }

            if (_undoList.Count > config.undoSize)
            {
                for (var i = _undoList.Count - config.undoSize - 1; i >= 0; i--)
                {
                    ReleaseUndoItem(_undoList[i]);
                }
                _undoList.RemoveRange(0, _undoList.Count - config.undoSize);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
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

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        void UndoInner()
        {
            if (disableRecord) return;

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

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        void RedoInner()
        {
            if (disableRecord) return;

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

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        #endregion
    }
}