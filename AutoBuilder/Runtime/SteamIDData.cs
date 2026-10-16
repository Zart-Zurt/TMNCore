using UnityEngine;

namespace AutoBuilder
{
    [CreateAssetMenu(fileName = "SteamIDSO", menuName = "Auto Builder/SteamIDSO")]
    public class SteamIDData : ScriptableObject
    {
        [Tooltip("App ID for the Steam Demo.")]
        public uint SteamDemoAppId = 4734900;

        [Tooltip("App ID for the Steam Playtest.")]
        public uint SteamPlaytestAppId = 4734920;

        [Tooltip("App ID for the Steam Main game.")]
        public uint SteamAppId = 4593660;

        [Tooltip("App ID for Local testing.")]
        public uint SteamLocalAppId = 480;
    }
}
