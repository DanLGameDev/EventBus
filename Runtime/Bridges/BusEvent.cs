using UnityEngine;
using System;

namespace DGP.EventBus.Bridges
{
    [Serializable]
    public class BusEvent
    {
        [SerializeField] private string EventTypeName;
        
        private Type _cachedType;
        private EventBinding _currentBinding;
        private Action _callback;

        public void Register(Action callback)
        {
            if (string.IsNullOrEmpty(EventTypeName))
                return;
            
            _callback = callback;
            
            EnsureTypeIsCached();
            RegisterToEvent();
        }

        public void Deregister()
        {
            DeregisterFromEvent();
        }

        private void EnsureTypeIsCached()
        {
            if (_cachedType == null)
                _cachedType = EventTypeUtils.FindEventType(EventTypeName);
        }

        private void RegisterToEvent()
        {
            if (_cachedType == null)
                return;

            DeregisterFromEvent();

            var busType = typeof(EventBus<>).MakeGenericType(_cachedType);
            var registerMethod = busType.GetMethod("Register", new[] { typeof(Action) });

            if (registerMethod != null) _currentBinding = registerMethod.Invoke(null, new object[] { (Action)_callback }) as EventBinding; 
        }

        private void DeregisterFromEvent()
        {
            if (_cachedType == null || _currentBinding == null)
                return;

            var busType = typeof(EventBus<>).MakeGenericType(_cachedType);
            var deregisterMethod = busType.GetMethod("Deregister");
            deregisterMethod?.Invoke(null, new[] { _currentBinding });

            _currentBinding = null;
        }
    }
    
}