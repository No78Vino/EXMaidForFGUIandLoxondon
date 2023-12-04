using System;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class ScrollPane : EventDispatcher
    {
        private static int _gestureFlag;

        public static float TWEEN_TIME_GO = 0.3f; //调用SetPos(ani)时使用的缓动时间
        public static float TWEEN_TIME_DEFAULT = 0.3f; //惯性滚动的最小缓动时间
        public static float PULL_RATIO = 0.5f; //下拉过顶或者上拉过底时允许超过的距离占显示区域的比例
        private int _aniFlag;
        private Vector2 _beginTouchPos;
        private readonly Container _container;
        private Vector2 _containerPos;
        private Vector2 _contentSize;
        internal bool _displayInDemand;

        private bool _displayOnLeft;
        private bool _dontClipMargin;
        private bool _floating;
        private int _footerLockedSize;
        private int _headerLockedSize;
        private readonly GTweenCallback1 _hideScrollBarDelegate;
        private bool _hover;
        private bool _hScrollNone;
        private bool _isHoldAreaDone;
        private float _lastMoveTime;
        private Vector2 _lastTouchGlobalPos;
        private Vector2 _lastTouchPos;
        internal int _loop;
        private readonly Container _maskContainer;
        private bool _maskDisabled;
        private bool _needRefresh;
        private EventListener _onPullDownRelease;
        private EventListener _onPullUpRelease;

        private EventListener _onScroll;
        private EventListener _onScrollEnd;
        private Vector2 _overlapSize;

        private Vector2 _pageSize;
        private int _refreshBarAxis;

        private readonly Action _refreshDelegate;
        private bool _scrollBarDisplayAuto;
        private Margin _scrollBarMargin;
        private float _scrollStep;

        private ScrollType _scrollType;
        private Vector2 _tweenChange;
        private Vector2 _tweenDuration;

        private int _tweening;
        private Vector2 _tweenStart;
        private Vector2 _tweenTime;
        private readonly TimerCallback _tweenUpdateDelegate;
        private Vector2 _velocity;
        private float _velocityScale;

        private Vector2 _viewSize;
        private bool _vScrollNone;

        private float _xPos;
        private float _yPos;

        public ScrollPane(GComponent owner)
        {
            _onScroll = new EventListener(this, "onScroll");
            _onScrollEnd = new EventListener(this, "onScrollEnd");

            _scrollStep = UIConfig.defaultScrollStep;
            softnessOnTopOrLeftSide = UIConfig.allowSoftnessOnTopOrLeftSide;
            decelerationRate = UIConfig.defaultScrollDecelerationRate;
            touchEffect = UIConfig.defaultScrollTouchEffect;
            bouncebackEffect = UIConfig.defaultScrollBounceEffect;
            mouseWheelEnabled = true;
            _pageSize = Vector2.one;

            _refreshDelegate = Refresh;
            _tweenUpdateDelegate = TweenUpdate;
            _hideScrollBarDelegate = __barTweenComplete;

            this.owner = owner;

            _maskContainer = new Container();
            this.owner.rootContainer.AddChild(_maskContainer);

            _container = this.owner.container;
            _container.SetXY(0, 0);
            _maskContainer.AddChild(_container);

            this.owner.rootContainer.onMouseWheel.Add(__mouseWheel);
            this.owner.rootContainer.onTouchBegin.Add(__touchBegin);
            this.owner.rootContainer.onTouchMove.Add(__touchMove);
            this.owner.rootContainer.onTouchEnd.Add(__touchEnd);
        }

        /// <summary>
        ///     当前被拖拽的滚动面板。同一时间只能有一个在进行此操作。
        /// </summary>
        public static ScrollPane draggingPane { get; private set; }

        /// <summary>
        ///     Dispatched when scrolling.
        ///     在滚动时派发该事件。
        /// </summary>
        public EventListener onScroll => _onScroll ?? (_onScroll = new EventListener(this, "onScroll"));

        /// <summary>
        ///     在滚动结束时派发该事件。
        /// </summary>
        public EventListener onScrollEnd => _onScrollEnd ?? (_onScrollEnd = new EventListener(this, "onScrollEnd"));

        /// <summary>
        ///     向下拉过上边缘后释放则派发该事件。
        /// </summary>
        public EventListener onPullDownRelease =>
            _onPullDownRelease ?? (_onPullDownRelease = new EventListener(this, "onPullDownRelease"));

        /// <summary>
        ///     向上拉过下边缘后释放则派发该事件。
        /// </summary>
        public EventListener onPullUpRelease =>
            _onPullUpRelease ?? (_onPullUpRelease = new EventListener(this, "onPullUpRelease"));

        /// <summary>
        /// </summary>
        public GComponent owner { get; }

        /// <summary>
        /// </summary>
        public GScrollBar hzScrollBar { get; private set; }

        /// <summary>
        /// </summary>
        public GScrollBar vtScrollBar { get; private set; }

        /// <summary>
        /// </summary>
        public GComponent header { get; private set; }

        /// <summary>
        /// </summary>
        public GComponent footer { get; private set; }

        /// <summary>
        ///     滚动到达边缘时是否允许回弹效果。
        /// </summary>
        public bool bouncebackEffect { get; set; }

        /// <summary>
        ///     是否允许拖拽内容区域进行滚动。
        /// </summary>
        public bool touchEffect { get; set; }

        /// <summary>
        ///     是否允许惯性滚动。
        /// </summary>
        public bool inertiaDisabled { get; set; }

        /// <summary>
        ///     是否允许在左/上边缘显示虚化效果。
        /// </summary>
        public bool softnessOnTopOrLeftSide { get; set; }

        /// <summary>
        ///     当调用ScrollPane.scrollUp/Down/Left/Right时，或者点击滚动条的上下箭头时，滑动的距离。
        /// </summary>
        public float scrollStep
        {
            get => _scrollStep;
            set
            {
                _scrollStep = value;
                if (_scrollStep == 0)
                    _scrollStep = UIConfig.defaultScrollStep;
            }
        }

        /// <summary>
        ///     滚动位置是否保持贴近在某个元件的边缘。
        /// </summary>
        public bool snapToItem { get; set; }

        /// <summary>
        ///     是否页面滚动模式。
        /// </summary>
        public bool pageMode { get; set; }

        /// <summary>
        /// </summary>
        public Controller pageController { get; set; }

        /// <summary>
        ///     是否允许使用鼠标滚轮进行滚动。
        /// </summary>
        public bool mouseWheelEnabled { get; set; }

        /// <summary>
        ///     当处于惯性滚动时减速的速率。默认值是UIConfig.defaultScrollDecelerationRate。
        ///     越接近1，减速越慢，意味着滑动的时间和距离更长。
        /// </summary>
        public float decelerationRate { get; set; }

        /// <summary>
        /// </summary>
        public bool isDragged { get; private set; }

        /// <summary>
        ///     当前X轴滚动位置百分比，0~1（包含）。
        /// </summary>
        public float percX
        {
            get => _overlapSize.x == 0 ? 0 : _xPos / _overlapSize.x;
            set => SetPercX(value, false);
        }

        /// <summary>
        ///     当前Y轴滚动位置百分比，0~1（包含）。
        /// </summary>
        public float percY
        {
            get => _overlapSize.y == 0 ? 0 : _yPos / _overlapSize.y;
            set => SetPercY(value, false);
        }

        /// <summary>
        ///     当前X轴滚动位置，值范围是viewWidth与contentWidth之差。
        /// </summary>
        public float posX
        {
            get => _xPos;
            set => SetPosX(value, false);
        }

        /// <summary>
        ///     当前Y轴滚动位置，值范围是viewHeight与contentHeight之差。
        /// </summary>
        public float posY
        {
            get => _yPos;
            set => SetPosY(value, false);
        }

        /// <summary>
        ///     返回当前滚动位置是否在最下边。
        /// </summary>
        public bool isBottomMost => _yPos == _overlapSize.y || _overlapSize.y == 0;

        /// <summary>
        ///     返回当前滚动位置是否在最右边。
        /// </summary>
        public bool isRightMost => _xPos == _overlapSize.x || _overlapSize.x == 0;

        /// <summary>
        ///     如果处于分页模式，返回当前在X轴的页码。
        /// </summary>
        public int currentPageX
        {
            get
            {
                if (!pageMode)
                    return 0;

                var page = Mathf.FloorToInt(_xPos / _pageSize.x);
                if (_xPos - page * _pageSize.x > _pageSize.x * 0.5f)
                    page++;

                return page;
            }
            set
            {
                if (!pageMode)
                    return;

                owner.EnsureBoundsCorrect();

                if (_overlapSize.x > 0)
                    SetPosX(value * _pageSize.x, false);
            }
        }

        /// <summary>
        ///     如果处于分页模式，返回当前在Y轴的页码。
        /// </summary>
        public int currentPageY
        {
            get
            {
                if (!pageMode)
                    return 0;

                var page = Mathf.FloorToInt(_yPos / _pageSize.y);
                if (_yPos - page * _pageSize.y > _pageSize.y * 0.5f)
                    page++;

                return page;
            }
            set
            {
                if (!pageMode)
                    return;

                owner.EnsureBoundsCorrect();

                if (_overlapSize.y > 0)
                    SetPosY(value * _pageSize.y, false);
            }
        }

        /// <summary>
        ///     这个值与PosX不同在于，他反映的是实时位置，而PosX在有缓动过程的情况下只是终值。
        /// </summary>
        public float scrollingPosX => Mathf.Clamp(-_container.x, 0, _overlapSize.x);

        /// <summary>
        ///     这个值与PosY不同在于，他反映的是实时位置，而PosY在有缓动过程的情况下只是终值。
        /// </summary>
        public float scrollingPosY => Mathf.Clamp(-_container.y, 0, _overlapSize.y);

        /// <summary>
        ///     显示内容宽度。
        /// </summary>
        public float contentWidth => _contentSize.x;

        /// <summary>
        ///     显示内容高度。
        /// </summary>
        public float contentHeight => _contentSize.y;

        /// <summary>
        ///     显示区域宽度。
        /// </summary>
        public float viewWidth
        {
            get => _viewSize.x;
            set
            {
                value = value + owner.margin.left + owner.margin.right;
                if (vtScrollBar != null && !_floating)
                    value += vtScrollBar.width;
                owner.width = value;
            }
        }

        /// <summary>
        ///     显示区域高度。
        /// </summary>
        public float viewHeight
        {
            get => _viewSize.y;
            set
            {
                value = value + owner.margin.top + owner.margin.bottom;
                if (hzScrollBar != null && !_floating)
                    value += hzScrollBar.height;
                owner.height = value;
            }
        }

        public void Setup(ByteBuffer buffer)
        {
            _scrollType = (ScrollType)buffer.ReadByte();
            var scrollBarDisplay = (ScrollBarDisplayType)buffer.ReadByte();
            var flags = buffer.ReadInt();

            if (buffer.ReadBool())
            {
                _scrollBarMargin.top = buffer.ReadInt();
                _scrollBarMargin.bottom = buffer.ReadInt();
                _scrollBarMargin.left = buffer.ReadInt();
                _scrollBarMargin.right = buffer.ReadInt();
            }

            var vtScrollBarRes = buffer.ReadS();
            var hzScrollBarRes = buffer.ReadS();
            var headerRes = buffer.ReadS();
            var footerRes = buffer.ReadS();

            _displayOnLeft = (flags & 1) != 0;
            snapToItem = (flags & 2) != 0;
            _displayInDemand = (flags & 4) != 0;
            pageMode = (flags & 8) != 0;
            if ((flags & 16) != 0)
                touchEffect = true;
            else if ((flags & 32) != 0)
                touchEffect = false;
            if ((flags & 64) != 0)
                bouncebackEffect = true;
            else if ((flags & 128) != 0)
                bouncebackEffect = false;
            inertiaDisabled = (flags & 256) != 0;
            _maskDisabled = (flags & 512) != 0;
            _floating = (flags & 1024) != 0;
            _dontClipMargin = (flags & 2048) != 0;

            if (scrollBarDisplay == ScrollBarDisplayType.Default)
            {
                if (Application.isMobilePlatform)
                    scrollBarDisplay = ScrollBarDisplayType.Auto;
                else
                    scrollBarDisplay = UIConfig.defaultScrollBarDisplay;
            }

            if (scrollBarDisplay != ScrollBarDisplayType.Hidden)
            {
                if (_scrollType == ScrollType.Both || _scrollType == ScrollType.Vertical)
                {
                    var res = vtScrollBarRes != null ? vtScrollBarRes : UIConfig.verticalScrollBar;
                    if (!string.IsNullOrEmpty(res))
                    {
                        vtScrollBar = UIPackage.CreateObjectFromURL(res) as GScrollBar;
                        if (vtScrollBar == null)
                        {
                            Debug.LogWarning("FairyGUI: cannot create scrollbar from " + res);
                        }
                        else
                        {
                            vtScrollBar.SetScrollPane(this, true);
                            owner.rootContainer.AddChild(vtScrollBar.displayObject);
                        }
                    }
                }

                if (_scrollType == ScrollType.Both || _scrollType == ScrollType.Horizontal)
                {
                    var res = hzScrollBarRes != null ? hzScrollBarRes : UIConfig.horizontalScrollBar;
                    if (!string.IsNullOrEmpty(res))
                    {
                        hzScrollBar = UIPackage.CreateObjectFromURL(res) as GScrollBar;
                        if (hzScrollBar == null)
                        {
                            Debug.LogWarning("FairyGUI: cannot create scrollbar from " + res);
                        }
                        else
                        {
                            hzScrollBar.SetScrollPane(this, false);
                            owner.rootContainer.AddChild(hzScrollBar.displayObject);
                        }
                    }
                }

                _scrollBarDisplayAuto = scrollBarDisplay == ScrollBarDisplayType.Auto;
                if (_scrollBarDisplayAuto)
                {
                    if (vtScrollBar != null)
                        vtScrollBar.displayObject.visible = false;
                    if (hzScrollBar != null)
                        hzScrollBar.displayObject.visible = false;

                    owner.rootContainer.onRollOver.Add(__rollOver);
                    owner.rootContainer.onRollOut.Add(__rollOut);
                }
            }
            else
            {
                mouseWheelEnabled = false;
            }

            if (Application.isPlaying)
            {
                if (headerRes != null)
                {
                    header = (GComponent)UIPackage.CreateObjectFromURL(headerRes);
                    if (header == null)
                        Debug.LogWarning("FairyGUI: cannot create scrollPane header from " + headerRes);
                }

                if (footerRes != null)
                {
                    footer = (GComponent)UIPackage.CreateObjectFromURL(footerRes);
                    if (footer == null)
                        Debug.LogWarning("FairyGUI: cannot create scrollPane footer from " + footerRes);
                }

                if (header != null || footer != null)
                    _refreshBarAxis = _scrollType == ScrollType.Both || _scrollType == ScrollType.Vertical ? 1 : 0;
            }

            SetSize(owner.width, owner.height);
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            RemoveEventListeners();

            if (_tweening != 0)
                Timers.inst.Remove(_tweenUpdateDelegate);

            if (draggingPane == this)
                draggingPane = null;

            pageController = null;

            if (hzScrollBar != null)
                hzScrollBar.Dispose();
            if (vtScrollBar != null)
                vtScrollBar.Dispose();
            if (header != null)
                header.Dispose();
            if (footer != null)
                footer.Dispose();
        }

        /// <summary>
        ///     设置当前X轴滚动位置百分比，0~1（包含）。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ani">是否使用缓动到达目标。</param>
        public void SetPercX(float value, bool ani)
        {
            owner.EnsureBoundsCorrect();
            SetPosX(_overlapSize.x * Mathf.Clamp01(value), ani);
        }

        /// <summary>
        ///     设置当前Y轴滚动位置百分比，0~1（包含）。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ani">是否使用缓动到达目标。</param>
        public void SetPercY(float value, bool ani)
        {
            owner.EnsureBoundsCorrect();
            SetPosY(_overlapSize.y * Mathf.Clamp01(value), ani);
        }

        /// <summary>
        ///     设置当前X轴滚动位置。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ani">是否使用缓动到达目标。</param>
        public void SetPosX(float value, bool ani)
        {
            owner.EnsureBoundsCorrect();

            if (_loop == 1)
                LoopCheckingNewPos(ref value, 0);

            value = Mathf.Clamp(value, 0, _overlapSize.x);
            if (value != _xPos)
            {
                _xPos = value;
                PosChanged(ani);
            }
        }

        /// <summary>
        ///     设置当前Y轴滚动位置。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ani">是否使用缓动到达目标。</param>
        public void SetPosY(float value, bool ani)
        {
            owner.EnsureBoundsCorrect();

            if (_loop == 2)
                LoopCheckingNewPos(ref value, 1);

            value = Mathf.Clamp(value, 0, _overlapSize.y);
            if (value != _yPos)
            {
                _yPos = value;
                PosChanged(ani);
            }
        }

        /// <summary>
        ///     如果处于分页模式，可设置X轴的页码。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ani">是否使用缓动到达目标。</param>
        public void SetCurrentPageX(int value, bool ani)
        {
            if (!pageMode)
                return;

            owner.EnsureBoundsCorrect();

            if (_overlapSize.x > 0)
                SetPosX(value * _pageSize.x, ani);
        }

        /// <summary>
        ///     如果处于分页模式，可设置Y轴的页码。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ani">是否使用缓动到达目标。</param>
        public void SetCurrentPageY(int value, bool ani)
        {
            if (!pageMode)
                return;

            owner.EnsureBoundsCorrect();

            if (_overlapSize.y > 0)
                SetPosY(value * _pageSize.y, ani);
        }

        /// <summary>
        /// </summary>
        public void ScrollTop()
        {
            ScrollTop(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="ani"></param>
        public void ScrollTop(bool ani)
        {
            SetPercY(0, ani);
        }

        /// <summary>
        /// </summary>
        public void ScrollBottom()
        {
            ScrollBottom(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="ani"></param>
        public void ScrollBottom(bool ani)
        {
            SetPercY(1, ani);
        }

        /// <summary>
        /// </summary>
        public void ScrollUp()
        {
            ScrollUp(1, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="ani"></param>
        public void ScrollUp(float ratio, bool ani)
        {
            if (pageMode)
                SetPosY(_yPos - _pageSize.y * ratio, ani);
            else
                SetPosY(_yPos - _scrollStep * ratio, ani);
        }

        /// <summary>
        /// </summary>
        public void ScrollDown()
        {
            ScrollDown(1, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="ani"></param>
        public void ScrollDown(float ratio, bool ani)
        {
            if (pageMode)
                SetPosY(_yPos + _pageSize.y * ratio, ani);
            else
                SetPosY(_yPos + _scrollStep * ratio, ani);
        }

        /// <summary>
        /// </summary>
        public void ScrollLeft()
        {
            ScrollLeft(1, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="ani"></param>
        public void ScrollLeft(float ratio, bool ani)
        {
            if (pageMode)
                SetPosX(_xPos - _pageSize.x * ratio, ani);
            else
                SetPosX(_xPos - _scrollStep * ratio, ani);
        }

        /// <summary>
        /// </summary>
        public void ScrollRight()
        {
            ScrollRight(1, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="ani"></param>
        public void ScrollRight(float ratio, bool ani)
        {
            if (pageMode)
                SetPosX(_xPos + _pageSize.x * ratio, ani);
            else
                SetPosX(_xPos + _scrollStep * ratio, ani);
        }

        /// <summary>
        /// </summary>
        /// <param name="obj">obj can be any object on stage, not limited to the direct child of this container.</param>
        public void ScrollToView(GObject obj)
        {
            ScrollToView(obj, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="obj">obj can be any object on stage, not limited to the direct child of this container.</param>
        /// <param name="ani">If moving to target position with animation</param>
        public void ScrollToView(GObject obj, bool ani)
        {
            ScrollToView(obj, ani, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="obj">obj can be any object on stage, not limited to the direct child of this container.</param>
        /// <param name="ani">If moving to target position with animation</param>
        /// <param name="setFirst">
        ///     If true, scroll to make the target on the top/left; If false, scroll to make the target any
        ///     position in view.
        /// </param>
        public void ScrollToView(GObject obj, bool ani, bool setFirst)
        {
            owner.EnsureBoundsCorrect();
            if (_needRefresh)
                Refresh();

            var rect = new Rect(obj.x, obj.y, obj.width, obj.height);
            if (obj.parent != owner)
                rect = obj.parent.TransformRect(rect, owner);
            ScrollToView(rect, ani, setFirst);
        }

        /// <summary>
        /// </summary>
        /// <param name="rect">Rect in local coordinates</param>
        /// <param name="ani">If moving to target position with animation</param>
        /// <param name="setFirst">
        ///     If true, scroll to make the target on the top/left; If false, scroll to make the target any
        ///     position in view.
        /// </param>
        public void ScrollToView(Rect rect, bool ani, bool setFirst)
        {
            owner.EnsureBoundsCorrect();
            if (_needRefresh)
                Refresh();

            if (_overlapSize.y > 0)
            {
                var bottom = _yPos + _viewSize.y;
                if (setFirst || rect.y <= _yPos || rect.height >= _viewSize.y)
                {
                    if (pageMode)
                        SetPosY(Mathf.Floor(rect.y / _pageSize.y) * _pageSize.y, ani);
                    else
                        SetPosY(rect.y, ani);
                }
                else if (rect.y + rect.height > bottom)
                {
                    if (pageMode)
                        SetPosY(Mathf.Floor(rect.y / _pageSize.y) * _pageSize.y, ani);
                    else if (rect.height <= _viewSize.y / 2)
                        SetPosY(rect.y + rect.height * 2 - _viewSize.y, ani);
                    else
                        SetPosY(rect.y + rect.height - _viewSize.y, ani);
                }
            }

            if (_overlapSize.x > 0)
            {
                var right = _xPos + _viewSize.x;
                if (setFirst || rect.x <= _xPos || rect.width >= _viewSize.x)
                {
                    if (pageMode)
                        SetPosX(Mathf.Floor(rect.x / _pageSize.x) * _pageSize.x, ani);
                    SetPosX(rect.x, ani);
                }
                else if (rect.x + rect.width > right)
                {
                    if (pageMode)
                        SetPosX(Mathf.Floor(rect.x / _pageSize.x) * _pageSize.x, ani);
                    else if (rect.width <= _viewSize.x / 2)
                        SetPosX(rect.x + rect.width * 2 - _viewSize.x, ani);
                    else
                        SetPosX(rect.x + rect.width - _viewSize.x, ani);
                }
            }

            if (!ani && _needRefresh)
                Refresh();
        }

        /// <summary>
        /// </summary>
        /// <param name="obj">obj must be the direct child of this container</param>
        /// <returns></returns>
        public bool IsChildInView(GObject obj)
        {
            if (_overlapSize.y > 0)
            {
                var dist = obj.y + _container.y;
                if (dist <= -obj.height || dist >= _viewSize.y)
                    return false;
            }

            if (_overlapSize.x > 0)
            {
                var dist = obj.x + _container.x;
                if (dist <= -obj.width || dist >= _viewSize.x)
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     当滚动面板处于拖拽滚动状态或即将进入拖拽状态时，可以调用此方法停止或禁止本次拖拽。
        /// </summary>
        public void CancelDragging()
        {
            Stage.inst.RemoveTouchMonitor(owner.rootContainer);

            if (draggingPane == this)
                draggingPane = null;

            _gestureFlag = 0;
            isDragged = false;
        }

        /// <summary>
        ///     设置Header固定显示。如果size为0，则取消固定显示。
        /// </summary>
        /// <param name="size">Header显示的大小</param>
        public void LockHeader(int size)
        {
            if (_headerLockedSize == size)
                return;

            _headerLockedSize = size;
            if (!isDispatching("onPullDownRelease") && _container.xy[_refreshBarAxis] >= 0)
            {
                _tweenStart = _container.xy;
                _tweenChange = Vector2.zero;
                _tweenChange[_refreshBarAxis] = _headerLockedSize - _tweenStart[_refreshBarAxis];
                _tweenDuration = new Vector2(TWEEN_TIME_DEFAULT, TWEEN_TIME_DEFAULT);
                StartTween(2);
            }
        }

        /// <summary>
        ///     设置Footer固定显示。如果size为0，则取消固定显示。
        /// </summary>
        /// <param name="size"></param>
        public void LockFooter(int size)
        {
            if (_footerLockedSize == size)
                return;

            _footerLockedSize = size;
            if (!isDispatching("onPullUpRelease") && _container.xy[_refreshBarAxis] <= -_overlapSize[_refreshBarAxis])
            {
                _tweenStart = _container.xy;
                _tweenChange = Vector2.zero;
                var max = _overlapSize[_refreshBarAxis];
                if (max == 0)
                    max = Mathf.Max(_contentSize[_refreshBarAxis] + _footerLockedSize - _viewSize[_refreshBarAxis], 0);
                else
                    max += _footerLockedSize;
                _tweenChange[_refreshBarAxis] = -max - _tweenStart[_refreshBarAxis];
                _tweenDuration = new Vector2(TWEEN_TIME_DEFAULT, TWEEN_TIME_DEFAULT);
                StartTween(2);
            }
        }

        internal void OnOwnerSizeChanged()
        {
            SetSize(owner.width, owner.height);
            PosChanged(false);
        }

        internal void HandleControllerChanged(Controller c)
        {
            if (pageController == c)
            {
                if (_scrollType == ScrollType.Horizontal)
                    SetCurrentPageX(c.selectedIndex, true);
                else
                    SetCurrentPageY(c.selectedIndex, true);
            }
        }

        private void UpdatePageController()
        {
            if (pageController != null && !pageController.changing)
            {
                int index;
                if (_scrollType == ScrollType.Horizontal)
                    index = currentPageX;
                else
                    index = currentPageY;
                if (index < pageController.pageCount)
                {
                    var c = pageController;
                    pageController = null; //防止HandleControllerChanged的调用
                    c.selectedIndex = index;
                    pageController = c;
                }
            }
        }

        internal void AdjustMaskContainer()
        {
            float mx, my;
            if (_displayOnLeft && vtScrollBar != null && !_floating)
                mx = Mathf.FloorToInt(owner.margin.left + vtScrollBar.width);
            else
                mx = owner.margin.left;
            my = owner.margin.top;
            mx += owner._alignOffset.x;
            my += owner._alignOffset.y;

            _maskContainer.SetXY(mx, my);
        }

        private void SetSize(float aWidth, float aHeight)
        {
            AdjustMaskContainer();

            if (hzScrollBar != null)
            {
                hzScrollBar.y = aHeight - hzScrollBar.height;
                if (vtScrollBar != null)
                {
                    hzScrollBar.width = aWidth - vtScrollBar.width - _scrollBarMargin.left - _scrollBarMargin.right;
                    if (_displayOnLeft)
                        hzScrollBar.x = _scrollBarMargin.left + vtScrollBar.width;
                    else
                        hzScrollBar.x = _scrollBarMargin.left;
                }
                else
                {
                    hzScrollBar.width = aWidth - _scrollBarMargin.left - _scrollBarMargin.right;
                    hzScrollBar.x = _scrollBarMargin.left;
                }
            }

            if (vtScrollBar != null)
            {
                if (!_displayOnLeft)
                    vtScrollBar.x = aWidth - vtScrollBar.width;
                if (hzScrollBar != null)
                    vtScrollBar.height = aHeight - hzScrollBar.height - _scrollBarMargin.top - _scrollBarMargin.bottom;
                else
                    vtScrollBar.height = aHeight - _scrollBarMargin.top - _scrollBarMargin.bottom;
                vtScrollBar.y = _scrollBarMargin.top;
            }

            _viewSize.x = aWidth;
            _viewSize.y = aHeight;
            if (hzScrollBar != null && !_floating)
                _viewSize.y -= hzScrollBar.height;
            if (vtScrollBar != null && !_floating)
                _viewSize.x -= vtScrollBar.width;
            _viewSize.x -= owner.margin.left + owner.margin.right;
            _viewSize.y -= owner.margin.top + owner.margin.bottom;

            _viewSize.x = Mathf.Max(1, _viewSize.x);
            _viewSize.y = Mathf.Max(1, _viewSize.y);
            _pageSize.x = _viewSize.x;
            _pageSize.y = _viewSize.y;

            HandleSizeChanged();
        }

        internal void SetContentSize(float aWidth, float aHeight)
        {
            if (Mathf.Approximately(_contentSize.x, aWidth) && Mathf.Approximately(_contentSize.y, aHeight))
                return;

            _contentSize.x = aWidth;
            _contentSize.y = aHeight;
            HandleSizeChanged();
        }

        /// <summary>
        ///     内部使用。由虚拟列表调用。在滚动时修改显示内容的大小，需要进行修正，避免滚动跳跃。
        /// </summary>
        /// <param name="deltaWidth"></param>
        /// <param name="deltaHeight"></param>
        /// <param name="deltaPosX"></param>
        /// <param name="deltaPosY"></param>
        internal void ChangeContentSizeOnScrolling(float deltaWidth, float deltaHeight, float deltaPosX,
            float deltaPosY)
        {
            var isRightmost = _xPos == _overlapSize.x;
            var isBottom = _yPos == _overlapSize.y;

            _contentSize.x += deltaWidth;
            _contentSize.y += deltaHeight;
            HandleSizeChanged();

            if (_tweening == 1)
            {
                //如果原来滚动位置是贴边，加入处理继续贴边。
                if (deltaWidth != 0 && isRightmost && _tweenChange.x < 0)
                {
                    _xPos = _overlapSize.x;
                    _tweenChange.x = -_xPos - _tweenStart.x;
                }

                if (deltaHeight != 0 && isBottom && _tweenChange.y < 0)
                {
                    _yPos = _overlapSize.y;
                    _tweenChange.y = -_yPos - _tweenStart.y;
                }
            }
            else if (_tweening == 2)
            {
                //重新调整起始位置，确保能够顺滑滚下去
                if (deltaPosX != 0)
                {
                    _container.x -= deltaPosX;
                    _tweenStart.x -= deltaPosX;
                    _xPos = -_container.x;
                }

                if (deltaPosY != 0)
                {
                    _container.y -= deltaPosY;
                    _tweenStart.y -= deltaPosY;
                    _yPos = -_container.y;
                }
            }
            else if (isDragged)
            {
                if (deltaPosX != 0)
                {
                    _container.x -= deltaPosX;
                    _containerPos.x -= deltaPosX;
                    _xPos = -_container.x;
                }

                if (deltaPosY != 0)
                {
                    _container.y -= deltaPosY;
                    _containerPos.y -= deltaPosY;
                    _yPos = -_container.y;
                }
            }
            else
            {
                //如果原来滚动位置是贴边，加入处理继续贴边。
                if (deltaWidth != 0 && isRightmost)
                {
                    _xPos = _overlapSize.x;
                    _container.x = -_xPos;
                }

                if (deltaHeight != 0 && isBottom)
                {
                    _yPos = _overlapSize.y;
                    _container.y = -_yPos;
                }
            }

            if (pageMode)
                UpdatePageController();
        }

        private void HandleSizeChanged()
        {
            if (_displayInDemand)
            {
                _vScrollNone = _contentSize.y <= _viewSize.y;
                _hScrollNone = _contentSize.x <= _viewSize.x;
            }

            if (vtScrollBar != null)
            {
                if (_contentSize.y == 0)
                    vtScrollBar.SetDisplayPerc(0);
                else
                    vtScrollBar.SetDisplayPerc(Mathf.Min(1, _viewSize.y / _contentSize.y));
            }

            if (hzScrollBar != null)
            {
                if (_contentSize.x == 0)
                    hzScrollBar.SetDisplayPerc(0);
                else
                    hzScrollBar.SetDisplayPerc(Mathf.Min(1, _viewSize.x / _contentSize.x));
            }

            UpdateScrollBarVisible();

            if (!_maskDisabled)
            {
                var rect = new Rect(-owner._alignOffset.x, -owner._alignOffset.y, _viewSize.x, _viewSize.y);
                if (_vScrollNone && vtScrollBar != null)
                    rect.width += vtScrollBar.width;
                if (_hScrollNone && hzScrollBar != null)
                    rect.height += hzScrollBar.height;
                if (_dontClipMargin)
                {
                    rect.x -= owner.margin.left;
                    rect.width += owner.margin.left + owner.margin.right;
                    rect.y -= owner.margin.top;
                    rect.height += owner.margin.top + owner.margin.bottom;
                }

                _maskContainer.clipRect = rect;
            }

            if (_scrollType == ScrollType.Horizontal || _scrollType == ScrollType.Both)
                _overlapSize.x = Mathf.CeilToInt(Math.Max(0, _contentSize.x - _viewSize.x));
            else
                _overlapSize.x = 0;
            if (_scrollType == ScrollType.Vertical || _scrollType == ScrollType.Both)
                _overlapSize.y = Mathf.CeilToInt(Math.Max(0, _contentSize.y - _viewSize.y));
            else
                _overlapSize.y = 0;

            //边界检查
            _xPos = Mathf.Clamp(_xPos, 0, _overlapSize.x);
            _yPos = Mathf.Clamp(_yPos, 0, _overlapSize.y);
            var max = _overlapSize[_refreshBarAxis];
            if (max == 0)
                max = Mathf.Max(_contentSize[_refreshBarAxis] + _footerLockedSize - _viewSize[_refreshBarAxis], 0);
            else
                max += _footerLockedSize;
            if (_refreshBarAxis == 0)
                _container.SetXY(Mathf.Clamp(_container.x, -max, _headerLockedSize),
                    Mathf.Clamp(_container.y, -_overlapSize.y, 0));
            else
                _container.SetXY(Mathf.Clamp(_container.x, -_overlapSize.x, 0),
                    Mathf.Clamp(_container.y, -max, _headerLockedSize));

            if (header != null)
            {
                if (_refreshBarAxis == 0)
                    header.height = _viewSize.y;
                else
                    header.width = _viewSize.x;
            }

            if (footer != null)
            {
                if (_refreshBarAxis == 0)
                    footer.height = _viewSize.y;
                else
                    footer.width = _viewSize.x;
            }

            UpdateScrollBarPos();
            if (pageMode)
                UpdatePageController();
        }

        private void PosChanged(bool ani)
        {
            //只要有1处要求不要缓动，那就不缓动
            if (_aniFlag == 0)
                _aniFlag = ani ? 1 : -1;
            else if (_aniFlag == 1 && !ani)
                _aniFlag = -1;

            _needRefresh = true;

            UpdateContext.OnBegin -= _refreshDelegate;
            UpdateContext.OnBegin += _refreshDelegate;
        }

        private void Refresh()
        {
            _needRefresh = false;
            UpdateContext.OnBegin -= _refreshDelegate;

            if (owner.displayObject == null || owner.displayObject.isDisposed)
                return;

            if (pageMode || snapToItem)
            {
                var pos = new Vector2(-_xPos, -_yPos);
                AlignPosition(ref pos, false);
                _xPos = -pos.x;
                _yPos = -pos.y;
            }

            Refresh2();

            _onScroll.Call();
            if (_needRefresh) //在onScroll事件里开发者可能修改位置，这里再刷新一次，避免闪烁
            {
                _needRefresh = false;
                UpdateContext.OnBegin -= _refreshDelegate;

                Refresh2();
            }

            UpdateScrollBarPos();
            _aniFlag = 0;
        }

        private void Refresh2()
        {
            if (_aniFlag == 1 && !isDragged)
            {
                var pos = new Vector2();

                if (_overlapSize.x > 0)
                {
                    pos.x = -(int)_xPos;
                }
                else
                {
                    if (_container.x != 0)
                        _container.x = 0;
                    pos.x = 0;
                }

                if (_overlapSize.y > 0)
                {
                    pos.y = -(int)_yPos;
                }
                else
                {
                    if (_container.y != 0)
                        _container.y = 0;
                    pos.y = 0;
                }

                if (pos.x != _container.x || pos.y != _container.y)
                {
                    _tweenDuration = new Vector2(TWEEN_TIME_GO, TWEEN_TIME_GO);
                    _tweenStart = _container.xy;
                    _tweenChange = pos - _tweenStart;
                    StartTween(1);
                }
                else if (_tweening != 0)
                {
                    KillTween();
                }
            }
            else
            {
                if (_tweening != 0)
                    KillTween();

                _container.SetXY((int)-_xPos, (int)-_yPos);

                LoopCheckingCurrent();
            }

            if (pageMode)
                UpdatePageController();
        }

        private void __touchBegin(EventContext context)
        {
            if (!touchEffect)
                return;

            var evt = context.inputEvent;
            if (evt.button != 0)
                return;

            context.CaptureTouch();

            var pt = owner.GlobalToLocal(evt.position);

            if (_tweening != 0)
            {
                KillTween();
                Stage.inst.CancelClick(evt.touchId);

                //立刻停止惯性滚动，可能位置不对齐，设定这个标志，使touchEnd时归位
                isDragged = true;
            }
            else
            {
                isDragged = false;
            }

            _containerPos = _container.xy;
            _beginTouchPos = _lastTouchPos = pt;
            _lastTouchGlobalPos = evt.position;
            _isHoldAreaDone = false;
            _velocity = Vector2.zero;
            _velocityScale = 1;
            _lastMoveTime = Time.unscaledTime;
        }

        private void __touchMove(EventContext context)
        {
            if (!touchEffect || (draggingPane != null && draggingPane != this) ||
                GObject.draggingObject != null) //已经有其他拖动
                return;

            var evt = context.inputEvent;
            var pt = owner.GlobalToLocal(evt.position);
            if (float.IsNaN(pt.x))
                return;

            int sensitivity;
            if (Stage.touchScreen)
                sensitivity = UIConfig.touchScrollSensitivity;
            else
                sensitivity = 8;

            float diff;
            bool sv = false, sh = false;

            if (_scrollType == ScrollType.Vertical)
            {
                if (!_isHoldAreaDone)
                {
                    //表示正在监测垂直方向的手势
                    _gestureFlag |= 1;

                    diff = Mathf.Abs(_beginTouchPos.y - pt.y);
                    if (diff < sensitivity)
                        return;

                    if ((_gestureFlag & 2) != 0) //已经有水平方向的手势在监测，那么我们用严格的方式检查是不是按垂直方向移动，避免冲突
                    {
                        var diff2 = Mathf.Abs(_beginTouchPos.x - pt.x);
                        if (diff < diff2) //不通过则不允许滚动了
                            return;
                    }
                }

                sv = true;
            }
            else if (_scrollType == ScrollType.Horizontal)
            {
                if (!_isHoldAreaDone)
                {
                    _gestureFlag |= 2;

                    diff = Mathf.Abs(_beginTouchPos.x - pt.x);
                    if (diff < sensitivity)
                        return;

                    if ((_gestureFlag & 1) != 0)
                    {
                        var diff2 = Mathf.Abs(_beginTouchPos.y - pt.y);
                        if (diff < diff2)
                            return;
                    }
                }

                sh = true;
            }
            else
            {
                _gestureFlag = 3;

                if (!_isHoldAreaDone)
                {
                    diff = Mathf.Abs(_beginTouchPos.y - pt.y);
                    if (diff < sensitivity)
                    {
                        diff = Mathf.Abs(_beginTouchPos.x - pt.x);
                        if (diff < sensitivity)
                            return;
                    }
                }

                sv = sh = true;
            }

            var newPos = _containerPos + pt - _beginTouchPos;
            newPos.x = (int)newPos.x;
            newPos.y = (int)newPos.y;

            if (sv)
            {
                if (newPos.y > 0)
                {
                    if (!bouncebackEffect)
                        _container.y = 0;
                    else if (header != null && header.maxHeight != 0)
                        _container.y = (int)Mathf.Min(newPos.y * 0.5f, header.maxHeight);
                    else
                        _container.y = (int)Mathf.Min(newPos.y * 0.5f, _viewSize.y * PULL_RATIO);
                }
                else if (newPos.y < -_overlapSize.y)
                {
                    if (!bouncebackEffect)
                        _container.y = -_overlapSize.y;
                    else if (footer != null && footer.maxHeight > 0)
                        _container.y = (int)Mathf.Max((newPos.y + _overlapSize.y) * 0.5f, -footer.maxHeight) -
                                       _overlapSize.y;
                    else
                        _container.y = (int)Mathf.Max((newPos.y + _overlapSize.y) * 0.5f, -_viewSize.y * PULL_RATIO) -
                                       _overlapSize.y;
                }
                else
                {
                    _container.y = newPos.y;
                }
            }

            if (sh)
            {
                if (newPos.x > 0)
                {
                    if (!bouncebackEffect)
                        _container.x = 0;
                    else if (header != null && header.maxWidth != 0)
                        _container.x = (int)Mathf.Min(newPos.x * 0.5f, header.maxWidth);
                    else
                        _container.x = (int)Mathf.Min(newPos.x * 0.5f, _viewSize.x * PULL_RATIO);
                }
                else if (newPos.x < 0 - _overlapSize.x)
                {
                    if (!bouncebackEffect)
                        _container.x = -_overlapSize.x;
                    else if (footer != null && footer.maxWidth > 0)
                        _container.x = (int)Mathf.Max((newPos.x + _overlapSize.x) * 0.5f, -footer.maxWidth) -
                                       _overlapSize.x;
                    else
                        _container.x = (int)Mathf.Max((newPos.x + _overlapSize.x) * 0.5f, -_viewSize.x * PULL_RATIO) -
                                       _overlapSize.x;
                }
                else
                {
                    _container.x = newPos.x;
                }
            }

            //更新速度
            var deltaTime = Time.unscaledDeltaTime;
            var elapsed = (Time.unscaledTime - _lastMoveTime) * 60 - 1;
            if (elapsed > 1) //速度衰减
                _velocity = _velocity * Mathf.Pow(0.833f, elapsed);
            var deltaPosition = pt - _lastTouchPos;
            if (!sh)
                deltaPosition.x = 0;
            if (!sv)
                deltaPosition.y = 0;
            _velocity = Vector2.Lerp(_velocity, deltaPosition / deltaTime, deltaTime * 10);

            /*速度计算使用的是本地位移，但在后续的惯性滚动判断中需要用到屏幕位移，所以这里要记录一个位移的比例。
             *后续的处理要使用这个比例但不使用坐标转换的方法的原因是，在曲面UI等异形UI中，还无法简单地进行屏幕坐标和本地坐标的转换。
             */
            var deltaGlobalPosition = _lastTouchGlobalPos - evt.position;
            if (deltaPosition.x != 0)
                _velocityScale = Mathf.Abs(deltaGlobalPosition.x / deltaPosition.x);
            else if (deltaPosition.y != 0)
                _velocityScale = Mathf.Abs(deltaGlobalPosition.y / deltaPosition.y);

            _lastTouchPos = pt;
            _lastTouchGlobalPos = evt.position;
            _lastMoveTime = Time.unscaledTime;

            //同步更新pos值
            if (_overlapSize.x > 0)
                _xPos = Mathf.Clamp(-_container.x, 0, _overlapSize.x);
            if (_overlapSize.y > 0)
                _yPos = Mathf.Clamp(-_container.y, 0, _overlapSize.y);

            //循环滚动特别检查
            if (_loop != 0)
            {
                newPos = _container.xy;
                if (LoopCheckingCurrent())
                    _containerPos += _container.xy - newPos;
            }

            draggingPane = this;
            _isHoldAreaDone = true;
            isDragged = true;

            UpdateScrollBarPos();
            UpdateScrollBarVisible();
            if (pageMode)
                UpdatePageController();
            _onScroll.Call();
        }

        private void __touchEnd(EventContext context)
        {
            if (draggingPane == this)
                draggingPane = null;

            _gestureFlag = 0;

            if (!isDragged || !touchEffect)
            {
                isDragged = false;
                return;
            }

            isDragged = false;
            _tweenStart = _container.xy;

            var endPos = _tweenStart;
            var flag = false;
            if (_container.x > 0)
            {
                endPos.x = 0;
                flag = true;
            }
            else if (_container.x < -_overlapSize.x)
            {
                endPos.x = -_overlapSize.x;
                flag = true;
            }

            if (_container.y > 0)
            {
                endPos.y = 0;
                flag = true;
            }
            else if (_container.y < -_overlapSize.y)
            {
                endPos.y = -_overlapSize.y;
                flag = true;
            }

            if (flag)
            {
                _tweenChange = endPos - _tweenStart;
                if (_tweenChange.x < -UIConfig.touchDragSensitivity || _tweenChange.y < -UIConfig.touchDragSensitivity)
                    DispatchEvent("onPullDownRelease", null);
                else if (_tweenChange.x > UIConfig.touchDragSensitivity ||
                         _tweenChange.y > UIConfig.touchDragSensitivity)
                    DispatchEvent("onPullUpRelease", null);

                if (_headerLockedSize > 0 && endPos[_refreshBarAxis] == 0)
                {
                    endPos[_refreshBarAxis] = _headerLockedSize;
                    _tweenChange = endPos - _tweenStart;
                }
                else if (_footerLockedSize > 0 && endPos[_refreshBarAxis] == -_overlapSize[_refreshBarAxis])
                {
                    var max = _overlapSize[_refreshBarAxis];
                    if (max == 0)
                        max = Mathf.Max(_contentSize[_refreshBarAxis] + _footerLockedSize - _viewSize[_refreshBarAxis],
                            0);
                    else
                        max += _footerLockedSize;
                    endPos[_refreshBarAxis] = -max;
                    _tweenChange = endPos - _tweenStart;
                }

                _tweenDuration.Set(TWEEN_TIME_DEFAULT, TWEEN_TIME_DEFAULT);
            }
            else
            {
                //更新速度
                if (!inertiaDisabled)
                {
                    var elapsed = (Time.unscaledTime - _lastMoveTime) * 60 - 1;
                    if (elapsed > 1)
                        _velocity = _velocity * Mathf.Pow(0.833f, elapsed);

                    //根据速度计算目标位置和需要时间
                    endPos = UpdateTargetAndDuration(_tweenStart);
                }
                else
                {
                    _tweenDuration.Set(TWEEN_TIME_DEFAULT, TWEEN_TIME_DEFAULT);
                }

                var oldChange = endPos - _tweenStart;

                //调整目标位置
                LoopCheckingTarget(ref endPos);
                if (pageMode || snapToItem)
                    AlignPosition(ref endPos, true);

                _tweenChange = endPos - _tweenStart;
                if (_tweenChange.x == 0 && _tweenChange.y == 0)
                {
                    UpdateScrollBarVisible();
                    return;
                }

                //如果目标位置已调整，随之调整需要时间
                if (pageMode || snapToItem)
                {
                    FixDuration(0, oldChange.x);
                    FixDuration(1, oldChange.y);
                }
            }

            StartTween(2);
        }

        private void __mouseWheel(EventContext context)
        {
            if (!mouseWheelEnabled)
                return;

            var evt = context.inputEvent;
            var delta = evt.mouseWheelDelta / Stage.devicePixelRatio;
            if (snapToItem && Mathf.Abs(delta) < 1)
                delta = Mathf.Sign(delta);

            if (_overlapSize.x > 0 && _overlapSize.y == 0)
            {
                var step = pageMode ? _pageSize.x : _scrollStep;
                SetPosX(_xPos + step * delta, false);
            }
            else
            {
                var step = pageMode ? _pageSize.y : _scrollStep;
                SetPosY(_yPos + step * delta, false);
            }
        }

        private void __rollOver()
        {
            _hover = true;
            UpdateScrollBarVisible();
        }

        private void __rollOut()
        {
            _hover = false;
            UpdateScrollBarVisible();
        }

        internal void UpdateClipSoft()
        {
            var softness = owner.clipSoftness;
            if (softness.x != 0 || softness.y != 0)
                _maskContainer.clipSoftness = new Vector4(
                    _container.x >= 0 || !softnessOnTopOrLeftSide ? 0 : softness.x,
                    _container.y >= 0 || !softnessOnTopOrLeftSide ? 0 : softness.y,
                    -_container.x - _overlapSize.x >= 0 ? 0 : softness.x,
                    -_container.y - _overlapSize.y >= 0 ? 0 : softness.y);
            else
                _maskContainer.clipSoftness = null;
        }

        private void UpdateScrollBarPos()
        {
            if (vtScrollBar != null)
                vtScrollBar.setScrollPerc(_overlapSize.y == 0
                    ? 0
                    : Mathf.Clamp(-_container.y, 0, _overlapSize.y) / _overlapSize.y);

            if (hzScrollBar != null)
                hzScrollBar.setScrollPerc(_overlapSize.x == 0
                    ? 0
                    : Mathf.Clamp(-_container.x, 0, _overlapSize.x) / _overlapSize.x);

            UpdateClipSoft();
            CheckRefreshBar();
        }

        public void UpdateScrollBarVisible()
        {
            if (vtScrollBar != null)
            {
                if (_viewSize.y <= vtScrollBar.minSize || _vScrollNone)
                    vtScrollBar.displayObject.visible = false;
                else
                    UpdateScrollBarVisible2(vtScrollBar);
            }

            if (hzScrollBar != null)
            {
                if (_viewSize.x <= hzScrollBar.minSize || _hScrollNone)
                    hzScrollBar.displayObject.visible = false;
                else
                    UpdateScrollBarVisible2(hzScrollBar);
            }
        }

        private void UpdateScrollBarVisible2(GScrollBar bar)
        {
            if (_scrollBarDisplayAuto)
                GTween.Kill(bar, TweenPropType.Alpha, false);

            if (_scrollBarDisplayAuto && !_hover && _tweening == 0 && !isDragged && !bar.gripDragging)
            {
                if (bar.displayObject.visible)
                    GTween.To(1, 0, 0.5f).SetDelay(0.5f).OnComplete(_hideScrollBarDelegate)
                        .SetTarget(bar, TweenPropType.Alpha);
            }
            else
            {
                bar.alpha = 1;
                bar.displayObject.visible = true;
            }
        }

        private void __barTweenComplete(GTweener tweener)
        {
            var bar = (GObject)tweener.target;
            bar.alpha = 1;
            bar.displayObject.visible = false;
        }

        private float GetLoopPartSize(float division, int axis)
        {
            return (_contentSize[axis] + (axis == 0 ? ((GList)owner).columnGap : ((GList)owner).lineGap)) / division;
        }

        /// <summary>
        ///     对当前的滚动位置进行循环滚动边界检查。当到达边界时，回退一半内容区域（循环滚动内容大小通常是真实内容大小的偶数倍）。
        /// </summary>
        /// <returns></returns>
        private bool LoopCheckingCurrent()
        {
            var changed = false;
            if (_loop == 1 && _overlapSize.x > 0)
            {
                if (_xPos < 0.001f)
                {
                    _xPos += GetLoopPartSize(2, 0);
                    changed = true;
                }
                else if (_xPos >= _overlapSize.x)
                {
                    _xPos -= GetLoopPartSize(2, 0);
                    changed = true;
                }
            }
            else if (_loop == 2 && _overlapSize.y > 0)
            {
                if (_yPos < 0.001f)
                {
                    _yPos += GetLoopPartSize(2, 1);
                    changed = true;
                }
                else if (_yPos >= _overlapSize.y)
                {
                    _yPos -= GetLoopPartSize(2, 1);
                    changed = true;
                }
            }

            if (changed)
                _container.SetXY((int)-_xPos, (int)-_yPos);

            return changed;
        }

        /// <summary>
        ///     对目标位置进行循环滚动边界检查。当到达边界时，回退一半内容区域（循环滚动内容大小通常是真实内容大小的偶数倍）。
        /// </summary>
        /// <param name="endPos"></param>
        private void LoopCheckingTarget(ref Vector2 endPos)
        {
            if (_loop == 1)
                LoopCheckingTarget(ref endPos, 0);

            if (_loop == 2)
                LoopCheckingTarget(ref endPos, 1);
        }

        private void LoopCheckingTarget(ref Vector2 endPos, int axis)
        {
            if (endPos[axis] > 0)
            {
                var halfSize = GetLoopPartSize(2, axis);
                var tmp = _tweenStart[axis] - halfSize;
                if (tmp <= 0 && tmp >= -_overlapSize[axis])
                {
                    endPos[axis] -= halfSize;
                    _tweenStart[axis] = tmp;
                }
            }
            else if (endPos[axis] < -_overlapSize[axis])
            {
                var halfSize = GetLoopPartSize(2, axis);
                var tmp = _tweenStart[axis] + halfSize;
                if (tmp <= 0 && tmp >= -_overlapSize[axis])
                {
                    endPos[axis] += halfSize;
                    _tweenStart[axis] = tmp;
                }
            }
        }

        private void LoopCheckingNewPos(ref float value, int axis)
        {
            if (_overlapSize[axis] == 0)
                return;

            var pos = axis == 0 ? _xPos : _yPos;
            var changed = false;
            if (value < 0.001f)
            {
                value += GetLoopPartSize(2, axis);
                if (value > pos)
                {
                    var v = GetLoopPartSize(6, axis);
                    v = Mathf.CeilToInt((value - pos) / v) * v;
                    pos = Mathf.Clamp(pos + v, 0, _overlapSize[axis]);
                    changed = true;
                }
            }
            else if (value >= _overlapSize[axis])
            {
                value -= GetLoopPartSize(2, axis);
                if (value < pos)
                {
                    var v = GetLoopPartSize(6, axis);
                    v = Mathf.CeilToInt((pos - value) / v) * v;
                    pos = Mathf.Clamp(pos - v, 0, _overlapSize[axis]);
                    changed = true;
                }
            }

            if (changed)
            {
                if (axis == 0)
                    _container.x = -(int)pos;
                else
                    _container.y = -(int)pos;
            }
        }

        /// <summary>
        ///     从oldPos滚动至pos，调整pos位置对齐页面、对齐item等（如果需要）。
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="inertialScrolling"></param>
        private void AlignPosition(ref Vector2 pos, bool inertialScrolling)
        {
            if (pageMode)
            {
                pos.x = AlignByPage(pos.x, 0, inertialScrolling);
                pos.y = AlignByPage(pos.y, 1, inertialScrolling);
            }
            else if (snapToItem)
            {
                var tmpX = -pos.x;
                var tmpY = -pos.y;
                float xDir = 0;
                float yDir = 0;
                if (inertialScrolling)
                {
                    xDir = pos.x - _containerPos.x;
                    yDir = pos.y - _containerPos.y;
                }

                owner.GetSnappingPositionWithDir(ref tmpX, ref tmpY, xDir, yDir);
                if (pos.x < 0 && pos.x > -_overlapSize.x)
                    pos.x = -tmpX;
                if (pos.y < 0 && pos.y > -_overlapSize.y)
                    pos.y = -tmpY;
            }
        }

        /// <summary>
        ///     从oldPos滚动至pos，调整目标位置到对齐页面。
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="axis"></param>
        /// <param name="inertialScrolling"></param>
        /// <returns></returns>
        private float AlignByPage(float pos, int axis, bool inertialScrolling)
        {
            int page;

            if (pos > 0)
            {
                page = 0;
            }
            else if (pos < -_overlapSize[axis])
            {
                page = Mathf.CeilToInt(_contentSize[axis] / _pageSize[axis]) - 1;
            }
            else
            {
                page = Mathf.FloorToInt(-pos / _pageSize[axis]);
                var change = inertialScrolling ? pos - _containerPos[axis] : pos - _container.xy[axis];
                var testPageSize = Mathf.Min(_pageSize[axis], _contentSize[axis] - (page + 1) * _pageSize[axis]);
                var delta = -pos - page * _pageSize[axis];

                //页面吸附策略
                if (Mathf.Abs(change) > _pageSize[axis]) //如果滚动距离超过1页,则需要超过页面的一半，才能到更下一页
                {
                    if (delta > testPageSize * 0.5f)
                        page++;
                }
                else //否则只需要页面的1/3，当然，需要考虑到左移和右移的情况
                {
                    if (delta > testPageSize * (change < 0
                            ? UIConfig.defaultScrollPagingThreshold
                            : 1 - UIConfig.defaultScrollPagingThreshold))
                        page++;
                }

                //重新计算终点
                pos = -page * _pageSize[axis];
                if (pos < -_overlapSize[axis]) //最后一页未必有pageSize那么大
                    pos = -_overlapSize[axis];
            }

            //惯性滚动模式下，会增加判断尽量不要滚动超过一页
            if (inertialScrolling)
            {
                var oldPos = _tweenStart[axis];
                int oldPage;
                if (oldPos > 0)
                    oldPage = 0;
                else if (oldPos < -_overlapSize[axis])
                    oldPage = Mathf.CeilToInt(_contentSize[axis] / _pageSize[axis]) - 1;
                else
                    oldPage = Mathf.FloorToInt(-oldPos / _pageSize[axis]);
                var startPage = Mathf.FloorToInt(-_containerPos[axis] / _pageSize[axis]);
                if (Mathf.Abs(page - startPage) > 1 && Mathf.Abs(oldPage - startPage) <= 1)
                {
                    if (page > startPage)
                        page = startPage + 1;
                    else
                        page = startPage - 1;
                    pos = -page * _pageSize[axis];
                }
            }

            return pos;
        }

        /// <summary>
        ///     根据当前速度，计算滚动的目标位置，以及到达时间。
        /// </summary>
        /// <param name="orignPos"></param>
        /// <returns></returns>
        private Vector2 UpdateTargetAndDuration(Vector2 orignPos)
        {
            var ret = Vector2.zero;
            ret.x = UpdateTargetAndDuration(orignPos.x, 0);
            ret.y = UpdateTargetAndDuration(orignPos.y, 1);
            return ret;
        }

        private float UpdateTargetAndDuration(float pos, int axis)
        {
            var v = _velocity[axis];
            float duration = 0;

            if (pos > 0)
            {
                pos = 0;
            }
            else if (pos < -_overlapSize[axis])
            {
                pos = -_overlapSize[axis];
            }
            else
            {
                //以屏幕像素为基准
                var v2 = Mathf.Abs(v) * _velocityScale;
                //在移动设备上，需要对不同分辨率做一个适配，我们的速度判断以1136分辨率为基准
                if (Stage.touchScreen)
                    v2 *= 1136f / Mathf.Max(Screen.width, Screen.height);
                //这里有一些阈值的处理，因为在低速内，不希望产生较大的滚动（甚至不滚动）
                float ratio = 0;
                if (pageMode || !Stage.touchScreen)
                {
                    if (v2 > 500)
                        ratio = Mathf.Pow((v2 - 500) / 500, 2);
                }
                else
                {
                    if (v2 > 1000)
                        ratio = Mathf.Pow((v2 - 1000) / 1000, 2);
                }

                if (ratio != 0)
                {
                    if (ratio > 1)
                        ratio = 1;

                    v2 *= ratio;
                    v *= ratio;
                    _velocity[axis] = v;

                    //算法：v*（_decelerationRate的n次幂）= 60，即在n帧后速度降为60（假设每秒60帧）。
                    duration = Mathf.Log(60 / v2, decelerationRate) / 60;

                    //计算距离要使用本地速度
                    //理论公式貌似滚动的距离不够，改为经验公式
                    //float change = (int)((v/ 60 - 1) / (1 - _decelerationRate));
                    float change = (int)(v * duration * 0.4f);
                    pos += change;
                }
            }

            if (duration < TWEEN_TIME_DEFAULT)
                duration = TWEEN_TIME_DEFAULT;
            _tweenDuration[axis] = duration;

            return pos;
        }

        /// <summary>
        ///     根据修改后的tweenChange重新计算减速时间。
        /// </summary>
        private void FixDuration(int axis, float oldChange)
        {
            if (_tweenChange[axis] == 0 || Mathf.Abs(_tweenChange[axis]) >= Mathf.Abs(oldChange))
                return;

            var newDuration = Mathf.Abs(_tweenChange[axis] / oldChange) * _tweenDuration[axis];
            if (newDuration < TWEEN_TIME_DEFAULT)
                newDuration = TWEEN_TIME_DEFAULT;

            _tweenDuration[axis] = newDuration;
        }

        private void StartTween(int type)
        {
            _tweenTime.Set(0, 0);
            _tweening = type;
            Timers.inst.AddUpdate(_tweenUpdateDelegate);

            UpdateScrollBarVisible();
        }

        private void KillTween()
        {
            if (_tweening == 1) //取消类型为1的tween需立刻设置到终点
            {
                _container.xy = _tweenStart + _tweenChange;
                _onScroll.Call();
            }

            _tweening = 0;
            Timers.inst.Remove(_tweenUpdateDelegate);

            UpdateScrollBarVisible();

            _onScrollEnd.Call();
        }

        private void CheckRefreshBar()
        {
            if (header == null && footer == null)
                return;

            var pos = _container.xy[_refreshBarAxis];
            if (header != null)
            {
                if (pos > 0)
                {
                    if (header.displayObject.parent == null)
                        _maskContainer.AddChildAt(header.displayObject, 0);
                    Vector2 vec;

                    vec = header.size;
                    vec[_refreshBarAxis] = pos;
                    header.size = vec;
                }
                else
                {
                    if (header.displayObject.parent != null)
                        _maskContainer.RemoveChild(header.displayObject);
                }
            }

            if (footer != null)
            {
                var max = _overlapSize[_refreshBarAxis];
                if (pos < -max || (max == 0 && _footerLockedSize > 0))
                {
                    if (footer.displayObject.parent == null)
                        _maskContainer.AddChildAt(footer.displayObject, 0);

                    Vector2 vec;

                    vec = footer.xy;
                    if (max > 0)
                        vec[_refreshBarAxis] = pos + _contentSize[_refreshBarAxis];
                    else
                        vec[_refreshBarAxis] =
                            Mathf.Max(
                                Mathf.Min(pos + _viewSize[_refreshBarAxis],
                                    _viewSize[_refreshBarAxis] - _footerLockedSize),
                                _viewSize[_refreshBarAxis] - _contentSize[_refreshBarAxis]);
                    footer.xy = vec;

                    vec = footer.size;
                    if (max > 0)
                        vec[_refreshBarAxis] = -max - pos;
                    else
                        vec[_refreshBarAxis] = _viewSize[_refreshBarAxis] - footer.xy[_refreshBarAxis];
                    footer.size = vec;
                }
                else
                {
                    if (footer.displayObject.parent != null)
                        _maskContainer.RemoveChild(footer.displayObject);
                }
            }
        }

        private void TweenUpdate(object param)
        {
            if (owner.displayObject == null || owner.displayObject.isDisposed)
            {
                Timers.inst.Remove(_tweenUpdateDelegate);
                return;
            }

            var nx = RunTween(0);
            var ny = RunTween(1);

            _container.SetXY(nx, ny);

            if (_tweening == 2)
            {
                if (_overlapSize.x > 0)
                    _xPos = Mathf.Clamp(-nx, 0, _overlapSize.x);
                if (_overlapSize.y > 0)
                    _yPos = Mathf.Clamp(-ny, 0, _overlapSize.y);

                if (pageMode)
                    UpdatePageController();
            }

            if (_tweenChange.x == 0 && _tweenChange.y == 0)
            {
                _tweening = 0;
                Timers.inst.Remove(_tweenUpdateDelegate);

                LoopCheckingCurrent();

                UpdateScrollBarPos();
                UpdateScrollBarVisible();

                _onScroll.Call();
                _onScrollEnd.Call();
            }
            else
            {
                UpdateScrollBarPos();
                _onScroll.Call();
            }
        }

        private float RunTween(int axis)
        {
            float newValue;
            if (_tweenChange[axis] != 0)
            {
                _tweenTime[axis] += Time.unscaledDeltaTime;
                if (_tweenTime[axis] >= _tweenDuration[axis])
                {
                    newValue = _tweenStart[axis] + _tweenChange[axis];
                    _tweenChange[axis] = 0;
                }
                else
                {
                    var ratio = EaseFunc(_tweenTime[axis], _tweenDuration[axis]);
                    newValue = _tweenStart[axis] + (int)(_tweenChange[axis] * ratio);
                }

                float threshold1 = 0;
                var threshold2 = -_overlapSize[axis];
                if (_headerLockedSize > 0 && _refreshBarAxis == axis)
                    threshold1 = _headerLockedSize;
                if (_footerLockedSize > 0 && _refreshBarAxis == axis)
                {
                    var max = _overlapSize[_refreshBarAxis];
                    if (max == 0)
                        max = Mathf.Max(_contentSize[_refreshBarAxis] + _footerLockedSize - _viewSize[_refreshBarAxis],
                            0);
                    else
                        max += _footerLockedSize;
                    threshold2 = -max;
                }

                if (_tweening == 2 && bouncebackEffect)
                {
                    if ((newValue > 20 + threshold1 && _tweenChange[axis] > 0)
                        || (newValue > threshold1 && _tweenChange[axis] == 0)) //开始回弹
                    {
                        _tweenTime[axis] = 0;
                        _tweenDuration[axis] = TWEEN_TIME_DEFAULT;
                        _tweenChange[axis] = -newValue + threshold1;
                        _tweenStart[axis] = newValue;
                    }
                    else if ((newValue < threshold2 - 20 && _tweenChange[axis] < 0)
                             || (newValue < threshold2 && _tweenChange[axis] == 0)) //开始回弹
                    {
                        _tweenTime[axis] = 0;
                        _tweenDuration[axis] = TWEEN_TIME_DEFAULT;
                        _tweenChange[axis] = threshold2 - newValue;
                        _tweenStart[axis] = newValue;
                    }
                }
                else
                {
                    if (newValue > threshold1)
                    {
                        newValue = threshold1;
                        _tweenChange[axis] = 0;
                    }
                    else if (newValue < threshold2)
                    {
                        newValue = threshold2;
                        _tweenChange[axis] = 0;
                    }
                }
            }
            else
            {
                newValue = _container.xy[axis];
            }

            return newValue;
        }

        private static float EaseFunc(float t, float d)
        {
            return (t = t / d - 1) * t * t + 1; //cubicOut
        }
    }
}