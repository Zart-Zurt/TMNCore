using System;
using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    [CreateAssetMenu(fileName = "AndroidConfigSO", menuName = "Auto Builder/AndroidConfigSO")]
    public class AndroidBuildConfigSO : ScriptableObject
    {
        private const string KeystorePasswordPrefKey = "AutoBuilder_Android_KeystorePassword";
        private const string KeyPasswordPrefKey = "AutoBuilder_Android_KeyPassword";

        [SerializeField] private string applicationIdentifier = "";
        [SerializeField] private bool buildAppBundle;
        [SerializeField] private AndroidArchitecture targetArchitectures = AndroidArchitecture.ARM64;
        [SerializeField] private bool useCustomKeystore;
        [SerializeField] private string keystorePath = "";
        [SerializeField] private string keyAlias = "";
        [SerializeField] private bool autoInstallViaAdb = true;

        public string ApplicationIdentifier
        {
            get => applicationIdentifier;
            set => applicationIdentifier = value;
        }

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
            get
            {
                var envValue = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_PASSWORD");
                if (!string.IsNullOrEmpty(envValue))
                {
                    return envValue;
                }

                envValue = Environment.GetEnvironmentVariable("AUTOBUILDER_ANDROID_KEYSTORE_PASSWORD");
                if (!string.IsNullOrEmpty(envValue))
                {
                    return envValue;
                }

                return EditorPrefs.GetString(KeystorePasswordPrefKey, "");
            }
            set => EditorPrefs.SetString(KeystorePasswordPrefKey, value ?? "");
        }

        public string KeyAlias
        {
            get => keyAlias;
            set => keyAlias = value;
        }

        public string KeyPassword
        {
            get
            {
                var envValue = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEY_ALIAS_PASSWORD");
                if (!string.IsNullOrEmpty(envValue))
                {
                    return envValue;
                }

                envValue = Environment.GetEnvironmentVariable("AUTOBUILDER_ANDROID_KEY_PASSWORD");
                if (!string.IsNullOrEmpty(envValue))
                {
                    return envValue;
                }

                return EditorPrefs.GetString(KeyPasswordPrefKey, "");
            }
            set => EditorPrefs.SetString(KeyPasswordPrefKey, value ?? "");
        }

        public bool AutoInstallViaAdb
        {
            get => autoInstallViaAdb;
            set => autoInstallViaAdb = value;
        }
    }
}

