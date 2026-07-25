using System;
using UnityEngine;

namespace UnityPlugin.GameUndo
{
    internal interface IUndoItem : IDisposable
    {
        string Name { get; }
        object Context { get; }
        void DoGet(bool oldValue);
        void DoSet(bool oldValue);
        void Reset();
    }

    internal sealed class UndoObjectItem : IUndoItem
    {
        Action<DynamicObject> _getter;
        Action<DynamicObject> _setter;
        DynamicObject _oldValue = new DynamicObject();
        DynamicObject _newValue = new DynamicObject();

        public string Name { get; private set; }
        public object Context { get; private set; }

        public void Setup(string name, Action<DynamicObject> getter, Action<DynamicObject> setter, object context)
        {
            Name = name;
            Context = context;
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
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
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
    }

    internal sealed class UndoValueItem<T> : IUndoItem
    {
        Func<T> _getter;
        Action<T> _setter;
        T _oldValue;
        T _newValue;

        public string Name { get; private set; }
        public object Context { get; private set; }

        public void Setup(string name, Func<T> getter, Action<T> setter, object context)
        {
            Name = name;
            Context = context;
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
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
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
    }
}
