using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public class AutoBuilderContext
    {
        private string _baseName = "";
        private string _version = "";
        private bool _enableCheats = true;
        private bool _disableRemoteData;
        private VersionData _versionData;
        private string _customOutputPath = "";
        private bool _skipDeploy;
        private bool _allowAdbInstall = true;
        private bool _incrementAndroidVersionCode = true;
        private string _androidApplicationIdentifierOverride = "";
        private int? _androidBundleVersionCodeOverride;
        private bool? _androidBuildAppBundleOverride;
        private string _androidKeystorePathOverride = "";
        private string _androidKeystorePasswordOverride = "";
        private string _androidKeyAliasOverride = "";
        private string _androidKeyPasswordOverride = "";

        public string BaseName
        {
            get => _baseName;
            set => _baseName = value;
        }

        public string Version
        {
            get => _version;
            set => _version = value;
        }

        public bool EnableCheats
        {
            get => _enableCheats;
            set => _enableCheats = value;
        }

        public bool DisableRemoteData
        {
            get => _disableRemoteData;
            set => _disableRemoteData = value;
        }

        public VersionData VersionData
        {
            get => _versionData;
            set => _versionData = value;
        }

        public string CustomOutputPath
        {
            get => _customOutputPath;
            set => _customOutputPath = value ?? "";
        }

        public bool SkipDeploy
        {
            get => _skipDeploy;
            set => _skipDeploy = value;
        }

        public bool AllowAdbInstall
        {
            get => _allowAdbInstall;
            set => _allowAdbInstall = value;
        }

        public bool IncrementAndroidVersionCode
        {
            get => _incrementAndroidVersionCode;
            set => _incrementAndroidVersionCode = value;
        }

        public string AndroidApplicationIdentifierOverride
        {
            get => _androidApplicationIdentifierOverride;
            set => _androidApplicationIdentifierOverride = value ?? "";
        }

        public int? AndroidBundleVersionCodeOverride
        {
            get => _androidBundleVersionCodeOverride;
            set => _androidBundleVersionCodeOverride = value;
        }

        public bool? AndroidBuildAppBundleOverride
        {
            get => _androidBuildAppBundleOverride;
            set => _androidBuildAppBundleOverride = value;
        }

        public string AndroidKeystorePathOverride
        {
            get => _androidKeystorePathOverride;
            set => _androidKeystorePathOverride = value ?? "";
        }

        public string AndroidKeystorePasswordOverride
        {
            get => _androidKeystorePasswordOverride;
            set => _androidKeystorePasswordOverride = value ?? "";
        }

        public string AndroidKeyAliasOverride
        {
            get => _androidKeyAliasOverride;
            set => _androidKeyAliasOverride = value ?? "";
        }

        public string AndroidKeyPasswordOverride
        {
            get => _androidKeyPasswordOverride;
            set => _androidKeyPasswordOverride = value ?? "";
        }

        public string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds"));

        public string CleanName
        {
            get
            {
                var clean = _baseName.Replace("_v", "").Replace("_", "").Trim();
                return string.IsNullOrEmpty(clean) ? "Game" : clean;
            }
        }

        public string[] GetBuildScenes()
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes.ToArray();
        }
    }
}
