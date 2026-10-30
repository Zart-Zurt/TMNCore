using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Renci.SshNet;
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
            var host = config.GetEffectiveSshHost();
            var port = config.GetEffectiveSshPort();
            var username = config.GetEffectiveSshUsername();
            var password = config.SshPassword;
            var remoteBasePath = config.GetEffectiveSshRemotePath();

            try
            {
                var sshSynced = await Task.Run(() => SyncToRemoteVpsViaSshNet(webGlBuildFolder, slug, cleanVersion, host, port, username, password, remoteBasePath));
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

        private static bool SyncToRemoteVpsViaSshNet(string webGlBuildFolder, string slug, string cleanVersion, string host, int port, string username, string password, string remoteBasePath)
        {
            Debug.Log($"[AutoBuilder Penta] Packaging WebGL build: {slug} ({cleanVersion})...");

            var tempArchiveName = $"proto_{slug}_{cleanVersion}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.tar.gz";
            var tempArchivePath = Path.Combine(Path.GetTempPath(), tempArchiveName);

            try
            {
                var tarSuccess = CreateTarGz(webGlBuildFolder, tempArchivePath);
                if (!tarSuccess || !File.Exists(tempArchivePath))
                {
                    Debug.LogError("[AutoBuilder Penta] Failed to create tar.gz archive.");
                    return false;
                }

                Debug.Log($"[AutoBuilder Penta] Connecting directly to VPS via C# SSH ({username}@{host}:{port})...");
                var connectionInfo = new ConnectionInfo(host, port, username, new PasswordAuthenticationMethod(username, password))
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                using var sftp = new SftpClient(connectionInfo);
                sftp.Connect();
                Debug.Log("[AutoBuilder Penta] SFTP Connected.");

                var remoteArchivePath = $"{remoteBasePath}/{tempArchiveName}";
                var remoteDestDir = $"{remoteBasePath}/public/prototypes/{slug}/{cleanVersion}";

                Debug.Log($"[AutoBuilder Penta] Uploading archive to {remoteArchivePath}...");
                using (var fileStream = File.OpenRead(tempArchivePath))
                {
                    sftp.UploadFile(fileStream, remoteArchivePath, true);
                }
                sftp.Disconnect();

                using var ssh = new SshClient(connectionInfo);
                ssh.Connect();
                Debug.Log("[AutoBuilder Penta] SSH Connected.");

                Debug.Log($"[AutoBuilder Penta] Extracting remote archive to {remoteDestDir}...");
                var extractCmd = $"mkdir -p {remoteDestDir} && tar -xzf {remoteArchivePath} -C {remoteDestDir} && rm -f {remoteArchivePath}";
                var result = ssh.RunCommand(extractCmd);
                if (result.ExitStatus != 0)
                {
                    Debug.LogError($"[AutoBuilder Penta] Remote extraction failed: {result.Error} {result.Result}");
                    return false;
                }

                Debug.Log("[AutoBuilder Penta] Syncing files into penta-web Docker container...");
                var dockerCommands = new[]
                {
                    $"docker exec penta-web mkdir -p /app/public/prototypes/{slug}/{cleanVersion}",
                    $"docker cp {remoteDestDir}/. penta-web:/app/public/prototypes/{slug}/{cleanVersion}/",
                    $"docker exec penta-web chmod -R 755 /app/public/prototypes/{slug}/{cleanVersion}",
                    "docker restart penta-web"
                };

                foreach (var cmd in dockerCommands)
                {
                    var cmdResult = ssh.RunCommand(cmd);
                    if (cmdResult.ExitStatus != 0)
                    {
                        Debug.LogWarning($"[AutoBuilder Penta] Docker container command warning ({cmd}): {cmdResult.Error} {cmdResult.Result}");
                    }
                }

                ssh.Disconnect();
                Debug.Log("[AutoBuilder Penta] Prototype files synchronized and live on VPS!");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder Penta] SSH/SFTP deployment failed: {exception.Message}\n{exception.StackTrace}");
                return false;
            }
            finally
            {
                if (File.Exists(tempArchivePath))
                {
                    try
                    {
                        File.Delete(tempArchivePath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool CreateTarGz(string sourceDir, string destinationTarGzPath)
        {
            var systemTar = "tar.exe";
            var parentDir = Path.GetDirectoryName(sourceDir);
            var folderName = Path.GetFileName(sourceDir);

            var code = ProcessRunner.Run(systemTar, $"-czf \"{destinationTarGzPath}\" -C \"{sourceDir}\" .", sourceDir, true);
            if (code == 0 && File.Exists(destinationTarGzPath))
            {
                return true;
            }

            return false;
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
                notes = "Automated deploy via Unity AutoBuilder Native SSH",
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
