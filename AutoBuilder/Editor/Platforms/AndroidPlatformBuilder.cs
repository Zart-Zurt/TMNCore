using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public class AndroidPlatformBuilder : IPlatformBuilder
    {
        private AndroidBuildConfigSO _config;
        private AutoBuilderContext _context;

        public TargetPlatform Platform => TargetPlatform.Android;
        public BuildTarget Target => BuildTarget.Android;
        public BuildTargetGroup TargetGroup => BuildTargetGroup.Android;
        public string DisplayName => "Android";
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

            EditorGUILayout.LabelField("Android Build Options", EditorStyles.boldLabel);

            _config.BuildAppBundle = EditorGUILayout.Toggle("Build App Bundle (.aab)", _config.BuildAppBundle);
            _config.TargetArchitectures = (AndroidArchitecture)EditorGUILayout.EnumPopup("Target Architecture", _config.TargetArchitectures);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Keystore & Signing", EditorStyles.boldLabel);
            _config.UseCustomKeystore = EditorGUILayout.Toggle("Use Custom Keystore", _config.UseCustomKeystore);

            if (_config.UseCustomKeystore)
            {
                EditorGUILayout.BeginHorizontal();
                _config.KeystorePath = EditorGUILayout.TextField("Keystore Path", _config.KeystorePath);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    var selected = EditorUtility.OpenFilePanel("Select Keystore File", "", "keystore,jks");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        _config.KeystorePath = selected;
                    }
                }
                EditorGUILayout.EndHorizontal();

                _config.KeystorePassword = EditorGUILayout.PasswordField("Keystore Password", _config.KeystorePassword);
                _config.KeyAlias = EditorGUILayout.TextField("Key Alias", _config.KeyAlias);
                _config.KeyPassword = EditorGUILayout.PasswordField("Key Password", _config.KeyPassword);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Device Deployment", EditorStyles.boldLabel);
            _config.AutoInstallViaAdb = EditorGUILayout.Toggle("Auto Install via ADB (APK)", _config.AutoInstallViaAdb);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Version & Bundle Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            if (_context?.VersionData != null)
            {
                EditorGUILayout.IntField("Bundle Version Code", _context.VersionData.androidBundleVersionCode);
                EditorGUILayout.TextField("Last Android Version", _context.VersionData.lastAndroidVersion);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Android Build Folder", GUILayout.Height(28)))
            {
                var folder = Path.Combine(_context.OutputDirectory, "Android");
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

            ScriptingDefinesManager.ApplyCommonDefines(TargetGroup, context.EnableCheats, context.DisableRemoteData, "ANDROID_BUILD");

            EditorUserBuildSettings.buildAppBundle = _config.BuildAppBundle;
            PlayerSettings.Android.targetArchitectures = _config.TargetArchitectures;

            if (_config.UseCustomKeystore && File.Exists(_config.KeystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = _config.KeystorePath;
                PlayerSettings.Android.keystorePass = _config.KeystorePassword;
                PlayerSettings.Android.keyaliasName = _config.KeyAlias;
                PlayerSettings.Android.keyaliasPass = _config.KeyPassword;
            }

            if (context.VersionData != null)
            {
                context.VersionData.androidBundleVersionCode++;
                PlayerSettings.Android.bundleVersionCode = context.VersionData.androidBundleVersionCode;
                EditorUtility.SetDirty(context.VersionData);
                AssetDatabase.SaveAssets();
            }
        }

        public BuildPlayerOptions ConfigureBuild(AutoBuilderContext context)
        {
            _context = context;

            var folder = Path.Combine(context.OutputDirectory, "Android");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var extension = _config.BuildAppBundle ? "aab" : "apk";
            var fileName = $"{context.CleanName}_v{context.Version}.{extension}";
            var fullPath = Path.Combine(folder, fileName);

            if (File.Exists(fullPath))
            {
                ProcessRunner.DeleteFileWithRetries(fullPath);
            }

            return new BuildPlayerOptions
            {
                scenes = context.GetBuildScenes(),
                locationPathName = fullPath,
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
                context.VersionData.lastAndroidVersion = context.Version;
                EditorUtility.SetDirty(context.VersionData);
                AssetDatabase.SaveAssets();
            }

            var folder = Path.Combine(context.OutputDirectory, "Android");
            var extension = _config.BuildAppBundle ? "aab" : "apk";
            var fileName = $"{context.CleanName}_v{context.Version}.{extension}";
            var fullPath = Path.Combine(folder, fileName);

            if (!_config.BuildAppBundle && _config.AutoInstallViaAdb && File.Exists(fullPath))
            {
                Debug.Log($"[AutoBuilder Android] Attempting ADB install for: {fullPath}");
                var code = ProcessRunner.Run("adb", $"install -r \"{fullPath}\"");
                if (code == 0)
                {
                    Debug.Log("[AutoBuilder Android] ADB install completed successfully.");
                }
                else
                {
                    Debug.LogWarning("[AutoBuilder Android] ADB install failed or ADB is not in system PATH.");
                }
            }

            if (File.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
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
            var guids = AssetDatabase.FindAssets("t:AndroidBuildConfigSO");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<AndroidBuildConfigSO>(path);
            }

            if (_config == null)
            {
                var folder = "Assets/_GAME/Data";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var assetPath = Path.Combine(folder, "AndroidConfigSO.asset");
                _config = ScriptableObject.CreateInstance<AndroidBuildConfigSO>();
                AssetDatabase.CreateAsset(_config, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
