using System;
using FairyGUI;
using UnityEditor;
using UnityEngine;

namespace Framework.Utilities
{
    public static class FGUIPackageExtension
    {
        private static readonly string FileNamePrefix = "Assets/Game/FGUI/";

        public delegate byte[] OnLoadDescData(string path);
        
        public delegate object OnLoadResource(string path, Type type);
        
        /// <summary>
        /// 这个必须要注册，不然默认使用的是Resources加载
        /// </summary>
        public static OnLoadDescData OnLoadDescDataHandler;

        /// <summary>
        /// 这个必须要注册，不然默认使用的是Resources加载
        /// </summary>
        public static OnLoadResource OnLoadResourceHandler;
        
        public static UIPackage.LoadResourceAsync LoadResourceAsyncHandler;
        
        private static byte[] LoadDescData(string packageName)
        {
            string path = $"{FileNamePrefix}{packageName}/{packageName}_fui.bytes";
            if (OnLoadDescDataHandler != null)
            {
                return OnLoadDescDataHandler(path);
            }
            //EXLog.Warning($"[FGUI] OnLoadDescDataHandler is null, use default load method!");
            
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path).bytes;
#else
            return Resources.Load<TextAsset>(path).bytes;
#endif
        }

        private static object LoadResource(string name, string extension, System.Type type,
            out DestroyMethod destroyMethod)
        {
            destroyMethod = DestroyMethod.Unload;
            // 剔除alpha文件检查
            if (extension == ".png" && name.EndsWith("!a"))
            {
                return null;
            }

            string path = $"{FileNamePrefix}{name}{extension}";
            if (OnLoadResourceHandler != null)
            {
                return OnLoadResourceHandler(path,type);
            }
            
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath(path, type);
#else
            return Resources.Load(path, type);
#endif
        }


        public static UIPackage AddPackage(string packageName)
        {
            byte[] descData = LoadDescData(packageName);

            var loadResourceAsync = new UIPackage.LoadResourceAsync(
                (string name, string ext, Type t, PackageItem item) =>
                {
                    // 剔除alpha文件检查
                    if (ext == ".png" && name.EndsWith("!a"))
                    {
                        return;
                    }

                    AssetUtil.LoadAssetAsync<UnityEngine.Object>($"{FileNamePrefix}{packageName}/{name}{ext}",
                        asset =>
                        {
                            item.owner.SetItemAsset(item, asset, DestroyMethod.Unload);
                        }).Forget();
                });
            return UIPackage.AddPackage(descData, packageName, loadResourceAsync);
        }

        /// <summary>
        /// FGUI 包体加载（包括依赖包的加载）
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
                for (int i = 0; i < p.dependencies.Length; i++)
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