#if UNITY_EDITOR
#define GAMEUNDO_TO_STRING
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityPlugin.Bridge;

#if GAMEUNDO_TO_STRING
using System.Text;
#endif

namespace UnityPlugin.GameUndo
{
    public class DynamicObject : IDisposable
    {
        protected Dictionary<string, object> _values = UnityGenericPool<Dictionary<string, object>>.Get();
#if GAMEUNDO_TO_STRING
        bool _changed;
        string _str;
#endif

        public object this[string key]
        {
            get => _values != null && _values.TryGetValue(key, out var value) ? value : null;
            set
            {
                if (_values != null)
                {
                    _values[key] = value;
#if GAMEUNDO_TO_STRING
                    _changed = true;
#endif
                }
            }
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
            if (_values != null)
            {
                _values.Clear();
#if GAMEUNDO_TO_STRING
                _changed = true;
#endif
            }
        }

        public void Dispose()
        {
            if (_values != null)
            {
                _values.Clear();
                UnityGenericPool<Dictionary<string, object>>.Release(_values);
                _values = null;
            }
        }

        public bool Equals(DynamicObject obj)
        {
            if (obj == null) return false;
            if (_values == null && obj._values == null) return true;
            if (_values == null || obj._values == null) return false;
            if (_values.Count != obj._values.Count) return false;

            foreach (var pair in _values)
            {
                if (!obj._values.ContainsKey(pair.Key)) return false;
                if (pair.Value != obj._values[pair.Key]) return false;
            }
            return true;
        }

#if GAMEUNDO_TO_STRING
        public override string ToString()
        {
            if (string.IsNullOrEmpty(_str) || _changed)
            {
                using (PoolExt.GetScope<StringBuilder>(out var sb))
                {
                    sb.Clear();
                    foreach (var pair in _values)
                    {
                        sb.Append(pair.Key).Append('(').Append(pair.Value).Append(')').Append(',');
                    }
                    if (sb.Length > 0) sb.Length--;
                    _str = sb.ToString();
                    _changed = false;
                }
            }
            return _str;
        }
#endif
    }
}
