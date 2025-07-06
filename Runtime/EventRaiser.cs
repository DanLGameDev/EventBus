using System;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus
{
    public class EventRaiser<T> where T : IEvent
    {
        private T _eventData;
        private EventContainer _targetContainer;
        private bool _useGlobalBus = true;
        private bool _polymorphic = true;
        private Func<bool> _condition;

        internal EventRaiser(T eventData)
        {
            _eventData = eventData;
        }

        /// <summary>
        /// Use a specific EventContainer instead of the global EventBus
        /// </summary>
        public EventRaiser<T> WithContainer(EventContainer container)
        {
            _targetContainer = container ?? throw new ArgumentNullException(nameof(container));
            _useGlobalBus = false;
            return this;
        }

        /// <summary>
        /// Use the global EventBus (default behavior)
        /// </summary>
        public EventRaiser<T> WithGlobalBus()
        {
            _useGlobalBus = true;
            _targetContainer = null;
            return this;
        }

        /// <summary>
        /// Set whether to use polymorphic raising (only applies to EventContainer)
        /// </summary>
        public EventRaiser<T> WithPolymorphic(bool polymorphic = true)
        {
            _polymorphic = polymorphic;
            return this;
        }

        /// <summary>
        /// Only raise the event if the condition returns true
        /// </summary>
        public EventRaiser<T> When(Func<bool> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            return this;
        }

        /// <summary>
        /// Raise the event synchronously
        /// </summary>
        public void RaiseSync()
        {
            if (_condition != null && !_condition())
                return;

            if (_useGlobalBus)
            {
                EventBus<T>.Raise(_eventData);
            }
            else
            {
                _targetContainer.Raise(_eventData, _polymorphic);
            }
        }

#if UNITASK_SUPPORT
        /// <summary>
        /// Raise the event asynchronously with sequential execution
        /// </summary>
        public async UniTask RaiseSequentialAsync()
        {
            if (_condition != null && !_condition())
                return;

            await (_useGlobalBus 
                ? EventBus<T>.RaiseSequentialAsync(_eventData)
                : _targetContainer.RaiseSequentialAsync(_eventData, _polymorphic));
        }

        /// <summary>
        /// Raise the event asynchronously with concurrent execution
        /// </summary>
        public async UniTask RaiseConcurrentAsync()
        {
            if (_condition != null && !_condition())
                return;

            await (_useGlobalBus 
                ? EventBus<T>.RaiseConcurrentAsync(_eventData)
                : _targetContainer.RaiseConcurrentAsync(_eventData, _polymorphic));
        }

        /// <summary>
        /// Alias for RaiseSequentialAsync (default async behavior)
        /// </summary>
        public UniTask RaiseAsync() => RaiseSequentialAsync();
#endif
    }
    
    public static class RaiseEvent
    {
        /// <summary>
        /// Create a builder for raising an event
        /// </summary>
        public static EventRaiser<T> Event<T>(T eventData = default) where T : IEvent
        {
            return new EventRaiser<T>(eventData);
        }
    }
}