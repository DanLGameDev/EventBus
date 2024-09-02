#if ODIN_INSPECTOR
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DGP.EventBus.Editor
{
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
}
#endif