using System.Text;
using UnityEditor;
using UnityEngine;
using UnityPlugin.Bridge;
using UnityPlugin.EditorUtils;

namespace UnityPlugin.GameUndo
{
    [CustomEditor(typeof(GameUndo))]
    public class GameUndoEditor : BaseEditor<GameUndo>
    {
        static Color UNDO_COLOR = Color.white;
        static Color REDO_COLOR = new Color(1, 1, 1, 0.5f);

        static GUILayoutOption COL_SIZE = GUILayout.Width(10);
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var sb = UnityGenericPool<StringBuilder>.Get();
            using (var scope = IMGUI.Foldout("GameUndoEditor_Undo List"))
            {
                scope.name.text = sb.Clear().Append("Undo List (").Append(_target.UndoCount).Append(')').ToString();
                if (scope.fold)
                {
                    using (IMGUI.Indent(-1))
                    {
                        var index = _target.UndoIndex;
                        for (var i = _target.UndoCount - 1; i >= 0; i--)
                        {
                            using (IMGUI.Color(i > index ? REDO_COLOR : UNDO_COLOR))
                            {
                                using (IMGUI.Horizontal())
                                {
                                    EditorGUILayout.LabelField(i == index ? ">" : " ", COL_SIZE);

                                    var num = _target.UndoCount - i;
                                    EditorGUILayout.LabelField(IMGUI.GetGUIContent(num.ToString()), COL_SIZE);

                                    sb.Clear().Append("Undo Label ").Append(i);
                                    var label = IMGUI.GetGUIContent(sb.ToString());
                                    label.text = _target.GetUndoName(i);
                                    EditorGUILayout.LabelField(label);
                                }
                            }
                        }
                    }
                }
            }
            UnityGenericPool<StringBuilder>.Release(sb);
        }
    }
}
