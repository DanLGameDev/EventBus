using System;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus.Bindings
{
    public class NoArgsEventBinding : IEventBindingNoArgs
    {
        private readonly Action _syncHandler;
#if UNITASK_SUPPORT
        private readonly Func<UniTask> _asyncHandler;
#endif
        
        public int Priority { get; }

        // Constructor for sync handlers
        public NoArgsEventBinding(Action handler, int priority = 0)
        {
            _syncHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            Priority = priority;
        }

#if UNITASK_SUPPORT
        // Constructor for async handlers
        public NoArgsEventBinding(Func<UniTask> handler, int priority = 0)
        {
            _asyncHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            Priority = priority;
        }
#endif

        public void Invoke()
        {
            if (_syncHandler != null)
            {
                _syncHandler();
            }
#if UNITASK_SUPPORT
            else if (_asyncHandler != null)
            {
                // Block on async handler for sync invocation
                _asyncHandler().GetAwaiter().GetResult();
            }
#endif
        }

#if UNITASK_SUPPORT
        public UniTask InvokeAsync()
        {
            if (_asyncHandler != null)
            {
                return _asyncHandler();
            }
            else if (_syncHandler != null)
            {
                _syncHandler();
                return UniTask.CompletedTask;
            }
            
            return UniTask.CompletedTask;
        }
#endif
        
        /// <summary>
        /// Checks if this binding wraps the given delegate
        /// </summary>
        public bool MatchesHandler(Action handler)
        {
            return ReferenceEquals(_syncHandler, handler);
        }

#if UNITASK_SUPPORT
        /// <summary>
        /// Checks if this binding wraps the given async delegate
        /// </summary>
        public bool MatchesHandler(Func<UniTask> handler)
        {
            return ReferenceEquals(_asyncHandler, handler);
        }
#endif
    }
}