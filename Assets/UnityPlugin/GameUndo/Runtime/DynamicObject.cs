using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPlugin.GameUndo
{
    public class DynamicObject : IDisposable
    {
        protected Dictionary<string, object> _values = new Dictionary<string, object>();

        public object this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set => _values[key] = value;
        }

        public T Get<T>(string key)
        {
            try
            {
                return (T)this[key];
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return default;
        }

        public void Clear()
        {
            _values.Clear();
        }

        public void Dispose()
        {
            _values.Clear();
            _values = null;
        }
    }
}
