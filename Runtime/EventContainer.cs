using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public EventBinding<TEvent> Register<TEvent>(EventBinding<TEvent> binding, bool repeatLastRaisedValue = false) where TEvent : IEvent
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
                
            return GetContainer<TEvent>().Register(binding, repeatLastRaisedValue);
        }

        public EventBinding<TEvent> Register<TEvent>(Action<TEvent> onEvent, int priority = 0, bool repeatLastRaisedValue = false) where TEvent : IEvent
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
        
            // Use the container's register method directly instead of creating a new binding first
            return GetContainer<TEvent>().Register(onEvent, priority, repeatLastRaisedValue);
        }

        public EventBinding<TEvent> Register<TEvent>(Action onEventNoArgs, int priority = 0) where TEvent : IEvent
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
                
            return GetContainer<TEvent>().Register(onEventNoArgs, priority);
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
    
        public void Deregister<TEvent>(Action onEventNoArgs) where TEvent : IEvent
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
            
            GetContainer<TEvent>().Deregister(onEventNoArgs);
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
        
        public TEvent GetLastRaisedValue<TEvent>() where TEvent : IEvent
        {
            return GetContainer<TEvent>().LastRaisedValue;
        }
        
        
        // Added async registration methods
        public EventBinding<TEvent> Register<TEvent>(Func<TEvent, Task> onEventAsync, int priority = 0, bool repeatLastRaisedValue = false) where TEvent : IEvent
        {
            if (onEventAsync == null)
                throw new ArgumentNullException(nameof(onEventAsync));
                
            return GetContainer<TEvent>().Register(onEventAsync, priority, repeatLastRaisedValue);
        }
        
        public EventBinding<TEvent> Register<TEvent>(Func<Task> onEventNoArgsAsync, int priority = 0) where TEvent : IEvent
        {
            if (onEventNoArgsAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsAsync));
                
            return GetContainer<TEvent>().Register(onEventNoArgsAsync, priority);
        }
        
        public void Deregister<TEvent>(Func<TEvent, Task> onEventAsync) where TEvent : IEvent
        {
            if (onEventAsync == null)
                throw new ArgumentNullException(nameof(onEventAsync));
                
            GetContainer<TEvent>().Deregister(onEventAsync);
        }
        
        public void Deregister<TEvent>(Func<Task> onEventNoArgsAsync) where TEvent : IEvent
        {
            if (onEventNoArgsAsync == null)
                throw new ArgumentNullException(nameof(onEventNoArgsAsync));
                
            GetContainer<TEvent>().Deregister(onEventNoArgsAsync);
        }
        
        // Added async raise method
        public async Task RaiseAsync<TEvent>(TEvent @event = default) where TEvent : IEvent
        {
            await GetContainer<TEvent>().RaiseAsync(@event);
        }
    }
}