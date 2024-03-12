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
    /// <summary>
    /// </summary>
    [CustomEditor(typeof(UIPanel))]
    public class UIPanelEditor : Editor
    {
        private SerializedProperty componentName;
        private SerializedProperty fairyBatching;
        private SerializedProperty fitScreen;
        private SerializedProperty hitTestMode;
        private SerializedProperty packageName;
        private SerializedProperty packagePath;
        private SerializedProperty position;

        private string[] propertyToExclude;
        private SerializedProperty renderCamera;
        private SerializedProperty renderMode;
        private SerializedProperty rotation;
        private SerializedProperty scale;
        private SerializedProperty setNativeChildrenOrder;
        private SerializedProperty sortingOrder;
        private SerializedProperty touchDisabled;

        private void OnEnable()
        {
            packageName = serializedObject.FindProperty("packageName");
            componentName = serializedObject.FindProperty("componentName");
            packagePath = serializedObject.FindProperty("packagePath");
            renderMode = serializedObject.FindProperty("renderMode");
            renderCamera = serializedObject.FindProperty("renderCamera");
            sortingOrder = serializedObject.FindProperty("sortingOrder");
            position = serializedObject.FindProperty("position");
            scale = serializedObject.FindProperty("scale");
            rotation = serializedObject.FindProperty("rotation");
            fairyBatching = serializedObject.FindProperty("fairyBatching");
            fitScreen = serializedObject.FindProperty("fitScreen");
            touchDisabled = serializedObject.FindProperty("touchDisabled");
            hitTestMode = serializedObject.FindProperty("hitTestMode");
            setNativeChildrenOrder = serializedObject.FindProperty("setNativeChildrenOrder");


            propertyToExclude = new[]
            {
                "m_Script", "packageName", "componentName", "packagePath", "renderMode",
                "renderCamera", "sortingOrder", "position", "scale", "rotation", "fairyBatching", "fitScreen",
                "touchDisabled",
                "hitTestMode", "cachedUISize", "setNativeChildrenOrder"
            };
        }

        private void OnSceneGUI()
        {
            var panel = target as UIPanel;
            if (panel.container == null)
                return;

            var pos = panel.GetUIWorldPosition();
            var sizeFactor = HandleUtility.GetHandleSize(pos);
#if UNITY_2017_1_OR_NEWER
            var fmh_147_58_638373071186873791 = Quaternion.identity;
            var newPos = Handles.FreeMoveHandle(pos, sizeFactor, Vector3.one, Handles.ArrowHandleCap);
#else
            Vector3 newPos =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   Handles.FreeMoveHandle(pos, Quaternion.identity, sizeFactor, Vector3.one, Handles.ArrowCap);
#endif
            if (newPos != pos)
            {
                var v1 = HandleUtility.WorldToGUIPoint(pos);
                var v2 = HandleUtility.WorldToGUIPoint(newPos);
                Vector3 delta = v2 - v1;
                delta.x = (int)delta.x;
                delta.y = (int)delta.y;

                panel.MoveUI(delta);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var panel = target as UIPanel;

            DrawPropertiesExcluding(serializedObject, propertyToExclude);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Package Name");
            if (GUILayout.Button(packageName.stringValue, "ObjectField"))
                EditorWindow.GetWindow<PackagesWindow>(true, "Select a UI Component")
                    .SetSelection(packageName.stringValue, componentName.stringValue);

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
#if UNITY_2018_3_OR_NEWER
                var isPrefab = PrefabUtility.GetPrefabAssetType(panel) != PrefabAssetType.NotAPrefab;
#else
                bool isPrefab = PrefabUtility.GetPrefabType(panel) == PrefabType.Prefab;
#endif
                panel.SendMessage("OnUpdateSource", new object[] { null, null, null, !isPrefab });

#if UNITY_5_3_OR_NEWER
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#elif UNITY_5
                EditorApplication.MarkSceneDirty();
#else
                EditorUtility.SetDirty(panel);
#endif
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Component Name");
            if (GUILayout.Button(componentName.stringValue, "ObjectField"))
                EditorWindow.GetWindow<PackagesWindow>(true, "Select a UI Component")
                    .SetSelection(packageName.stringValue, componentName.stringValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Package Path");
            EditorGUILayout.LabelField(packagePath.stringValue, (GUIStyle)"helpbox");
            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying)
                EditorGUILayout.EnumPopup("Render Mode", panel.container.renderMode);
            else
                EditorGUILayout.PropertyField(renderMode);
            if ((RenderMode)renderMode.enumValueIndex != RenderMode.ScreenSpaceOverlay)
                EditorGUILayout.PropertyField(renderCamera);

            var oldSortingOrder = panel.sortingOrder;
            EditorGUILayout.PropertyField(sortingOrder);
            EditorGUILayout.PropertyField(fairyBatching);
            EditorGUILayout.PropertyField(hitTestMode);
            EditorGUILayout.PropertyField(touchDisabled);
            EditorGUILayout.PropertyField(setNativeChildrenOrder);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("UI Transform", (GUIStyle)"OL Title");
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(position);
            EditorGUILayout.PropertyField(rotation);
            EditorGUILayout.PropertyField(scale);
            EditorGUILayout.Space();

            var oldFitScreen = (FitScreen)fitScreen.enumValueIndex;
            EditorGUILayout.PropertyField(fitScreen);

            if (serializedObject.ApplyModifiedProperties())
            {
#if UNITY_2018_3_OR_NEWER
                var isPrefab = PrefabUtility.GetPrefabAssetType(panel) != PrefabAssetType.NotAPrefab;
#else
                bool isPrefab = PrefabUtility.GetPrefabType(panel) == PrefabType.Prefab;
#endif
                if (!isPrefab)
                    panel.ApplyModifiedProperties(sortingOrder.intValue != oldSortingOrder,
                        (FitScreen)fitScreen.enumValueIndex != oldFitScreen);
            }
        }
    }
}
#endif