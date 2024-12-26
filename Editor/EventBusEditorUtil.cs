using UnityEditor;

namespace DGP.EventBus.Editor
{
    public static class EventBusEditorUtil
    {
        [InitializeOnLoadMethod]
        private static void Initialize() {
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }

        private static void PlayModeStateChange(PlayModeStateChange state) {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                EventBusRegistry.ClearAllBuses();
        }
        
        
    }
}