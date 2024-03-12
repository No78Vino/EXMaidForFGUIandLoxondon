using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class PixelHitTestData
    {
        public byte[] pixels;
        public int pixelsLength;
        public int pixelsOffset;
        public int pixelWidth;
        public float scale;

        public void Load(ByteBuffer ba)
        {
            ba.ReadInt();
            pixelWidth = ba.ReadInt();
            scale = 1.0f / ba.ReadByte();
            pixels = ba.buffer;
            pixelsLength = ba.ReadInt();
            pixelsOffset = ba.position;
            ba.Skip(pixelsLength);
        }
    }

    /// <summary>
    /// </summary>
    public class PixelHitTest : IHitTest
    {
        private readonly PixelHitTestData _data;
        public int offsetX;
        public int offsetY;
        public float sourceHeight;
        public float sourceWidth;

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        public PixelHitTest(PixelHitTestData data, int offsetX, int offsetY, float sourceWidth, float sourceHeight)
        {
            _data = data;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.sourceWidth = sourceWidth;
            this.sourceHeight = sourceHeight;
        }

        /// <summary>
        /// </summary>
        /// <param name="contentRect"></param>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        public bool HitTest(Rect contentRect, Vector2 localPoint)
        {
            if (!contentRect.Contains(localPoint))
                return false;

            var x = Mathf.FloorToInt((localPoint.x * sourceWidth / contentRect.width - offsetX) * _data.scale);
            var y = Mathf.FloorToInt((localPoint.y * sourceHeight / contentRect.height - offsetY) * _data.scale);
            if (x < 0 || y < 0 || x >= _data.pixelWidth)
                return false;

            var pos = y * _data.pixelWidth + x;
            var pos2 = pos / 8;
            var pos3 = pos % 8;

            if (pos2 >= 0 && pos2 < _data.pixelsLength)
                return ((_data.pixels[_data.pixelsOffset + pos2] >> pos3) & 0x1) > 0;
            return false;
        }
    }
}