using System;
using System.Collections.Generic;
using DGP.EventBus.Bindings;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public class EventContainer
    {
        private readonly Dictionary<Type, object> _containers = new();
        
        private EventBindingContainer<TEvent> GetContainer<TEvent>() where TEvent : IEvent
        {
            var type = typeof(TEvent);
            if (!_containers.TryGetValue(type, out var container))
            {
                container = new EventBindingContainer<TEvent>();
                _containers[type] = container;
            }
            
            return (EventBindingContainer<TEvent>)container;
        }
        
        #region Registration
        
        /// <summary>
        /// Registers a pre-created binding directly
        /// </summary>
        public TBinding Register<TEvent, TBinding>(TBinding binding) where TEvent : IEvent where TBinding : IEventBinding
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
                
            return GetContainer<TEvent>().Register(binding);
        }

        /// <summary>
        /// Registers a typed action handler
        /// </summary>
        public IEventBinding<TEvent> Register<TEvent>(Action<TEvent> onEvent, int priority = 0) where TEvent : IEvent
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
        
            return GetContainer<TEvent>().Register(onEvent, priority);
        }

        /// <summary>
        /// Registers a no-args action handler
        /// </summary>
        public IEventBindingNoArgs Register<TEvent>(Action onEventNoArgs, int priority = 0) where TEvent : IEvent
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
                
            return GetContainer<TEvent>().Register(onEventNoArgs, priority);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Registers a typed async handler
        /// </summary>
        public IEventBinding<TEvent> Register<TEvent>(Func<TEvent, UniTask> onEventUniAsync, int priority = 0) where TEvent : IEvent
        {
            if (onEventUniAsync == null)
                throw new ArgumentNullException(nameof(onEventUniAsync));
                
            return GetContainer<TEvent>().Register(onEventUniAsync, priority);
        }

        /// <summary>
        /// Registers a no-args async handler
        /// </summary>
        public IEventBindingNoArgs Register<TEvent>(Func<UniTask> onEventNoArgsUniAsync, int priority = 0) where TEvent : IEvent
        {
            if (onEventNoArgsUniAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsUniAsync));
                
            return GetContainer<TEvent>().Register(onEventNoArgsUniAsync, priority);
        }
        #endif
        
        /// <summary>
        /// Deregisters a binding directly
        /// </summary>
        public void Deregister<TEvent>(IEventBinding binding) where TEvent : IEvent
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
                
            GetContainer<TEvent>().Deregister(binding);
        }
        
        /// <summary>
        /// Deregisters a typed action handler
        /// </summary>
        public void Deregister<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
            
            GetContainer<TEvent>().Deregister(onEvent);
        }

        /// <summary>
        /// Deregisters a no-args action handler
        /// </summary>
        public void Deregister<TEvent>(Action onEventNoArgs) where TEvent : IEvent
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
            
            GetContainer<TEvent>().Deregister(onEventNoArgs);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Deregisters a typed async handler
        /// </summary>
        public void Deregister<TEvent>(Func<TEvent, UniTask> onEventUniAsync) where TEvent : IEvent
        {
            if (onEventUniAsync == null)
                throw new ArgumentNullException(nameof(onEventUniAsync));
                
            GetContainer<TEvent>().Deregister(onEventUniAsync);
        }

        /// <summary>
        /// Deregisters a no-args async handler
        /// </summary>
        public void Deregister<TEvent>(Func<UniTask> onEventNoArgsUniAsync) where TEvent : IEvent
        {
            if (onEventNoArgsUniAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsUniAsync));
                
            GetContainer<TEvent>().Deregister(onEventNoArgsUniAsync);
        }
        #endif
        
        /// <summary>
        /// Clears all bindings for a specific event type
        /// </summary>
        public void ClearBindings<TEvent>() where TEvent : IEvent
        {
            GetContainer<TEvent>().ClearAllBindings();
        }
        
        /// <summary>
        /// Clears all bindings for all event types
        /// </summary>
        public void ClearAllBindings()
        {
            foreach (var container in _containers.Values)
            {
                var method = container.GetType().GetMethod("ClearAllBindings");
                method?.Invoke(container, null);
            }
        }
        #endregion
        
        /// <summary>
        /// Raises the event synchronously
        /// </summary>
        public void Raise<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent
        {
            if (polymorphic)
            {
                var eventType = typeof(TEvent);
                foreach (var (registeredType, container) in _containers)
                {
                    if (registeredType.IsAssignableFrom(eventType))
                    {
                        // Check if event propagation should stop before invoking each container
                        if (@event is IStoppableEvent stoppable && stoppable.StopPropagation)
                            break;

                        var raiseMethod = container.GetType().GetMethod("Raise");
                        raiseMethod?.Invoke(container, new object[] { @event });
                    }
                }
            }
            else
            {
                GetContainer<TEvent>().Raise(@event);
            }
        }
        
        #if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public async UniTask RaiseAsync<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent
        {
            await RaiseSequentialAsync(@event, polymorphic);
        }

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public async UniTask RaiseSequentialAsync<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent
        {
            if (polymorphic)
            {
                var eventType = typeof(TEvent);
                foreach (var (registeredType, container) in _containers)
                {
                    if (registeredType.IsAssignableFrom(eventType))
                    {
                        if (@event is IStoppableEvent stoppable && stoppable.StopPropagation)
                            break;

                        var raiseMethod = container.GetType().GetMethod("RaiseSequentialAsync");
                        if (raiseMethod != null)
                        {
                            var task = (UniTask)raiseMethod.Invoke(container, new object[] { @event });
                            await task;
                        }
                    }
                }
            }
            else
            {
                await GetContainer<TEvent>().RaiseSequentialAsync(@event);
            }
        }

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings concurrently
        /// </summary>
        public async UniTask RaiseConcurrentAsync<TEvent>(TEvent @event = default, bool polymorphic = true) where TEvent : IEvent
        {
            if (polymorphic)
            {
                var eventType = typeof(TEvent);
                var tasks = new List<UniTask>();
                
                foreach (var (registeredType, container) in _containers)
                {
                    if (registeredType.IsAssignableFrom(eventType))
                    {
                        var raiseMethod = container.GetType().GetMethod("RaiseConcurrentAsync");
                        if (raiseMethod != null)
                        {
                            var task = (UniTask)raiseMethod.Invoke(container, new object[] { @event });
                            tasks.Add(task);
                        }
                    }
                }
                
                if (tasks.Count > 0)
                    await UniTask.WhenAll(tasks);
            }
            else
            {
                await GetContainer<TEvent>().RaiseConcurrentAsync(@event);
            }
        }
        #endif
    }
}