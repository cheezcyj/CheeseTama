using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class PictogramGraphic : MaskableGraphic
    {
        public enum PictogramType
        {
            Book,
            Gear,
            MilkCarton,
            Ball,
            Brush,
            Pot,
            SnackBag,
            Spray,
            Bed
        }

        [SerializeField] private PictogramType pictogramType;
        [SerializeField] private Color accentColor = new Color(1f, 0.86f, 0.48f, 1f);

        public PictogramType Type
        {
            get => pictogramType;
            set
            {
                if (pictogramType == value)
                {
                    return;
                }

                pictogramType = value;
                SetVerticesDirty();
            }
        }

        public Color AccentColor
        {
            get => accentColor;
            set
            {
                accentColor = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            switch (pictogramType)
            {
                case PictogramType.Book:
                    DrawBook(vh);
                    break;
                case PictogramType.Gear:
                    DrawGear(vh);
                    break;
                case PictogramType.MilkCarton:
                    DrawMilkCarton(vh);
                    break;
                case PictogramType.Ball:
                    DrawBall(vh);
                    break;
                case PictogramType.Brush:
                    DrawBrush(vh);
                    break;
                case PictogramType.Pot:
                    DrawPot(vh);
                    break;
                case PictogramType.SnackBag:
                    DrawSnackBag(vh);
                    break;
                case PictogramType.Spray:
                    DrawSpray(vh);
                    break;
                case PictogramType.Bed:
                    DrawBed(vh);
                    break;
            }
        }

        private void DrawBook(VertexHelper vh)
        {
            AddPolygon(vh, new[]
            {
                new Vector2(-0.48f, 0.42f),
                new Vector2(-0.06f, 0.32f),
                new Vector2(-0.06f, -0.42f),
                new Vector2(-0.48f, -0.33f)
            }, color);
            AddPolygon(vh, new[]
            {
                new Vector2(0.48f, 0.42f),
                new Vector2(0.06f, 0.32f),
                new Vector2(0.06f, -0.42f),
                new Vector2(0.48f, -0.33f)
            }, color);
            AddRotatedRect(vh, new Vector2(0f, -0.05f), new Vector2(0.045f, 0.78f), 0f, accentColor);
            AddRotatedRect(vh, new Vector2(-0.27f, 0.15f), new Vector2(0.20f, 0.055f), -7f, accentColor);
            AddRotatedRect(vh, new Vector2(-0.27f, -0.08f), new Vector2(0.22f, 0.055f), -7f, accentColor);
            AddRotatedRect(vh, new Vector2(0.27f, 0.15f), new Vector2(0.20f, 0.055f), 7f, accentColor);
            AddRotatedRect(vh, new Vector2(0.27f, -0.08f), new Vector2(0.22f, 0.055f), 7f, accentColor);
        }

        private void DrawGear(VertexHelper vh)
        {
            for (var i = 0; i < 8; i += 1)
            {
                var angle = i * 45f;
                var radians = angle * Mathf.Deg2Rad;
                var center = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * 0.39f;
                AddRotatedRect(vh, center, new Vector2(0.15f, 0.25f), -angle, color);
            }

            AddCircle(vh, Vector2.zero, 0.33f, color, 24);
            AddCircle(vh, Vector2.zero, 0.14f, accentColor, 18);
        }

        private void DrawMilkCarton(VertexHelper vh)
        {
            AddPolygon(vh, new[]
            {
                new Vector2(-0.34f, -0.48f),
                new Vector2(0.34f, -0.48f),
                new Vector2(0.34f, 0.18f),
                new Vector2(0.18f, 0.42f),
                new Vector2(0f, 0.52f),
                new Vector2(-0.18f, 0.42f),
                new Vector2(-0.34f, 0.18f)
            }, color);
            AddPolygon(vh, new[]
            {
                new Vector2(-0.28f, 0.18f),
                new Vector2(0f, 0.30f),
                new Vector2(0.28f, 0.18f),
                new Vector2(0f, 0.46f)
            }, accentColor);
            AddRotatedRect(vh, new Vector2(0f, -0.12f), new Vector2(0.43f, 0.25f), 0f, accentColor);
            AddRotatedRect(vh, new Vector2(0f, 0.03f), new Vector2(0.43f, 0.055f), 0f, color);
            AddRotatedRect(vh, new Vector2(0f, 0.25f), new Vector2(0.045f, 0.42f), 0f, color);
        }

        private void DrawBall(VertexHelper vh)
        {
            AddCircle(vh, Vector2.zero, 0.46f, color, 40);
            AddCircle(vh, Vector2.zero, 0.38f, accentColor, 40);
            AddBezierStroke(vh, new Vector2(-0.38f, 0f), new Vector2(0f, 0.08f), new Vector2(0.38f, 0f), 0.065f, color, 12);
            AddBezierStroke(vh, new Vector2(0f, 0.38f), new Vector2(0.07f, 0f), new Vector2(0f, -0.38f), 0.065f, color, 12);
            AddBezierStroke(vh, new Vector2(-0.28f, 0.31f), new Vector2(-0.08f, 0.02f), new Vector2(-0.28f, -0.31f), 0.055f, color, 12);
            AddBezierStroke(vh, new Vector2(0.28f, 0.31f), new Vector2(0.08f, 0.02f), new Vector2(0.28f, -0.31f), 0.055f, color, 12);
        }

        private void DrawBrush(VertexHelper vh)
        {
            AddRotatedRect(vh, new Vector2(-0.02f, -0.05f), new Vector2(0.16f, 0.84f), -32f, color);
            AddRotatedRect(vh, new Vector2(0.28f, 0.30f), new Vector2(0.30f, 0.18f), -32f, accentColor);
            AddRotatedRect(vh, new Vector2(-0.32f, 0.34f), new Vector2(0.08f, 0.28f), 0f, color);
            AddRotatedRect(vh, new Vector2(-0.32f, 0.34f), new Vector2(0.28f, 0.08f), 0f, color);
        }

        private void DrawPot(VertexHelper vh)
        {
            AddRotatedRect(vh, new Vector2(0f, -0.12f), new Vector2(0.72f, 0.38f), 0f, color);
            AddRotatedRect(vh, new Vector2(0f, 0.16f), new Vector2(0.54f, 0.09f), 0f, color);
            AddRotatedRect(vh, new Vector2(-0.45f, -0.12f), new Vector2(0.16f, 0.22f), 0f, color);
            AddRotatedRect(vh, new Vector2(0.45f, -0.12f), new Vector2(0.16f, 0.22f), 0f, color);
            AddRotatedRect(vh, new Vector2(-0.18f, 0.42f), new Vector2(0.08f, 0.30f), 0f, accentColor);
            AddRotatedRect(vh, new Vector2(0.16f, 0.44f), new Vector2(0.08f, 0.28f), 0f, accentColor);
        }

        private void DrawSnackBag(VertexHelper vh)
        {
            AddPolygon(vh, new[]
            {
                new Vector2(-0.36f, -0.46f),
                new Vector2(0.36f, -0.46f),
                new Vector2(0.30f, 0.34f),
                new Vector2(0.18f, 0.46f),
                new Vector2(-0.18f, 0.46f),
                new Vector2(-0.30f, 0.34f)
            }, color);
            AddRotatedRect(vh, new Vector2(0f, 0.27f), new Vector2(0.50f, 0.12f), 0f, accentColor);
            AddCircle(vh, new Vector2(-0.13f, -0.13f), 0.06f, accentColor, 10);
            AddCircle(vh, new Vector2(0.13f, -0.13f), 0.06f, accentColor, 10);
        }

        private void DrawSpray(VertexHelper vh)
        {
            AddRotatedRect(vh, new Vector2(-0.07f, -0.16f), new Vector2(0.38f, 0.58f), 0f, color);
            AddRotatedRect(vh, new Vector2(0.02f, 0.20f), new Vector2(0.20f, 0.20f), 0f, color);
            AddRotatedRect(vh, new Vector2(0.25f, 0.34f), new Vector2(0.38f, 0.12f), 0f, color);
            AddCircle(vh, new Vector2(0.37f, 0.03f), 0.055f, accentColor, 10);
            AddCircle(vh, new Vector2(0.47f, -0.18f), 0.055f, accentColor, 10);
        }

        private void DrawBed(VertexHelper vh)
        {
            AddRotatedRect(vh, new Vector2(0f, -0.22f), new Vector2(0.80f, 0.22f), 0f, color);
            AddRotatedRect(vh, new Vector2(-0.38f, 0.00f), new Vector2(0.14f, 0.50f), 0f, color);
            AddRotatedRect(vh, new Vector2(-0.10f, 0.05f), new Vector2(0.30f, 0.17f), 0f, accentColor);
            AddCircle(vh, new Vector2(0.30f, 0.25f), 0.18f, color, 18);
            AddCircle(vh, new Vector2(0.40f, 0.29f), 0.15f, accentColor, 18);
        }

        private void AddRotatedRect(VertexHelper vh, Vector2 center, Vector2 size, float angle, Color drawColor)
        {
            var half = size * 0.5f;
            var points = new[]
            {
                new Vector2(-half.x, -half.y),
                new Vector2(-half.x, half.y),
                new Vector2(half.x, half.y),
                new Vector2(half.x, -half.y)
            };
            var radians = angle * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            for (var i = 0; i < points.Length; i += 1)
            {
                var point = points[i];
                points[i] = new Vector2(
                    (point.x * cos) - (point.y * sin),
                    (point.x * sin) + (point.y * cos)) + center;
            }

            AddPolygon(vh, points, drawColor);
        }

        private void AddCircle(VertexHelper vh, Vector2 center, float radius, Color drawColor, int segments)
        {
            var points = new List<Vector2>();
            for (var i = 0; i < segments; i += 1)
            {
                var radians = Mathf.PI * 2f * i / segments;
                points.Add(center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius);
            }

            AddPolygon(vh, points, drawColor);
        }

        private void AddBezierStroke(VertexHelper vh, Vector2 start, Vector2 control, Vector2 end, float width, Color drawColor, int segments)
        {
            var previous = start;
            var halfWidth = width * 0.5f;
            AddCircle(vh, previous, halfWidth, drawColor, 8);

            for (var i = 1; i <= segments; i += 1)
            {
                var t = i / (float)segments;
                var current = QuadraticBezier(start, control, end, t);
                AddStrokeSegment(vh, previous, current, halfWidth, drawColor);
                AddCircle(vh, current, halfWidth, drawColor, 8);
                previous = current;
            }
        }

        private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            var inverse = 1f - t;
            return (inverse * inverse * start) + (2f * inverse * t * control) + (t * t * end);
        }

        private void AddStrokeSegment(VertexHelper vh, Vector2 start, Vector2 end, float halfWidth, Color drawColor)
        {
            var direction = end - start;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var normal = new Vector2(-direction.y, direction.x).normalized * halfWidth;
            AddPolygon(vh, new[]
            {
                start - normal,
                start + normal,
                end + normal,
                end - normal
            }, drawColor);
        }

        private void AddPolygon(VertexHelper vh, IReadOnlyList<Vector2> normalizedPoints, Color drawColor)
        {
            if (normalizedPoints == null || normalizedPoints.Count < 3)
            {
                return;
            }

            var startIndex = vh.currentVertCount;
            for (var i = 0; i < normalizedPoints.Count; i += 1)
            {
                vh.AddVert(ToLocal(normalizedPoints[i]), drawColor, Vector2.zero);
            }

            for (var i = 1; i < normalizedPoints.Count - 1; i += 1)
            {
                vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
            }
        }

        private Vector3 ToLocal(Vector2 normalized)
        {
            var rect = GetPixelAdjustedRect();
            return new Vector3(
                rect.x + ((normalized.x + 0.5f) * rect.width),
                rect.y + ((normalized.y + 0.5f) * rect.height),
                0f);
        }
    }
}
