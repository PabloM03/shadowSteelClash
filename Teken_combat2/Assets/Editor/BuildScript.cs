using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void PerformServerBuild()
    {
        // Carpeta de salida del build
        string buildPath = "buildServer/servidorJuego.x86_64";

        // Escenas que quieres incluir (las que aparecen en tu Build Settings)
        string[] scenes = {
            "Assets/Scenes/scene1 2.unity"
        };

        // Configuración de build para Linux headless (modo servidor)
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode
        };

        // Ejecutar el build
        BuildPipeline.BuildPlayer(buildOptions);
        Debug.Log("✅ Build de servidor completado en: " + buildPath);
    }
}
