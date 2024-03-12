using FairyGUI;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FairyGUIEditor
{
    /// <summary>
    /// </summary>
    [CustomEditor(typeof(UIConfig))]
    public class UIConfigEditor : Editor
    {
        private const float kButtonWidth = 18f;
        private int errorState;
        private bool itemsFoldout;
        private bool packagesFoldOut;
        private string[] propertyToExclude;

        private void OnEnable()
        {
            propertyToExclude = new[] { "m_Script", "Items", "PreloadPackages" };

            itemsFoldout = EditorPrefs.GetBool("itemsFoldOut");
            packagesFoldOut = EditorPrefs.GetBool("packagesFoldOut");
            errorState = 0;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, propertyToExclude);

            var config = (UIConfig)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            itemsFoldout = EditorGUILayout.Foldout(itemsFoldout, "Config Items");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool("itemsFoldOut", itemsFoldout);
            EditorGUILayout.EndHorizontal();

            var modified = false;

            if (itemsFoldout)
            {
                Undo.RecordObject(config, "Items");

                var len = config.Items.Count;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Add");
                var selectedKey = (UIConfig.ConfigKey)EditorGUILayout.EnumPopup(UIConfig.ConfigKey.PleaseSelect);

                if (selectedKey != UIConfig.ConfigKey.PleaseSelect)
                {
                    var index = (int)selectedKey;

                    if (index > len - 1)
                    {
                        for (var i = len; i < index; i++)
                            config.Items.Add(new UIConfig.ConfigValue());

                        var value = new UIConfig.ConfigValue();
                        value.valid = true;
                        UIConfig.SetDefaultValue(selectedKey, value);
                        config.Items.Add(value);
                    }
                    else
                    {
                        var value = config.Items[index];
                        if (value == null)
                        {
                            value = new UIConfig.ConfigValue();
                            value.valid = true;
                            UIConfig.SetDefaultValue(selectedKey, value);
                            config.Items[index] = value;
                        }
                        else if (!value.valid)
                        {
                            value.valid = true;
                            UIConfig.SetDefaultValue(selectedKey, value);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();

                for (var i = 0; i < len; i++)
                {
                    var value = config.Items[i];
                    if (value == null || !value.valid)
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(((UIConfig.ConfigKey)i).ToString());
                    switch ((UIConfig.ConfigKey)i)
                    {
                        case UIConfig.ConfigKey.ClickDragSensitivity:
                        case UIConfig.ConfigKey.DefaultComboBoxVisibleItemCount:
                        case UIConfig.ConfigKey.DefaultScrollStep:
                        case UIConfig.ConfigKey.TouchDragSensitivity:
                        case UIConfig.ConfigKey.TouchScrollSensitivity:
                        case UIConfig.ConfigKey.InputCaretSize:
                            value.i = EditorGUILayout.IntField(value.i);
                            break;

                        case UIConfig.ConfigKey.ButtonSound:
                        case UIConfig.ConfigKey.GlobalModalWaiting:
                        case UIConfig.ConfigKey.HorizontalScrollBar:
                        case UIConfig.ConfigKey.LoaderErrorSign:
                        case UIConfig.ConfigKey.PopupMenu:
                        case UIConfig.ConfigKey.PopupMenu_seperator:
                        case UIConfig.ConfigKey.TooltipsWin:
                        case UIConfig.ConfigKey.VerticalScrollBar:
                        case UIConfig.ConfigKey.WindowModalWaiting:
                        case UIConfig.ConfigKey.DefaultFont:
                            value.s = EditorGUILayout.TextField(value.s);
                            break;

                        case UIConfig.ConfigKey.DefaultScrollBounceEffect:
                        case UIConfig.ConfigKey.DefaultScrollTouchEffect:
                        case UIConfig.ConfigKey.RenderingTextBrighterOnDesktop:
                        case UIConfig.ConfigKey.AllowSoftnessOnTopOrLeftSide:
                        case UIConfig.ConfigKey.DepthSupportForPaintingMode:
                            value.b = EditorGUILayout.Toggle(value.b);
                            break;

                        case UIConfig.ConfigKey.ButtonSoundVolumeScale:
                            value.f = EditorGUILayout.Slider(value.f, 0, 1);
                            break;

                        case UIConfig.ConfigKey.ModalLayerColor:
                        case UIConfig.ConfigKey.InputHighlightColor:
                            value.c = EditorGUILayout.ColorField(value.c);
                            break;

                        case UIConfig.ConfigKey.Branch:
                            EditorGUI.BeginChangeCheck();
                            value.s = EditorGUILayout.TextField(value.s);
                            if (EditorGUI.EndChangeCheck())
                                modified = true;
                            break;
                    }

                    if (GUILayout.Button(new GUIContent("X", "Delete Item"), EditorStyles.miniButtonRight,
                            GUILayout.Width(30)))
                    {
                        config.Items[i].Reset();
                        UIConfig.SetDefaultValue((UIConfig.ConfigKey)i, config.Items[i]);
                        modified = true;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            packagesFoldOut = EditorGUILayout.Foldout(packagesFoldOut, "Preload Packages");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool("packagesFoldOut", packagesFoldOut);
            EditorGUILayout.EndHorizontal();

            if (packagesFoldOut)
            {
                Undo.RecordObject(config, "PreloadPackages");

                EditorToolSet.LoadPackages();

                if (EditorToolSet.packagesPopupContents != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Add");
                    var selected = EditorGUILayout.Popup(EditorToolSet.packagesPopupContents.Length - 1,
                        EditorToolSet.packagesPopupContents);
                    EditorGUILayout.EndHorizontal();

                    if (selected != EditorToolSet.packagesPopupContents.Length - 1)
                    {
                        var pkg = UIPackage.GetPackages()[selected];
                        var tmp = pkg.assetPath.ToLower();
                        var pos = tmp.LastIndexOf("resources/");
                        if (pos != -1)
                        {
                            var packagePath = pkg.assetPath.Substring(pos + 10);
                            if (config.PreloadPackages.IndexOf(packagePath) == -1)
                                config.PreloadPackages.Add(packagePath);

                            errorState = 0;
                        }
                        else
                        {
                            errorState = 10;
                        }
                    }
                }

                if (errorState > 0)
                {
                    errorState--;
                    EditorGUILayout.HelpBox("Package is not in resources folder.", MessageType.Warning);
                }

                var cnt = config.PreloadPackages.Count;
                var pi = 0;
                while (pi < cnt)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("" + pi + ".");
                    config.PreloadPackages[pi] = EditorGUILayout.TextField(config.PreloadPackages[pi]);
                    if (GUILayout.Button(new GUIContent("X", "Delete Item"), EditorStyles.miniButtonRight,
                            GUILayout.Width(30)))
                    {
                        config.PreloadPackages.RemoveAt(pi);
                        cnt--;
                    }
                    else
                    {
                        pi++;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                errorState = 0;
            }

            if (serializedObject.ApplyModifiedProperties() || modified)
                (target as UIConfig).ApplyModifiedProperties();
        }
    }
}
#endif