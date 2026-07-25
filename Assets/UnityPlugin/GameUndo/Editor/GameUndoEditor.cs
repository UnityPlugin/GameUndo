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
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var sb = UnityGenericPool<StringBuilder>.Get();
            using (var scope = IMGUI.Foldout("Undo List"))
            {
                if (scope.fold)
                {
                    var index = _target.UndoIndex;
                    for (var i = _target.UndoCount - 1; i >= 0; i--)
                    {
                        var num = _target.UndoCount - i;
                        sb.Clear().Append("Undo Label ").Append(num);
                        var label = IMGUI.GetGUIContent(sb.ToString());

                        if (i == index)
                        {
                            sb.Clear().AppendFormat("> {0}\t", num).Append(_target.GetUndoName(i));
                            label.text = sb.ToString();
                        }
                        else
                        {
                            sb.Clear().AppendFormat("   {0}\t", num).Append(_target.GetUndoName(i));
                            label.text = sb.ToString();
                        }
                        if (i > index)
                        {
                            using (IMGUI.Color(new Color(1, 1, 1, 0.5f)))
                            {
                                EditorGUILayout.LabelField(label);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField(label);
                        }

                    }
                }
            }
            UnityGenericPool<StringBuilder>.Release(sb);
        }
    }
}
