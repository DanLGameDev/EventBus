using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGP.EventBus.Editor
{

    public class EventBusDebuggerWindow : EditorWindow
    {
        private readonly HashSet<string> _openBusses = new HashSet<string>();
        private GUIStyle _inactiveStyle;
        private const float FlashTime = 0.5f;

        [MenuItem("Tools/EventBus Monitor")]
        private static void OpenWindow() {
            GetWindow<EventBusDebuggerWindow>("EventBus Monitor").Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= Repaint;
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.update += Repaint;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    EditorApplication.update -= Repaint;
                    break;
            }
        }

        private GUIStyle GetInactiveStyle() {
            if (_inactiveStyle == null) {
                _inactiveStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.gray }
                };
            }

            return _inactiveStyle;
        }

        private void OnGUI() {
            if (!Application.isPlaying) {
                EditorGUILayout.LabelField("EventBus Monitor is only active at runtime", EditorStyles.boldLabel);
                return;
            }

            var busses = EventBusRegistry.RegisteredBuses;

            foreach (var bus in busses) {
                if (bus.BindingCount > 0)
                    DrawBus(bus);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inactive");

            GUIStyle style = GetInactiveStyle();
            foreach (var bus in busses) {
                if (bus.BindingCount == 0)
                    EditorGUILayout.LabelField(bus.Name, style);
            }
        }

        private void DrawBus(EventTypeBusBase bus) {
            var isOpen = _openBusses.Contains(bus.Name);

            Rect foldoutRect = EditorGUILayout.GetControlRect();

            // Draw flash effect
            if (EventBusRegistry.GetLastInvocationTime(bus.Name) > 0) {
                double timeSinceInvoke = Time.realtimeSinceStartup - EventBusRegistry.GetLastInvocationTime(bus.Name);
                if (timeSinceInvoke < FlashTime) {
                    float alpha = 1f - (float)(timeSinceInvoke / FlashTime);
                    EditorGUI.DrawRect(foldoutRect, new Color(1, 1, 0, alpha * 0.3f));
                }
            }
            
            isOpen = EditorGUI.Foldout(foldoutRect, isOpen, $"{bus.Name} ({bus.BindingCount})", true);

            if (isOpen) {
                _openBusses.Add(bus.Name);
                EditorGUI.indentLevel++;
                foreach (var bindingName in bus.GetBindingNames()) {
                    EditorGUILayout.LabelField(bindingName);
                }

                EditorGUI.indentLevel--;
            }
            else {
                _openBusses.Remove(bus.Name);
            }
        }
    }
}