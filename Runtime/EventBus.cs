using System;
using System.Collections.Generic;

namespace DGP.EventBus
{
    public static class EventBus<T> where T : IEvent
    {
        internal static readonly HashSet<EventBinding<T>> Bindings = new();
        private static readonly List<EventBinding<T>> BindingsPendingRemoval = new();
      
        // ReSharper disable once StaticMemberInGenericType
        private static bool _isCurrentlyRaising;
        
#if UNITY_EDITOR
        static EventBus() {
            EventBusRegistry.RegisterBusType<T>();
        }
#endif

        /// <summary>
        /// Registers an EventBinding to the EventBus
        /// </summary>
        /// <param name="binding">The event binding to register</param>
        public static EventBinding<T> Register(EventBinding<T> binding) {
            Bindings.Add(binding);
            return binding;
        }
        
        /// <summary>
        /// Registers an action of a given event type to the EventBus and returns the binding
        /// </summary>
        /// <param name="onEvent">The Action<T> to invoke when the event occurs</param>
        /// <returns>The event binding created by this method</returns>
        public static EventBinding<T> Register(Action<T> onEvent) => Register(new EventBinding<T>(onEvent));
        
        /// <summary>
        /// Registers an action with no arguments to the EventBus and returns the binding
        /// </summary>
        /// <param name="onEventNoArgs">The Action to invoke when the event occurs</param>
        /// <returns>The event binding created by this method</returns>
        public static EventBinding<T> Register(Action onEventNoArgs) => Register(new EventBinding<T>(onEventNoArgs));
        
        /// <summary>
        /// De-registers an event binding from the EventBus
        /// </summary>
        /// <param name="binding">The binding to deregister</param>
        public static void Deregister(EventBinding<T> binding) {
            if (_isCurrentlyRaising) {
                BindingsPendingRemoval.Add(binding);
                return;
            }
            Bindings.Remove(binding);
        }
        
        /// <summary>
        /// Raises the event, invoking all registered event bindings
        /// </summary>
        /// <param name="event">The event to invoke</param>
        public static void Raise(T @event = default(T))
        {
            #if UNITY_EDITOR
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
            foreach (var binding in BindingsPendingRemoval) {
                Bindings.Remove(binding);
            }
        }
    }
}