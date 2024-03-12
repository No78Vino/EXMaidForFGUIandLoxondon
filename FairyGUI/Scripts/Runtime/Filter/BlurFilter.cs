using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class BlurFilter : IFilter
    {
        private Material _blitMaterial;

        private DisplayObject _target;
        //ref http://blog.csdn.net/u011047171/article/details/47947397

        /// <summary>
        /// </summary>
        public float blurSize;

        public BlurFilter()
        {
            blurSize = 1f;
        }

        public DisplayObject target
        {
            get => _target;
            set
            {
                _target = value;
                _target.EnterPaintingMode(1, null);
                _target.onPaint += OnRenderImage;

                _blitMaterial = new Material(ShaderConfig.GetShader("FairyGUI/BlurFilter"));
                _blitMaterial.hideFlags = DisplayObject.hideFlags;
            }
        }

        public void Dispose()
        {
            _target.LeavePaintingMode(1);
            _target.onPaint -= OnRenderImage;
            _target = null;

            if (Application.isPlaying)
                Object.Destroy(_blitMaterial);
            else
                Object.DestroyImmediate(_blitMaterial);
        }

        public void Update()
        {
        }

        private void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
        {
            var off = blurSize * iteration + 0.5f;
            Graphics.BlitMultiTap(source, dest, _blitMaterial,
                new Vector2(-off, -off),
                new Vector2(-off, off),
                new Vector2(off, off),
                new Vector2(off, -off)
            );
        }

        private void DownSample4x(RenderTexture source, RenderTexture dest)
        {
            var off = 1.0f;
            Graphics.BlitMultiTap(source, dest, _blitMaterial,
                new Vector2(off, off),
                new Vector2(-off, off),
                new Vector2(off, off),
                new Vector2(off, -off)
            );
        }

        private void OnRenderImage()
        {
            if (blurSize < 0.01)
                return;

            var sourceTexture = (RenderTexture)_target.paintingGraphics.texture.nativeTexture;
            var rtW = sourceTexture.width / 8;
            var rtH = sourceTexture.height / 8;
            var buffer = RenderTexture.GetTemporary(rtW, rtH, 0);

            DownSample4x(sourceTexture, buffer);

            for (var i = 0; i < 2; i++)
            {
                var buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0);
                FourTapCone(buffer, buffer2, i);
                RenderTexture.ReleaseTemporary(buffer);
                buffer = buffer2;
            }

            Graphics.Blit(buffer, sourceTexture);

            RenderTexture.ReleaseTemporary(buffer);
        }
    }
}