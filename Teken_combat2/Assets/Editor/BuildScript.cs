// Assets/Editor/BuildScript.cs
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.CloudBuild;     // <–– necesario
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

public class BuildScript
{
    // Unity Cloud Build invoca este método en "Post-Export Method"
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
        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("BuildScript: build server failed");

        Debug.Log("✅ Build completado en: " + buildPath);

        // 2) Dispatch a GitHub
        DispatchToGitHubSync();
    }

    static void DispatchToGitHubSync()
    {
        // Esto recoge la variable que definiste en Unity Cloud Build → Environment Variables
        string token = CloudBuildSettings.GetValue("GITHUB_TOKEN", null);
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("GITHUB_TOKEN no está definido en las Environment Variables de Cloud Build.");

        const string owner = "PabloM03";
        const string repo  = "shadowSteelClash";
        var url = $"https://api.github.com/repos/{owner}/{repo}/dispatches";
        var payload = JsonUtility.ToJson(new { event_type = "unity-build-complete" });

        using (var client = new WebClient())
        {
            client.Headers.Add("User-Agent", "UnityCloudBuild");
            client.Headers.Add("Authorization", $"token {token}");
            client.Headers.Add("Content-Type", "application/json");

            try
            {
                var response = client.UploadString(url, "POST", payload);
                Debug.Log("� Dispatch enviado a GitHub correctamente. Response: " + response);
            }
            catch (WebException wex) when (wex.Response is HttpWebResponse resp)
            {
                string body;
                using (var sr = new StreamReader(resp.GetResponseStream()))
                    body = sr.ReadToEnd();

                Debug.LogError($"❌ Dispatch falló: HTTP {(int)resp.StatusCode} {resp.StatusCode}\n{body}");
                throw;
            }
        }
    }
}
