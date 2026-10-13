using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class UPoseWebGlBuilder
{
    [MenuItem("UPose/Build WebGL to UPOSE_BACKUP_TEMP")]
    public static void BuildToBackupTempMenu()
    {
        BuildInternal(false);
    }

    public static void BuildToBackupTemp()
    {
        BuildInternal(true);
    }

    static void BuildInternal(bool exitOnFailure)
    {
        string[] scenes = { "Assets/Saves/8_4_edited.unity" };
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = @"C:\Users\adora\UPOSE_BACKUP_TEMP",
            targetGroup = BuildTargetGroup.WebGL,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        bool ok = report != null && report.summary.result == BuildResult.Succeeded;
        if (ok)
        {
            Debug.Log("WebGL build succeeded: C:\\Users\\adora\\UPOSE_BACKUP_TEMP");
            return;
        }

        Debug.LogError("WebGL build failed.");
        if (exitOnFailure)
        {
            EditorApplication.Exit(1);
        }
    }
}
