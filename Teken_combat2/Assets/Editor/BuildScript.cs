// Assets/Editor/BuildScript.cs
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
            Debug.Log("✅ Build completado en: " + buildPath);
        else {
            Debug.LogError("❌ Build falló");
            // Opcional: lanzar exception para que Cloud Build lo marque como failed
            throw new Exception("BuildScript: build server failed");
        }

        // 2) Si fue exitoso, disparar el dispatch a GitHub
        try
        {
            DispatchToGitHub().Wait();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al hacer dispatch a GitHub: " + ex);
            // Opcional: volver a fallar el build
            throw;
        }
    }

    // Lanza un repository_dispatch usando HttpClient
    static async Task DispatchToGitHub()
    {
        string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("GITHUB_TOKEN no está definido en el environment.");

        string owner = "PabloM03";
        string repo  = "shadowSteelClash";
        string url   = $"https://api.github.com/repos/{owner}/{repo}/dispatches";

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "UnityCloudBuild");
            client.DefaultRequestHeaders.Add("Authorization", $"token {token}");

            // Cuerpo del dispatch
            var payload = new { event_type = "unity-build-complete" };
            string json = JsonUtility.ToJson(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await client.PostAsync(url, content);
            if (!res.IsSuccessStatusCode)
            {
                string body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Dispatch falló ({(int)res.StatusCode}): {body}");
            }
            Debug.Log("�️ Dispatch enviado a GitHub correctamente.");
        }
    }
}
