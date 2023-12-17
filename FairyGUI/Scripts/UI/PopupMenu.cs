using System;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class PopupMenu : EventDispatcher
    {
        private const string EVENT_TYPE = "PopupMenuItemClick";
        private TimerCallback _closeSubMenu;
        protected GComponent _contentPane;
        protected GObject _expandingItem;
        protected GList _list;
        private EventListener _onClose;
        private EventListener _onPopup;

        private PopupMenu _parentMenu;
        private TimerCallback _showSubMenu;
        public bool autoSize;
        public bool hideOnClickItem;

        public int visibleItemCount;

        public PopupMenu()
        {
            Create(null);
        }

        /// <summary>
        /// </summary>
        /// <param name="resourceURL"></param>
        public PopupMenu(string resourceURL)
        {
            Create(resourceURL);
        }

        public EventListener onPopup => _onPopup ?? (_onPopup = new EventListener(this, "onPopup"));

        public EventListener onClose => _onClose ?? (_onClose = new EventListener(this, "onClose"));

        /// <summary>
        /// </summary>
        public int itemCount => _list.numChildren;

        /// <summary>
        /// </summary>
        public GComponent contentPane => _contentPane;

        /// <summary>
        /// </summary>
        public GList list => _list;

        private void Create(string resourceURL)
        {
            if (resourceURL == null)
            {
                resourceURL = UIConfig.popupMenu;
                if (resourceURL == null)
                {
                    Debug.LogError("FairyGUI: UIConfig.popupMenu not defined");
                    return;
                }
            }

            _contentPane = UIPackage.CreateObjectFromURL(resourceURL).asCom;
            _contentPane.onAddedToStage.Add(__addedToStage);
            _contentPane.onRemovedFromStage.Add(__removeFromStage);
            _contentPane.focusable = false;

            _list = _contentPane.GetChild("list").asList;
            _list.RemoveChildrenToPool();

            _list.AddRelation(_contentPane, RelationType.Width);
            _list.RemoveRelation(_contentPane, RelationType.Height);
            _contentPane.AddRelation(_list, RelationType.Height);

            _list.onClickItem.Add(__clickItem);

            hideOnClickItem = true;
            _showSubMenu = __showSubMenu;
            _closeSubMenu = CloseSubMenu;
        }

        /// <summary>
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GButton AddItem(string caption, EventCallback0 callback)
        {
            var item = CreateItem(caption, callback);
            _list.AddChild(item);

            return item;
        }

        /// <summary>
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GButton AddItem(string caption, EventCallback1 callback)
        {
            var item = CreateItem(caption, callback);
            _list.AddChild(item);

            return item;
        }

        /// <summary>
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GButton AddItemAt(string caption, int index, EventCallback1 callback)
        {
            var item = CreateItem(caption, callback);
            _list.AddChildAt(item, index);

            return item;
        }

        /// <summary>
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GButton AddItemAt(string caption, int index, EventCallback0 callback)
        {
            var item = CreateItem(caption, callback);
            _list.AddChildAt(item, index);

            return item;
        }

        private GButton CreateItem(string caption, Delegate callback)
        {
            var item = _list.GetFromPool(_list.defaultItem).asButton;
            item.title = caption;
            item.grayed = false;
            var c = item.GetController("checked");
            if (c != null)
                c.selectedIndex = 0;
            item.RemoveEventListeners(EVENT_TYPE);
            if (callback is EventCallback0)
                item.AddEventListener(EVENT_TYPE, (EventCallback0)callback);
            else
                item.AddEventListener(EVENT_TYPE, (EventCallback1)callback);

            item.onRollOver.Add(__rollOver);
            item.onRollOut.Add(__rollOut);

            return item;
        }

        /// <summary>
        /// </summary>
        public void AddSeperator()
        {
            AddSeperator(-1);
        }

        /// <summary>
        /// </summary>
        public void AddSeperator(int index)
        {
            if (UIConfig.popupMenu_seperator == null)
            {
                Debug.LogError("FairyGUI: UIConfig.popupMenu_seperator not defined");
                return;
            }

            if (index == -1)
            {
                _list.AddItemFromPool(UIConfig.popupMenu_seperator);
            }
            else
            {
                var item = _list.GetFromPool(UIConfig.popupMenu_seperator);
                _list.AddChildAt(item, index);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetItemName(int index)
        {
            var item = _list.GetChildAt(index).asButton;
            return item.name;
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="caption"></param>
        public void SetItemText(string name, string caption)
        {
            var item = _list.GetChild(name).asButton;
            item.title = caption;
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="visible"></param>
        public void SetItemVisible(string name, bool visible)
        {
            var item = _list.GetChild(name).asButton;
            if (item.visible != visible)
            {
                item.visible = visible;
                _list.SetBoundsChangedFlag();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="grayed"></param>
        public void SetItemGrayed(string name, bool grayed)
        {
            var item = _list.GetChild(name).asButton;
            item.grayed = grayed;
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="checkable"></param>
        public void SetItemCheckable(string name, bool checkable)
        {
            var item = _list.GetChild(name).asButton;
            var c = item.GetController("checked");
            if (c != null)
            {
                if (checkable)
                {
                    if (c.selectedIndex == 0)
                        c.selectedIndex = 1;
                }
                else
                {
                    c.selectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="check"></param>
        public void SetItemChecked(string name, bool check)
        {
            var item = _list.GetChild(name).asButton;
            var c = item.GetController("checked");
            if (c != null)
                c.selectedIndex = check ? 2 : 1;
        }

        [Obsolete("Use IsItemChecked instead")]
        public bool isItemChecked(string name)
        {
            return IsItemChecked(name);
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsItemChecked(string name)
        {
            var item = _list.GetChild(name).asButton;
            var c = item.GetController("checked");
            if (c != null)
                return c.selectedIndex == 2;
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        public void RemoveItem(string name)
        {
            var item = _list.GetChild(name).asCom;
            if (item != null)
            {
                item.RemoveEventListeners(EVENT_TYPE);
                if (item.data is PopupMenu)
                {
                    ((PopupMenu)item.data).Dispose();
                    item.data = null;
                }

                var index = _list.GetChildIndex(item);
                _list.RemoveChildToPoolAt(index);
            }
        }

        /// <summary>
        /// </summary>
        public void ClearItems()
        {
            _list.RemoveChildrenToPool();
        }

        public void Dispose()
        {
            var cnt = _list.numChildren;
            for (var i = 0; i < cnt; i++)
            {
                var obj = _list.GetChildAt(i);
                if (obj.data is PopupMenu)
                    ((PopupMenu)obj.data).Dispose();
            }

            _contentPane.Dispose();
        }

        /// <summary>
        /// </summary>
        public void Show()
        {
            Show(null, PopupDirection.Auto);
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        public void Show(GObject target)
        {
            Show(target, PopupDirection.Auto, null);
        }

        [Obsolete]
        public void Show(GObject target, object downward)
        {
            Show(target,
                downward == null ? PopupDirection.Auto : (bool)downward ? PopupDirection.Down : PopupDirection.Up,
                null);
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dir"></param>
        public void Show(GObject target, PopupDirection dir)
        {
            Show(target, PopupDirection.Auto, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dir"></param>
        /// <param name="parentMenu"></param>
        public void Show(GObject target, PopupDirection dir, PopupMenu parentMenu)
        {
            var r = target != null ? target.root : GRoot.inst;
            r.ShowPopup(contentPane, target is GRoot ? null : target, dir);
            _parentMenu = parentMenu;
        }

        public void Hide()
        {
            if (contentPane.parent != null)
                ((GRoot)contentPane.parent).HidePopup(contentPane);
        }

        private void ShowSubMenu(GObject item)
        {
            _expandingItem = item;

            var popup = item.data as PopupMenu;
            if (item is GButton)
                ((GButton)item).selected = true;
            popup.Show(item, PopupDirection.Auto, this);

            var pt = contentPane.LocalToRoot(new Vector2(item.x + item.width - 5, item.y - 5), item.root);
            popup.contentPane.position = pt;
        }

        private void CloseSubMenu(object param)
        {
            if (contentPane.isDisposed)
                return;

            if (_expandingItem == null)
                return;

            if (_expandingItem is GButton)
                ((GButton)_expandingItem).selected = false;
            var popup = (PopupMenu)_expandingItem.data;
            if (popup == null)
                return;

            _expandingItem = null;
            popup.Hide();
        }

        private void __clickItem(EventContext context)
        {
            var item = ((GObject)context.data).asButton;
            if (item == null)
                return;

            if (item.grayed)
            {
                _list.selectedIndex = -1;
                return;
            }

            var c = item.GetController("checked");
            if (c != null && c.selectedIndex != 0)
            {
                if (c.selectedIndex == 1)
                    c.selectedIndex = 2;
                else
                    c.selectedIndex = 1;
            }

            if (hideOnClickItem)
            {
                if (_parentMenu != null)
                    _parentMenu.Hide();
                Hide();
            }

            item.DispatchEvent(EVENT_TYPE, item); //event data is for backward compatibility 
        }

        private void __addedToStage()
        {
            DispatchEvent("onPopup", null);

            if (autoSize)
            {
                _list.EnsureBoundsCorrect();
                var cnt = _list.numChildren;
                float maxDelta = -1000;
                for (var i = 0; i < cnt; i++)
                {
                    var obj = _list.GetChildAt(i).asButton;
                    if (obj == null)
                        continue;
                    var tf = obj.GetTextField();
                    if (tf != null)
                    {
                        var v = tf.textWidth - tf.width;
                        if (v > maxDelta)
                            maxDelta = v;
                    }
                }

                if (contentPane.width + maxDelta > contentPane.initWidth)
                    contentPane.width += maxDelta;
                else
                    contentPane.width = contentPane.initWidth;
            }

            _list.selectedIndex = -1;
            _list.ResizeToFit(visibleItemCount > 0 ? visibleItemCount : int.MaxValue, 10);
        }

        private void __removeFromStage()
        {
            _parentMenu = null;

            if (_expandingItem != null)
                Timers.inst.Add(0, 1, _closeSubMenu);

            DispatchEvent("onClose", null);
        }

        private void __rollOver(EventContext context)
        {
            var item = (GObject)context.sender;
            if (item.data is PopupMenu || _expandingItem != null) Timers.inst.Add(0.1f, 1, _showSubMenu, item);
        }

        private void __showSubMenu(object param)
        {
            if (contentPane.isDisposed)
                return;

            var item = (GObject)param;
            var r = contentPane.root;
            if (r == null)
                return;

            if (_expandingItem != null)
            {
                if (_expandingItem == item)
                    return;

                CloseSubMenu(null);
            }

            var popup = item.data as PopupMenu;
            if (popup == null)
                return;

            ShowSubMenu(item);
        }

        private void __rollOut(EventContext context)
        {
            if (_expandingItem == null)
                return;

            Timers.inst.Remove(_showSubMenu);

            var r = contentPane.root;
            if (r != null)
            {
                var popup = (PopupMenu)_expandingItem.data;
                var pt = popup.contentPane.GlobalToLocal(context.inputEvent.position);
                if (pt.x >= 0 && pt.y >= 0 && pt.x < popup.contentPane.width && pt.y < popup.contentPane.height)
                    return;
            }

            CloseSubMenu(null);
        }
    }
}