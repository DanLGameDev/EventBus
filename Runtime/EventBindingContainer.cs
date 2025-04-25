using System;
using System.Collections.Generic;

namespace DGP.EventBus
{
    public class EventBindingContainer<T> where T : IEvent
    {
        private readonly List<EventBinding<T>> _bindings = new();
        private readonly List<EventBinding<T>> _bindingsPendingRemoval = new();

        // Track when handlers are registered to avoid duplicate registrations
        private readonly Dictionary<Action<T>, EventBinding<T>> _registeredHandlers = new();
        private readonly Dictionary<Action, EventBinding<T>> _registeredNoArgHandlers = new();
        
        internal List<EventBinding<T>> Bindings => _bindings;
        
        private bool _isCurrentlyRaising;

        private T _lastRaisedValue = default(T);
        public T LastRaisedValue => _lastRaisedValue;
        
        #region Registration
        /// <summary>
        /// Registers an EventBinding to the EventBus
        /// </summary>
        /// <param name="binding">The event binding to register.</param>
        /// <param name="repeastLastRaisedValue">If true and this event has been previously raised
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="binding"/> is null.</exception>
        public EventBinding<T> Register(EventBinding<T> binding, bool repeastLastRaisedValue = false) {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            
            _bindings.Add(binding);
            
            if (repeastLastRaisedValue)
                binding.Invoke(_lastRaisedValue);
            
            return binding;
        }
        
        /// <summary>
        /// Registers an action of a given event type to the EventBus and returns the binding
        /// </summary>
        /// <param name="onEvent">The Action<T> to invoke when the event occurs</param>
        /// <returns>The event binding created by this method</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEvent"/> is null.</exception>
        public EventBinding<T> Register(Action<T> onEvent, bool repeastLastRaisedValue = false) {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
            
            // Check if we've already registered this handler
            if (_registeredHandlers.TryGetValue(onEvent, out var existingBinding))
                return existingBinding;
                
            var handler = new EventBinding<T>(onEvent);
            _registeredHandlers.Add(onEvent, handler);
            
            return Register(handler, repeastLastRaisedValue);
        }

        /// <summary>
        /// Registers an action with no arguments to the EventBus and returns the binding
        /// </summary>
        /// <param name="onEventNoArgs">The Action to invoke when the event occurs</param>
        /// <returns>The event binding created by this method</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEventNoArgs"/> is null.</exception>
        public EventBinding<T> Register(Action onEventNoArgs) {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
            
            // Check if we've already registered this handler
            if (_registeredNoArgHandlers.TryGetValue(onEventNoArgs, out var existingBinding))
                return existingBinding;
                
            var handler = new EventBinding<T>(onEventNoArgs);
            _registeredNoArgHandlers.Add(onEventNoArgs, handler);
            
            return Register(handler);
        }

        /// <summary>
        /// De-registers an event binding from the EventBus
        /// </summary>
        /// <param name="binding">The binding to deregister</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="binding"/> is null.</exception>
        public void Deregister(EventBinding<T> binding) {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            
            if (_isCurrentlyRaising) {
                if (!_bindingsPendingRemoval.Contains(binding))
                    _bindingsPendingRemoval.Add(binding);
                
                return;
            }
            
            _bindings.Remove(binding);
        }
        
        /// <summary>
        /// De-registers an action from the EventBus
        /// </summary>
        /// <param name="onEvent">The action to deregister</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEvent"/> is null.</exception>
        public void Deregister(Action<T> onEvent) {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
            
            if (_registeredHandlers.TryGetValue(onEvent, out var binding)) {
                Deregister(binding);
                _registeredHandlers.Remove(onEvent);
            }
        }
        
        /// <summary>
        /// De-registers an action with no arguments from the EventBus
        /// </summary>
        /// <param name="onEventNoArgs">The action to deregister</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEventNoArgs"/> is null.</exception>
        public void Deregister(Action onEventNoArgs) {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
            
            if (_registeredNoArgHandlers.TryGetValue(onEventNoArgs, out var binding)) {
                Deregister(binding);
                _registeredNoArgHandlers.Remove(onEventNoArgs);
            }
        }
        #endregion
        
        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public void ClearAllBindings() {
            _bindings.Clear();
            _bindingsPendingRemoval.Clear();
            _registeredHandlers.Clear();
            _registeredNoArgHandlers.Clear();
            
            _lastRaisedValue = default(T);
        }
        
        /// <summary>
        /// Raises the event, invoking all registered event bindings
        /// </summary>
        /// <param name="event">The event to invoke</param>
        public void Raise(T @event = default)
        {
            _lastRaisedValue = @event;
            _isCurrentlyRaising = true;
            InvokeBindings(@event);
            _isCurrentlyRaising = false;
            
            ProcessPendingRemovals();
        }

        private void InvokeBindings(T @event) {
            foreach (var binding in _bindings) {
                binding.Invoke(@event);
            }
        }
        
        private void ProcessPendingRemovals() {
            foreach (var binding in _bindingsPendingRemoval) {
                _bindings.Remove(binding);
            }
            
            _bindingsPendingRemoval.Clear();
        }
    }
}