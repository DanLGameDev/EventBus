using System;
using System.Threading.Tasks;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public class EventBinding
    {
        internal readonly Action OnEventNoArgs = () => { };
        internal readonly Func<Task> OnEventNoArgsAsync = null;
        #if UNITASK_SUPPORT
        internal readonly Func<UniTask> OnEventNoArgsUniAsync = null;
        #endif
        internal int Priority { get; }
        
        protected EventBinding(int priority = 0)
        {
            Priority = priority;
        }

        protected EventBinding(Action eventNoArgs, int priority = 0) : this(priority)
        {
            if (eventNoArgs != null)
                OnEventNoArgs = eventNoArgs;
        }

        protected EventBinding(Func<Task> eventNoArgsAsync, int priority = 0) : this(priority)
        {
            if (eventNoArgsAsync != null)
                OnEventNoArgsAsync = eventNoArgsAsync;
        }
        
        #if UNITASK_SUPPORT
        protected EventBinding(Func<UniTask> eventNoArgsUniAsync, int priority = 0) : this(priority)
        {
            if (eventNoArgsUniAsync != null)
                OnEventNoArgsUniAsync = eventNoArgsUniAsync;
        }
        #endif

        public void Invoke()
        {
            OnEventNoArgs.Invoke();
        }

        public async Task InvokeAsync()
        {
            if (OnEventNoArgsAsync != null)
                await OnEventNoArgsAsync();
            #if UNITASK_SUPPORT
            else if (OnEventNoArgsUniAsync != null)
                await OnEventNoArgsUniAsync();
            #endif
            else
                OnEventNoArgs.Invoke();
        }

        #if UNITASK_SUPPORT
        public async UniTask InvokeUniAsync()
        {
            if (OnEventNoArgsUniAsync != null)
                await OnEventNoArgsUniAsync();
            else if (OnEventNoArgsAsync != null)
                await OnEventNoArgsAsync();
            else
                OnEventNoArgs.Invoke();
        }
        #endif
    }

    public class EventBinding<TEventType> : EventBinding where TEventType : IEvent
    {
        internal readonly Action<TEventType> OnEvent = _ => { };
        internal readonly Func<TEventType, Task> OnEventAsync = null;
        #if UNITASK_SUPPORT
        internal readonly Func<TEventType, UniTask> OnEventUniAsync = null;
        #endif

        public EventBinding(Action<TEventType> @event, int priority = 0)
            : base(priority)
        {
            if (@event != null)
                OnEvent = @event;
        }

        public EventBinding(Func<TEventType, Task> eventAsync, int priority = 0)
            : base(priority)
        {
            if (eventAsync != null)
                OnEventAsync = eventAsync;
        }

        #if UNITASK_SUPPORT
        public EventBinding(Func<TEventType, UniTask> eventUniAsync, int priority = 0)
            : base(priority)
        {
            if (eventUniAsync != null)
                OnEventUniAsync = eventUniAsync;
        }
        #endif

        public EventBinding(Action eventNoArgs, int priority = 0)
            : base(eventNoArgs, priority) { }

        public EventBinding(Func<Task> eventNoArgsAsync, int priority = 0)
            : base(eventNoArgsAsync, priority) { }

        #if UNITASK_SUPPORT
        public EventBinding(Func<UniTask> eventNoArgsUniAsync, int priority = 0)
            : base(eventNoArgsUniAsync, priority) { }
        #endif

        public EventBinding(Action<TEventType> @event, Action eventNoArgs, int priority = 0)
            : base(eventNoArgs, priority)
        {
            if (@event != null)
                OnEvent = @event;
        }

        public EventBinding(Func<TEventType, Task> eventAsync, Func<Task> eventNoArgsAsync, int priority = 0)
            : base(eventNoArgsAsync, priority)
        {
            if (eventAsync != null)
                OnEventAsync = eventAsync;
        }

        #if UNITASK_SUPPORT
        public EventBinding(Func<TEventType, UniTask> eventUniAsync, Func<UniTask> eventNoArgsUniAsync, int priority = 0)
            : base(eventNoArgsUniAsync, priority)
        {
            if (eventUniAsync != null)
                OnEventUniAsync = eventUniAsync;
        }
        #endif

        public void Invoke(TEventType @event)
        {
            base.Invoke();
            OnEvent.Invoke(@event);
        }

        public async Task InvokeAsync(TEventType @event)
        {
            await base.InvokeAsync();

            if (OnEventAsync != null)
                await OnEventAsync(@event);
            #if UNITASK_SUPPORT
            else if (OnEventUniAsync != null)
                await OnEventUniAsync(@event);
            #endif
            else
                OnEvent.Invoke(@event);
        }

        #if UNITASK_SUPPORT
        public async UniTask InvokeUniAsync(TEventType @event)
        {
            await base.InvokeUniAsync();

            if (OnEventUniAsync != null)
                await OnEventUniAsync(@event);
            else if (OnEventAsync != null)
                await OnEventAsync(@event);
            else
                OnEvent.Invoke(@event);
        }
        #endif
    }
}