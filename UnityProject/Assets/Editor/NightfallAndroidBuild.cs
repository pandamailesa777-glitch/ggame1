using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nightfall.UnityMvp.Editor
{
    public static class NightfallAndroidBuild
    {
        public static void Build()
        {
            BuildInternal("Builds/Android/BureauBreakers-Chapter1-beta-0.3.0.apk", ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64);
        }

        private static void BuildInternal(string output, ScriptingImplementation backend, AndroidArchitecture architectures)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Could not switch active build target to Android.");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.nightfall.protocol.unity");
            PlayerSettings.productName = "Bureau breakers: Chapter 1";
            PlayerSettings.bundleVersion = "0.3.0-beta.1";
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
            PlayerSettings.Android.targetArchitectures = architectures;
            // ARM translation in Android's x86 emulator does not implement all Vulkan entry points.
            // GLES3 is also the safer default for the mid-range Android devices targeted by the MVP.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Android build failed: " + report.summary.result);
            Debug.Log("NIGHTFALL_ANDROID_BUILD_COMPLETE " + output);
        }

    }
}
