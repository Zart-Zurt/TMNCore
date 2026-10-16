using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AutoBuilder.Editor
{
    [InitializeOnLoad]
    public class AutoBuilderWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private VersionData _versionData;
        private AutoBuilderContext _context;
        private IPlatformBuilder[] _builders;
        private int _selectedPlatformIndex;
        private bool _autoIncrementVersion = true;

        static AutoBuilderWindow()
        {
            EditorApplication.delayCall += ResumePendingBuild;
        }

        private IPlatformBuilder ActiveBuilder => _builders[_selectedPlatformIndex];

        public static void OpenWindow()
        {
            var window = GetWindow<AutoBuilderWindow>("Auto Builder");
            window.minSize = new Vector2(480, 560);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeBuilders();
            LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void InitializeBuilders()
        {
            _context = new AutoBuilderContext();

            _builders = new IPlatformBuilder[]
            {
                new StandaloneSteamBuilder(),
                new AndroidPlatformBuilder(),
                new IosPlatformBuilder(),
                new WebGlPlatformBuilder()
            };

            foreach (var builder in _builders)
            {
                builder.Initialize(_context);
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeaderAndGlobalSettings();
            EditorGUILayout.Space();

            DrawPlatformToolbar();
            EditorGUILayout.Space();

            DrawPlatformSwitchStatus();
            EditorGUILayout.Space();

            DrawActivePlatformSettings();
            EditorGUILayout.Space();

            DrawBuildExecutionSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeaderAndGlobalSettings()
        {
            EditorGUILayout.LabelField("Auto Builder - Global Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_versionData == null)
            {
                EditorGUILayout.HelpBox("VersionData ScriptableObject is missing! It will be recreated on launch.", MessageType.Warning);
            }

            var newBaseName = EditorGUILayout.TextField("Base Name", _context.BaseName);
            if (newBaseName != _context.BaseName)
            {
                _context.BaseName = newBaseName;
                if (_versionData != null)
                {
                    _versionData.baseName = _context.BaseName;
                    EditorUtility.SetDirty(_versionData);
                }
            }

            var newVersion = EditorGUILayout.TextField("Version", _context.Version);
            if (newVersion != _context.Version)
            {
                _context.Version = newVersion;
                PlayerSettings.bundleVersion = _context.Version;
                if (_versionData != null)
                {
                    _versionData.buildVersion = _context.Version;
                    EditorUtility.SetDirty(_versionData);
                }
            }

            _autoIncrementVersion = EditorGUILayout.Toggle("Auto Increment Version", _autoIncrementVersion);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Defines", EditorStyles.boldLabel);
            _context.EnableCheats = EditorGUILayout.Toggle("Enable Cheats", _context.EnableCheats);
            _context.DisableRemoteData = EditorGUILayout.Toggle("Disable Remote Data", _context.DisableRemoteData);
        }

        private void DrawPlatformToolbar()
        {
            EditorGUILayout.LabelField("Target Platform", EditorStyles.boldLabel);

            var platformNames = new string[_builders.Length];
            for (var i = 0; i < _builders.Length; i++)
            {
                var builder = _builders[i];
                if (!builder.IsHostSupported)
                {
                    platformNames[i] = $"{builder.DisplayName} (Mac Only)";
                }
                else
                {
                    platformNames[i] = builder.DisplayName;
                }
            }

            var newIndex = GUILayout.Toolbar(_selectedPlatformIndex, platformNames, GUILayout.Height(32));
            if (newIndex != _selectedPlatformIndex)
            {
                _selectedPlatformIndex = newIndex;
                EditorPrefs.SetInt("AutoBuilder_SelectedPlatformIndex", _selectedPlatformIndex);
            }
        }

        private void DrawPlatformSwitchStatus()
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            var targetPlatform = ActiveBuilder.Target;

            if (activeTarget != targetPlatform)
            {
                EditorGUILayout.HelpBox(
                    $"Current active Unity target is '{activeTarget}', but selected builder target is '{targetPlatform}'. Please switch platform before building.",
                    MessageType.Warning);

                if (!ActiveBuilder.IsHostSupported)
                {
                    EditorGUILayout.HelpBox(ActiveBuilder.HostUnsupportedReason, MessageType.Error);
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Button($"Switch Platform to {ActiveBuilder.DisplayName} (Unsupported Host)", GUILayout.Height(30));
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    GUI.backgroundColor = new Color(1f, 0.7f, 0.2f);
                    if (GUILayout.Button($"Switch Platform to {ActiveBuilder.DisplayName}", GUILayout.Height(34)))
                    {
                        SwitchPlatform(ActiveBuilder);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Active Unity build target is correctly set to '{activeTarget}'.", MessageType.Info);
            }
        }

        private void DrawActivePlatformSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            ActiveBuilder.DrawSettingsGUI();
            EditorGUILayout.EndVertical();
        }

        private void DrawBuildExecutionSection()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.Space();

            if (!ActiveBuilder.IsHostSupported)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button($"Build Project ({ActiveBuilder.HostUnsupportedReason})", GUILayout.Height(42));
                EditorGUI.EndDisabledGroup();
                return;
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Build Project ({ActiveBuilder.DisplayName})", GUILayout.Height(42)))
            {
                BuildProject();
            }
            GUI.backgroundColor = Color.white;
        }

        private void SwitchPlatform(IPlatformBuilder builder)
        {
            if (!builder.IsHostSupported)
            {
                EditorUtility.DisplayDialog("Host Not Supported", builder.HostUnsupportedReason, "OK");
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(builder.TargetGroup, builder.Target);
        }

        private void BuildProject()
        {
            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning("[AutoBuilder] Unity is currently compiling scripts. Please wait for compilation to finish before building.");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            if (!ActiveBuilder.IsHostSupported)
            {
                EditorUtility.DisplayDialog("Host Not Supported", ActiveBuilder.HostUnsupportedReason, "OK");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != ActiveBuilder.Target)
            {
                var shouldSwitch = EditorUtility.DisplayDialog(
                    "Switch Platform Required",
                    $"The active build target ({EditorUserBuildSettings.activeBuildTarget}) does not match the target ({ActiveBuilder.Target}). Do you want to switch platform now?",
                    "Switch & Build",
                    "Cancel");

                if (shouldSwitch)
                {
                    SwitchPlatform(ActiveBuilder);
                }
                return;
            }

            if (_autoIncrementVersion)
            {
                IncrementVersion();
            }

            SaveSettings();

            EditorPrefs.SetBool("AutoBuilder_PendingBuild", true);
            ActiveBuilder.PreBuild(_context);

            if (EditorApplication.isCompiling)
            {
                Debug.Log("[AutoBuilder] Compilation triggered by pre-build step. Build will resume automatically after compilation...");
                return;
            }

            EditorPrefs.SetBool("AutoBuilder_PendingBuild", false);
            ExecuteBuild();
        }

        private void ExecuteBuild()
        {
            Debug.Log($"[AutoBuilder] Starting build for: {ActiveBuilder.DisplayName}");

            var buildOptions = ActiveBuilder.ConfigureBuild(_context);
            var report = BuildPipeline.BuildPlayer(buildOptions);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AutoBuilder] Build succeeded for {ActiveBuilder.DisplayName}! Time: {summary.totalTime}");
                ActiveBuilder.PostBuild(_context, report);
            }
            else
            {
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error || message.type == LogType.Exception)
                        {
                            Debug.LogError($"[AutoBuilder Build Error] {message.content}");
                        }
                    }
                }

                Debug.LogError($"[AutoBuilder] Build failed: {summary.result}");
            }
        }

        private static void ResumePendingBuild()
        {
            if (!EditorPrefs.GetBool("AutoBuilder_PendingBuild", false))
            {
                return;
            }

            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += ResumePendingBuild;
                return;
            }

            EditorPrefs.SetBool("AutoBuilder_PendingBuild", false);
            Debug.Log("[AutoBuilder] Compilation finished. Resuming build...");

            var window = GetWindow<AutoBuilderWindow>("Auto Builder");
            window.InitializeBuilders();
            window.LoadSettings();
            window.ExecuteBuild();
        }

        private void IncrementVersion()
        {
            var match = Regex.Match(_context.Version, @"(\d+)(?!.*\d)");
            if (match.Success)
            {
                var num = int.Parse(match.Value);
                num++;
                _context.Version = _context.Version.Remove(match.Index, match.Length).Insert(match.Index, num.ToString());
                PlayerSettings.bundleVersion = _context.Version;

                if (_versionData != null)
                {
                    _versionData.buildVersion = _context.Version;
                    EditorUtility.SetDirty(_versionData);
                }

                Debug.Log($"[AutoBuilder] Version automatically incremented to: {_context.Version}");
            }
        }

        private void LoadSettings()
        {
            FindOrCreateVersionData();

            _selectedPlatformIndex = EditorPrefs.GetInt("AutoBuilder_SelectedPlatformIndex", 0);
            if (_selectedPlatformIndex < 0 || _selectedPlatformIndex >= _builders.Length)
            {
                _selectedPlatformIndex = 0;
            }

            _autoIncrementVersion = EditorPrefs.GetBool("AutoBuilder_AutoIncrementVersion", true);

            if (_versionData != null)
            {
                _context.BaseName = _versionData.baseName;
                _context.Version = _versionData.buildVersion;
                _context.VersionData = _versionData;
            }
            else
            {
                _context.BaseName = EditorPrefs.GetString("AutoBuilder_BaseName", "TotallyNormalMeatCo");
                _context.Version = PlayerSettings.bundleVersion;
            }

            _context.EnableCheats = EditorPrefs.GetBool("AutoBuilder_EnableCheats", true);
            _context.DisableRemoteData = EditorPrefs.GetBool("AutoBuilder_DisableRemoteData", false);

            foreach (var builder in _builders)
            {
                builder.LoadSettings();
            }
        }

        private void SaveSettings()
        {
            EditorPrefs.SetInt("AutoBuilder_SelectedPlatformIndex", _selectedPlatformIndex);
            EditorPrefs.SetBool("AutoBuilder_AutoIncrementVersion", _autoIncrementVersion);
            EditorPrefs.SetString("AutoBuilder_BaseName", _context.BaseName);
            EditorPrefs.SetBool("AutoBuilder_EnableCheats", _context.EnableCheats);
            EditorPrefs.SetBool("AutoBuilder_DisableRemoteData", _context.DisableRemoteData);

            if (_versionData != null)
            {
                _versionData.baseName = _context.BaseName;
                _versionData.buildVersion = _context.Version;
                EditorUtility.SetDirty(_versionData);
                AssetDatabase.SaveAssets();
            }

            PlayerSettings.bundleVersion = _context.Version;

            foreach (var builder in _builders)
            {
                builder.SaveSettings();
            }
        }

        private void FindOrCreateVersionData()
        {
            var guids = AssetDatabase.FindAssets("t:VersionData");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _versionData = AssetDatabase.LoadAssetAtPath<VersionData>(path);
            }

            if (_versionData == null)
            {
                var folderPath = "Assets/Plugins/TMN/AutoBuilder/Data";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var assetPath = Path.Combine(folderPath, "VersionSO.asset");
                _versionData = CreateInstance<VersionData>();
                AssetDatabase.CreateAsset(_versionData, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}