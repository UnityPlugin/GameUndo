using System;

namespace UnityPlugin.GameUndo
{
    internal interface IUndoItem : IDisposable
    {
        string Name { get; }
        object Context { get; }
        object Target { get; }
        bool Mergeable { get; }
        void DoGet(bool oldValue);
        void DoSet(bool oldValue);
        bool IsChanged();
        bool Merge(IUndoItem item);
        void Reset();
    }
}
