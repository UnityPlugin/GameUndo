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
    public struct UndoObjectParam
    {
        public string name;
        public Action<object, DynamicObject> getter;
        public Action<object, DynamicObject> setter;
        public object context;
        public object target;
        public bool mergeable;
    }

    internal sealed class UndoObjectItem : IUndoItem
    {
        Action<object, DynamicObject> _getter;
        Action<object, DynamicObject> _setter;
        DynamicObject _oldValue = new DynamicObject();
        DynamicObject _newValue = new DynamicObject();

#if GAMEUNDO_TO_STRING
        bool _changed;
        string _str;
#endif

        public string Name { get; private set; }
        public object Context { get; private set; }
        public object Target { get; private set; }
        public bool Mergeable { get; private set; }

        public void Setup(UndoObjectParam param)
        {
            Name = param.name;
            Context = param.context;
            Target = param.target;
            Mergeable = param.mergeable;

            _getter = param.getter;
            _setter = param.setter;

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
                    if (oldValue) _getter.Invoke(Target, _oldValue);
                    else _getter.Invoke(Target, _newValue);
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
                    if (oldValue) _setter.Invoke(Target, _oldValue);
                    else _setter.Invoke(Target, _newValue);
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

        public bool IsChanged()
        {
            if (_newValue == null && _oldValue == null) return false;
            if (_newValue == null || _oldValue == null) return true;

            return !_newValue.Equals(_oldValue);
        }

        public bool Merge(IUndoItem item)
        {
            do
            {
                if (item == null) break;
                if (!Mergeable || !item.Mergeable) break;
                if (Name != item.Name) break;
                if (Context != item.Context) break;
                if (Target != item.Target) break;

                if (!(item is UndoObjectItem)) break;
                // if (_getter != tmp._getter) break;
                // if (_setter != tmp._setter) break;

                DoGet(false);
                return true;
            } while (false);
            return false;
        }

        public void Reset()
        {
            Name = null;
            Context = null;
            Target = null;

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
                var targetStr = Target == null ? "Null" : Target.GetType().Name;
                var contextStr = Context == null ? "Null" : Context.GetType().Name;
                var sb = UnityGenericPool<StringBuilder>.Get();
                sb.Clear()
                .Append(Name)
                .Append(" [").Append(targetStr).Append('@').Append(contextStr).Append("] : ")
                .Append(_oldValue).Append(" -> ").Append(_newValue);
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
