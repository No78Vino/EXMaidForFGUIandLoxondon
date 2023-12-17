using System;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class GSlider : GComponent
    {
        private float _barMaxHeight;
        private float _barMaxHeightDelta;
        private float _barMaxWidth;
        private float _barMaxWidthDelta;
        private GObject _barObjectH;
        private GObject _barObjectV;
        private float _barStartX;
        private float _barStartY;
        private float _clickPercent;
        private Vector2 _clickPos;
        private GObject _gripObject;
        private double _max;
        private double _min;

        private EventListener _onChanged;
        private EventListener _onGripTouchEnd;
        private bool _reverse;

        private GObject _titleObject;
        private ProgressTitleType _titleType;
        private double _value;
        private bool _wholeNumbers;
        public bool canDrag;

        public bool changeOnClick;

        public GSlider()
        {
            _value = 50;
            _max = 100;
            changeOnClick = true;
            canDrag = true;
        }

        /// <summary>
        /// </summary>
        public EventListener onChanged => _onChanged ?? (_onChanged = new EventListener(this, "onChanged"));

        /// <summary>
        /// </summary>
        public EventListener onGripTouchEnd =>
            _onGripTouchEnd ?? (_onGripTouchEnd = new EventListener(this, "onGripTouchEnd"));

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
                    Update();
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
                    Update();
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
                    Update();
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
                    _value = value;
                    Update();
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool wholeNumbers
        {
            get => _wholeNumbers;
            set
            {
                if (_wholeNumbers != value)
                {
                    _wholeNumbers = value;
                    Update();
                }
            }
        }

        private void Update()
        {
            UpdateWithPercent((float)((_value - _min) / (_max - _min)), false);
        }

        private void UpdateWithPercent(float percent, bool manual)
        {
            percent = Mathf.Clamp01(percent);
            if (manual)
            {
                var newValue = _min + (_max - _min) * percent;
                if (newValue < _min)
                    newValue = _min;
                if (newValue > _max)
                    newValue = _max;
                if (_wholeNumbers)
                {
                    newValue = Math.Round(newValue);
                    percent = Mathf.Clamp01((float)((newValue - _min) / (_max - _min)));
                }

                if (newValue != _value)
                {
                    _value = newValue;
                    if (DispatchEvent("onChanged", null))
                        return;
                }
            }

            if (_titleObject != null)
                switch (_titleType)
                {
                    case ProgressTitleType.Percent:
                        _titleObject.text = Mathf.FloorToInt(percent * 100) + "%";
                        break;

                    case ProgressTitleType.ValueAndMax:
                        _titleObject.text = Math.Round(_value) + "/" + Math.Round(max);
                        break;

                    case ProgressTitleType.Value:
                        _titleObject.text = "" + Math.Round(_value);
                        break;

                    case ProgressTitleType.Max:
                        _titleObject.text = "" + Math.Round(_max);
                        break;
                }

            var fullWidth = width - _barMaxWidthDelta;
            var fullHeight = height - _barMaxHeightDelta;
            if (!_reverse)
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
            _reverse = buffer.ReadBool();
            if (buffer.version >= 2)
            {
                _wholeNumbers = buffer.ReadBool();
                changeOnClick = buffer.ReadBool();
            }

            _titleObject = GetChild("title");
            _barObjectH = GetChild("bar");
            _barObjectV = GetChild("bar_v");
            _gripObject = GetChild("grip");

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

            if (_gripObject != null)
            {
                _gripObject.onTouchBegin.Add(__gripTouchBegin);
                _gripObject.onTouchMove.Add(__gripTouchMove);
                _gripObject.onTouchEnd.Add(__gripTouchEnd);
            }

            onTouchBegin.Add(__barTouchBegin);
        }

        public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);

            if (!buffer.Seek(beginPos, 6))
            {
                Update();
                return;
            }

            if ((ObjectType)buffer.ReadByte() != packageItem.objectType)
            {
                Update();
                return;
            }

            _value = buffer.ReadInt();
            _max = buffer.ReadInt();
            if (buffer.version >= 2)
                _min = buffer.ReadInt();


            Update();
        }

        protected override void HandleSizeChanged()
        {
            base.HandleSizeChanged();

            if (_barObjectH != null)
                _barMaxWidth = width - _barMaxWidthDelta;
            if (_barObjectV != null)
                _barMaxHeight = height - _barMaxHeightDelta;

            if (!underConstruct)
                Update();
        }

        private void __gripTouchBegin(EventContext context)
        {
            canDrag = true;

            context.StopPropagation();

            var evt = context.inputEvent;
            if (evt.button != 0)
                return;

            context.CaptureTouch();

            _clickPos = GlobalToLocal(new Vector2(evt.x, evt.y));
            _clickPercent = Mathf.Clamp01((float)((_value - _min) / (_max - _min)));
        }

        private void __gripTouchMove(EventContext context)
        {
            if (!canDrag)
                return;

            var evt = context.inputEvent;
            var pt = GlobalToLocal(new Vector2(evt.x, evt.y));
            if (float.IsNaN(pt.x))
                return;

            var deltaX = pt.x - _clickPos.x;
            var deltaY = pt.y - _clickPos.y;
            if (_reverse)
            {
                deltaX = -deltaX;
                deltaY = -deltaY;
            }

            float percent;
            if (_barObjectH != null)
                percent = _clickPercent + deltaX / _barMaxWidth;
            else
                percent = _clickPercent + deltaY / _barMaxHeight;

            UpdateWithPercent(percent, true);
        }

        private void __gripTouchEnd(EventContext context)
        {
            DispatchEvent("onGripTouchEnd", null);
        }

        private void __barTouchBegin(EventContext context)
        {
            if (!changeOnClick)
                return;

            var evt = context.inputEvent;
            var pt = _gripObject.GlobalToLocal(new Vector2(evt.x, evt.y));
            var percent = Mathf.Clamp01((float)((_value - _min) / (_max - _min)));
            float delta = 0;
            if (_barObjectH != null)
                delta = (pt.x - _gripObject.width / 2) / _barMaxWidth;
            if (_barObjectV != null)
                delta = (pt.y - _gripObject.height / 2) / _barMaxHeight;
            if (_reverse)
                percent -= delta;
            else
                percent += delta;

            UpdateWithPercent(percent, true);
        }
    }
}