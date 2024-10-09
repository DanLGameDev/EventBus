using System.Collections.Generic;
using System.Reflection;

namespace DGP.EventBus
{
    // Utility class for formatting and retrieving names of event bindings for EventBus<T>
    public class EventBusType<T> : EventTypeBusBase where T : IEvent
    {
        private const string DefaultConstructorName = "<.ctor>";
        
        public override int BindingCount => EventBus<T>.Bindings.Count;
        public override string Name => typeof(T).Name;
        
        public override IEnumerable<string> GetBindingNames() {
            foreach (var binding in EventBus<T>.Bindings)
            {
                if (binding.OnEventNoArgs != null && !binding.OnEventNoArgs.Method.Name.Contains(DefaultConstructorName))
                    yield return FormatMethodName(binding.OnEventNoArgs.Method, isNoArgs: true);
                else if (binding.OnEvent != null && !binding.OnEvent.Method.Name.Contains(DefaultConstructorName))
                    yield return FormatMethodName(binding.OnEvent.Method, isNoArgs: false);
                else
                    yield return "Unknown";
            }
        }
        
        private static string FormatMethodName(MethodInfo method, bool isNoArgs)
        {
            var declaringTypeName = method.DeclaringType?.Name ?? "UnknownType";
            var methodName = method.Name;
            var argsSuffix = isNoArgs ? "()" : "(args)";
            return $"{declaringTypeName}:{methodName}{argsSuffix}";
        }
    }
}
