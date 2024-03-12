using System;
using System.Collections.Generic;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     Callback function when an item is needed to update its look.
    /// </summary>
    /// <param name="index">Item index.</param>
    /// <param name="item">Item object.</param>
    public delegate void ListItemRenderer(int index, GObject item);

    /// <summary>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public delegate string ListItemProvider(int index);

    /// <summary>
    ///     GList class.
    /// </summary>
    public class GList : GComponent
    {
        private AlignType _align;
        private bool _autoResizeItem;
        private int _columnCount;
        private int _columnGap;
        private int _curLineItemCount; //item count in one line
        private int _curLineItemCount2; //只用在页面模式，表示垂直方向的项目数

        private string _defaultItem;
        private int _firstIndex; //the top left index

        private readonly EventCallback1 _itemClickDelegate;
        private Vector2 _itemSize;
        private int _lastSelectedIndex;
        private ListLayoutType _layout;
        private int _lineCount;
        private int _lineGap;
        private bool _loop;

        private int _miscFlags; //1-event locked, 2-focus events registered
        private int _numItems;

        private EventListener _onClickItem;
        private EventListener _onRightClickItem;

        private int _realNumItems;
        private VertAlignType _verticalAlign;

        //Virtual List support
        private List<ItemInfo> _virtualItems;
        private int _virtualListChanged; //1-content changed, 2-size changed

        /// <summary>
        ///     如果true，当item不可见时自动折叠，否则依然占位
        /// </summary>
        public bool foldInvisibleItems;

        private uint itemInfoVer; //用来标志item是否在本次处理中已经被重用了

        /// <summary>
        ///     Callback funtion to return item resource url.
        /// </summary>
        public ListItemProvider itemProvider;

        /// <summary>
        ///     Callback function when an item is needed to update its look.
        /// </summary>
        public ListItemRenderer itemRenderer;

        /// <summary>
        /// </summary>
        public bool scrollItemToViewOnClick;

        /// <summary>
        ///     List selection mode
        /// </summary>
        /// <seealso cref="ListSelectionMode" />
        public ListSelectionMode selectionMode;

        public GList()
        {
            _trackBounds = true;
            opaque = true;
            scrollItemToViewOnClick = true;

            container = new Container();
            rootContainer.AddChild(container);
            rootContainer.gameObject.name = "GList";

            itemPool = new GObjectPool(container.cachedTransform);

            _itemClickDelegate = __clickItem;
        }

        /// <summary>
        ///     Dispatched when a list item being clicked.
        /// </summary>
        public EventListener onClickItem => _onClickItem ?? (_onClickItem = new EventListener(this, "onClickItem"));

        /// <summary>
        ///     Dispatched when a list item being clicked with right button.
        /// </summary>
        public EventListener onRightClickItem =>
            _onRightClickItem ?? (_onRightClickItem = new EventListener(this, "onRightClickItem"));

        /// <summary>
        ///     Resource url of the default item.
        /// </summary>
        public string defaultItem
        {
            get => _defaultItem;
            set => _defaultItem = UIPackage.NormalizeURL(value);
        }

        /// <summary>
        ///     List layout type.
        /// </summary>
        public ListLayoutType layout
        {
            get => _layout;
            set
            {
                if (_layout != value)
                {
                    _layout = value;
                    SetBoundsChangedFlag();
                    if (isVirtual)
                        SetVirtualListChangedFlag(true);
                }
            }
        }

        /// <summary>
        /// </summary>
        public int lineCount
        {
            get => _lineCount;
            set
            {
                if (_lineCount != value)
                {
                    _lineCount = value;
                    if (_layout == ListLayoutType.FlowVertical || _layout == ListLayoutType.Pagination)
                    {
                        SetBoundsChangedFlag();
                        if (isVirtual)
                            SetVirtualListChangedFlag(true);
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        public int columnCount
        {
            get => _columnCount;
            set
            {
                if (_columnCount != value)
                {
                    _columnCount = value;
                    if (_layout == ListLayoutType.FlowHorizontal || _layout == ListLayoutType.Pagination)
                    {
                        SetBoundsChangedFlag();
                        if (isVirtual)
                            SetVirtualListChangedFlag(true);
                    }
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
                    SetBoundsChangedFlag();
                    if (isVirtual)
                        SetVirtualListChangedFlag(true);
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
                    SetBoundsChangedFlag();
                    if (isVirtual)
                        SetVirtualListChangedFlag(true);
                }
            }
        }

        /// <summary>
        /// </summary>
        public AlignType align
        {
            get => _align;
            set
            {
                if (_align != value)
                {
                    _align = value;
                    SetBoundsChangedFlag();
                    if (isVirtual)
                        SetVirtualListChangedFlag(true);
                }
            }
        }

        /// <summary>
        /// </summary>
        public VertAlignType verticalAlign
        {
            get => _verticalAlign;
            set
            {
                if (_verticalAlign != value)
                {
                    _verticalAlign = value;
                    SetBoundsChangedFlag();
                    if (isVirtual)
                        SetVirtualListChangedFlag(true);
                }
            }
        }

        /// <summary>
        ///     If the item will resize itself to fit the list width/height.
        /// </summary>
        public bool autoResizeItem
        {
            get => _autoResizeItem;
            set
            {
                if (_autoResizeItem != value)
                {
                    _autoResizeItem = value;
                    SetBoundsChangedFlag();
                    if (isVirtual)
                        SetVirtualListChangedFlag(true);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <value></value>
        public Vector2 defaultItemSize
        {
            get => _itemSize;
            set
            {
                _itemSize = value;
                if (isVirtual)
                {
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                        scrollPane.scrollStep = _itemSize.y;
                    else
                        scrollPane.scrollStep = _itemSize.x;
                    SetVirtualListChangedFlag(true);
                }
            }
        }

        /// <summary>
        /// </summary>
        public GObjectPool itemPool { get; }

        /// <summary>
        /// </summary>
        public int selectedIndex
        {
            get
            {
                if (isVirtual)
                {
                    var cnt = _realNumItems;
                    for (var i = 0; i < cnt; i++)
                    {
                        var ii = _virtualItems[i];
                        if ((ii.obj is GButton && ((GButton)ii.obj).selected)
                            || (ii.obj == null && ii.selected))
                        {
                            if (_loop)
                                return i % _numItems;
                            return i;
                        }
                    }
                }
                else
                {
                    var cnt = _children.Count;
                    for (var i = 0; i < cnt; i++)
                    {
                        var obj = _children[i].asButton;
                        if (obj != null && obj.selected)
                            return i;
                    }
                }

                return -1;
            }

            set
            {
                if (value >= 0 && value < numItems)
                {
                    if (selectionMode != ListSelectionMode.Single)
                        ClearSelection();
                    AddSelection(value, false);
                }
                else
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// </summary>
        public Controller selectionController { get; set; }

        /// <summary>
        ///     获取当前点击哪个item
        /// </summary>
        public GObject touchItem
        {
            get
            {
                //find out which item is under finger
                //逐层往上知道查到点击了那个item
                var obj = GRoot.inst.touchTarget;
                GObject p = obj.parent;
                while (p != null)
                {
                    if (p == this)
                        return obj;

                    obj = p;
                    p = p.parent;
                }

                return null;
            }
        }

        public bool isVirtual { get; private set; }

        /// <summary>
        ///     Set the list item count.
        ///     If the list is not virtual, specified number of items will be created.
        ///     If the list is virtual, only items in view will be created.
        /// </summary>
        public int numItems
        {
            get
            {
                if (isVirtual)
                    return _numItems;
                return _children.Count;
            }
            set
            {
                if (isVirtual)
                {
                    if (itemRenderer == null)
                        throw new Exception("FairyGUI: Set itemRenderer first!");

                    _numItems = value;
                    if (_loop)
                        _realNumItems = _numItems * 6; //设置6倍数量，用于循环滚动
                    else
                        _realNumItems = _numItems;

                    //_virtualItems的设计是只增不减的
                    var oldCount = _virtualItems.Count;
                    if (_realNumItems > oldCount)
                        for (var i = oldCount; i < _realNumItems; i++)
                        {
                            var ii = new ItemInfo();
                            ii.size = _itemSize;

                            _virtualItems.Add(ii);
                        }
                    else
                        for (var i = _realNumItems; i < oldCount; i++)
                            _virtualItems[i].selected = false;

                    if (_virtualListChanged != 0)
                        Timers.inst.Remove(RefreshVirtualList);
                    //立即刷新
                    RefreshVirtualList(null);
                }
                else
                {
                    var cnt = _children.Count;
                    if (value > cnt)
                        for (var i = cnt; i < value; i++)
                            if (itemProvider == null)
                                AddItemFromPool();
                            else
                                AddItemFromPool(itemProvider(i));
                    else
                        RemoveChildrenToPool(value, cnt);

                    if (itemRenderer != null)
                        for (var i = 0; i < value; i++)
                            itemRenderer(i, GetChildAt(i));
                }
            }
        }

        public override void Dispose()
        {
            itemPool.Clear();
            if (_virtualListChanged != 0)
                Timers.inst.Remove(RefreshVirtualList);

            selectionController = null;
            scrollItemToViewOnClick = false;
            itemRenderer = null;
            itemProvider = null;

            base.Dispose();
        }

        /// <summary>
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public GObject GetFromPool(string url)
        {
            if (string.IsNullOrEmpty(url))
                url = _defaultItem;

            var ret = itemPool.GetObject(url);
            if (ret != null)
                ret.visible = true;
            return ret;
        }

        private void ReturnToPool(GObject obj)
        {
            itemPool.ReturnObject(obj);
        }

        /// <summary>
        ///     Add a item to list, same as GetFromPool+AddChild
        /// </summary>
        /// <returns>Item object</returns>
        public GObject AddItemFromPool()
        {
            var obj = GetFromPool(null);

            return AddChild(obj);
        }

        /// <summary>
        ///     Add a item to list, same as GetFromPool+AddChild
        /// </summary>
        /// <param name="url">Item resource url</param>
        /// <returns>Item object</returns>
        public GObject AddItemFromPool(string url)
        {
            var obj = GetFromPool(url);

            return AddChild(obj);
        }

        /// <summary>
        /// </summary>
        /// <param name="child"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public override GObject AddChildAt(GObject child, int index)
        {
            base.AddChildAt(child, index);
            if (child is GButton)
            {
                var button = (GButton)child;
                button.selected = false;
                button.changeStateOnClick = false;
            }

            child.onClick.Add(_itemClickDelegate);
            child.onRightClick.Add(_itemClickDelegate);

            return child;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dispose"></param>
        /// <returns></returns>
        public override GObject RemoveChildAt(int index, bool dispose)
        {
            var child = base.RemoveChildAt(index, dispose);
            child.onClick.Remove(_itemClickDelegate);
            child.onRightClick.Remove(_itemClickDelegate);

            return child;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public void RemoveChildToPoolAt(int index)
        {
            var child = base.RemoveChildAt(index);
            ReturnToPool(child);
        }

        /// <summary>
        /// </summary>
        /// <param name="child"></param>
        public void RemoveChildToPool(GObject child)
        {
            RemoveChild(child);
            ReturnToPool(child);
        }

        /// <summary>
        /// </summary>
        public void RemoveChildrenToPool()
        {
            RemoveChildrenToPool(0, -1);
        }

        /// <summary>
        /// </summary>
        /// <param name="beginIndex"></param>
        /// <param name="endIndex"></param>
        public void RemoveChildrenToPool(int beginIndex, int endIndex)
        {
            if (endIndex < 0 || endIndex >= _children.Count)
                endIndex = _children.Count - 1;

            for (var i = beginIndex; i <= endIndex; ++i)
                RemoveChildToPoolAt(beginIndex);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public List<int> GetSelection()
        {
            return GetSelection(null);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public List<int> GetSelection(List<int> result)
        {
            if (result == null)
                result = new List<int>();
            if (isVirtual)
            {
                var cnt = _realNumItems;
                for (var i = 0; i < cnt; i++)
                {
                    var ii = _virtualItems[i];
                    if ((ii.obj is GButton && ((GButton)ii.obj).selected)
                        || (ii.obj == null && ii.selected))
                    {
                        var j = i;
                        if (_loop)
                        {
                            j = i % _numItems;
                            if (result.Contains(j))
                                continue;
                        }

                        result.Add(j);
                    }
                }
            }
            else
            {
                var cnt = _children.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = _children[i].asButton;
                    if (obj != null && obj.selected)
                        result.Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="scrollItToView"></param>
        public void AddSelection(int index, bool scrollItToView)
        {
            if (selectionMode == ListSelectionMode.None)
                return;

            CheckVirtualList();

            if (selectionMode == ListSelectionMode.Single)
                ClearSelection();

            if (scrollItToView)
                ScrollToView(index);

            _lastSelectedIndex = index;
            GButton obj = null;
            if (isVirtual)
            {
                var ii = _virtualItems[index];
                if (ii.obj != null)
                    obj = ii.obj.asButton;
                ii.selected = true;
            }
            else
            {
                obj = GetChildAt(index).asButton;
            }

            if (obj != null && !obj.selected)
            {
                obj.selected = true;
                UpdateSelectionController(index);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public void RemoveSelection(int index)
        {
            if (selectionMode == ListSelectionMode.None)
                return;

            GButton obj = null;
            if (isVirtual)
            {
                var ii = _virtualItems[index];
                if (ii.obj != null)
                    obj = ii.obj.asButton;
                ii.selected = false;
            }
            else
            {
                obj = GetChildAt(index).asButton;
            }

            if (obj != null)
                obj.selected = false;
        }

        /// <summary>
        /// </summary>
        public void ClearSelection()
        {
            if (isVirtual)
            {
                var cnt = _realNumItems;
                for (var i = 0; i < cnt; i++)
                {
                    var ii = _virtualItems[i];
                    if (ii.obj is GButton)
                        ((GButton)ii.obj).selected = false;
                    ii.selected = false;
                }
            }
            else
            {
                var cnt = _children.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = _children[i].asButton;
                    if (obj != null)
                        obj.selected = false;
                }
            }
        }

        private void ClearSelectionExcept(GObject g)
        {
            if (isVirtual)
            {
                var cnt = _realNumItems;
                for (var i = 0; i < cnt; i++)
                {
                    var ii = _virtualItems[i];
                    if (ii.obj != g)
                    {
                        if (ii.obj is GButton)
                            ((GButton)ii.obj).selected = false;
                        ii.selected = false;
                    }
                }
            }
            else
            {
                var cnt = _children.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = _children[i].asButton;
                    if (obj != null && obj != g)
                        obj.selected = false;
                }
            }
        }

        /// <summary>
        /// </summary>
        public void SelectAll()
        {
            CheckVirtualList();

            var last = -1;
            if (isVirtual)
            {
                var cnt = _realNumItems;
                for (var i = 0; i < cnt; i++)
                {
                    var ii = _virtualItems[i];
                    if (ii.obj is GButton && !((GButton)ii.obj).selected)
                    {
                        ((GButton)ii.obj).selected = true;
                        last = i;
                    }

                    ii.selected = true;
                }
            }
            else
            {
                var cnt = _children.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = _children[i].asButton;
                    if (obj != null && !obj.selected)
                    {
                        obj.selected = true;
                        last = i;
                    }
                }
            }

            if (last != -1)
                UpdateSelectionController(last);
        }

        /// <summary>
        /// </summary>
        public void SelectNone()
        {
            ClearSelection();
        }

        /// <summary>
        /// </summary>
        public void SelectReverse()
        {
            CheckVirtualList();

            var last = -1;
            if (isVirtual)
            {
                var cnt = _realNumItems;
                for (var i = 0; i < cnt; i++)
                {
                    var ii = _virtualItems[i];
                    if (ii.obj is GButton)
                    {
                        ((GButton)ii.obj).selected = !((GButton)ii.obj).selected;
                        if (((GButton)ii.obj).selected)
                            last = i;
                    }

                    ii.selected = !ii.selected;
                }
            }
            else
            {
                var cnt = _children.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = _children[i].asButton;
                    if (obj != null)
                    {
                        obj.selected = !obj.selected;
                        if (obj.selected)
                            last = i;
                    }
                }
            }

            if (last != -1)
                UpdateSelectionController(last);
        }

        /// <summary>
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableSelectionFocusEvents(bool enabled)
        {
            if ((_miscFlags & 2) != 0 == enabled)
                return;

            if (enabled)
            {
                _miscFlags |= 2;
                tabStopChildren = true;
                onFocusIn.Add(NotifySelection);
                onFocusOut.Add(NotifySelection);
            }
            else
            {
                _miscFlags &= 0xFD;
                onFocusIn.Remove(NotifySelection);
                onFocusOut.Remove(NotifySelection);
            }
        }

        private void NotifySelection(EventContext context)
        {
            var eventType = context.type == "onFocusIn" ? "onListFocusIn" : "onListFocusOut";
            var cnt = _children.Count;
            for (var i = 0; i < cnt; i++)
            {
                var obj = _children[i].asButton;
                if (obj != null && obj.selected)
                    obj.DispatchEvent(eventType);
            }
        }

        /// <summary>
        /// </summary>
        public void EnableArrowKeyNavigation(bool enabled)
        {
            if (enabled)
            {
                tabStopChildren = true;
                onKeyDown.Add(__keydown);
            }
            else
            {
                tabStopChildren = false;
                onKeyDown.Remove(__keydown);
            }
        }

        private void __keydown(EventContext context)
        {
            var index = -1;
            switch (context.inputEvent.keyCode)
            {
                case KeyCode.LeftArrow:
                    index = HandleArrowKey(7);
                    break;

                case KeyCode.RightArrow:
                    index = HandleArrowKey(3);
                    break;

                case KeyCode.UpArrow:
                    index = HandleArrowKey(1);
                    break;

                case KeyCode.DownArrow:
                    index = HandleArrowKey(5);
                    break;
            }

            if (index != -1)
            {
                index = ItemIndexToChildIndex(index);
                if (index != -1)
                    DispatchItemEvent(GetChildAt(index), context);

                context.StopPropagation();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="dir"></param>
        public int HandleArrowKey(int dir)
        {
            var curIndex = selectedIndex;
            if (curIndex == -1)
                return -1;

            var index = curIndex;
            switch (dir)
            {
                case 1: //up
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowVertical)
                    {
                        index--;
                    }
                    else if (_layout == ListLayoutType.FlowHorizontal || _layout == ListLayoutType.Pagination)
                    {
                        if (isVirtual)
                        {
                            index -= _curLineItemCount;
                        }
                        else
                        {
                            var current = _children[index];
                            var k = 0;
                            int i;
                            for (i = index - 1; i >= 0; i--)
                            {
                                var obj = _children[i];
                                if (obj.y != current.y)
                                {
                                    current = obj;
                                    break;
                                }

                                k++;
                            }

                            for (; i >= 0; i--)
                            {
                                var obj = _children[i];
                                if (obj.y != current.y)
                                {
                                    index = i + k + 1;
                                    break;
                                }
                            }
                        }
                    }

                    break;

                case 3: //right
                    if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowHorizontal ||
                        _layout == ListLayoutType.Pagination)
                    {
                        index++;
                    }
                    else if (_layout == ListLayoutType.FlowVertical)
                    {
                        if (isVirtual)
                        {
                            index += _curLineItemCount;
                        }
                        else
                        {
                            var current = _children[index];
                            var k = 0;
                            var cnt = _children.Count;
                            int i;
                            for (i = index + 1; i < cnt; i++)
                            {
                                var obj = _children[i];
                                if (obj.x != current.x)
                                {
                                    current = obj;
                                    break;
                                }

                                k++;
                            }

                            for (; i < cnt; i++)
                            {
                                var obj = _children[i];
                                if (obj.x != current.x)
                                {
                                    index = i - k - 1;
                                    break;
                                }
                            }
                        }
                    }

                    break;

                case 5: //down
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowVertical)
                    {
                        index++;
                    }
                    else if (_layout == ListLayoutType.FlowHorizontal || _layout == ListLayoutType.Pagination)
                    {
                        if (isVirtual)
                        {
                            index += _curLineItemCount;
                        }
                        else
                        {
                            var current = _children[index];
                            var k = 0;
                            var cnt = _children.Count;
                            int i;
                            for (i = index + 1; i < cnt; i++)
                            {
                                var obj = _children[i];
                                if (obj.y != current.y)
                                {
                                    current = obj;
                                    break;
                                }

                                k++;
                            }

                            for (; i < cnt; i++)
                            {
                                var obj = _children[i];
                                if (obj.y != current.y)
                                {
                                    index = i - k - 1;
                                    break;
                                }
                            }
                        }
                    }

                    break;

                case 7: //left
                    if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowHorizontal ||
                        _layout == ListLayoutType.Pagination)
                    {
                        index--;
                    }
                    else if (_layout == ListLayoutType.FlowVertical)
                    {
                        if (isVirtual)
                        {
                            index -= _curLineItemCount;
                        }
                        else
                        {
                            var current = _children[index];
                            var k = 0;
                            int i;
                            for (i = index - 1; i >= 0; i--)
                            {
                                var obj = _children[i];
                                if (obj.x != current.x)
                                {
                                    current = obj;
                                    break;
                                }

                                k++;
                            }

                            for (; i >= 0; i--)
                            {
                                var obj = _children[i];
                                if (obj.x != current.x)
                                {
                                    index = i + k + 1;
                                    break;
                                }
                            }
                        }
                    }

                    break;
            }

            if (index != curIndex && index >= 0 && index < numItems)
            {
                ClearSelection();
                AddSelection(index, true);
                return index;
            }

            return -1;
        }

        private void __clickItem(EventContext context)
        {
            var item = context.sender as GObject;
            if (item is GButton && selectionMode != ListSelectionMode.None)
                SetSelectionOnEvent(item, context.inputEvent);

            if (scrollPane != null && scrollItemToViewOnClick)
                scrollPane.ScrollToView(item, true);

            DispatchItemEvent(item, context);
        }

        protected virtual void DispatchItemEvent(GObject item, EventContext context)
        {
            if (context.type == item.onRightClick.type)
                DispatchEvent("onRightClickItem", item);
            else
                DispatchEvent("onClickItem", item);
        }

        private void SetSelectionOnEvent(GObject item, InputEvent evt)
        {
            var dontChangeLastIndex = false;
            var button = (GButton)item;
            var index = ChildIndexToItemIndex(GetChildIndex(item));

            if (selectionMode == ListSelectionMode.Single)
            {
                if (!button.selected)
                {
                    ClearSelectionExcept(button);
                    button.selected = true;
                }
            }
            else
            {
                if (evt.shift)
                {
                    if (!button.selected)
                    {
                        if (_lastSelectedIndex != -1)
                        {
                            var min = Math.Min(_lastSelectedIndex, index);
                            var max = Math.Max(_lastSelectedIndex, index);
                            max = Math.Min(max, numItems - 1);
                            if (isVirtual)
                                for (var i = min; i <= max; i++)
                                {
                                    var ii = _virtualItems[i];
                                    if (ii.obj is GButton)
                                        ((GButton)ii.obj).selected = true;
                                    ii.selected = true;
                                }
                            else
                                for (var i = min; i <= max; i++)
                                {
                                    var obj = GetChildAt(i).asButton;
                                    if (obj != null && !obj.selected)
                                        obj.selected = true;
                                }

                            dontChangeLastIndex = true;
                        }
                        else
                        {
                            button.selected = true;
                        }
                    }
                }
                else if (evt.ctrlOrCmd || selectionMode == ListSelectionMode.Multiple_SingleClick)
                {
                    button.selected = !button.selected;
                }
                else
                {
                    if (!button.selected)
                    {
                        ClearSelectionExcept(button);
                        button.selected = true;
                    }
                    else if (evt.button == 0)
                    {
                        ClearSelectionExcept(button);
                    }
                }
            }

            if (!dontChangeLastIndex)
                _lastSelectedIndex = index;

            if (button.selected)
                UpdateSelectionController(index);
        }

        /// <summary>
        ///     Resize to list size to fit specified item count.
        ///     If list layout is single column or flow horizontally, the height will change to fit.
        ///     If list layout is single row or flow vertically, the width will change to fit.
        /// </summary>
        public void ResizeToFit()
        {
            ResizeToFit(int.MaxValue, 0);
        }

        /// <summary>
        ///     Resize to list size to fit specified item count.
        ///     If list layout is single column or flow horizontally, the height will change to fit.
        ///     If list layout is single row or flow vertically, the width will change to fit.
        /// </summary>
        /// <param name="itemCount">Item count</param>
        public void ResizeToFit(int itemCount)
        {
            ResizeToFit(itemCount, 0);
        }

        /// <summary>
        ///     Resize to list size to fit specified item count.
        ///     If list layout is single column or flow horizontally, the height will change to fit.
        ///     If list layout is single row or flow vertically, the width will change to fit.
        /// </summary>
        /// <param name="itemCount">>Item count</param>
        /// <param name="minSize">If the result size if smaller than minSize, then use minSize.</param>
        public void ResizeToFit(int itemCount, int minSize)
        {
            EnsureBoundsCorrect();

            var curCount = numItems;
            if (itemCount > curCount)
                itemCount = curCount;

            if (isVirtual)
            {
                var lineCount = Mathf.CeilToInt((float)itemCount / _curLineItemCount);
                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                    viewHeight = lineCount * _itemSize.y + Math.Max(0, lineCount - 1) * _lineGap;
                else
                    viewWidth = lineCount * _itemSize.x + Math.Max(0, lineCount - 1) * _columnGap;
            }
            else if (itemCount == 0)
            {
                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                    viewHeight = minSize;
                else
                    viewWidth = minSize;
            }
            else
            {
                var i = itemCount - 1;
                GObject obj = null;
                while (i >= 0)
                {
                    obj = GetChildAt(i);
                    if (!foldInvisibleItems || obj.visible)
                        break;
                    i--;
                }

                if (i < 0)
                {
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                        viewHeight = minSize;
                    else
                        viewWidth = minSize;
                }
                else
                {
                    float size;
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                    {
                        size = obj.y + obj.height;
                        if (size < minSize)
                            size = minSize;
                        viewHeight = size;
                    }
                    else
                    {
                        size = obj.x + obj.width;
                        if (size < minSize)
                            size = minSize;
                        viewWidth = size;
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        protected override void HandleSizeChanged()
        {
            base.HandleSizeChanged();

            SetBoundsChangedFlag();
            if (isVirtual)
                SetVirtualListChangedFlag(true);
        }

        public override void HandleControllerChanged(Controller c)
        {
            base.HandleControllerChanged(c);

            if (selectionController == c)
                selectedIndex = c.selectedIndex;
        }

        private void UpdateSelectionController(int index)
        {
            if (selectionController != null && !selectionController.changing
                                            && index < selectionController.pageCount)
            {
                var c = selectionController;
                selectionController = null;
                c.selectedIndex = index;
                selectionController = c;
            }
        }

        /// <summary>
        ///     Scroll the list to make an item with certain index visible.
        /// </summary>
        /// <param name="index">Item index</param>
        public void ScrollToView(int index)
        {
            ScrollToView(index, false);
        }

        /// <summary>
        ///     Scroll the list to make an item with certain index visible.
        /// </summary>
        /// <param name="index">Item index</param>
        /// <param name="ani">True to scroll smoothly, othewise immdediately.</param>
        public void ScrollToView(int index, bool ani)
        {
            ScrollToView(index, ani, false);
        }

        /// <summary>
        ///     Scroll the list to make an item with certain index visible.
        /// </summary>
        /// <param name="index">Item index</param>
        /// <param name="ani">True to scroll smoothly, othewise immdediately.</param>
        /// <param name="setFirst">
        ///     If true, scroll to make the target on the top/left; If false, scroll to make the target any
        ///     position in view.
        /// </param>
        public void ScrollToView(int index, bool ani, bool setFirst)
        {
            if (isVirtual)
            {
                if (_numItems == 0)
                    return;

                CheckVirtualList();

                if (index >= _virtualItems.Count)
                    throw new Exception("Invalid child index: " + index + ">" + _virtualItems.Count);

                if (_loop)
                    index = Mathf.FloorToInt((float)_firstIndex / _numItems) * _numItems + index;

                Rect rect;
                var ii = _virtualItems[index];
                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                {
                    float pos = 0;
                    for (var i = _curLineItemCount - 1; i < index; i += _curLineItemCount)
                        pos += _virtualItems[i].size.y + _lineGap;
                    rect = new Rect(0, pos, _itemSize.x, ii.size.y);
                }
                else if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowVertical)
                {
                    float pos = 0;
                    for (var i = _curLineItemCount - 1; i < index; i += _curLineItemCount)
                        pos += _virtualItems[i].size.x + _columnGap;
                    rect = new Rect(pos, 0, ii.size.x, _itemSize.y);
                }
                else
                {
                    var page = index / (_curLineItemCount * _curLineItemCount2);
                    rect = new Rect(page * viewWidth + index % _curLineItemCount * (ii.size.x + _columnGap),
                        index / _curLineItemCount % _curLineItemCount2 * (ii.size.y + _lineGap),
                        ii.size.x, ii.size.y);
                }

                if (scrollPane != null)
                    scrollPane.ScrollToView(rect, ani, setFirst);
                else if (parent != null && parent.scrollPane != null)
                    parent.scrollPane.ScrollToView(TransformRect(rect, parent), ani, setFirst);
            }
            else
            {
                var obj = GetChildAt(index);
                if (scrollPane != null)
                    scrollPane.ScrollToView(obj, ani, setFirst);
                else if (parent != null && parent.scrollPane != null)
                    parent.scrollPane.ScrollToView(obj, ani, setFirst);
            }
        }

        /// <summary>
        ///     Get first child in view.
        /// </summary>
        /// <returns></returns>
        public override int GetFirstChildInView()
        {
            return ChildIndexToItemIndex(base.GetFirstChildInView());
        }

        public int ChildIndexToItemIndex(int index)
        {
            if (!isVirtual)
                return index;

            if (_layout == ListLayoutType.Pagination)
            {
                for (var i = _firstIndex; i < _realNumItems; i++)
                    if (_virtualItems[i].obj != null)
                    {
                        index--;
                        if (index < 0)
                            return i;
                    }

                return index;
            }

            index += _firstIndex;
            if (_loop && _numItems > 0)
                index = index % _numItems;

            return index;
        }

        public int ItemIndexToChildIndex(int index)
        {
            if (!isVirtual)
                return index;

            if (_layout == ListLayoutType.Pagination) return GetChildIndex(_virtualItems[index].obj);

            if (_loop && _numItems > 0)
            {
                var j = _firstIndex % _numItems;
                if (index >= j)
                    index = index - j;
                else
                    index = _numItems - j + index;
            }
            else
            {
                index -= _firstIndex;
            }

            return index;
        }


        /// <summary>
        ///     Set the list to be virtual list.
        ///     设置列表为虚拟列表模式。在虚拟列表模式下，列表不会为每一条列表数据创建一个实体对象，而是根据视口大小创建最小量的显示对象，然后通过itemRenderer指定的回调函数设置列表数据。
        ///     在虚拟模式下，你不能通过AddChild、RemoveChild等方式管理列表，只能通过设置numItems设置列表数据的长度。
        ///     如果要刷新列表，可以通过重新设置numItems，或者调用RefreshVirtualList完成。
        ///     ‘单行’或者‘单列’的列表布局可支持不等高的列表项目。
        ///     除了‘页面’的列表布局，其他布局均支持使用不同资源构建列表项目，你可以在itemProvider里返回。如果不提供，默认使用defaultItem。
        /// </summary>
        public void SetVirtual()
        {
            SetVirtual(false);
        }

        /// <summary>
        ///     Set the list to be virtual list, and has loop behavior.
        /// </summary>
        public void SetVirtualAndLoop()
        {
            SetVirtual(true);
        }

        private void SetVirtual(bool loop)
        {
            if (!isVirtual)
            {
                if (scrollPane == null)
                    Debug.LogError("FairyGUI: Virtual list must be scrollable!");

                if (loop)
                {
                    if (_layout == ListLayoutType.FlowHorizontal || _layout == ListLayoutType.FlowVertical)
                        Debug.LogError(
                            "FairyGUI: Loop list is not supported for FlowHorizontal or FlowVertical layout!");

                    scrollPane.bouncebackEffect = false;
                }

                isVirtual = true;
                _loop = loop;
                _virtualItems = new List<ItemInfo>();
                RemoveChildrenToPool();

                if (_itemSize.x == 0 || _itemSize.y == 0)
                {
                    var obj = GetFromPool(null);
                    if (obj == null)
                    {
                        Debug.LogError("FairyGUI: Virtual List must have a default list item resource.");
                        _itemSize = new Vector2(100, 100);
                    }
                    else
                    {
                        _itemSize = obj.size;
                        _itemSize.x = Mathf.CeilToInt(_itemSize.x);
                        _itemSize.y = Mathf.CeilToInt(_itemSize.y);
                        ReturnToPool(obj);
                    }
                }

                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                {
                    scrollPane.scrollStep = _itemSize.y;
                    if (_loop)
                        scrollPane._loop = 2;
                }
                else
                {
                    scrollPane.scrollStep = _itemSize.x;
                    if (_loop)
                        scrollPane._loop = 1;
                }

                scrollPane.onScroll.AddCapture(__scrolled);
                SetVirtualListChangedFlag(true);
            }
        }

        public void RefreshVirtualList()
        {
            if (!isVirtual)
                throw new Exception("FairyGUI: not virtual list");

            SetVirtualListChangedFlag(false);
        }

        private void CheckVirtualList()
        {
            if (_virtualListChanged != 0)
            {
                RefreshVirtualList(null);
                Timers.inst.Remove(RefreshVirtualList);
            }
        }

        private void SetVirtualListChangedFlag(bool layoutChanged)
        {
            if (layoutChanged)
                _virtualListChanged = 2;
            else if (_virtualListChanged == 0)
                _virtualListChanged = 1;

            Timers.inst.CallLater(RefreshVirtualList);
        }

        private void RefreshVirtualList(object param)
        {
            var layoutChanged = _virtualListChanged == 2;
            _virtualListChanged = 0;
            _miscFlags |= 1;

            if (layoutChanged)
            {
                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.SingleRow)
                {
                    _curLineItemCount = 1;
                }
                else if (_layout == ListLayoutType.FlowHorizontal)
                {
                    if (_columnCount > 0)
                    {
                        _curLineItemCount = _columnCount;
                    }
                    else
                    {
                        _curLineItemCount =
                            Mathf.FloorToInt((scrollPane.viewWidth + _columnGap) / (_itemSize.x + _columnGap));
                        if (_curLineItemCount <= 0)
                            _curLineItemCount = 1;
                    }
                }
                else if (_layout == ListLayoutType.FlowVertical)
                {
                    if (_lineCount > 0)
                    {
                        _curLineItemCount = _lineCount;
                    }
                    else
                    {
                        _curLineItemCount =
                            Mathf.FloorToInt((scrollPane.viewHeight + _lineGap) / (_itemSize.y + _lineGap));
                        if (_curLineItemCount <= 0)
                            _curLineItemCount = 1;
                    }
                }
                else //pagination
                {
                    if (_columnCount > 0)
                    {
                        _curLineItemCount = _columnCount;
                    }
                    else
                    {
                        _curLineItemCount =
                            Mathf.FloorToInt((scrollPane.viewWidth + _columnGap) / (_itemSize.x + _columnGap));
                        if (_curLineItemCount <= 0)
                            _curLineItemCount = 1;
                    }

                    if (_lineCount > 0)
                    {
                        _curLineItemCount2 = _lineCount;
                    }
                    else
                    {
                        _curLineItemCount2 =
                            Mathf.FloorToInt((scrollPane.viewHeight + _lineGap) / (_itemSize.y + _lineGap));
                        if (_curLineItemCount2 <= 0)
                            _curLineItemCount2 = 1;
                    }
                }
            }

            float ch = 0, cw = 0;
            if (_realNumItems > 0)
            {
                var len = Mathf.CeilToInt((float)_realNumItems / _curLineItemCount) * _curLineItemCount;
                var len2 = Math.Min(_curLineItemCount, _realNumItems);
                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                {
                    for (var i = 0; i < len; i += _curLineItemCount)
                        ch += _virtualItems[i].size.y + _lineGap;
                    if (ch > 0)
                        ch -= _lineGap;

                    if (_autoResizeItem)
                    {
                        cw = scrollPane.viewWidth;
                    }
                    else
                    {
                        for (var i = 0; i < len2; i++)
                            cw += _virtualItems[i].size.x + _columnGap;
                        if (cw > 0)
                            cw -= _columnGap;
                    }
                }
                else if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowVertical)
                {
                    for (var i = 0; i < len; i += _curLineItemCount)
                        cw += _virtualItems[i].size.x + _columnGap;
                    if (cw > 0)
                        cw -= _columnGap;

                    if (_autoResizeItem)
                    {
                        ch = scrollPane.viewHeight;
                    }
                    else
                    {
                        for (var i = 0; i < len2; i++)
                            ch += _virtualItems[i].size.y + _lineGap;
                        if (ch > 0)
                            ch -= _lineGap;
                    }
                }
                else
                {
                    var pageCount = Mathf.CeilToInt((float)len / (_curLineItemCount * _curLineItemCount2));
                    cw = pageCount * viewWidth;
                    ch = viewHeight;
                }
            }

            HandleAlign(cw, ch);
            scrollPane.SetContentSize(cw, ch);

            _miscFlags &= 0xFE;

            HandleScroll(true);
        }

        private void __scrolled(EventContext context)
        {
            HandleScroll(false);
        }

        private int GetIndexOnPos1(ref float pos, bool forceUpdate)
        {
            if (_realNumItems < _curLineItemCount)
            {
                pos = 0;
                return 0;
            }

            if (numChildren > 0 && !forceUpdate)
            {
                var pos2 = GetChildAt(0).y;
                if (pos2 + (_lineGap > 0 ? 0 : -_lineGap) > pos)
                {
                    for (var i = _firstIndex - _curLineItemCount; i >= 0; i -= _curLineItemCount)
                    {
                        pos2 -= _virtualItems[i].size.y + _lineGap;
                        if (pos2 <= pos)
                        {
                            pos = pos2;
                            return i;
                        }
                    }

                    pos = 0;
                    return 0;
                }

                float testGap = _lineGap > 0 ? _lineGap : 0;
                for (var i = _firstIndex; i < _realNumItems; i += _curLineItemCount)
                {
                    var pos3 = pos2 + _virtualItems[i].size.y;
                    if (pos3 + testGap > pos)
                    {
                        pos = pos2;
                        return i;
                    }

                    pos2 = pos3 + _lineGap;
                }

                pos = pos2;
                return _realNumItems - _curLineItemCount;
            }
            else
            {
                float pos2 = 0;
                float testGap = _lineGap > 0 ? _lineGap : 0;
                for (var i = 0; i < _realNumItems; i += _curLineItemCount)
                {
                    var pos3 = pos2 + _virtualItems[i].size.y;
                    if (pos3 + testGap > pos)
                    {
                        pos = pos2;
                        return i;
                    }

                    pos2 = pos3 + _lineGap;
                }

                pos = pos2;
                return _realNumItems - _curLineItemCount;
            }
        }

        private int GetIndexOnPos2(ref float pos, bool forceUpdate)
        {
            if (_realNumItems < _curLineItemCount)
            {
                pos = 0;
                return 0;
            }

            if (numChildren > 0 && !forceUpdate)
            {
                var pos2 = GetChildAt(0).x;
                if (pos2 + (_columnGap > 0 ? 0 : -_columnGap) > pos)
                {
                    for (var i = _firstIndex - _curLineItemCount; i >= 0; i -= _curLineItemCount)
                    {
                        pos2 -= _virtualItems[i].size.x + _columnGap;
                        if (pos2 <= pos)
                        {
                            pos = pos2;
                            return i;
                        }
                    }

                    pos = 0;
                    return 0;
                }

                float testGap = _columnGap > 0 ? _columnGap : 0;
                for (var i = _firstIndex; i < _realNumItems; i += _curLineItemCount)
                {
                    var pos3 = pos2 + _virtualItems[i].size.x;
                    if (pos3 + testGap > pos)
                    {
                        pos = pos2;
                        return i;
                    }

                    pos2 = pos3 + _columnGap;
                }

                pos = pos2;
                return _realNumItems - _curLineItemCount;
            }
            else
            {
                float pos2 = 0;
                float testGap = _columnGap > 0 ? _columnGap : 0;
                for (var i = 0; i < _realNumItems; i += _curLineItemCount)
                {
                    var pos3 = pos2 + _virtualItems[i].size.x;
                    if (pos3 + testGap > pos)
                    {
                        pos = pos2;
                        return i;
                    }

                    pos2 = pos3 + _columnGap;
                }

                pos = pos2;
                return _realNumItems - _curLineItemCount;
            }
        }

        private int GetIndexOnPos3(ref float pos, bool forceUpdate)
        {
            if (_realNumItems < _curLineItemCount)
            {
                pos = 0;
                return 0;
            }

            var viewWidth = this.viewWidth;
            var page = Mathf.FloorToInt(pos / viewWidth);
            var startIndex = page * _curLineItemCount * _curLineItemCount2;
            var pos2 = page * viewWidth;
            float testGap = _columnGap > 0 ? _columnGap : 0;
            for (var i = 0; i < _curLineItemCount; i++)
            {
                var pos3 = pos2 + _virtualItems[startIndex + i].size.x;
                if (pos3 + testGap > pos)
                {
                    pos = pos2;
                    return startIndex + i;
                }

                pos2 = pos3 + _columnGap;
            }

            pos = pos2;
            return startIndex + _curLineItemCount - 1;
        }

        private void HandleScroll(bool forceUpdate)
        {
            if ((_miscFlags & 1) != 0)
                return;

            if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
            {
                var enterCounter = 0;
                while (HandleScroll1(forceUpdate))
                {
                    //可能会因为ITEM资源改变导致ITEM大小发生改变，所有出现最后一页填不满的情况，这时要反复尝试填满。
                    enterCounter++;
                    forceUpdate = false;
                    if (enterCounter > 20)
                    {
                        Debug.Log(
                            "FairyGUI: list will never be filled as the item renderer function always returns a different size.");
                        break;
                    }
                }

                HandleArchOrder1();
            }
            else if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowVertical)
            {
                var enterCounter = 0;
                while (HandleScroll2(forceUpdate))
                {
                    enterCounter++;
                    forceUpdate = false;
                    if (enterCounter > 20)
                    {
                        Debug.Log(
                            "FairyGUI: list will never be filled as the item renderer function always returns a different size.");
                        break;
                    }
                }

                HandleArchOrder2();
            }
            else
            {
                HandleScroll3(forceUpdate);
            }

            _boundsChanged = false;
        }

        private bool HandleScroll1(bool forceUpdate)
        {
            var pos = scrollPane.scrollingPosY;
            var max = pos + scrollPane.viewHeight;
            var end = max == scrollPane.contentHeight; //这个标志表示当前需要滚动到最末，无论内容变化大小

            //寻找当前位置的第一条项目
            var newFirstIndex = GetIndexOnPos1(ref pos, forceUpdate);
            if (newFirstIndex == _firstIndex && !forceUpdate)
                return false;

            var oldFirstIndex = _firstIndex;
            _firstIndex = newFirstIndex;
            var curIndex = newFirstIndex;
            var forward = oldFirstIndex > newFirstIndex;
            var childCount = numChildren;
            var lastIndex = oldFirstIndex + childCount - 1;
            var reuseIndex = forward ? lastIndex : oldFirstIndex;
            float curX = 0, curY = pos;
            bool needRender;
            float deltaSize = 0;
            float firstItemDeltaSize = 0;
            var url = _defaultItem;
            var partSize = (int)((scrollPane.viewWidth - _columnGap * (_curLineItemCount - 1)) / _curLineItemCount);

            itemInfoVer++;
            while (curIndex < _realNumItems && (end || curY < max))
            {
                var ii = _virtualItems[curIndex];

                if (ii.obj == null || forceUpdate)
                {
                    if (itemProvider != null)
                    {
                        url = itemProvider(curIndex % _numItems);
                        if (url == null)
                            url = _defaultItem;
                        url = UIPackage.NormalizeURL(url);
                    }

                    if (ii.obj != null && ii.obj.resourceURL != url)
                    {
                        if (ii.obj is GButton)
                            ii.selected = ((GButton)ii.obj).selected;
                        RemoveChildToPool(ii.obj);
                        ii.obj = null;
                    }
                }

                if (ii.obj == null)
                {
                    //搜索最适合的重用item，保证每次刷新需要新建或者重新render的item最少
                    if (forward)
                        for (var j = reuseIndex; j >= oldFirstIndex; j--)
                        {
                            var ii2 = _virtualItems[j];
                            if (ii2.obj != null && ii2.updateFlag != itemInfoVer && ii2.obj.resourceURL == url)
                            {
                                if (ii2.obj is GButton)
                                    ii2.selected = ((GButton)ii2.obj).selected;
                                ii.obj = ii2.obj;
                                ii2.obj = null;
                                if (j == reuseIndex)
                                    reuseIndex--;
                                break;
                            }
                        }
                    else
                        for (var j = reuseIndex; j <= lastIndex; j++)
                        {
                            var ii2 = _virtualItems[j];
                            if (ii2.obj != null && ii2.updateFlag != itemInfoVer && ii2.obj.resourceURL == url)
                            {
                                if (ii2.obj is GButton)
                                    ii2.selected = ((GButton)ii2.obj).selected;
                                ii.obj = ii2.obj;
                                ii2.obj = null;
                                if (j == reuseIndex)
                                    reuseIndex++;
                                break;
                            }
                        }

                    if (ii.obj != null)
                    {
                        SetChildIndex(ii.obj, forward ? curIndex - newFirstIndex : numChildren);
                    }
                    else
                    {
                        ii.obj = itemPool.GetObject(url);
                        if (forward)
                            AddChildAt(ii.obj, curIndex - newFirstIndex);
                        else
                            AddChild(ii.obj);
                    }

                    if (ii.obj is GButton)
                        ((GButton)ii.obj).selected = ii.selected;

                    needRender = true;
                }
                else
                {
                    needRender = forceUpdate;
                }

                if (needRender)
                {
                    if (_autoResizeItem && (_layout == ListLayoutType.SingleColumn || _columnCount > 0))
                        ii.obj.SetSize(partSize, ii.obj.height, true);

                    itemRenderer(curIndex % _numItems, ii.obj);
                    if (curIndex % _curLineItemCount == 0)
                    {
                        deltaSize += Mathf.CeilToInt(ii.obj.size.y) - ii.size.y;
                        if (curIndex == newFirstIndex && oldFirstIndex > newFirstIndex)
                            //当内容向下滚动时，如果新出现的项目大小发生变化，需要做一个位置补偿，才不会导致滚动跳动
                            firstItemDeltaSize = Mathf.CeilToInt(ii.obj.size.y) - ii.size.y;
                    }

                    ii.size.x = Mathf.CeilToInt(ii.obj.size.x);
                    ii.size.y = Mathf.CeilToInt(ii.obj.size.y);
                }

                ii.updateFlag = itemInfoVer;
                ii.obj.SetXY(curX, curY);
                if (curIndex == newFirstIndex) //要显示多一条才不会穿帮
                    max += ii.size.y;

                curX += ii.size.x + _columnGap;

                if (curIndex % _curLineItemCount == _curLineItemCount - 1)
                {
                    curX = 0;
                    curY += ii.size.y + _lineGap;
                }

                curIndex++;
            }

            for (var i = 0; i < childCount; i++)
            {
                var ii = _virtualItems[oldFirstIndex + i];
                if (ii.updateFlag != itemInfoVer && ii.obj != null)
                {
                    if (ii.obj is GButton)
                        ii.selected = ((GButton)ii.obj).selected;
                    RemoveChildToPool(ii.obj);
                    ii.obj = null;
                }
            }

            childCount = _children.Count;
            for (var i = 0; i < childCount; i++)
            {
                var obj = _virtualItems[newFirstIndex + i].obj;
                if (_children[i] != obj)
                    SetChildIndex(obj, i);
            }

            if (deltaSize != 0 || firstItemDeltaSize != 0)
                scrollPane.ChangeContentSizeOnScrolling(0, deltaSize, 0, firstItemDeltaSize);

            if (curIndex > 0 && numChildren > 0 && container.y <= 0 && GetChildAt(0).y > -container.y) //最后一页没填满！
                return true;
            return false;
        }

        private bool HandleScroll2(bool forceUpdate)
        {
            var pos = scrollPane.scrollingPosX;
            var max = pos + scrollPane.viewWidth;
            var end = pos == scrollPane.contentWidth; //这个标志表示当前需要滚动到最末，无论内容变化大小

            //寻找当前位置的第一条项目
            var newFirstIndex = GetIndexOnPos2(ref pos, forceUpdate);
            if (newFirstIndex == _firstIndex && !forceUpdate)
                return false;

            var oldFirstIndex = _firstIndex;
            _firstIndex = newFirstIndex;
            var curIndex = newFirstIndex;
            var forward = oldFirstIndex > newFirstIndex;
            var childCount = numChildren;
            var lastIndex = oldFirstIndex + childCount - 1;
            var reuseIndex = forward ? lastIndex : oldFirstIndex;
            float curX = pos, curY = 0;
            bool needRender;
            float deltaSize = 0;
            float firstItemDeltaSize = 0;
            var url = _defaultItem;
            var partSize = (int)((scrollPane.viewHeight - _lineGap * (_curLineItemCount - 1)) / _curLineItemCount);

            itemInfoVer++;
            while (curIndex < _realNumItems && (end || curX < max))
            {
                var ii = _virtualItems[curIndex];

                if (ii.obj == null || forceUpdate)
                {
                    if (itemProvider != null)
                    {
                        url = itemProvider(curIndex % _numItems);
                        if (url == null)
                            url = _defaultItem;
                        url = UIPackage.NormalizeURL(url);
                    }

                    if (ii.obj != null && ii.obj.resourceURL != url)
                    {
                        if (ii.obj is GButton)
                            ii.selected = ((GButton)ii.obj).selected;
                        RemoveChildToPool(ii.obj);
                        ii.obj = null;
                    }
                }

                if (ii.obj == null)
                {
                    if (forward)
                        for (var j = reuseIndex; j >= oldFirstIndex; j--)
                        {
                            var ii2 = _virtualItems[j];
                            if (ii2.obj != null && ii2.updateFlag != itemInfoVer && ii2.obj.resourceURL == url)
                            {
                                if (ii2.obj is GButton)
                                    ii2.selected = ((GButton)ii2.obj).selected;
                                ii.obj = ii2.obj;
                                ii2.obj = null;
                                if (j == reuseIndex)
                                    reuseIndex--;
                                break;
                            }
                        }
                    else
                        for (var j = reuseIndex; j <= lastIndex; j++)
                        {
                            var ii2 = _virtualItems[j];
                            if (ii2.obj != null && ii2.updateFlag != itemInfoVer && ii2.obj.resourceURL == url)
                            {
                                if (ii2.obj is GButton)
                                    ii2.selected = ((GButton)ii2.obj).selected;
                                ii.obj = ii2.obj;
                                ii2.obj = null;
                                if (j == reuseIndex)
                                    reuseIndex++;
                                break;
                            }
                        }

                    if (ii.obj != null)
                    {
                        SetChildIndex(ii.obj, forward ? curIndex - newFirstIndex : numChildren);
                    }
                    else
                    {
                        ii.obj = itemPool.GetObject(url);
                        if (forward)
                            AddChildAt(ii.obj, curIndex - newFirstIndex);
                        else
                            AddChild(ii.obj);
                    }

                    if (ii.obj is GButton)
                        ((GButton)ii.obj).selected = ii.selected;

                    needRender = true;
                }
                else
                {
                    needRender = forceUpdate;
                }

                if (needRender)
                {
                    if (_autoResizeItem && (_layout == ListLayoutType.SingleRow || _lineCount > 0))
                        ii.obj.SetSize(ii.obj.width, partSize, true);

                    itemRenderer(curIndex % _numItems, ii.obj);
                    if (curIndex % _curLineItemCount == 0)
                    {
                        deltaSize += Mathf.CeilToInt(ii.obj.size.x) - ii.size.x;
                        if (curIndex == newFirstIndex && oldFirstIndex > newFirstIndex)
                            //当内容向下滚动时，如果新出现的一个项目大小发生变化，需要做一个位置补偿，才不会导致滚动跳动
                            firstItemDeltaSize = Mathf.CeilToInt(ii.obj.size.x) - ii.size.x;
                    }

                    ii.size.x = Mathf.CeilToInt(ii.obj.size.x);
                    ii.size.y = Mathf.CeilToInt(ii.obj.size.y);
                }

                ii.updateFlag = itemInfoVer;
                ii.obj.SetXY(curX, curY);
                if (curIndex == newFirstIndex) //要显示多一条才不会穿帮
                    max += ii.size.x;

                curY += ii.size.y + _lineGap;

                if (curIndex % _curLineItemCount == _curLineItemCount - 1)
                {
                    curY = 0;
                    curX += ii.size.x + _columnGap;
                }

                curIndex++;
            }

            for (var i = 0; i < childCount; i++)
            {
                var ii = _virtualItems[oldFirstIndex + i];
                if (ii.updateFlag != itemInfoVer && ii.obj != null)
                {
                    if (ii.obj is GButton)
                        ii.selected = ((GButton)ii.obj).selected;
                    RemoveChildToPool(ii.obj);
                    ii.obj = null;
                }
            }

            childCount = _children.Count;
            for (var i = 0; i < childCount; i++)
            {
                var obj = _virtualItems[newFirstIndex + i].obj;
                if (_children[i] != obj)
                    SetChildIndex(obj, i);
            }

            if (deltaSize != 0 || firstItemDeltaSize != 0)
                scrollPane.ChangeContentSizeOnScrolling(deltaSize, 0, firstItemDeltaSize, 0);

            if (curIndex > 0 && numChildren > 0 && container.x <= 0 && GetChildAt(0).x > -container.x) //最后一页没填满！
                return true;
            return false;
        }

        private void HandleScroll3(bool forceUpdate)
        {
            var pos = scrollPane.scrollingPosX;

            //寻找当前位置的第一条项目
            var newFirstIndex = GetIndexOnPos3(ref pos, forceUpdate);
            if (newFirstIndex == _firstIndex && !forceUpdate)
                return;

            var oldFirstIndex = _firstIndex;
            _firstIndex = newFirstIndex;

            //分页模式不支持不等高，所以渲染满一页就好了

            var reuseIndex = oldFirstIndex;
            var virtualItemCount = _virtualItems.Count;
            var pageSize = _curLineItemCount * _curLineItemCount2;
            var startCol = newFirstIndex % _curLineItemCount;
            var viewWidth = this.viewWidth;
            var page = newFirstIndex / pageSize;
            var startIndex = page * pageSize;
            var lastIndex = startIndex + pageSize * 2; //测试两页
            bool needRender;
            var url = _defaultItem;
            var partWidth = (int)((scrollPane.viewWidth - _columnGap * (_curLineItemCount - 1)) / _curLineItemCount);
            var partHeight = (int)((scrollPane.viewHeight - _lineGap * (_curLineItemCount2 - 1)) / _curLineItemCount2);
            itemInfoVer++;

            //先标记这次要用到的项目
            for (var i = startIndex; i < lastIndex; i++)
            {
                if (i >= _realNumItems)
                    continue;

                var col = i % _curLineItemCount;
                if (i - startIndex < pageSize)
                {
                    if (col < startCol)
                        continue;
                }
                else
                {
                    if (col > startCol)
                        continue;
                }

                var ii = _virtualItems[i];
                ii.updateFlag = itemInfoVer;
            }

            GObject lastObj = null;
            var insertIndex = 0;
            for (var i = startIndex; i < lastIndex; i++)
            {
                if (i >= _realNumItems)
                    continue;

                var ii = _virtualItems[i];
                if (ii.updateFlag != itemInfoVer)
                    continue;

                if (ii.obj == null)
                {
                    //寻找看有没有可重用的
                    while (reuseIndex < virtualItemCount)
                    {
                        var ii2 = _virtualItems[reuseIndex];
                        if (ii2.obj != null && ii2.updateFlag != itemInfoVer)
                        {
                            if (ii2.obj is GButton)
                                ii2.selected = ((GButton)ii2.obj).selected;
                            ii.obj = ii2.obj;
                            ii2.obj = null;
                            break;
                        }

                        reuseIndex++;
                    }

                    if (insertIndex == -1)
                        insertIndex = GetChildIndex(lastObj) + 1;

                    if (ii.obj == null)
                    {
                        if (itemProvider != null)
                        {
                            url = itemProvider(i % _numItems);
                            if (url == null)
                                url = _defaultItem;
                            url = UIPackage.NormalizeURL(url);
                        }

                        ii.obj = itemPool.GetObject(url);
                        AddChildAt(ii.obj, insertIndex);
                    }
                    else
                    {
                        insertIndex = SetChildIndexBefore(ii.obj, insertIndex);
                    }

                    insertIndex++;

                    if (ii.obj is GButton)
                        ((GButton)ii.obj).selected = ii.selected;

                    needRender = true;
                }
                else
                {
                    needRender = forceUpdate;
                    insertIndex = -1;
                    lastObj = ii.obj;
                }

                if (needRender)
                {
                    if (_autoResizeItem)
                    {
                        if (_curLineItemCount == _columnCount && _curLineItemCount2 == _lineCount)
                            ii.obj.SetSize(partWidth, partHeight, true);
                        else if (_curLineItemCount == _columnCount)
                            ii.obj.SetSize(partWidth, ii.obj.height, true);
                        else if (_curLineItemCount2 == _lineCount)
                            ii.obj.SetSize(ii.obj.width, partHeight, true);
                    }

                    itemRenderer(i % _numItems, ii.obj);
                    ii.size.x = Mathf.CeilToInt(ii.obj.size.x);
                    ii.size.y = Mathf.CeilToInt(ii.obj.size.y);
                }
            }

            //排列item
            var borderX = startIndex / pageSize * viewWidth;
            var xx = borderX;
            float yy = 0;
            float lineHeight = 0;
            for (var i = startIndex; i < lastIndex; i++)
            {
                if (i >= _realNumItems)
                    continue;

                var ii = _virtualItems[i];
                if (ii.updateFlag == itemInfoVer)
                    ii.obj.SetXY(xx, yy);

                if (ii.size.y > lineHeight)
                    lineHeight = ii.size.y;
                if (i % _curLineItemCount == _curLineItemCount - 1)
                {
                    xx = borderX;
                    yy += lineHeight + _lineGap;
                    lineHeight = 0;

                    if (i == startIndex + pageSize - 1)
                    {
                        borderX += viewWidth;
                        xx = borderX;
                        yy = 0;
                    }
                }
                else
                {
                    xx += ii.size.x + _columnGap;
                }
            }

            //释放未使用的
            for (var i = reuseIndex; i < virtualItemCount; i++)
            {
                var ii = _virtualItems[i];
                if (ii.updateFlag != itemInfoVer && ii.obj != null)
                {
                    if (ii.obj is GButton)
                        ii.selected = ((GButton)ii.obj).selected;
                    RemoveChildToPool(ii.obj);
                    ii.obj = null;
                }
            }
        }

        private void HandleArchOrder1()
        {
            if (childrenRenderOrder == ChildrenRenderOrder.Arch)
            {
                var mid = scrollPane.posY + viewHeight / 2;
                float minDist = int.MaxValue, dist;
                var apexIndex = 0;
                var cnt = numChildren;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = GetChildAt(i);
                    if (!foldInvisibleItems || obj.visible)
                    {
                        dist = Mathf.Abs(mid - obj.y - obj.height / 2);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            apexIndex = i;
                        }
                    }
                }

                this.apexIndex = apexIndex;
            }
        }

        private void HandleArchOrder2()
        {
            if (childrenRenderOrder == ChildrenRenderOrder.Arch)
            {
                var mid = scrollPane.posX + viewWidth / 2;
                float minDist = int.MaxValue, dist;
                var apexIndex = 0;
                var cnt = numChildren;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = GetChildAt(i);
                    if (!foldInvisibleItems || obj.visible)
                    {
                        dist = Mathf.Abs(mid - obj.x - obj.width / 2);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            apexIndex = i;
                        }
                    }
                }

                this.apexIndex = apexIndex;
            }
        }

        public override void GetSnappingPositionWithDir(ref float xValue, ref float yValue, float xDir, float yDir)
        {
            if (isVirtual)
            {
                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                {
                    var saved = yValue;
                    var index = GetIndexOnPos1(ref yValue, false);
                    if (index < _virtualItems.Count && index < _realNumItems)
                    {
                        var size = _virtualItems[index].size.y;
                        if (ShouldSnapToNext(yDir, saved - yValue, size))
                            yValue += size + _lineGap;
                    }
                }
                else if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowVertical)
                {
                    var saved = xValue;
                    var index = GetIndexOnPos2(ref xValue, false);
                    if (index < _virtualItems.Count && index < _realNumItems)
                    {
                        var size = _virtualItems[index].size.x;
                        if (ShouldSnapToNext(xDir, saved - xValue, size))
                            xValue += size + _columnGap;
                    }
                }
                else
                {
                    var saved = xValue;
                    var index = GetIndexOnPos3(ref xValue, false);
                    if (index < _virtualItems.Count && index < _realNumItems)
                    {
                        var size = _virtualItems[index].size.x;
                        if (ShouldSnapToNext(xDir, saved - xValue, size))
                            xValue += size + _columnGap;
                    }
                }
            }
            else
            {
                base.GetSnappingPositionWithDir(ref xValue, ref yValue, xDir, yDir);
            }
        }

        private void HandleAlign(float contentWidth, float contentHeight)
        {
            var newOffset = Vector2.zero;

            if (contentHeight < viewHeight)
            {
                if (_verticalAlign == VertAlignType.Middle)
                    newOffset.y = (int)((viewHeight - contentHeight) / 2);
                else if (_verticalAlign == VertAlignType.Bottom)
                    newOffset.y = viewHeight - contentHeight;
            }

            if (contentWidth < viewWidth)
            {
                if (_align == AlignType.Center)
                    newOffset.x = (int)((viewWidth - contentWidth) / 2);
                else if (_align == AlignType.Right)
                    newOffset.x = viewWidth - contentWidth;
            }

            if (newOffset != _alignOffset)
            {
                _alignOffset = newOffset;
                if (scrollPane != null)
                    scrollPane.AdjustMaskContainer();
                else
                    container.SetXY(_margin.left + _alignOffset.x, _margin.top + _alignOffset.y);
            }
        }

        protected override void UpdateBounds()
        {
            if (isVirtual)
                return;

            var cnt = _children.Count;
            int i;
            var j = 0;
            GObject child;
            float curX = 0;
            float curY = 0;
            float cw, ch;
            float maxWidth = 0;
            float maxHeight = 0;
            var viewWidth = this.viewWidth;
            var viewHeight = this.viewHeight;

            if (_layout == ListLayoutType.SingleColumn)
            {
                for (i = 0; i < cnt; i++)
                {
                    child = GetChildAt(i);
                    if (foldInvisibleItems && !child.visible)
                        continue;

                    if (curY != 0)
                        curY += _lineGap;
                    child.y = curY;
                    if (_autoResizeItem)
                        child.SetSize(viewWidth, child.height, true);
                    curY += Mathf.CeilToInt(child.height);
                    if (child.width > maxWidth)
                        maxWidth = child.width;
                }

                ch = curY;
                if (ch <= viewHeight && _autoResizeItem && scrollPane != null && scrollPane._displayInDemand &&
                    scrollPane.vtScrollBar != null)
                {
                    viewWidth += scrollPane.vtScrollBar.width;
                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        child.SetSize(viewWidth, child.height, true);
                        if (child.width > maxWidth)
                            maxWidth = child.width;
                    }
                }

                cw = Mathf.CeilToInt(maxWidth);
            }
            else if (_layout == ListLayoutType.SingleRow)
            {
                for (i = 0; i < cnt; i++)
                {
                    child = GetChildAt(i);
                    if (foldInvisibleItems && !child.visible)
                        continue;

                    if (curX != 0)
                        curX += _columnGap;
                    child.x = curX;
                    if (_autoResizeItem)
                        child.SetSize(child.width, viewHeight, true);
                    curX += Mathf.CeilToInt(child.width);
                    if (child.height > maxHeight)
                        maxHeight = child.height;
                }

                cw = curX;
                if (cw <= viewWidth && _autoResizeItem && scrollPane != null && scrollPane._displayInDemand &&
                    scrollPane.hzScrollBar != null)
                {
                    viewHeight += scrollPane.hzScrollBar.height;
                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        child.SetSize(child.width, viewHeight, true);
                        if (child.height > maxHeight)
                            maxHeight = child.height;
                    }
                }

                ch = Mathf.CeilToInt(maxHeight);
            }
            else if (_layout == ListLayoutType.FlowHorizontal)
            {
                if (_autoResizeItem && _columnCount > 0)
                {
                    float lineSize = 0;
                    var lineStart = 0;
                    float remainSize;
                    float remainPercent;

                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        lineSize += child.sourceWidth;
                        j++;
                        if (j == _columnCount || i == cnt - 1)
                        {
                            remainSize = viewWidth - (j - 1) * _columnGap;
                            remainPercent = 1;
                            curX = 0;
                            for (j = lineStart; j <= i; j++)
                            {
                                child = GetChildAt(j);
                                if (foldInvisibleItems && !child.visible)
                                    continue;

                                child.SetXY(curX, curY);
                                var perc = child.sourceWidth / lineSize;
                                child.SetSize(Mathf.Round(perc / remainPercent * remainSize), child.height, true);
                                remainSize -= child.width;
                                remainPercent -= perc;
                                curX += child.width + _columnGap;

                                if (child.height > maxHeight)
                                    maxHeight = child.height;
                            }

                            //new line
                            curY += Mathf.CeilToInt(maxHeight) + _lineGap;
                            maxHeight = 0;
                            j = 0;
                            lineStart = i + 1;
                            lineSize = 0;
                        }
                    }

                    ch = curY + Mathf.CeilToInt(maxHeight);
                    cw = viewWidth;
                }
                else
                {
                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        if (curX != 0)
                            curX += _columnGap;

                        if ((_columnCount != 0 && j >= _columnCount)
                            || (_columnCount == 0 && curX + child.width > viewWidth && maxHeight != 0))
                        {
                            //new line
                            curX = 0;
                            curY += Mathf.CeilToInt(maxHeight) + _lineGap;
                            maxHeight = 0;
                            j = 0;
                        }

                        child.SetXY(curX, curY);
                        curX += Mathf.CeilToInt(child.width);
                        if (curX > maxWidth)
                            maxWidth = curX;
                        if (child.height > maxHeight)
                            maxHeight = child.height;
                        j++;
                    }

                    ch = curY + Mathf.CeilToInt(maxHeight);
                    cw = Mathf.CeilToInt(maxWidth);
                }
            }
            else if (_layout == ListLayoutType.FlowVertical)
            {
                if (_autoResizeItem && _lineCount > 0)
                {
                    float lineSize = 0;
                    var lineStart = 0;
                    float remainSize;
                    float remainPercent;

                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        lineSize += child.sourceHeight;
                        j++;
                        if (j == _lineCount || i == cnt - 1)
                        {
                            remainSize = viewHeight - (j - 1) * _lineGap;
                            remainPercent = 1;
                            curY = 0;
                            for (j = lineStart; j <= i; j++)
                            {
                                child = GetChildAt(j);
                                if (foldInvisibleItems && !child.visible)
                                    continue;

                                child.SetXY(curX, curY);
                                var perc = child.sourceHeight / lineSize;
                                child.SetSize(child.width, Mathf.Round(perc / remainPercent * remainSize), true);
                                remainSize -= child.height;
                                remainPercent -= perc;
                                curY += child.height + _lineGap;

                                if (child.width > maxWidth)
                                    maxWidth = child.width;
                            }

                            //new line
                            curX += Mathf.CeilToInt(maxWidth) + _columnGap;
                            maxWidth = 0;
                            j = 0;
                            lineStart = i + 1;
                            lineSize = 0;
                        }
                    }

                    cw = curX + Mathf.CeilToInt(maxWidth);
                    ch = viewHeight;
                }
                else
                {
                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        if (curY != 0)
                            curY += _lineGap;

                        if ((_lineCount != 0 && j >= _lineCount)
                            || (_lineCount == 0 && curY + child.height > viewHeight && maxWidth != 0))
                        {
                            curY = 0;
                            curX += Mathf.CeilToInt(maxWidth) + _columnGap;
                            maxWidth = 0;
                            j = 0;
                        }

                        child.SetXY(curX, curY);
                        curY += child.height;
                        if (curY > maxHeight)
                            maxHeight = curY;
                        if (child.width > maxWidth)
                            maxWidth = child.width;
                        j++;
                    }

                    cw = curX + Mathf.CeilToInt(maxWidth);
                    ch = Mathf.CeilToInt(maxHeight);
                }
            }
            else //pagination
            {
                var page = 0;
                var k = 0;
                float eachHeight = 0;
                if (_autoResizeItem && _lineCount > 0)
                    eachHeight = Mathf.Floor((viewHeight - (_lineCount - 1) * _lineGap) / _lineCount);

                if (_autoResizeItem && _columnCount > 0)
                {
                    float lineSize = 0;
                    var lineStart = 0;
                    float remainSize;
                    float remainPercent;

                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        if (j == 0 && ((_lineCount != 0 && k >= _lineCount)
                                       || (_lineCount == 0 && curY + (_lineCount > 0 ? eachHeight : child.height) >
                                           viewHeight)))
                        {
                            //new page
                            page++;
                            curY = 0;
                            k = 0;
                        }

                        lineSize += child.sourceWidth;
                        j++;
                        if (j == _columnCount || i == cnt - 1)
                        {
                            remainSize = viewWidth - (j - 1) * _columnGap;
                            remainPercent = 1;
                            curX = 0;
                            for (j = lineStart; j <= i; j++)
                            {
                                child = GetChildAt(j);
                                if (foldInvisibleItems && !child.visible)
                                    continue;

                                child.SetXY(page * viewWidth + curX, curY);
                                var perc = child.sourceWidth / lineSize;
                                child.SetSize(Mathf.Round(perc / remainPercent * remainSize),
                                    _lineCount > 0 ? eachHeight : child.height, true);
                                remainSize -= child.width;
                                remainPercent -= perc;
                                curX += child.width + _columnGap;

                                if (child.height > maxHeight)
                                    maxHeight = child.height;
                            }

                            //new line
                            curY += Mathf.CeilToInt(maxHeight) + _lineGap;
                            maxHeight = 0;
                            j = 0;
                            lineStart = i + 1;
                            lineSize = 0;

                            k++;
                        }
                    }
                }
                else
                {
                    for (i = 0; i < cnt; i++)
                    {
                        child = GetChildAt(i);
                        if (foldInvisibleItems && !child.visible)
                            continue;

                        if (curX != 0)
                            curX += _columnGap;

                        if (_autoResizeItem && _lineCount > 0)
                            child.SetSize(child.width, eachHeight, true);

                        if ((_columnCount != 0 && j >= _columnCount)
                            || (_columnCount == 0 && curX + child.width > viewWidth && maxHeight != 0))
                        {
                            curX = 0;
                            curY += maxHeight + _lineGap;
                            maxHeight = 0;
                            j = 0;
                            k++;

                            if ((_lineCount != 0 && k >= _lineCount)
                                || (_lineCount == 0 && curY + child.height > viewHeight && maxWidth != 0)) //new page
                            {
                                page++;
                                curY = 0;
                                k = 0;
                            }
                        }

                        child.SetXY(page * viewWidth + curX, curY);
                        curX += Mathf.CeilToInt(child.width);
                        if (curX > maxWidth)
                            maxWidth = curX;
                        if (child.height > maxHeight)
                            maxHeight = child.height;
                        j++;
                    }
                }

                ch = page > 0 ? viewHeight : curY + Mathf.CeilToInt(maxHeight);
                cw = (page + 1) * viewWidth;
            }

            HandleAlign(cw, ch);
            SetBounds(0, 0, cw, ch);

            InvalidateBatchingState(true);
        }

        public override void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_BeforeAdd(buffer, beginPos);

            buffer.Seek(beginPos, 5);

            _layout = (ListLayoutType)buffer.ReadByte();
            selectionMode = (ListSelectionMode)buffer.ReadByte();
            _align = (AlignType)buffer.ReadByte();
            _verticalAlign = (VertAlignType)buffer.ReadByte();
            _lineGap = buffer.ReadShort();
            _columnGap = buffer.ReadShort();
            _lineCount = buffer.ReadShort();
            _columnCount = buffer.ReadShort();
            _autoResizeItem = buffer.ReadBool();
            _childrenRenderOrder = (ChildrenRenderOrder)buffer.ReadByte();
            _apexIndex = buffer.ReadShort();

            if (buffer.ReadBool())
            {
                _margin.top = buffer.ReadInt();
                _margin.bottom = buffer.ReadInt();
                _margin.left = buffer.ReadInt();
                _margin.right = buffer.ReadInt();
            }

            var overflow = (OverflowType)buffer.ReadByte();
            if (overflow == OverflowType.Scroll)
            {
                var savedPos = buffer.position;
                buffer.Seek(beginPos, 7);
                SetupScroll(buffer);
                buffer.position = savedPos;
            }
            else
            {
                SetupOverflow(overflow);
            }

            if (buffer.ReadBool())
            {
                var i1 = buffer.ReadInt();
                var i2 = buffer.ReadInt();
                clipSoftness = new Vector2(i1, i2);
            }

            if (buffer.version >= 2)
            {
                scrollItemToViewOnClick = buffer.ReadBool();
                foldInvisibleItems = buffer.ReadBool();
            }

            buffer.Seek(beginPos, 8);

            _defaultItem = buffer.ReadS();
            ReadItems(buffer);
        }

        protected virtual void ReadItems(ByteBuffer buffer)
        {
            int itemCount = buffer.ReadShort();
            for (var i = 0; i < itemCount; i++)
            {
                int nextPos = buffer.ReadUshort();
                nextPos += buffer.position;

                var str = buffer.ReadS();
                if (str == null)
                {
                    str = _defaultItem;
                    if (string.IsNullOrEmpty(str))
                    {
                        buffer.position = nextPos;
                        continue;
                    }
                }

                var obj = GetFromPool(str);
                if (obj != null)
                {
                    AddChild(obj);
                    SetupItem(buffer, obj);
                }

                buffer.position = nextPos;
            }
        }

        protected void SetupItem(ByteBuffer buffer, GObject obj)
        {
            string str;
            str = buffer.ReadS();
            if (str != null)
                obj.text = str;
            str = buffer.ReadS();
            if (str != null && obj is GButton)
                (obj as GButton).selectedTitle = str;
            str = buffer.ReadS();
            if (str != null)
                obj.icon = str;
            str = buffer.ReadS();
            if (str != null && obj is GButton)
                (obj as GButton).selectedIcon = str;
            str = buffer.ReadS();
            if (str != null)
                obj.name = str;

            if (obj is GComponent)
            {
                int cnt = buffer.ReadShort();
                for (var i = 0; i < cnt; i++)
                {
                    var cc = ((GComponent)obj).GetController(buffer.ReadS());
                    str = buffer.ReadS();
                    if (cc != null)
                        cc.selectedPageId = str;
                }

                if (buffer.version >= 2)
                {
                    cnt = buffer.ReadShort();
                    for (var i = 0; i < cnt; i++)
                    {
                        var target = buffer.ReadS();
                        int propertyId = buffer.ReadShort();
                        var value = buffer.ReadS();
                        var obj2 = ((GComponent)obj).GetChildByPath(target);
                        if (obj2 != null)
                        {
                            if (propertyId == 0)
                                obj2.text = value;
                            else if (propertyId == 1)
                                obj2.icon = value;
                        }
                    }
                }
            }
        }

        public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);

            buffer.Seek(beginPos, 6);

            int i = buffer.ReadShort();
            if (i != -1)
                selectionController = parent.GetControllerAt(i);
        }

        private class ItemInfo
        {
            public GObject obj;
            public bool selected;
            public Vector2 size;
            public uint updateFlag;
        }
    }
}