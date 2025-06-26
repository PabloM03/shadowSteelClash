// Assets/Editor/BuildScript.cs
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Net;

public class BuildScript
{
    // Método que Unity Cloud Build invocará en <<Post-Export method>>
    public static void PerformServerBuild()
    {
        // 1) Generar el build
        string buildPath = "BuildOutput/servidorJuego.x86_64";
        string[] scenes = { "Assets/Scenes/scene1 2.unity" };
        var opts = new BuildPlayerOptions {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode | BuildOptions.CompressWithLz4
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);

        bool success = report.summary.result == BuildResult.Succeeded;
        if (success)
        {
            Debug.Log("✅ Build completado en: " + buildPath);
        }
        else
        {
            Debug.LogError("❌ Build falló");
            // para que Cloud Build detecte el fallo:
            throw new Exception("BuildScript: build server failed");
        }

        // 2) Si fue exitoso, disparar el dispatch a GitHub de forma síncrona
        try
        {
            DispatchToGitHubSync();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al hacer dispatch a GitHub: " + ex);
            // si quieres que falle el build en caso de dispatch fallido, descomenta:
            // throw;
        }
    }

    // Método síncrono para enviar el repository_dispatch
    
    static void DispatchToGitHubSync()
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("GITHUB_TOKEN no está definido en el environment.");

        const string owner = "PabloM03";
        const string repo  = "shadowSteelClash";
        var url   = $"https://api.github.com/repos/{owner}/{repo}/dispatches";

        var payload = JsonUtility.ToJson(new { event_type = "unity-build-complete" });

        using (var client = new WebClient())
        {
            client.Headers.Add("User-Agent", "UnityCloudBuild");
            client.Headers.Add("Authorization", $"token {token}");
            client.Headers.Add("Content-Type", "application/json");

            try
            {
                string response = client.UploadString(url, "POST", payload);
                Debug.Log("� Dispatch enviado a GitHub correctamente. Response: " + response);
            }
            catch (WebException wex) when (wex.Response is HttpWebResponse resp)
            {
                string body;
                using (var sr = new StreamReader(resp.GetResponseStream()))
                    body = sr.ReadToEnd();

                Debug.LogError($"❌ Dispatch falló: HTTP {(int)resp.StatusCode} {resp.StatusCode}\n{body}");
                throw;  // para que Unity Cloud Build marque error si lo deseas
            }
        }
    }
}
