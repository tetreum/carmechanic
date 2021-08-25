using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomEditor(typeof(StudioGlobalParameterTrigger))]
    public class StudioGlobalParameterTriggerEditor : Editor
    {
        [SerializeField] private EditorParamRef editorParamRef;

        private SerializedProperty data1, data2;
        private SerializedProperty param;
        private SerializedProperty tag;
        private SerializedProperty trigger;
        private SerializedProperty value;

        private void OnEnable()
        {
            param = serializedObject.FindProperty("parameter");
            trigger = serializedObject.FindProperty("TriggerEvent");
            tag = serializedObject.FindProperty("CollisionTag");
            value = serializedObject.FindProperty("value");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(trigger, new GUIContent("Trigger"));
            if (trigger.enumValueIndex >= (int) EmitterGameEvent.TriggerEnter &&
                trigger.enumValueIndex <= (int) EmitterGameEvent.TriggerExit2D)
                tag.stringValue = EditorGUILayout.TagField("Collision Tag", tag.stringValue);

            EditorGUI.BeginChangeCheck();

            var oldParam = param.stringValue;
            EditorGUILayout.PropertyField(param, new GUIContent("Parameter"));

            if (!string.IsNullOrEmpty(param.stringValue))
            {
                if (!editorParamRef || param.stringValue != oldParam)
                    editorParamRef = EventManager.ParamFromPath(param.stringValue);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Override Value");
                value.floatValue = EditorGUILayout.Slider(value.floatValue, editorParamRef.Min, editorParamRef.Max);
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}