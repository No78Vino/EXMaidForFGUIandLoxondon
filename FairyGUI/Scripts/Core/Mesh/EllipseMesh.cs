using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class EllipseMesh : IMeshFactory, IHitTest
    {
        private static readonly int[] SECTOR_CENTER_TRIANGLES =
        {
            0, 4, 1,
            0, 3, 4,
            0, 2, 3,
            0, 8, 5,
            0, 7, 8,
            0, 6, 7,
            6, 5, 2,
            2, 1, 6
        };

        /// <summary>
        /// </summary>
        public Color32? centerColor;

        /// <summary>
        /// </summary>
        public Rect? drawRect;

        /// <summary>
        /// </summary>
        public float endDegreee;

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
        public float startDegree;

        public EllipseMesh()
        {
            lineColor = Color.black;
            startDegree = 0;
            endDegreee = 360;
        }

        public bool HitTest(Rect contentRect, Vector2 point)
        {
            if (!contentRect.Contains(point))
                return false;

            var radiusX = contentRect.width * 0.5f;
            var raduisY = contentRect.height * 0.5f;
            var xx = point.x - radiusX - contentRect.x;
            var yy = point.y - raduisY - contentRect.y;
            if (Mathf.Pow(xx / radiusX, 2) + Mathf.Pow(yy / raduisY, 2) < 1)
            {
                if (startDegree != 0 || endDegreee != 360)
                {
                    var deg = Mathf.Atan2(yy, xx) * Mathf.Rad2Deg;
                    if (deg < 0)
                        deg += 360;
                    return deg >= startDegree && deg <= endDegreee;
                }

                return true;
            }

            return false;
        }

        public void OnPopulateMesh(VertexBuffer vb)
        {
            var rect = drawRect != null ? (Rect)drawRect : vb.contentRect;
            var color = fillColor != null ? (Color32)fillColor : vb.vertexColor;

            var sectionStart = Mathf.Clamp(startDegree, 0, 360);
            var sectionEnd = Mathf.Clamp(endDegreee, 0, 360);
            var clipped = sectionStart > 0 || sectionEnd < 360;
            sectionStart = sectionStart * Mathf.Deg2Rad;
            sectionEnd = sectionEnd * Mathf.Deg2Rad;
            var centerColor2 = centerColor == null ? color : (Color32)centerColor;

            var radiusX = rect.width / 2;
            var radiusY = rect.height / 2;
            var sides = Mathf.CeilToInt(Mathf.PI * (radiusX + radiusY) / 4);
            sides = Mathf.Clamp(sides, 40, 800);
            var angleDelta = 2 * Mathf.PI / sides;
            float angle = 0;
            float lineAngle = 0;

            if (lineWidth > 0 && clipped)
            {
                lineAngle = lineWidth / Mathf.Max(radiusX, radiusY);
                sectionStart += lineAngle;
                sectionEnd -= lineAngle;
            }

            var vpos = vb.currentVertCount;
            var centerX = rect.x + radiusX;
            var centerY = rect.y + radiusY;
            vb.AddVert(new Vector3(centerX, centerY, 0), centerColor2);
            for (var i = 0; i < sides; i++)
            {
                if (angle < sectionStart)
                    angle = sectionStart;
                else if (angle > sectionEnd)
                    angle = sectionEnd;
                var vec = new Vector3(Mathf.Cos(angle) * (radiusX - lineWidth) + centerX,
                    Mathf.Sin(angle) * (radiusY - lineWidth) + centerY, 0);
                vb.AddVert(vec, color);
                if (lineWidth > 0)
                {
                    vb.AddVert(vec, lineColor);
                    vb.AddVert(
                        new Vector3(Mathf.Cos(angle) * radiusX + centerX, Mathf.Sin(angle) * radiusY + centerY, 0),
                        lineColor);
                }

                angle += angleDelta;
            }

            if (lineWidth > 0)
            {
                var cnt = sides * 3;
                for (var i = 0; i < cnt; i += 3)
                    if (i != cnt - 3)
                    {
                        vb.AddTriangle(0, i + 1, i + 4);
                        vb.AddTriangle(i + 5, i + 2, i + 3);
                        vb.AddTriangle(i + 3, i + 6, i + 5);
                    }
                    else if (!clipped)
                    {
                        vb.AddTriangle(0, i + 1, 1);
                        vb.AddTriangle(2, i + 2, i + 3);
                        vb.AddTriangle(i + 3, 3, 2);
                    }
                    else
                    {
                        vb.AddTriangle(0, i + 1, i + 1);
                        vb.AddTriangle(i + 2, i + 2, i + 3);
                        vb.AddTriangle(i + 3, i + 3, i + 2);
                    }
            }
            else
            {
                for (var i = 0; i < sides; i++)
                    if (i != sides - 1)
                        vb.AddTriangle(0, i + 1, i + 2);
                    else if (!clipped)
                        vb.AddTriangle(0, i + 1, 1);
                    else
                        vb.AddTriangle(0, i + 1, i + 1);
            }

            if (lineWidth > 0 && clipped)
            {
                //扇形内边缘的线条

                vb.AddVert(new Vector3(radiusX, radiusY, 0), lineColor);
                var centerRadius = lineWidth * 0.5f;

                sectionStart -= lineAngle;
                angle = sectionStart + lineAngle * 0.5f + Mathf.PI * 0.5f;
                vb.AddVert(
                    new Vector3(Mathf.Cos(angle) * centerRadius + radiusX, Mathf.Sin(angle) * centerRadius + radiusY,
                        0), lineColor);
                angle -= Mathf.PI;
                vb.AddVert(
                    new Vector3(Mathf.Cos(angle) * centerRadius + radiusX, Mathf.Sin(angle) * centerRadius + radiusY,
                        0), lineColor);
                vb.AddVert(
                    new Vector3(Mathf.Cos(sectionStart) * radiusX + radiusX,
                        Mathf.Sin(sectionStart) * radiusY + radiusY, 0), lineColor);
                vb.AddVert(vb.GetPosition(vpos + 3), lineColor);

                sectionEnd += lineAngle;
                angle = sectionEnd - lineAngle * 0.5f + Mathf.PI * 0.5f;
                vb.AddVert(
                    new Vector3(Mathf.Cos(angle) * centerRadius + radiusX, Mathf.Sin(angle) * centerRadius + radiusY,
                        0), lineColor);
                angle -= Mathf.PI;
                vb.AddVert(
                    new Vector3(Mathf.Cos(angle) * centerRadius + radiusX, Mathf.Sin(angle) * centerRadius + radiusY,
                        0), lineColor);
                vb.AddVert(vb.GetPosition(vpos + sides * 3), lineColor);
                vb.AddVert(
                    new Vector3(Mathf.Cos(sectionEnd) * radiusX + radiusX, Mathf.Sin(sectionEnd) * radiusY + radiusY,
                        0), lineColor);

                vb.AddTriangles(SECTOR_CENTER_TRIANGLES, sides * 3 + 1);
            }
        }
    }
}