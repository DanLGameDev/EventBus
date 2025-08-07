using System;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus.Bindings
{
    public class TypedEventBinding<T> : IEventBinding<T> where T : IEvent
    {
        private readonly Action<T> _syncHandler;
#if UNITASK_SUPPORT
        private readonly Func<T, UniTask> _asyncHandler;
#endif
        
        public int Priority { get; }
        
        public TypedEventBinding(Action<T> handler, int priority = 0)
        {
            _syncHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            Priority = priority;
        }

#if UNITASK_SUPPORT
        public TypedEventBinding(Func<T, UniTask> handler, int priority = 0)
        {
            _asyncHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            Priority = priority;
        }
#endif

        public void Invoke(T e)
        {
            if (_syncHandler != null)
                _syncHandler(e);
#if UNITASK_SUPPORT
            else if (_asyncHandler != null)
                _asyncHandler(e).GetAwaiter().GetResult();
#endif
        }

#if UNITASK_SUPPORT
        public UniTask InvokeAsync(T eventData)
        {
            if (_asyncHandler != null) {
                return _asyncHandler(eventData);
            } else if (_syncHandler != null) {
                _syncHandler(eventData);
                return UniTask.CompletedTask;
            }
            
            return UniTask.CompletedTask;
        }
#endif
        
        /// <summary>
        /// Checks if this binding wraps the given delegate using method and target equality
        /// </summary>
        public bool MatchesHandler(Action<T> handler)
        {
            if (_syncHandler == null || handler == null)
                return false;
                
            // Use method and target equality instead of reference equality
            return _syncHandler.Method == handler.Method && 
                   ReferenceEquals(_syncHandler.Target, handler.Target);
        }

#if UNITASK_SUPPORT
        /// <summary>
        /// Checks if this binding wraps the given async delegate using method and target equality
        /// </summary>
        public bool MatchesHandler(Func<T, UniTask> handler)
        {
            if (_asyncHandler == null || handler == null)
                return false;
                
            // Use method and target equality instead of reference equality
            return _asyncHandler.Method == handler.Method && 
                   ReferenceEquals(_asyncHandler.Target, handler.Target);
        }
#endif
    }
}