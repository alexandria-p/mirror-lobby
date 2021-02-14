#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Editors
{

    [CustomEditor(typeof(FlexNetworkTransformChild))]
    [CanEditMultipleObjects]
    public class FlexNetworkTransformChildEditor : FlexNetworkTransformBaseEditor
    {
        private SerializedProperty _target;

        protected override void OnEnable()
        {
            base.OnEnable();
            _target = serializedObject.FindProperty("_target");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //Transform.
            EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_target, new GUIContent("Target", "Transform to synchronize."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

        }

    }
}
#endif