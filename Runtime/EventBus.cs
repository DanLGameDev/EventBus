using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

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
        /// Registers an async function of a given event type to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Func<T, Task> onEventAsync, int priority = 0) {
            return _eventBindingContainer.Register(onEventAsync, priority);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Registers a UniTask async function of a given event type to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Func<T, UniTask> onEventUniAsync, int priority = 0) {
            return _eventBindingContainer.Register(onEventUniAsync, priority);
        }
        #endif

        /// <summary>
        /// Registers an action with no arguments to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Action onEventNoArgs, int priority = 0) {
            return _eventBindingContainer.Register(onEventNoArgs, priority);
        }
        
        /// <summary>
        /// Registers an async function with no arguments to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Func<Task> onEventNoArgsAsync, int priority = 0) {
            return _eventBindingContainer.Register(onEventNoArgsAsync, priority);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Registers a UniTask async function with no arguments to the EventBus and returns the binding
        /// </summary>
        public static EventBinding<T> Register(Func<UniTask> onEventNoArgsUniAsync, int priority = 0) {
            return _eventBindingContainer.Register(onEventNoArgsUniAsync, priority);
        }
        #endif

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
        /// De-registers an async function handler from the EventBus
        /// </summary>
        public static void Deregister(Func<T, Task> onEventAsync) {
            _eventBindingContainer.Deregister(onEventAsync);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// De-registers a UniTask async function handler from the EventBus
        /// </summary>
        public static void Deregister(Func<T, UniTask> onEventUniAsync) {
            _eventBindingContainer.Deregister(onEventUniAsync);
        }
        #endif
        
        /// <summary>
        /// De-registers an action with no arguments from the EventBus
        /// </summary>
        public static void Deregister(Action onEventNoArgs) {
            _eventBindingContainer.Deregister(onEventNoArgs);
        }
        
        /// <summary>
        /// De-registers an async function with no arguments from the EventBus
        /// </summary>
        public static void Deregister(Func<Task> onEventNoArgsAsync) {
            _eventBindingContainer.Deregister(onEventNoArgsAsync);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// De-registers a UniTask async function with no arguments from the EventBus
        /// </summary>
        public static void Deregister(Func<UniTask> onEventNoArgsUniAsync) {
            _eventBindingContainer.Deregister(onEventNoArgsUniAsync);
        }
        #endif
        
        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public static void ClearAllBindings() {
            _eventBindingContainer.ClearAllBindings();
        }
        #endregion
        
        /// <summary>
        /// Raises the event, invoking all registered event bindings synchronously
        /// </summary>
        public static void Raise(T @event = default)
        {
            #if UNITY_EDITOR
            EventBusRegistry.RecordInvocation<T>();
            #endif
            
            _eventBindingContainer.Raise(@event);
        }
        
        /// <summary>
        /// Raises the event, invoking all registered event bindings asynchronously
        /// </summary>
        public static async Task RaiseAsync(T @event = default)
        {
            #if UNITY_EDITOR
            EventBusRegistry.RecordInvocation<T>();
            #endif
            
            await _eventBindingContainer.RaiseAsync(@event);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event, invoking all registered event bindings asynchronously with UniTask
        /// </summary>
        public static async UniTask RaiseUniAsync(T @event = default)
        {
            #if UNITY_EDITOR
            EventBusRegistry.RecordInvocation<T>();
            #endif
            
            await _eventBindingContainer.RaiseUniAsync(@event);
        }
        #endif
        
        /// <summary>
        /// Gets the last value raised on this event bus.
        /// </summary>
        public static T GetLastRaisedValue()
        {
            return _eventBindingContainer.LastRaisedValue;
        }
    }
}