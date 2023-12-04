using System.Collections.Generic;

namespace FairyGUI.Utils
{
    /// <summary>
    /// </summary>
    public class XMLList
    {
        private static List<XML> _tmpList = new();
        public List<XML> rawList;

        public XMLList()
        {
            rawList = new List<XML>();
        }

        public XMLList(List<XML> list)
        {
            rawList = list;
        }

        public int Count => rawList.Count;

        public XML this[int index] => rawList[index];

        public void Add(XML xml)
        {
            rawList.Add(xml);
        }

        public void Clear()
        {
            rawList.Clear();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(rawList, null);
        }

        public Enumerator GetEnumerator(string selector)
        {
            return new Enumerator(rawList, selector);
        }

        public XMLList Filter(string selector)
        {
            var allFit = true;
            _tmpList.Clear();
            var cnt = rawList.Count;
            for (var i = 0; i < cnt; i++)
            {
                var xml = rawList[i];
                if (xml.name == selector)
                    _tmpList.Add(xml);
                else
                    allFit = false;
            }

            if (allFit)
            {
                return this;
            }

            var ret = new XMLList(_tmpList);
            _tmpList = new List<XML>();
            return ret;
        }

        public XML Find(string selector)
        {
            var cnt = rawList.Count;
            for (var i = 0; i < cnt; i++)
            {
                var xml = rawList[i];
                if (xml.name == selector)
                    return xml;
            }

            return null;
        }

        public void RemoveAll(string selector)
        {
            rawList.RemoveAll(xml => xml.name == selector);
        }

        public struct Enumerator
        {
            private readonly List<XML> _source;
            private readonly string _selector;
            private int _index;
            private int _total;

            public Enumerator(List<XML> source, string selector)
            {
                _source = source;
                _selector = selector;
                _index = -1;
                if (_source != null)
                    _total = _source.Count;
                else
                    _total = 0;
                Current = null;
            }

            public XML Current { get; private set; }

            public bool MoveNext()
            {
                while (++_index < _total)
                {
                    Current = _source[_index];
                    if (_selector == null || Current.name == _selector)
                        return true;
                }

                return false;
            }

            public void Erase()
            {
                _source.RemoveAt(_index);
                _total--;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}