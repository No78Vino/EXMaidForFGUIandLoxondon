using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EXMaidUI.Editor
{
    public class EXUIMaidSetting:OdinEditorWindow
    {
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
    }
}