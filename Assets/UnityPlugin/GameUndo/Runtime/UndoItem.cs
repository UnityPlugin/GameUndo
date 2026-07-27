#if UNITY_EDITOR
#define GAMEUNDO_TO_STRING
#endif

using System;
using UnityEngine;

#if GAMEUNDO_TO_STRING
using System.Text;
using UnityPlugin.Bridge;
#endif


namespace UnityPlugin.GameUndo
{
    internal interface IUndoItem : IDisposable
    {
        string Name { get; }
        object Context { get; }
        bool Mergeable { get; }
        void DoGet(bool oldValue);
        void DoSet(bool oldValue);
        bool Merge(IUndoItem item);
        void Reset();
    }

    internal sealed class UndoObjectItem : IUndoItem
    {
        Action<DynamicObject> _getter;
        Action<DynamicObject> _setter;
        DynamicObject _oldValue = new DynamicObject();
        DynamicObject _newValue = new DynamicObject();

#if GAMEUNDO_TO_STRING
        bool _changed;
        string _str;
#endif

        public string Name { get; private set; }
        public object Context { get; private set; }
        public bool Mergeable { get; private set; }

        public void Setup(string name, Action<DynamicObject> getter, Action<DynamicObject> setter, object context, bool mergeable)
        {
            Name = name;
            Context = context;
            Mergeable = mergeable;

            _getter = getter;
            _setter = setter;

            _oldValue.Clear();
            _newValue.Clear();

            DoGet(true);
            DoGet(false);
        }

        public void DoGet(bool oldValue)
        {
            try
            {
                if (_getter != null)
                {
                    if (oldValue) _getter.Invoke(_oldValue);
                    else _getter.Invoke(_newValue);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void DoSet(bool oldValue)
        {
            try
            {
                if (_setter != null)
                {
                    if (oldValue) _setter.Invoke(_oldValue);
                    else _setter.Invoke(_newValue);
#if GAMEUNDO_TO_STRING
                    _changed = true;
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public bool Merge(IUndoItem item)
        {
            do
            {
                if (item == null) break;
                if (!Mergeable || !item.Mergeable) break;
                if (Name != item.Name) break;
                if (Context != item.Context) break;
                if (!(item is UndoObjectItem tmp)) break;
                if (_getter != tmp._getter) break;
                if (_setter != tmp._setter) break;

                DoGet(false);
                return true;
            } while (false);
            return false;
        }

        public void Reset()
        {
            Name = null;
            Context = null;

            _getter = null;
            _setter = null;

            _oldValue.Clear();
            _newValue.Clear();
        }

        public void Dispose()
        {
            Reset();

            _oldValue = null;
            _newValue = null;
        }

        public override string ToString()
        {
#if GAMEUNDO_TO_STRING
            if (string.IsNullOrEmpty(_str) || _changed)
            {
                var sb = UnityGenericPool<StringBuilder>.Get();
                sb.Clear().Append(Name).Append(' ').Append(_newValue);
                _str = sb.ToString();
                UnityGenericPool<StringBuilder>.Release(sb);
            }
            return _str;
#else
            return Name;
#endif
        }
    }

    internal sealed class UndoValueItem<T> : IUndoItem
    {
        Func<T> _getter;
        Action<T> _setter;
        T _oldValue;
        T _newValue;

#if GAMEUNDO_TO_STRING
        bool _changed;
        string _str;
#endif

        public string Name { get; private set; }
        public object Context { get; private set; }
        public bool Mergeable { get; private set; }

        public void Setup(string name, Func<T> getter, Action<T> setter, object context, bool mergeable)
        {
            Name = name;
            Context = context;
            Mergeable = mergeable;

            _getter = getter;
            _setter = setter;

            DoGet(true);
            DoSet(false);
        }

        public void DoGet(bool oldValue)
        {
            try
            {
                if (_getter != null)
                {
                    if (oldValue) _oldValue = _getter.Invoke();
                    else _newValue = _getter.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void DoSet(bool oldValue)
        {
            try
            {
                if (_setter != null)
                {
                    if (oldValue) _setter.Invoke(_oldValue);
                    else _setter.Invoke(_newValue);
#if GAMEUNDO_TO_STRING
                    _changed = true;
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public bool Merge(IUndoItem item)
        {
            do
            {
                if (item == null) break;
                if (!Mergeable || !item.Mergeable) break;
                if (Name != item.Name) break;
                if (Context != item.Context) break;

                if (!(item is UndoValueItem<T> tmp)) break;
                if (_getter != tmp._getter) break;
                if (_setter != tmp._setter) break;

                DoGet(false);
                return true;
            } while (false);
            return false;
        }

        public void Reset()
        {
            Name = null;
            Context = null;

            _getter = null;
            _setter = null;

            _oldValue = default;
            _newValue = default;
        }

        public void Dispose()
        {
            Reset();
        }

        public override string ToString()
        {
#if GAMEUNDO_TO_STRING
            if (string.IsNullOrEmpty(_str) || _changed)
            {
                var sb = UnityGenericPool<StringBuilder>.Get();
                sb.Clear().Append(Name).Append(' ').Append(_newValue);
                _str = sb.ToString();
                UnityGenericPool<StringBuilder>.Release(sb);
            }
            return _str;
#else
            return Name;
#endif
        }
    }
}
