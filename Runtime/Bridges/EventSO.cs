using System;
using UnityEngine;

namespace DGP.EventBus.Bridges
{
    [CreateAssetMenu(fileName = "EventBusBridge", menuName = "DGP/EventBus/Event Bridge")]
    public class EventSO : ScriptableObject
    {
        public event Action OnEvent;
        
        [SerializeField] private string eventTypeName;

        internal Type EventType;
        internal EventBinding CurrentBinding;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(eventTypeName))
                return;
            
            EventType = EventTypeUtils.FindEventType(eventTypeName);
        }

        private void OnDisable()
        {
            UnsubscribeFromEvent();
        }

        public void SubscribeToEvent()
        {
            if (EventType == null)
                return;

            UnsubscribeFromEvent();

            var busType = typeof(EventBus<>).MakeGenericType(EventType);
            var registerMethod = busType.GetMethod("Register", new[] { typeof(Action) });

            if (registerMethod != null) {
                // call the HandleEventInvocation method when the event is raised
                CurrentBinding = registerMethod.Invoke(null, new object[] { (Action)HandleEventInvocation })
                    as EventBinding;
            }
        }

        private void HandleEventInvocation()
        {
            OnEvent?.Invoke();
        }

        private void UnsubscribeFromEvent()
        {
            if (EventType == null || CurrentBinding == null)
                return;

            var busType = typeof(EventBus<>).MakeGenericType(EventType);
            var deregisterMethod = busType.GetMethod("Deregister");
            deregisterMethod?.Invoke(null, new[] { CurrentBinding });

            CurrentBinding = null;
        }
    }
    
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EventSO))]
    public class EventBusBridgeEditor : UnityEditor.Editor
    {
        private System.Type[] EventTypes;

        private void OnEnable()
        {
            EventTypes = EventTypeUtils.GetAllEventTypes();
        }

        public override void OnInspectorGUI()
        {
            var bridge = (EventSO)target;
            UnityEditor.EditorGUI.BeginChangeCheck();
            
            var options = new string[EventTypes.Length];
            var currentIndex = 0;
            
            for (var i = 0; i < EventTypes.Length; i++)
            {
                options[i] = EventTypes[i].FullName;
                if (options[i] == serializedObject.FindProperty("eventTypeName").stringValue)
                    currentIndex = i;
            }

            currentIndex = UnityEditor.EditorGUILayout.Popup("Event Type", currentIndex, options);
            
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(bridge, "Change Event Type");
                serializedObject.FindProperty("eventTypeName").stringValue = options[currentIndex];
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
    #endif
}