using System;
using Cysharp.Threading.Tasks;
using FairyGUI;
using UnityEngine;
using YooAsset;

namespace Framework.Utilities
{
    public static class FGUIPackageExtension
    {
        private static readonly string FileNamePrefix = "Assets/Game/FGUI/";

        private static byte[] LoadDescData(string packageName)
        {
            return ((TextAsset)YooAssets
                    .LoadAssetSync($"{FileNamePrefix}{packageName}/{packageName}_fui.bytes", typeof(TextAsset))
                    .AssetObject)
                .bytes;
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

            return YooAssets.LoadAssetSync($"{FileNamePrefix}{name}{extension}", type).AssetObject;
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