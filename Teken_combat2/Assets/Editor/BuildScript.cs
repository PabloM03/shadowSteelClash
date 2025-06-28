using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Linq;

public static class BuildScript
{
    public static void PerformServerBuild()
    {
        var scenes = EditorBuildSettings.scenes
                      .Where(s => s.enabled)
                      .Select(s => s.path)
                      .ToArray();

        if (scenes.Length == 0)
            throw new System.Exception("❌ No hay escenas habilitadas");

        PlayerSettings.runInBackground = true;

        var opts = new BuildPlayerOptions {
            scenes           = scenes,
            locationPathName = "BuildOutput/windows-server.exe",
            target           = BuildTarget.StandaloneWindows64,
            subtarget        = (int)StandaloneBuildSubtarget.Server,
            options          = BuildOptions.CompressWithLz4 | BuildOptions.EnableHeadlessMode
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("❌ Build falló");

        UnityEngine.Debug.Log("✅ Build servidor listo");
    }
}
