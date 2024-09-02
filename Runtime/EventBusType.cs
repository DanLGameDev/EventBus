using System.Collections.Generic;

namespace DGP.EventBus
{
    public class EventBusType<T> : EventTypeBusBase where T : IEvent
    {
        public override int BindingCount => EventBus<T>.Bindings.Count;
        public override string Name => typeof(T).Name;
        public override List<string> GetBindingNames() {
            var bindings = EventBus<T>.Bindings;
            var names = new List<string>();
            foreach (var binding in bindings) {
                if (binding.OnEventNoArgs != null && (binding.OnEventNoArgs.Method.Name.Contains("<.ctor>")==false)) {
                    names.Add(binding.OnEventNoArgs.Method.DeclaringType?.Name + ":" + binding.OnEventNoArgs.Method.Name + "()");
                } else if (binding.OnEvent != null && (binding.OnEvent.Method.Name.Contains("<.ctor>")==false)) {
                    var method = binding.OnEvent.Method;
                    names.Add(method.DeclaringType?.Name + ":" + method.Name + "(args)");
                } else {
                    names.Add("Unknown");
                }
            }
            return names;
        }
    }
}
