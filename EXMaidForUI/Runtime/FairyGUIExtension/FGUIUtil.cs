using System;
using System.Collections.Generic;
using EXMaidForUI.Runtime.FairyGUIExtension;
using FairyGUI;
using UnityEngine;

namespace Logic.LogicUtil
{
    public static class FGUIUtil
    {
        private static readonly Dictionary<Tuple<string, string>, Sprite> SpriteCache =
            new Dictionary<Tuple<string, string>, Sprite>();

        private static readonly Dictionary<Tuple<string, string>, Texture2D> TextureCache =
            new Dictionary<Tuple<string, string>, Texture2D>();

        private static NTexture GetItem(string packName, string texName)
        {
            FairyGUIPackageExtension.LoadPackage(packName);
            var url = UIPackage.GetItemURL(packName, texName);
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError($"[FGUI] 资源不存在，package:{packName}  texture:{texName}");
                return null;
            }

            var res = UIPackage.GetItemAssetByURL(url) as NTexture;

            if (res != null) return res;
            Debug.LogError($"[FGUI] 资源非纹理资源，package:{packName}  texture:{texName}");
            return null;

        }

        /// <summary>
        /// 获取FGUI合图内纹理为Sprite资源
        /// </summary>
        /// <param name="packName"></param>
        /// <param name="texName"></param>
        /// <returns></returns>
        public static Sprite GetSprite(string packName, string texName)
        {
            var key = new Tuple<string, string>(packName, texName);
            if (!SpriteCache.TryGetValue(key, out var sprite))
            {
                var res = GetItem(packName, texName);
                var tex = res.nativeTexture;
                var rect = res.uvRect;
                sprite = Sprite.Create(tex as Texture2D,
                    new Rect(rect.x * tex.width, rect.y * tex.height, res.width, res.height),
                    new Vector2(0.5f, 0.5f), 500);
                SpriteCache.Add(key, sprite);
            }

            return sprite;
        }

        /// <summary>
        /// 获取FGUI合图内纹理为Texture资源
        /// </summary>
        /// <param name="packName"></param>
        /// <param name="texName"></param>
        /// <returns></returns>
        public static Texture2D GetTexture(string packName, string texName)
        {
            var key = new Tuple<string, string>(packName, texName);
            if (!TextureCache.TryGetValue(key, out var texture))
            {
                var res = GetItem(packName, texName);
                var tex = (Texture2D)res.nativeTexture;
                var rect = res.uvRect;

                int xStart = Mathf.FloorToInt(rect.x * tex.width);
                int yStart = Mathf.FloorToInt(rect.y * tex.height);
                int width = Mathf.FloorToInt(rect.width * tex.width);
                int height = Mathf.FloorToInt(rect.height * tex.height);

                texture = new Texture2D(width, height);

                Color[] pixels = tex.GetPixels(xStart, yStart, width, height);
                texture.SetPixels(pixels);
                texture.Apply();
                TextureCache.Add(key, texture);
            }

            return texture;
        }
        
        /// <summary>
        /// 获取GObject
        /// </summary>
        /// <param name="packName"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public static GObject GetObject(string packName, string itemName)
        {
            FairyGUIPackageExtension.LoadPackage(packName);
            var url = UIPackage.GetItemURL(packName, itemName);
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError($"[FGUI] 资源不存在，package:{packName}  object:{itemName}");
                return null;
            }

            var res = UIPackage.GetItemAssetByURL(url) as GObject;

            if (res == null)
            {
                Debug.LogError($"[FGUI]资源非组件资源，package:{packName}  item:{itemName}");
                return null;
            }

            return res;
        }

        public static GObject CreateObject(string packName, string itemName)
        {
            FairyGUIPackageExtension.LoadPackage(packName);
            return UIPackage.CreateObject(packName, itemName);
        }

        public static Vector3 GetGobjectWSCenterPosition(GObject target)
        {
            Vector2 localToGlobal = target.LocalToGlobal(Vector2.zero);
            Vector2 pos = GRoot.inst.GlobalToLocal(localToGlobal);
            if (target.pivotAsAnchor)
                pos += target.size * (Vector2.one * 0.5f - target.pivot);
            else
                pos += target.size * 0.5f;
            
            var screenPos = GRoot.inst.LocalToGlobal(pos);
            screenPos.y = Screen.height - screenPos.y;
            var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y));
            
            return worldPos;
        }

        public static Vector2 WorldSpaceToFGUIScreenSpace(Vector3 worldSpacePos)
        {
            var screenPos = Camera.main.WorldToScreenPoint(worldSpacePos);
            screenPos.y = Screen.height - screenPos.y;
            var pos = GRoot.inst.GlobalToLocal(screenPos);
            return pos;
        }
    }
}