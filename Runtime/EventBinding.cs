using System;
namespace DGP.EventBus
{
    public class EventBinding
    {
        internal readonly Action OnEventNoArgs = () => { };

        protected EventBinding(Action eventNoArgs) {
            if (eventNoArgs != null)
                OnEventNoArgs = eventNoArgs;
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
        
#region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBinding{TEventType}"/> class with an event handler that takes event arguments.
        /// </summary>
        /// <param name="event">The event handler to be invoked with event arguments.</param>
        public EventBinding(Action<TEventType> @event) : base(null) {
            if (@event != null)
                OnEvent = @event;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBinding{TEventType}"/> class with an event handler that takes no arguments.
        /// </summary>
        /// <param name="eventNoArgs">The event handler to be invoked without arguments.</param>
        public EventBinding(Action eventNoArgs) : base(eventNoArgs) { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBinding{TEventType}"/> class with both types of event handlers.
        /// </summary>
        /// <param name="event">The event handler to be invoked with event arguments.</param>
        /// <param name="eventNoArgs">The event handler to be invoked without arguments.</param>
        public EventBinding(Action<TEventType> @event, Action eventNoArgs) : base(eventNoArgs) {
            if (@event != null)
                OnEvent = @event;
        }
#endregion
        
        /// <summary>
        /// Invokes the bound event handlers.
        /// </summary>
        /// <param name="event">The event arguments to pass to the handler that accepts arguments.</param>
        public void Invoke(TEventType @event) {
            base.Invoke();
            OnEvent.Invoke(@event);
        }
    }
}