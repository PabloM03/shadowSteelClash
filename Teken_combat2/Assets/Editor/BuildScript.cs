using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;


public class BuildScript
{
    public static void PerformServerBuild()
    {
        // Ruta relativa para Cloud Build (la carpeta "BuildOutput" se comprimirá en el .zip automáticamente)
        string buildPath = "BuildOutput/servidorJuego.x86_64";

        // Escenas que deben estar incluidas en el build
        string[] scenes = {
            "Assets/Scenes/scene1 2.unity" // Asegúrate que el nombre esté bien exacto y que la escena esté en Build Settings
        };

        // Opciones del compilador
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode | BuildOptions.CompressWithLz4
        };

        // Lanzar el build
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

        // Validar el resultado
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("✅ Build de servidor completado exitosamente en: " + buildPath);
        }
        else
        {
            Debug.LogError("❌ El build del servidor ha fallado.");
        }
    }
}
