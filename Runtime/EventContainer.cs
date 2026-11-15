using System;
using System.Collections.Generic;
using System.Linq;
using DGP.EventBus.Bindings;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public class EventContainer
    {
        private readonly Dictionary<Type, EventBindingContainer> _containers = new();
        private readonly Dictionary<Type, EventBindingContainer[]> _assignableContainers = new();

        internal event Action<EventBindingContainer> ContainerCreated;

        internal IReadOnlyCollection<EventBindingContainer> Containers => _containers.Values;

        internal EventBindingContainer<TEvent> GetContainer<TEvent>() where TEvent : IEvent
        {
            if (_containers.TryGetValue(typeof(TEvent), out var existing))
                return (EventBindingContainer<TEvent>)existing;

            var container = new EventBindingContainer<TEvent>();
            _containers[typeof(TEvent)] = container;
            _assignableContainers.Clear();

            ContainerCreated?.Invoke(container);
            return container;
        }

        #region Registration

        /// <summary>
        /// Registers a pre-created binding directly
        /// </summary>
        public TBinding Register<TEvent, TBinding>(TBinding binding) where TEvent : IEvent where TBinding : IEventBinding =>
            GetContainer<TEvent>().Register(binding);

        /// <summary>
        /// Registers a typed action handler
        /// </summary>
        public IEventBinding<TEvent> Register<TEvent>(Action<TEvent> onEvent, int priority = 0) where TEvent : IEvent =>
            GetContainer<TEvent>().Register(onEvent, priority);

        /// <summary>
        /// Registers a no-args action handler
        /// </summary>
        public IEventBindingNoArgs Register<TEvent>(Action onEventNoArgs, int priority = 0) where TEvent : IEvent =>
            GetContainer<TEvent>().Register(onEventNoArgs, priority);

#if UNITASK_SUPPORT
        /// <summary>
        /// Registers a typed async handler
        /// </summary>
        public IEventBinding<TEvent> Register<TEvent>(Func<TEvent, UniTask> onEventUniAsync, int priority = 0) where TEvent : IEvent =>
            GetContainer<TEvent>().Register(onEventUniAsync, priority);

        /// <summary>
        /// Registers a no-args async handler
        /// </summary>
        public IEventBindingNoArgs Register<TEvent>(Func<UniTask> onEventNoArgsUniAsync, int priority = 0) where TEvent : IEvent =>
            GetContainer<TEvent>().Register(onEventNoArgsUniAsync, priority);
#endif

        /// <summary>
        /// Deregisters a binding directly
        /// </summary>
        public void Deregister<TEvent>(IEventBinding binding) where TEvent : IEvent =>
            GetContainer<TEvent>().Deregister(binding);

        /// <summary>
        /// Deregisters a typed action handler
        /// </summary>
        public void Deregister<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent =>
            GetContainer<TEvent>().Deregister(onEvent);

        /// <summary>
        /// Deregisters a no-args action handler
        /// </summary>
        public void Deregister<TEvent>(Action onEventNoArgs) where TEvent : IEvent =>
            GetContainer<TEvent>().Deregister(onEventNoArgs);

#if UNITASK_SUPPORT
        /// <summary>
        /// Deregisters a typed async handler
        /// </summary>
        public void Deregister<TEvent>(Func<TEvent, UniTask> onEventUniAsync) where TEvent : IEvent =>
            GetContainer<TEvent>().Deregister(onEventUniAsync);

        /// <summary>
        /// Deregisters a no-args async handler
        /// </summary>
        public void Deregister<TEvent>(Func<UniTask> onEventNoArgsUniAsync) where TEvent : IEvent =>
            GetContainer<TEvent>().Deregister(onEventNoArgsUniAsync);
#endif

        /// <summary>
        /// Clears all bindings for a specific event type
        /// </summary>
        public void ClearBindings<TEvent>() where TEvent : IEvent =>
            GetContainer<TEvent>().ClearAllBindings();

        /// <summary>
        /// Clears all bindings for all event types
        /// </summary>
        public void ClearAllBindings()
        {
            foreach (var container in _containers.Values) {
                container.ClearAllBindings();
            }
        }

        #endregion

        #region Raising Events

        /// <summary>
        /// Raises the event synchronously. When polymorphic, handlers registered for the event's runtime type and any of its base types or interfaces are invoked
        /// </summary>
        public void Raise<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent
        {
            if (!polymorphic) {
                GetContainer<TEvent>().Raise(@event);
                return;
            }

            object boxedEvent = @event;
            foreach (var container in GetAssignableContainers(EventTypeOf(boxedEvent, typeof(TEvent)))) {
                if (EventBindingContainer.IsPropagationStopped(boxedEvent))
                    break;

                container.RaiseBoxed(boxedEvent);
            }
        }

#if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public UniTask RaiseAsync<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent =>
            RaiseSequentialAsync(@event, polymorphic);

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public async UniTask RaiseSequentialAsync<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent
        {
            if (!polymorphic) {
                await GetContainer<TEvent>().RaiseSequentialAsync(@event);
                return;
            }

            object boxedEvent = @event;
            foreach (var container in GetAssignableContainers(EventTypeOf(boxedEvent, typeof(TEvent)))) {
                if (EventBindingContainer.IsPropagationStopped(boxedEvent))
                    break;

                await container.RaiseSequentialBoxedAsync(boxedEvent);
            }
        }

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings concurrently
        /// </summary>
        public UniTask RaiseConcurrentAsync<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent
        {
            if (!polymorphic)
                return GetContainer<TEvent>().RaiseConcurrentAsync(@event);

            object boxedEvent = @event;
            return UniTask.WhenAll(GetAssignableContainers(EventTypeOf(boxedEvent, typeof(TEvent))).Select(c => c.RaiseConcurrentBoxedAsync(boxedEvent)));
        }
#endif

        #endregion

        // Polymorphic raises dispatch on the runtime type, so an event raised through a base-typed variable still reaches its concrete handlers
        private static Type EventTypeOf(object boxedEvent, Type declaredType) => boxedEvent?.GetType() ?? declaredType;

        // Returns an array rather than iterating _containers so a handler can touch a new event type mid-raise
        private EventBindingContainer[] GetAssignableContainers(Type eventType)
        {
            if (_assignableContainers.TryGetValue(eventType, out var cached))
                return cached;

            var assignable = _containers
                .Where(kvp => kvp.Key.IsAssignableFrom(eventType))
                .Select(kvp => kvp.Value)
                .ToArray();

            _assignableContainers[eventType] = assignable;
            return assignable;
        }
    }
}
