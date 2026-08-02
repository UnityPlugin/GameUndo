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
    public struct UndoListParam<T>
    {
        public string name;
        public Action<object, T> before;
        public Action<object, T> after;
        public object context;
        public object target;
        public T refItem;
    }

    internal sealed class UndoListItem<T> : IUndoItem
    {
        Action<object, T> _before;
        Action<object, T> _after;

        T _refItem;

#if GAMEUNDO_TO_STRING
        bool _changed;
        string _str;
#endif

        public string Name { get; private set; }
        public object Context { get; private set; }
        public bool Mergeable { get; private set; }
        public object Target { get; private set; }

        public void Setup(UndoListParam<T> param)
        {
            Name = param.name;
            Context = param.context;
            Target = param.target;
            Mergeable = false;

            _refItem = param.refItem;
            _before = param.before;
            _after = param.after;
        }

        public void DoGet(bool oldValue) { }

        public void DoSet(bool oldValue)
        {
            try
            {
                if (oldValue) _before?.Invoke(Target, _refItem);
                else _after?.Invoke(Target, _refItem);
#if GAMEUNDO_TO_STRING
                _changed = true;
#endif
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public bool IsChanged() => true;


        public bool Merge(IUndoItem item)
        {
            do
            {
                if (item == null) break;
                if (!Mergeable || !item.Mergeable) break;
                if (Name != item.Name) break;
                if (Context != item.Context) break;
                if (Target != item.Target) break;

                if (!(item is UndoListItem<T> tmp)) break;
                if (_refItem.Equals(tmp._refItem)) break;

                return true;
            } while (false);
            return false;
        }

        public void Reset()
        {
            Name = null;
            Context = null;
            Target = null;

            _before = null;
            _after = null;

            _refItem = default;
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
                var targetStr = Target == null ? "Null" : Target.GetType().Name;
                var contextStr = Context == null ? "Null" : Context.GetType().Name;
                var sb = UnityGenericPool<StringBuilder>.Get();
                sb.Clear()
                .Append(Name)
                .Append(" [").Append(targetStr).Append('@').Append(contextStr).Append("] : ")
                .Append(_refItem);
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
