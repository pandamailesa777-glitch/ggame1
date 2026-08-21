using UnityEngine;

namespace Nightfall.UnityMvp
{
    public static class NightfallBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (Object.FindAnyObjectByType<NightfallGame>() != null) return;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            var root = new GameObject("NightfallGame");
            root.AddComponent<NightfallGame>();
        }
    }
}
