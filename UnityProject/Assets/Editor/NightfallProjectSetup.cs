using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nightfall.UnityMvp.Editor
{
    public static class NightfallProjectSetup
    {
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

            PlayerSettings.productName = "Nightfall Protocol Unity";
            PlayerSettings.companyName = "Nightfall Studio";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.nightfall.protocol.unity");
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            AssetDatabase.SaveAssets();
            Debug.Log("NIGHTFALL_SETUP_COMPLETE");
        }
    }
}
