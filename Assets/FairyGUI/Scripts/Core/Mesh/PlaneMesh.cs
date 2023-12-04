using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class PlaneMesh : IMeshFactory
    {
        public int gridSize = 30;

        public void OnPopulateMesh(VertexBuffer vb)
        {
            var w = vb.contentRect.width;
            var h = vb.contentRect.height;
            var xMax = vb.contentRect.xMax;
            var yMax = vb.contentRect.yMax;
            var hc = Mathf.Min(Mathf.CeilToInt(w / gridSize), 9);
            var vc = Mathf.Min(Mathf.CeilToInt(h / gridSize), 9);
            var eachPartX = Mathf.FloorToInt(w / hc);
            var eachPartY = Mathf.FloorToInt(h / vc);
            float x, y;
            for (var i = 0; i <= vc; i++)
            {
                if (i == vc)
                    y = yMax;
                else
                    y = vb.contentRect.y + i * eachPartY;
                for (var j = 0; j <= hc; j++)
                {
                    if (j == hc)
                        x = xMax;
                    else
                        x = vb.contentRect.x + j * eachPartX;
                    vb.AddVert(new Vector3(x, y, 0));
                }
            }

            for (var i = 0; i < vc; i++)
            {
                var k = i * (hc + 1);
                for (var j = 1; j <= hc; j++)
                {
                    var m = k + j;
                    vb.AddTriangle(m - 1, m, m + hc);
                    vb.AddTriangle(m, m + hc + 1, m + hc);
                }
            }
        }
    }
}