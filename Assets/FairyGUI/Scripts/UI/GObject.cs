using System;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    public class GObject : EventDispatcher
    {
        internal static uint _gInstanceCounter;
        private float _alpha;
        internal bool _disposed;
        private bool _draggable;
        internal bool _gearLocked;

        private readonly GearBase[] _gears;
        private bool _grayed;
        private GGroup _group;
        private bool _handlingController;
        internal float _height;
        private bool _internalVisible;
        private EventListener _onAddedToStage;

        private EventListener _onClick;
        private EventListener _onClickLink;
        private EventListener _onDragEnd;
        private EventListener _onDragMove;
        private EventListener _onDragStart;
        private EventListener _onFocusIn;
        private EventListener _onFocusOut;
        private EventListener _onGearStop;
        private EventListener _onKeyDown;
        private EventListener _onPositionChanged;
        private EventListener _onRemovedFromStage;
        private EventListener _onRightClick;
        private EventListener _onRollOut;
        private EventListener _onRollOver;
        private EventListener _onSizeChanged;
        private EventListener _onTouchBegin;
        private EventListener _onTouchEnd;
        private EventListener _onTouchMove;
        private bool _pivotAsAnchor;
        private float _pivotX;
        private float _pivotY;
        internal float _rawHeight;
        internal float _rawWidth;
        private float _rotation;
        private float _rotationX;
        private float _rotationY;
        private float _scaleX;
        private float _scaleY;
        internal float _sizePercentInGroup;
        private int _sortingOrder;
        private string _tooltips;
        private bool _touchable;
        internal GTreeNode _treeNode;
        private bool _visible;
        internal float _width;

        private float _x;
        private float _y;
        private float _z;

        /// <summary>
        ///     User defined data.
        /// </summary>
        public object data;

        /// <summary>
        ///     Restricted range of dragging.
        /// </summary>
        public Rect? dragBounds;

        /// <summary>
        ///     The initial height of the object.
        /// </summary>
        public int initHeight;

        /// <summary>
        ///     The initial width of the object.
        /// </summary>
        public int initWidth;

        /// <summary>
        /// </summary>
        public int maxHeight;

        /// <summary>
        /// </summary>
        public int maxWidth;

        /// <summary>
        /// </summary>
        public int minHeight;

        /// <summary>
        /// </summary>
        public int minWidth;

        /// <summary>
        ///     Name of the object.
        /// </summary>
        public string name;

        /// <summary>
        /// </summary>
        public PackageItem packageItem;

        /// <summary>
        ///     The source height of the object.
        /// </summary>
        public int sourceHeight;

        /// <summary>
        ///     The source width of the object.
        /// </summary>
        public int sourceWidth;

        protected internal bool underConstruct;

        public GObject()
        {
            _width = 0;
            _height = 0;
            _alpha = 1;
            _visible = true;
            _touchable = true;
            _scaleX = 1;
            _scaleY = 1;
            _internalVisible = true;
            id = "_n" + _gInstanceCounter++;
            name = string.Empty;

            CreateDisplayObject();

            relations = new Relations(this);
            _gears = new GearBase[10];
        }

        /// <summary>
        ///     GObject的id，仅作为内部使用。与name不同，id值是不会相同的。
        ///     id is for internal use only.
        /// </summary>
        public string id { get; private set; }

        /// <summary>
        ///     Relations Object.
        /// </summary>
        public Relations relations { get; private set; }

        /// <summary>
        ///     Parent object.
        /// </summary>
        public GComponent parent { get; private set; }

        /// <summary>
        ///     Lowlevel display object.
        /// </summary>
        public DisplayObject displayObject { get; protected set; }

        /// <summary>
        ///     当前全局正在被拖动的对象
        /// </summary>
        public static GObject draggingObject { get; private set; }

        /// <summary>
        ///     Dispatched when the object or its child was clicked.
        /// </summary>
        public EventListener onClick => _onClick ?? (_onClick = new EventListener(this, "onClick"));

        /// <summary>
        ///     Dispatched when the object or its child was clicked by right mouse button. Web only.
        /// </summary>
        public EventListener onRightClick => _onRightClick ?? (_onRightClick = new EventListener(this, "onRightClick"));

        /// <summary>
        ///     Dispatched when the finger touched the object or its child just now.
        /// </summary>
        public EventListener onTouchBegin => _onTouchBegin ?? (_onTouchBegin = new EventListener(this, "onTouchBegin"));

        /// <summary>
        /// </summary>
        public EventListener onTouchMove => _onTouchMove ?? (_onTouchMove = new EventListener(this, "onTouchMove"));

        /// <summary>
        ///     Dispatched when the finger was lifted from the screen or from the mouse button.
        /// </summary>
        public EventListener onTouchEnd => _onTouchEnd ?? (_onTouchEnd = new EventListener(this, "onTouchEnd"));

        /// <summary>
        ///     The cursor or finger hovers over an object.
        /// </summary>
        public EventListener onRollOver => _onRollOver ?? (_onRollOver = new EventListener(this, "onRollOver"));

        /// <summary>
        ///     The cursor or finger leave an object.
        /// </summary>
        public EventListener onRollOut => _onRollOut ?? (_onRollOut = new EventListener(this, "onRollOut"));

        /// <summary>
        ///     Dispatched when the object was added to the stage.
        /// </summary>
        public EventListener onAddedToStage =>
            _onAddedToStage ?? (_onAddedToStage = new EventListener(this, "onAddedToStage"));

        /// <summary>
        ///     Dispatched when the object was removed from the stage.
        /// </summary>
        public EventListener onRemovedFromStage => _onRemovedFromStage ??
                                                   (_onRemovedFromStage =
                                                       new EventListener(this, "onRemovedFromStage"));

        /// <summary>
        ///     Dispatched on key pressed when the object is in focus.
        /// </summary>
        public EventListener onKeyDown => _onKeyDown ?? (_onKeyDown = new EventListener(this, "onKeyDown"));

        /// <summary>
        ///     Dispatched when links in the object or its child was clicked.
        /// </summary>
        public EventListener onClickLink => _onClickLink ?? (_onClickLink = new EventListener(this, "onClickLink"));

        /// <summary>
        ///     Dispatched when the object was moved.
        /// </summary>
        public EventListener onPositionChanged =>
            _onPositionChanged ?? (_onPositionChanged = new EventListener(this, "onPositionChanged"));

        /// <summary>
        ///     Dispatched when the object was resized.
        /// </summary>
        public EventListener onSizeChanged =>
            _onSizeChanged ?? (_onSizeChanged = new EventListener(this, "onSizeChanged"));

        /// <summary>
        ///     Dispatched when drag start.
        /// </summary>
        public EventListener onDragStart => _onDragStart ?? (_onDragStart = new EventListener(this, "onDragStart"));

        /// <summary>
        ///     Dispatched when dragging.
        /// </summary>
        public EventListener onDragMove => _onDragMove ?? (_onDragMove = new EventListener(this, "onDragMove"));

        /// <summary>
        ///     Dispatched when drag end.
        /// </summary>
        public EventListener onDragEnd => _onDragEnd ?? (_onDragEnd = new EventListener(this, "onDragEnd"));

        /// <summary>
        /// </summary>
        public EventListener onGearStop => _onGearStop ?? (_onGearStop = new EventListener(this, "onGearStop"));

        /// <summary>
        /// </summary>
        public EventListener onFocusIn => _onFocusIn ?? (_onFocusIn = new EventListener(this, "onFocusIn"));

        /// <summary>
        /// </summary>
        public EventListener onFocusOut => _onFocusOut ?? (_onFocusOut = new EventListener(this, "onFocusOut"));

        /// <summary>
        ///     The x coordinate of the object relative to the local coordinates of the parent.
        /// </summary>
        public float x
        {
            get => _x;
            set => SetPosition(value, _y, _z);
        }

        /// <summary>
        ///     The y coordinate of the object relative to the local coordinates of the parent.
        /// </summary>
        public float y
        {
            get => _y;
            set => SetPosition(_x, value, _z);
        }

        /// <summary>
        ///     The z coordinate of the object relative to the local coordinates of the parent.
        /// </summary>
        public float z
        {
            get => _z;
            set => SetPosition(_x, _y, value);
        }

        /// <summary>
        ///     The x and y coordinates of the object relative to the local coordinates of the parent.
        /// </summary>
        public Vector2 xy
        {
            get => new(_x, _y);
            set => SetPosition(value.x, value.y, _z);
        }

        /// <summary>
        ///     The x,y,z coordinates of the object relative to the local coordinates of the parent.
        /// </summary>
        public Vector3 position
        {
            get => new(_x, _y, _z);
            set => SetPosition(value.x, value.y, value.z);
        }

        [Obsolete("Use UIConfig.makePixelPerfect or DisplayObject.pixelPerfect")]
        public bool pixelSnapping
        {
            get => false;
            set { }
        }

        /// <summary>
        ///     The width of the object in pixels.
        /// </summary>
        public float width
        {
            get => _width;
            set => SetSize(value, _rawHeight);
        }

        /// <summary>
        ///     The height of the object in pixels.
        /// </summary>
        public float height
        {
            get => _height;
            set => SetSize(_rawWidth, value);
        }

        /// <summary>
        ///     The size of the object in pixels.
        /// </summary>
        public Vector2 size
        {
            get => new(width, height);
            set => SetSize(value.x, value.y);
        }

        /// <summary>
        ///     actualWidth = width * scalex
        /// </summary>
        public float actualWidth => width * _scaleX;

        /// <summary>
        ///     actualHeight = height * scaleY
        /// </summary>
        public float actualHeight => height * _scaleY;

        /// <summary>
        /// </summary>
        public float xMin
        {
            get => _pivotAsAnchor ? _x - _width * _pivotX : _x;
            set
            {
                if (_pivotAsAnchor)
                    SetPosition(value + _width * _pivotX, _y, _z);
                else
                    SetPosition(value, _y, _z);
            }
        }

        /// <summary>
        /// </summary>
        public float yMin
        {
            get => _pivotAsAnchor ? _y - _height * _pivotY : _y;
            set
            {
                if (_pivotAsAnchor)
                    SetPosition(_x, value + _height * _pivotY, _z);
                else
                    SetPosition(_x, value, _z);
            }
        }

        /// <summary>
        ///     The horizontal scale factor. '1' means no scale, cannt be negative.
        /// </summary>
        public float scaleX
        {
            get => _scaleX;
            set => SetScale(value, _scaleY);
        }

        /// <summary>
        ///     The vertical scale factor. '1' means no scale, cannt be negative.
        /// </summary>
        public float scaleY
        {
            get => _scaleY;
            set => SetScale(_scaleX, value);
        }

        /// <summary>
        ///     The scale factor.
        /// </summary>
        public Vector2 scale
        {
            get => new(_scaleX, _scaleY);
            set => SetScale(value.x, value.y);
        }

        /// <summary>
        /// </summary>
        public Vector2 skew
        {
            get
            {
                if (displayObject != null)
                    return displayObject.skew;
                return Vector2.zero;
            }

            set
            {
                if (displayObject != null)
                    displayObject.skew = value;
            }
        }

        /// <summary>
        ///     The x coordinate of the object's origin in its own coordinate space.
        /// </summary>
        public float pivotX
        {
            get => _pivotX;
            set => SetPivot(value, _pivotY, _pivotAsAnchor);
        }

        /// <summary>
        ///     The y coordinate of the object's origin in its own coordinate space.
        /// </summary>
        public float pivotY
        {
            get => _pivotY;
            set => SetPivot(_pivotX, value, _pivotAsAnchor);
        }

        /// <summary>
        ///     The x and y coordinates of the object's origin in its own coordinate space.
        /// </summary>
        public Vector2 pivot
        {
            get => new(_pivotX, _pivotY);
            set => SetPivot(value.x, value.y, _pivotAsAnchor);
        }

        public bool pivotAsAnchor
        {
            get => _pivotAsAnchor;
            set => SetPivot(_pivotX, _pivotY, value);
        }

        /// <summary>
        ///     If the object can touch or click. GImage/GTextField is not touchable even it is true.
        /// </summary>
        public bool touchable
        {
            get => _touchable;
            set
            {
                if (_touchable != value)
                {
                    _touchable = value;
                    UpdateGear(3);

                    if (displayObject != null)
                        displayObject.touchable = _touchable;
                }
            }
        }

        /// <summary>
        ///     If true, apply a grayed effect on this object.
        /// </summary>
        public bool grayed
        {
            get => _grayed;
            set
            {
                if (_grayed != value)
                {
                    _grayed = value;
                    HandleGrayedChanged();
                    UpdateGear(3);
                }
            }
        }

        /// <summary>
        ///     Enabled is shortcut for grayed and !touchable combination.
        /// </summary>
        public bool enabled
        {
            get => !_grayed && _touchable;
            set
            {
                grayed = !value;
                touchable = value;
            }
        }

        /// <summary>
        ///     The rotation around the z axis of the object in degrees.
        /// </summary>
        public float rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                if (displayObject != null)
                    displayObject.rotation = _rotation;
                UpdateGear(3);
            }
        }

        /// <summary>
        ///     The rotation around the x axis of the object in degrees.
        /// </summary>
        public float rotationX
        {
            get => _rotationX;
            set
            {
                _rotationX = value;
                if (displayObject != null)
                    displayObject.rotationX = _rotationX;
            }
        }

        /// <summary>
        ///     The rotation around the y axis of the object in degrees.
        /// </summary>
        public float rotationY
        {
            get => _rotationY;
            set
            {
                _rotationY = value;
                if (displayObject != null)
                    displayObject.rotationY = _rotationY;
            }
        }

        /// <summary>
        ///     The opacity of the object. 0 = transparent, 1 = opaque.
        /// </summary>
        public float alpha
        {
            get => _alpha;

            set
            {
                _alpha = value;
                HandleAlphaChanged();
                UpdateGear(3);
            }
        }

        /// <summary>
        ///     The visibility of the object. An invisible object will be untouchable.
        /// </summary>
        public bool visible
        {
            get => _visible;

            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    HandleVisibleChanged();
                    if (parent != null)
                        parent.SetBoundsChangedFlag();
                    if (_group != null && _group.excludeInvisibles)
                        _group.SetBoundsChangedFlag();
                }
            }
        }

        internal bool internalVisible => _internalVisible && (group == null || group.internalVisible);

        internal bool internalVisible2 => _visible && (group == null || group.internalVisible2);

        internal bool internalVisible3 => _visible && _internalVisible;

        /// <summary>
        ///     By default(when sortingOrder==0), object added to component is arrange by the added roder.
        ///     The bigger is the sorting order, the object is more in front.
        /// </summary>
        public int sortingOrder
        {
            get => _sortingOrder;
            set
            {
                if (value < 0)
                    value = 0;
                if (_sortingOrder != value)
                {
                    var old = _sortingOrder;
                    _sortingOrder = value;
                    if (parent != null)
                        parent.ChildSortingOrderChanged(this, old, _sortingOrder);
                }
            }
        }

        /// <summary>
        ///     If the object can be focused?
        /// </summary>
        public bool focusable
        {
            get => displayObject != null && displayObject.focusable;
            set
            {
                if (displayObject != null) displayObject.focusable = value;
            }
        }

        /// <summary>
        ///     If the object can be navigated by TAB?
        /// </summary>
        public bool tabStop
        {
            get => displayObject != null && displayObject.tabStop;
            set
            {
                if (displayObject != null) displayObject.tabStop = value;
            }
        }

        /// <summary>
        ///     If the object is focused.
        /// </summary>
        public bool focused => displayObject != null && displayObject.focused;

        /// <summary>
        ///     Tooltips of this object. UIConfig.tooltipsWin must be set first.
        /// </summary>
        public string tooltips
        {
            get => _tooltips;
            set
            {
                if (!string.IsNullOrEmpty(_tooltips))
                {
                    onRollOver.Remove(__rollOver);
                    onRollOut.Remove(__rollOut);
                }

                _tooltips = value;
                if (!string.IsNullOrEmpty(_tooltips))
                {
                    onRollOver.Add(__rollOver);
                    onRollOut.Add(__rollOut);
                }
            }
        }


        /// <summary>
        /// </summary>
        /// <value></value>
        public string cursor
        {
            get => displayObject != null ? displayObject.cursor : null;
            set
            {
                if (displayObject != null)
                    displayObject.cursor = value;
            }
        }

        /// <summary>
        /// </summary>
        public virtual IFilter filter
        {
            get => displayObject != null ? displayObject.filter : null;
            set
            {
                if (displayObject != null) displayObject.filter = value;
            }
        }

        /// <summary>
        /// </summary>
        public virtual BlendMode blendMode
        {
            get => displayObject != null ? displayObject.blendMode : BlendMode.None;
            set
            {
                if (displayObject != null) displayObject.blendMode = value;
            }
        }

        /// <summary>
        ///     设定GameObject的名称
        /// </summary>
        public string gameObjectName
        {
            get
            {
                if (displayObject != null)
                    return displayObject.gameObject.name;
                return null;
            }

            set
            {
                if (displayObject != null)
                    displayObject.gameObject.name = value;
            }
        }

        /// <summary>
        ///     If the object has lowlevel displayobject and the displayobject has a container parent?
        /// </summary>
        public bool inContainer => displayObject != null && displayObject.parent != null;

        /// <summary>
        ///     If the object is on stage.
        /// </summary>
        public bool onStage => displayObject != null && displayObject.stage != null;

        /// <summary>
        ///     Resource url of this object.
        /// </summary>
        public string resourceURL
        {
            get
            {
                if (packageItem != null)
                    return UIPackage.URL_PREFIX + packageItem.owner.id + packageItem.id;
                return null;
            }
        }

        /// <summary>
        ///     Gear to xy controller.
        /// </summary>
        public GearXY gearXY => (GearXY)GetGear(1);

        /// <summary>
        ///     Gear to size controller.
        /// </summary>
        public GearSize gearSize => (GearSize)GetGear(2);

        /// <summary>
        ///     Gear to look controller.
        /// </summary>
        public GearLook gearLook => (GearLook)GetGear(3);

        /// <summary>
        ///     Group belonging to.
        /// </summary>
        public GGroup group
        {
            get => _group;
            set
            {
                if (_group != value)
                {
                    if (_group != null)
                        _group.SetBoundsChangedFlag();
                    _group = value;
                    if (_group != null)
                        _group.SetBoundsChangedFlag();
                    HandleVisibleChanged();
                    if (parent != null)
                        parent.ChildStateChanged(this);
                }
            }
        }

        /// <summary>
        /// </summary>
        public GRoot root
        {
            get
            {
                var p = this;
                while (p.parent != null)
                    p = p.parent;

                if (p is GRoot)
                    return (GRoot)p;

                if (p.displayObject != null && p.displayObject.parent != null)
                {
                    var d = p.displayObject.parent.GetChild("GRoot");
                    if (d != null && d.gOwner is GRoot)
                        return (GRoot)d.gOwner;
                }

                return GRoot.inst;
            }
        }

        /// <summary>
        /// </summary>
        public virtual string text
        {
            get => null;
            set
            {
                /*override in child*/
            }
        }

        /// <summary>
        /// </summary>
        public virtual string icon
        {
            get => null;
            set
            {
                /*override in child*/
            }
        }

        /// <summary>
        /// </summary>
        public bool draggable
        {
            get => _draggable;
            set
            {
                if (_draggable != value)
                {
                    _draggable = value;
                    InitDrag();
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool dragging => draggingObject == this;

        /// <summary>
        /// </summary>
        public bool isDisposed => _disposed;

        /// <summary>
        /// </summary>
        public GImage asImage => this as GImage;

        /// <summary>
        /// </summary>
        public GComponent asCom => this as GComponent;

        /// <summary>
        /// </summary>
        public GButton asButton => this as GButton;

        /// <summary>
        /// </summary>
        public GLabel asLabel => this as GLabel;

        /// <summary>
        /// </summary>
        public GProgressBar asProgress => this as GProgressBar;

        /// <summary>
        /// </summary>
        public GSlider asSlider => this as GSlider;

        /// <summary>
        /// </summary>
        public GComboBox asComboBox => this as GComboBox;

        /// <summary>
        /// </summary>
        public GTextField asTextField => this as GTextField;

        /// <summary>
        /// </summary>
        public GRichTextField asRichTextField => this as GRichTextField;

        /// <summary>
        /// </summary>
        public GTextInput asTextInput => this as GTextInput;

        /// <summary>
        /// </summary>
        public GLoader asLoader => this as GLoader;

        /// <summary>
        /// </summary>
        public GLoader3D asLoader3D => this as GLoader3D;

        /// <summary>
        /// </summary>
        public GList asList => this as GList;

        /// <summary>
        /// </summary>
        public GGraph asGraph => this as GGraph;

        /// <summary>
        /// </summary>
        public GGroup asGroup => this as GGroup;

        /// <summary>
        /// </summary>
        public GMovieClip asMovieClip => this as GMovieClip;

        /// <summary>
        /// </summary>
        public GTree asTree => this as GTree;

        /// <summary>
        /// </summary>
        public GTreeNode treeNode => _treeNode;

        /// <summary>
        ///     change the x and y coordinates of the object relative to the local coordinates of the parent.
        /// </summary>
        /// <param name="xv">x value.</param>
        /// <param name="yv">y value.</param>
        public void SetXY(float xv, float yv)
        {
            SetPosition(xv, yv, _z);
        }

        /// <summary>
        /// </summary>
        /// <param name="xv"></param>
        /// <param name="yv"></param>
        /// <param name="topLeft"></param>
        public void SetXY(float xv, float yv, bool topLeftValue)
        {
            if (_pivotAsAnchor)
                SetPosition(xv + _pivotX * _width, yv + _pivotY * _height, _z);
            else
                SetPosition(xv, yv, _z);
        }

        /// <summary>
        ///     change the x,y,z coordinates of the object relative to the local coordinates of the parent.
        /// </summary>
        /// <param name="xv">x value.</param>
        /// <param name="yv">y value.</param>
        /// <param name="zv">z value.</param>
        public void SetPosition(float xv, float yv, float zv)
        {
            if (_x != xv || _y != yv || _z != zv)
            {
                var dx = xv - _x;
                var dy = yv - _y;
                _x = xv;
                _y = yv;
                _z = zv;

                HandlePositionChanged();

                if (this is GGroup)
                    ((GGroup)this).MoveChildren(dx, dy);

                UpdateGear(1);

                if (parent != null && !(parent is GList))
                {
                    parent.SetBoundsChangedFlag();
                    if (_group != null)
                        _group.SetBoundsChangedFlag(true);
                    DispatchEvent("onPositionChanged", null);
                }

                if (draggingObject == this && !sUpdateInDragging)
                    sGlobalRect = LocalToGlobal(new Rect(0, 0, width, height));
            }
        }

        /// <summary>
        ///     Set the object in middle of the parent or GRoot if the parent is not set.
        /// </summary>
        public void Center()
        {
            Center(false);
        }

        /// <summary>
        ///     Set the object in middle of the parent or GRoot if the parent is not set.
        /// </summary>
        /// <param name="restraint">Add relations to maintain the center state.</param>
        public void Center(bool restraint)
        {
            GComponent r;
            if (parent != null)
                r = parent;
            else
                r = root;

            SetXY((int)((r.width - width) / 2), (int)((r.height - height) / 2), true);
            if (restraint)
            {
                AddRelation(r, RelationType.Center_Center);
                AddRelation(r, RelationType.Middle_Middle);
            }
        }

        /// <summary>
        ///     设置对象为全屏大小（逻辑屏幕）。
        /// </summary>
        public void MakeFullScreen()
        {
            SetSize(GRoot.inst.width, GRoot.inst.height);
        }

        /// <summary>
        ///     Change size.
        /// </summary>
        /// <param name="wv">Width value.</param>
        /// <param name="hv">Height value.</param>
        public void SetSize(float wv, float hv)
        {
            SetSize(wv, hv, false);
        }

        /// <summary>
        ///     Change size.
        /// </summary>
        /// <param name="wv">Width value.</param>
        /// <param name="hv">Height value.</param>
        /// <param name="ignorePivot">
        ///     If pivot is set, the object's positon will change when its size change. Set ignorePivot=true
        ///     to keep the position.
        /// </param>
        public void SetSize(float wv, float hv, bool ignorePivot)
        {
            if (_rawWidth != wv || _rawHeight != hv)
            {
                _rawWidth = wv;
                _rawHeight = hv;
                if (wv < minWidth)
                    wv = minWidth;
                else if (maxWidth > 0 && wv > maxWidth)
                    wv = maxWidth;
                if (hv < minHeight)
                    hv = minHeight;
                else if (maxHeight > 0 && hv > maxHeight)
                    hv = maxHeight;
                var dWidth = wv - _width;
                var dHeight = hv - _height;
                _width = wv;
                _height = hv;

                HandleSizeChanged();

                if (_pivotX != 0 || _pivotY != 0)
                {
                    if (!_pivotAsAnchor)
                    {
                        if (!ignorePivot)
                            SetXY(_x - _pivotX * dWidth, _y - _pivotY * dHeight);
                        else
                            HandlePositionChanged();
                    }
                    else
                    {
                        HandlePositionChanged();
                    }
                }

                if (this is GGroup)
                    ((GGroup)this).ResizeChildren(dWidth, dHeight);

                UpdateGear(2);

                if (parent != null)
                {
                    relations.OnOwnerSizeChanged(dWidth, dHeight, _pivotAsAnchor || !ignorePivot);
                    parent.SetBoundsChangedFlag();
                    if (_group != null)
                        _group.SetBoundsChangedFlag();
                }

                DispatchEvent("onSizeChanged", null);
            }
        }

        protected void SetSizeDirectly(float wv, float hv)
        {
            _rawWidth = wv;
            _rawHeight = hv;
            if (wv < 0)
                wv = 0;
            if (hv < 0)
                hv = 0;
            _width = wv;
            _height = hv;
        }

        /// <summary>
        ///     Change the scale factor.
        /// </summary>
        /// <param name="wv">The horizontal scale factor.</param>
        /// <param name="hv">The vertical scale factor</param>
        public void SetScale(float wv, float hv)
        {
            if (_scaleX != wv || _scaleY != hv)
            {
                _scaleX = wv;
                _scaleY = hv;
                HandleScaleChanged();

                UpdateGear(2);
            }
        }

        /// <summary>
        ///     Change the x and y coordinates of the object's origin in its own coordinate space.
        /// </summary>
        /// <param name="xv">x value in ratio</param>
        /// <param name="yv">y value in ratio</param>
        public void SetPivot(float xv, float yv)
        {
            SetPivot(xv, yv, false);
        }

        /// <summary>
        ///     Change the x and y coordinates of the object's origin in its own coordinate space.
        /// </summary>
        /// <param name="xv">x value in ratio</param>
        /// <param name="yv">y value in ratio</param>
        /// <param name="asAnchor">If use the pivot as the anchor position</param>
        public void SetPivot(float xv, float yv, bool asAnchor)
        {
            if (_pivotX != xv || _pivotY != yv || _pivotAsAnchor != asAnchor)
            {
                _pivotX = xv;
                _pivotY = yv;
                _pivotAsAnchor = asAnchor;
                if (displayObject != null)
                    displayObject.pivot = new Vector2(_pivotX, _pivotY);
                HandlePositionChanged();
            }
        }

        /// <summary>
        ///     Request focus on this object.
        /// </summary>
        public void RequestFocus()
        {
            if (displayObject != null)
                Stage.inst.SetFocus(displayObject);
        }

        /// <summary>
        ///     Request focus on this object.
        /// </summary>
        public void RequestFocus(bool byKey)
        {
            if (displayObject != null)
                Stage.inst.SetFocus(displayObject, byKey);
        }

        private void __rollOver()
        {
            root.ShowTooltips(tooltips);
        }

        private void __rollOut()
        {
            root.HideTooltips();
        }

        /// <summary>
        /// </summary>
        /// <param name="obj"></param>
        public void SetHome(GObject obj)
        {
            if (obj != null && displayObject != null && obj.displayObject != null)
                displayObject.home = obj.displayObject.cachedTransform;
        }

        public GearBase GetGear(int index)
        {
            var gear = _gears[index];
            if (gear == null)
            {
                switch (index)
                {
                    case 0:
                        gear = new GearDisplay(this);
                        break;
                    case 1:
                        gear = new GearXY(this);
                        break;
                    case 2:
                        gear = new GearSize(this);
                        break;
                    case 3:
                        gear = new GearLook(this);
                        break;
                    case 4:
                        gear = new GearColor(this);
                        break;
                    case 5:
                        gear = new GearAnimation(this);
                        break;
                    case 6:
                        gear = new GearText(this);
                        break;
                    case 7:
                        gear = new GearIcon(this);
                        break;
                    case 8:
                        gear = new GearDisplay2(this);
                        break;
                    case 9:
                        gear = new GearFontSize(this);
                        break;
                    default:
                        throw new Exception("FairyGUI: invalid gear index!");
                }

                _gears[index] = gear;
            }

            return gear;
        }

        protected void UpdateGear(int index)
        {
            if (underConstruct || _gearLocked)
                return;

            var gear = _gears[index];
            if (gear != null && gear.controller != null)
                gear.UpdateState();
        }

        internal bool CheckGearController(int index, Controller c)
        {
            return _gears[index] != null && _gears[index].controller == c;
        }

        internal void UpdateGearFromRelations(int index, float dx, float dy)
        {
            if (_gears[index] != null)
                _gears[index].UpdateFromRelations(dx, dy);
        }

        internal uint AddDisplayLock()
        {
            var gearDisplay = (GearDisplay)_gears[0];
            if (gearDisplay != null && gearDisplay.controller != null)
            {
                var ret = gearDisplay.AddLock();
                CheckGearDisplay();

                return ret;
            }

            return 0;
        }

        internal void ReleaseDisplayLock(uint token)
        {
            var gearDisplay = (GearDisplay)_gears[0];
            if (gearDisplay != null && gearDisplay.controller != null)
            {
                gearDisplay.ReleaseLock(token);
                CheckGearDisplay();
            }
        }

        private void CheckGearDisplay()
        {
            if (_handlingController)
                return;

            var connected = _gears[0] == null || ((GearDisplay)_gears[0]).connected;
            if (_gears[8] != null)
                connected = ((GearDisplay2)_gears[8]).Evaluate(connected);

            if (connected != _internalVisible)
            {
                _internalVisible = connected;
                if (parent != null)
                    parent.ChildStateChanged(this);
                if (_group != null && _group.excludeInvisibles)
                    _group.SetBoundsChangedFlag();
            }
        }

        /// <summary>
        ///     Mark the fairy batching state is invalid.
        /// </summary>
        public void InvalidateBatchingState()
        {
            if (displayObject != null)
                displayObject.InvalidateBatchingState();
            else if (this is GGroup && parent != null)
                parent.container.InvalidateBatchingState(true);
        }

        public virtual void HandleControllerChanged(Controller c)
        {
            _handlingController = true;
            for (var i = 0; i < 10; i++)
            {
                var gear = _gears[i];
                if (gear != null && gear.controller == c)
                    gear.Apply();
            }

            _handlingController = false;

            CheckGearDisplay();
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relationType"></param>
        public void AddRelation(GObject target, RelationType relationType)
        {
            AddRelation(target, relationType, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relationType"></param>
        /// <param name="usePercent"></param>
        public void AddRelation(GObject target, RelationType relationType, bool usePercent)
        {
            relations.Add(target, relationType, usePercent);
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relationType"></param>
        public void RemoveRelation(GObject target, RelationType relationType)
        {
            relations.Remove(target, relationType);
        }

        /// <summary>
        /// </summary>
        public void RemoveFromParent()
        {
            if (parent != null)
                parent.RemoveChild(this);
        }

        /// <summary>
        /// </summary>
        public void StartDrag()
        {
            StartDrag(-1);
        }

        /// <summary>
        /// </summary>
        /// <param name="touchId"></param>
        public void StartDrag(int touchId)
        {
            if (displayObject.stage == null)
                return;

            DragBegin(touchId);
        }

        /// <summary>
        /// </summary>
        public void StopDrag()
        {
            DragEnd();
        }

        /// <summary>
        ///     Transforms a point from the local coordinate system to global (Stage) coordinates.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Vector2 LocalToGlobal(Vector2 pt)
        {
            if (_pivotAsAnchor)
            {
                pt.x += _width * _pivotX;
                pt.y += _height * _pivotY;
            }

            return displayObject.LocalToGlobal(pt);
        }

        /// <summary>
        ///     Transforms a point from global (Stage) coordinates to the local coordinate system.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Vector2 GlobalToLocal(Vector2 pt)
        {
            pt = displayObject.GlobalToLocal(pt);
            if (_pivotAsAnchor)
            {
                pt.x -= _width * _pivotX;
                pt.y -= _height * _pivotY;
            }

            return pt;
        }

        /// <summary>
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Rect LocalToGlobal(Rect rect)
        {
            var ret = new Rect();
            var v = LocalToGlobal(new Vector2(rect.xMin, rect.yMin));
            ret.xMin = v.x;
            ret.yMin = v.y;
            v = LocalToGlobal(new Vector2(rect.xMax, rect.yMax));
            ret.xMax = v.x;
            ret.yMax = v.y;
            return ret;
        }

        /// <summary>
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Rect GlobalToLocal(Rect rect)
        {
            var ret = new Rect();
            var v = GlobalToLocal(new Vector2(rect.xMin, rect.yMin));
            ret.xMin = v.x;
            ret.yMin = v.y;
            v = GlobalToLocal(new Vector2(rect.xMax, rect.yMax));
            ret.xMax = v.x;
            ret.yMax = v.y;
            return ret;
        }

        /// <summary>
        ///     Transforms a point from the local coordinate system to GRoot coordinates.
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public Vector2 LocalToRoot(Vector2 pt, GRoot r)
        {
            pt = LocalToGlobal(pt);
            if (r == null || r == GRoot.inst)
            {
                //fast
                pt.x /= UIContentScaler.scaleFactor;
                pt.y /= UIContentScaler.scaleFactor;
                return pt;
            }

            return r.GlobalToLocal(pt);
        }

        /// <summary>
        ///     Transforms a point from the GRoot coordinate  to local coordinates system.
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public Vector2 RootToLocal(Vector2 pt, GRoot r)
        {
            if (r == null || r == GRoot.inst)
            {
                //fast
                pt.x *= UIContentScaler.scaleFactor;
                pt.y *= UIContentScaler.scaleFactor;
            }
            else
            {
                pt = r.LocalToGlobal(pt);
            }

            return GlobalToLocal(pt);
        }

        /// <summary>
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Vector2 WorldToLocal(Vector3 pt)
        {
            return WorldToLocal(pt, HitTestContext.cachedMainCamera);
        }

        /// <summary>
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public Vector2 WorldToLocal(Vector3 pt, Camera camera)
        {
            var v = camera.WorldToScreenPoint(pt);
            v.y = Screen.height - v.y;
            v.z = 0;
            return GlobalToLocal(v);
        }

        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        /// <param name="targetSpace"></param>
        /// <returns></returns>
        public Vector2 TransformPoint(Vector2 pt, GObject targetSpace)
        {
            if (_pivotAsAnchor)
            {
                pt.x += _width * _pivotX;
                pt.y += _height * _pivotY;
            }

            return displayObject.TransformPoint(pt, targetSpace != null ? targetSpace.displayObject : Stage.inst);
        }

        /// <summary>
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="targetSpace"></param>
        /// <returns></returns>
        public Rect TransformRect(Rect rect, GObject targetSpace)
        {
            if (_pivotAsAnchor)
            {
                rect.x += _width * _pivotX;
                rect.y += _height * _pivotY;
            }

            return displayObject.TransformRect(rect, targetSpace != null ? targetSpace.displayObject : Stage.inst);
        }

        /// <summary>
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            RemoveFromParent();
            RemoveEventListeners();
            relations.Dispose();
            relations = null;
            for (var i = 0; i < 10; i++)
            {
                var gear = _gears[i];
                if (gear != null)
                    gear.Dispose();
            }

            if (displayObject != null)
            {
                displayObject.gOwner = null;
                displayObject.Dispose();
            }

            data = null;
        }

        protected virtual void CreateDisplayObject()
        {
        }

        internal void InternalSetParent(GComponent value)
        {
            parent = value;
        }

        protected virtual void HandlePositionChanged()
        {
            if (displayObject != null)
            {
                var xv = _x;
                var yv = _y;
                if (!_pivotAsAnchor)
                {
                    xv += _width * _pivotX;
                    yv += _height * _pivotY;
                }

                displayObject.location = new Vector3(xv, yv, _z);
            }
        }

        protected virtual void HandleSizeChanged()
        {
            if (displayObject != null)
                displayObject.SetSize(_width, _height);
        }

        protected virtual void HandleScaleChanged()
        {
            if (displayObject != null)
                displayObject.SetScale(_scaleX, _scaleY);
        }

        protected virtual void HandleGrayedChanged()
        {
            if (displayObject != null)
                displayObject.grayed = _grayed;
        }

        protected virtual void HandleAlphaChanged()
        {
            if (displayObject != null)
                displayObject.alpha = _alpha;
        }

        protected internal virtual void HandleVisibleChanged()
        {
            if (displayObject != null)
                displayObject.visible = internalVisible2;
        }

        public virtual void ConstructFromResource()
        {
        }

        public virtual void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
        {
            buffer.Seek(beginPos, 0);
            buffer.Skip(5);

            id = buffer.ReadS();
            name = buffer.ReadS();
            float f1 = buffer.ReadInt();
            float f2 = buffer.ReadInt();
            SetXY(f1, f2);

            if (buffer.ReadBool())
            {
                initWidth = buffer.ReadInt();
                initHeight = buffer.ReadInt();
                SetSize(initWidth, initHeight, true);
            }

            if (buffer.ReadBool())
            {
                minWidth = buffer.ReadInt();
                maxWidth = buffer.ReadInt();
                minHeight = buffer.ReadInt();
                maxHeight = buffer.ReadInt();
            }

            if (buffer.ReadBool())
            {
                f1 = buffer.ReadFloat();
                f2 = buffer.ReadFloat();
                SetScale(f1, f2);
            }

            if (buffer.ReadBool())
            {
                f1 = buffer.ReadFloat();
                f2 = buffer.ReadFloat();
                skew = new Vector2(f1, f2);
            }

            if (buffer.ReadBool())
            {
                f1 = buffer.ReadFloat();
                f2 = buffer.ReadFloat();
                SetPivot(f1, f2, buffer.ReadBool());
            }

            f1 = buffer.ReadFloat();
            if (f1 != 1)
                alpha = f1;

            f1 = buffer.ReadFloat();
            if (f1 != 0)
                rotation = f1;

            if (!buffer.ReadBool())
                visible = false;
            if (!buffer.ReadBool())
                touchable = false;
            if (buffer.ReadBool())
                grayed = true;
            blendMode = (BlendMode)buffer.ReadByte();

            int filter = buffer.ReadByte();
            if (filter == 1)
            {
                var cf = new ColorFilter();
                this.filter = cf;

                cf.AdjustBrightness(buffer.ReadFloat());
                cf.AdjustContrast(buffer.ReadFloat());
                cf.AdjustSaturation(buffer.ReadFloat());
                cf.AdjustHue(buffer.ReadFloat());
            }

            var str = buffer.ReadS();
            if (str != null)
                data = str;
        }

        public virtual void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            buffer.Seek(beginPos, 1);

            var str = buffer.ReadS();
            if (str != null)
                tooltips = str;

            int groupId = buffer.ReadShort();
            if (groupId >= 0)
                group = parent.GetChildAt(groupId) as GGroup;

            buffer.Seek(beginPos, 2);

            int cnt = buffer.ReadShort();
            for (var i = 0; i < cnt; i++)
            {
                int nextPos = buffer.ReadUshort();
                nextPos += buffer.position;

                var gear = GetGear(buffer.ReadByte());
                gear.Setup(buffer);

                buffer.position = nextPos;
            }
        }

        #region Drag support

        private Vector2 _dragTouchStartPos;
        private bool _dragTesting;

        private static Vector2 sGlobalDragStart;
        private static Rect sGlobalRect;
        private static bool sUpdateInDragging;

        private void InitDrag()
        {
            if (_draggable)
            {
                onTouchBegin.Add(__touchBegin);
                onTouchMove.Add(__touchMove);
                onTouchEnd.Add(__touchEnd);
            }
            else
            {
                onTouchBegin.Remove(__touchBegin);
                onTouchMove.Remove(__touchMove);
                onTouchEnd.Remove(__touchEnd);
            }
        }

        private void DragBegin(int touchId)
        {
            if (DispatchEvent("onDragStart", touchId))
                return;

            if (draggingObject != null)
            {
                var tmp = draggingObject;
                draggingObject.StopDrag();
                draggingObject = null;
                tmp.DispatchEvent("onDragEnd", null);
            }

            onTouchMove.Add(__touchMove);
            onTouchEnd.Add(__touchEnd);

            sGlobalDragStart = Stage.inst.GetTouchPosition(touchId);
            sGlobalRect = LocalToGlobal(new Rect(0, 0, width, height));
            _dragTesting = false;

            draggingObject = this;
            Stage.inst.AddTouchMonitor(touchId, this);
        }

        private void DragEnd()
        {
            if (draggingObject == this)
            {
                _dragTesting = false;
                draggingObject = null;
            }
        }

        private void __touchBegin(EventContext context)
        {
            if (Stage.inst.focus is InputTextField && ((InputTextField)Stage.inst.focus).editable)
            {
                _dragTesting = false;
                return;
            }

            var evt = context.inputEvent;
            _dragTouchStartPos = evt.position;
            _dragTesting = true;
            context.CaptureTouch();
        }

        private void __touchMove(EventContext context)
        {
            var evt = context.inputEvent;

            if (_dragTesting && draggingObject != this)
            {
                int sensitivity;
                if (Stage.touchScreen)
                    sensitivity = UIConfig.touchDragSensitivity;
                else
                    sensitivity = UIConfig.clickDragSensitivity;
                if (Mathf.Abs(_dragTouchStartPos.x - evt.x) < sensitivity
                    && Mathf.Abs(_dragTouchStartPos.y - evt.y) < sensitivity)
                    return;

                _dragTesting = false;
                DragBegin(evt.touchId);
            }

            if (draggingObject == this)
            {
                var xx = evt.x - sGlobalDragStart.x + sGlobalRect.x;
                var yy = evt.y - sGlobalDragStart.y + sGlobalRect.y;

                if (dragBounds != null)
                {
                    var rect = GRoot.inst.LocalToGlobal((Rect)dragBounds);
                    if (xx < rect.x)
                    {
                        xx = rect.x;
                    }
                    else if (xx + sGlobalRect.width > rect.xMax)
                    {
                        xx = rect.xMax - sGlobalRect.width;
                        if (xx < rect.x)
                            xx = rect.x;
                    }

                    if (yy < rect.y)
                    {
                        yy = rect.y;
                    }
                    else if (yy + sGlobalRect.height > rect.yMax)
                    {
                        yy = rect.yMax - sGlobalRect.height;
                        if (yy < rect.y)
                            yy = rect.y;
                    }
                }

                var pt = parent.GlobalToLocal(new Vector2(xx, yy));
                if (float.IsNaN(pt.x))
                    return;

                sUpdateInDragging = true;
                SetXY(Mathf.RoundToInt(pt.x), Mathf.RoundToInt(pt.y));
                sUpdateInDragging = false;

                DispatchEvent("onDragMove", null);
            }
        }

        private void __touchEnd(EventContext context)
        {
            if (draggingObject == this)
            {
                draggingObject = null;
                DispatchEvent("onDragEnd", null);
            }
        }

        #endregion

        #region Tween Helpers

        public GTweener TweenMove(Vector2 endValue, float duration)
        {
            return GTween.To(xy, endValue, duration).SetTarget(this, TweenPropType.XY);
        }

        public GTweener TweenMoveX(float endValue, float duration)
        {
            return GTween.To(_x, endValue, duration).SetTarget(this, TweenPropType.X);
        }

        public GTweener TweenMoveY(float endValue, float duration)
        {
            return GTween.To(_y, endValue, duration).SetTarget(this, TweenPropType.Y);
        }

        public GTweener TweenScale(Vector2 endValue, float duration)
        {
            return GTween.To(scale, endValue, duration).SetTarget(this, TweenPropType.Scale);
        }

        public GTweener TweenScaleX(float endValue, float duration)
        {
            return GTween.To(_scaleX, endValue, duration).SetTarget(this, TweenPropType.ScaleX);
        }

        public GTweener TweenScaleY(float endValue, float duration)
        {
            return GTween.To(_scaleY, endValue, duration).SetTarget(this, TweenPropType.ScaleY);
        }

        public GTweener TweenResize(Vector2 endValue, float duration)
        {
            return GTween.To(size, endValue, duration).SetTarget(this, TweenPropType.Size);
        }

        public GTweener TweenFade(float endValue, float duration)
        {
            return GTween.To(_alpha, endValue, duration).SetTarget(this, TweenPropType.Alpha);
        }

        public GTweener TweenRotate(float endValue, float duration)
        {
            return GTween.To(_rotation, endValue, duration).SetTarget(this, TweenPropType.Rotation);
        }

        #endregion
    }
}