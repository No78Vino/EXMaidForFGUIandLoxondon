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
    [CustomEditor(typeof(UIPainter))]
    public class UIPainterEditor : Editor
    {
        private SerializedProperty componentName;
        private SerializedProperty fairyBatching;
        private SerializedProperty packageName;

        private string[] propertyToExclude;
        private SerializedProperty renderCamera;
        private SerializedProperty sortingOrder;
        private SerializedProperty touchDisabled;

        private void OnEnable()
        {
            packageName = serializedObject.FindProperty("packageName");
            componentName = serializedObject.FindProperty("componentName");
            renderCamera = serializedObject.FindProperty("renderCamera");
            fairyBatching = serializedObject.FindProperty("fairyBatching");
            touchDisabled = serializedObject.FindProperty("touchDisabled");
            sortingOrder = serializedObject.FindProperty("sortingOrder");

            propertyToExclude = new[]
            {
                "m_Script", "packageName", "componentName", "packagePath",
                "renderCamera", "fairyBatching", "touchDisabled", "sortingOrder"
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var panel = target as UIPainter;

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
            var oldSortingOrder = panel.sortingOrder;
            EditorGUILayout.PropertyField(sortingOrder);
            EditorGUILayout.PropertyField(renderCamera);
            EditorGUILayout.PropertyField(fairyBatching);
            EditorGUILayout.PropertyField(touchDisabled);

            if (serializedObject.ApplyModifiedProperties())
            {
#if UNITY_2018_3_OR_NEWER
                var isPrefab = PrefabUtility.GetPrefabAssetType(panel) != PrefabAssetType.NotAPrefab;
#else
                bool isPrefab = PrefabUtility.GetPrefabType(panel) == PrefabType.Prefab;
#endif
                if (!isPrefab) panel.ApplyModifiedProperties(sortingOrder.intValue != oldSortingOrder);
            }
        }
    }
}
#endif