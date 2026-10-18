using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public class WebGlPlatformBuilder : IPlatformBuilder
    {
        private WebGlBuildConfigSO _config;
        private AutoBuilderContext _context;

        public TargetPlatform Platform => TargetPlatform.WebGL;
        public BuildTarget Target => BuildTarget.WebGL;
        public BuildTargetGroup TargetGroup => BuildTargetGroup.WebGL;
        public string DisplayName => "WebGL";
        public bool IsHostSupported => true;
        public string HostUnsupportedReason => "";

        public void Initialize(AutoBuilderContext context)
        {
            _context = context;
            LoadOrCreateConfig();
        }

        public void DrawSettingsGUI()
        {
            if (_config == null)
            {
                LoadOrCreateConfig();
            }

            EditorGUILayout.LabelField("WebGL Build Options", EditorStyles.boldLabel);

            _config.CompressionFormat = (WebGLCompressionFormat)EditorGUILayout.EnumPopup("Compression Format", _config.CompressionFormat);
            _config.TemplateName = EditorGUILayout.TextField("WebGL Template", _config.TemplateName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local Test Server (Embedded)", EditorStyles.boldLabel);
            _config.AutoStartLocalServer = EditorGUILayout.Toggle("Auto Start Local Server", _config.AutoStartLocalServer);
            _config.LocalServerPort = EditorGUILayout.IntField("Local Server Port", _config.LocalServerPort);

            EditorGUILayout.BeginHorizontal();
            if (WebGlLocalServer.IsRunning)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Stop Local Server", GUILayout.Height(26)))
                {
                    WebGlLocalServer.StopServer();
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Open in Browser", GUILayout.Height(26)))
                {
                    Application.OpenURL($"http://localhost:{WebGlLocalServer.CurrentPort}/index.html");
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
                if (GUILayout.Button("Start Server on Last Build", GUILayout.Height(26)))
                {
                    var folder = GetLastBuildFolder();
                    if (Directory.Exists(folder))
                    {
                        WebGlLocalServer.StartServer(folder, _config.LocalServerPort);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", $"Build folder not found: {folder}", "OK");
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Penta Studio WebGL Integration", EditorStyles.boldLabel);
            _config.EnablePentaDeployment = EditorGUILayout.Toggle("Deploy to Penta", _config.EnablePentaDeployment);

            if (_config.EnablePentaDeployment)
            {
                EditorGUILayout.BeginHorizontal();
                _config.PentaProjectPath = EditorGUILayout.TextField("Penta Project Path", _config.PentaProjectPath);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    var selected = EditorUtility.OpenFolderPanel("Select Penta Project Root", _config.PentaProjectPath, "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        _config.PentaProjectPath = selected;
                    }
                }
                EditorGUILayout.EndHorizontal();

                _config.PentaServerUrl = EditorGUILayout.TextField("Penta Server URL", _config.PentaServerUrl);
                _config.PentaGameSlug = EditorGUILayout.TextField("Game Slug", _config.PentaGameSlug);
                _config.PentaApiKey = EditorGUILayout.PasswordField("API Key", _config.PentaApiKey);
                _config.PentaAspectRatio = EditorGUILayout.TextField("Aspect Ratio", _config.PentaAspectRatio);

                if (GUILayout.Button("Deploy Last Build to Penta Now", GUILayout.Height(26)))
                {
                    var folder = GetLastBuildFolder();
                    if (Directory.Exists(folder))
                    {
                        _ = PentaDeployer.DeployToPentaAsync(folder, _config, _context.BaseName, _context.Version);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", $"Build folder not found: {folder}", "OK");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "GitHub Pages Dağıtımı (İlk Kurulum):\nGitHub reposunda bir kerelik 'Settings > Pages' bölümüne gidip Branch olarak 'gh-pages' / '/ (root)' seçilmeli ve Save denilmelidir. Ardından build çıktısını bu branch'e push ederek Penta'ya URL olarak bağlayabilirsiniz.",
                    MessageType.Info);

                var folder = GetLastBuildFolder();
                if (Directory.Exists(folder))
                {
                    GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
                    if (GUILayout.Button("Deploy Last Build to GitHub Pages Now", GUILayout.Height(30)))
                    {
                        DeployToGitHubPagesDirectly();
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Version & Output Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            if (_context?.VersionData != null)
            {
                EditorGUILayout.TextField("Last WebGL Version", _context.VersionData.lastWebGlVersion);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Open WebGL Builds Folder", GUILayout.Height(28)))
            {
                var folder = Path.Combine(_context.OutputDirectory, "WebGL");
                if (Directory.Exists(folder))
                {
                    EditorUtility.RevealInFinder(folder);
                }
            }
        }

        public void PreBuild(AutoBuilderContext context)
        {
            _context = context;
            SaveSettings();

            ScriptingDefinesManager.ApplyCommonDefines(TargetGroup, context.EnableCheats, context.DisableRemoteData, "WEBGL_BUILD");

            PlayerSettings.WebGL.compressionFormat = _config.CompressionFormat;
            if (!string.IsNullOrEmpty(_config.TemplateName))
            {
                PlayerSettings.WebGL.template = _config.TemplateName;
            }
        }

        public BuildPlayerOptions ConfigureBuild(AutoBuilderContext context)
        {
            _context = context;

            var folder = Path.Combine(context.OutputDirectory, "WebGL", $"{context.CleanName}_v{context.Version}");
            if (Directory.Exists(folder))
            {
                ProcessRunner.DeleteDirectoryWithRetries(folder);
            }

            Directory.CreateDirectory(folder);

            return new BuildPlayerOptions
            {
                scenes = context.GetBuildScenes(),
                locationPathName = folder,
                target = Target,
                options = BuildOptions.None
            };
        }

        public void PostBuild(AutoBuilderContext context, BuildReport report)
        {
            _context = context;

            if (report.summary.result != BuildResult.Succeeded)
            {
                return;
            }

            if (context.VersionData != null)
            {
                context.VersionData.lastWebGlVersion = context.Version;
                EditorUtility.SetDirty(context.VersionData);
                AssetDatabase.SaveAssets();
            }

            var folder = Path.Combine(context.OutputDirectory, "WebGL", $"{context.CleanName}_v{context.Version}");

            if (_config.EnablePentaDeployment)
            {
                _ = PentaDeployer.DeployToPentaAsync(folder, _config, context.BaseName, context.Version);
            }

            if (_config.AutoStartLocalServer && Directory.Exists(folder))
            {
                WebGlLocalServer.StartServer(folder, _config.LocalServerPort);
            }

            if (Directory.Exists(folder))
            {
                EditorUtility.RevealInFinder(folder);
            }
        }

        public void SaveSettings()
        {
            if (_config != null)
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }
        }

        public void LoadSettings()
        {
            LoadOrCreateConfig();
        }

        private string GetLastBuildFolder()
        {
            return Path.Combine(_context.OutputDirectory, "WebGL", $"{_context.CleanName}_v{_context.Version}");
        }

        private void LoadOrCreateConfig()
        {
            var guids = AssetDatabase.FindAssets("t:WebGlBuildConfigSO");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<WebGlBuildConfigSO>(path);
            }

            if (_config == null)
            {
                var folder = "Assets/Plugins/TMN/AutoBuilder/Data";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var assetPath = Path.Combine(folder, "WebGlConfigSO.asset");
                _config = ScriptableObject.CreateInstance<WebGlBuildConfigSO>();
                AssetDatabase.CreateAsset(_config, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void DeployToGitHubPagesDirectly()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var scriptPath = Path.Combine(projectRoot, "Deploy-WebGl-To-GitHubPages.ps1");

            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("Script Missing", "Deploy-WebGl-To-GitHubPages.ps1 not found in project root.", "OK");
                return;
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            EditorUtility.DisplayProgressBar("Deploying to GitHub Pages", "Pushing WebGL build to gh-pages branch...", 0.5f);

            try
            {
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Error", "Could not start PowerShell process.", "OK");
                    return;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                EditorUtility.ClearProgressBar();

                if (process.ExitCode == 0)
                {
                    Debug.Log($"[GitHub Pages Deploy] {output}");
                    EditorUtility.DisplayDialog("Success", $"WebGL build for '{Application.productName}' was successfully pushed to 'gh-pages' branch!", "OK");
                }
                else
                {
                    Debug.LogError($"[GitHub Pages Deploy Error] {error}\n{output}");
                    EditorUtility.DisplayDialog("Deployment Failed", error, "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[GitHub Pages Deploy Exception] {ex.Message}");
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }
    }
}
