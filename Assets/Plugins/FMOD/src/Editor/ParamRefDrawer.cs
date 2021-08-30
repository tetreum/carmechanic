﻿using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomPropertyDrawer(typeof(ParamRefAttribute))]
    internal class ParamRefDrawer : PropertyDrawer
    {
        public bool MouseDrag(Event e)
        {
            var isDragging = false;

            if (e.type == EventType.DragPerform) isDragging = true;

            return isDragging;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var browseIcon = EditorGUIUtility.Load("FMOD/SearchIconBlack.png") as Texture;
            var openIcon = EditorGUIUtility.Load("FMOD/BrowserIcon.png") as Texture;
            var addIcon = EditorGUIUtility.Load("FMOD/AddIcon.png") as Texture;

            EditorGUI.BeginProperty(position, label, property);
            var pathProperty = property;

            var e = Event.current;
            if (MouseDrag(e) && position.Contains(e.mousePosition))
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorParamRef))
                {
                    pathProperty.stringValue = ((EditorParamRef) DragAndDrop.objectReferences[0]).Name;
                    GUI.changed = true;
                    e.Use();
                }

            if (e.type == EventType.DragUpdated && position.Contains(e.mousePosition))
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorParamRef))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    DragAndDrop.AcceptDrag();
                    e.Use();
                }

            var baseHeight = GUI.skin.textField.CalcSize(new GUIContent()).y;

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding.top = 1;
            buttonStyle.padding.bottom = 1;

            var addRect = new Rect(position.x + position.width - addIcon.width - 7, position.y, addIcon.width + 7,
                baseHeight);
            var openRect = new Rect(addRect.x - openIcon.width - 7, position.y, openIcon.width + 6, baseHeight);
            var searchRect = new Rect(openRect.x - browseIcon.width - 9, position.y, browseIcon.width + 8, baseHeight);
            var pathRect = new Rect(position.x, position.y, searchRect.x - position.x - 3, baseHeight);

            EditorGUI.PropertyField(pathRect, pathProperty, GUIContent.none);

            if (GUI.Button(searchRect, new GUIContent(browseIcon, "Search"), buttonStyle))
            {
                var eventBrowser = ScriptableObject.CreateInstance<EventBrowser>();

                eventBrowser.ChooseParameter(property);
                var windowRect = position;
                windowRect.position = GUIUtility.GUIToScreenPoint(windowRect.position);
                windowRect.height = openRect.height + 1;
                eventBrowser.ShowAsDropDown(windowRect, new Vector2(windowRect.width, 400));
            }
        }
    }
}