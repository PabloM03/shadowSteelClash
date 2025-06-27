using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;

public class BuildScript
{
    // Llamado como “Post-Export Method” en Cloud Build
    public static void PerformServerBuild()
    {
        string buildPath = "BuildOutput/windows-server.exe";
        string[] scenes  = { "Assets/Scenes/scene1 2.unity" };

        var opts = new BuildPlayerOptions
        {
            scenes             = scenes,
            locationPathName   = buildPath,
            target             = BuildTarget.StandaloneWindows64,
            subtarget          = (int)StandaloneBuildSubtarget.Server,   // Dedicated Server
            options            = BuildOptions.CompressWithLz4
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
            UnityEngine.Debug.Log("✅ Build servidor Windows listo: " + buildPath);
        else
            throw new System.Exception("❌ Build de servidor falló");
    }
}
