using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DGP.EventBus.Bindings;
using DGP.EventBus.Bridges;
using UnityEditor;
using UnityEngine;

namespace DGP.EventBus.Editor
{
    public class EventBusDebugWindow : EditorWindow
    {
        [MenuItem("DGP/Event Bus Debugger")]
        private static void OpenWindow()
        {
            var window = GetWindow<EventBusDebugWindow>("Event Bus Debugger");
            window.minSize = new Vector2(400f, 300f);
            window.Show();
        }

        // ── Flash ─────────────────────────────────────────────────────────────
        private readonly Dictionary<Type, double> _flashTimestamps = new();
        private const double FlashDuration = 0.6;
        private static readonly Color FlashColor  = new Color(1f, 0.85f, 0f, 1f);
        private static readonly Color NormalBg    = new Color(0.20f, 0.20f, 0.20f, 1f);

        // ── Live panel ────────────────────────────────────────────────────────
        private readonly Dictionary<Type, bool> _foldouts = new();
        private Vector2 _liveScrollPos;

        // ── Log panel ─────────────────────────────────────────────────────────
        private bool _showLog;
        private readonly List<(double time, Type type)> _log = new(200);
        private const int MaxLogEntries = 200;
        private readonly HashSet<Type> _mutedTypes = new();
        private Vector2 _logScrollPos;
        private bool _autoScrollLog = true;

        // ── Reflection cache (static: survives window close/reopen) ───────────
        private static readonly Dictionary<Type, FieldInfo> _syncFieldCache  = new();
        private static readonly Dictionary<Type, FieldInfo> _asyncFieldCache = new();

        // ── Cached GUIStyles (lazy: must not init before first OnGUI) ─────────
        private GUIStyle _priorityBadgeStyle;
        private GUIStyle _asyncBadgeStyle;
        private GUIStyle _handlerLabelStyle;

        private static readonly Color PriorityBadgeColor = new Color(0.20f, 0.45f, 0.85f, 1f);
        private static readonly Color AsyncBadgeColor    = new Color(0.20f, 0.65f, 0.35f, 1f);

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBusBridge.OnEventRaised         += HandleEventRaised;
            EventBusBridge.OnContainerRegistered += HandleContainerRegistered;
            EditorApplication.update             += OnEditorUpdate;

            foreach (var kvp in EventBusBridge.RegisteredContainers)
                EnsureTypeTracked(kvp.Key);
        }

        private void OnDisable()
        {
            EventBusBridge.OnEventRaised         -= HandleEventRaised;
            EventBusBridge.OnContainerRegistered -= HandleContainerRegistered;
            EditorApplication.update             -= OnEditorUpdate;
        }

        private void HandleEventRaised(Type eventType)
        {
            _flashTimestamps[eventType] = EditorApplication.timeSinceStartup;

            if (!_mutedTypes.Contains(eventType))
            {
                if (_log.Count >= MaxLogEntries)
                    _log.RemoveAt(0);
                _log.Add((EditorApplication.timeSinceStartup, eventType));
            }
        }

        private void HandleContainerRegistered(Type eventType, object _)
        {
            EnsureTypeTracked(eventType);
            Repaint();
        }

        private void EnsureTypeTracked(Type t)
        {
            if (!_foldouts.ContainsKey(t))
                _foldouts[t] = false;
        }

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            foreach (var kvp in _flashTimestamps)
            {
                if (now - kvp.Value < FlashDuration)
                {
                    Repaint();
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GUI
        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            float toolbarHeight = EditorGUIUtility.singleLineHeight + 6f;
            float logHeight     = _showLog ? Mathf.Max(position.height * 0.35f, 120f) : 0f;
            float liveHeight    = position.height - toolbarHeight - logHeight;

            DrawLivePanel(new Rect(0f, toolbarHeight, position.width, liveHeight));

            if (_showLog)
                DrawLogPanel(new Rect(0f, toolbarHeight + liveHeight, position.width, logHeight));
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Event Bus Debugger", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            _showLog = GUILayout.Toggle(_showLog, "Log", EditorStyles.toolbarButton, GUILayout.Width(36f));
            if (GUILayout.Button("Clear Log", EditorStyles.toolbarButton))
                _log.Clear();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLivePanel(Rect rect)
        {
            // Snapshot to avoid mutating the dict mid-iteration if a container registers
            var containers = new List<KeyValuePair<Type, object>>(EventBusBridge.RegisteredContainers);

            GUILayout.BeginArea(rect);
            _liveScrollPos = EditorGUILayout.BeginScrollView(_liveScrollPos);

            if (containers.Count == 0)
            {
                EditorGUILayout.LabelField("No event types registered yet.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                double now = EditorApplication.timeSinceStartup;
                foreach (var kvp in containers)
                    DrawEventTypeRow(kvp.Key, kvp.Value, now);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawEventTypeRow(Type eventType, object container, double now)
        {
            float flashT = 0f;
            if (_flashTimestamps.TryGetValue(eventType, out double ts))
                flashT = Mathf.Clamp01(1f - (float)((now - ts) / FlashDuration));

            Color rowBg = Color.Lerp(NormalBg, FlashColor, Mathf.SmoothStep(0f, 1f, flashT));

            Rect rowRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rowRect, rowBg);

            int count = GetBindingCount(container);
            string header = $"{eventType.Name}  ({count} handler{(count == 1 ? "" : "s")})";

            if (!_foldouts.TryGetValue(eventType, out bool open))
                open = false;

            Color prevColor = GUI.contentColor;
            GUI.contentColor = flashT > 0.05f ? Color.black : Color.white;
            _foldouts[eventType] = EditorGUILayout.Foldout(open, header, true);
            GUI.contentColor = prevColor;

            if (_foldouts[eventType])
                DrawHandlerRows(container);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }

        private void DrawHandlerRows(object container)
        {
            IEnumerable bindings = GetBindings(container);
            if (bindings == null) return;

            EditorGUI.indentLevel++;
            foreach (object rawBinding in bindings)
            {
                if (rawBinding is not IEventBinding binding) continue;

                var (methodName, targetType, isAsync) = GetHandlerInfo(binding);
                string cleanMethod = CleanMethodName(methodName);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15f);

                DrawBadge($"P:{binding.Priority}", PriorityBadgeColor, 36f);

                if (isAsync)
                    DrawBadge("async", AsyncBadgeColor, 40f);

                GUILayout.Label($"{targetType}.{cleanMethod}", _handlerLabelStyle);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawBadge(string text, Color color, float width)
        {
            Rect r = GUILayoutUtility.GetRect(width, EditorGUIUtility.singleLineHeight,
                GUILayout.Width(width));
            EditorGUI.DrawRect(r, color);

            GUIStyle style = text.StartsWith("P:") ? _priorityBadgeStyle : _asyncBadgeStyle;
            GUI.Label(r, text, style);
        }

        private void DrawLogPanel(Rect rect)
        {
            // Divider line
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(0.1f, 0.1f, 0.1f, 1f));

            GUILayout.BeginArea(new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 1f));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Log  ({_log.Count}/{MaxLogEntries})", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            _autoScrollLog = GUILayout.Toggle(_autoScrollLog, "Auto-scroll", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos);

            for (int i = 0; i < _log.Count; i++)
            {
                var (time, type) = _log[i];
                if (_mutedTypes.Contains(type)) continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(FormatTime(time), EditorStyles.miniLabel, GUILayout.Width(58f));
                GUILayout.Label(type.Name, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                bool muted = _mutedTypes.Contains(type);
                if (GUILayout.Button(muted ? "Unmute" : "Mute", EditorStyles.miniButton, GUILayout.Width(48f)))
                {
                    if (muted) _mutedTypes.Remove(type);
                    else       _mutedTypes.Add(type);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (_autoScrollLog)
                _logScrollPos.y = float.MaxValue;

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Reflection helpers
        // ─────────────────────────────────────────────────────────────────────

        private static IEnumerable GetBindings(object container)
        {
            return container.GetType()
                .GetProperty("Bindings", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(container) as IEnumerable;
        }

        private static int GetBindingCount(object container)
        {
            var prop = container.GetType()
                .GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            return prop != null ? (int)prop.GetValue(container) : 0;
        }

        private static (string method, string target, bool isAsync) GetHandlerInfo(IEventBinding binding)
        {
            Type bindingType = binding.GetType();

            // Try _syncHandler first, then _asyncHandler
            foreach (var (fieldName, cache, isAsync) in new[]
            {
                ("_syncHandler",  _syncFieldCache,  false),
                ("_asyncHandler", _asyncFieldCache, true),
            })
            {
                if (!cache.TryGetValue(bindingType, out FieldInfo fi))
                    cache[bindingType] = fi = bindingType.GetField(fieldName,
                        BindingFlags.NonPublic | BindingFlags.Instance);

                if (fi == null) continue;

                if (fi.GetValue(binding) is Delegate del)
                {
                    string targetName = del.Target?.GetType().Name
                                     ?? del.Method.DeclaringType?.Name
                                     ?? "static";
                    return (del.Method.Name, targetName, isAsync);
                }
            }

            return ("<unknown>", "<unknown>", false);
        }

        // Strips compiler-generated wrapper noise from lambda/local method names.
        // "<MethodName>b__3_0" → "MethodName"
        private static string CleanMethodName(string name)
        {
            var match = Regex.Match(name, @"<(.+?)>");
            return match.Success ? match.Groups[1].Value : name;
        }

        private static string FormatTime(double t)
        {
            int totalSec = (int)t;
            int min  = totalSec / 60;
            int sec  = totalSec % 60;
            int cs   = (int)((t - totalSec) * 100);
            return $"{min:00}:{sec:00}.{cs:00}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Style init (lazy — GUIStyle can't be created before first OnGUI)
        // ─────────────────────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_priorityBadgeStyle != null) return;

            _priorityBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(2, 2, 1, 1),
                normal    = { textColor = Color.white }
            };

            _asyncBadgeStyle = new GUIStyle(_priorityBadgeStyle);

            _handlerLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
            };
        }
    }
}
