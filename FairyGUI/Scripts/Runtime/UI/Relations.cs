using System.Collections.Generic;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class Relations
    {
        private readonly List<RelationItem> _items;
        private readonly GObject _owner;

        public GObject handling;

        public Relations(GObject owner)
        {
            _owner = owner;
            _items = new List<RelationItem>();
        }

        /// <summary>
        /// </summary>
        public bool isEmpty => _items.Count == 0;

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relationType"></param>
        public void Add(GObject target, RelationType relationType)
        {
            Add(target, relationType, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relationType"></param>
        /// <param name="usePercent"></param>
        public void Add(GObject target, RelationType relationType, bool usePercent)
        {
            var cnt = _items.Count;
            for (var i = 0; i < cnt; i++)
            {
                var item = _items[i];
                if (item.target == target)
                {
                    item.Add(relationType, usePercent);
                    return;
                }
            }

            var newItem = new RelationItem(_owner);
            newItem.target = target;
            newItem.Add(relationType, usePercent);
            _items.Add(newItem);
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relationType"></param>
        public void Remove(GObject target, RelationType relationType)
        {
            var cnt = _items.Count;
            var i = 0;
            while (i < cnt)
            {
                var item = _items[i];
                if (item.target == target)
                {
                    item.Remove(relationType);
                    if (item.isEmpty)
                    {
                        item.Dispose();
                        _items.RemoveAt(i);
                        cnt--;
                        continue;
                    }

                    i++;
                }

                i++;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool Contains(GObject target)
        {
            var cnt = _items.Count;
            for (var i = 0; i < cnt; i++)
            {
                var item = _items[i];
                if (item.target == target)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        public void ClearFor(GObject target)
        {
            var cnt = _items.Count;
            var i = 0;
            while (i < cnt)
            {
                var item = _items[i];
                if (item.target == target)
                {
                    item.Dispose();
                    _items.RemoveAt(i);
                    cnt--;
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// </summary>
        public void ClearAll()
        {
            var cnt = _items.Count;
            for (var i = 0; i < cnt; i++)
            {
                var item = _items[i];
                item.Dispose();
            }

            _items.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="source"></param>
        public void CopyFrom(Relations source)
        {
            ClearAll();

            var arr = source._items;
            foreach (var ri in arr)
            {
                var item = new RelationItem(_owner);
                item.CopyFrom(ri);
                _items.Add(item);
            }
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            ClearAll();
            handling = null;
        }

        /// <summary>
        /// </summary>
        /// <param name="dWidth"></param>
        /// <param name="dHeight"></param>
        /// <param name="applyPivot"></param>
        public void OnOwnerSizeChanged(float dWidth, float dHeight, bool applyPivot)
        {
            var cnt = _items.Count;
            if (cnt == 0)
                return;

            for (var i = 0; i < cnt; i++)
                _items[i].ApplyOnSelfSizeChanged(dWidth, dHeight, applyPivot);
        }

        public void Setup(ByteBuffer buffer, bool parentToChild)
        {
            int cnt = buffer.ReadByte();
            GObject target;
            for (var i = 0; i < cnt; i++)
            {
                int targetIndex = buffer.ReadShort();
                if (targetIndex == -1)
                    target = _owner.parent;
                else if (parentToChild)
                    target = ((GComponent)_owner).GetChildAt(targetIndex);
                else
                    target = _owner.parent.GetChildAt(targetIndex);

                var newItem = new RelationItem(_owner);
                newItem.target = target;
                _items.Add(newItem);

                int cnt2 = buffer.ReadByte();
                for (var j = 0; j < cnt2; j++)
                {
                    var rt = (RelationType)buffer.ReadByte();
                    var usePercent = buffer.ReadBool();
                    newItem.InternalAdd(rt, usePercent);
                }
            }
        }
    }
}