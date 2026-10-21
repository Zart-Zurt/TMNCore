using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    [CreateAssetMenu(fileName = "WebGlConfigSO", menuName = "Auto Builder/WebGlConfigSO")]
    public class WebGlBuildConfigSO : ScriptableObject
    {
        private const string PentaApiKeyPrefKey = "AutoBuilder_WebGL_PentaApiKey";

        [SerializeField] private WebGLCompressionFormat compressionFormat = WebGLCompressionFormat.Disabled;
        [SerializeField] private string templateName = "APPLICATION:Default";
        [SerializeField] private bool autoStartLocalServer;
        [SerializeField] private int localServerPort = 8080;
        [SerializeField] private bool enablePentaDeployment = true;
        [SerializeField] private string pentaProjectPath = "C:/WebProjects/Penta";
        [SerializeField] private string pentaServerUrl = "https://penta.zart-zurt.com";
        [SerializeField] private string pentaGameSlug = "";
        [SerializeField] private string pentaAspectRatio = "9:16";
        [SerializeField] private bool syncRemoteVps = true;

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

        public string PentaProjectPath
        {
            get => pentaProjectPath;
            set => pentaProjectPath = value;
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

        public bool SyncRemoteVps
        {
            get => syncRemoteVps;
            set => syncRemoteVps = value;
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
    }
}
