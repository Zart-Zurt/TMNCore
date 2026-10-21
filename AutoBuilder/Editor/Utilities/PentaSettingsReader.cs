using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public static class PentaSettingsReader
    {
        public static string GetServerUrl()
        {
            var serializedObject = GetPentaSettingsSerializedObject();
            if (serializedObject == null)
            {
                return string.Empty;
            }

            var prop = serializedObject.FindProperty("serverUrl");
            return prop != null ? prop.stringValue.TrimEnd('/') : string.Empty;
        }

        public static string GetGameId()
        {
            var serializedObject = GetPentaSettingsSerializedObject();
            if (serializedObject == null)
            {
                return string.Empty;
            }

            var prop = serializedObject.FindProperty("gameId");
            return prop != null ? prop.stringValue.Trim() : string.Empty;
        }

        public static string GetApiKey()
        {
            var serializedObject = GetPentaSettingsSerializedObject();
            if (serializedObject == null)
            {
                return string.Empty;
            }

            var prop = serializedObject.FindProperty("apiKey");
            return prop != null ? prop.stringValue.Trim() : string.Empty;
        }

        private static SerializedObject GetPentaSettingsSerializedObject()
        {
            var resourceAsset = Resources.Load("PentaSettings");
            if (resourceAsset != null)
            {
                return new SerializedObject(resourceAsset);
            }

            var guids = AssetDatabase.FindAssets("PentaSettings t:ScriptableObject");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var loadedAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            return loadedAsset != null ? new SerializedObject(loadedAsset) : null;
        }
    }
}
