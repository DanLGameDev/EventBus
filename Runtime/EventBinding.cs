using System;

namespace DGP.EventBus.Runtime
{
    public class EventBinding<TEventType> where TEventType : IEvent
    {
        internal readonly Action<TEventType> OnEvent = _ => { };
        internal readonly Action OnEventNoArgs = () => { };
        
        public void Invoke(TEventType @event) {
            OnEvent.Invoke(@event);
            OnEventNoArgs.Invoke();
        }

        public EventBinding(Action<TEventType> onEvent) {
            OnEvent = onEvent;
        }
        
        public EventBinding(Action onEventNoArgs) {
            OnEventNoArgs = onEventNoArgs;
        }
        
        public EventBinding(Action<TEventType> onEvent, Action onEventNoArgs) {
            OnEvent = onEvent;
            OnEventNoArgs = onEventNoArgs;
        }
    }
}