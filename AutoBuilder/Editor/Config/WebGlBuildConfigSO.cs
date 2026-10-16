using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    [CreateAssetMenu(fileName = "WebGlConfigSO", menuName = "Auto Builder/WebGlConfigSO")]
    public class WebGlBuildConfigSO : ScriptableObject
    {
        [SerializeField] private WebGLCompressionFormat compressionFormat = WebGLCompressionFormat.Gzip;
        [SerializeField] private string templateName = "APPLICATION:Default";
        [SerializeField] private bool autoStartLocalServer = true;
        [SerializeField] private int localServerPort = 8080;
        [SerializeField] private bool enablePentaDeployment = true;
        [SerializeField] private string pentaProjectPath = "C:/WebProjects/Penta";
        [SerializeField] private string pentaServerUrl = "http://localhost:3000";
        [SerializeField] private string pentaGameSlug = "totally-normal-meat-co";
        [SerializeField] private string pentaApiKey = "";
        [SerializeField] private string pentaAspectRatio = "16:9";

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
            get => pentaApiKey;
            set => pentaApiKey = value;
        }

        public string PentaAspectRatio
        {
            get => pentaAspectRatio;
            set => pentaAspectRatio = value;
        }
    }
}
