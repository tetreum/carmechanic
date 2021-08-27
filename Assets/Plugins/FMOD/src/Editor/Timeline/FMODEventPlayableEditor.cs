#if (UNITY_TIMELINE_EXIST || !UNITY_2019_1_OR_NEWER)

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Timeline;

namespace FMODUnity
{
    [CustomEditor(typeof(FMODEventPlayable))]
    public class FMODEventPlayableEditor : Editor
    {
        private EditorEventRef editorEventRef;

        private string eventName;
        private FMODEventPlayable eventPlayable;
        private ListView initialParameterValuesView;
        private List<EditorParamRef> missingInitialParameterValues = new List<EditorParamRef>();
        private List<EditorParamRef> missingParameterAutomations = new List<EditorParamRef>();
        private SerializedProperty parameterAutomationProperty;
        private SerializedProperty parameterLinksProperty;

        private ListView parameterLinksView;

        private SerializedProperty parametersProperty;

        public void OnEnable()
        {
            eventPlayable = target as FMODEventPlayable;

            parametersProperty = serializedObject.FindProperty("template.parameters");
            parameterLinksProperty = serializedObject.FindProperty("template.parameterLinks");
            parameterAutomationProperty = serializedObject.FindProperty("template.parameterAutomation");

            parameterLinksView = new ListView(parameterLinksProperty);
            parameterLinksView.drawElementWithLabelCallback = DrawParameterLink;
            parameterLinksView.onCanAddCallback = list => missingParameterAutomations.Count > 0;
            parameterLinksView.onAddDropdownCallback = DoAddParameterLinkMenu;
            parameterLinksView.onRemoveCallback = list => DeleteParameterAutomation(list.index);

            initialParameterValuesView = new ListView(parametersProperty);
            initialParameterValuesView.drawElementWithLabelCallback = DrawInitialParameterValue;
            initialParameterValuesView.onCanAddCallback = list => missingInitialParameterValues.Count > 0;
            initialParameterValuesView.onAddDropdownCallback = DoAddInitialParameterValueMenu;
            initialParameterValuesView.onRemoveCallback = list => DeleteInitialParameterValue(list.index);

            RefreshEventRef();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            RefreshMissingParameterLists();

            // This is in case the undo/redo modified any curves on the Playable's clip
            RefreshTimelineEditor();
        }

        private void RefreshEventRef()
        {
            if (eventName != eventPlayable.eventName)
            {
                eventName = eventPlayable.eventName;

                if (!string.IsNullOrEmpty(eventName))
                    editorEventRef = EventManager.EventFromPath(eventName);
                else
                    editorEventRef = null;

                if (editorEventRef != null)
                    eventPlayable.UpdateEventDuration(
                        editorEventRef.IsOneShot ? editorEventRef.Length : float.PositiveInfinity);

                ValidateParameterSettings();
                RefreshMissingParameterLists();
            }
        }

        private void ValidateParameterSettings()
        {
            if (editorEventRef != null)
            {
                var namesToDelete = new List<string>();

                for (var i = 0; i < parametersProperty.arraySize; ++i)
                {
                    var current = parametersProperty.GetArrayElementAtIndex(i);
                    var name = current.FindPropertyRelative("Name");

                    var paramRef = editorEventRef.LocalParameters.FirstOrDefault(p => p.Name == name.stringValue);

                    if (paramRef != null)
                    {
                        var value = current.FindPropertyRelative("Value");
                        value.floatValue = Mathf.Clamp(value.floatValue, paramRef.Min, paramRef.Max);
                    }
                    else
                    {
                        namesToDelete.Add(name.stringValue);
                    }
                }

                foreach (var name in namesToDelete) DeleteInitialParameterValue(name);

                namesToDelete.Clear();

                for (var i = 0; i < parameterLinksProperty.arraySize; ++i)
                {
                    var current = parameterLinksProperty.GetArrayElementAtIndex(i);
                    var name = current.FindPropertyRelative("Name");

                    if (!editorEventRef.LocalParameters.Any(p => p.Name == name.stringValue))
                        namesToDelete.Add(name.stringValue);
                }

                foreach (var name in namesToDelete) DeleteParameterAutomation(name);
            }
        }

        private void RefreshMissingParameterLists()
        {
            if (editorEventRef != null)
            {
                serializedObject.Update();

                missingInitialParameterValues =
                    editorEventRef.LocalParameters.Where(p => !InitialParameterValueExists(p.Name)).ToList();
                missingParameterAutomations =
                    editorEventRef.LocalParameters.Where(p => !ParameterLinkExists(p.Name)).ToList();
            }
            else
            {
                missingInitialParameterValues.Clear();
                missingParameterAutomations.Clear();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RefreshEventRef();

            var ev = serializedObject.FindProperty("eventName");
            var stopType = serializedObject.FindProperty("stopType");

            EditorGUILayout.PropertyField(ev, new GUIContent("Event"));
            EditorGUILayout.PropertyField(stopType, new GUIContent("Stop Mode"));

            DrawInitialParameterValues();
            DrawParameterAutomations();

            eventPlayable.OnValidate();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInitialParameterValues()
        {
            if (editorEventRef != null)
            {
                parametersProperty.isExpanded =
                    EditorGUILayout.Foldout(parametersProperty.isExpanded, "Initial Parameter Values", true);

                if (parametersProperty.isExpanded) initialParameterValuesView.DrawLayout();
            }
        }

        private void DoAddInitialParameterValueMenu(Rect rect, ReorderableList list)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), false, () =>
            {
                foreach (var parameter in missingInitialParameterValues) AddInitialParameterValue(parameter);
            });

            menu.AddSeparator(string.Empty);

            foreach (var parameter in missingInitialParameterValues)
            {
                var text = parameter.Name;

                if (ParameterLinkExists(parameter.Name)) text += " (automated)";

                menu.AddItem(new GUIContent(text), false,
                    userData => { AddInitialParameterValue(userData as EditorParamRef); },
                    parameter);
            }

            menu.DropDown(rect);
        }

        private void DrawInitialParameterValue(Rect rect, float labelRight, int index, bool active, bool focused)
        {
            if (editorEventRef == null) return;

            var property = parametersProperty.GetArrayElementAtIndex(index);

            var name = property.FindPropertyRelative("Name").stringValue;

            var paramRef = editorEventRef.LocalParameters.FirstOrDefault(p => p.Name == name);

            if (paramRef == null) return;

            var nameLabelRect = rect;
            nameLabelRect.xMax = labelRight;

            var sliderRect = rect;
            sliderRect.xMin = nameLabelRect.xMax;

            var valueProperty = property.FindPropertyRelative("Value");

            GUI.Label(nameLabelRect, name);

            using (new NoIndentScope())
            {
                valueProperty.floatValue =
                    EditorGUI.Slider(sliderRect, valueProperty.floatValue, paramRef.Min, paramRef.Max);
            }
        }

        private void DrawParameterAutomations()
        {
            if (editorEventRef != null)
            {
                parameterLinksProperty.isExpanded =
                    EditorGUILayout.Foldout(parameterLinksProperty.isExpanded, "Parameter Automations", true);

                if (parameterLinksProperty.isExpanded) parameterLinksView.DrawLayout();
            }
        }

        private void DoAddParameterLinkMenu(Rect rect, ReorderableList list)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), false, () =>
            {
                foreach (var parameter in missingParameterAutomations) AddParameterAutomation(parameter.Name);
            });

            menu.AddSeparator(string.Empty);

            foreach (var parameter in missingParameterAutomations)
            {
                var text = parameter.Name;

                if (InitialParameterValueExists(parameter.Name)) text += " (has initial value)";

                menu.AddItem(new GUIContent(text), false,
                    userData => { AddParameterAutomation(userData as string); },
                    parameter.Name);
            }

            menu.DropDown(rect);
        }

        private void DrawParameterLink(Rect rect, float labelRight, int index, bool active, bool focused)
        {
            if (editorEventRef == null) return;

            var linkProperty = parameterLinksProperty.GetArrayElementAtIndex(index);

            var name = linkProperty.FindPropertyRelative("Name").stringValue;

            var paramRef = editorEventRef.LocalParameters.FirstOrDefault(p => p.Name == name);

            if (paramRef == null) return;

            var slot = linkProperty.FindPropertyRelative("Slot").intValue;

            var slotName = string.Format("slot{0:D2}", slot);
            var valueProperty = parameterAutomationProperty.FindPropertyRelative(slotName);

            var slotStyle = GUI.skin.label;

            var slotRect = rect;
            slotRect.width = slotStyle.CalcSize(new GUIContent("slot 00:")).x;

            var nameRect = rect;
            nameRect.xMin = slotRect.xMax;
            nameRect.xMax = labelRight;

            var valueRect = rect;
            valueRect.xMin = nameRect.xMax;

            using (new EditorGUI.PropertyScope(rect, GUIContent.none, valueProperty))
            {
                GUI.Label(slotRect, string.Format("slot {0:D2}:", slot), slotStyle);
                GUI.Label(nameRect, name);

                using (new NoIndentScope())
                {
                    valueProperty.floatValue =
                        EditorGUI.Slider(valueRect, valueProperty.floatValue, paramRef.Min, paramRef.Max);
                }
            }
        }

        private bool InitialParameterValueExists(string name)
        {
            return parametersProperty.ArrayContains("Name", p => p.stringValue == name);
        }

        private bool ParameterLinkExists(string name)
        {
            return parameterLinksProperty.ArrayContains("Name", p => p.stringValue == name);
        }

        private void AddInitialParameterValue(EditorParamRef editorParamRef)
        {
            serializedObject.Update();

            if (!InitialParameterValueExists(editorParamRef.Name))
            {
                DeleteParameterAutomation(editorParamRef.Name);

                parametersProperty.ArrayAdd(p =>
                {
                    p.FindPropertyRelative("Name").stringValue = editorParamRef.Name;
                    p.FindPropertyRelative("Value").floatValue = editorParamRef.Default;
                });

                serializedObject.ApplyModifiedProperties();

                RefreshMissingParameterLists();
            }
        }

        private void DeleteInitialParameterValue(string name)
        {
            serializedObject.Update();

            var index = parametersProperty.FindArrayIndex("Name", p => p.stringValue == name);

            if (index >= 0) DeleteInitialParameterValue(index);
        }

        private void DeleteInitialParameterValue(int index)
        {
            serializedObject.Update();

            parametersProperty.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
            RefreshMissingParameterLists();
        }

        private void AddParameterAutomation(string name)
        {
            serializedObject.Update();

            if (!ParameterLinkExists(name))
            {
                var slot = -1;

                for (var i = 0; i < AutomatableSlots.Count; ++i)
                    if (!parameterLinksProperty.ArrayContains("Slot", p => p.intValue == i))
                    {
                        slot = i;
                        break;
                    }

                if (slot >= 0)
                {
                    DeleteInitialParameterValue(name);

                    parameterLinksProperty.ArrayAdd(p =>
                    {
                        p.FindPropertyRelative("Name").stringValue = name;
                        p.FindPropertyRelative("Slot").intValue = slot;
                    });

                    serializedObject.ApplyModifiedProperties();

                    RefreshMissingParameterLists();
                    RefreshTimelineEditor();
                }
            }
        }

        private static bool ClipHasCurves(TimelineClip clip)
        {
#if UNITY_2019_OR_NEWER
            return clip.hasCurves;
#else
            return clip.curves != null && !clip.curves.empty;
#endif
        }

        private void DeleteParameterAutomation(string name)
        {
            serializedObject.Update();

            var index = parameterLinksProperty.FindArrayIndex("Name", p => p.stringValue == name);

            if (index >= 0) DeleteParameterAutomation(index);
        }

        private void DeleteParameterAutomation(int index)
        {
            serializedObject.Update();

            if (ClipHasCurves(eventPlayable.OwningClip))
            {
                var linkProperty = parameterLinksProperty.GetArrayElementAtIndex(index);
                var slotProperty = linkProperty.FindPropertyRelative("Slot");

                var curvesClip = eventPlayable.OwningClip.curves;

                Undo.RecordObject(curvesClip, string.Empty);
                AnimationUtility.SetEditorCurve(curvesClip, GetParameterCurveBinding(slotProperty.intValue), null);
            }

            parameterLinksProperty.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            RefreshMissingParameterLists();

            RefreshTimelineEditor();
        }

        private static EditorCurveBinding GetParameterCurveBinding(int index)
        {
            var result = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(FMODEventPlayable),
                propertyName = string.Format("parameterAutomation.slot{0:D2}", index)
            };

            return result;
        }

        private static void RefreshTimelineEditor()
        {
#if UNITY_2018_3_OR_NEWER
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
#else
            object[] noParameters = new object[] { };

            Type timelineType = typeof(TimelineEditor);

            Assembly assembly = timelineType.Assembly;
            Type windowType = assembly.GetType("UnityEditor.Timeline.TimelineWindow");

            PropertyInfo windowInstanceProperty = windowType.GetProperty("instance");
            object windowInstance = windowInstanceProperty.GetValue(null, noParameters);

            if (windowInstance == null)
            {
                return;
            }

            PropertyInfo windowStateProperty = windowType.GetProperty("state");
            object windowState = windowStateProperty.GetValue(windowInstance, noParameters);

            if (windowState == null)
            {
                return;
            }

            Type windowStateType = windowState.GetType();
            MethodInfo refreshMethod = windowStateType.GetMethod("Refresh", new Type[] { });

            refreshMethod.Invoke(windowState, noParameters);
#endif
        }
    }
}
#endif