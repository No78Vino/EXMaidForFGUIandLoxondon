using System;
using System.Collections.Generic;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class GTreeNode
    {
        internal GComponent _cell;

        private readonly List<GTreeNode> _children;
        private bool _expanded;
        internal string _resURL;

        /// <summary>
        /// </summary>
        public object data;

        /// <summary>
        /// </summary>
        /// <param name="hasChild"></param>
        public GTreeNode(bool hasChild) : this(hasChild, null)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="hasChild"></param>
        /// <param name="resURL"></param>
        public GTreeNode(bool hasChild, string resURL)
        {
            if (hasChild)
                _children = new List<GTreeNode>();
            _resURL = resURL;
        }

        /// <summary>
        /// </summary>
        public GTreeNode parent { get; private set; }

        /// <summary>
        /// </summary>
        public GTree tree { get; private set; }

        /// <summary>
        /// </summary>
        public GComponent cell => _cell;

        /// <summary>
        /// </summary>
        public int level { get; private set; }

        /// <summary>
        /// </summary>
        public bool expanded
        {
            get => _expanded;

            set
            {
                if (_children == null)
                    return;

                if (_expanded != value)
                {
                    _expanded = value;
                    if (tree != null)
                    {
                        if (_expanded)
                            tree._AfterExpanded(this);
                        else
                            tree._AfterCollapsed(this);
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool isFolder => _children != null;

        /// <summary>
        /// </summary>
        public string text
        {
            get
            {
                if (_cell != null)
                    return _cell.text;
                return null;
            }

            set
            {
                if (_cell != null)
                    _cell.text = value;
            }
        }

        /// <summary>
        /// </summary>
        public string icon
        {
            get
            {
                if (_cell != null)
                    return _cell.icon;
                return null;
            }

            set
            {
                if (_cell != null)
                    _cell.icon = value;
            }
        }

        /// <summary>
        /// </summary>
        public int numChildren => null == _children ? 0 : _children.Count;

        /// <summary>
        /// </summary>
        public void ExpandToRoot()
        {
            var p = this;
            while (p != null)
            {
                p.expanded = true;
                p = p.parent;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        public GTreeNode AddChild(GTreeNode child)
        {
            AddChildAt(child, _children.Count);
            return child;
        }

        /// <summary>
        /// </summary>
        /// <param name="child"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public GTreeNode AddChildAt(GTreeNode child, int index)
        {
            if (child == null)
                throw new Exception("child is null");

            var numChildren = _children.Count;

            if (index >= 0 && index <= numChildren)
            {
                if (child.parent == this)
                {
                    SetChildIndex(child, index);
                }
                else
                {
                    if (child.parent != null)
                        child.parent.RemoveChild(child);

                    var cnt = _children.Count;
                    if (index == cnt)
                        _children.Add(child);
                    else
                        _children.Insert(index, child);

                    child.parent = this;
                    child.level = level + 1;
                    child._SetTree(tree);
                    if ((tree != null && this == tree.rootNode) || (_cell != null && _cell.parent != null && _expanded))
                        tree._AfterInserted(child);
                }

                return child;
            }

            throw new Exception("Invalid child index");
        }

        /// <summary>
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        public GTreeNode RemoveChild(GTreeNode child)
        {
            var childIndex = _children.IndexOf(child);
            if (childIndex != -1) RemoveChildAt(childIndex);
            return child;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GTreeNode RemoveChildAt(int index)
        {
            if (index >= 0 && index < numChildren)
            {
                var child = _children[index];
                _children.RemoveAt(index);

                child.parent = null;
                if (tree != null)
                {
                    child._SetTree(null);
                    tree._AfterRemoved(child);
                }

                return child;
            }

            throw new Exception("Invalid child index");
        }

        /// <summary>
        /// </summary>
        /// <param name="beginIndex"></param>
        /// <param name="endIndex"></param>
        public void RemoveChildren(int beginIndex = 0, int endIndex = -1)
        {
            if (endIndex < 0 || endIndex >= numChildren)
                endIndex = numChildren - 1;

            for (var i = beginIndex; i <= endIndex; ++i)
                RemoveChildAt(beginIndex);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GTreeNode GetChildAt(int index)
        {
            if (index >= 0 && index < numChildren)
                return _children[index];
            throw new Exception("Invalid child index");
        }

        /// <summary>
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        public int GetChildIndex(GTreeNode child)
        {
            return _children.IndexOf(child);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public GTreeNode GetPrevSibling()
        {
            if (parent == null)
                return null;

            var i = parent._children.IndexOf(this);
            if (i <= 0)
                return null;

            return parent._children[i - 1];
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public GTreeNode GetNextSibling()
        {
            if (parent == null)
                return null;

            var i = parent._children.IndexOf(this);
            if (i < 0 || i >= parent._children.Count - 1)
                return null;

            return parent._children[i + 1];
        }

        /// <summary>
        /// </summary>
        /// <param name="child"></param>
        /// <param name="index"></param>
        public void SetChildIndex(GTreeNode child, int index)
        {
            var oldIndex = _children.IndexOf(child);
            if (oldIndex == -1)
                throw new Exception("Not a child of this container");

            var cnt = _children.Count;
            if (index < 0)
                index = 0;
            else if (index > cnt)
                index = cnt;

            if (oldIndex == index)
                return;

            _children.RemoveAt(oldIndex);
            _children.Insert(index, child);
            if ((tree != null && this == tree.rootNode) || (_cell != null && _cell.parent != null && _expanded))
                tree._AfterMoved(child);
        }

        /// <summary>
        /// </summary>
        /// <param name="child1"></param>
        /// <param name="child2"></param>
        public void SwapChildren(GTreeNode child1, GTreeNode child2)
        {
            var index1 = _children.IndexOf(child1);
            var index2 = _children.IndexOf(child2);
            if (index1 == -1 || index2 == -1)
                throw new Exception("Not a child of this container");
            SwapChildrenAt(index1, index2);
        }

        /// <summary>
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        public void SwapChildrenAt(int index1, int index2)
        {
            var child1 = _children[index1];
            var child2 = _children[index2];

            SetChildIndex(child1, index2);
            SetChildIndex(child2, index1);
        }

        internal void _SetTree(GTree value)
        {
            tree = value;
            if (tree != null && tree.treeNodeWillExpand != null && _expanded)
                tree.treeNodeWillExpand(this, true);

            if (_children != null)
            {
                var cnt = _children.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var node = _children[i];
                    node.level = level + 1;
                    node._SetTree(value);
                }
            }
        }
    }
}