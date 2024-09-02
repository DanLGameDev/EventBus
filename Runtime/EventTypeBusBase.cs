using System.Collections.Generic;

namespace DGP.EventBus
{
    public abstract class EventTypeBusBase {
        public abstract int BindingCount { get; }
        public abstract string Name { get; }
        public abstract List<string> GetBindingNames();
    }
}