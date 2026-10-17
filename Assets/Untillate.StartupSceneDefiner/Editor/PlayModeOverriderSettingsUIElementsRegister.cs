#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Untillate.StartupSceneDefiner.Editor
{
    public class PlayModeOverriderSettingsUIElementsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateOverridePlayModeSettingsProvider()
        {
            string path = "Project/Startup Scene Definer";
            var provider = new SettingsProvider(path, SettingsScope.Project)
            {
                label = "Startup Scene Definer",
                guiHandler = (searchContext) =>
                {
                    SerializedObject settings = PlayModeOverriderSettings.GetSerializedSettings();

                    SerializedProperty overrideCurrentSceneProperty = settings.FindProperty("_overrideCurrentScene");
                    SerializedProperty defaultSceneIndexProperty = settings.FindProperty("_defaultSceneIndex");
                    SerializedProperty ignoreNameSuffixProperty = settings.FindProperty("_ignoreNameSuffix");

                    EditorGUILayout.PropertyField(overrideCurrentSceneProperty, new GUIContent("Enable"));

                    if (overrideCurrentSceneProperty.boolValue)
                    {
                        EditorGUILayout.PropertyField(defaultSceneIndexProperty, new GUIContent("Startup Scene Index"));
                        EditorGUILayout.PropertyField(ignoreNameSuffixProperty, new GUIContent("Ignore Name Suffix"));
                        EditorGUILayout.HelpBox("Use this suffix in test scenes to ignore overriding. Example: GameTest", MessageType.Info);

                    }
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },

                keywords = new string[] { "Override", "Play Mode", "Scene", "Startup" }
            };

            return provider;
        }

    }
}
#endif