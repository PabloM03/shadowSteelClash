// Assets/Editor/BuildScript.cs
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;

public class BuildScript
{
    // Unity Cloud Build lo invoca en "Post-Export Method"
    public static void PerformServerBuild()
    {
        string buildPath = "BuildOutput/servidorJuego.x86_64";
        string[] scenes = { "Assets/Scenes/scene1 2.unity" };

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneLinux64,
            // El warning indica que EnableHeadlessMode está obsoleto.
            // Puedes reemplazarlo por StandaloneBuildSubtarget.Server si lo prefieres.
            options = BuildOptions.EnableHeadlessMode | BuildOptions.CompressWithLz4
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("✅ Build completado en: " + buildPath);
        }
        else
        {
            Debug.LogError("❌ Build falló");
            throw new Exception("BuildScript: el build de servidor falló");
        }
    }
}
