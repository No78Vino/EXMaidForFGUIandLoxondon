#if UNITY_EDITOR
namespace EXMaidUI.Editor
{
    using EXMaidForUI.Runtime.EXMaid;
    using Sirenix.OdinInspector;
    using Sirenix.OdinInspector.Editor;
    using UnityEditor;
    using UnityEngine;

    public class EXUIMaidSetting:OdinEditorWindow
    {
        private EXMaidUIAsset _asset;
        [MenuItem("EXTool/EX Maid For UI/Setting")]
        public static void Open()
        {
            GetWindow<EXUIMaidSetting>().Show();
        }

        [MenuItem( "EXTool/EX Maid For UI/Document/FairyGUI")]
        public static void OpenDocumentOfFairyGUI()
        {
            Application.OpenURL("https://www.fairygui.com/docs/guide/index.html");
        }

        [MenuItem( "EXTool/EX Maid For UI/Document/Loxodon Framework")]
        public static void OpenDocumentOfLoxodonFramework()
        {
            Application.OpenURL("https://github.com/vovgou/loxodon-framework/blob/master/docs/LoxodonFramework.md");
        }

        [LabelText("FairyGUI 资源包路径:")]
        [LabelWidth(200)]
        [OnValueChanged("OnAssetChanged")]
        public string FairyGUIPackagePath;
        
        [LabelText("FairyGUI 定义代码生成路径:")]
        [LabelWidth(200)]
        [OnValueChanged("OnAssetChanged")]
        public string FairyGUIGenUIDefinePath;

        public void OnAssetChanged()
        {
            _asset.fguiPackagePath = FairyGUIPackagePath;
            _asset.fguiGenUIDefinePath = FairyGUIGenUIDefinePath;
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
        }

        protected override void OnEnable()
        {
            _asset = EXMaidUIAsset.Load();
            FairyGUIPackagePath = _asset.fguiPackagePath;
            FairyGUIGenUIDefinePath = _asset.fguiGenUIDefinePath;
        }
    }
}
#endif