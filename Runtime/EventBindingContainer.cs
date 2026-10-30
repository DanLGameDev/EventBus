using System;
using System.Collections.Generic;
using System.Linq;
using DGP.EventBus.Bindings;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public abstract class EventBindingContainer
    {
        private readonly List<IEventBinding> _bindings = new();
        private readonly List<IEventBinding> _bindingsPendingRemoval = new();

        private int _raiseDepth;

        public abstract Type EventType { get; }
        public IReadOnlyList<IEventBinding> Bindings => _bindings;
        public int Count => _bindings.Count;

        /// <summary>
        /// Deregisters a binding directly. During a raise, removal is deferred until the raise completes
        /// </summary>
        public void Deregister(IEventBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));

            if (_raiseDepth == 0) {
                _bindings.Remove(binding);
                return;
            }

            if (!_bindingsPendingRemoval.Contains(binding))
                _bindingsPendingRemoval.Add(binding);
        }

        /// <summary>
        /// Clears all event bindings
        /// </summary>
        public void ClearAllBindings()
        {
            _bindings.Clear();
            _bindingsPendingRemoval.Clear();
        }

        internal abstract void RaiseBoxed(object eventData);
#if UNITASK_SUPPORT
        internal abstract UniTask RaiseSequentialBoxedAsync(object eventData);
        internal abstract UniTask RaiseConcurrentBoxedAsync(object eventData);
#endif

        internal static bool IsPropagationStopped<TEvent>(TEvent eventData) =>
            eventData is IStoppableEvent stoppable && stoppable.StopPropagation;

        private protected TBinding AddBinding<TBinding>(TBinding binding) where TBinding : IEventBinding
        {
            // Insert after every binding of equal or higher priority so equal priorities keep registration order
            var index = _bindings.FindLastIndex(b => b.Priority >= binding.Priority) + 1;
            _bindings.Insert(index, binding);
            return binding;
        }

        private protected EventBinding FindBinding(Delegate handler) =>
            _bindings.OfType<EventBinding>().FirstOrDefault(b => b.MatchesHandler(handler));

        private protected bool IsRegistered(IEventBinding binding) => _bindings.Contains(binding);

        private protected IEventBinding[] BeginRaise()
        {
            EventBusDiagnostics.NotifyEventRaised(EventType);
            _raiseDepth++;
            return _bindings.ToArray();
        }

        private protected void EndRaise()
        {
            _raiseDepth--;
            if (_raiseDepth > 0)
                return;

            foreach (var binding in _bindingsPendingRemoval) {
                _bindings.Remove(binding);
            }

            _bindingsPendingRemoval.Clear();
        }
    }

    public class EventBindingContainer<T> : EventBindingContainer where T : IEvent
    {
        public override Type EventType => typeof(T);

        #region Registration

        /// <summary>
        /// Registers a typed action handler
        /// <param name="handler">The handler to register</param>
        /// <param name="priority">The priority of the handler (higher values are invoked first)</param>
        /// </summary>
        public IEventBinding<T> Register(Action<T> handler, int priority = 0) =>
            RegisterHandler(handler, () => new TypedEventBinding<T>(handler, priority));

        /// <summary>
        /// Registers a no-args action handler
        /// <param name="handler">The handler to register</param>
        /// <param name="priority">The priority of the handler (higher values are invoked first)</param>
        /// </summary>
        public IEventBindingNoArgs Register(Action handler, int priority = 0) =>
            RegisterHandler(handler, () => new NoArgsEventBinding(handler, priority));

#if UNITASK_SUPPORT
        /// <summary>
        /// Registers a typed async handler
        /// <param name="handler">The handler to register</param>
        /// <param name="priority">The priority of the handler (higher values are invoked first)</param>
        /// </summary>
        public IEventBinding<T> Register(Func<T, UniTask> handler, int priority = 0) =>
            RegisterHandler(handler, () => new TypedEventBinding<T>(handler, priority));

        /// <summary>
        /// Registers a no-args async handler
        /// <param name="handler">The handler to register</param>
        /// <param name="priority">The priority of the handler (higher values are invoked first)</param>
        /// </summary>
        public IEventBindingNoArgs Register(Func<UniTask> handler, int priority = 0) =>
            RegisterHandler(handler, () => new NoArgsEventBinding(handler, priority));
#endif

        /// <summary>
        /// Registers a pre-created binding directly. The binding must implement IEventBinding&lt;T&gt; or IEventBindingNoArgs
        /// </summary>
        public TBinding Register<TBinding>(TBinding binding) where TBinding : IEventBinding
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (!(binding is IEventBinding<T>) && !(binding is IEventBindingNoArgs))
                throw new ArgumentException($"Binding cannot be invoked for {typeof(T).Name}", nameof(binding));

            return AddBinding(binding);
        }

        #endregion

        #region Deregistration

        /// <summary>
        /// Deregisters a typed action handler
        /// </summary>
        public void Deregister(Action<T> handler) => DeregisterHandler(handler);

        /// <summary>
        /// Deregisters a no-args action handler
        /// </summary>
        public void Deregister(Action handler) => DeregisterHandler(handler);

#if UNITASK_SUPPORT
        /// <summary>
        /// Deregisters a typed async handler
        /// </summary>
        public void Deregister(Func<T, UniTask> handler) => DeregisterHandler(handler);

        /// <summary>
        /// Deregisters a no-args async handler
        /// </summary>
        public void Deregister(Func<UniTask> handler) => DeregisterHandler(handler);
#endif

        #endregion

        #region Raising Events

        /// <summary>
        /// Raises the event synchronously, invoking all registered bindings sequentially
        /// </summary>
        public void Raise(T eventData = default)
        {
            var snapshot = BeginRaise();

            try {
                foreach (var binding in snapshot) {
                    if (!IsRegistered(binding))
                        continue;

                    if (IsPropagationStopped(eventData))
                        break;

                    Invoke(binding, eventData);
                }
            } finally {
                EndRaise();
            }
        }

#if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public UniTask RaiseAsync(T eventData = default) => RaiseSequentialAsync(eventData);

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public async UniTask RaiseSequentialAsync(T eventData = default)
        {
            var snapshot = BeginRaise();

            try {
                foreach (var binding in snapshot) {
                    if (!IsRegistered(binding))
                        continue;

                    if (IsPropagationStopped(eventData))
                        break;

                    await InvokeAsync(binding, eventData);
                }
            } finally {
                EndRaise();
            }
        }

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings concurrently
        /// </summary>
        public async UniTask RaiseConcurrentAsync(T eventData = default)
        {
            var snapshot = BeginRaise();

            try {
                // Lazy so a sync handler that deregisters a later binding still prevents it starting
                await UniTask.WhenAll(snapshot.Where(IsRegistered).Select(b => InvokeAsync(b, eventData)));
            } finally {
                EndRaise();
            }
        }
#endif

        #endregion

        internal override void RaiseBoxed(object eventData) => Raise((T)eventData);

#if UNITASK_SUPPORT
        internal override UniTask RaiseSequentialBoxedAsync(object eventData) => RaiseSequentialAsync((T)eventData);
        internal override UniTask RaiseConcurrentBoxedAsync(object eventData) => RaiseConcurrentAsync((T)eventData);
#endif

        private TBinding RegisterHandler<TBinding>(Delegate handler, Func<TBinding> createBinding)
            where TBinding : class, IEventBinding
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return FindBinding(handler) as TBinding ?? AddBinding(createBinding());
        }

        private void DeregisterHandler(Delegate handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var binding = FindBinding(handler);
            if (binding != null)
                Deregister(binding);
        }

        private static void Invoke(IEventBinding binding, T eventData)
        {
            if (binding is IEventBinding<T> typedBinding)
                typedBinding.Invoke(eventData);
            else if (binding is IEventBindingNoArgs noArgsBinding)
                noArgsBinding.Invoke();
        }

#if UNITASK_SUPPORT
        private static UniTask InvokeAsync(IEventBinding binding, T eventData)
        {
            if (binding is IEventBinding<T> typedBinding)
                return typedBinding.InvokeAsync(eventData);

            if (binding is IEventBindingNoArgs noArgsBinding)
                return noArgsBinding.InvokeAsync();

            return UniTask.CompletedTask;
        }
#endif
    }
}
