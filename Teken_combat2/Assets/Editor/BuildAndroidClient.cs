using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildAndroidClient
{
    private const string ApkPath = "Build/Android/TekenCombat2.apk";

    [MenuItem("Build/Android Client APK")]
    public static void BuildAPK()
    {
        EditorUserBuildSettings.buildAppBundle = false;

        var buildOptions = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Menu/OutdoorsScene.unity",
                "Assets/Scenes/Scene_Loading.unity",
                "Assets/Scenes/scene1 2.unity"
            },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4
        };

        Debug.Log("[BuildAndroidClient] Empezando build de Android (APK cliente)...");
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        var summary = report.summary;

        Debug.Log($"[BuildAndroidClient] Resultado: {summary.result} | Tamaño: {summary.totalSize / (1024 * 1024)} MB | Errores: {summary.totalErrors} | Warnings: {summary.totalWarnings} | Ruta: {ApkPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[BuildAndroidClient] Build FALLIDO");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("[BuildAndroidClient] Build EXITOSO");
            EditorApplication.Exit(0);
        }
    }
}
