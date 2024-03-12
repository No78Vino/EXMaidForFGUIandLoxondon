using System;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     GProgressBar class.
    /// </summary>
    public class GProgressBar : GComponent
    {
        private GMovieClip _aniObject;
        private float _barMaxHeight;
        private float _barMaxHeightDelta;
        private float _barMaxWidth;
        private float _barMaxWidthDelta;
        private GObject _barObjectH;
        private GObject _barObjectV;
        private float _barStartX;
        private float _barStartY;
        private double _max;
        private double _min;

        private GObject _titleObject;
        private ProgressTitleType _titleType;
        private double _value;

        public GProgressBar()
        {
            _value = 50;
            _max = 100;
        }

        /// <summary>
        /// </summary>
        public ProgressTitleType titleType
        {
            get => _titleType;
            set
            {
                if (_titleType != value)
                {
                    _titleType = value;
                    Update(_value);
                }
            }
        }

        /// <summary>
        /// </summary>
        public double min
        {
            get => _min;
            set
            {
                if (_min != value)
                {
                    _min = value;
                    Update(_value);
                }
            }
        }

        /// <summary>
        /// </summary>
        public double max
        {
            get => _max;
            set
            {
                if (_max != value)
                {
                    _max = value;
                    Update(_value);
                }
            }
        }

        /// <summary>
        /// </summary>
        public double value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    GTween.Kill(this, TweenPropType.Progress, false);

                    _value = value;
                    Update(_value);
                }
            }
        }

        public bool reverse { get; set; }

        /// <summary>
        ///     动态改变进度值。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        public GTweener TweenValue(double value, float duration)
        {
            double oldValule;

            var twener = GTween.GetTween(this, TweenPropType.Progress);
            if (twener != null)
            {
                oldValule = twener.value.d;
                twener.Kill();
            }
            else
            {
                oldValule = _value;
            }

            _value = value;
            return GTween.ToDouble(oldValule, _value, duration)
                .SetEase(EaseType.Linear)
                .SetTarget(this, TweenPropType.Progress);
        }

        /// <summary>
        /// </summary>
        /// <param name="newValue"></param>
        public void Update(double newValue)
        {
            var percent = Mathf.Clamp01((float)((newValue - _min) / (_max - _min)));
            if (_titleObject != null)
                switch (_titleType)
                {
                    case ProgressTitleType.Percent:
                        if (RTLSupport.BaseDirection == RTLSupport.DirectionType.RTL)
                            _titleObject.text = "%" + Mathf.FloorToInt(percent * 100);
                        else
                            _titleObject.text = Mathf.FloorToInt(percent * 100) + "%";
                        break;

                    case ProgressTitleType.ValueAndMax:
                        if (RTLSupport.BaseDirection == RTLSupport.DirectionType.RTL)
                            _titleObject.text = Math.Round(max) + "/" + Math.Round(newValue);
                        else
                            _titleObject.text = Math.Round(newValue) + "/" + Math.Round(max);
                        break;

                    case ProgressTitleType.Value:
                        _titleObject.text = "" + Math.Round(newValue);
                        break;

                    case ProgressTitleType.Max:
                        _titleObject.text = "" + Math.Round(_max);
                        break;
                }

            var fullWidth = width - _barMaxWidthDelta;
            var fullHeight = height - _barMaxHeightDelta;
            if (!reverse)
            {
                if (_barObjectH != null)
                    if (!SetFillAmount(_barObjectH, percent))
                        _barObjectH.width = Mathf.RoundToInt(fullWidth * percent);
                if (_barObjectV != null)
                    if (!SetFillAmount(_barObjectV, percent))
                        _barObjectV.height = Mathf.RoundToInt(fullHeight * percent);
            }
            else
            {
                if (_barObjectH != null)
                    if (!SetFillAmount(_barObjectH, 1 - percent))
                    {
                        _barObjectH.width = Mathf.RoundToInt(fullWidth * percent);
                        _barObjectH.x = _barStartX + (fullWidth - _barObjectH.width);
                    }

                if (_barObjectV != null)
                    if (!SetFillAmount(_barObjectV, 1 - percent))
                    {
                        _barObjectV.height = Mathf.RoundToInt(fullHeight * percent);
                        _barObjectV.y = _barStartY + (fullHeight - _barObjectV.height);
                    }
            }

            if (_aniObject != null)
                _aniObject.frame = Mathf.RoundToInt(percent * 100);

            InvalidateBatchingState(true);
        }

        private bool SetFillAmount(GObject bar, float amount)
        {
            if (bar is GImage && ((GImage)bar).fillMethod != FillMethod.None)
                ((GImage)bar).fillAmount = amount;
            else if (bar is GLoader && ((GLoader)bar).fillMethod != FillMethod.None)
                ((GLoader)bar).fillAmount = amount;
            else
                return false;

            return true;
        }

        protected override void ConstructExtension(ByteBuffer buffer)
        {
            buffer.Seek(0, 6);

            _titleType = (ProgressTitleType)buffer.ReadByte();
            reverse = buffer.ReadBool();

            _titleObject = GetChild("title");
            _barObjectH = GetChild("bar");
            _barObjectV = GetChild("bar_v");
            _aniObject = GetChild("ani") as GMovieClip;

            if (_barObjectH != null)
            {
                _barMaxWidth = _barObjectH.width;
                _barMaxWidthDelta = width - _barMaxWidth;
                _barStartX = _barObjectH.x;
            }

            if (_barObjectV != null)
            {
                _barMaxHeight = _barObjectV.height;
                _barMaxHeightDelta = height - _barMaxHeight;
                _barStartY = _barObjectV.y;
            }
        }

        public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);

            if (!buffer.Seek(beginPos, 6))
            {
                Update(_value);
                return;
            }

            if ((ObjectType)buffer.ReadByte() != packageItem.objectType)
            {
                Update(_value);
                return;
            }

            _value = buffer.ReadInt();
            _max = buffer.ReadInt();
            if (buffer.version >= 2)
                _min = buffer.ReadInt();

            if (buffer.version >= 5)
            {
                var sound = buffer.ReadS();
                if (!string.IsNullOrEmpty(sound))
                {
                    var volumeScale = buffer.ReadFloat();
                    displayObject.onClick.Add(() =>
                    {
                        var audioClip = UIPackage.GetItemAssetByURL(sound) as NAudioClip;
                        if (audioClip != null && audioClip.nativeClip != null)
                            Stage.inst.PlayOneShotSound(audioClip.nativeClip, volumeScale);
                    });
                }
                else
                {
                    buffer.Skip(4);
                }
            }

            Update(_value);
        }

        protected override void HandleSizeChanged()
        {
            base.HandleSizeChanged();

            if (_barObjectH != null)
                _barMaxWidth = width - _barMaxWidthDelta;
            if (_barObjectV != null)
                _barMaxHeight = height - _barMaxHeightDelta;

            if (!underConstruct)
                Update(_value);
        }
    }
}