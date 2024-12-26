using System;
using UnityEngine;
using UnityEngine.Events;

namespace DGP.EventBus.Bridges
{
    public class UnityEventBridge : MonoBehaviour
    {
        [HideInInspector] public event Action OnEvent;
        
        [SerializeField] private string eventTypeName;
        [SerializeField] private UnityEvent onEvent;

        private Type _eventType;
        private EventBinding _currentBinding;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(eventTypeName))
                return;
            
            _eventType = EventTypeUtils.FindEventType(eventTypeName);
            SubscribeToEvent();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvent();
        }

        private void SubscribeToEvent()
        {
            if (_eventType == null)
                return;

            UnsubscribeFromEvent();

            var busType = typeof(EventBus<>).MakeGenericType(_eventType);
            var registerMethod = busType.GetMethod("Register", new[] { typeof(Action) });

            if (registerMethod != null) {
                _currentBinding = registerMethod.Invoke(null, new object[] { (Action)HandleEventInvocation })
                    as EventBinding;
            }
        }

        private void HandleEventInvocation()
        {
            onEvent?.Invoke();
            OnEvent?.Invoke();
        }

        private void UnsubscribeFromEvent()
        {
            if (_eventType == null || _currentBinding == null)
                return;

            var busType = typeof(EventBus<>).MakeGenericType(_eventType);
            var deregisterMethod = busType.GetMethod("Deregister");
            deregisterMethod?.Invoke(null, new[] { _currentBinding });

            _currentBinding = null;
        }
    }
    
    
}