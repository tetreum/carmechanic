using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Build : MonoBehaviour
{
    [MenuItem("Build/release/windows")]
    private static void BuildWindows()
    {
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[]
            {"Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity"};
        buildPlayerOptions.locationPathName = "Build/win/carmechanicx86.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded) Debug.Log("Build succeeded: " + summary.totalSize + " bytes");

        if (summary.result == BuildResult.Failed) Debug.Log("Build failed");
    }

    [MenuItem("Build/release/windows64")]
    private static void BuildWindows64()
    {
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[]
            {"Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity"};
        buildPlayerOptions.locationPathName = "Build/win64/carmechanicx64.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded) Debug.Log("Build succeeded: " + summary.totalSize + " bytes");

        if (summary.result == BuildResult.Failed) Debug.Log("Build failed");
    }

    [MenuItem("Build/dev/windows")]
    private static void BuildDevWindows()
    {
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[]
            {"Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity"};
        buildPlayerOptions.locationPathName = "Build/dev/win/carmechanic-devx86.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.options = BuildOptions.AllowDebugging;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded) Debug.Log("Build succeeded: " + summary.totalSize + " bytes");

        if (summary.result == BuildResult.Failed) Debug.Log("Build failed");
    }

    [MenuItem("Build/dev/windows64")]
    private static void BuildDevWindows64()
    {
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[]
            {"Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity"};
        buildPlayerOptions.locationPathName = "Build/dev/win64/carmechanic-devx64.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.options = BuildOptions.AllowDebugging;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded) Debug.Log("Build succeeded: " + summary.totalSize + " bytes");

        if (summary.result == BuildResult.Failed) Debug.Log("Build failed");
    }
}