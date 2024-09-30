using UnityEditor;

namespace DGP.EventBus.Editor
{
    public class EventBusEditorUtil
    {
        
        [InitializeOnLoadMethod]
        private static void Initialize() {
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }

        private static void PlayModeStateChange(PlayModeStateChange obj) {
            if (obj == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                EventBusRegistry.ClearAllBuses();
            }
        }
    }
}