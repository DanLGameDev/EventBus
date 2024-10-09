using System;
namespace DGP.EventBus
{
    /// <summary>
    /// Represents a binding for events of type <typeparamref name="TEventType"/>.
    /// This class allows for flexible event handling with or without event arguments.
    /// </summary>
    /// <typeparam name="TEventType">The type of event to be handled, must implement IEvent.</typeparam>
    public class EventBinding<TEventType> where TEventType : IEvent
    {
        internal readonly Action<TEventType> OnEvent = _ => { };
        internal readonly Action OnEventNoArgs = () => { };

#region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBinding{TEventType}"/> class with an event handler that takes event arguments.
        /// </summary>
        /// <param name="event">The event handler to be invoked with event arguments.</param>
        public EventBinding(Action<TEventType> @event) {
            if (@event != null)
                OnEvent = @event;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBinding{TEventType}"/> class with an event handler that takes no arguments.
        /// </summary>
        /// <param name="eventNoArgs">The event handler to be invoked without arguments.</param>
        public EventBinding(Action eventNoArgs) {
            if (eventNoArgs != null)
                OnEventNoArgs = eventNoArgs;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBinding{TEventType}"/> class with both types of event handlers.
        /// </summary>
        /// <param name="event">The event handler to be invoked with event arguments.</param>
        /// <param name="eventNoArgs">The event handler to be invoked without arguments.</param>
        public EventBinding(Action<TEventType> @event, Action eventNoArgs) {
            if (@event != null)
                OnEvent = @event;
            
            if (eventNoArgs != null)
                OnEventNoArgs = eventNoArgs;
        }
#endregion
        
        /// <summary>
        /// Invokes the bound event handlers.
        /// </summary>
        /// <param name="event">The event arguments to pass to the handler that accepts arguments.</param>
        public void Invoke(TEventType @event) {
            OnEvent.Invoke(@event);
            OnEventNoArgs.Invoke();
        }
    }
}