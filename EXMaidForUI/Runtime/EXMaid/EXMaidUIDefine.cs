using UnityEditor;
using UnityEngine;

namespace EXMaidForUI.Runtime.EXMaid
{
    public static class EXMaidUIDefine
    {
        public static string ASSET_FOLDER_NAME = "EXMaid";
        public static string ASSET_FOLDER_PATH = "Assets/EXMaid";
        public static string ASSET_PATH = $"{ASSET_FOLDER_PATH}/EXMaidUIAsset.asset";

        private static EXMaidUIAsset _exMaidUIAsset;
        static EXMaidUIAsset EXMaidUIAsset => _exMaidUIAsset ? _exMaidUIAsset : _exMaidUIAsset = EXMaidUIAsset.Load();
        public static string FGUI_GEN_UI_DEFINE_PATH => EXMaidUIAsset.fguiGenUIDefinePath;
        public static string FGUI_PACKAGE_PATH => EXMaidUIAsset.fguiPackagePath;
        
        public static void CheckEXMaidUIFolder()
        {
            if (!AssetDatabase.IsValidFolder(ASSET_FOLDER_PATH))
            {
                AssetDatabase.CreateFolder("Assets", ASSET_FOLDER_NAME);
                Debug.Log("EXMaid folder created!");
            }
        }
    }
}