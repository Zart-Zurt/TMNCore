using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public class IosPlatformBuilder : IPlatformBuilder
    {
        private IosBuildConfigSO _config;
        private AutoBuilderContext _context;

        public TargetPlatform Platform => TargetPlatform.IOS;
        public BuildTarget Target => BuildTarget.iOS;
        public BuildTargetGroup TargetGroup => BuildTargetGroup.iOS;
        public string DisplayName => "iOS";
        public bool IsHostSupported => Application.platform == RuntimePlatform.OSXEditor;
        public string HostUnsupportedReason => "iOS build and platform switching is only supported on macOS (OSXEditor).";

        public void Initialize(AutoBuilderContext context)
        {
            _context = context;
            LoadOrCreateConfig();
        }

        public void DrawSettingsGUI()
        {
            if (!IsHostSupported)
            {
                EditorGUILayout.HelpBox(HostUnsupportedReason, MessageType.Error);
                return;
            }

            if (_config == null)
            {
                LoadOrCreateConfig();
            }

            EditorGUILayout.LabelField("iOS & Xcode Export Options", EditorStyles.boldLabel);

            _config.BundleIdentifier = EditorGUILayout.TextField("Bundle Identifier", _config.BundleIdentifier);
            _config.SigningTeamId = EditorGUILayout.TextField("Apple Team ID", _config.SigningTeamId);
            _config.TargetSdk = (IosTargetSdk)EditorGUILayout.EnumPopup("Target SDK", _config.TargetSdk);
            _config.AutoZipXcodeProject = EditorGUILayout.Toggle("Auto Zip Xcode Project", _config.AutoZipXcodeProject);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Version & Build Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            if (_context?.VersionData != null)
            {
                EditorGUILayout.IntField("iOS Build Number", _context.VersionData.iosBuildNumber);
                EditorGUILayout.TextField("Last iOS Version", _context.VersionData.lastIosVersion);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Open iOS Build Folder", GUILayout.Height(28)))
            {
                var folder = Path.Combine(_context.OutputDirectory, "iOS");
                if (Directory.Exists(folder))
                {
                    EditorUtility.RevealInFinder(folder);
                }
            }
        }

        public void PreBuild(AutoBuilderContext context)
        {
            if (!IsHostSupported)
            {
                throw new InvalidOperationException(HostUnsupportedReason);
            }

            _context = context;
            SaveSettings();

            ScriptingDefinesManager.ApplyCommonDefines(TargetGroup, context.EnableCheats, context.DisableRemoteData, "IOS_BUILD");

            if (!string.IsNullOrEmpty(_config.BundleIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, _config.BundleIdentifier);
            }

            if (!string.IsNullOrEmpty(_config.SigningTeamId))
            {
                PlayerSettings.iOS.appleDeveloperTeamID = _config.SigningTeamId;
            }

            try
            {
                var sdkProp = typeof(PlayerSettings.iOS).GetProperty("sdkVersion");
                if (sdkProp != null)
                {
                    var sdkEnumType = sdkProp.PropertyType;
                    var sdkVal = Enum.Parse(sdkEnumType, _config.TargetSdk.ToString());
                    sdkProp.SetValue(null, sdkVal);
                }
            }
            catch
            {
            }

            if (context.VersionData != null)
            {
                context.VersionData.iosBuildNumber++;
                PlayerSettings.iOS.buildNumber = context.VersionData.iosBuildNumber.ToString();
                EditorUtility.SetDirty(context.VersionData);
                AssetDatabase.SaveAssets();
            }
        }

        public BuildPlayerOptions ConfigureBuild(AutoBuilderContext context)
        {
            if (!IsHostSupported)
            {
                throw new InvalidOperationException(HostUnsupportedReason);
            }

            _context = context;

            var folder = Path.Combine(context.OutputDirectory, "iOS", $"{context.CleanName}_Xcode");
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
                context.VersionData.lastIosVersion = context.Version;
                EditorUtility.SetDirty(context.VersionData);
                AssetDatabase.SaveAssets();
            }

            var folder = Path.Combine(context.OutputDirectory, "iOS", $"{context.CleanName}_Xcode");

            if (_config.AutoZipXcodeProject && Directory.Exists(folder))
            {
                var zipPath = Path.Combine(context.OutputDirectory, "iOS", $"{context.CleanName}_Xcode_v{context.Version}.zip");
                ProcessRunner.CreateZipWithTar(folder, zipPath);
                if (File.Exists(zipPath))
                {
                    EditorUtility.RevealInFinder(zipPath);
                    return;
                }
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

        private void LoadOrCreateConfig()
        {
            var guids = AssetDatabase.FindAssets("t:IosBuildConfigSO");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<IosBuildConfigSO>(path);
            }

            if (_config == null)
            {
                var folder = "Assets/Plugins/TMN/AutoBuilder/Data";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var assetPath = Path.Combine(folder, "IosConfigSO.asset");
                _config = ScriptableObject.CreateInstance<IosBuildConfigSO>();
                AssetDatabase.CreateAsset(_config, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
