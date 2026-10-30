using System;

namespace DGP.EventBus.Bindings
{
    public abstract class EventBinding : IEventBinding
    {
        public int Priority { get; }

        /// <summary>
        /// The delegate this binding was created from
        /// </summary>
        public abstract Delegate Handler { get; }

        /// <summary>
        /// True when the handler returns a UniTask
        /// </summary>
        public abstract bool IsAsync { get; }

        protected EventBinding(int priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// Checks if this binding wraps the given delegate using method and target equality
        /// </summary>
        public bool MatchesHandler(Delegate handler)
        {
            if (handler == null)
                return false;

            // A method group converts to a new delegate instance each time, so reference equality misses it
            return Handler.GetType() == handler.GetType()
                && Handler.Method == handler.Method
                && ReferenceEquals(Handler.Target, handler.Target);
        }
    }
}
