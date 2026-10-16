using UnityEngine;

namespace AutoBuilder.Editor
{
    public enum IosTargetSdk
    {
        DeviceSDK,
        SimulatorSDK
    }

    [CreateAssetMenu(fileName = "IosConfigSO", menuName = "Auto Builder/IosConfigSO")]
    public class IosBuildConfigSO : ScriptableObject
    {
        [SerializeField] private string bundleIdentifier = "";
        [SerializeField] private string signingTeamId = "";
        [SerializeField] private IosTargetSdk targetSdk = IosTargetSdk.DeviceSDK;
        [SerializeField] private bool autoZipXcodeProject = true;

        public string BundleIdentifier
        {
            get => bundleIdentifier;
            set => bundleIdentifier = value;
        }

        public string SigningTeamId
        {
            get => signingTeamId;
            set => signingTeamId = value;
        }

        public IosTargetSdk TargetSdk
        {
            get => targetSdk;
            set => targetSdk = value;
        }

        public bool AutoZipXcodeProject
        {
            get => autoZipXcodeProject;
            set => autoZipXcodeProject = value;
        }
    }
}
