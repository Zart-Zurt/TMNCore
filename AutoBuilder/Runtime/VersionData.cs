using UnityEngine;

namespace AutoBuilder
{
    [CreateAssetMenu(fileName = "VersionSO", menuName = "Auto Builder/VersionSO")]
    public class VersionData : ScriptableObject
    {
        public string baseName = "GameName_v";
        public string buildVersion = "0.1.0";
        public int androidBundleVersionCode = 1;
        public int iosBuildNumber = 1;
        public string lastDemoVersion = "None";
        public string lastPlaytestVersion = "None";
        public string lastSteamVersion = "None";
        public string lastAndroidVersion = "None";
        public string lastIosVersion = "None";
        public string lastWebGlVersion = "None";

        public string BaseName
        {
            get => baseName;
            set => baseName = value;
        }

        public string BuildVersion
        {
            get => buildVersion;
            set => buildVersion = value;
        }

        public int AndroidBundleVersionCode
        {
            get => androidBundleVersionCode;
            set => androidBundleVersionCode = value;
        }

        public int IosBuildNumber
        {
            get => iosBuildNumber;
            set => iosBuildNumber = value;
        }

        public string LastDemoVersion
        {
            get => lastDemoVersion;
            set => lastDemoVersion = value;
        }

        public string LastPlaytestVersion
        {
            get => lastPlaytestVersion;
            set => lastPlaytestVersion = value;
        }

        public string LastSteamVersion
        {
            get => lastSteamVersion;
            set => lastSteamVersion = value;
        }

        public string LastAndroidVersion
        {
            get => lastAndroidVersion;
            set => lastAndroidVersion = value;
        }

        public string LastIosVersion
        {
            get => lastIosVersion;
            set => lastIosVersion = value;
        }

        public string LastWebGlVersion
        {
            get => lastWebGlVersion;
            set => lastWebGlVersion = value;
        }
    }
}
