﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomEditor(typeof(StudioParameterTrigger))]
    public class StudioParameterTriggerEditor : Editor
    {
        private SerializedProperty emitters;

        private bool[] expanded;
        private SerializedProperty tag;
        private StudioEventEmitter targetEmitter;
        private SerializedProperty trigger;

        private void OnEnable()
        {
            emitters = serializedObject.FindProperty("Emitters");
            trigger = serializedObject.FindProperty("TriggerEvent");
            tag = serializedObject.FindProperty("CollisionTag");
            targetEmitter = null;
            for (var i = 0; i < emitters.arraySize; i++)
            {
                targetEmitter =
                    emitters.GetArrayElementAtIndex(i).FindPropertyRelative("Target").objectReferenceValue as
                        StudioEventEmitter;
                if (targetEmitter != null)
                {
                    expanded = new bool[targetEmitter.GetComponents<StudioEventEmitter>().Length];
                    break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var newTargetEmitter =
                EditorGUILayout.ObjectField("Target", targetEmitter, typeof(StudioEventEmitter), true) as
                    StudioEventEmitter;
            if (newTargetEmitter != targetEmitter)
            {
                emitters.ClearArray();
                targetEmitter = newTargetEmitter;

                if (targetEmitter == null)
                {
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                var newEmitters = new List<StudioEventEmitter>();
                targetEmitter.GetComponents(newEmitters);
                expanded = new bool[newEmitters.Count];
                foreach (var emitter in newEmitters)
                {
                    emitters.InsertArrayElementAtIndex(0);
                    emitters.GetArrayElementAtIndex(0).FindPropertyRelative("Target").objectReferenceValue = emitter;
                }
            }

            if (targetEmitter == null) return;

            EditorGUILayout.PropertyField(trigger, new GUIContent("Trigger"));

            if (trigger.enumValueIndex >= (int) EmitterGameEvent.TriggerEnter &&
                trigger.enumValueIndex <= (int) EmitterGameEvent.TriggerExit2D)
                tag.stringValue = EditorGUILayout.TagField("Collision Tag", tag.stringValue);

            var localEmitters = new List<StudioEventEmitter>();
            targetEmitter.GetComponents(localEmitters);

            var emitterIndex = 0;
            foreach (var emitter in localEmitters)
            {
                SerializedProperty emitterProperty = null;
                for (var i = 0; i < emitters.arraySize; i++)
                    if (emitters.GetArrayElementAtIndex(i).FindPropertyRelative("Target").objectReferenceValue ==
                        emitter)
                    {
                        emitterProperty = emitters.GetArrayElementAtIndex(i);
                        break;
                    }

                // New emitter component added to game object since we last looked
                if (emitterProperty == null)
                {
                    emitters.InsertArrayElementAtIndex(0);
                    emitterProperty = emitters.GetArrayElementAtIndex(0);
                    emitterProperty.FindPropertyRelative("Target").objectReferenceValue = emitter;
                }

                if (!string.IsNullOrEmpty(emitter.Event))
                {
                    expanded[emitterIndex] = EditorGUILayout.Foldout(expanded[emitterIndex], emitter.Event);
                    if (expanded[emitterIndex])
                    {
                        var eventRef = EventManager.EventFromPath(emitter.Event);
                        if (emitter.Event.StartsWith("{"))
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.TextField("Path:", eventRef.Path);
                            EditorGUI.EndDisabledGroup();
                        }

                        foreach (var paramRef in eventRef.LocalParameters)
                        {
                            var set = false;
                            var index = -1;
                            for (var i = 0; i < emitterProperty.FindPropertyRelative("Params").arraySize; i++)
                                if (emitterProperty.FindPropertyRelative("Params").GetArrayElementAtIndex(i)
                                    .FindPropertyRelative("Name").stringValue == paramRef.Name)
                                {
                                    index = i;
                                    set = true;
                                    break;
                                }

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PrefixLabel(paramRef.Name);
                            var newSet = GUILayout.Toggle(set, "");
                            if (!set && newSet)
                            {
                                index = 0;
                                emitterProperty.FindPropertyRelative("Params").InsertArrayElementAtIndex(0);
                                emitterProperty.FindPropertyRelative("Params").GetArrayElementAtIndex(0)
                                    .FindPropertyRelative("Name").stringValue = paramRef.Name;
                                emitterProperty.FindPropertyRelative("Params").GetArrayElementAtIndex(0)
                                    .FindPropertyRelative("Value").floatValue = 0;
                            }

                            if (set && !newSet)
                                emitterProperty.FindPropertyRelative("Params").DeleteArrayElementAtIndex(index);
                            set = newSet;
                            EditorGUI.BeginDisabledGroup(!set);
                            if (set)
                            {
                                var valueProperty = emitterProperty.FindPropertyRelative("Params")
                                    .GetArrayElementAtIndex(index).FindPropertyRelative("Value");
                                valueProperty.floatValue = EditorGUILayout.Slider(valueProperty.floatValue,
                                    paramRef.Min, paramRef.Max);
                            }
                            else
                            {
                                EditorGUILayout.Slider(0, paramRef.Min, paramRef.Max);
                            }

                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                emitterIndex++;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}