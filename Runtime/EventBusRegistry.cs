using System.Collections.Generic;
using UnityEngine;

namespace DGP.EventBus
{
    public static class EventBusRegistry
    {
        private static List<EventTypeBusBase> busses = new();
        private static Dictionary<string, float> busTimers = new();
        
        public static float GetLastInvocationTime(string name) {
            if (busTimers.ContainsKey(name)) {
                return busTimers[name];
            }
            return 0;
        }
        
        public static IReadOnlyList<EventTypeBusBase> Busses => busses;
        public static void RegisterBusType<T>() where T : IEvent {
            busses.Add(new EventBusType<T>());
            busses.Sort((a, b) => a.Name.CompareTo(b.Name));
        }
        
        public static void RecordInvocation<T>() where T : IEvent {
            var name = typeof(T).Name;
            if (!busTimers.ContainsKey(name)) {
                busTimers[name] = 0;
            }
            busTimers[name] = Time.realtimeSinceStartup;
        }
    }
}