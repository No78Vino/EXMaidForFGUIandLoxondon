#if UNITY_EDITOR
namespace EXMaidForUI.Runtime.EXMaid
{
    using EXMaidForUI.Runtime.FairyGUIExtension;
    using UnityEditor;
    using UnityEngine;
    
    public class EXMaidUIAsset : ScriptableObject
    {
        public string fguiPackagePath;
        public string fguiGenUIDefinePath;

        
        private static EXMaidUIAsset _asset;
        public static EXMaidUIAsset Asset => _asset ? _asset : _asset = Load();

        public static EXMaidUIAsset Load()
        {
            string path = EXMaidUIDefine.ASSET_PATH;

            if (Application.isPlaying)
            {
                EXMaidUIAsset exMaidUIAsset =
                    FairyGUIPackageExtension.OnLoadResourceHandler(path, typeof(EXMaidUIAsset)) as EXMaidUIAsset;
                if (exMaidUIAsset == null)
                {
                    Debug.LogError("[EX] EXMaidUIAsset is null!" +
                                   "Set it in EXMaidUI Setting Editor(EXTool/EX Maid For UI/Setting)!");
                }

                return exMaidUIAsset;
            }
            else
            {
                EXMaidUIAsset exMaidUIAsset = AssetDatabase.LoadAssetAtPath<EXMaidUIAsset>(path);

                if (exMaidUIAsset != null) return exMaidUIAsset;
                EXMaidUIDefine.CheckEXMaidUIFolder();
                // 如果文件不存在，则创建一个新的EXMaidUIAsset实例
                exMaidUIAsset = CreateInstance<EXMaidUIAsset>();
                exMaidUIAsset.fguiGenUIDefinePath = "/Scripts/UI/Gen/";
                exMaidUIAsset.fguiPackagePath = "Assets/Game/FairyGUI/";
                AssetDatabase.CreateAsset(exMaidUIAsset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return exMaidUIAsset;
            }
        }

    }
}
#endif