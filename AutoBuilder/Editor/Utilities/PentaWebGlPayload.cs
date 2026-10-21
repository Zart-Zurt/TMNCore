using System;

namespace AutoBuilder.Editor
{
    [Serializable]
    public class PentaWebGlPayload
    {
        public string title;
        public string version;
        public string buildUrl;
        public string aspectRatio;
        public string notes;
        public bool makeActive;
    }
}
