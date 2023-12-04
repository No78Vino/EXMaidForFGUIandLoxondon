using System;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     GGroup class.
    ///     组对象，对应编辑器里的高级组。
    /// </summary>
    public class GGroup : GObject
    {
        private bool _autoSizeDisabled;
        private bool _boundsChanged;
        private int _columnGap;

        private bool _excludeInvisibles;
        private GroupLayoutType _layout;
        private int _lineGap;
        private int _mainChildIndex;
        private int _mainGridIndex;
        private int _mainGridMinSize;
        private int _numChildren;

        private bool _percentReady;

        private readonly Action _refreshDelegate;
        private float _totalSize;
        internal int _updating;

        public GGroup()
        {
            _mainGridIndex = -1;
            _mainChildIndex = -1;
            _mainGridMinSize = 50;
            _refreshDelegate = EnsureBoundsCorrect;
        }

        /// <summary>
        ///     Group layout type.
        /// </summary>
        public GroupLayoutType layout
        {
            get => _layout;
            set
            {
                if (_layout != value)
                {
                    _layout = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        /// <summary>
        /// </summary>
        public int lineGap
        {
            get => _lineGap;
            set
            {
                if (_lineGap != value)
                {
                    _lineGap = value;
                    SetBoundsChangedFlag(true);
                }
            }
        }

        /// <summary>
        /// </summary>
        public int columnGap
        {
            get => _columnGap;
            set
            {
                if (_columnGap != value)
                {
                    _columnGap = value;
                    SetBoundsChangedFlag(true);
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool excludeInvisibles
        {
            get => _excludeInvisibles;
            set
            {
                if (_excludeInvisibles != value)
                {
                    _excludeInvisibles = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool autoSizeDisabled
        {
            get => _autoSizeDisabled;
            set
            {
                if (_autoSizeDisabled != value)
                {
                    _autoSizeDisabled = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        /// <summary>
        /// </summary>
        public int mainGridMinSize
        {
            get => _mainGridMinSize;
            set
            {
                if (_mainGridMinSize != value)
                {
                    _mainGridMinSize = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        /// <summary>
        /// </summary>
        public int mainGridIndex
        {
            get => _mainGridIndex;
            set
            {
                if (_mainGridIndex != value)
                {
                    _mainGridIndex = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        /// <summary>
        ///     Update group bounds.
        ///     更新组的包围.
        /// </summary>
        public void SetBoundsChangedFlag(bool positionChangedOnly = false)
        {
            if (_updating == 0 && parent != null)
            {
                if (!positionChangedOnly)
                    _percentReady = false;

                if (!_boundsChanged)
                {
                    _boundsChanged = true;

                    if (_layout != GroupLayoutType.None)
                    {
                        UpdateContext.OnBegin -= _refreshDelegate;
                        UpdateContext.OnBegin += _refreshDelegate;
                    }
                }
            }
        }

        public void EnsureBoundsCorrect()
        {
            if (parent == null || !_boundsChanged)
                return;

            UpdateContext.OnBegin -= _refreshDelegate;
            _boundsChanged = false;

            if (_autoSizeDisabled)
            {
                ResizeChildren(0, 0);
            }
            else
            {
                HandleLayout();
                UpdateBounds();
            }
        }

        private void UpdateBounds()
        {
            var cnt = parent.numChildren;
            int i;
            GObject child;
            float ax = int.MaxValue, ay = int.MaxValue;
            float ar = int.MinValue, ab = int.MinValue;
            float tmp;
            var empty = true;
            var skipInvisibles = _layout != GroupLayoutType.None && _excludeInvisibles;

            for (i = 0; i < cnt; i++)
            {
                child = parent.GetChildAt(i);
                if (child.group != this)
                    continue;

                if (skipInvisibles && !child.internalVisible3)
                    continue;

                tmp = child.xMin;
                if (tmp < ax)
                    ax = tmp;
                tmp = child.yMin;
                if (tmp < ay)
                    ay = tmp;
                tmp = child.xMin + child.width;
                if (tmp > ar)
                    ar = tmp;
                tmp = child.yMin + child.height;
                if (tmp > ab)
                    ab = tmp;

                empty = false;
            }

            float w;
            float h;
            if (!empty)
            {
                _updating |= 1;
                SetXY(ax, ay);
                _updating &= 2;

                w = ar - ax;
                h = ab - ay;
            }
            else
            {
                w = h = 0;
            }

            if ((_updating & 2) == 0)
            {
                _updating |= 2;
                SetSize(w, h);
                _updating &= 1;
            }
            else
            {
                _updating &= 1;
                ResizeChildren(_width - w, _height - h);
            }
        }

        private void HandleLayout()
        {
            _updating |= 1;

            if (_layout == GroupLayoutType.Horizontal)
            {
                var curX = x;
                var cnt = parent.numChildren;
                for (var i = 0; i < cnt; i++)
                {
                    var child = parent.GetChildAt(i);
                    if (child.group != this)
                        continue;
                    if (_excludeInvisibles && !child.internalVisible3)
                        continue;

                    child.xMin = curX;
                    if (child.width != 0)
                        curX += child.width + _columnGap;
                }
            }
            else if (_layout == GroupLayoutType.Vertical)
            {
                var curY = y;
                var cnt = parent.numChildren;
                for (var i = 0; i < cnt; i++)
                {
                    var child = parent.GetChildAt(i);
                    if (child.group != this)
                        continue;
                    if (_excludeInvisibles && !child.internalVisible3)
                        continue;

                    child.yMin = curY;
                    if (child.height != 0)
                        curY += child.height + _lineGap;
                }
            }

            _updating &= 2;
        }

        internal void MoveChildren(float dx, float dy)
        {
            if ((_updating & 1) != 0 || parent == null)
                return;

            _updating |= 1;

            var cnt = parent.numChildren;
            int i;
            GObject child;
            for (i = 0; i < cnt; i++)
            {
                child = parent.GetChildAt(i);
                if (child.group == this) child.SetXY(child.x + dx, child.y + dy);
            }

            _updating &= 2;
        }

        internal void ResizeChildren(float dw, float dh)
        {
            if (_layout == GroupLayoutType.None || (_updating & 2) != 0 || parent == null)
                return;

            _updating |= 2;

            if (_boundsChanged)
            {
                _boundsChanged = false;
                if (!_autoSizeDisabled)
                {
                    UpdateBounds();
                    return;
                }
            }

            var cnt = parent.numChildren;

            if (!_percentReady)
            {
                _percentReady = true;
                _numChildren = 0;
                _totalSize = 0;
                _mainChildIndex = -1;

                var j = 0;
                for (var i = 0; i < cnt; i++)
                {
                    var child = parent.GetChildAt(i);
                    if (child.group != this)
                        continue;

                    if (!_excludeInvisibles || child.internalVisible3)
                    {
                        if (j == _mainGridIndex)
                            _mainChildIndex = i;

                        _numChildren++;

                        if (_layout == GroupLayoutType.Horizontal)
                            _totalSize += child.width;
                        else
                            _totalSize += child.height;
                    }

                    j++;
                }

                if (_mainChildIndex != -1)
                {
                    if (_layout == GroupLayoutType.Horizontal)
                    {
                        var child = parent.GetChildAt(_mainChildIndex);
                        _totalSize += _mainGridMinSize - child.width;
                        child._sizePercentInGroup = _mainGridMinSize / _totalSize;
                    }
                    else
                    {
                        var child = parent.GetChildAt(_mainChildIndex);
                        _totalSize += _mainGridMinSize - child.height;
                        child._sizePercentInGroup = _mainGridMinSize / _totalSize;
                    }
                }

                for (var i = 0; i < cnt; i++)
                {
                    var child = parent.GetChildAt(i);
                    if (child.group != this)
                        continue;

                    if (i == _mainChildIndex)
                        continue;

                    if (_totalSize > 0)
                        child._sizePercentInGroup =
                            (_layout == GroupLayoutType.Horizontal ? child.width : child.height) / _totalSize;
                    else
                        child._sizePercentInGroup = 0;
                }
            }

            float remainSize = 0;
            float remainPercent = 1;
            var priorHandled = false;

            if (_layout == GroupLayoutType.Horizontal)
            {
                remainSize = width - (_numChildren - 1) * _columnGap;
                if (_mainChildIndex != -1 && remainSize >= _totalSize)
                {
                    var child = parent.GetChildAt(_mainChildIndex);
                    child.SetSize(remainSize - (_totalSize - _mainGridMinSize), child._rawHeight + dh, true);
                    remainSize -= child.width;
                    remainPercent -= child._sizePercentInGroup;
                    priorHandled = true;
                }

                var curX = x;
                for (var i = 0; i < cnt; i++)
                {
                    var child = parent.GetChildAt(i);
                    if (child.group != this)
                        continue;

                    if (_excludeInvisibles && !child.internalVisible3)
                    {
                        child.SetSize(child._rawWidth, child._rawHeight + dh, true);
                        continue;
                    }

                    if (!priorHandled || i != _mainChildIndex)
                    {
                        child.SetSize(Mathf.Round(child._sizePercentInGroup / remainPercent * remainSize),
                            child._rawHeight + dh, true);
                        remainPercent -= child._sizePercentInGroup;
                        remainSize -= child.width;
                    }

                    child.xMin = curX;
                    if (child.width != 0)
                        curX += child.width + _columnGap;
                }
            }
            else
            {
                remainSize = height - (_numChildren - 1) * _lineGap;
                if (_mainChildIndex != -1 && remainSize >= _totalSize)
                {
                    var child = parent.GetChildAt(_mainChildIndex);
                    child.SetSize(child._rawWidth + dw, remainSize - (_totalSize - _mainGridMinSize), true);
                    remainSize -= child.height;
                    remainPercent -= child._sizePercentInGroup;
                    priorHandled = true;
                }

                var curY = y;
                for (var i = 0; i < cnt; i++)
                {
                    var child = parent.GetChildAt(i);
                    if (child.group != this)
                        continue;

                    if (_excludeInvisibles && !child.internalVisible3)
                    {
                        child.SetSize(child._rawWidth + dw, child._rawHeight, true);
                        continue;
                    }

                    if (!priorHandled || i != _mainChildIndex)
                    {
                        child.SetSize(child._rawWidth + dw,
                            Mathf.Round(child._sizePercentInGroup / remainPercent * remainSize), true);
                        remainPercent -= child._sizePercentInGroup;
                        remainSize -= child.height;
                    }

                    child.yMin = curY;
                    if (child.height != 0)
                        curY += child.height + _lineGap;
                }
            }

            _updating &= 1;
        }

        protected override void HandleAlphaChanged()
        {
            base.HandleAlphaChanged();

            if (underConstruct || parent == null)
                return;

            var cnt = parent.numChildren;
            var a = alpha;
            for (var i = 0; i < cnt; i++)
            {
                var child = parent.GetChildAt(i);
                if (child.group == this)
                    child.alpha = a;
            }
        }

        protected internal override void HandleVisibleChanged()
        {
            if (parent == null)
                return;

            var cnt = parent.numChildren;
            for (var i = 0; i < cnt; i++)
            {
                var child = parent.GetChildAt(i);
                if (child.group == this)
                    child.HandleVisibleChanged();
            }
        }

        public override void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_BeforeAdd(buffer, beginPos);

            buffer.Seek(beginPos, 5);

            _layout = (GroupLayoutType)buffer.ReadByte();
            _lineGap = buffer.ReadInt();
            _columnGap = buffer.ReadInt();
            if (buffer.version >= 2)
            {
                _excludeInvisibles = buffer.ReadBool();
                _autoSizeDisabled = buffer.ReadBool();
                _mainGridIndex = buffer.ReadShort();
            }
        }

        public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);

            if (!visible)
                HandleVisibleChanged();
        }
    }
}