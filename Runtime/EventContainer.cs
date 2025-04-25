using System;
using System.Collections.Generic;

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

        public EventBinding<TEvent> Register<TEvent>(Action<TEvent> onEvent, bool repeatLastRaisedValue = false) where TEvent : IEvent
        {
            if (onEvent == null)
                throw new ArgumentNullException(nameof(onEvent));
                
            return GetContainer<TEvent>().Register(onEvent, repeatLastRaisedValue);
        }

        public EventBinding<TEvent> Register<TEvent>(Action onEventNoArgs) where TEvent : IEvent
        {
            if (onEventNoArgs == null)
                throw new ArgumentNullException(nameof(onEventNoArgs));
                
            return GetContainer<TEvent>().Register(onEventNoArgs);
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
    }
}