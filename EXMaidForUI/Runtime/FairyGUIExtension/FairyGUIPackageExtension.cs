using System;
using EXMaidForUI.Runtime.EXMaid;
using FairyGUI;
using UnityEditor;
using UnityEngine;

namespace EXMaidForUI.Runtime.FairyGUIExtension
{
    public static class FairyGUIPackageExtension
    {
        public delegate object OnLoadResource(string path, Type type);

        /// <summary>
        ///     这个必须要注册，不然没有匹配的前缀可用
        /// </summary>
        private static string FileNamePrefix; // => EXMaidUIDefine.FGUI_PACKAGE_PATH;

        /// <summary>
        ///     这个必须要注册，不然默认使用的是Resources加载
        /// </summary>
        private static OnLoadResource _onLoadResourceHandler;

        public static OnLoadResource OnLoadResourceHandler => _onLoadResourceHandler;
        
        public static void RegisterOnLoadResourceHandler(OnLoadResource handler)
        {
            _onLoadResourceHandler = handler;
        }
        
        public static void InitFileNamePrefix(string prefix)
        {
            FileNamePrefix = prefix;
        }
        
        private static byte[] LoadDescData(string packageName)
        {
            var path = $"{FileNamePrefix}{packageName}/{packageName}_fui.bytes";
            if (_onLoadResourceHandler != null)
            {
                return ((TextAsset)_onLoadResourceHandler(path, typeof(TextAsset))).bytes;
            }
            //EXLog.Warning($"[FGUI] OnLoadDescDataHandler is null, use default load method!");

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path).bytes;
#else
            return Resources.Load<TextAsset>(path).bytes;
#endif
        }

        private static object LoadResource(string name, string extension, Type type,
            out DestroyMethod destroyMethod)
        {
            destroyMethod = DestroyMethod.Unload;
            // 剔除alpha文件检查
            if (extension == ".png" && name.EndsWith("!a")) return null;

            var path = $"{FileNamePrefix}{name}{extension}";
            if (_onLoadResourceHandler != null) return _onLoadResourceHandler(path, type);

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath(path, type);
#else
            return Resources.Load(path, type);
#endif
        }


        public static UIPackage AddPackage(string packageName)
        {
            var descData = LoadDescData(packageName);

            UIPackage.LoadResource loadResource =
                delegate(string name, string ext, Type type, out DestroyMethod destroyMethod)
                {
                    destroyMethod = DestroyMethod.Unload;
                    // 剔除alpha文件检查
                    if (ext == ".png" && name.EndsWith("!a")) return null;

                    var path = $"{FileNamePrefix}{packageName}/{name}{ext}";
                    if (_onLoadResourceHandler != null) return _onLoadResourceHandler(path, type);

#if UNITY_EDITOR
                    return AssetDatabase.LoadAssetAtPath(path, type);
#else
                return Resources.Load(path, type);
#endif
                };
            return UIPackage.AddPackage(descData, packageName, loadResource);
        }

        /// <summary>
        ///     FGUI 包体加载（包括依赖包的加载）
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public static bool LoadPackage(string packageName)
        {
            if (IsPackageLoaded(packageName)) return true;

            var p = AddPackage(packageName);
            if (p == null)
            {
                Debug.LogError($"[FGUI] Package:{packageName} IS NOT EXPORTED!");
                return false;
            }

            if (p.dependencies != null && p.dependencies.Length > 0)
                for (var i = 0; i < p.dependencies.Length; i++)
                {
                    var name = p.dependencies[i]["name"];
                    if (IsPackageLoaded(name)) continue;

                    if (!LoadPackage(name)) return false;
                }

            return true;
        }

        public static bool IsPackageLoaded(string name)
        {
            return UIPackage.GetByName(name) != null;
        }
    }
}