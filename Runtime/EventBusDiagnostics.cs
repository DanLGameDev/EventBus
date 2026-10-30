using System;
using System.Collections.Generic;

namespace DGP.EventBus
{
    public static class EventBusDiagnostics
    {
        /// <summary>
        /// Binding containers created on the global bus, in creation order
        /// </summary>
        public static IReadOnlyCollection<EventBindingContainer> GlobalContainers => EventBus.Global.Containers;

        /// <summary>
        /// Fired the first time an event type is used on the global bus
        /// </summary>
        public static event Action<EventBindingContainer> GlobalContainerCreated
        {
            add => EventBus.Global.ContainerCreated += value;
            remove => EventBus.Global.ContainerCreated -= value;
        }

        /// <summary>
        /// Fired immediately before handlers are invoked for any raise, on any bus
        /// </summary>
        public static event Action<Type> EventRaised;

        internal static void NotifyEventRaised(Type eventType) => EventRaised?.Invoke(eventType);
    }
}
