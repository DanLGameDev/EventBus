using System;
using System.Collections.Generic;
using DGP.EventBus.Bindings;

namespace DGP.EventBus
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, object> registeredContainers = new();
    
        internal static void RegisterContainer<T>(EventBindingContainer<T> container) where T : IEvent
        {
            registeredContainers[typeof(T)] = container;
        }
    
        public static void Raise<T>(T @event = default) where T : IEvent
        {
            var eventType = typeof(T);
            
            foreach (var (registeredType, container) in registeredContainers)
            {
                if (registeredType.IsAssignableFrom(eventType))
                {
                    if (@event is IStoppableEvent stoppable && stoppable.StopPropagation)
                        break;

                    var raiseMethod = container.GetType().GetMethod("Raise");
                    raiseMethod?.Invoke(container, new object[] { @event });
                }
            }
        }
    }
}