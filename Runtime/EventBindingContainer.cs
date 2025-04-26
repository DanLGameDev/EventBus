using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public class EventBindingContainer<T> where T : IEvent
    {
        private readonly List<EventBinding<T>> _bindings = new();
        private readonly List<EventBinding<T>> _bindingsPendingRemoval = new();

        // Track when handlers are registered to avoid duplicate registrations
        private readonly Dictionary<Action<T>, EventBinding<T>> _registeredHandlers = new();
        private readonly Dictionary<Func<T, Task>, EventBinding<T>> _registeredAsyncHandlers = new();
        #if UNITASK_SUPPORT
        private readonly Dictionary<Func<T, UniTask>, EventBinding<T>> _registeredUniAsyncHandlers = new();
        #endif
        private readonly Dictionary<Action, EventBinding<T>> _registeredNoArgHandlers = new();
        private readonly Dictionary<Func<Task>, EventBinding<T>> _registeredNoArgAsyncHandlers = new();
        #if UNITASK_SUPPORT
        private readonly Dictionary<Func<UniTask>, EventBinding<T>> _registeredNoArgUniAsyncHandlers = new();
        #endif

        internal List<EventBinding<T>> Bindings => _bindings;

        private bool _isCurrentlyRaising;

        private T _lastRaisedValue = default(T);
        public T LastRaisedValue => _lastRaisedValue;

        private bool _needsSorting = false;

        #region Registration

        /// <summary>
        /// Registers an EventBinding to the EventBus
        /// </summary>
        public EventBinding<T> Register(EventBinding<T> binding, bool repeatLastRaisedValue = false)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            _bindings.Add(binding);
            _needsSorting = true;

            if (repeatLastRaisedValue)
                binding.Invoke(_lastRaisedValue);

            return binding;
        }

        /// <summary>
        /// Registers an action of a given event type to the EventBus
        /// </summary>
        public EventBinding<T> Register(Action<T> onEvent, int priority = 0, bool repeatLastRaisedValue = false)
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));

            // Check if we've already registered this handler
            if (_registeredHandlers.TryGetValue(onEvent, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEvent, priority);
            _registeredHandlers.Add(onEvent, handler);
            _needsSorting = true;

            return Register(handler, repeatLastRaisedValue);
        }

        /// <summary>
        /// Registers an async function of a given event type to the EventBus
        /// </summary>
        public EventBinding<T> Register(Func<T, Task> onEventAsync, int priority = 0, bool repeatLastRaisedValue = false)
        {
            if (onEventAsync == null)
                throw new ArgumentNullException(nameof(onEventAsync));

            // Check if we've already registered this handler
            if (_registeredAsyncHandlers.TryGetValue(onEventAsync, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEventAsync, priority);
            _registeredAsyncHandlers.Add(onEventAsync, handler);
            _needsSorting = true;

            if (repeatLastRaisedValue)
                handler.InvokeAsync(_lastRaisedValue).ConfigureAwait(false);

            return Register(handler, false); // We already handled repetition if needed
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Registers a UniTask async function of a given event type to the EventBus
        /// </summary>
        public EventBinding<T> Register(Func<T, UniTask> onEventUniAsync, int priority = 0, bool repeatLastRaisedValue = false)
        {
            if (onEventUniAsync == null)
                throw new ArgumentNullException(nameof(onEventUniAsync));

            // Check if we've already registered this handler
            if (_registeredUniAsyncHandlers.TryGetValue(onEventUniAsync, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEventUniAsync, priority);
            _registeredUniAsyncHandlers.Add(onEventUniAsync, handler);
            _needsSorting = true;

            if (repeatLastRaisedValue)
                handler.InvokeUniAsync(_lastRaisedValue).Forget();

            return Register(handler, false); // We already handled repetition if needed
        }
        #endif

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
        /// Registers an async function with no arguments to the EventBus
        /// </summary>
        public EventBinding<T> Register(Func<Task> onEventNoArgsAsync, int priority = 0)
        {
            if (onEventNoArgsAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsAsync));

            // Check if we've already registered this handler
            if (_registeredNoArgAsyncHandlers.TryGetValue(onEventNoArgsAsync, out var existingBinding))
                return existingBinding;

            var handler = new EventBinding<T>(onEventNoArgsAsync, priority);
            _registeredNoArgAsyncHandlers.Add(onEventNoArgsAsync, handler);
            _needsSorting = true;

            return Register(handler);
        }

        #if UNITASK_SUPPORT
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
        #endif

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
        /// De-registers an async function handler from the EventBus
        /// </summary>
        public void Deregister(Func<T, Task> onEventAsync)
        {
            if (onEventAsync == null)
                throw new ArgumentNullException(nameof(onEventAsync));

            if (_registeredAsyncHandlers.TryGetValue(onEventAsync, out var binding)) {
                Deregister(binding);
                _registeredAsyncHandlers.Remove(onEventAsync);
            }
        }

        #if UNITASK_SUPPORT
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
        #endif

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
        /// De-registers an async function with no arguments from the EventBus
        /// </summary>
        public void Deregister(Func<Task> onEventNoArgsAsync)
        {
            if (onEventNoArgsAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsAsync));

            if (_registeredNoArgAsyncHandlers.TryGetValue(onEventNoArgsAsync, out var binding)) {
                Deregister(binding);
                _registeredNoArgAsyncHandlers.Remove(onEventNoArgsAsync);
            }
        }

        #if UNITASK_SUPPORT
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
        #endif

        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public void ClearAllBindings()
        {
            _bindings.Clear();
            _bindingsPendingRemoval.Clear();
            _registeredHandlers.Clear();
            _registeredAsyncHandlers.Clear();
            #if UNITASK_SUPPORT
            _registeredUniAsyncHandlers.Clear();
            #endif
            _registeredNoArgHandlers.Clear();
            _registeredNoArgAsyncHandlers.Clear();
            #if UNITASK_SUPPORT
            _registeredNoArgUniAsyncHandlers.Clear();
            #endif

            _lastRaisedValue = default(T);
        }

        #endregion

        /// <summary>
        /// Raises the event, invoking all registered event bindings synchronously
        /// </summary>
        public void Raise(T @event = default)
        {
            _lastRaisedValue = @event;
            _isCurrentlyRaising = true;

            if (_needsSorting)
                SortBindings();

            InvokeBindings(@event);

            _isCurrentlyRaising = false;

            ProcessPendingRemovals();
        }

        /// <summary>
        /// Raises the event, invoking all registered event bindings asynchronously
        /// </summary>
        public async Task RaiseAsync(T @event = default)
        {
            _lastRaisedValue = @event;
            _isCurrentlyRaising = true;

            if (_needsSorting)
                SortBindings();

            await InvokeBindingsAsync(@event);

            _isCurrentlyRaising = false;

            ProcessPendingRemovals();
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event, invoking all registered event bindings asynchronously with UniTask
        /// </summary>
        public async UniTask RaiseUniAsync(T @event = default)
        {
            _lastRaisedValue = @event;
            _isCurrentlyRaising = true;

            if (_needsSorting)
                SortBindings();

            await InvokeBindingsUniAsync(@event);

            _isCurrentlyRaising = false;

            ProcessPendingRemovals();
        }
        #endif

        private void SortBindings()
        {
            _bindings.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            _needsSorting = false;
        }

        private void InvokeBindings(T @event)
        {
            foreach (var binding in _bindings) {
                binding.Invoke(@event);
            }
        }

        private async Task InvokeBindingsAsync(T @event)
        {
            foreach (var binding in _bindings) {
                await binding.InvokeAsync(@event);
            }
        }

        #if UNITASK_SUPPORT
        private async UniTask InvokeBindingsUniAsync(T @event)
        {
            foreach (var binding in _bindings) {
                await binding.InvokeUniAsync(@event);
            }
        }
        #endif

        private void ProcessPendingRemovals()
        {
            foreach (var binding in _bindingsPendingRemoval) {
                _bindings.Remove(binding);
            }

            _bindingsPendingRemoval.Clear();
        }
    }
}