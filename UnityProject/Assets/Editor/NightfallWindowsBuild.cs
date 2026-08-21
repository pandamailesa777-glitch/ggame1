using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nightfall.UnityMvp.Editor
{
    public static class NightfallWindowsBuild
    {
        public static void BuildClean()
        {
            const string output="Builds/Windows/NightfallUnity.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Windows clean build failed: "+report.summary.result);Debug.Log("NIGHTFALL_WINDOWS_CLEAN_BUILD_COMPLETE");
        }
    }
}
