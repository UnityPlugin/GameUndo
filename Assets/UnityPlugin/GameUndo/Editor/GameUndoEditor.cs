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

            var current = GameUndo.CurrentRecord;
            if (!string.IsNullOrEmpty(current))
            {
                EditorGUILayout.Space();
                EditorGUILayout.PrefixLabel(IMGUI.TmpC("Recording"));
                using (IMGUI.Indent())
                {
                    EditorGUILayout.LabelField(IMGUI.TmpC(current));
                }
            }

            EditorGUILayout.Space();
            var sb = UnityGenericPool<StringBuilder>.Get();
            using (var scope = IMGUI.Foldout("GameUndoEditor_Undo List"))
            {
                scope.name.text = sb.Clear().Append("Undo List (").Append(GameUndo.UndoCount).Append(')').ToString();
                if (scope.fold)
                {
                    using (IMGUI.Indent(-1))
                    {
                        var index = GameUndo.UndoIndex;
                        for (var i = GameUndo.UndoCount - 1; i >= 0; i--)
                        {
                            using (IMGUI.Color(i > index ? REDO_COLOR : UNDO_COLOR))
                            {
                                using (IMGUI.Horizontal())
                                {
                                    EditorGUILayout.LabelField(IMGUI.TmpC(i == index ? ">" : " "), COL_SIZE);

                                    var num = GameUndo.UndoCount - i;
                                    EditorGUILayout.LabelField(IMGUI.TmpC(num.ToString()), COL_SIZE);
                                    var content = GameUndo.GetUndoName(i);
                                    EditorGUILayout.LabelField(IMGUI.TmpC(content, null, content));
                                }
                            }
                        }
                    }
                }
            }
            UnityGenericPool<StringBuilder>.Release(sb);

            EditorGUILayout.Space();
            if (GUILayout.Button("Clear"))
            {
                GameUndo.Clear();
            }
        }
    }
}
