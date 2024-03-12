using FairyGUI;
using UnityEditor;

#if UNITY_EDITOR
namespace FairyGUIEditor
{
    /// <summary>
    /// </summary>
    [CustomEditor(typeof(StageCamera))]
    public class StageCameraEditor : Editor
    {
        private string[] propertyToExclude;

        private void OnEnable()
        {
            propertyToExclude = new[] { "m_Script" };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, propertyToExclude);

            if (serializedObject.ApplyModifiedProperties())
                (target as StageCamera).ApplyModifiedProperties();
        }
    }
}
#endif