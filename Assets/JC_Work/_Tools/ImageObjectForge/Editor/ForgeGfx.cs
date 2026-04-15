using UnityEngine;

namespace Orora.ImageObjectForge
{
    internal static class ForgeGfx
    {
        static Texture2D _white;
        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                    _white.hideFlags = HideFlags.HideAndDontSave;
                }
                return _white;
            }
        }

        public static void DrawLine(Vector2 a, Vector2 b, Color color, float thickness = 1.5f)
        {
            var d = b - a;
            float len = d.magnitude;
            if (len < 0.0001f) return;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            var savedColor = GUI.color;
            var savedMatrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, len, thickness), White);
            GUI.matrix = savedMatrix;
            GUI.color = savedColor;
        }

        public static void DrawRectOutline(Rect r, Color color, float thickness = 1f)
        {
            DrawLine(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), color, thickness);
            DrawLine(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), color, thickness);
            DrawLine(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color, thickness);
            DrawLine(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), color, thickness);
        }

        public static void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1.5f, int segments = 32)
        {
            if (radius < 0.5f) return;
            Vector2 prev = center + new Vector2(radius, 0);
            for (int i = 1; i <= segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 cur = center + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
                DrawLine(prev, cur, color, thickness);
                prev = cur;
            }
        }

        public static void DrawPolyline(Vector2[] pts, Color color, float thickness = 1.5f, bool close = false)
        {
            if (pts == null || pts.Length < 2) return;
            for (int i = 0; i < pts.Length - 1; i++)
                DrawLine(pts[i], pts[i + 1], color, thickness);
            if (close)
                DrawLine(pts[pts.Length - 1], pts[0], color, thickness);
        }

        public static void FilledRect(Rect r, Color color)
        {
            var saved = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, White);
            GUI.color = saved;
        }
    }
}
