using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public class SteamworksSdkSetupWindow : EditorWindow
    {
        private string _installParentDir = "";
        private SteamBuildConfigSO _config;
        private SteamSubPlatform _platform;
        private string _projectName = "";
        private string _sdkVersion = "164";
        private string _zipPath = "";

        private string TargetFolderName => $"steamworks_sdk_{_sdkVersion}_{_projectName}_{_platform.ToString().ToLower()}";
        private string TargetInstallPath => Path.Combine(_installParentDir, TargetFolderName);

        public static void ShowWindow(SteamSubPlatform platform, SteamBuildConfigSO config)
        {
            var window = GetWindow<SteamworksSdkSetupWindow>("Steamworks SDK Setup");
            window._platform = platform;
            window._config = config;
            window._projectName = Path.GetFileName(Path.GetDirectoryName(Application.dataPath));
            window.minSize = new Vector2(450, 320);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Steamworks SDK Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Setting up Steamworks SDK for: {_platform}", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Step 1: Download Steamworks SDK", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Download the official Steamworks SDK zip file from the partner portal:");
            if (GUILayout.Button("Open Steamworks Partner Portal", GUILayout.Height(25)))
            {
                Application.OpenURL("https://partner.steamgames.com/");
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Step 2: Select Downloaded ZIP File", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _zipPath = EditorGUILayout.TextField("SDK ZIP Path", _zipPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var path = EditorUtility.OpenFilePanel("Select Steamworks SDK ZIP", "", "zip");
                if (!string.IsNullOrEmpty(path))
                {
                    _zipPath = path;
                    ParseVersionFromZip(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Step 3: Choose Installation Parent Folder", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _installParentDir = EditorGUILayout.TextField("Install Parent Folder", _installParentDir);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Parent Installation Folder", _installParentDir, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _installParentDir = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_installParentDir))
            {
                EditorGUILayout.HelpBox($"Target Folder: {TargetFolderName}\nFull Path: {TargetInstallPath}", MessageType.None);
            }
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_zipPath) || string.IsNullOrEmpty(_installParentDir) || !File.Exists(_zipPath));
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Extract & Install SDK", GUILayout.Height(40)))
            {
                InstallSdk();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
        }

        private void ParseVersionFromZip(string path)
        {
            var fileName = Path.GetFileName(path);
            var match = Regex.Match(fileName, @"\d+");
            if (match.Success)
            {
                _sdkVersion = match.Value;
            }
        }

        private void InstallSdk()
        {
            try
            {
                if (Directory.Exists(TargetInstallPath))
                {
                    Directory.Delete(TargetInstallPath, true);
                }

                Directory.CreateDirectory(TargetInstallPath);

                Debug.Log($"[AutoBuilder] Extracting ZIP to: {TargetInstallPath}");
                ZipFile.ExtractToDirectory(_zipPath, TargetInstallPath);

                var expectedSdkPath = Path.Combine(TargetInstallPath, "sdk");
                if (!Directory.Exists(expectedSdkPath))
                {
                    Debug.LogWarning("[AutoBuilder] Expected 'sdk' subfolder not found. Scanning directories...");
                    var subdirs = Directory.GetDirectories(TargetInstallPath);
                    foreach (var subdir in subdirs)
                    {
                        if (Path.GetFileName(subdir).ToLower() == "sdk")
                        {
                            expectedSdkPath = subdir;
                            break;
                        }
                    }
                }

                if (Directory.Exists(expectedSdkPath))
                {
                    var finalSdkPath = Path.GetFullPath(expectedSdkPath);

                    var toolsPath = Path.Combine(finalSdkPath, "tools");
                    var guiZipPath = Path.Combine(toolsPath, "SteamPipeGUI.zip");
                    if (File.Exists(guiZipPath))
                    {
                        var guiExtractPath = Path.Combine(toolsPath, "SteamPipeGUI");
                        try
                        {
                            if (Directory.Exists(guiExtractPath))
                            {
                                Directory.Delete(guiExtractPath, true);
                            }
                            Directory.CreateDirectory(guiExtractPath);
                            ZipFile.ExtractToDirectory(guiZipPath, guiExtractPath);
                        }
                        catch (Exception zipEx)
                        {
                            Debug.LogError($"[AutoBuilder] Failed to extract SteamPipeGUI.zip: {zipEx.Message}");
                        }
                    }

                    if (_config != null)
                    {
                        switch (_platform)
                        {
                            case SteamSubPlatform.SteamDemo:
                                _config.SteamDemoSdkPath = finalSdkPath;
                                break;
                            case SteamSubPlatform.SteamPlaytest:
                                _config.SteamPlaytestSdkPath = finalSdkPath;
                                break;
                            case SteamSubPlatform.Steam:
                                _config.SteamSdkPath = finalSdkPath;
                                break;
                        }

                        EditorUtility.SetDirty(_config);
                        AssetDatabase.SaveAssets();
                    }

                    EditorUtility.DisplayDialog("Success", $"Steamworks SDK successfully installed to:\n{finalSdkPath}\n\nPath has been configured in Auto Builder.", "OK");
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Could not find the 'sdk' directory inside the extracted files. Please verify the ZIP contents.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoBuilder] SDK extraction failed: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"SDK Installation failed:\n{ex.Message}", "OK");
            }
        }
    }
}
