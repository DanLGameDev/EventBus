using System;
using System.Collections.Generic;
using DGP.EventBus.Editor;

namespace DGP.EventBus.Runtime
{
    public static class EventBus<T> where T : IEvent
    {
        internal static readonly HashSet<EventBinding<T>> Bindings = new();
        private static readonly List<EventBinding<T>> bindingsPendingRemoval = new();
      
        // ReSharper disable once StaticMemberInGenericType
        private static bool _isCurrentlyRaising;

        public static EventBinding<T> Register(EventBinding<T> binding) {
            Bindings.Add(binding);
            return binding;
        }
        public static EventBinding<T> Register(Action<T> onEvent) => Register(new EventBinding<T>(onEvent));
        public static EventBinding<T> Register(Action onEventNoArgs) => Register(new EventBinding<T>(onEventNoArgs));
        
        public static void Deregister(EventBinding<T> binding) {
            if (_isCurrentlyRaising) {
                bindingsPendingRemoval.Add(binding);
                return;
            }
            Bindings.Remove(binding);
        }
        
        public static void Raise(T @event = default(T))
        {
            #if UNITY_EDITOR && ODIN_INSPECTOR
                EventBusRegistry.RecordInvocation<T>();
            #endif
            
            _isCurrentlyRaising = true;
            InvokeBindings(@event);
            _isCurrentlyRaising = false;
            
            ProcessPendingRemovals();
        }

        private static void InvokeBindings(T @event) {
            foreach (var binding in Bindings) {
                binding.Invoke(@event);
            }
        }
        
        private static void ProcessPendingRemovals() {
            foreach (var binding in bindingsPendingRemoval) {
                Bindings.Remove(binding);
            }
        }
            
        
#if UNITY_EDITOR
        static EventBus() {
            EventBusRegistry.RegisterBusType<T>();
        }
#endif
    }
}