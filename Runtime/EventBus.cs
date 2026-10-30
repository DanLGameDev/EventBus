using UnityEngine;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public static class EventBus
    {
        internal static readonly EventContainer Global = new();

        /// <summary>
        /// Raises the event on the global bus, including handlers registered for any base type or interface of the event
        /// </summary>
        public static void Raise<T>(T @event = default) where T : IEvent => Global.Raise(@event);

#if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event on the global bus asynchronously and polymorphically, invoking bindings sequentially
        /// </summary>
        public static UniTask RaiseAsync<T>(T @event = default) where T : IEvent => Global.RaiseSequentialAsync(@event);

        /// <summary>
        /// Raises the event on the global bus asynchronously and polymorphically, invoking bindings sequentially
        /// </summary>
        public static UniTask RaiseSequentialAsync<T>(T @event = default) where T : IEvent => Global.RaiseSequentialAsync(@event);

        /// <summary>
        /// Raises the event on the global bus asynchronously and polymorphically, invoking bindings concurrently
        /// </summary>
        public static UniTask RaiseConcurrentAsync<T>(T @event = default) where T : IEvent => Global.RaiseConcurrentAsync(@event);
#endif

        // With Domain Reload disabled, static state survives between play sessions
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearGlobalBindings() => Global.ClearAllBindings();
    }
}
