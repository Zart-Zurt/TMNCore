using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public static class AutoBuilderCLI
    {
        public static void Build()
        {
            var args = Environment.GetCommandLineArgs();
            var platform = ResolvePlatform(args);
            ExecuteBuild(platform, args);
        }

        public static void BuildAndroid()
        {
            ExecuteBuild(TargetPlatform.Android, Environment.GetCommandLineArgs());
        }

        public static void BuildWebGL()
        {
            ExecuteBuild(TargetPlatform.WebGL, Environment.GetCommandLineArgs());
        }

        public static void BuildSteam()
        {
            ExecuteBuild(TargetPlatform.Steam, Environment.GetCommandLineArgs());
        }

        public static void BuildIOS()
        {
            ExecuteBuild(TargetPlatform.IOS, Environment.GetCommandLineArgs());
        }

        private static void ExecuteBuild(TargetPlatform platform, string[] args)
        {
            try
            {
                var context = new AutoBuilderContext();
                var versionData = LoadVersionData();
                context.VersionData = versionData;

                if (versionData != null)
                {
                    context.BaseName = versionData.baseName;
                    context.Version = versionData.buildVersion;
                }
                else
                {
                    context.BaseName = Application.productName;
                    context.Version = PlayerSettings.bundleVersion;
                }

                ApplyCliArguments(context, args);

                var builder = CreateBuilder(platform);
                builder.Initialize(context);
                builder.LoadSettings();

                if (!builder.IsHostSupported)
                {
                    Debug.LogError($"[AutoBuilderCLI] Host not supported for {builder.DisplayName}: {builder.HostUnsupportedReason}");
                    ExitFailure();
                    return;
                }

                if (EditorUserBuildSettings.activeBuildTarget != builder.Target)
                {
                    Debug.Log($"[AutoBuilderCLI] Switching active build target from {EditorUserBuildSettings.activeBuildTarget} to {builder.Target}...");
                    var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(builder.TargetGroup, builder.Target);
                    if (!switched)
                    {
                        Debug.LogError($"[AutoBuilderCLI] Failed to switch active build target to {builder.Target}.");
                        ExitFailure();
                        return;
                    }
                }

                SyncVersionData(context, versionData);

                Debug.Log($"[AutoBuilderCLI] Starting {builder.DisplayName} build v{context.Version}...");
                builder.PreBuild(context);

                var buildOptions = builder.ConfigureBuild(context);
                Debug.Log($"[AutoBuilderCLI] Building player to: {buildOptions.locationPathName}");
                var report = BuildPipeline.BuildPlayer(buildOptions);
                var summary = report.summary;

                if (summary.result != BuildResult.Succeeded)
                {
                    LogBuildErrors(report);
                    Debug.LogError($"[AutoBuilderCLI] Build failed: {summary.result}");
                    ExitFailure();
                    return;
                }

                Debug.Log($"[AutoBuilderCLI] Build succeeded in {summary.totalTime}. Output: {summary.outputPath}");
                builder.PostBuild(context, report);
                AssetDatabase.SaveAssets();
                ExitSuccess();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ExitFailure();
            }
        }

        private static TargetPlatform ResolvePlatform(string[] args)
        {
            if (TryGetArg(args, "-autoBuilderPlatform", out var explicitPlatform))
            {
                if (TryParsePlatform(explicitPlatform, out var parsedPlatform))
                {
                    return parsedPlatform;
                }

                throw new ArgumentException($"[AutoBuilderCLI] Unknown -autoBuilderPlatform value: {explicitPlatform}");
            }

            if (TryGetArg(args, "-buildTarget", out var unityBuildTarget))
            {
                if (TryParsePlatform(unityBuildTarget, out var parsedTarget))
                {
                    return parsedTarget;
                }
            }

            return EditorUserBuildSettings.activeBuildTarget switch
            {
                BuildTarget.Android => TargetPlatform.Android,
                BuildTarget.WebGL => TargetPlatform.WebGL,
                BuildTarget.iOS => TargetPlatform.IOS,
                _ => TargetPlatform.Steam
            };
        }

        private static bool TryParsePlatform(string rawValue, out TargetPlatform platform)
        {
            switch (rawValue.Trim().ToLowerInvariant())
            {
                case "android":
                    platform = TargetPlatform.Android;
                    return true;
                case "webgl":
                    platform = TargetPlatform.WebGL;
                    return true;
                case "ios":
                    platform = TargetPlatform.IOS;
                    return true;
                case "steam":
                case "win64":
                case "standalonewindows64":
                case "standalonewindows":
                case "standaloneosx":
                case "standalonelinux64":
                    platform = TargetPlatform.Steam;
                    return true;
                default:
                    platform = TargetPlatform.Steam;
                    return false;
            }
        }

        private static IPlatformBuilder CreateBuilder(TargetPlatform platform)
        {
            return platform switch
            {
                TargetPlatform.Android => new AndroidPlatformBuilder(),
                TargetPlatform.WebGL => new WebGlPlatformBuilder(),
                TargetPlatform.IOS => new IosPlatformBuilder(),
                _ => new StandaloneSteamBuilder()
            };
        }

        private static void ApplyCliArguments(AutoBuilderContext context, string[] args)
        {
            var hasExplicitVersion = TryGetArg(args, "-autoBuilderVersion", out var explicitVersion);
            if (hasExplicitVersion && !string.IsNullOrWhiteSpace(explicitVersion))
            {
                context.Version = explicitVersion.Trim();
            }
            else if (TryGetBoolArg(args, "-autoBuilderIncrementVersion", out var incrementVersion) && incrementVersion)
            {
                context.Version = IncrementVersionString(context.Version);
            }

            if (TryGetArg(args, "-autoBuilderOutput", out var customOutput) || TryGetArg(args, "-buildOutput", out customOutput))
            {
                context.CustomOutputPath = customOutput;
            }

            if (TryGetBoolArg(args, "-autoBuilderCheats", out var enableCheats))
            {
                context.EnableCheats = enableCheats;
            }
            else
            {
                context.EnableCheats = false;
            }

            if (TryGetBoolArg(args, "-autoBuilderDisableRemoteData", out var disableRemoteData))
            {
                context.DisableRemoteData = disableRemoteData;
            }
            else
            {
                context.DisableRemoteData = EditorPrefs.GetBool("AutoBuilder_DisableRemoteData", false);
            }

            if (TryGetBoolArg(args, "-autoBuilderDeploy", out var enableDeploy))
            {
                context.SkipDeploy = !enableDeploy;
            }
            else
            {
                context.SkipDeploy = true;
            }

            if (TryGetBoolArg(args, "-autoBuilderAdbInstall", out var allowAdbInstall))
            {
                context.AllowAdbInstall = allowAdbInstall;
            }
            else
            {
                context.AllowAdbInstall = false;
            }

            if (TryGetArg(args, "-autoBuilderPackageId", out var packageId))
            {
                context.AndroidApplicationIdentifierOverride = packageId;
            }

            if (TryGetBoolArg(args, "-autoBuilderAab", out var buildAab))
            {
                context.AndroidBuildAppBundleOverride = buildAab;
            }

            if (TryGetIntArg(args, "-autoBuilderVersionCode", out var versionCode) || TryGetIntArg(args, "-androidVersionCode", out versionCode))
            {
                context.AndroidBundleVersionCodeOverride = versionCode;
                context.IncrementAndroidVersionCode = false;
            }
            else if (TryGetBoolArg(args, "-autoBuilderIncrementVersionCode", out var incrementCode))
            {
                context.IncrementAndroidVersionCode = incrementCode;
            }

            if (TryGetArg(args, "-androidKeystoreName", out var keystoreName))
            {
                context.AndroidKeystorePathOverride = keystoreName;
            }

            if (TryGetArg(args, "-androidKeystorePass", out var keystorePass))
            {
                context.AndroidKeystorePasswordOverride = keystorePass;
            }

            if (TryGetArg(args, "-androidKeyaliasName", out var keyAlias))
            {
                context.AndroidKeyAliasOverride = keyAlias;
            }

            if (TryGetArg(args, "-androidKeyaliasPass", out var keyPass))
            {
                context.AndroidKeyPasswordOverride = keyPass;
            }
        }

        private static VersionData LoadVersionData()
        {
            var guids = AssetDatabase.FindAssets("t:VersionData");
            if (guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<VersionData>(path);
        }

        private static void SyncVersionData(AutoBuilderContext context, VersionData versionData)
        {
            if (!string.IsNullOrWhiteSpace(context.Version))
            {
                PlayerSettings.bundleVersion = context.Version;
            }

            if (versionData == null)
            {
                return;
            }

            var dirty = false;
            if (!string.IsNullOrWhiteSpace(context.Version) && versionData.buildVersion != context.Version)
            {
                versionData.buildVersion = context.Version;
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(versionData);
                AssetDatabase.SaveAssets();
            }
        }

        private static string IncrementVersionString(string currentVersion)
        {
            if (string.IsNullOrEmpty(currentVersion))
            {
                return currentVersion;
            }

            var match = Regex.Match(currentVersion, @"(\d+)(?!.*\d)");
            if (!match.Success)
            {
                return currentVersion;
            }

            var num = int.Parse(match.Value) + 1;
            return currentVersion.Remove(match.Index, match.Length).Insert(match.Index, num.ToString());
        }

        private static void LogBuildErrors(BuildReport report)
        {
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError($"[AutoBuilderCLI Build Error] {message.content}");
                    }
                }
            }
        }

        private static bool TryGetArg(string[] args, string flagName, out string value)
        {
            var prefix = flagName + "=";
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals(flagName, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        value = args[i + 1];
                        return true;
                    }
                }
                else if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = arg.Substring(prefix.Length);
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }

        private static bool TryGetBoolArg(string[] args, string flagName, out bool value)
        {
            if (TryGetArg(args, flagName, out var raw) && bool.TryParse(raw, out value))
            {
                return true;
            }

            value = false;
            return false;
        }

        private static bool TryGetIntArg(string[] args, string flagName, out int value)
        {
            if (TryGetArg(args, flagName, out var raw) && int.TryParse(raw, out value))
            {
                return true;
            }

            value = 0;
            return false;
        }

        private static void ExitSuccess()
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void ExitFailure()
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
