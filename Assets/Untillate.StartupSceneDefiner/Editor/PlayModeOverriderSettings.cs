#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Untillate.StartupSceneDefiner.Editor
{
    public class PlayModeOverriderSettings : ScriptableObject
    {
        public static string FolderPath = "Assets";
        public static string FolderName = "StartupSceneAssetsData";
        public static string AssetName = "PlayModeSceneSettings.asset";
        public static string AssetPath = $"{FolderPath}/{FolderName}/{AssetName}";

        [SerializeField] private int _defaultSceneIndex;
        [SerializeField] private bool _overrideCurrentScene;
        [SerializeField] private string _ignoreNameSuffix = "Test";

        public bool OverrideCurrentScene => _overrideCurrentScene;
        public int DefaultSceneIndex => _defaultSceneIndex;
        public string IgnoreNameSuffix => _ignoreNameSuffix;

        internal static PlayModeOverriderSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayModeOverriderSettings>(AssetPath);
          
            if (settings == null)
            {
                if(!AssetDatabase.IsValidFolder($"{FolderPath}/{FolderName}")){
                    AssetDatabase.CreateFolder(FolderPath, FolderName);
                }
                settings = CreateInstance<PlayModeOverriderSettings>();
                settings._defaultSceneIndex = 0;
                settings._overrideCurrentScene = false;
                AssetDatabase.CreateAsset(settings, AssetPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

}
#endif