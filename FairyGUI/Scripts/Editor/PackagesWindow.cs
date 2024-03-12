#if UNITY_EDITOR
namespace FairyGUIEditor
{
    using FairyGUI;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
#if UNITY_5_3_OR_NEWER
    using UnityEditor.SceneManagement;
#endif
#if UNITY_2018_3_OR_NEWER
#endif
    
    /// <summary>
    /// </summary>
    public class PackagesWindow : EditorWindow
    {
        private GUIStyle itemStyle;
        private Vector2 scrollPos1;
        private Vector2 scrollPos2;
        private string selectedComponentName;

        private int selectedPackage;
        private string selectedPackageName;

        public PackagesWindow()
        {
            maxSize = new Vector2(550, 400);
            minSize = new Vector2(550, 400);
        }

        private void OnGUI()
        {
            if (itemStyle == null)
                itemStyle = new GUIStyle(GUI.skin.GetStyle("Tag MenuItem"));

            EditorGUILayout.BeginHorizontal();

            //package list start------
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Packages", (GUIStyle)"OL Title", GUILayout.Width(300));
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);

            scrollPos1 =
                EditorGUILayout.BeginScrollView(scrollPos1, "CN Box", GUILayout.Height(300), GUILayout.Width(300));
            EditorToolSet.LoadPackages();
            var pkgs = UIPackage.GetPackages();
            var cnt = pkgs.Count;
            if (cnt == 0)
            {
                selectedPackage = -1;
                selectedPackageName = null;
            }
            else
            {
                for (var i = 0; i < cnt; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Toggle(selectedPackageName == pkgs[i].name, pkgs[i].name, itemStyle,
                            GUILayout.ExpandWidth(true)))
                    {
                        selectedPackage = i;
                        selectedPackageName = pkgs[i].name;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            //package list end------

            //component list start------

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Components", (GUIStyle)"OL Title", GUILayout.Width(220));
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);

            scrollPos2 =
                EditorGUILayout.BeginScrollView(scrollPos2, "CN Box", GUILayout.Height(300), GUILayout.Width(220));
            if (selectedPackage >= 0)
            {
                var items = pkgs[selectedPackage].GetItems();
                var i = 0;
                foreach (var pi in items)
                    if (pi.type == PackageItemType.Component && pi.exported)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Toggle(selectedComponentName == pi.name, pi.name, itemStyle,
                                GUILayout.ExpandWidth(true)))
                            selectedComponentName = pi.name;
                        i++;
                        EditorGUILayout.EndHorizontal();
                    }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            //component list end------

            GUILayout.Space(10);

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            //buttons start---
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(180);

            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                EditorToolSet.ReloadPackages();

            GUILayout.Space(20);
            if (GUILayout.Button("OK", GUILayout.Width(100)) && selectedPackage >= 0)
            {
                var selectedPkg = pkgs[selectedPackage];
                var tmp = selectedPkg.assetPath.ToLower();
                string packagePath;
                var pos = tmp.LastIndexOf("/resources/");
                if (pos != -1)
                {
                    packagePath = selectedPkg.assetPath.Substring(pos + 11);
                }
                else
                {
                    pos = tmp.IndexOf("resources/");
                    if (pos == 0)
                        packagePath = selectedPkg.assetPath.Substring(pos + 10);
                    else
                        packagePath = selectedPkg.assetPath;
                }

                if (Selection.activeGameObject != null)
                {
#if UNITY_2018_3_OR_NEWER
                    var isPrefab = PrefabUtility.GetPrefabAssetType(Selection.activeGameObject) !=
                                   PrefabAssetType.NotAPrefab;
#else
                    bool isPrefab = PrefabUtility.GetPrefabType(Selection.activeGameObject) == PrefabType.Prefab;
#endif
                    Selection.activeGameObject.SendMessage("OnUpdateSource",
                        new object[] { selectedPkg.name, packagePath, selectedComponentName, !isPrefab },
                        SendMessageOptions.DontRequireReceiver);
                }

#if UNITY_2018_3_OR_NEWER
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                    EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                else
                    ApplyChange();
#else
                ApplyChange();
#endif
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        public void SetSelection(string packageName, string componentName)
        {
            selectedPackageName = packageName;
            selectedComponentName = componentName;
        }

        private void ApplyChange()
        {
#if UNITY_5_3_OR_NEWER
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#elif UNITY_5
            EditorApplication.MarkSceneDirty();
#else
            EditorUtility.SetDirty(Selection.activeGameObject);
#endif
        }
    }
}
#endif