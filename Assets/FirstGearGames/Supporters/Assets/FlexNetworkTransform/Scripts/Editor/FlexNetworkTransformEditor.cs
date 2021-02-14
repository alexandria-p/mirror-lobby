#if UNITY_EDITOR
using UnityEditor;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Editors
{

    [CustomEditor(typeof(FlexNetworkTransform))]
    public class FlexNetworkTransformEditor : FlexNetworkTransformBaseEditor
    {
        //private  MonoScript _script;

        protected override void OnEnable()
        {
            //_script = MonoScript.FromMonoBehaviour((FlexNetworkTransform)target);
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            //EditorGUI.BeginDisabledGroup(true);
            //_script = EditorGUILayout.ObjectField("Script:", _script, typeof(MonoScript), false) as MonoScript;
            //EditorGUI.EndDisabledGroup();

            base.OnInspectorGUI();
        }


    }

}
#endif