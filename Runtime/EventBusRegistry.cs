using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DGP.EventBus
{
    public static class EventBusRegistry
    {
        private static List<EventTypeBusBase> busses = new();
        public static IReadOnlyList<EventTypeBusBase> Busses => busses;
        
        private static Dictionary<string, float> busTimers = new();
        
        private static List<Type> busTypes = new();
        
        public static void ClearAllBuses() {
            Debug.Log("Cleaning up event buses");
            foreach (var busType in busTypes) {
                var method = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, null);
            }
            
            busTypes.Clear();
            busses.Clear();
            busTimers.Clear();
        }
        
        public static float GetLastInvocationTime(string name) {
            if (busTimers.ContainsKey(name)) {
                return busTimers[name];
            }
            return 0;
        }
        
        
        
        public static void RegisterBusType<T>() where T : IEvent {
            busses.Add(new EventBusType<T>());
            busTypes.Add(typeof(EventBus<T>));
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