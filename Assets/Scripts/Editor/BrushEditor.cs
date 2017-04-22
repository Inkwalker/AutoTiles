using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Autotiles
{
    [CustomEditor(typeof(Brush))]
    public class BrushEditor : Editor
    {
        private ReorderableList list;
        private BrushEditorPreview brushPreview;

        private void OnEnable()
        {
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("tiles"), true, true, true, true);
            list.drawElementCallback += OnDrawListElement;
            list.elementHeightCallback += (i) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(i);
                var prefabs = element.FindPropertyRelative("prefabs");
                return Mathf.Max(100, (prefabs.arraySize + 1) * (EditorGUIUtility.singleLineHeight + 2) + 13);
            };
            list.onSelectCallback += (l) => { };
            list.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                var prefabs = element.FindPropertyRelative("prefabs");
                rect.height = Mathf.Max(100, (prefabs.arraySize + 1) * (EditorGUIUtility.singleLineHeight + 2) + 13);
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color(0.33f, 0.66f, 1f, 0.66f));
                tex.Apply();
                if (active)
                    GUI.DrawTexture(rect, tex as Texture);
            };
            list.drawHeaderCallback += (r) =>
            {
                EditorGUI.LabelField(r, "Tiles");
            };
            list.onAddCallback += (ReorderableList l) =>
            {
                l.serializedProperty.arraySize++;
                l.serializedProperty.GetArrayElementAtIndex(l.count - 1).FindPropertyRelative("rule").arraySize = 8;
            };

            brushPreview = new BrushEditorPreview(target as Brush);
        }

        void OnDisable()
        {
            brushPreview.Cleanup();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var sizeField = serializedObject.FindProperty("size");
            sizeField.floatValue = EditorGUILayout.FloatField("Size", sizeField.floatValue);

            var groupField = serializedObject.FindProperty("group");
            groupField.stringValue = EditorGUILayout.TextField("Group", groupField.stringValue);

            var interactGroupsField = serializedObject.FindProperty("interactGroups");
            EditorGUILayout.PropertyField(interactGroupsField, true);

            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnDrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var prefabs = element.FindPropertyRelative("prefabs");
            var rule = element.FindPropertyRelative("rule");
            var rotation = element.FindPropertyRelative("rotation");

            Texture2D preview = null;

            if (prefabs.arraySize > 0)
            {
                var previewPrefab = prefabs.GetArrayElementAtIndex(0).FindPropertyRelative("prefab");
                preview = AssetPreview.GetAssetPreview(previewPrefab.objectReferenceValue);
            }

            var elementRect  = new Rect(rect.x, rect.y + 2, rect.width, Mathf.Max(100 - 4, (prefabs.arraySize + 1) * (EditorGUIUtility.singleLineHeight + 2) + 8));
            var previewRect  = new Rect(elementRect.x + 5, elementRect.y + 5, 85, 85);
            var togglesRect  = new Rect(previewRect.xMax + 5, elementRect.y + 5, 85, 85);
            var rotationRect = new Rect(togglesRect.xMax + 5, elementRect.y + 5, elementRect.width - togglesRect.width - previewRect.width - 20, EditorGUIUtility.singleLineHeight);
            var prefabsRect  = new Rect(togglesRect.xMax + 5, rotationRect.yMax + 5, elementRect.width - togglesRect.width - previewRect.width - 20, elementRect.height - rotationRect.height - 15);

            GUI.Box(elementRect, GUIContent.none);
            if (preview != null) //might be null in some cases
            {
                EditorGUI.DrawPreviewTexture(previewRect, preview);
            }
            else
            {
                GUI.Box(previewRect, GUIContent.none);
            }

            rotation.quaternionValue = Quaternion.AngleAxis(EditorGUI.FloatField(rotationRect, "Rotation:", rotation.quaternionValue.eulerAngles.y), Vector3.up);

            for (int i = 0; i < prefabs.arraySize; i++)
            {
                Rect r = new Rect(prefabsRect.x, prefabsRect.y + i * (EditorGUIUtility.singleLineHeight + 2), prefabsRect.width, EditorGUIUtility.singleLineHeight);
                Rect pr = new Rect(r.x, r.y, r.width - r.height - 10 - 25, r.height);
                Rect wr = new Rect(pr.xMax + 5, r.y, 25, r.height);
                Rect br = new Rect(r.xMax - r.height, r.y, r.height, r.height);
                var prefab = prefabs.GetArrayElementAtIndex(i);
                var weight = prefab.FindPropertyRelative("weight");

                EditorGUI.ObjectField(pr, prefab.FindPropertyRelative("prefab"), GUIContent.none);
                weight.floatValue = EditorGUI.FloatField(wr, weight.floatValue);
                if (GUI.Button(br, "-"))
                {
                    prefabs.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            Rect addButtonRect = new Rect(
                prefabsRect.x,
                prefabsRect.y + prefabs.arraySize * (EditorGUIUtility.singleLineHeight + 2),
                prefabsRect.width - EditorGUIUtility.singleLineHeight - 23,
                EditorGUIUtility.singleLineHeight
            );

            if (GUI.Button(addButtonRect, "Add"))
            {
                prefabs.arraySize++;
            }

            for (int i = 0; i < 9; i++)
            {
                if (i == 4) continue; //skip the button in the center

                float x = togglesRect.width / 3 * (i % 3)  + togglesRect.x;
                float y = togglesRect.yMax - togglesRect.height / 3 * (i / 3 + 1);
                float size = togglesRect.width / 3f;

                int j = i > 4 ? i - 1 : i;

                var n = rule.GetArrayElementAtIndex(j);
                int state = n.enumValueIndex;

                string text = "?";
                switch (state)
                {
                    case 1:
                        text = "";
                        break;
                    case 2:
                        text = "*";
                        break;
                }
                if (GUI.Button(new Rect(x, y, size, size), text))
                {
                    state++;
                    if (state > 2) state = 0;
                    rule.GetArrayElementAtIndex(j).enumValueIndex = state;
                }
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            brushPreview.Update(r);

            if (Event.current.type == EventType.Repaint)
            {
                if (list.index > -1)
                {
                    brushPreview.Draw(r, background, list.index);
                }
            }
        }
    }
}
