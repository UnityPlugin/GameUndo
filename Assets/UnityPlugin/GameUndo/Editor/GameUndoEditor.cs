using UnityEditor;
using UnityPlugin.EditorUtils;

namespace UnityPlugin.GameUndo
{
    [CustomEditor(typeof(GameUndo))]
    public class GameUndoEditor : BaseEditor<GameUndo>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (IMGUI.Foldout("Undo List"))
            {
                var list = _target.UndoList;
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.LabelField(IMGUI.GetGUIContent(list[i].name));
                }
            }
        }
    }
}
