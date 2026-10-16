using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    [CreateAssetMenu(fileName = "AndroidConfigSO", menuName = "Auto Builder/AndroidConfigSO")]
    public class AndroidBuildConfigSO : ScriptableObject
    {
        [SerializeField] private bool buildAppBundle;
        [SerializeField] private AndroidArchitecture targetArchitectures = AndroidArchitecture.ARM64;
        [SerializeField] private bool useCustomKeystore;
        [SerializeField] private string keystorePath = "";
        [SerializeField] private string keystorePassword = "";
        [SerializeField] private string keyAlias = "";
        [SerializeField] private string keyPassword = "";
        [SerializeField] private bool autoInstallViaAdb = true;

        public bool BuildAppBundle
        {
            get => buildAppBundle;
            set => buildAppBundle = value;
        }

        public AndroidArchitecture TargetArchitectures
        {
            get => targetArchitectures;
            set => targetArchitectures = value;
        }

        public bool UseCustomKeystore
        {
            get => useCustomKeystore;
            set => useCustomKeystore = value;
        }

        public string KeystorePath
        {
            get => keystorePath;
            set => keystorePath = value;
        }

        public string KeystorePassword
        {
            get => keystorePassword;
            set => keystorePassword = value;
        }

        public string KeyAlias
        {
            get => keyAlias;
            set => keyAlias = value;
        }

        public string KeyPassword
        {
            get => keyPassword;
            set => keyPassword = value;
        }

        public bool AutoInstallViaAdb
        {
            get => autoInstallViaAdb;
            set => autoInstallViaAdb = value;
        }
    }
}
