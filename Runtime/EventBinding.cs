using System;
using System.Threading.Tasks;

namespace DGP.EventBus
{
    public class EventBinding
    {
        internal readonly Action OnEventNoArgs = () => { };
        internal readonly Func<Task> OnEventNoArgsAsync = null;
        internal int Priority { get; }

        protected EventBinding(Action eventNoArgs, int priority = 0)
        {
            if (eventNoArgs != null)
                OnEventNoArgs = eventNoArgs;
            Priority = priority;
        }

        protected EventBinding(Func<Task> eventNoArgsAsync, int priority = 0)
        {
            if (eventNoArgsAsync != null)
                OnEventNoArgsAsync = eventNoArgsAsync;
            Priority = priority;
        }

        public void Invoke()
        {
            OnEventNoArgs.Invoke();
        }

        public async Task InvokeAsync()
        {
            if (OnEventNoArgsAsync != null)
                await OnEventNoArgsAsync();
            else
                OnEventNoArgs.Invoke();
        }
    }

    public class EventBinding<TEventType> : EventBinding where TEventType : IEvent
    {
        internal readonly Action<TEventType> OnEvent = _ => { };
        internal readonly Func<TEventType, Task> OnEventAsync = null;

        public EventBinding(Action<TEventType> @event, int priority = 0)
            : base(null, priority)
        {
            if (@event != null)
                OnEvent = @event;
        }

        public EventBinding(Func<TEventType, Task> eventAsync, int priority = 0)
            : base(null, priority)
        {
            if (eventAsync != null)
                OnEventAsync = eventAsync;
        }

        public EventBinding(Action eventNoArgs, int priority = 0)
            : base(eventNoArgs, priority) { }

        public EventBinding(Func<Task> eventNoArgsAsync, int priority = 0)
            : base(eventNoArgsAsync, priority) { }

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
            else
                OnEvent.Invoke(@event);
        }
    }
}