using System;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("FairyGUI/UI Content Scaler")]
    public class UIContentScaler : MonoBehaviour
    {
        /// <summary>
        /// </summary>
        public enum ScaleMode
        {
            ConstantPixelSize,
            ScaleWithScreenSize,
            ConstantPhysicalSize
        }

        /// <summary>
        /// </summary>
        public enum ScreenMatchMode
        {
            MatchWidthOrHeight,
            MatchWidth,
            MatchHeight
        }

        [NonSerialized] public static float scaleFactor = 1;

        [NonSerialized] public static int scaleLevel;

        /// <summary>
        /// </summary>
        public ScaleMode scaleMode;

        /// <summary>
        /// </summary>
        public ScreenMatchMode screenMatchMode;

        /// <summary>
        /// </summary>
        public int designResolutionX;

        /// <summary>
        /// </summary>
        public int designResolutionY;

        /// <summary>
        /// </summary>
        public int fallbackScreenDPI = 96;

        /// <summary>
        /// </summary>
        public int defaultSpriteDPI = 96;

        /// <summary>
        /// </summary>
        public float constantScaleFactor = 1;

        /// <summary>
        ///     当false时，计算比例时会考虑designResolutionX/Y的设置是针对横屏还是竖屏。否则不考虑。
        /// </summary>
        public bool ignoreOrientation;

        [NonSerialized] private bool _changed;

        private void Update()
        {
            if (_changed)
            {
                _changed = false;
                ApplyChange();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                //播放模式下都是通过Stage自带的UIContentScaler实现调整的，所以这里只是把参数传过去
                var scaler = Stage.inst.gameObject.GetComponent<UIContentScaler>();
                if (scaler != this)
                {
                    scaler.scaleMode = scaleMode;
                    if (scaleMode == ScaleMode.ScaleWithScreenSize)
                    {
                        scaler.designResolutionX = designResolutionX;
                        scaler.designResolutionY = designResolutionY;
                        scaler.screenMatchMode = screenMatchMode;
                        scaler.ignoreOrientation = ignoreOrientation;
                    }
                    else if (scaleMode == ScaleMode.ConstantPhysicalSize)
                    {
                        scaler.fallbackScreenDPI = fallbackScreenDPI;
                        scaler.defaultSpriteDPI = defaultSpriteDPI;
                    }
                    else
                    {
                        scaler.constantScaleFactor = constantScaleFactor;
                    }

                    scaler.ApplyChange();
                    GRoot.inst.ApplyContentScaleFactor();
                }
            }
            else //Screen width/height is not reliable in OnEnable in editmode
            {
                _changed = true;
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                scaleFactor = 1;
                scaleLevel = 0;
            }
        }

        //For UIContentScalerEditor Only, as the Screen.width/height is not correct in OnInspectorGUI
        /// <summary>
        /// </summary>
        public void ApplyModifiedProperties()
        {
            _changed = true;
        }

        /// <summary>
        /// </summary>
        public void ApplyChange()
        {
            float screenWidth;
            float screenHeight;

            if (Application.isPlaying) //In case of multi display， we keep using the display which Stage object resides.
            {
                screenWidth = Stage.inst.width;
                screenHeight = Stage.inst.height;
            }
            else
            {
                screenWidth = Screen.width;
                screenHeight = Screen.height;
            }

            if (scaleMode == ScaleMode.ScaleWithScreenSize)
            {
                if (designResolutionX == 0 || designResolutionY == 0)
                    return;

                var dx = designResolutionX;
                var dy = designResolutionY;
                if (!ignoreOrientation &&
                    ((screenWidth > screenHeight && dx < dy) || (screenWidth < screenHeight && dx > dy)))
                {
                    //scale should not change when orientation change
                    var tmp = dx;
                    dx = dy;
                    dy = tmp;
                }

                if (screenMatchMode == ScreenMatchMode.MatchWidthOrHeight)
                {
                    var s1 = screenWidth / dx;
                    var s2 = screenHeight / dy;
                    scaleFactor = Mathf.Min(s1, s2);
                }
                else if (screenMatchMode == ScreenMatchMode.MatchWidth)
                {
                    scaleFactor = screenWidth / dx;
                }
                else
                {
                    scaleFactor = screenHeight / dy;
                }
            }
            else if (scaleMode == ScaleMode.ConstantPhysicalSize)
            {
                var dpi = Screen.dpi;
                if (dpi == 0)
                    dpi = fallbackScreenDPI;
                if (dpi == 0)
                    dpi = 96;
                scaleFactor = dpi / (defaultSpriteDPI == 0 ? 96 : defaultSpriteDPI);
            }
            else
            {
                scaleFactor = constantScaleFactor;
            }

            if (scaleFactor > 10)
                scaleFactor = 10;

            UpdateScaleLevel();

            StageCamera.screenSizeVer++;
        }

        private void UpdateScaleLevel()
        {
            if (scaleFactor > 3)
                scaleLevel = 3; //x4
            else if (scaleFactor > 2)
                scaleLevel = 2; //x3
            else if (scaleFactor > 1)
                scaleLevel = 1; //x2
            else
                scaleLevel = 0;
        }
    }
}