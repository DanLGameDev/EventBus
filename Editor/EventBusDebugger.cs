using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DGP.EventBus;

#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace DGP.EventBus.Editor
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
    
    public abstract class EventTypeBusBase {
        public abstract int BindingCount { get; }
        public abstract string Name { get; }
        public abstract List<string> GetBindingNames();
    }

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
    
#if UNITY_EDITOR && ODIN_INSPECTOR
    public class EventBusDebuggerWindow : OdinEditorWindow
    {
        private readonly HashSet<string> openBusses = new();
        private GUIStyle inactiveStyle;
        
        [MenuItem("Tools/EventBus Monitor")]
        private static void OpenWindow()
        {
            GetWindow<EventBusDebuggerWindow>().Show();
        }
        
        private GUIStyle GetInactiveStyle() {
            if (inactiveStyle == null) {
                inactiveStyle = new GUIStyle(EditorStyles.label)
                {
                    normal =
                    {
                        textColor = Color.gray
                    }
                };
            }
            return inactiveStyle;
        }
        
        private void Update() {
            Repaint();
        }

        protected override void DrawEditors() {
            base.DrawEditors();
            
            if (!Application.isPlaying) {
                GUI.Label(new Rect(10, 10, position.width - 20, 20), "EventBus Monitor is only active at runtime", EditorStyles.boldLabel);
                return;
            }
            
            var busses = EventBusRegistry.Busses;
            
            foreach (var bus in busses) {
                if (bus.BindingCount>0)
                    DrawBus(bus);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inactive");

            GUIStyle style = GetInactiveStyle();
            foreach (var bus in busses) {
                if (bus.BindingCount==0)
                    EditorGUILayout.LabelField(bus.Name, style);
            }
        }
        
        const float flashTime = 0.5f;

        private void DrawBus(EventTypeBusBase bus) {
            var isOpen = openBusses.Contains(bus.Name);
            
            isOpen = EditorGUILayout.Foldout(isOpen, bus.Name + " (" + bus.BindingCount + ")", true);
            
            var lastInvokeTime = EventBusRegistry.GetLastInvocationTime(bus.Name);
            
            if (Time.realtimeSinceStartup - lastInvokeTime < flashTime) {
                var rect = GUILayoutUtility.GetLastRect();
                var alpha = Mathf.Lerp(0.5f, 0f, (Time.realtimeSinceStartup - lastInvokeTime) / flashTime);
                EditorGUI.DrawRect(new Rect(0, rect.y, position.width, rect.height), new Color(1, 1, 0, alpha));
            }
            
            if (isOpen) {
                openBusses.Add(bus.Name);
            } else {
                openBusses.Remove(bus.Name);
            }
            
            if (isOpen) {
                EditorGUI.indentLevel++;
                foreach (var bindingName in bus.GetBindingNames()) {
                    EditorGUILayout.LabelField(bindingName);
                }
                EditorGUI.indentLevel--;
            }
        }
    }
#endif
}
