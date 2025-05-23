using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace DGP.EventBus
{
    public class EventBindingContainer<T> where T : IEvent
    {
        private readonly List<EventBinding<T>> _bindings = new();
        private readonly List<EventBinding<T>> _bindingsPendingRemoval = new();

        // Track when handlers are registered to avoid duplicate registrations
        private readonly Dictionary<Action<T>, EventBinding<T>> _registeredHandlers = new();
        private readonly Dictionary<Func<T, UniTask>, EventBinding<T>> _registeredUniAsyncHandlers = new();
        private readonly Dictionary<Action, EventBinding<T>> _registeredNoArgHandlers = new();
        private readonly Dictionary<Func<UniTask>, EventBinding<T>> _registeredNoArgUniAsyncHandlers = new();

        internal List<EventBinding<T>> Bindings => _bindings;

        private bool _isCurrentlyRaising;

        private T _lastRaisedValue = default(T);
        public T LastRaisedValue => _lastRaisedValue;

        private bool _needsSorting = false;

        #region Registration

        /// <summary>
        /// Registers an EventBinding to the EventBus
        /// </summary>
        public EventBinding<T> Register(EventBinding<T> binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            _bindings.Add(binding);
            _needsSorting = true;

            return binding;
        }

        /// <summary>
        /// Registers an action of a given event type to the EventBus
        /// </summary>
        public EventBinding<T> Register(Action<T> onEvent, int priority = 0)
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));

            // Check if we've already registered this handler
            if (_registeredHandlers.TryGetValue(onEvent, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEvent, priority);
            _registeredHandlers.Add(onEvent, handler);
            _needsSorting = true;

            return Register(handler);
        }

        /// <summary>
        /// Registers a UniTask async function of a given event type to the EventBus
        /// </summary>
        public EventBinding<T> Register(Func<T, UniTask> onEventUniAsync, int priority = 0)
        {
            if (onEventUniAsync == null)
                throw new ArgumentNullException(nameof(onEventUniAsync));

            // Check if we've already registered this handler
            if (_registeredUniAsyncHandlers.TryGetValue(onEventUniAsync, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEventUniAsync, priority);
            _registeredUniAsyncHandlers.Add(onEventUniAsync, handler);
            _needsSorting = true;

            return Register(handler);
        }

        /// <summary>
        /// Registers an action with no arguments to the EventBus
        /// </summary>
        public EventBinding<T> Register(Action onEventNoArgs, int priority = 0)
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));

            // Check if we've already registered this handler
            if (_registeredNoArgHandlers.TryGetValue(onEventNoArgs, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEventNoArgs, priority);
            _registeredNoArgHandlers.Add(onEventNoArgs, handler);
            _needsSorting = true;

            return Register(handler);
        }

        /// <summary>
        /// Registers a UniTask async function with no arguments to the EventBus
        /// </summary>
        public EventBinding<T> Register(Func<UniTask> onEventNoArgsUniAsync, int priority = 0)
        {
            if (onEventNoArgsUniAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsUniAsync));

            // Check if we've already registered this handler
            if (_registeredNoArgUniAsyncHandlers.TryGetValue(onEventNoArgsUniAsync, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEventNoArgsUniAsync, priority);
            _registeredNoArgUniAsyncHandlers.Add(onEventNoArgsUniAsync, handler);
            _needsSorting = true;

            return Register(handler);
        }

        /// <summary>
        /// De-registers an event binding from the EventBus
        /// </summary>
        public void Deregister(EventBinding<T> binding)
        {
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
        /// De-registers an action handler from the EventBus
        /// </summary>
        public void Deregister(Action<T> onEvent)
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));

            if (_registeredHandlers.TryGetValue(onEvent, out var binding)) {
                Deregister(binding);
                _registeredHandlers.Remove(onEvent);
            }
        }

        /// <summary>
        /// De-registers a UniTask async function handler from the EventBus
        /// </summary>
        public void Deregister(Func<T, UniTask> onEventUniAsync)
        {
            if (onEventUniAsync == null)
                throw new ArgumentNullException(nameof(onEventUniAsync));

            if (_registeredUniAsyncHandlers.TryGetValue(onEventUniAsync, out var binding)) {
                Deregister(binding);
                _registeredUniAsyncHandlers.Remove(onEventUniAsync);
            }
        }

        /// <summary>
        /// De-registers an action with no arguments from the EventBus
        /// </summary>
        public void Deregister(Action onEventNoArgs)
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));

            if (_registeredNoArgHandlers.TryGetValue(onEventNoArgs, out var binding)) {
                Deregister(binding);
                _registeredNoArgHandlers.Remove(onEventNoArgs);
            }
        }

        /// <summary>
        /// De-registers a UniTask async function with no arguments from the EventBus
        /// </summary>
        public void Deregister(Func<UniTask> onEventNoArgsUniAsync)
        {
            if (onEventNoArgsUniAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsUniAsync));

            if (_registeredNoArgUniAsyncHandlers.TryGetValue(onEventNoArgsUniAsync, out var binding)) {
                Deregister(binding);
                _registeredNoArgUniAsyncHandlers.Remove(onEventNoArgsUniAsync);
            }
        }

        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public void ClearAllBindings()
        {
            _bindings.Clear();
            _bindingsPendingRemoval.Clear();
            _registeredHandlers.Clear();
            _registeredUniAsyncHandlers.Clear();
            _registeredNoArgHandlers.Clear();
            _registeredNoArgUniAsyncHandlers.Clear();

            _lastRaisedValue = default(T);
        }

        #endregion

        /// <summary>
        /// Raises the event, invoking all registered event bindings sequentially
        /// </summary>
        public async UniTask RaiseAsync(T @event = default)
        {
            _lastRaisedValue = @event;
            _isCurrentlyRaising = true;

            if (_needsSorting)
                SortBindings();

            await InvokeBindingsSequentialAsync(@event);

            _isCurrentlyRaising = false;

            ProcessPendingRemovals();
        }

        /// <summary>
        /// Raises the event, invoking all registered event bindings sequentially (one after another)
        /// </summary>
        public async UniTask RaiseSequentialAsync(T @event = default)
        {
            await RaiseAsync(@event);
        }

        /// <summary>
        /// Raises the event, invoking all registered event bindings concurrently (all at once)
        /// </summary>
        public async UniTask RaiseConcurrentAsync(T @event = default)
        {
            _lastRaisedValue = @event;
            _isCurrentlyRaising = true;

            if (_needsSorting)
                SortBindings();

            await InvokeBindingsConcurrentAsync(@event);

            _isCurrentlyRaising = false;

            ProcessPendingRemovals();
        }

        private void SortBindings()
        {
            _bindings.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            _needsSorting = false;
        }

        private async UniTask InvokeBindingsSequentialAsync(T @event)
        {
            foreach (var binding in _bindings) {
                await binding.InvokeAsync(@event);
            }
        }

        private async UniTask InvokeBindingsConcurrentAsync(T @event)
        {
            var tasks = _bindings.Select(async binding => await binding.InvokeAsync(@event));
            await UniTask.WhenAll(tasks);
        }

        private void ProcessPendingRemovals()
        {
            foreach (var binding in _bindingsPendingRemoval) {
                _bindings.Remove(binding);
            }

            _bindingsPendingRemoval.Clear();
        }
    }
}