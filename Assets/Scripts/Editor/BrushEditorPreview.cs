using UnityEditor;
using UnityEngine;

namespace Autotiles
{
    //TODO: fix memory leak
    public class BrushEditorPreview
    {
        private const string FillMeshResName  = "editor_brush_fill";
        private const string EmptyMeshResName = "editor_brush_empty";
        private const string UndefMeshResName = "editor_brush_undef";
        private const string MatResName       = "editor_brush_mat";

        private PreviewRenderUtility previewRenderUtility;
        private Vector2 drag;
        private float cameraDistance;
        private Brush brush;

        private Mesh fillMesh;
        private Mesh emptyMesh;
        private Mesh undefMesh;
        private Material mat;

        public BrushEditorPreview(Brush brush)
        {
            this.brush = brush;

            previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.camera.farClipPlane = 200;

            drag = new Vector2(0, -60f);
            cameraDistance = brush.size * 12f;


            LoadResources();
        }

        public void Cleanup()
        {
            previewRenderUtility.Cleanup();
        }

        public void Draw(Rect r, GUIStyle background, int tileIndex)
        {
            previewRenderUtility.BeginPreview(r, background);

            DrawPrefab(brush.tiles[tileIndex].prefabs[0].prefab, Vector3.zero, brush.tiles[tileIndex].rotation);

            for (int i = 0; i < brush.tiles[tileIndex].rule.Length; i++)
            {
                int gridIndex = i > 3 ? i + 1 : i;
                int x = gridIndex % 3;
                int y = gridIndex / 3;

                Matrix4x4 trs = Matrix4x4.TRS(new Vector3((x - 1) * brush.size, 0, (y - 1) * brush.size), Quaternion.identity, Vector3.one * brush.size);
                Mesh mesh = null;

                switch (brush.tiles[tileIndex].rule[i])
                {
                    case Brush.NeighborState.Empty:
                        mesh = emptyMesh;
                        break;
                    case Brush.NeighborState.Filled:
                        mesh = fillMesh;
                        break;
                    case Brush.NeighborState.Undefined:
                        mesh = undefMesh;
                        break;
                }

                previewRenderUtility.DrawMesh(mesh, trs, mat, 0);
            }

            previewRenderUtility.camera.transform.position = Vector3.zero;
            previewRenderUtility.camera.transform.rotation = Quaternion.Euler(new Vector3(-drag.y, -drag.x, 0));
            previewRenderUtility.camera.transform.position = previewRenderUtility.camera.transform.forward * -cameraDistance;
            previewRenderUtility.camera.Render();

            previewRenderUtility.EndAndDrawPreview(r);
        }

        public void Update(Rect r)
        {
            drag = Drag2D(drag, ref cameraDistance, r);
        }

        //preview uses 3 x 3 field
        private bool[] GetNeighbors(int x, int y, Brush.NeighborState[] rule)
        {
            int index = 0;
            bool[] result = new bool[8];

            for (int i = y - 1; i < y + 2; i++)
            {
                for (int j = x - 1; j < x + 2; j++)
                {
                    if (i == y && j == x) continue; //skip the middle tile

                    if ((i >= 0 && i <= 2) && (j >= 0 && j <= 2))
                    {
                        int ruleIndex = j % 2 + i * 3;

                        if (ruleIndex == 4)
                        {
                            result[index] = true;
                        }
                        else
                        {
                            ruleIndex = ruleIndex > 4 ? ruleIndex - 1 : ruleIndex;
                            result[index] = rule[ruleIndex] == Brush.NeighborState.Filled;
                        }
                    }

                    index++;
                }
            }

            return result;
        }

        private void DrawPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return;

            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                var child = prefab.transform.GetChild(i);
                DrawPrefab(child.gameObject, position, rotation);
            }

            var meshFilter = prefab.GetComponent<MeshFilter>();
            var meshRenderer = prefab.GetComponent<MeshRenderer>();

            if (meshFilter != null && meshRenderer != null)
            {
                previewRenderUtility.DrawMesh(meshFilter.sharedMesh, position, prefab.transform.rotation * rotation, meshRenderer.sharedMaterial, 0);
            }
        }

        private static Vector2 Drag2D(Vector2 scrollPosition, ref float zoom, Rect position)
        {
            int controlID = GUIUtility.GetControlID("Slider".GetHashCode(), FocusType.Passive);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && position.width > 50f)
                    {
                        GUIUtility.hotControl = controlID;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        scrollPosition -= current.delta * (float)((!current.shift) ? 1 : 3) / Mathf.Min(position.width, position.height) * 140f;
                        scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
                        current.Use();
                        GUI.changed = true;
                    }
                    break;
                case EventType.ScrollWheel:
                    if (position.Contains(current.mousePosition) && position.width > 50f)
                    {
                        zoom += current.delta.y;
                        current.Use();
                    }
                   break;
            }
            return scrollPosition;
        }

        private void LoadResources()
        {
            string[] fillGuids = AssetDatabase.FindAssets(FillMeshResName);
            string[] emptyGuids = AssetDatabase.FindAssets(EmptyMeshResName);
            string[] undefGuids = AssetDatabase.FindAssets(UndefMeshResName);
            string[] matGuids = AssetDatabase.FindAssets(MatResName);

            fillMesh  = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(fillGuids[0]));
            emptyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(emptyGuids[0]));
            undefMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(undefGuids[0]));
            mat       = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuids[0]));
        }
    }
}
