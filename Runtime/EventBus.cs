using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace DGP.EventBus
{
    public static class EventBus<T> where T : IEvent
    {
        internal static EventBindingContainer<T> _eventBindingContainer = new();
        internal static List<EventBinding<T>> Bindings => _eventBindingContainer.Bindings;
      
        static EventBus() {
#if UNITY_EDITOR
            EventBusRegistry.RegisterBusType<T>();
#endif
        }
        
        #region Registration
        /// <summary>
        /// Registers an EventBinding to the EventBus
        /// </summary>
        public static EventBinding<T> Register(EventBinding<T> binding) {
            return _eventBindingContainer.Register(binding);
        }
        
        /// <summary>
        /// Registers an action of a given event type to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Action<T> onEvent, int priority = 0) {
            return _eventBindingContainer.Register(onEvent, priority);
        }

        /// <summary>
        /// Registers a UniTask async function of a given event type to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Func<T, UniTask> onEventUniAsync, int priority = 0) {
            return _eventBindingContainer.Register(onEventUniAsync, priority);
        }

        /// <summary>
        /// Registers an action with no arguments to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Action onEventNoArgs, int priority = 0) {
            return _eventBindingContainer.Register(onEventNoArgs, priority);
        }

        /// <summary>
        /// Registers a UniTask async function with no arguments to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Func<UniTask> onEventNoArgsUniAsync, int priority = 0) {
            return _eventBindingContainer.Register(onEventNoArgsUniAsync, priority);
        }

        /// <summary>
        /// De-registers an event binding from the EventBus
        /// </summary>
        public static void Deregister(EventBinding<T> binding) {
            _eventBindingContainer.Deregister(binding);
        }
        
        /// <summary>
        /// De-registers an action handler from the EventBus
        /// </summary>
        public static void Deregister(Action<T> onEvent) {
            _eventBindingContainer.Deregister(onEvent);
        }

        /// <summary>
        /// De-registers a UniTask async function handler from the EventBus
        /// </summary>
        public static void Deregister(Func<T, UniTask> onEventUniAsync) {
            _eventBindingContainer.Deregister(onEventUniAsync);
        }
        
        /// <summary>
        /// De-registers an action with no arguments from the EventBus
        /// </summary>
        public static void Deregister(Action onEventNoArgs) {
            _eventBindingContainer.Deregister(onEventNoArgs);
        }

        /// <summary>
        /// De-registers a UniTask async function with no arguments from the EventBus
        /// </summary>
        public static void Deregister(Func<UniTask> onEventNoArgsUniAsync) {
            _eventBindingContainer.Deregister(onEventNoArgsUniAsync);
        }
        
        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public static void ClearAllBindings() {
            _eventBindingContainer.ClearAllBindings();
        }
        #endregion
        
        /// <summary>
        /// Raises the event, invoking all registered event bindings sequentially
        /// </summary>
        public static async UniTask RaiseAsync(T @event = default)
        {
            #if UNITY_EDITOR
            EventBusRegistry.RecordInvocation<T>();
            #endif
            
            await _eventBindingContainer.RaiseAsync(@event);
        }

        /// <summary>
        /// Raises the event, invoking all registered event bindings sequentially (one after another)
        /// </summary>
        public static async UniTask RaiseSequentialAsync(T @event = default)
        {
            #if UNITY_EDITOR
            EventBusRegistry.RecordInvocation<T>();
            #endif
            
            await _eventBindingContainer.RaiseSequentialAsync(@event);
        }

        /// <summary>
        /// Raises the event, invoking all registered event bindings concurrently (all at once)
        /// </summary>
        public static async UniTask RaiseConcurrentAsync(T @event = default)
        {
            #if UNITY_EDITOR
            EventBusRegistry.RecordInvocation<T>();
            #endif
            
            await _eventBindingContainer.RaiseConcurrentAsync(@event);
        }
        
        /// <summary>
        /// Gets the last value raised on this event bus.
        /// </summary>
        public static T GetLastRaisedValue()
        {
            return _eventBindingContainer.LastRaisedValue;
        }
    }
}