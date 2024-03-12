using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FairyGUI.Utils
{
    /// <summary>
    ///     A simplest and readonly XML class
    /// </summary>
    public class XML
    {
        private static readonly Stack<XML> sNodeStack = new();

        private Dictionary<string, string> _attributes;
        private XMLList _children;
        public string name;
        public string text;

        public XML(string XmlString)
        {
            Parse(XmlString);
        }

        private XML()
        {
        }

        public Dictionary<string, string> attributes
        {
            get
            {
                if (_attributes == null)
                    _attributes = new Dictionary<string, string>();
                return _attributes;
            }
        }

        public XMLList elements
        {
            get
            {
                if (_children == null)
                    _children = new XMLList();
                return _children;
            }
        }

        public static XML Create(string tag)
        {
            var xml = new XML();
            xml.name = tag;
            return xml;
        }

        public bool HasAttribute(string attrName)
        {
            if (_attributes == null)
                return false;

            return _attributes.ContainsKey(attrName);
        }

        public string GetAttribute(string attrName)
        {
            return GetAttribute(attrName, null);
        }

        public string GetAttribute(string attrName, string defValue)
        {
            if (_attributes == null)
                return defValue;

            string ret;
            if (_attributes.TryGetValue(attrName, out ret))
                return ret;
            return defValue;
        }

        public int GetAttributeInt(string attrName)
        {
            return GetAttributeInt(attrName, 0);
        }

        public int GetAttributeInt(string attrName, int defValue)
        {
            var value = GetAttribute(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            int ret;
            if (int.TryParse(value, out ret))
                return ret;
            return defValue;
        }

        public float GetAttributeFloat(string attrName)
        {
            return GetAttributeFloat(attrName, 0);
        }

        public float GetAttributeFloat(string attrName, float defValue)
        {
            var value = GetAttribute(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            float ret;
            if (float.TryParse(value, out ret))
                return ret;
            return defValue;
        }

        public bool GetAttributeBool(string attrName)
        {
            return GetAttributeBool(attrName, false);
        }

        public bool GetAttributeBool(string attrName, bool defValue)
        {
            var value = GetAttribute(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            bool ret;
            if (bool.TryParse(value, out ret))
                return ret;
            return defValue;
        }

        public string[] GetAttributeArray(string attrName)
        {
            var value = GetAttribute(attrName);
            if (value != null)
            {
                if (value.Length == 0)
                    return new string[] { };
                return value.Split(',');
            }

            return null;
        }

        public string[] GetAttributeArray(string attrName, char seperator)
        {
            var value = GetAttribute(attrName);
            if (value != null)
            {
                if (value.Length == 0)
                    return new string[] { };
                return value.Split(seperator);
            }

            return null;
        }

        public Color GetAttributeColor(string attrName, Color defValue)
        {
            var value = GetAttribute(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            return ToolSet.ConvertFromHtmlColor(value);
        }

        public Vector2 GetAttributeVector(string attrName)
        {
            var value = GetAttribute(attrName);
            if (value != null)
            {
                var arr = value.Split(',');
                return new Vector2(float.Parse(arr[0]), float.Parse(arr[1]));
            }

            return Vector2.zero;
        }

        public void SetAttribute(string attrName, string attrValue)
        {
            if (_attributes == null)
                _attributes = new Dictionary<string, string>();

            _attributes[attrName] = attrValue;
        }

        public void SetAttribute(string attrName, bool attrValue)
        {
            if (_attributes == null)
                _attributes = new Dictionary<string, string>();

            _attributes[attrName] = attrValue ? "true" : "false";
        }

        public void SetAttribute(string attrName, int attrValue)
        {
            if (_attributes == null)
                _attributes = new Dictionary<string, string>();

            _attributes[attrName] = attrValue.ToString();
        }

        public void SetAttribute(string attrName, float attrValue)
        {
            if (_attributes == null)
                _attributes = new Dictionary<string, string>();

            _attributes[attrName] = string.Format("{0:#.####}", attrValue);
        }

        public void RemoveAttribute(string attrName)
        {
            if (_attributes != null)
                _attributes.Remove(attrName);
        }

        public XML GetNode(string selector)
        {
            if (_children == null)
                return null;
            return _children.Find(selector);
        }

        public XMLList Elements()
        {
            if (_children == null)
                _children = new XMLList();
            return _children;
        }

        public XMLList Elements(string selector)
        {
            if (_children == null)
                _children = new XMLList();
            return _children.Filter(selector);
        }

        public XMLList.Enumerator GetEnumerator()
        {
            if (_children == null)
                return new XMLList.Enumerator(null, null);
            return new XMLList.Enumerator(_children.rawList, null);
        }

        public XMLList.Enumerator GetEnumerator(string selector)
        {
            if (_children == null)
                return new XMLList.Enumerator(null, selector);
            return new XMLList.Enumerator(_children.rawList, selector);
        }

        public void AppendChild(XML child)
        {
            elements.Add(child);
        }

        public void RemoveChild(XML child)
        {
            if (_children == null)
                return;

            _children.rawList.Remove(child);
        }

        public void RemoveChildren(string selector)
        {
            if (_children == null)
                return;

            if (string.IsNullOrEmpty(selector))
                _children.Clear();
            else
                _children.RemoveAll(selector);
        }

        public void Parse(string aSource)
        {
            Reset();

            XML lastOpenNode = null;
            sNodeStack.Clear();

            XMLIterator.Begin(aSource);
            while (XMLIterator.NextTag())
                if (XMLIterator.tagType == XMLTagType.Start || XMLIterator.tagType == XMLTagType.Void)
                {
                    XML childNode;
                    if (lastOpenNode != null)
                    {
                        childNode = new XML();
                    }
                    else
                    {
                        if (name != null)
                        {
                            Reset();
                            throw new Exception("Invalid xml format - no root node.");
                        }

                        childNode = this;
                    }

                    childNode.name = XMLIterator.tagName;
                    childNode._attributes = XMLIterator.GetAttributes(childNode._attributes);

                    if (lastOpenNode != null)
                    {
                        if (XMLIterator.tagType != XMLTagType.Void)
                            sNodeStack.Push(lastOpenNode);
                        if (lastOpenNode._children == null)
                            lastOpenNode._children = new XMLList();
                        lastOpenNode._children.Add(childNode);
                    }

                    if (XMLIterator.tagType != XMLTagType.Void)
                        lastOpenNode = childNode;
                }
                else if (XMLIterator.tagType == XMLTagType.End)
                {
                    if (lastOpenNode == null || lastOpenNode.name != XMLIterator.tagName)
                    {
                        Reset();
                        throw new Exception("Invalid xml format - <" + XMLIterator.tagName + "> dismatched.");
                    }

                    if (lastOpenNode._children == null || lastOpenNode._children.Count == 0)
                        lastOpenNode.text = XMLIterator.GetText();

                    if (sNodeStack.Count > 0)
                        lastOpenNode = sNodeStack.Pop();
                    else
                        lastOpenNode = null;
                }
        }

        public void Reset()
        {
            if (_attributes != null)
                _attributes.Clear();
            if (_children != null)
                _children.Clear();
            text = null;
        }

        public string ToXMLString(bool includeHeader)
        {
            var sb = new StringBuilder();
            if (includeHeader)
                sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            ToXMLString(sb, 0);
            return sb.ToString();
        }

        private void ToXMLString(StringBuilder sb, int tabs)
        {
            if (tabs > 0)
                sb.Append(' ', tabs * 2);

            if (name == "!")
            {
                sb.Append("<!--");
                if (text != null)
                {
                    var c = sb.Length;
                    sb.Append(text);
                    XMLUtils.EncodeString(sb, c);
                }

                sb.Append("-->");
                return;
            }

            sb.Append('<').Append(name);
            if (_attributes != null)
                foreach (var kv in _attributes)
                {
                    sb.Append(' ');
                    sb.Append(kv.Key).Append('=').Append('\"');
                    var c = sb.Length;
                    sb.Append(kv.Value);
                    XMLUtils.EncodeString(sb, c, true);
                    sb.Append("\"");
                }

            var numChildren = _children != null ? _children.Count : 0;

            if (string.IsNullOrEmpty(text) && numChildren == 0)
            {
                sb.Append("/>");
            }
            else
            {
                sb.Append('>');

                if (!string.IsNullOrEmpty(text))
                {
                    var c = sb.Length;
                    sb.Append(text);
                    XMLUtils.EncodeString(sb, c);
                }

                if (numChildren > 0)
                {
                    sb.Append('\n');
                    var ctabs = tabs + 1;
                    for (var i = 0; i < numChildren; i++)
                    {
                        _children[i].ToXMLString(sb, ctabs);
                        sb.Append('\n');
                    }

                    if (tabs > 0)
                        sb.Append(' ', tabs * 2);
                }

                sb.Append("</").Append(name).Append(">");
            }
        }
    }
}