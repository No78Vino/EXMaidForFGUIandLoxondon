using System;
using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class FontManager
    {
        public static Dictionary<string, BaseFont> sFontFactory = new();

        /// <summary>
        /// </summary>
        /// <param name="font"></param>
        /// <param name="alias"></param>
        public static void RegisterFont(BaseFont font, string alias = null)
        {
            sFontFactory[font.name] = font;
            if (alias != null)
                sFontFactory[alias] = font;
        }

        /// <summary>
        /// </summary>
        /// <param name="font"></param>
        public static void UnregisterFont(BaseFont font)
        {
            var toDelete = new List<string>();
            foreach (var kv in sFontFactory)
                if (kv.Value == font)
                    toDelete.Add(kv.Key);

            foreach (var key in toDelete)
                sFontFactory.Remove(key);
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static BaseFont GetFont(string name)
        {
            BaseFont font;
            if (name.StartsWith(UIPackage.URL_PREFIX))
            {
                font = UIPackage.GetItemAssetByURL(name) as BaseFont;
                if (font != null)
                    return font;
            }

            if (sFontFactory.TryGetValue(name, out font))
                return font;

            object asset = Resources.Load(name);
            if (asset == null)
                asset = Resources.Load("Fonts/" + name);

            //Try to use new API in Uinty5 to load
            if (asset == null)
            {
                if (name.IndexOf(",") != -1)
                {
                    var arr = name.Split(',');
                    var cnt = arr.Length;
                    for (var i = 0; i < cnt; i++)
                        arr[i] = arr[i].Trim();
                    asset = Font.CreateDynamicFontFromOSFont(arr, 16);
                }
                else
                {
                    asset = Font.CreateDynamicFontFromOSFont(name, 16);
                }
            }

            if (asset == null)
                return Fallback(name);

            if (asset is Font)
            {
                font = new DynamicFont();
                font.name = name;
                sFontFactory.Add(name, font);

                ((DynamicFont)font).nativeFont = (Font)asset;
            }
#if FAIRYGUI_TMPRO
            else if (asset is TMPro.TMP_FontAsset)
            {
                font = new TMPFont();
                font.name = name;
                sFontFactory.Add(name, font);
                ((TMPFont)font).fontAsset = (TMPro.TMP_FontAsset)asset;
            }
#endif
            else
            {
                if (asset.GetType().Name.Contains("TMP_FontAsset"))
                    Debug.LogWarning("To enable TextMeshPro support, add script define symbol: FAIRYGUI_TMPRO");

                return Fallback(name);
            }

            return font;
        }

        private static BaseFont Fallback(string name)
        {
            if (name != UIConfig.defaultFont)
            {
                BaseFont ff;
                if (sFontFactory.TryGetValue(UIConfig.defaultFont, out ff))
                {
                    sFontFactory[name] = ff;
                    return ff;
                }
            }

            var asset = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            if (asset == null)
                throw new Exception("Failed to load font '" + name + "'");

            BaseFont font = new DynamicFont();
            font.name = name;
            ((DynamicFont)font).nativeFont = asset;

            sFontFactory.Add(name, font);
            return font;
        }

        /// <summary>
        /// </summary>
        public static void Clear()
        {
            foreach (var kv in sFontFactory)
                kv.Value.Dispose();

            sFontFactory.Clear();
        }
    }
}