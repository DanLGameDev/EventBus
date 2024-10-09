using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DGP.EventBus
{
    /// <summary>
    /// Manages and tracks different event bus types.
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly List<EventTypeBusBase> Buses = new();
        public static IReadOnlyList<EventTypeBusBase> RegisteredBuses => Buses;
        private static readonly Dictionary<string, float> LastInvocationTimes = new();
        private static readonly List<Type> BusTypes = new();
        
        /// <summary>
        /// Clears all registered buses and their associated data.
        /// </summary>
        public static void ClearAllBuses() {
            foreach (var busType in BusTypes) {
                var method = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, null);
            }
            
            BusTypes.Clear();
            Buses.Clear();
            LastInvocationTimes.Clear();
        }
        
        /// <summary>
        /// Gets the last invocation time for a given bus name.
        /// </summary>
        /// <param name="name">The name of the bus.</param>
        /// <returns>The last invocation time, or 0 if not found.</returns>
        public static float GetLastInvocationTime(string name) => LastInvocationTimes.GetValueOrDefault(name, 0);

        /// <summary>
        /// Registers a new bus type.
        /// </summary>
        /// <typeparam name="T">The event type for the bus.</typeparam>
        public static void RegisterBusType<T>() where T : IEvent {
            var busType = typeof(EventBus<T>);
            if (BusTypes.Contains(busType))
                return;
            
            Buses.Add(new EventBusType<T>());
            BusTypes.Add(busType);
            Buses.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }
        
        /// <summary>
        /// Records an invocation for a given event type.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        public static void RecordInvocation<T>() where T : IEvent {
            Debug.Log("Recording invocation for " + typeof(T).Name);
            var name = typeof(T).Name;
            LastInvocationTimes[name] = Time.realtimeSinceStartup;
        }
    }
}