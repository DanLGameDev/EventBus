using System;
namespace DGP.EventBus
{
    public class EventBinding
    {
        internal readonly Action OnEventNoArgs = () => { };
        internal int Priority { get; }

        protected EventBinding(Action eventNoArgs, int priority = 0) {
            if (eventNoArgs != null)
                OnEventNoArgs = eventNoArgs;
            Priority = priority;
        }
    
        public void Invoke() {
            OnEventNoArgs.Invoke();
        }
    }
    
    
    /// <summary>
    /// Represents a binding for events of type <typeparamref name="TEventType"/>.
    /// This class allows for flexible event handling with or without event arguments.
    /// </summary>
    /// <typeparam name="TEventType">The type of event to be handled, must implement IEvent.</typeparam>
    public class EventBinding<TEventType> : EventBinding where TEventType : IEvent
    {
        internal readonly Action<TEventType> OnEvent = _ => { };
        
        public EventBinding(Action<TEventType> @event, int priority = 0) : base(null, priority) {
            if (@event != null)
                OnEvent = @event;
        }
    
        public EventBinding(Action eventNoArgs, int priority = 0) : base(eventNoArgs, priority) { }
    
        public EventBinding(Action<TEventType> @event, Action eventNoArgs, int priority = 0) : base(eventNoArgs, priority) {
            if (@event != null)
                OnEvent = @event;
        }
    
        public void Invoke(TEventType @event) {
            base.Invoke();
            OnEvent.Invoke(@event);
        }
    }
}