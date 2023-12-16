using System;
using FairyGUI;
using UnityEditor;
using UnityEngine;

namespace Framework.Utilities
{
    public static class FGUIPackageExtension
    {
        public delegate object OnLoadResource(string path, Type type);

        private static readonly string FileNamePrefix = "Assets/Game/FGUI/";

        /// <summary>
        ///     这个必须要注册，不然默认使用的是Resources加载
        /// </summary>
        private static OnLoadResource OnLoadResourceHandler;
        
        public static void RegisterOnLoadResourceHandler(OnLoadResource handler)
        {
            OnLoadResourceHandler = handler;
        }
        
        private static byte[] LoadDescData(string packageName)
        {
            var path = $"{FileNamePrefix}{packageName}/{packageName}_fui.bytes";
            if (OnLoadResourceHandler != null)
            {
                return ((TextAsset)OnLoadResourceHandler(path, typeof(TextAsset))).bytes;
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
            if (OnLoadResourceHandler != null) return OnLoadResourceHandler(path, type);

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
                    if (OnLoadResourceHandler != null) return OnLoadResourceHandler(path, type);

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