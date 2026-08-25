using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nightfall.UnityMvp.Editor
{
    public sealed class NightfallUiPreview:EditorWindow
    {
        private string auditResult="Run the game, then choose a UI state.";
        private bool showBounds;
        [MenuItem("Nightfall/UI Audit Preview")]
        private static void Open()=>GetWindow<NightfallUiPreview>("UI Audit");
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Nightfall UI Preview",EditorStyles.boldLabel);EditorGUILayout.HelpBox("Preview every major state without completing a run. Use the Game View aspect selector for 1920×1080, 2400×1080, 2560×1440 and a 20:9 preset, then run Validate and Capture.",MessageType.Info);
            if(!EditorApplication.isPlaying){EditorGUILayout.HelpBox("Enter Play Mode to activate preview controls.",MessageType.Warning);return;}
            string[] states={"Menu","HeroSelect","HUD","Upgrade","Pet","Pause","Boss","End"};int columns=2;for(int i=0;i<states.Length;i+=columns){EditorGUILayout.BeginHorizontal();for(int j=0;j<columns&&i+j<states.Length;j++){string state=states[i+j];if(GUILayout.Button(state,GUILayout.Height(34)))Game()?.DebugPreviewUi(state);}EditorGUILayout.EndHorizontal();}
            GUILayout.Space(10);bool nextBounds=EditorGUILayout.ToggleLeft("Show UI Layout Bounds",showBounds);if(nextBounds!=showBounds){showBounds=nextBounds;Game()?.SetUiLayoutBounds(showBounds);}
            if(GUILayout.Button("Validate active UI",GUILayout.Height(34))){var game=Game();auditResult=game!=null?game.RunUiLayoutAudit():"NightfallGame not found";}
            if(GUILayout.Button("Stress test long strings",GUILayout.Height(34))){var game=Game();auditResult=game!=null?game.RunUiLongStringAudit():"NightfallGame not found";}EditorGUILayout.HelpBox(auditResult,MessageType.None);
            if(GUILayout.Button("Capture Game View",GUILayout.Height(34))){string folder=Path.Combine(Application.dataPath,"../UiAuditScreenshots");Directory.CreateDirectory(folder);string path=Path.Combine(folder,"ui_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss")+".png");ScreenCapture.CaptureScreenshot(path);auditResult="Screenshot queued: "+path;}
        }
        private static NightfallGame Game()=>Object.FindFirstObjectByType<NightfallGame>();
    }
}
