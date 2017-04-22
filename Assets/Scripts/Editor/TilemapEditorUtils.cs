using UnityEditor;
using UnityEngine;

namespace Autotiles
{
    public static class TilemapEditorUtils
    {
        public static void DrawRect(Rect rect, float tileSize, Transform parent)
        {
            Vector3 point0 = parent.TransformPoint(new Vector3(rect.xMin * tileSize, 0, rect.yMin * tileSize));
            Vector3 point1 = parent.TransformPoint(new Vector3(rect.xMax * tileSize, 0, rect.yMin * tileSize));
            Vector3 point2 = parent.TransformPoint(new Vector3(rect.xMax * tileSize, 0, rect.yMax * tileSize));
            Vector3 point3 = parent.TransformPoint(new Vector3(rect.xMin * tileSize, 0, rect.yMax * tileSize));

            Handles.DrawAAConvexPolygon(point0, point1, point2, point3);
        }

        //draws selection rect in tilemap coordinates
        public static void DrawSelectionRect(Rect rect, float tileSize, Transform parent)
        {
            Color fillColor = new Color(1, 1, 1, 0.5f);
            Color outlineColor = new Color(1, 1, 1, 1);
            float outlineWidth = 5f;

            Vector3 point0 = parent.TransformPoint( new Vector3(rect.xMin * tileSize, 0, rect.yMin * tileSize) );
            Vector3 point1 = parent.TransformPoint( new Vector3(rect.xMax * tileSize, 0, rect.yMin * tileSize) );
            Vector3 point2 = parent.TransformPoint( new Vector3(rect.xMax * tileSize, 0, rect.yMax * tileSize) );
            Vector3 point3 = parent.TransformPoint( new Vector3(rect.xMin * tileSize, 0, rect.yMax * tileSize) );

            Handles.color = fillColor;
            Handles.DrawAAConvexPolygon(point0, point1, point2, point3);

            Handles.color = outlineColor;
            Handles.DrawAAPolyLine(outlineWidth, point0, point1, point2, point3);
        }

        public static void DrawLocalLine(Transform parent, float width, Vector3 p1, Vector3 p2)
        {
            Vector3 worldP1 = parent.TransformPoint(p1);
            Vector3 worldP2 = parent.TransformPoint(p2);
            if (width < 0)
            {
                Handles.DrawLine(worldP1, worldP2);
            }
            else
            {
                Handles.DrawAAPolyLine(width, worldP1, worldP2);
            }
        }

        public static void DrawLocalPolyLine(Transform parent, float width, params Vector3[] points)
        {
            for (int i = 1; i < points.Length; i++)
            {
                DrawLocalLine(parent, width, points[i - 1], points[i]);
            }
        }

        //Rect in tilemap coords. No outline
        public static void DrawGrid(Rect rect, float tileSize, Transform parent)
        {
            Vector3 position = new Vector3(rect.xMin * tileSize, 0, rect.yMin * tileSize);

            for (float i = tileSize; i < rect.width * tileSize; i += tileSize)
            {
                Vector3 p0 = new Vector3(i + position.x, 0, position.z);
                Vector3 p1 = new Vector3(i + position.x, 0, rect.height * tileSize + position.z);

                DrawLocalLine(parent, -1, p0, p1);
            }
            for (float i = tileSize; i < rect.height * tileSize; i += tileSize)
            {
                Vector3 p0 = new Vector3(position.x, 0, i + position.z);
                Vector3 p1 = new Vector3(rect.width * tileSize + position.x, 0, i + position.z);

                DrawLocalLine(parent, -1, p0, p1);
            }
        }

        public static void SetSelectionState(GameObject obj, EditorSelectedRenderState state)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                EditorUtility.SetSelectedRenderState(renderers[i], state);
            }
        }

        public static void SetSelectionState(GameObject[] objs, EditorSelectedRenderState state)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                SetSelectionState(objs[i], state);
            }
        }
    }
}
