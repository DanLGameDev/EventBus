#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus.Bindings
{
    public interface IEventBinding
    {
        int Priority { get; }
    }
    
    public interface IEventBindingNoArgs : IEventBinding
    {
        void Invoke();
        
#if UNITASK_SUPPORT
        UniTask InvokeAsync();
#endif
    }
    
    public interface IEventBinding<in T> : IEventBinding where T : IEvent
    {
        void Invoke(T e);
        
#if UNITASK_SUPPORT
        UniTask InvokeAsync(T eventData);
#endif
    }
}