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

            try
            {
                var sshSynced = SyncToRemoteVpsViaSsh(webGlBuildFolder, slug, cleanVersion, config);
                if (!sshSynced)
                {
                    Debug.LogError("[AutoBuilder Penta] SSH sync could not be completed. Aborting deployment.");
                    return false;
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
                            $"WebGL build {cleanVersion} was successfully deployed to Penta VPS via SSH!\n\nOpen WebGL player in browser now?",
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

        private static bool SyncToRemoteVpsViaSsh(string webGlBuildFolder, string slug, string cleanVersion, WebGlBuildConfigSO config)
        {
            var syncScriptPath = ResolveSshSyncScriptPath();
            if (string.IsNullOrEmpty(syncScriptPath) || !File.Exists(syncScriptPath))
            {
                Debug.LogError($"[AutoBuilder Penta] SSH sync script not found: {syncScriptPath}");
                return false;
            }

            var nodeExecutable = ResolveNodeExecutable();
            if (string.IsNullOrEmpty(nodeExecutable))
            {
                Debug.LogError("[AutoBuilder Penta] Node.js executable could not be found! Please install Node.js (https://nodejs.org) on this machine.");
                return false;
            }

            var host = config.GetEffectiveSshHost();
            var port = config.GetEffectiveSshPort();
            var username = config.GetEffectiveSshUsername();
            var password = config.SshPassword;
            var remoteBasePath = config.GetEffectiveSshRemotePath();

            Debug.Log($"[AutoBuilder Penta] Using Node.js at: {nodeExecutable}");
            Debug.Log($"[AutoBuilder Penta] Uploading build to VPS via SSH ({username}@{host}:{port})...");

            var arguments = $"\"{syncScriptPath}\" \"{webGlBuildFolder}\" \"{slug}\" \"{cleanVersion}\" \"{host}\" \"{port}\" \"{username}\" \"{password}\" \"{remoteBasePath}\"";
            var workingDir = Path.GetDirectoryName(syncScriptPath);
            var exitCode = ProcessRunner.Run(nodeExecutable, arguments, workingDir, true);

            if (exitCode == 0)
            {
                Debug.Log("[AutoBuilder Penta] WebGL prototype successfully deployed to VPS via SSH!");
                return true;
            }

            Debug.LogError($"[AutoBuilder Penta] SSH deployment failed with exit code: {exitCode}");
            return false;
        }

        private static string ResolveNodeExecutable()
        {
            var systemPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var pathParts = systemPath.Split(Path.PathSeparator);

            foreach (var part in pathParts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                try
                {
                    var candidate = Path.Combine(part.Trim(), "node.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                {
                }
            }

            var programFilesNode = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe");
            if (File.Exists(programFilesNode))
            {
                return programFilesNode;
            }

            var unityEditorDir = Path.GetDirectoryName(EditorApplication.applicationPath);
            if (!string.IsNullOrEmpty(unityEditorDir))
            {
                var unityNode = Path.Combine(unityEditorDir, "Data", "PlaybackEngines", "WebGLSupport", "BuildTools", "Emscripten", "node", "node.exe");
                if (File.Exists(unityNode))
                {
                    return unityNode;
                }
            }

            return "node.exe";
        }

        private static string ResolveSshSyncScriptPath()
        {
            var guids = AssetDatabase.FindAssets("penta_ssh_sync");
            if (guids != null && guids.Length > 0)
            {
                var relativePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                if (!string.IsNullOrEmpty(relativePath))
                {
                    return Path.GetFullPath(relativePath);
                }
            }

            var directPath = Path.Combine(Application.dataPath, "..", "Packages", "com.tmn.core", "AutoBuilder", "Editor", "Utilities", "penta_ssh_sync.cjs");
            if (File.Exists(directPath))
            {
                return Path.GetFullPath(directPath);
            }

            var localRepoPath = Path.Combine("C:", "UnityWorks", "TMN", "TMNCore", "AutoBuilder", "Editor", "Utilities", "penta_ssh_sync.cjs");
            if (File.Exists(localRepoPath))
            {
                return localRepoPath;
            }

            return string.Empty;
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
                notes = "Automated deploy via Unity AutoBuilder SSH",
                makeActive = true
            };

            var json = JsonUtility.ToJson(payload);

            using var httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("x-penta-api-key", apiKey);
            }

            const int maxRetries = 3;
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(endpoint, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log($"[AutoBuilder Penta] Successfully registered WebGL build in Penta API: {responseBody}");
                        return true;
                    }

                    if ((int)response.StatusCode >= 500 && attempt < maxRetries)
                    {
                        Debug.LogWarning($"[AutoBuilder Penta] API registration returned [{(int)response.StatusCode}], retrying ({attempt}/{maxRetries})...");
                        await Task.Delay(2000 * attempt);
                        continue;
                    }

                    Debug.LogWarning($"[AutoBuilder Penta] API registration returned [{(int)response.StatusCode}]: {responseBody}");
                    return false;
                }
                catch (Exception exception) when (attempt < maxRetries)
                {
                    Debug.LogWarning($"[AutoBuilder Penta] Request failed on attempt {attempt}: {exception.Message}. Retrying...");
                    await Task.Delay(2000 * attempt);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[AutoBuilder Penta] Could not reach Penta API at {endpoint}: {exception.Message}");
                    return false;
                }
            }

            return false;
        }
    }
}
