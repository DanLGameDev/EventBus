using System;
using System.Collections.Generic;

namespace DGP.EventBus
{
    public class EventTypeUtils
    {
        private static Type[] CachedEventTypes;
        
        public static Type[] GetAllEventTypes()
        {
            if (CachedEventTypes != null)
                return CachedEventTypes;
                
            var eventTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IEvent).IsAssignableFrom(type) && !type.IsInterface)
                        eventTypes.Add(type);
                }
            }
            
            CachedEventTypes = eventTypes.ToArray();
            return CachedEventTypes;
        }
        
        public static Type FindEventType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
                
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var eventType = assembly.GetType(typeName);
                if (eventType != null && typeof(IEvent).IsAssignableFrom(eventType))
                    return eventType;
            }
            
            return null;
        }
    }
}