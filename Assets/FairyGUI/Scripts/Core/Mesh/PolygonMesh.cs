using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class PolygonMesh : IMeshFactory, IHitTest
    {
        private static readonly List<int> sRestIndices = new();

        /// <summary>
        ///     points must be in clockwise order, and must start from bottom-left if stretchUV is set.
        /// </summary>
        public readonly List<Vector2> points;

        /// <summary>
        ///     if you dont want to provide uv, leave it empty.
        /// </summary>
        public readonly List<Vector2> texcoords;

        /// <summary>
        /// </summary>
        public Color32[] colors;

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
        public bool usePercentPositions;

        public PolygonMesh()
        {
            points = new List<Vector2>();
            texcoords = new List<Vector2>();
        }

        public bool HitTest(Rect contentRect, Vector2 point)
        {
            if (!contentRect.Contains(point))
                return false;

            // Algorithm & implementation thankfully taken from:
            // -> http://alienryderflex.com/polygon/
            // inspired by Starling
            var len = points.Count;
            int i;
            var j = len - 1;
            var oddNodes = false;
            var w = contentRect.width;
            var h = contentRect.height;

            for (i = 0; i < len; ++i)
            {
                var ix = points[i].x;
                var iy = points[i].y;
                var jx = points[j].x;
                var jy = points[j].y;
                if (usePercentPositions)
                {
                    ix *= w;
                    iy *= h;
                    ix *= w;
                    iy *= h;
                }

                if (((iy < point.y && jy >= point.y) || (jy < point.y && iy >= point.y)) &&
                    (ix <= point.x || jx <= point.x))
                    if (ix + (point.y - iy) / (jy - iy) * (jx - ix) < point.x)
                        oddNodes = !oddNodes;

                j = i;
            }

            return oddNodes;
        }

        public void OnPopulateMesh(VertexBuffer vb)
        {
            var numVertices = points.Count;
            if (numVertices < 3)
                return;

            int restIndexPos, numRestIndices;
            var color = fillColor != null ? (Color32)fillColor : vb.vertexColor;

            var w = vb.contentRect.width;
            var h = vb.contentRect.height;
            var useTexcoords = texcoords.Count >= numVertices;
            var fullUV = true;
            for (var i = 0; i < numVertices; i++)
            {
                var vec = new Vector3(points[i].x, points[i].y, 0);
                if (usePercentPositions)
                {
                    vec.x *= w;
                    vec.y *= h;
                }

                if (useTexcoords)
                {
                    var uv = texcoords[i];
                    if ((uv.x != 0 && uv.x != 1) || (uv.y != 0 && uv.y != 1))
                        fullUV = false;
                    uv.x = Mathf.Lerp(vb.uvRect.x, vb.uvRect.xMax, uv.x);
                    uv.y = Mathf.Lerp(vb.uvRect.y, vb.uvRect.yMax, uv.y);
                    vb.AddVert(vec, color, uv);
                }
                else
                {
                    vb.AddVert(vec, color);
                }
            }

            if (useTexcoords && fullUV && numVertices == 4)
                vb._isArbitraryQuad = true;

            // Algorithm "Ear clipping method" described here:
            // -> https://en.wikipedia.org/wiki/Polygon_triangulation
            //
            // Implementation inspired by:
            // -> http://polyk.ivank.net
            // -> Starling

            sRestIndices.Clear();
            for (var i = 0; i < numVertices; ++i)
                sRestIndices.Add(i);

            restIndexPos = 0;
            numRestIndices = numVertices;

            Vector2 a, b, c, p;
            int otherIndex;
            bool earFound;
            int i0, i1, i2;

            while (numRestIndices > 3)
            {
                earFound = false;
                i0 = sRestIndices[restIndexPos % numRestIndices];
                i1 = sRestIndices[(restIndexPos + 1) % numRestIndices];
                i2 = sRestIndices[(restIndexPos + 2) % numRestIndices];

                a = points[i0];
                b = points[i1];
                c = points[i2];

                if ((a.y - b.y) * (c.x - b.x) + (b.x - a.x) * (c.y - b.y) >= 0)
                {
                    earFound = true;
                    for (var i = 3; i < numRestIndices; ++i)
                    {
                        otherIndex = sRestIndices[(restIndexPos + i) % numRestIndices];
                        p = points[otherIndex];

                        if (IsPointInTriangle(ref p, ref a, ref b, ref c))
                        {
                            earFound = false;
                            break;
                        }
                    }
                }

                if (earFound)
                {
                    vb.AddTriangle(i0, i1, i2);
                    sRestIndices.RemoveAt((restIndexPos + 1) % numRestIndices);

                    numRestIndices--;
                    restIndexPos = 0;
                }
                else
                {
                    restIndexPos++;
                    if (restIndexPos == numRestIndices) break; // no more ears
                }
            }

            vb.AddTriangle(sRestIndices[0], sRestIndices[1], sRestIndices[2]);

            if (colors != null)
                vb.RepeatColors(colors, 0, vb.currentVertCount);

            if (lineWidth > 0)
                DrawOutline(vb);
        }

        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        public void Add(Vector2 point)
        {
            points.Add(point);
        }

        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        /// <param name="texcoord"></param>
        public void Add(Vector2 point, Vector2 texcoord)
        {
            points.Add(point);
            texcoords.Add(texcoord);
        }

        private void DrawOutline(VertexBuffer vb)
        {
            var numVertices = points.Count;
            var start = vb.currentVertCount - numVertices;
            var k = vb.currentVertCount;
            for (var i = 0; i < numVertices; i++)
            {
                var p0 = vb.vertices[start + i];
                p0.y = -p0.y;
                Vector3 p1;
                if (i < numVertices - 1)
                    p1 = vb.vertices[start + i + 1];
                else
                    p1 = vb.vertices[start];
                p1.y = -p1.y;

                var lineVector = p1 - p0;
                var widthVector = Vector3.Cross(lineVector, new Vector3(0, 0, 1));
                widthVector.Normalize();

                vb.AddVert(p0 - widthVector * lineWidth * 0.5f, lineColor);
                vb.AddVert(p0 + widthVector * lineWidth * 0.5f, lineColor);
                vb.AddVert(p1 - widthVector * lineWidth * 0.5f, lineColor);
                vb.AddVert(p1 + widthVector * lineWidth * 0.5f, lineColor);

                k += 4;
                vb.AddTriangle(k - 4, k - 3, k - 1);
                vb.AddTriangle(k - 4, k - 1, k - 2);

                //joint
                if (i != 0)
                {
                    vb.AddTriangle(k - 6, k - 5, k - 3);
                    vb.AddTriangle(k - 6, k - 3, k - 4);
                }

                if (i == numVertices - 1)
                {
                    start += numVertices;
                    vb.AddTriangle(k - 2, k - 1, start + 1);
                    vb.AddTriangle(k - 2, start + 1, start);
                }
            }
        }

        private bool IsPointInTriangle(ref Vector2 p, ref Vector2 a, ref Vector2 b, ref Vector2 c)
        {
            // From Starling
            // This algorithm is described well in this article:
            // http://www.blackpawn.com/texts/pointinpoly/default.html

            var v0x = c.x - a.x;
            var v0y = c.y - a.y;
            var v1x = b.x - a.x;
            var v1y = b.y - a.y;
            var v2x = p.x - a.x;
            var v2y = p.y - a.y;

            var dot00 = v0x * v0x + v0y * v0y;
            var dot01 = v0x * v1x + v0y * v1y;
            var dot02 = v0x * v2x + v0y * v2y;
            var dot11 = v1x * v1x + v1y * v1y;
            var dot12 = v1x * v2x + v1y * v2y;

            var invDen = 1.0f / (dot00 * dot11 - dot01 * dot01);
            var u = (dot11 * dot02 - dot01 * dot12) * invDen;
            var v = (dot00 * dot12 - dot01 * dot02) * invDen;

            return u >= 0 && v >= 0 && u + v < 1;
        }
    }
}