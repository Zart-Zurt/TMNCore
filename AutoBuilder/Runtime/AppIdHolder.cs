using System.IO;
using UnityEngine;

namespace AutoBuilder
{
    public static class AppIdHolder
    {
        private const uint DefaultAppId = 4593660;

        public static uint GetAppId()
        {
            try
            {
                var rootDir = Path.GetDirectoryName(Application.dataPath);
                if (!string.IsNullOrEmpty(rootDir))
                {
                    var appIdPath = Path.Combine(rootDir, "steam_appid.txt");
                    if (File.Exists(appIdPath))
                    {
                        var content = File.ReadAllText(appIdPath).Trim();
                        if (uint.TryParse(content, out var parsedId) && parsedId > 0)
                        {
                            return parsedId;
                        }
                    }
                }
            }
            catch
            {
            }

            return DefaultAppId;
        }
    }
}

