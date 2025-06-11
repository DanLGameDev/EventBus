using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

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
        public EventBinding<TEvent> Register<TEvent>(EventBinding<TEvent> binding) where TEvent : IEvent
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
                
            return GetContainer<TEvent>().Register(binding);
        }

        public EventBinding<TEvent> Register<TEvent>(Action<TEvent> onEvent, int priority = 0) where TEvent : IEvent
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
        
            return GetContainer<TEvent>().Register(onEvent, priority);
        }

        public EventBinding<TEvent> Register<TEvent>(Func<TEvent, UniTask> onEventUniAsync, int priority = 0) where TEvent : IEvent
        {
            if (onEventUniAsync == null)
                throw new ArgumentNullException(nameof(onEventUniAsync));
                
            return GetContainer<TEvent>().Register(onEventUniAsync, priority);
        }

        public EventBinding<TEvent> Register<TEvent>(Action onEventNoArgs, int priority = 0) where TEvent : IEvent
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
                
            return GetContainer<TEvent>().Register(onEventNoArgs, priority);
        }

        public EventBinding<TEvent> Register<TEvent>(Func<UniTask> onEventNoArgsUniAsync, int priority = 0) where TEvent : IEvent
        {
            if (onEventNoArgsUniAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsUniAsync));
                
            return GetContainer<TEvent>().Register(onEventNoArgsUniAsync, priority);
        }
        
        public void Deregister<TEvent>(EventBinding<TEvent> binding) where TEvent : IEvent
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
                
            GetContainer<TEvent>().Deregister(binding);
        }
        
        public void Deregister<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
            
            GetContainer<TEvent>().Deregister(onEvent);
        }

        public void Deregister<TEvent>(Func<TEvent, UniTask> onEventUniAsync) where TEvent : IEvent
        {
            if (onEventUniAsync == null)
                throw new ArgumentNullException(nameof(onEventUniAsync));
                
            GetContainer<TEvent>().Deregister(onEventUniAsync);
        }
    
        public void Deregister<TEvent>(Action onEventNoArgs) where TEvent : IEvent
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
            
            GetContainer<TEvent>().Deregister(onEventNoArgs);
        }

        public void Deregister<TEvent>(Func<UniTask> onEventNoArgsUniAsync) where TEvent : IEvent
        {
            if (onEventNoArgsUniAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsUniAsync));
                
            GetContainer<TEvent>().Deregister(onEventNoArgsUniAsync);
        }
        
        public void ClearBindings<TEvent>() where TEvent : IEvent
        {
            GetContainer<TEvent>().ClearAllBindings();
        }
        
        public void ClearAllBindings()
        {
            foreach (var container in _containers.Values)
            {
                var method = container.GetType().GetMethod("ClearAllBindings");
                method?.Invoke(container, null);
            }
        }
        #endregion
        
        public void Raise<TEvent>(TEvent @event = default) where TEvent : IEvent
        {
            GetContainer<TEvent>().Raise(@event);
        }
        
        public async UniTask RaiseAsync<TEvent>(TEvent @event = default) where TEvent : IEvent
        {
            await GetContainer<TEvent>().RaiseAsync(@event);
        }

        public async UniTask RaiseSequentialAsync<TEvent>(TEvent @event = default) where TEvent : IEvent
        {
            await GetContainer<TEvent>().RaiseSequentialAsync(@event);
        }

        public async UniTask RaiseConcurrentAsync<TEvent>(TEvent @event = default) where TEvent : IEvent
        {
            await GetContainer<TEvent>().RaiseConcurrentAsync(@event);
        }
        
        public TEvent GetLastRaisedValue<TEvent>() where TEvent : IEvent
        {
            return GetContainer<TEvent>().LastRaisedValue;
        }
    }
}