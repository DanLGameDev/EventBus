using System;
using DGP.EventBus.Bridges;

namespace DGP.EventBus.Editor
{
    [UnityEditor.CustomEditor(typeof(UnityEventBridge))]
    public class UnityEventBridgeEditor : UnityEditor.Editor
    {
        private Type[] _eventTypes;

        private void OnEnable()
        {
            _eventTypes = EventTypeUtils.GetAllEventTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            UnityEditor.EditorGUI.BeginChangeCheck();
            
            var options = new string[_eventTypes.Length + 1];
            var currentIndex = 0;
            
            options[0] = "None";
            
            for (var i = 0; i < _eventTypes.Length; i++)
            {
                options[i + 1] = _eventTypes[i].FullName;
                if (options[i + 1] == serializedObject.FindProperty("eventTypeName").stringValue)
                    currentIndex = i + 1;
            }

            currentIndex = UnityEditor.EditorGUILayout.Popup("Event Type", currentIndex, options);
            
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(target, "Change Event Type");
                serializedObject.FindProperty("eventTypeName").stringValue = 
                    currentIndex > 0 ? options[currentIndex] : "";
            }

            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("onEvent"));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}