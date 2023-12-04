using System;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class CaptureCamera : MonoBehaviour
    {
        public const string Name = "Capture Camera";
        public const string LayerName = "VUI";
        public const string HiddenLayerName = "Hidden VUI";

        [NonSerialized] private static CaptureCamera _main;

        [NonSerialized] private static int _layer = -1;

        private static int _hiddenLayer = -1;

        /// <summary>
        /// </summary>
        [NonSerialized] public Camera cachedCamera;

        /// <summary>
        /// </summary>
        [NonSerialized] public Transform cachedTransform;

        /// <summary>
        /// </summary>
        public static int layer
        {
            get
            {
                if (_layer == -1)
                {
                    _layer = LayerMask.NameToLayer(LayerName);
                    if (_layer == -1)
                    {
                        _layer = 30;
                        Debug.LogWarning("Please define two layers named '" + LayerName + "' and '" + HiddenLayerName +
                                         "'");
                    }
                }

                return _layer;
            }
        }

        /// <summary>
        /// </summary>
        public static int hiddenLayer
        {
            get
            {
                if (_hiddenLayer == -1)
                {
                    _hiddenLayer = LayerMask.NameToLayer(HiddenLayerName);
                    if (_hiddenLayer == -1)
                    {
                        Debug.LogWarning("Please define two layers named '" + LayerName + "' and '" + HiddenLayerName +
                                         "'");
                        _hiddenLayer = 31;
                    }
                }

                return _hiddenLayer;
            }
        }

        private void OnEnable()
        {
            cachedCamera = GetComponent<Camera>();
            cachedTransform = gameObject.transform;

            if (gameObject.name == Name)
                _main = this;
        }

        /// <summary>
        /// </summary>
        public static void CheckMain()
        {
            if (_main != null && _main.cachedCamera != null)
                return;

            var go = GameObject.Find(Name);
            if (go != null)
            {
                _main = go.GetComponent<CaptureCamera>();
                return;
            }

            var cameraObject = new GameObject(Name);
            var camera = cameraObject.AddComponent<Camera>();
            camera.depth = 0;
            camera.cullingMask = 1 << layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.nearClipPlane = -30;
            camera.farClipPlane = 30;
            camera.enabled = false;
#if UNITY_5_4_OR_NEWER
            camera.stereoTargetEye = StereoTargetEyeMask.None;
#endif

#if UNITY_5_6_OR_NEWER
            camera.allowHDR = false;
            camera.allowMSAA = false;
#endif
            cameraObject.AddComponent<CaptureCamera>();
        }

        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="stencilSupport"></param>
        /// <returns></returns>
        public static RenderTexture CreateRenderTexture(int width, int height, bool stencilSupport)
        {
            var texture = new RenderTexture(width, height, stencilSupport ? 24 : 0, RenderTextureFormat.ARGB32);
            texture.antiAliasing = 1;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 0;
            texture.useMipMap = false;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = DisplayObject.hideFlags;
            return texture;
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="texture"></param>
        /// <param name="contentHeight"></param>
        /// <param name="offset"></param>
        public static void Capture(DisplayObject target, RenderTexture texture, float contentHeight, Vector2 offset)
        {
            CheckMain();

            var matrix = target.cachedTransform.localToWorldMatrix;
            var scaleX = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            var scaleY = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;

            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            var halfHeight = contentHeight * 0.5f;

            var camera = _main.cachedCamera;
            camera.targetTexture = texture;
            var aspect = (float)texture.width / texture.height;
            camera.aspect = aspect * scaleX / scaleY;
            camera.orthographicSize = halfHeight * scaleY;
            _main.cachedTransform.localPosition =
                target.cachedTransform.TransformPoint(halfHeight * aspect - offset.x, -halfHeight + offset.y, 0);
            if (forward != Vector3.zero)
                _main.cachedTransform.localRotation = Quaternion.LookRotation(forward, upwards);

            var oldLayer = 0;

            if (target.graphics != null)
            {
                oldLayer = target.graphics.gameObject.layer;
                target.graphics.gameObject.layer = layer;
            }

            if (target is Container)
            {
                oldLayer = ((Container)target).numChildren > 0 ? ((Container)target).GetChildAt(0).layer : hiddenLayer;
                ((Container)target).SetChildrenLayer(layer);
            }

            var old = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(true, true, Color.clear);
            camera.Render();
            RenderTexture.active = old;

            if (target.graphics != null)
                target.graphics.gameObject.layer = oldLayer;

            if (target is Container)
                ((Container)target).SetChildrenLayer(oldLayer);
        }
    }
}