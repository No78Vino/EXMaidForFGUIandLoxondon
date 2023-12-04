using UnityEngine;

namespace FairyGUI
{
    public class RoundedRectMesh : IMeshFactory, IHitTest
    {
        /// <summary>
        /// </summary>
        public float bottomLeftRadius;

        /// <summary>
        /// </summary>
        public float bottomRightRadius;

        /// <summary>
        /// </summary>
        public Rect? drawRect;

        /// <summary>
        /// </summary>
        public Color32? fillColor;

        /// <summary>
        /// </summary>
        public Color32 lineColor;

        /// <summary>
        /// </summary>
        public float lineWidth;

        /// <summary>
        /// </summary>
        public float topLeftRadius;

        /// <summary>
        /// </summary>
        public float topRightRadius;

        public RoundedRectMesh()
        {
            lineColor = Color.black;
        }

        public bool HitTest(Rect contentRect, Vector2 point)
        {
            if (drawRect != null)
                return ((Rect)drawRect).Contains(point);
            return contentRect.Contains(point);
        }

        public void OnPopulateMesh(VertexBuffer vb)
        {
            var rect = drawRect != null ? (Rect)drawRect : vb.contentRect;
            var color = fillColor != null ? (Color32)fillColor : vb.vertexColor;

            var radiusX = rect.width / 2;
            var radiusY = rect.height / 2;
            var cornerMaxRadius = Mathf.Min(radiusX, radiusY);
            var centerX = radiusX + rect.x;
            var centerY = radiusY + rect.y;

            vb.AddVert(new Vector3(centerX, centerY, 0), color);

            var cnt = vb.currentVertCount;
            for (var i = 0; i < 4; i++)
            {
                float radius = 0;
                switch (i)
                {
                    case 0:
                        radius = bottomRightRadius;
                        break;

                    case 1:
                        radius = bottomLeftRadius;
                        break;

                    case 2:
                        radius = topLeftRadius;
                        break;

                    case 3:
                        radius = topRightRadius;
                        break;
                }

                radius = Mathf.Min(cornerMaxRadius, radius);

                var offsetX = rect.x;
                var offsetY = rect.y;

                if (i == 0 || i == 3)
                    offsetX = rect.xMax - radius * 2;
                if (i == 0 || i == 1)
                    offsetY = rect.yMax - radius * 2;

                if (radius != 0)
                {
                    var partNumSides = Mathf.Max(1, Mathf.CeilToInt(Mathf.PI * radius / 8)) + 1;
                    var angleDelta = Mathf.PI / 2 / partNumSides;
                    var angle = Mathf.PI / 2 * i;
                    var startAngle = angle;

                    for (var j = 1; j <= partNumSides; j++)
                    {
                        if (j == partNumSides) //消除精度误差带来的不对齐
                            angle = startAngle + Mathf.PI / 2;
                        var v1 = new Vector3(offsetX + Mathf.Cos(angle) * (radius - lineWidth) + radius,
                            offsetY + Mathf.Sin(angle) * (radius - lineWidth) + radius, 0);
                        vb.AddVert(v1, color);
                        if (lineWidth != 0)
                        {
                            vb.AddVert(v1, lineColor);
                            vb.AddVert(new Vector3(offsetX + Mathf.Cos(angle) * radius + radius,
                                offsetY + Mathf.Sin(angle) * radius + radius, 0), lineColor);
                        }

                        angle += angleDelta;
                    }
                }
                else
                {
                    var v1 = new Vector3(offsetX, offsetY, 0);
                    if (lineWidth != 0)
                    {
                        if (i == 0 || i == 3)
                            offsetX -= lineWidth;
                        else
                            offsetX += lineWidth;
                        if (i == 0 || i == 1)
                            offsetY -= lineWidth;
                        else
                            offsetY += lineWidth;
                        var v2 = new Vector3(offsetX, offsetY, 0);
                        vb.AddVert(v2, color);
                        vb.AddVert(v2, lineColor);
                        vb.AddVert(v1, lineColor);
                    }
                    else
                    {
                        vb.AddVert(v1, color);
                    }
                }
            }

            cnt = vb.currentVertCount - cnt;

            if (lineWidth > 0)
                for (var i = 0; i < cnt; i += 3)
                    if (i != cnt - 3)
                    {
                        vb.AddTriangle(0, i + 1, i + 4);
                        vb.AddTriangle(i + 5, i + 2, i + 3);
                        vb.AddTriangle(i + 3, i + 6, i + 5);
                    }
                    else
                    {
                        vb.AddTriangle(0, i + 1, 1);
                        vb.AddTriangle(2, i + 2, i + 3);
                        vb.AddTriangle(i + 3, 3, 2);
                    }
            else
                for (var i = 0; i < cnt; i++)
                    vb.AddTriangle(0, i + 1, i == cnt - 1 ? 1 : i + 2);
        }
    }
}