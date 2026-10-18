using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Core.Events;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace Core.Localization
{
    [DefaultExecutionOrder(-110)]
    [AddComponentMenu("Core/Localization/Localization Manager")]
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        [SerializeField] private string defaultLocaleCode = "en";
        [SerializeField] private bool autoDetectSteamLanguage = true;

        private readonly Dictionary<string, LanguageData> _loadedLanguages = new(StringComparer.OrdinalIgnoreCase);
        private LanguageData _currentLanguageData;
        private LanguageData _fallbackLanguageData;

        public string CurrentLocaleCode { get; private set; } = "en";
        public LanguageData CurrentLanguage => _currentLanguageData;

        protected override void Awake()
        {
            base.Awake();
            ReloadLanguages();
            DetermineInitialLanguage();
        }

        public void ReloadLanguages()
        {
            _loadedLanguages.Clear();

            LoadResourcesLanguages();
            LoadStreamingAssetsLanguages();
            LoadExternalGameDirectoryLanguages();
            LoadPersistentDataLanguages();
            LoadSteamWorkshopLanguages();

            EnsureFallbackLanguage();

            if (!string.IsNullOrEmpty(CurrentLocaleCode) && _loadedLanguages.TryGetValue(CurrentLocaleCode, out var active))
            {
                _currentLanguageData = active;
            }
            else
            {
                _currentLanguageData = _fallbackLanguageData;
                CurrentLocaleCode = _fallbackLanguageData != null ? _fallbackLanguageData.LanguageCode : defaultLocaleCode;
            }
        }

        private void DetermineInitialLanguage()
        {
            var targetCode = defaultLocaleCode;

            if (autoDetectSteamLanguage)
            {
                var steamLangCode = DetectSteamLanguageCode();
                if (!string.IsNullOrEmpty(steamLangCode) && _loadedLanguages.ContainsKey(steamLangCode))
                {
                    targetCode = steamLangCode;
                }
                else
                {
                    var systemCode = MapSystemLanguageToCode(Application.systemLanguage);
                    if (!string.IsNullOrEmpty(systemCode) && _loadedLanguages.ContainsKey(systemCode))
                    {
                        targetCode = systemCode;
                    }
                }
            }

            SetLocale(targetCode);
        }

        public void SetLocale(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode))
            {
                localeCode = defaultLocaleCode;
            }

            if (_loadedLanguages.TryGetValue(localeCode, out var languageData))
            {
                _currentLanguageData = languageData;
                CurrentLocaleCode = localeCode;
            }
            else if (_fallbackLanguageData != null)
            {
                _currentLanguageData = _fallbackLanguageData;
                CurrentLocaleCode = _fallbackLanguageData.LanguageCode;
            }
            else
            {
                CurrentLocaleCode = localeCode;
            }

            var langName = _currentLanguageData != null ? _currentLanguageData.LanguageName : CurrentLocaleCode;
            var isCustom = _currentLanguageData != null && _currentLanguageData.IsCustom;

            EventBus.Raise(new LocaleChangedEvent(CurrentLocaleCode, langName, isCustom));
        }

        public string Get(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_currentLanguageData != null && _currentLanguageData.TryGetValue(key, out var localizedValue))
            {
                if (!string.IsNullOrEmpty(localizedValue))
                {
                    return localizedValue;
                }
            }

            if (_fallbackLanguageData != null && _fallbackLanguageData.TryGetValue(key, out var fallbackValue))
            {
                if (!string.IsNullOrEmpty(fallbackValue))
                {
                    return fallbackValue;
                }
            }

            return fallback ?? key;
        }

        public string GetFormat(string key, params object[] args)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch (Exception exception)
            {
                UnityDebug.LogWarning($"[LocalizationManager] Failed to format localization key '{key}': {exception.Message}");
                return format;
            }
        }

        public List<LanguageInfo> GetAvailableLanguages()
        {
            var result = new List<LanguageInfo>();
            foreach (var pair in _loadedLanguages)
            {
                var data = pair.Value;
                result.Add(new LanguageInfo(data.LanguageCode, data.LanguageName, data.Author, data.IsCustom));
            }

            result.Sort((a, b) =>
            {
                if (a.IsCustom != b.IsCustom)
                {
                    return a.IsCustom ? 1 : -1;
                }

                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            return result;
        }

        public void RegisterOrUpdateLanguage(LanguageData languageData)
        {
            if (languageData == null || string.IsNullOrEmpty(languageData.LanguageCode))
            {
                return;
            }

            languageData.InitializeLookup();
            _loadedLanguages[languageData.LanguageCode] = languageData;

            if (string.Equals(languageData.LanguageCode, defaultLocaleCode, StringComparison.OrdinalIgnoreCase))
            {
                _fallbackLanguageData = languageData;
            }

            if (string.Equals(languageData.LanguageCode, CurrentLocaleCode, StringComparison.OrdinalIgnoreCase))
            {
                _currentLanguageData = languageData;
            }
        }

        public void OpenLanguagesFolder()
        {
            var folderPath = GetCustomLanguagesDirectory();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                UnityDebug.LogError($"[LocalizationManager] Failed to open languages folder: {exception.Message}");
            }
        }

        public string GetCustomLanguagesDirectory()
        {
            var gameDirectoryLanguages = Path.Combine(Directory.GetCurrentDirectory(), "Languages");
            if (Directory.Exists(gameDirectoryLanguages))
            {
                return gameDirectoryLanguages;
            }

            var streamingAssetsLanguages = Path.Combine(Application.streamingAssetsPath, "Languages");
            if (Directory.Exists(streamingAssetsLanguages))
            {
                return streamingAssetsLanguages;
            }

            return Path.Combine(Application.persistentDataPath, "Languages");
        }

        private void LoadResourcesLanguages()
        {
            var textAssets = Resources.LoadAll<TextAsset>("Localization");
            for (var i = 0; i < textAssets.Length; i++)
            {
                var asset = textAssets[i];
                if (asset == null || string.IsNullOrEmpty(asset.text))
                {
                    continue;
                }

                try
                {
                    var data = LanguageData.FromJson(asset.text);
                    if (data != null && !string.IsNullOrEmpty(data.LanguageCode))
                    {
                        RegisterOrUpdateLanguage(data);
                    }
                }
                catch (Exception exception)
                {
                    UnityDebug.LogWarning($"[LocalizationManager] Error loading resource language '{asset.name}': {exception.Message}");
                }
            }
        }

        private void LoadStreamingAssetsLanguages()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Languages");
            LoadLanguagesFromDirectory(path, false);
        }

        private void LoadExternalGameDirectoryLanguages()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Languages");
            LoadLanguagesFromDirectory(path, true);
        }

        private void LoadPersistentDataLanguages()
        {
            var path = Path.Combine(Application.persistentDataPath, "Languages");
            LoadLanguagesFromDirectory(path, true);
        }

        private void LoadSteamWorkshopLanguages()
        {
            var gameDir = Directory.GetCurrentDirectory();
            var workshopRelativePath = Path.Combine(gameDir, "..", "..", "workshop", "content");
            if (!Directory.Exists(workshopRelativePath))
            {
                return;
            }

            try
            {
                var subDirs = Directory.GetDirectories(workshopRelativePath, "*", SearchOption.AllDirectories);
                for (var i = 0; i < subDirs.Length; i++)
                {
                    var dirName = Path.GetFileName(subDirs[i]);
                    if (string.Equals(dirName, "Languages", StringComparison.OrdinalIgnoreCase))
                    {
                        LoadLanguagesFromDirectory(subDirs[i], true);
                    }
                }
            }
            catch (Exception exception)
            {
                UnityDebug.LogWarning($"[LocalizationManager] Error reading Steam Workshop folders: {exception.Message}");
            }
        }

        private void LoadLanguagesFromDirectory(string directoryPath, bool isCustomDirectory)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            try
            {
                var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
                for (var i = 0; i < files.Length; i++)
                {
                    var filePath = files[i];
                    var fileName = Path.GetFileName(filePath);
                    if (fileName.StartsWith("template_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var json = File.ReadAllText(filePath);
                    var data = LanguageData.FromJson(json);
                    if (data != null && !string.IsNullOrEmpty(data.LanguageCode))
                    {
                        if (isCustomDirectory)
                        {
                            data.SetMetadata(data.LanguageCode, data.LanguageName, string.IsNullOrEmpty(data.Author) ? "Community Mod" : data.Author, true);
                        }

                        RegisterOrUpdateLanguage(data);
                    }
                }
            }
            catch (Exception exception)
            {
                UnityDebug.LogError($"[LocalizationManager] Error scanning directory '{directoryPath}': {exception.Message}");
            }
        }

        private void EnsureFallbackLanguage()
        {
            if (_fallbackLanguageData != null)
            {
                return;
            }

            if (_loadedLanguages.TryGetValue(defaultLocaleCode, out var foundDefault))
            {
                _fallbackLanguageData = foundDefault;
                return;
            }

            var defaultData = new LanguageData(defaultLocaleCode, "English", "Built-in", false);
            defaultData.AddOrUpdateEntry("menu.play", "Play");
            defaultData.AddOrUpdateEntry("menu.settings", "Settings");
            defaultData.AddOrUpdateEntry("menu.quit", "Quit");
            defaultData.InitializeLookup();

            RegisterOrUpdateLanguage(defaultData);
            _fallbackLanguageData = defaultData;
        }

        private string DetectSteamLanguageCode()
        {
#if USE_STEAMWORKS
            try
            {
                if (Steamworks.SteamClient.IsValid)
                {
                    var steamLang = Steamworks.SteamUtils.SteamUILanguage;
                    if (string.IsNullOrEmpty(steamLang))
                    {
                        steamLang = Steamworks.SteamApps.GameLanguage;
                    }

                    if (!string.IsNullOrEmpty(steamLang))
                    {
                        return MapSteamLanguageNameToCode(steamLang);
                    }
                }
            }
            catch (Exception exception)
            {
                UnityDebug.LogWarning($"[LocalizationManager] Steamworks language detection skipped: {exception.Message}");
            }
#endif
            return null;
        }

        private static string MapSteamLanguageNameToCode(string steamLang)
        {
            return steamLang.ToLowerInvariant() switch
            {
                "english" => "en",
                "turkish" => "tr",
                "german" => "de",
                "french" => "fr",
                "spanish" => "es",
                "latam" => "es",
                "italian" => "it",
                "russian" => "ru",
                "portuguese" => "pt",
                "brazilian" => "pt-BR",
                "polish" => "pl",
                "japanese" => "ja",
                "koreana" => "ko",
                "schinese" => "zh-CN",
                "tchinese" => "zh-TW",
                "czech" => "cs",
                "danish" => "da",
                "dutch" => "nl",
                "finnish" => "fi",
                "hungarian" => "hu",
                "norwegian" => "no",
                "swedish" => "sv",
                "ukrainian" => "uk",
                _ => steamLang
            };
        }

        private static string MapSystemLanguageToCode(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
            {
                SystemLanguage.English => "en",
                SystemLanguage.Turkish => "tr",
                SystemLanguage.German => "de",
                SystemLanguage.French => "fr",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Italian => "it",
                SystemLanguage.Russian => "ru",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Polish => "pl",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.Korean => "ko",
                SystemLanguage.Chinese => "zh-CN",
                SystemLanguage.ChineseSimplified => "zh-CN",
                SystemLanguage.ChineseTraditional => "zh-TW",
                SystemLanguage.Czech => "cs",
                SystemLanguage.Danish => "da",
                SystemLanguage.Dutch => "nl",
                SystemLanguage.Finnish => "fi",
                SystemLanguage.Hungarian => "hu",
                SystemLanguage.Norwegian => "no",
                SystemLanguage.Swedish => "sv",
                SystemLanguage.Ukrainian => "uk",
                _ => "en"
            };
        }
    }
}
