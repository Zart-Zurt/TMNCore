using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    [CreateAssetMenu(fileName = "WebGlConfigSO", menuName = "Auto Builder/WebGlConfigSO")]
    public class WebGlBuildConfigSO : ScriptableObject
    {
        private const string PentaApiKeyPrefKey = "AutoBuilder_WebGL_PentaApiKey";
        private const string SshPasswordPrefKey = "AutoBuilder_WebGL_SshPassword";

        [SerializeField] private WebGLCompressionFormat compressionFormat = WebGLCompressionFormat.Disabled;
        [SerializeField] private string templateName = "PROJECT:PentaWebGL";
        [SerializeField] private bool autoStartLocalServer;
        [SerializeField] private int localServerPort = 8080;
        [SerializeField] private bool enablePentaDeployment = true;
        [SerializeField] private string pentaServerUrl = "https://penta.zart-zurt.com";
        [SerializeField] private string pentaGameSlug = "";
        [SerializeField] private string pentaAspectRatio = "9:16";
        [SerializeField] private string sshHost = "72.62.38.185";
        [SerializeField] private int sshPort = 22;
        [SerializeField] private string sshUsername = "root";
        [SerializeField] private string sshRemotePath = "/root/penta";

        public WebGLCompressionFormat CompressionFormat
        {
            get => compressionFormat;
            set => compressionFormat = value;
        }

        public string TemplateName
        {
            get => templateName;
            set => templateName = value;
        }

        public bool AutoStartLocalServer
        {
            get => autoStartLocalServer;
            set => autoStartLocalServer = value;
        }

        public int LocalServerPort
        {
            get => localServerPort;
            set => localServerPort = value;
        }

        public bool EnablePentaDeployment
        {
            get => enablePentaDeployment;
            set => enablePentaDeployment = value;
        }

        public string PentaServerUrl
        {
            get => pentaServerUrl;
            set => pentaServerUrl = value;
        }

        public string PentaGameSlug
        {
            get => pentaGameSlug;
            set => pentaGameSlug = value;
        }

        public string PentaApiKey
        {
            get => EditorPrefs.GetString(PentaApiKeyPrefKey, "");
            set => EditorPrefs.SetString(PentaApiKeyPrefKey, value ?? "");
        }

        public string PentaAspectRatio
        {
            get => pentaAspectRatio;
            set => pentaAspectRatio = value;
        }

        public string SshHost
        {
            get => sshHost;
            set => sshHost = value;
        }

        public int SshPort
        {
            get => sshPort;
            set => sshPort = value;
        }

        public string SshUsername
        {
            get => sshUsername;
            set => sshUsername = value;
        }

        public string SshPassword
        {
            get => EditorPrefs.GetString(SshPasswordPrefKey, "");
            set => EditorPrefs.SetString(SshPasswordPrefKey, value ?? "");
        }

        public string SshRemotePath
        {
            get => sshRemotePath;
            set => sshRemotePath = value;
        }

        public string GetEffectiveServerUrl()
        {
            if (!string.IsNullOrEmpty(pentaServerUrl))
            {
                return pentaServerUrl.TrimEnd('/');
            }

            var fromSettings = PentaSettingsReader.GetServerUrl();
            if (!string.IsNullOrEmpty(fromSettings))
            {
                return fromSettings;
            }

            return "https://penta.zart-zurt.com";
        }

        public string GetEffectiveGameSlug()
        {
            if (!string.IsNullOrEmpty(pentaGameSlug))
            {
                return pentaGameSlug.Trim();
            }

            var fromSettings = PentaSettingsReader.GetGameId();
            if (!string.IsNullOrEmpty(fromSettings))
            {
                return fromSettings;
            }

            return string.IsNullOrWhiteSpace(Application.productName)
                ? "game"
                : Application.productName.Trim().ToLowerInvariant().Replace(" ", "-");
        }

        public string GetEffectiveApiKey()
        {
            var storedKey = PentaApiKey;
            if (!string.IsNullOrEmpty(storedKey))
            {
                return storedKey;
            }

            return PentaSettingsReader.GetApiKey();
        }

        public string GetEffectiveAspectRatio()
        {
            return string.IsNullOrEmpty(pentaAspectRatio) ? "9:16" : pentaAspectRatio.Trim();
        }

        public string GetEffectiveSshHost()
        {
            return string.IsNullOrEmpty(sshHost) ? "72.62.38.185" : sshHost.Trim();
        }

        public int GetEffectiveSshPort()
        {
            return sshPort > 0 ? sshPort : 22;
        }

        public string GetEffectiveSshUsername()
        {
            return string.IsNullOrEmpty(sshUsername) ? "root" : sshUsername.Trim();
        }

        public string GetEffectiveSshRemotePath()
        {
            return string.IsNullOrEmpty(sshRemotePath) ? "/root/penta" : sshRemotePath.TrimEnd('/');
        }
    }
}
