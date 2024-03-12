using UnityEditor;
using UnityEngine;

namespace EXMaidForUI.Runtime.EXMaid
{
    public static class EXMaidUIDefine
    {
        public static string ASSET_FOLDER_NAME = "EXMaid";
        public static string ASSET_FOLDER_PATH = "Assets/EXMaid";
        public static string ASSET_PATH = $"{ASSET_FOLDER_PATH}/EXMaidUIAsset.asset";

#if UNITY_EDITOR
        public static void CheckEXMaidUIFolder()
        {
            if (!AssetDatabase.IsValidFolder(ASSET_FOLDER_PATH))
            {
                AssetDatabase.CreateFolder("Assets", ASSET_FOLDER_NAME);
                Debug.Log("EXMaid folder created!");
            }
        }
#endif
    }
}