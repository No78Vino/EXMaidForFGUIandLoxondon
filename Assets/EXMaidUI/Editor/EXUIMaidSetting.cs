using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EXMaidUI.Editor
{
    public class EXUIMaidSetting:OdinEditorWindow
    {
        [MenuItem("EXMaidUI/Setting")]
        public static void Open()
        {
            GetWindow<EXUIMaidSetting>().Show();
        }

        [MenuItem( "EXMaidUI/Document/FairyGUI")]
        public static void OpenDocumentOfFairyGUI()
        {
            Application.OpenURL("https://www.fairygui.com/docs/guide/index.html");
        }

        [MenuItem( "EXMaidUI/Document/Loxodon Framework")]
        public static void OpenDocumentOfLoxodonFramework()
        {
            Application.OpenURL("https://github.com/vovgou/loxodon-framework/blob/master/docs/LoxodonFramework.md");
        }
    }
}