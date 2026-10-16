using UnityEngine;

namespace AutoBuilder.Editor
{
    public enum SteamSubPlatform
    {
        Local,
        SteamDemo,
        SteamPlaytest,
        Steam
    }

    [CreateAssetMenu(fileName = "SteamConfigSO", menuName = "Auto Builder/SteamConfigSO")]
    public class SteamBuildConfigSO : ScriptableObject
    {
        [SerializeField] private SteamSubPlatform subPlatform = SteamSubPlatform.Local;
        [SerializeField] private bool autoZip = true;
        [SerializeField] private bool autoRun = true;
        [SerializeField] private string steamDemoSdkPath = "";
        [SerializeField] private string steamPlaytestSdkPath = "";
        [SerializeField] private string steamSdkPath = "";
        [SerializeField] private string steamUsername = "";
        [SerializeField] private string steamPassword = "";
        [SerializeField] private uint steamDemoAppId = 4734900u;
        [SerializeField] private uint steamPlaytestAppId = 4734920u;
        [SerializeField] private uint steamAppId = 4593660u;
        [SerializeField] private uint steamLocalAppId = 480u;
        [SerializeField] private bool autoUploadToSteam = true;

        public SteamSubPlatform SubPlatform
        {
            get => subPlatform;
            set => subPlatform = value;
        }

        public bool AutoZip
        {
            get => autoZip;
            set => autoZip = value;
        }

        public bool AutoRun
        {
            get => autoRun;
            set => autoRun = value;
        }

        public string SteamDemoSdkPath
        {
            get => steamDemoSdkPath;
            set => steamDemoSdkPath = value;
        }

        public string SteamPlaytestSdkPath
        {
            get => steamPlaytestSdkPath;
            set => steamPlaytestSdkPath = value;
        }

        public string SteamSdkPath
        {
            get => steamSdkPath;
            set => steamSdkPath = value;
        }

        public string SteamUsername
        {
            get => steamUsername;
            set => steamUsername = value;
        }

        public string SteamPassword
        {
            get => steamPassword;
            set => steamPassword = value;
        }

        public uint SteamDemoAppId
        {
            get => steamDemoAppId;
            set => steamDemoAppId = value;
        }

        public uint SteamPlaytestAppId
        {
            get => steamPlaytestAppId;
            set => steamPlaytestAppId = value;
        }

        public uint SteamAppId
        {
            get => steamAppId;
            set => steamAppId = value;
        }

        public uint SteamLocalAppId
        {
            get => steamLocalAppId;
            set => steamLocalAppId = value;
        }

        public bool AutoUploadToSteam
        {
            get => autoUploadToSteam;
            set => autoUploadToSteam = value;
        }
    }
}
