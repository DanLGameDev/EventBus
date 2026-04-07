using System;
using System.Collections.Generic;

namespace DGP.EventBus.Bridges
{
    /// <summary>
    /// Runtime-safe communication layer between the EventBus runtime and editor tooling.
    /// Has no dependency on DGP.EventBus — containers are stored as object to avoid a
    /// circular assembly reference. Editor tools read container state via reflection.
    /// </summary>
    public static class EventBusBridge
    {
        private static readonly Dictionary<Type, object> _containers = new();

        /// <summary>
        /// All registered event containers keyed by event type.
        /// Values are EventBindingContainer&lt;T&gt; instances stored as object.
        /// </summary>
        public static IReadOnlyDictionary<Type, object> RegisteredContainers => _containers;

        /// <summary>
        /// Fired when a new EventBindingContainer is registered (i.e. the first time a
        /// particular EventBus&lt;T&gt; is accessed). Parameters: (eventType, container).
        /// </summary>
        public static event Action<Type, object> OnContainerRegistered;

        /// <summary>
        /// Fired immediately before handlers are invoked for a raise call.
        /// </summary>
        public static event Action<Type> OnEventRaised;

        public static void NotifyContainerRegistered(Type eventType, object container)
        {
            _containers[eventType] = container;
            OnContainerRegistered?.Invoke(eventType, container);
        }

        public static void NotifyEventRaised(Type eventType)
        {
            OnEventRaised?.Invoke(eventType);
        }
    }
}
