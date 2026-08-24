using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Nightfall.UnityMvp.Editor
{
    [InitializeOnLoad]
    internal static class NightfallTmpBootstrap
    {
        private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static NightfallTmpBootstrap()
        {
            EditorApplication.delayCall += EnsureSettings;
        }

        private static void EnsureSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath) != null)
                return;

            Directory.CreateDirectory("Assets/Resources");
            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            settings.name = "TMP Settings";
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log("NIGHTFALL_TMP_SETTINGS_CREATED " + SettingsPath);
        }
    }
}
