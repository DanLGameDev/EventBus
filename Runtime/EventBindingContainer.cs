using System;
using System.Collections.Generic;

namespace DGP.EventBus
{
    public class EventBindingContainer<T> where T : IEvent
    {
        private readonly HashSet<EventBinding<T>> _bindings = new();
        private readonly List<EventBinding<T>> _bindingsPendingRemoval = new();
        
        internal HashSet<EventBinding<T>> Bindings => _bindings;
        internal List<EventBinding<T>> BindingsPendingRemoval => _bindingsPendingRemoval;
      
        // ReSharper disable once StaticMemberInGenericType
        private static bool _isCurrentlyRaising;
        

        #region Registration
        /// <summary>
        /// Registers an EventBinding to the EventBus
        /// </summary>
        /// <param name="binding">The event binding to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="binding"/> is null.</exception>
        public EventBinding<T> Register(EventBinding<T> binding) {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            
            _bindings.Add(binding);
            return binding;
        }
        
        /// <summary>
        /// Registers an action of a given event type to the EventBus and returns the binding
        /// </summary>
        /// <param name="onEvent">The Action<T> to invoke when the event occurs</param>
        /// <returns>The event binding created by this method</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onEvent"/> is null.</exception>
        public EventBinding<T> Register(Action<T> onEvent) {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
            
            return Register(new EventBinding<T>(onEvent));
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
            
            return Register(new EventBinding<T>(onEventNoArgs));
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
        #endregion
        
        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public void ClearAllBindings() {
            _bindings.Clear();
            _bindingsPendingRemoval.Clear();
        }
        
        /// <summary>
        /// Raises the event, invoking all registered event bindings
        /// </summary>
        /// <param name="event">The event to invoke</param>
        public void Raise(T @event = default)
        {
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