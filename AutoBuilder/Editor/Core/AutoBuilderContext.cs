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
