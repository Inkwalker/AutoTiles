﻿using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace Autotiles
{
    [CustomEditor(typeof(Tilemap3D))]
    public class TilemapEditor : Editor
    {
        private static Rect toolbarRect = new Rect(10, 30, 100, 500);

        private static Brush3D activeBrush;

        private Tilemap3D tilemap;
        private Vector2 size;

        private EditorMode mode;
        private Tool currentTool;

        private Rect? selectionRect;

        void OnEnable()
        {
            tilemap = target as Tilemap3D;
            size = new Vector2(tilemap.Width, tilemap.Height);

            EditorApplication.update += Update;

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (tilemap != null)
                TilemapEditorUtils.SetSelectionState(tilemap.gameObject, EditorSelectedRenderState.Highlight);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            float tileSize = EditorGUILayout.FloatField("Tile size", tilemap.TileSize);
            if (EditorGUI.EndChangeCheck())
            {
                ChangeTilesSize(tileSize);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("Width: {0}    Height: {1}", tilemap.Width, tilemap.Height));
            if (GUILayout.Button(mode == EditorMode.Resize ? "Apply" : "Resize"))
            {
                if (mode == EditorMode.Resize)
                {
                    ResizeTilemap((int)size.x, (int)size.y);
                    mode = EditorMode.View;
                }
                else
                {
                    size = new Vector2(tilemap.Width, tilemap.Height);
                    mode = EditorMode.Resize;

                    TilemapEditorUtils.SetSelectionState(tilemap.gameObject, EditorSelectedRenderState.Hidden);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(mode == EditorMode.Edit ? "Exit edit mode" : "Edit"))
            {
                ToggleEditMode();
            }

            if (GUILayout.Button("Update"))
            {
                tilemap.Rebuild();
                TilemapEditorUtils.SetSelectionState(tilemap.gameObject, EditorSelectedRenderState.Hidden);
            }

        }

        void OnSceneGUI()
        {
            if (mode == EditorMode.Edit)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                HandleMouseEvents();

                GUI.Window(GUIUtility.GetControlID(FocusType.Passive), toolbarRect, ToolbarWindow, "Toolbar");
            }
            if (mode == EditorMode.Resize)
            {
                HandleResize();
            }

            DrawTilemapGrid();
        }

        private void Update()
        {
        }

        private void ToolbarWindow(int id)
        {
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                activeBrush = EditorGUIUtility.GetObjectPickerObject() as Brush3D;
            }

            var contentRec = new Rect(
                5,
                5 + EditorGUIUtility.singleLineHeight,
                toolbarRect.width - 10,
                toolbarRect.height - EditorGUIUtility.singleLineHeight - 10
            );
            var previewRect = new Rect(contentRec.x, contentRec.y, contentRec.width, contentRec.width);
            var brushRect = new Rect(contentRec.x, previewRect.yMax + 5, contentRec.width, EditorGUIUtility.singleLineHeight);
            var eraseRect = new Rect(contentRec.x, brushRect.yMax + 5, contentRec.width, EditorGUIUtility.singleLineHeight);
            var pickRect = new Rect(contentRec.x, eraseRect.yMax + 5, contentRec.width, EditorGUIUtility.singleLineHeight);
            var doneButtonRect = new Rect(contentRec.x, pickRect.yMax + 5 + EditorGUIUtility.singleLineHeight, contentRec.width, EditorGUIUtility.singleLineHeight);
            var cursorPosRect = new Rect(contentRec.x, contentRec.yMax - EditorGUIUtility.singleLineHeight, contentRec.width, EditorGUIUtility.singleLineHeight);
            var clearButtonRect = new Rect(contentRec.x, cursorPosRect.y - EditorGUIUtility.singleLineHeight - 5, contentRec.width, EditorGUIUtility.singleLineHeight);

            var brushNameRect = new Rect(previewRect.x + 5, previewRect.yMax - 5 - EditorGUIUtility.singleLineHeight, previewRect.width - 10, EditorGUIUtility.singleLineHeight);

            GUIContent buttonContent;
            if (activeBrush != null)
            {
                var preview = AssetPreview.GetAssetPreview(activeBrush);
                if (preview == null)
                {
                    if (activeBrush.tiles.Count > 0)
                    {
                        preview = AssetPreview.GetAssetPreview(activeBrush.tiles[0].prefabs[0].prefab);
                    }
                }
                if (preview != null)
                {
                    buttonContent = new GUIContent(preview);
                }
                else
                {
                    buttonContent = new GUIContent(activeBrush.name);
                }
            }
            else
            {
                buttonContent = new GUIContent("Select brush");
            }

            if (GUI.Button(previewRect, buttonContent))
            {
                EditorGUIUtility.ShowObjectPicker<Brush3D>(activeBrush, false, "", EditorGUIUtility.GetControlID(FocusType.Passive));
            }

            if (activeBrush != null)
            {
                GUI.Label(brushNameRect, activeBrush.name);
            }

            if (GUI.Toggle(brushRect, currentTool == Tool.Brush, "Brush", "Button"))
            {
                currentTool = Tool.Brush;
            }
            else
            {
                if (currentTool == Tool.Brush)
                {
                    currentTool = Tool.Brush;
                }
            }

            if (GUI.Toggle(eraseRect, currentTool == Tool.Erase, "Erase", "Button"))
            {
                currentTool = Tool.Erase;
            }
            else
            {
                if (currentTool == Tool.Erase)
                {
                    currentTool = Tool.Brush;
                }
            }

            if (GUI.Toggle(pickRect, currentTool == Tool.Pick, "Pick", "Button"))
            {
                currentTool = Tool.Pick;
            }
            else
            {
                if (currentTool == Tool.Pick)
                {
                    currentTool = Tool.Brush;
                }
            }

            if (GUI.Button(doneButtonRect, "Done"))
            {
                ToggleEditMode();
            }

            if (GUI.Button(clearButtonRect, "Clear"))
            {
                if (EditorUtility.DisplayDialog("Clear tilemap", "Clear the tilemap?", "Yes, clear", "No, cancel"))
                {
                    ClearTilemap();
                }
            }

            string positionText = selectionRect.HasValue ? string.Format("[{0},{1}]", selectionRect.Value.x, selectionRect.Value.y) : "[-,-]";
            GUI.Label(cursorPosRect, positionText);
        }

        private void HandleResize()
        {
            Vector3 posHandleZ = tilemap.transform.TransformPoint(new Vector3(size.x * 0.5f, 0, size.y)        * tilemap.TileSize);
            Vector3 posHandleX = tilemap.transform.TransformPoint(new Vector3(size.x,        0, size.y * 0.5f) * tilemap.TileSize);

            Vector3 dirHandleZ = tilemap.transform.forward;
            Vector3 dirHandleX = tilemap.transform.right;

            Vector3 sizeY = tilemap.transform.InverseTransformPoint(Handles.Slider(posHandleZ, dirHandleZ));
            Vector3 sizeX = tilemap.transform.InverseTransformPoint(Handles.Slider(posHandleX, dirHandleX));

            size.x = Mathf.Max(Mathf.RoundToInt(sizeX.x / tilemap.TileSize), 1);
            size.y = Mathf.Max(Mathf.RoundToInt(sizeY.z / tilemap.TileSize), 1);
        }

        private void DrawTilemapGrid()
        {
            int width = tilemap.Width;
            int height = tilemap.Height;
            float tileSize = tilemap.TileSize;

            //Draw main grid

            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            TilemapEditorUtils.DrawRect(new Rect(0, 0, width, height), tilemap.TileSize, tilemap.transform);

            Handles.color = Color.gray;
            TilemapEditorUtils.DrawGrid(new Rect(0, 0, width, height), tilemap.TileSize, tilemap.transform);

            Handles.color = Color.white;
            TilemapEditorUtils.DrawLocalPolyLine(tilemap.transform, 3,
                Vector3.zero,
                new Vector3(width * tileSize, 0, 0),
                new Vector3(width * tileSize, 0, height * tileSize),
                new Vector3(0,                0, height * tileSize),
                new Vector3(0,                0, height * tileSize),
                Vector3.zero
            );

            //Draw resize mode grid
            if (mode == EditorMode.Resize)
            {
                Handles.color = Color.green;
                if (size.x > width)
                {
                    TilemapEditorUtils.DrawGrid(new Rect(width, 0, (int)size.x - width, (int)size.y), tilemap.TileSize, tilemap.transform);
                    TilemapEditorUtils.DrawLocalPolyLine(tilemap.transform, 5,
                        new Vector3(width  * tileSize, 0, 0),
                        new Vector3(size.x * tileSize, 0, 0),
                        new Vector3(size.x * tileSize, 0, size.y * tileSize)
                    );
                    if (size.y <= height)
                    {
                        TilemapEditorUtils.DrawLocalLine(tilemap.transform, 5, 
                            new Vector3(width  * tileSize, 0, size.y * tileSize),
                            new Vector3(size.x * tileSize, 0, size.y * tileSize)
                        );
                    }
                }
                if (size.y > height)
                {
                    TilemapEditorUtils.DrawGrid(new Rect(0, height, (int)size.x, (int)size.y - height), tilemap.TileSize, tilemap.transform);
                    TilemapEditorUtils.DrawLocalPolyLine(tilemap.transform, 5,
                        new Vector3(0,                 0, height * tileSize),
                        new Vector3(0,                 0, size.y * tileSize),
                        new Vector3(size.x * tileSize, 0, size.y * tileSize) 
                    );
                    if (size.x <= width)
                    {
                        TilemapEditorUtils.DrawLocalLine(tilemap.transform, 5, 
                            new Vector3(size.x * tileSize, 0, height * tileSize),
                            new Vector3(size.x * tileSize, 0, size.y * tileSize)
                        );
                    }
                }

                Handles.color = Color.red;
                if (size.x < width)
                {
                    TilemapEditorUtils.DrawGrid(new Rect((int)size.x, 0, width - (int)size.x, height), tilemap.TileSize, tilemap.transform);
                    TilemapEditorUtils.DrawLocalLine(tilemap.transform, 5,
                        new Vector3(size.x * tileSize, 0, 0),
                        new Vector3(size.x * tileSize, 0, Mathf.Min(size.y, height) * tileSize)
                    );
                }
                if (size.y < height)
                {
                    TilemapEditorUtils.DrawGrid(new Rect(0, (int)size.y, width, height - (int)size.y), tilemap.TileSize, tilemap.transform);
                    TilemapEditorUtils.DrawLocalLine(tilemap.transform, 5,
                        new Vector3(0,                                   0, size.y * tileSize),
                        new Vector3(Mathf.Min(size.x, width) * tileSize, 0, size.y * tileSize)
                    );
                }
            }
        }

        private void HandleMouseEvents()
        {
            Event e = Event.current;

            Vector3? mousePos = GetMousePosition();

            if (mousePos.HasValue)
            {
                int x = (int)(mousePos.Value.x / tilemap.TileSize);
                int y = (int)(mousePos.Value.z / tilemap.TileSize);

                Rect newSelectionRect = new Rect(x, y, 1, 1);
                if (selectionRect.HasValue == false || selectionRect.Value != newSelectionRect)
                {
                    OnSelectionChanged(newSelectionRect);
                }
                selectionRect = newSelectionRect;

                TilemapEditorUtils.DrawSelectionRect(selectionRect.Value, tilemap.TileSize, tilemap.transform);

               
                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0) //left mouse button
                {
                    if (currentTool == Tool.Erase)
                    {
                        EraseTile(x, y);
                    }

                    if (currentTool == Tool.Pick)
                    {
                        activeBrush = tilemap.GetTile(x, y);
                    }

                    if (currentTool == Tool.Brush)
                    {
                        DrawTile(x, y, activeBrush);
                    }
                    e.Use();
                }
            }
            else
            {
                if (selectionRect.HasValue == true)
                {
                    OnSelectionChanged(null);
                }
                selectionRect = null;
            }
        }

        private void ToggleEditMode()
        {
            mode = mode == EditorMode.Edit ? EditorMode.View : EditorMode.Edit;
            TilemapEditorUtils.SetSelectionState(tilemap.gameObject, mode == EditorMode.Edit ? EditorSelectedRenderState.Hidden : EditorSelectedRenderState.Highlight);

            Repaint();
        }

        #region Tilemap Operations

        private void DrawTile(int x, int y, Brush3D brush)
        {
            if (tilemap.GetTile(x, y) != brush)
            {
                Undo.RecordObject(tilemap, "Draw Tile");

                tilemap.SetTile(x, y, brush);
                SetSelectionState(new Rect(x - 1, y - 1, 3, 3), EditorSelectedRenderState.Hidden);
                SetSelectionState(new Rect(x, y, 1, 1), EditorSelectedRenderState.Highlight);

                MarkSceneDirty();
            }
        }

        private void EraseTile(int x, int y)
        {
            Undo.RecordObject(tilemap, "Erase Tile");

            tilemap.SetTile(x, y, null);
            SetSelectionState(new Rect(x - 1, y - 1, 3, 3), EditorSelectedRenderState.Hidden);

            MarkSceneDirty();
        }

        private void ChangeTilesSize(float size)
        {
            Undo.RecordObject(tilemap, "Change Tile Size");

            size = size < 0.1f ? 0.1f : size;
            tilemap.ChangeTileSize(size);
            MarkSceneDirty();
        }

        private void ResizeTilemap(int sizeX, int sizeY)
        {
            Undo.RecordObject(tilemap, "Resize Tilemap");

            tilemap.Resize(sizeX, sizeY);
            MarkSceneDirty();

            TilemapEditorUtils.SetSelectionState(tilemap.gameObject, EditorSelectedRenderState.Highlight);
        }

        private void ClearTilemap()
        {
            Undo.RecordObject(tilemap, "Clear Tilemap");

            tilemap.Clear();
        }

        #endregion

        private void OnSelectionChanged(Rect? newSelection)
        {
            if (mode != EditorMode.Edit) return;

            //hide old selection
            if (selectionRect.HasValue)
            {
                SetSelectionState(selectionRect.Value, EditorSelectedRenderState.Hidden);
            }

            //highlight new selection
            if (newSelection.HasValue)
            {
                SetSelectionState(newSelection.Value, EditorSelectedRenderState.Highlight);
            }
        }

        private void SetSelectionState(Rect rect, EditorSelectedRenderState state)
        {
            for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
            {
                for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
                {
                    GameObject tile = tilemap.GetTileInstance(x, y);
                    if (tile != null)
                    {
                        TilemapEditorUtils.SetSelectionState(tile, state);
                    }
                }
            }
        }

        private void MarkSceneDirty()
        {
            if (EditorApplication.isPlaying == false)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        private Vector3? GetMousePosition()
        {
            if (SceneView.currentDrawingSceneView == null)
                return null;

            Vector3 mousePos = Vector3.zero;

            Plane plane = new Plane(tilemap.transform.up, tilemap.transform.position);
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            float distance;
            if (plane.Raycast(ray, out distance))
                mousePos = tilemap.transform.InverseTransformPoint(ray.origin + (ray.direction.normalized * distance));

            if (mousePos.x >= 0 && mousePos.x < tilemap.Width * tilemap.TileSize && mousePos.z >= 0 && mousePos.z < tilemap.Height * tilemap.TileSize)
            {
                return mousePos;
            }

            return null;
        }

        private void OnUndoRedo()
        {
            //Clear all children. We can't update instances referances after undo/redo operations.
            //The only way to fix this is to destroy and recreate every tile.

            List<GameObject> remove = new List<GameObject>(tilemap.transform.childCount);
            for (int i = 0; i < tilemap.transform.childCount; i++)
            {
                remove.Add(tilemap.transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < remove.Count; i++)
            {
                DestroyImmediate(remove[i]);
            }

            tilemap.Rebuild();

            selectionRect = null;
            TilemapEditorUtils.SetSelectionState(tilemap.gameObject, EditorSelectedRenderState.Hidden);
        }

        [MenuItem("GameObject/3D Object/Tilemap 3D")]
        static void CreateTilemap()
        {
            var obj = new GameObject("Tilemap");
            obj.AddComponent<Tilemap3D>();
        }

        private enum EditorMode
        {
            View,
            Edit,
            Resize,
        }

        private enum Tool
        {
            Brush,
            Erase,
            Pick
        }
    }
}
