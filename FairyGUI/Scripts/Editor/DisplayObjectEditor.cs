using FairyGUI;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FairyGUIEditor
{
    /// <summary>
    /// </summary>
    [CustomEditor(typeof(DisplayObjectInfo))]
    public class DisplayObjectEditor : Editor
    {
        private void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            var obj = (target as DisplayObjectInfo).displayObject;
            if (obj == null)
                return;

            EditorGUILayout.LabelField(obj.GetType().Name + ": " + obj.id, (GUIStyle)"OL Title");
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            var name = EditorGUILayout.TextField("Name", obj.name);
            if (EditorGUI.EndChangeCheck())
                obj.name = name;
            if (obj is Container)
            {
                EditorGUI.BeginChangeCheck();
                var fairyBatching = EditorGUILayout.Toggle("FairyBatching", ((Container)obj).fairyBatching);
                if (EditorGUI.EndChangeCheck())
                    ((Container)obj).fairyBatching = fairyBatching;
            }

            var gObj = obj.gOwner;
            if (gObj != null)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField(gObj.GetType().Name + ": " + gObj.id, (GUIStyle)"OL Title");
                EditorGUILayout.Separator();

                if (!string.IsNullOrEmpty(gObj.resourceURL))
                {
                    var pi = UIPackage.GetItemByURL(gObj.resourceURL);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Resource");
                    EditorGUILayout.LabelField(pi.name + "@" + pi.owner.name);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.BeginChangeCheck();
                name = EditorGUILayout.TextField("Name", gObj.name);
                if (EditorGUI.EndChangeCheck())
                    gObj.name = name;

                if (gObj.parent != null)
                {
                    var options = new string[gObj.parent.numChildren];
                    var values = new int[options.Length];
                    for (var i = 0; i < options.Length; i++)
                    {
                        options[i] = i.ToString();
                        values[i] = i;
                    }

                    EditorGUI.BeginChangeCheck();
                    var childIndex = EditorGUILayout.IntPopup("Child Index", gObj.parent.GetChildIndex(gObj), options,
                        values);
                    if (EditorGUI.EndChangeCheck())
                        gObj.parent.SetChildIndex(gObj, childIndex);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Child Index");
                    EditorGUILayout.LabelField("No Parent");
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.BeginChangeCheck();
                var position = EditorGUILayout.Vector3Field("Position", gObj.position);
                if (EditorGUI.EndChangeCheck())
                    gObj.position = position;

                EditorGUI.BeginChangeCheck();
                var rotation = EditorGUILayout.Vector3Field("Rotation",
                    new Vector3(gObj.rotationX, gObj.rotationY, gObj.rotation));
                if (EditorGUI.EndChangeCheck())
                {
                    gObj.rotationX = rotation.x;
                    gObj.rotationY = rotation.y;
                    gObj.rotation = rotation.z;
                }

                EditorGUI.BeginChangeCheck();
                var scale = EditorGUILayout.Vector2Field("Scale", gObj.scale);
                if (EditorGUI.EndChangeCheck())
                    gObj.scale = scale;

                EditorGUI.BeginChangeCheck();
                var skew = EditorGUILayout.Vector2Field("Skew", gObj.skew);
                if (EditorGUI.EndChangeCheck())
                    gObj.skew = skew;

                EditorGUI.BeginChangeCheck();
                var size = EditorGUILayout.Vector2Field("Size", gObj.size);
                if (EditorGUI.EndChangeCheck())
                    gObj.size = size;

                EditorGUI.BeginChangeCheck();
                var pivot = EditorGUILayout.Vector2Field("Pivot", gObj.pivot);
                if (EditorGUI.EndChangeCheck())
                    gObj.pivot = pivot;

                EditorGUI.BeginChangeCheck();
                var text = EditorGUILayout.TextField("Text", gObj.text);
                if (EditorGUI.EndChangeCheck())
                    gObj.text = text;

                EditorGUI.BeginChangeCheck();
                var icon = EditorGUILayout.TextField("Icon", gObj.icon);
                if (EditorGUI.EndChangeCheck())
                    gObj.icon = icon;
            }
        }
    }
}
#endif