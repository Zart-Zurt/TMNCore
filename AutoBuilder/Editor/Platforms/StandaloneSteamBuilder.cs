using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AutoBuilder.Editor
{
    public class StandaloneSteamBuilder : IPlatformBuilder
    {
        private SteamBuildConfigSO _config;
        private AutoBuilderContext _context;
        private CancellationTokenSource _dialogCloserCts;

        public TargetPlatform Platform => TargetPlatform.Steam;
        public BuildTarget Target => BuildTarget.StandaloneWindows64;
        public BuildTargetGroup TargetGroup => BuildTargetGroup.Standalone;
        public string DisplayName => "Steam / Windows";
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

            EditorGUILayout.LabelField("Steam & Windows Options", EditorStyles.boldLabel);

            _config.SubPlatform = (SteamSubPlatform)EditorGUILayout.EnumPopup("Sub Platform", _config.SubPlatform);

            if (_config.SubPlatform == SteamSubPlatform.Local)
            {
                _config.AutoZip = EditorGUILayout.Toggle("Auto Zip", _config.AutoZip);
                _config.AutoRun = EditorGUILayout.Toggle("Auto Run", _config.AutoRun);

                EditorGUILayout.Space();
                if (GUILayout.Button("Open Local Build Folder", GUILayout.Height(28)))
                {
                    OpenLocalBuildFolder();
                }
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Steam SDK Paths", EditorStyles.boldLabel);

                if (_config.SubPlatform == SteamSubPlatform.SteamDemo)
                {
                    _config.SteamDemoSdkPath = DrawFolderField("Demo SDK Path", _config.SteamDemoSdkPath, SteamSubPlatform.SteamDemo);
                    if (!SteamDeployer.ValidateSteamSdkPath(_config.SteamDemoSdkPath))
                    {
                        EditorGUILayout.HelpBox("Invalid Demo SDK path! Folder must contain tools/ContentBuilder/content", MessageType.Error);
                    }
                }
                else if (_config.SubPlatform == SteamSubPlatform.SteamPlaytest)
                {
                    _config.SteamPlaytestSdkPath = DrawFolderField("Playtest SDK Path", _config.SteamPlaytestSdkPath, SteamSubPlatform.SteamPlaytest);
                    if (!SteamDeployer.ValidateSteamSdkPath(_config.SteamPlaytestSdkPath))
                    {
                        EditorGUILayout.HelpBox("Invalid Playtest SDK path! Folder must contain tools/ContentBuilder/content", MessageType.Error);
                    }
                }
                else if (_config.SubPlatform == SteamSubPlatform.Steam)
                {
                    _config.SteamSdkPath = DrawFolderField("Main SDK Path", _config.SteamSdkPath, SteamSubPlatform.Steam);
                    if (!SteamDeployer.ValidateSteamSdkPath(_config.SteamSdkPath))
                    {
                        EditorGUILayout.HelpBox("Invalid Main SDK path! Folder must contain tools/ContentBuilder/content", MessageType.Error);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Steam Credentials & App IDs", EditorStyles.boldLabel);
                _config.SteamUsername = EditorGUILayout.TextField("Steam Username", _config.SteamUsername);
                _config.SteamPassword = EditorGUILayout.PasswordField("Steam Password", _config.SteamPassword);

                if (_config.SubPlatform == SteamSubPlatform.SteamDemo)
                {
                    _config.SteamDemoAppId = (uint)EditorGUILayout.IntField("Demo App ID", (int)_config.SteamDemoAppId);
                }
                else if (_config.SubPlatform == SteamSubPlatform.SteamPlaytest)
                {
                    _config.SteamPlaytestAppId = (uint)EditorGUILayout.IntField("Playtest App ID", (int)_config.SteamPlaytestAppId);
                }
                else if (_config.SubPlatform == SteamSubPlatform.Steam)
                {
                    _config.SteamAppId = (uint)EditorGUILayout.IntField("Main App ID", (int)_config.SteamAppId);
                }

                _config.AutoUploadToSteam = EditorGUILayout.Toggle("Auto Upload to Steam", _config.AutoUploadToSteam);

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Login SteamCMD", GUILayout.Height(28)))
                {
                    var sdk = GetCurrentSdkPath();
                    SteamDeployer.LoginSteamCmd(sdk, _config.SteamUsername, _config.SteamPassword);
                }

                if (GUILayout.Button("Open SteamPipe GUI", GUILayout.Height(28)))
                {
                    var sdk = GetCurrentSdkPath();
                    SteamDeployer.OpenSteamPipeGui(sdk);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Last Uploads", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                if (_context?.VersionData != null)
                {
                    EditorGUILayout.TextField("Last Demo", _context.VersionData.lastDemoVersion);
                    EditorGUILayout.TextField("Last Playtest", _context.VersionData.lastPlaytestVersion);
                    EditorGUILayout.TextField("Last Steam", _context.VersionData.lastSteamVersion);
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        public void PreBuild(AutoBuilderContext context)
        {
            _context = context;
            SaveSettings();

            var activeAppId = GetActiveAppId();
            SteamDeployer.UpdateAppIdInScript(activeAppId);
            SteamDeployer.WriteLocalSteamAppIdFile(activeAppId, _config.SubPlatform == SteamSubPlatform.Local);

            ScriptingDefinesManager.ApplyCommonDefines(TargetGroup, context.EnableCheats, context.DisableRemoteData, "STEAM_BUILD");

            ProcessRunner.KillRunningProcesses(context.BaseName);
            _dialogCloserCts = SteamDeployer.StartWindowAutoCloser();
        }

        public BuildPlayerOptions ConfigureBuild(AutoBuilderContext context)
        {
            _context = context;

            var cleanName = context.CleanName;
            var buildFolderName = _config.SubPlatform switch
            {
                SteamSubPlatform.SteamDemo => $"{cleanName}_Demo",
                SteamSubPlatform.SteamPlaytest => $"{cleanName}_Playtest",
                SteamSubPlatform.Steam => cleanName,
                _ => $"{context.BaseName}{context.Version}"
            };

            var exeName = _config.SubPlatform == SteamSubPlatform.Local
                ? $"{context.BaseName}{context.Version}.exe"
                : $"{cleanName}.exe";

            var buildFolder = Path.Combine(context.OutputDirectory, buildFolderName);
            var zipPath = Path.Combine(context.OutputDirectory, $"{buildFolderName}.zip");

            if (Directory.Exists(buildFolder))
            {
                ProcessRunner.DeleteDirectoryWithRetries(buildFolder);
            }

            if (_config.SubPlatform == SteamSubPlatform.Local && _config.AutoZip && File.Exists(zipPath))
            {
                ProcessRunner.DeleteFileWithRetries(zipPath);
            }

            Directory.CreateDirectory(buildFolder);

            return new BuildPlayerOptions
            {
                scenes = context.GetBuildScenes(),
                locationPathName = Path.Combine(buildFolder, exeName),
                target = Target,
                options = BuildOptions.None
            };
        }

        public void PostBuild(AutoBuilderContext context, BuildReport report)
        {
            _context = context;

            if (_dialogCloserCts != null)
            {
                _dialogCloserCts.Cancel();
                _dialogCloserCts = null;
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                return;
            }

            var cleanName = context.CleanName;
            var buildFolderName = _config.SubPlatform switch
            {
                SteamSubPlatform.SteamDemo => $"{cleanName}_Demo",
                SteamSubPlatform.SteamPlaytest => $"{cleanName}_Playtest",
                SteamSubPlatform.Steam => cleanName,
                _ => $"{context.BaseName}{context.Version}"
            };

            var exeName = _config.SubPlatform == SteamSubPlatform.Local
                ? $"{context.BaseName}{context.Version}.exe"
                : $"{cleanName}.exe";

            var buildFolder = Path.Combine(context.OutputDirectory, buildFolderName);

            try
            {
                var appIdFilePath = Path.Combine(buildFolder, "steam_appid.txt");
                File.WriteAllText(appIdFilePath, GetActiveAppId().ToString());
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder] Failed to create steam_appid.txt: {exception.Message}");
            }

            if (_config.SubPlatform == SteamSubPlatform.Local)
            {
                var zipPath = Path.Combine(context.OutputDirectory, $"{buildFolderName}.zip");
                if (_config.AutoZip)
                {
                    ProcessRunner.CreateZipWithTar(buildFolder, zipPath);
                    EditorUtility.RevealInFinder(zipPath);
                }
                else
                {
                    EditorUtility.RevealInFinder(buildFolder);
                }

                if (_config.AutoRun)
                {
                    var exePath = Path.Combine(buildFolder, exeName);
                    if (File.Exists(exePath))
                    {
                        Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = buildFolder });
                    }
                }
            }
            else
            {
                var currentSdkPath = GetCurrentSdkPath();
                var contentDir = Path.Combine(currentSdkPath, "tools", "ContentBuilder", "content");

                if (Directory.Exists(contentDir))
                {
                    ProcessRunner.DeleteDirectoryWithRetries(contentDir);
                }

                Directory.CreateDirectory(contentDir);
                ProcessRunner.CopyDirectory(buildFolder, contentDir, "steam_appid.txt");

                if (context.VersionData != null)
                {
                    if (_config.SubPlatform == SteamSubPlatform.SteamDemo)
                    {
                        context.VersionData.lastDemoVersion = context.Version;
                    }
                    else if (_config.SubPlatform == SteamSubPlatform.SteamPlaytest)
                    {
                        context.VersionData.lastPlaytestVersion = context.Version;
                    }
                    else if (_config.SubPlatform == SteamSubPlatform.Steam)
                    {
                        context.VersionData.lastSteamVersion = context.Version;
                    }

                    EditorUtility.SetDirty(context.VersionData);
                    AssetDatabase.SaveAssets();
                }

                if (_config.AutoUploadToSteam)
                {
                    SteamDeployer.UploadToSteam(currentSdkPath, GetActiveAppId(), context.Version, _config.SteamUsername, _config.SteamPassword);
                }
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

        private void LoadOrCreateConfig()
        {
            var guids = AssetDatabase.FindAssets("t:SteamBuildConfigSO");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<SteamBuildConfigSO>(path);
            }

            if (_config == null)
            {
                var folder = "Assets/Plugins/TMN/AutoBuilder/Data";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var assetPath = Path.Combine(folder, "SteamConfigSO.asset");
                _config = ScriptableObject.CreateInstance<SteamBuildConfigSO>();
                AssetDatabase.CreateAsset(_config, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private string GetCurrentSdkPath()
        {
            return _config.SubPlatform switch
            {
                SteamSubPlatform.SteamDemo => _config.SteamDemoSdkPath,
                SteamSubPlatform.SteamPlaytest => _config.SteamPlaytestSdkPath,
                SteamSubPlatform.Steam => _config.SteamSdkPath,
                _ => ""
            };
        }

        private uint GetActiveAppId()
        {
            return _config.SubPlatform switch
            {
                SteamSubPlatform.SteamDemo => _config.SteamDemoAppId,
                SteamSubPlatform.SteamPlaytest => _config.SteamPlaytestAppId,
                SteamSubPlatform.Steam => _config.SteamAppId,
                _ => _config.SteamLocalAppId
            };
        }

        private string DrawFolderField(string label, string path, SteamSubPlatform platform)
        {
            EditorGUILayout.BeginHorizontal();
            var newPath = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select SDK Folder", path, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    newPath = selected;
                }
            }

            if (GUILayout.Button("Setup SDK", GUILayout.Width(80)))
            {
                SteamworksSdkSetupWindow.ShowWindow(platform, _config);
            }
            EditorGUILayout.EndHorizontal();
            return newPath;
        }

        private void OpenLocalBuildFolder()
        {
            var folder = Path.Combine(_context.OutputDirectory, $"{_context.BaseName}{_context.Version}");
            if (Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
            }
            else if (Directory.Exists(_context.OutputDirectory))
            {
                Process.Start(new ProcessStartInfo { FileName = _context.OutputDirectory, UseShellExecute = true });
            }
        }
    }
}
