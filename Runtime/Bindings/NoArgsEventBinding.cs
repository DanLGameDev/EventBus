using System;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus.Bindings
{
    public class NoArgsEventBinding : EventBinding, IEventBindingNoArgs
    {
        private readonly Action _syncHandler;
#if UNITASK_SUPPORT
        private readonly Func<UniTask> _asyncHandler;
#endif

        public NoArgsEventBinding(Action handler, int priority = 0) : base(priority)
        {
            _syncHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

#if UNITASK_SUPPORT
        public NoArgsEventBinding(Func<UniTask> handler, int priority = 0) : base(priority)
        {
            _asyncHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override Delegate Handler => (Delegate)_syncHandler ?? _asyncHandler;
        public override bool IsAsync => _asyncHandler != null;
#else
        public override Delegate Handler => _syncHandler;
        public override bool IsAsync => false;
#endif

        public void Invoke()
        {
#if UNITASK_SUPPORT
            if (_asyncHandler != null) {
                _asyncHandler().GetAwaiter().GetResult();
                return;
            }
#endif
            _syncHandler();
        }

#if UNITASK_SUPPORT
        public UniTask InvokeAsync()
        {
            if (_asyncHandler != null)
                return _asyncHandler();

            _syncHandler();
            return UniTask.CompletedTask;
        }
#endif
    }
}
