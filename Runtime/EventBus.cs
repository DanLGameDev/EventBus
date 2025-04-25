using System;
using System.Collections.Generic;

namespace DGP.EventBus
{
    /// <summary>
    /// Represents a static event bus for events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of event to be handled, must implement IEvent.</typeparam>
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
        /// <param name="binding">The event binding to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="binding"/> is null.</exception>
        public static EventBinding<T> Register(EventBinding<T> binding) {
            return _eventBindingContainer.Register(binding);
        }
        
        /// <summary>
        /// Registers an action of a given event type to the EventBus and returns the binding
        /// </summary>
        /// <param name="onEvent">The Action<T> to invoke when the event occurs</param>
        /// <returns>The event binding created by this method</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEvent"/> is null.</exception>
        public static EventBinding<T> Register(Action<T> onEvent) {
            return _eventBindingContainer.Register(onEvent);
        }

        /// <summary>
        /// Registers an action with no arguments to the EventBus and returns the binding
        /// </summary>
        /// <param name="onEventNoArgs">The Action to invoke when the event occurs</param>
        /// <returns>The event binding created by this method</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEventNoArgs"/> is null.</exception>
        public static EventBinding<T> Register(Action onEventNoArgs) {
            return _eventBindingContainer.Register(onEventNoArgs);
        }

        /// <summary>
        /// De-registers an event binding from the EventBus
        /// </summary>
        /// <param name="binding">The binding to deregister</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="binding"/> is null.</exception>
        public static void Deregister(EventBinding<T> binding) {
            _eventBindingContainer.Deregister(binding);
        }
        
        /// <summary>
        /// De-registers an action handler from the EventBus
        /// </summary>
        /// <param name="onEvent">The action to deregister</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEvent"/> is null.</exception>
        public static void Deregister(Action<T> onEvent) {
            _eventBindingContainer.Deregister(onEvent);
        }
        
        /// <summary>
        /// De-registers an action with no arguments from the EventBus
        /// </summary>
        /// <param name="onEventNoArgs">The action to deregister</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEventNoArgs"/> is null.</exception>
        public static void Deregister(Action onEventNoArgs) {
            _eventBindingContainer.Deregister(onEventNoArgs);
        }
        
        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public static void ClearAllBindings() {
            _eventBindingContainer.ClearAllBindings();
        }
        #endregion
        
        /// <summary>
        /// Raises the event, invoking all registered event bindings
        /// </summary>
        /// <param name="event">The event to invoke</param>
        public static void Raise(T @event = default)
        {
            #if UNITY_EDITOR
            EventBusRegistry.RecordInvocation<T>();
            #endif
            
            _eventBindingContainer.Raise(@event);
        }
        
        /// <summary>
        /// Gets the last value raised on this event bus.
        /// </summary>
        /// <returns>The last raised value, or default if none.</returns>
        public static T GetLastRaisedValue()
        {
            return _eventBindingContainer.LastRaisedValue;
        }
    }
}