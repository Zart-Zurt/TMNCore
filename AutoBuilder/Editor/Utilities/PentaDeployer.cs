using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public static class PentaDeployer
    {
        public static async Task<bool> DeployToPentaAsync(string webGlBuildFolder, WebGlBuildConfigSO config, string baseName, string version)
        {
            if (config == null || !config.EnablePentaDeployment)
            {
                return false;
            }

            if (!Directory.Exists(webGlBuildFolder))
            {
                Debug.LogError($"[AutoBuilder Penta] Source build folder does not exist: {webGlBuildFolder}");
                return false;
            }

            var cleanVersion = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
            var slug = config.GetEffectiveGameSlug();
            var serverUrl = config.GetEffectiveServerUrl();
            var apiKey = config.GetEffectiveApiKey();
            var aspectRatio = config.GetEffectiveAspectRatio();

            var targetParent = Path.Combine(config.PentaProjectPath, "public", "prototypes", slug, cleanVersion);

            try
            {
                if (Directory.Exists(config.PentaProjectPath))
                {
                    Debug.Log($"[AutoBuilder Penta] Deploying WebGL files to local Penta folder: {targetParent}");

                    if (Directory.Exists(targetParent))
                    {
                        ProcessRunner.DeleteDirectoryWithRetries(targetParent);
                    }

                    Directory.CreateDirectory(targetParent);
                    ProcessRunner.CopyDirectory(webGlBuildFolder, targetParent);

                    Debug.Log("[AutoBuilder Penta] Files successfully copied to local Penta prototypes directory.");

                    if (config.SyncRemoteVps)
                    {
                        SyncToRemoteVps(config.PentaProjectPath, targetParent, slug, cleanVersion);
                    }
                }
                else
                {
                    Debug.LogWarning($"[AutoBuilder Penta] Penta project path not found on disk: {config.PentaProjectPath}");
                }

                var relativeUrl = $"/prototypes/{slug}/{cleanVersion}/index.html";
                var registered = await RegisterBuildToPentaApiAsync(serverUrl, apiKey, slug, baseName, cleanVersion, relativeUrl, aspectRatio);

                var webPlayerUrl = $"{serverUrl}/games/{slug}/webgl";

                if (registered)
                {
                    Debug.Log($"[AutoBuilder Penta] WebGL build deployed and live! Player URL: {webPlayerUrl}");

                    EditorApplication.delayCall += () =>
                    {
                        var open = EditorUtility.DisplayDialog(
                            "Penta Deployment Success",
                            $"WebGL build {cleanVersion} was successfully deployed to Penta!\n\nOpen WebGL player in browser now?",
                            "Open in Browser",
                            "Close");

                        if (open)
                        {
                            Application.OpenURL(webPlayerUrl);
                        }
                    };
                }

                return registered;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder Penta] Deployment failed: {exception.Message}");
                return false;
            }
        }

        private static void SyncToRemoteVps(string pentaProjectPath, string targetParent, string slug, string cleanVersion)
        {
            var syncScriptPath = Path.Combine(pentaProjectPath, "scripts", "sync_prototype_remote.cjs");
            if (!File.Exists(syncScriptPath))
            {
                Debug.LogWarning($"[AutoBuilder Penta] sync_prototype_remote.cjs not found at: {syncScriptPath}");
                return;
            }

            Debug.Log("[AutoBuilder Penta] Syncing prototype files to live Penta VPS...");
            var arguments = $"\"{syncScriptPath}\" \"{targetParent}\" \"{slug}\" \"{cleanVersion}\"";
            var exitCode = ProcessRunner.Run("node.exe", arguments, pentaProjectPath, true);

            if (exitCode == 0)
            {
                Debug.Log("[AutoBuilder Penta] Prototype files successfully synchronized to live VPS!");
            }
            else
            {
                Debug.LogWarning($"[AutoBuilder Penta] Prototype VPS sync exited with code {exitCode}. Files are still available locally.");
            }
        }

        private static async Task<bool> RegisterBuildToPentaApiAsync(string serverUrl, string apiKey, string slug, string baseName, string version, string buildUrl, string aspectRatio)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                return false;
            }

            var endpoint = $"{serverUrl}/api/games/{slug}/webgl";

            var payload = new PentaWebGlPayload
            {
                title = $"{baseName} WebGL {version}",
                version = version,
                buildUrl = buildUrl,
                aspectRatio = aspectRatio,
                notes = "Automated deploy via Unity AutoBuilder",
                makeActive = true
            };

            var json = JsonUtility.ToJson(payload);

            using var httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("x-penta-api-key", apiKey);
            }

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[AutoBuilder Penta] Successfully registered WebGL build in Penta API: {responseBody}");
                    return true;
                }

                Debug.LogWarning($"[AutoBuilder Penta] API registration returned [{(int)response.StatusCode}]: {responseBody}");
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AutoBuilder Penta] Could not reach Penta API at {endpoint}: {exception.Message}. WebGL files remain in prototypes folder.");
                return false;
            }
        }
    }
}
