using UnityEditor;
using UnityEngine;

public class BuildServer
{
    [MenuItem("Build/Server Build")]
    public static void BuildLinuxServer()
    {
        string buildPath = "Build/LinuxServer/TekenCombat2.x86_64";

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/scene1 2.unity" }, // Ajusta tus escenas
            locationPathName = buildPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode
        };

        BuildPipeline.BuildPlayer(buildOptions);
        Debug.Log("✅ Build de Linux Server completado: " + buildPath);
    }
}
