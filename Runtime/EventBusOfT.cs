using System;
using System.Collections.Generic;
using DGP.EventBus.Bindings;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public static class EventBus<T> where T : IEvent
    {
        public static readonly EventBindingContainer<T> BindingsContainer = new();
        
        static EventBus()
        {
            EventBus.RegisterContainer(BindingsContainer);
        }
        
        internal static IReadOnlyList<IEventBinding> Bindings => BindingsContainer.Bindings;
        
        #region Registration
        
        /// <summary>
        /// Registers a pre-created binding directly
        /// </summary>
        public static TBinding Register<TBinding>(TBinding binding) where TBinding : IEventBinding
        {
            return BindingsContainer.Register(binding);
        }
        
        /// <summary>
        /// Registers a typed action handler
        /// </summary>
        public static IEventBinding<T> Register(Action<T> onEvent, int priority = 0)
        {
            return BindingsContainer.Register(onEvent, priority);
        }

        /// <summary>
        /// Registers a no-args action handler
        /// </summary>
        public static IEventBindingNoArgs Register(Action onEventNoArgs, int priority = 0)
        {
            return BindingsContainer.Register(onEventNoArgs, priority);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Registers a typed async handler
        /// </summary>
        public static IEventBinding<T> Register(Func<T, UniTask> onEventUniAsync, int priority = 0)
        {
            return BindingsContainer.Register(onEventUniAsync, priority);
        }

        /// <summary>
        /// Registers a no-args async handler
        /// </summary>
        public static IEventBindingNoArgs Register(Func<UniTask> onEventNoArgsUniAsync, int priority = 0)
        {
            return BindingsContainer.Register(onEventNoArgsUniAsync, priority);
        }
        #endif

        /// <summary>
        /// Deregisters a binding directly
        /// </summary>
        public static void Deregister(IEventBinding binding)
        {
            BindingsContainer.Deregister(binding);
        }
        
        /// <summary>
        /// Deregisters a typed action handler
        /// </summary>
        public static void Deregister(Action<T> onEvent)
        {
            BindingsContainer.Deregister(onEvent);
        }

        /// <summary>
        /// Deregisters a no-args action handler
        /// </summary>
        public static void Deregister(Action onEventNoArgs)
        {
            BindingsContainer.Deregister(onEventNoArgs);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Deregisters a typed async handler
        /// </summary>
        public static void Deregister(Func<T, UniTask> onEventUniAsync)
        {
            BindingsContainer.Deregister(onEventUniAsync);
        }

        /// <summary>
        /// Deregisters a no-args async handler
        /// </summary>
        public static void Deregister(Func<UniTask> onEventNoArgsUniAsync)
        {
            BindingsContainer.Deregister(onEventNoArgsUniAsync);
        }
        #endif
        
        /// <summary>
        /// Clears all event bindings from the EventBus
        /// </summary>
        public static void ClearAllBindings()
        {
            BindingsContainer.ClearAllBindings();
        }
        
        #endregion
        
        /// <summary>
        /// Raises the event synchronously on this specific type only, invoking all registered bindings sequentially
        /// </summary>
		[Obsolete("Use RaiseAsync instead")]
        public static void Raise(T @event = default)
        {
            BindingsContainer.Raise(@event);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event asynchronously on this specific type only, invoking all registered bindings sequentially
        /// </summary>
        public static async UniTask RaiseAsync(T @event = default)
        {
            await BindingsContainer.RaiseAsync(@event);
        }

        /// <summary>
        /// Raises the event asynchronously on this specific type only, invoking all registered bindings sequentially
        /// </summary>
        public static async UniTask RaiseSequentialAsync(T @event = default)
        {
            await BindingsContainer.RaiseSequentialAsync(@event);
        }

        /// <summary>
        /// Raises the event asynchronously on this specific type only, invoking all registered bindings concurrently
        /// </summary>
        public static async UniTask RaiseConcurrentAsync(T @event = default)
        {
            await BindingsContainer.RaiseConcurrentAsync(@event);
        }
        #endif
    }
}