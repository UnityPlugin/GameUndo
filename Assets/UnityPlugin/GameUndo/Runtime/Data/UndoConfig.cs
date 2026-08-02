using System;
using UnityEngine;

namespace UnityPlugin.GameUndo
{
    [Serializable]
    public struct UndoConfig
    {
        [Range(32, 128)] public int undoSize;
        public float mergeInterval;
        public bool useInput;
#if UNITY_EDITOR
        public bool useEditorInput;
#endif
    }
}
