using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI.Utils
{
    /// <summary>
    /// </summary>
    public enum HtmlElementType
    {
        Text,
        Link,
        Image,
        Input,
        Select,
        Object,

        //internal
        LinkEnd
    }

    /// <summary>
    /// </summary>
    public class HtmlElement
    {
        private Hashtable attributes;
        public int charIndex;
        public TextFormat format;

        public IHtmlObject htmlObject;
        public string name;
        public Vector2 position;
        public int space;
        public int status; //1 hidden 2 clipped 4 added
        public string text;
        public HtmlElementType type;

        public HtmlElement()
        {
            format = new TextFormat();
        }


        public bool isEntity => type == HtmlElementType.Image || type == HtmlElementType.Select ||
                                type == HtmlElementType.Input || type == HtmlElementType.Object;

        public object Get(string attrName)
        {
            if (attributes == null)
                return null;

            return attributes[attrName];
        }

        public void Set(string attrName, object attrValue)
        {
            if (attributes == null)
                attributes = new Hashtable();

            attributes[attrName] = attrValue;
        }

        public string GetString(string attrName)
        {
            return GetString(attrName, null);
        }

        public string GetString(string attrName, string defValue)
        {
            if (attributes == null)
                return defValue;

            var ret = attributes[attrName];
            if (ret != null)
                return ret.ToString();
            return defValue;
        }

        public int GetInt(string attrName)
        {
            return GetInt(attrName, 0);
        }

        public int GetInt(string attrName, int defValue)
        {
            var value = GetString(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            if (value[value.Length - 1] == '%')
            {
                int ret;
                if (int.TryParse(value.Substring(0, value.Length - 1), out ret))
                    return Mathf.CeilToInt(ret / 100.0f * defValue);
                return defValue;
            }
            else
            {
                int ret;
                if (int.TryParse(value, out ret))
                    return ret;
                return defValue;
            }
        }

        public float GetFloat(string attrName)
        {
            return GetFloat(attrName, 0);
        }

        public float GetFloat(string attrName, float defValue)
        {
            var value = GetString(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            float ret;
            if (float.TryParse(value, out ret))
                return ret;
            return defValue;
        }

        public bool GetBool(string attrName)
        {
            return GetBool(attrName, false);
        }

        public bool GetBool(string attrName, bool defValue)
        {
            var value = GetString(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            bool ret;
            if (bool.TryParse(value, out ret))
                return ret;
            return defValue;
        }

        public Color GetColor(string attrName, Color defValue)
        {
            var value = GetString(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            return ToolSet.ConvertFromHtmlColor(value);
        }

        public void FetchAttributes()
        {
            attributes = XMLIterator.GetAttributes(attributes);
        }

        #region Pool Support

        private static readonly Stack<HtmlElement> elementPool = new();

        public static HtmlElement GetElement(HtmlElementType type)
        {
            HtmlElement ret;
            if (elementPool.Count > 0)
                ret = elementPool.Pop();
            else
                ret = new HtmlElement();
            ret.type = type;

            if (type != HtmlElementType.Text && ret.attributes == null)
                ret.attributes = new Hashtable();

            return ret;
        }

        public static void ReturnElement(HtmlElement element)
        {
            element.name = null;
            element.text = null;
            element.htmlObject = null;
            element.status = 0;
            if (element.attributes != null)
                element.attributes.Clear();
            elementPool.Push(element);
        }

        public static void ReturnElements(List<HtmlElement> elements)
        {
            var count = elements.Count;
            for (var i = 0; i < count; i++)
            {
                var element = elements[i];
                ReturnElement(element);
            }

            elements.Clear();
        }

        #endregion
    }
}