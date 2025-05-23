using System;
using Cysharp.Threading.Tasks;

namespace DGP.EventBus
{
    public class EventBinding
    {
        internal readonly Action OnEventNoArgs = () => { };
        internal readonly Func<UniTask> OnEventNoArgsAsync = () => UniTask.CompletedTask;
        internal int Priority { get; }

        protected EventBinding(int priority = 0)
        {
            Priority = priority;
        }

        protected EventBinding(Action eventNoArgs, int priority = 0) : this(priority)
        {
            if (eventNoArgs != null)
            {
                OnEventNoArgs = eventNoArgs;
                OnEventNoArgsAsync = () => { eventNoArgs(); return UniTask.CompletedTask; };
            }
        }

        protected EventBinding(Func<UniTask> eventNoArgsUniAsync, int priority = 0) : this(priority)
        {
            if (eventNoArgsUniAsync != null)
                OnEventNoArgsAsync = eventNoArgsUniAsync;
        }

        public async UniTask InvokeAsync()
        {
            await OnEventNoArgsAsync();
        }
    }

    public class EventBinding<TEventType> : EventBinding where TEventType : IEvent
    {
        internal readonly Func<TEventType, UniTask> OnEvent = _ => UniTask.CompletedTask;

        public EventBinding(Action<TEventType> @event, int priority = 0)
            : base(priority)
        {
            if (@event != null)
                OnEvent = evt => {
                    @event(evt);
                    return UniTask.CompletedTask;
                };
        }

        public EventBinding(Func<TEventType, UniTask> eventUniAsync, int priority = 0)
            : base(priority)
        {
            if (eventUniAsync != null)
                OnEvent = eventUniAsync;
        }

        public EventBinding(Action eventNoArgs, int priority = 0)
            : base(eventNoArgs, priority) { }

        public EventBinding(Func<UniTask> eventNoArgsUniAsync, int priority = 0)
            : base(eventNoArgsUniAsync, priority) { }

        public EventBinding(Action<TEventType> @event, Action eventNoArgs, int priority = 0)
            : base(eventNoArgs, priority)
        {
            if (@event != null)
                OnEvent = evt => {
                    @event(evt);
                    return UniTask.CompletedTask;
                };
        }

        public EventBinding(Func<TEventType, UniTask> eventUniAsync, Func<UniTask> eventNoArgsUniAsync, int priority = 0)
            : base(eventNoArgsUniAsync, priority)
        {
            if (eventUniAsync != null)
                OnEvent = eventUniAsync;
        }

        public async UniTask InvokeAsync(TEventType @event)
        {
            await base.InvokeAsync();
            await OnEvent(@event);
        }
    }
}