using System.IO;
using Core.Events;
using Core.Localization;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Settings
{
    [DefaultExecutionOrder(-95)]
    [AddComponentMenu("Core/Settings/Settings Manager")]
    public class SettingsManager : Singleton<SettingsManager>
    {
        [SerializeField] private AudioMixer audioMixer;

        public SettingsData CurrentSettings { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            LoadSettings();
            ApplySettings(CurrentSettings);
        }

        public void SaveSettings(SettingsData data)
        {
            CurrentSettings = data;
            ApplySettings(data);

            var json = JsonUtility.ToJson(data, true);
            var path = Path.Combine(Application.persistentDataPath, "game_settings.json");
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);

            EventBus.Raise(new SettingsChangedEvent { settings = data });
        }

        public void LoadSettings()
        {
            var path = Path.Combine(Application.persistentDataPath, "game_settings.json");

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loaded = JsonUtility.FromJson<SettingsData>(json);
                if (string.IsNullOrEmpty(loaded.languageCode))
                {
                    loaded.languageCode = LocalizationManager.Instance != null
                        ? LocalizationManager.Instance.CurrentLocaleCode
                        : "en";
                }

                CurrentSettings = loaded;
            }
            else
            {
                var defaultSettings = SettingsData.Default;
                if (LocalizationManager.Instance != null)
                {
                    defaultSettings.languageCode = LocalizationManager.Instance.CurrentLocaleCode;
                }

                CurrentSettings = defaultSettings;
                SaveSettings(CurrentSettings);
            }
        }

        public void ApplySettings(SettingsData data)
        {
            float ConvertToDb(float val)
            {
                return Mathf.Log10(Mathf.Clamp(val, 0.0001f, 1f)) * 20f;
            }

            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", ConvertToDb(data.masterVolume));
                audioMixer.SetFloat("MusicVolume", ConvertToDb(data.musicVolume));
                audioMixer.SetFloat("SFXVolume", ConvertToDb(data.sfxVolume));
            }

            Screen.SetResolution(
                data.resolutionWidth,
                data.resolutionHeight,
                data.fullScreenMode,
                new RefreshRate { numerator = (uint)Mathf.Max(1, data.refreshRate), denominator = 1 }
            );

            QualitySettings.vSyncCount = data.vSync ? 1 : 0;
            Application.targetFrameRate = data.targetFrameRate;

            if (!string.IsNullOrEmpty(data.languageCode) && LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLocale(data.languageCode);
            }
        }
    }
}
