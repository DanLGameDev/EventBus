using System;
using System.Collections.Generic;
using System.Linq;
using DGP.EventBus.Bindings;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public class EventBindingContainer<T> where T : IEvent
    {
        private readonly List<IEventBinding> _bindings = new();
        private readonly List<IEventBinding> _bindingsPendingRemoval = new();

        private bool _isCurrentlyRaising;

        public IReadOnlyList<IEventBinding> Bindings => _bindings;
        public int Count => _bindings.Count;

        #region Registration

        /// <summary>
        /// Registers a typed action handler
        /// </summary>
        public IEventBinding<T> Register(Action<T> handler, int priority = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var existing = _bindings.OfType<TypedEventBinding<T>>()
                                   .FirstOrDefault(b => b.MatchesHandler(handler));
            if (existing != null)
                return existing;

            var binding = new TypedEventBinding<T>(handler, priority);
            _bindings.Add(binding);
            SortBindings();

            return binding;
        }

        /// <summary>
        /// Registers a no-args action handler
        /// </summary>
        public IEventBindingNoArgs Register(Action handler, int priority = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var existing = _bindings.OfType<NoArgsEventBinding>()
                                   .FirstOrDefault(b => b.MatchesHandler(handler));
            if (existing != null)
                return existing;

            var binding = new NoArgsEventBinding(handler, priority);
            _bindings.Add(binding);
            SortBindings();

            return binding;
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Registers a typed async handler
        /// </summary>
        public IEventBinding<T> Register(Func<T, UniTask> handler, int priority = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var existing = _bindings.OfType<TypedEventBinding<T>>()
                                   .FirstOrDefault(b => b.MatchesHandler(handler));
            if (existing != null)
                return existing;

            var binding = new TypedEventBinding<T>(handler, priority);
            _bindings.Add(binding);
            SortBindings();

            return binding;
        }

        /// <summary>
        /// Registers a no-args async handler
        /// </summary>
        public IEventBindingNoArgs Register(Func<UniTask> handler, int priority = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var existing = _bindings.OfType<NoArgsEventBinding>()
                                   .FirstOrDefault(b => b.MatchesHandler(handler));
            if (existing != null)
                return existing;

            var binding = new NoArgsEventBinding(handler, priority);
            _bindings.Add(binding);
            SortBindings();

            return binding;
        }
        #endif

        /// <summary>
        /// Registers a pre-created binding directly
        /// </summary>
        public TBinding Register<TBinding>(TBinding binding) where TBinding : IEventBinding
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));

            _bindings.Add(binding);
            SortBindings();

            return binding;
        }

        #endregion

        #region Deregistration

        /// <summary>
        /// Deregisters a typed action handler
        /// </summary>
        public void Deregister(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var binding = _bindings.OfType<TypedEventBinding<T>>()
                                  .FirstOrDefault(b => b.MatchesHandler(handler));
            if (binding != null)
                DeregisterBinding(binding);
        }

        /// <summary>
        /// Deregisters a no-args action handler
        /// </summary>
        public void Deregister(Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var binding = _bindings.OfType<NoArgsEventBinding>()
                                  .FirstOrDefault(b => b.MatchesHandler(handler));
            if (binding != null)
                DeregisterBinding(binding);
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Deregisters a typed async handler
        /// </summary>
        public void Deregister(Func<T, UniTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var binding = _bindings.OfType<TypedEventBinding<T>>()
                                  .FirstOrDefault(b => b.MatchesHandler(handler));
            if (binding != null)
                DeregisterBinding(binding);
        }

        /// <summary>
        /// Deregisters a no-args async handler
        /// </summary>
        public void Deregister(Func<UniTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var binding = _bindings.OfType<NoArgsEventBinding>()
                                  .FirstOrDefault(b => b.MatchesHandler(handler));
            if (binding != null)
                DeregisterBinding(binding);
        }
        #endif

        /// <summary>
        /// Deregisters a binding directly
        /// </summary>
        public void Deregister(IEventBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            DeregisterBinding(binding);
        }

        private void DeregisterBinding(IEventBinding binding)
        {
            if (_isCurrentlyRaising)
            {
                if (!_bindingsPendingRemoval.Contains(binding))
                    _bindingsPendingRemoval.Add(binding);
            }
            else
            {
                _bindings.Remove(binding);
            }
        }

        /// <summary>
        /// Clears all event bindings
        /// </summary>
        public void ClearAllBindings()
        {
            _bindings.Clear();
            _bindingsPendingRemoval.Clear();
        }

        #endregion

        #region Raising Events

        /// <summary>
        /// Raises the event synchronously, invoking all registered bindings sequentially
        /// </summary>
        public void Raise(T eventData = default)
        {
            _isCurrentlyRaising = true;

            InvokeBindingsSync(eventData);

            _isCurrentlyRaising = false;
            ProcessPendingRemovals();
        }

        #if UNITASK_SUPPORT
        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public async UniTask RaiseAsync(T eventData = default)
        {
            await RaiseSequentialAsync(eventData);
        }

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings sequentially
        /// </summary>
        public async UniTask RaiseSequentialAsync(T eventData = default)
        {
            _isCurrentlyRaising = true;

            await InvokeBindingsSequentialAsync(eventData);

            _isCurrentlyRaising = false;
            ProcessPendingRemovals();
        }

        /// <summary>
        /// Raises the event asynchronously, invoking all registered bindings concurrently
        /// </summary>
        public async UniTask RaiseConcurrentAsync(T eventData = default)
        {
            _isCurrentlyRaising = true;

            await InvokeBindingsConcurrentAsync(eventData);

            _isCurrentlyRaising = false;
            ProcessPendingRemovals();
        }
        #endif

        #endregion

        private void SortBindings()
        {
            _bindings.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        private void InvokeBindingsSync(T eventData)
        {
            // Create a snapshot to allow safe modification during iteration
            var bindingsSnapshot = _bindings.ToArray();
            
            foreach (var binding in bindingsSnapshot)
            {
                // Skip if binding was removed during iteration
                if (!_bindings.Contains(binding))
                    continue;

                // Check if event propagation should stop
                if (eventData is IStoppableEvent stoppable && stoppable.StopPropagation)
                    break;
                    
                if (binding is IEventBinding<T> typedBinding)
                {
                    typedBinding.Invoke(eventData);
                }
                else if (binding is IEventBindingNoArgs noArgsBinding)
                {
                    noArgsBinding.Invoke();
                }
            }
        }

        #if UNITASK_SUPPORT
        private async UniTask InvokeBindingsSequentialAsync(T eventData)
        {
            // Create a snapshot to allow safe modification during iteration
            var bindingsSnapshot = _bindings.ToArray();
            
            foreach (var binding in bindingsSnapshot)
            {
                // Skip if binding was removed during iteration
                if (!_bindings.Contains(binding))
                    continue;

                // Check if event propagation should stop
                if (eventData is IStoppableEvent stoppable && stoppable.StopPropagation)
                    break;
                    
                if (binding is IEventBinding<T> typedBinding)
                {
                    await typedBinding.InvokeAsync(eventData);
                }
                else if (binding is IEventBindingNoArgs noArgsBinding)
                {
                    await noArgsBinding.InvokeAsync();
                }
            }
        }

        private async UniTask InvokeBindingsConcurrentAsync(T eventData)
        {
            var bindingsSnapshot = _bindings.ToArray();
            
            var tasks = bindingsSnapshot.Select(binding =>
            {
                if (!_bindings.Contains(binding))
                    return UniTask.CompletedTask;
                    
                if (binding is IEventBinding<T> typedBinding)
                    return typedBinding.InvokeAsync(eventData);
                else if (binding is IEventBindingNoArgs noArgsBinding)
                    return noArgsBinding.InvokeAsync();
                else
                    return UniTask.CompletedTask;
            });

            await UniTask.WhenAll(tasks);
        }
        #endif

        private void ProcessPendingRemovals()
        {
            foreach (var binding in _bindingsPendingRemoval)
            {
                _bindings.Remove(binding);
            }

            _bindingsPendingRemoval.Clear();
        }
    }
}