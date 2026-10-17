#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Untillate.StartupSceneDefiner.Editor
{
    public static partial class PlayModeOverrider
    {
        [InitializeOnLoadMethod]
        static void OverrideStartScene()
        {

            EditorApplication.projectChanged += TryOverride;
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            TryOverride();

        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            TryOverride();
        }

        private static void TryOverride()
        {
            var settings = PlayModeOverriderSettings.GetOrCreateSettings();

            if (settings.OverrideCurrentScene)
            {

                string currentSceneName = EditorSceneManager.GetActiveScene().name;

                if (!currentSceneName.Contains(settings.IgnoreNameSuffix))
                {

                    try
                    {
                        SetStartupScene(settings);

                    }
                    catch (System.Exception)
                    {
                        Debug.LogWarning($"[Startup Scene Definer] Fail to override play mode start scene. The startup scene index {settings.DefaultSceneIndex} is Invalid");
                        SetActiveScene();
                    } 
                }
                else
                {
                    SetActiveScene();
                }

            }
            else
            {
                SetActiveScene();
            }
        }

        private static void SetActiveScene()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneManager.GetActiveScene().path);
        }

        private static void SetStartupScene(PlayModeOverriderSettings settings)
        {
            string pathOfFirstScene = EditorBuildSettings.scenes[settings.DefaultSceneIndex].path;
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOfFirstScene);
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }
}
#endif