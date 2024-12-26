using DGP.EventBus.Bridges;
using UnityEditor;
using UnityEngine;

namespace DGP.EventBus.Editor
{
    [CustomPropertyDrawer(typeof(BusEvent))]
    public class BusEventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var eventTypes = EventTypeUtils.GetAllEventTypes();
            var options = new string[eventTypes.Length + 1];
            var currentIndex = 0;

            var eventTypeProp = property.FindPropertyRelative("EventTypeName");
            
            options[0] = "None";
            
            for (var i = 0; i < eventTypes.Length; i++) {
                options[i + 1] = eventTypes[i].FullName;
                if (options[i + 1] == eventTypeProp.stringValue)
                    currentIndex = i + 1;
            }

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            EditorGUI.BeginChangeCheck();
            currentIndex = EditorGUI.Popup(position, currentIndex, options);

            if (EditorGUI.EndChangeCheck()) {
                eventTypeProp.stringValue = currentIndex > 0 ? options[currentIndex] : "";
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
}