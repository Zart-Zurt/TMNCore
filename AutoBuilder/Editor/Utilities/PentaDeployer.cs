using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

            var cleanVersion = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
            var defaultSlug = string.IsNullOrWhiteSpace(Application.productName)
                ? "game"
                : Application.productName.Trim().ToLowerInvariant().Replace(" ", "-");
            var slug = string.IsNullOrEmpty(config.PentaGameSlug) ? defaultSlug : config.PentaGameSlug;
            var targetParent = Path.Combine(config.PentaProjectPath, "public", "prototypes", slug, cleanVersion);

            try
            {
                Debug.Log($"[AutoBuilder Penta] Deploying WebGL files to: {targetParent}");

                if (Directory.Exists(targetParent))
                {
                    ProcessRunner.DeleteDirectoryWithRetries(targetParent);
                }

                Directory.CreateDirectory(targetParent);
                ProcessRunner.CopyDirectory(webGlBuildFolder, targetParent);

                Debug.Log("[AutoBuilder Penta] Files successfully copied to Penta prototypes folder.");

                var relativeUrl = $"/prototypes/{slug}/{cleanVersion}/index.html";
                await RegisterBuildToPentaApiAsync(config, slug, baseName, cleanVersion, relativeUrl);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder Penta] Deployment failed: {exception.Message}");
                return false;
            }
        }

        private static async Task RegisterBuildToPentaApiAsync(WebGlBuildConfigSO config, string slug, string baseName, string version, string buildUrl)
        {
            if (string.IsNullOrEmpty(config.PentaServerUrl))
            {
                return;
            }

            var cleanServerUrl = config.PentaServerUrl.TrimEnd('/');
            var endpoint = $"{cleanServerUrl}/api/games/{slug}/webgl";

            var payload = new PentaWebGLPayload
            {
                title = $"{baseName} WebGL {version}",
                version = version,
                buildUrl = buildUrl,
                aspectRatio = string.IsNullOrEmpty(config.PentaAspectRatio) ? "16:9" : config.PentaAspectRatio,
                notes = "Automated deploy via Unity AutoBuilder",
                makeActive = true
            };

            var json = JsonUtility.ToJson(payload);

            using (var httpClient = new HttpClient())
            {
                if (!string.IsNullOrEmpty(config.PentaApiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("x-penta-api-key", config.PentaApiKey);
                }

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync(endpoint, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log($"[AutoBuilder Penta] Successfully registered WebGL build in Penta module: {responseBody}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AutoBuilder Penta] API registration returned [{(int)response.StatusCode}]: {responseBody}");
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[AutoBuilder Penta] Could not reach Penta API at {endpoint}: {exception.Message}. WebGL files are still available statically.");
                }
            }
        }

        [Serializable]
        private class PentaWebGLPayload
        {
            public string title;
            public string version;
            public string buildUrl;
            public string aspectRatio;
            public string notes;
            public bool makeActive;
        }
    }
}
