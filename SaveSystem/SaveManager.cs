using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Core.Events;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;

#if USE_STEAMWORKS
using Steamworks;
#endif

namespace Core.Save
{
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Core/Save/Save Manager")]
    public class SaveManager : Singleton<SaveManager>
    {
        private const int CurrentSaveVersion = 1;
        private readonly List<ISaveMigration> _migrations = new();
        private readonly HashSet<ISaveable> _saveables = new();

        public static string CurrentGameVersion => Application.version;

        public int ActiveSlotIndex { get; private set; }
        public SaveSlotData ActiveSlotData { get; private set; } = new();

        public SaveSlotData CreateEmptySlotData(int slotIndex)
        {
            return new SaveSlotData
            {
                version = CurrentSaveVersion,
                gameVersion = CurrentGameVersion,
                slotName = $"Slot {slotIndex + 1}"
            };
        }

        protected override void Awake()
        {
            base.Awake();
            ActiveSlotData = CreateEmptySlotData(ActiveSlotIndex);
            LoadSlotFromDisk(ActiveSlotIndex);
        }

        private void OnEnable()
        {
            Application.quitting += OnApplicationQuitting;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            Application.quitting -= OnApplicationQuitting;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnApplicationQuitting()
        {
            SaveAll();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused && Application.isPlaying)
            {
                SaveAll();
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (Application.isPlaying)
            {
                SaveAll();
            }
        }

        public void RegisterMigration(ISaveMigration migration)
        {
            if (migration != null && !_migrations.Contains(migration))
            {
                _migrations.Add(migration);
            }
        }

        public void Register(ISaveable saveable)
        {
            if (saveable != null)
            {
                _saveables.Add(saveable);
            }
        }

        public void Unregister(ISaveable saveable)
        {
            if (saveable != null)
            {
                _saveables.Remove(saveable);
            }
        }

        public void SetActiveSlot(int slotIndex)
        {
            ActiveSlotIndex = slotIndex;
            LoadSlotFromDisk(slotIndex);
        }

        public bool SlotExists(int slotIndex)
        {
            return File.Exists(GetSlotPath(slotIndex));
        }

        public void DeleteSlot(int slotIndex)
        {
            var path = GetSlotPath(slotIndex);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

#if USE_STEAMWORKS
            if (SteamClient.IsValid)
            {
                SteamRemoteStorage.FileDelete($"slot_{slotIndex}.json");
            }
#endif

            if (slotIndex == ActiveSlotIndex)
            {
                ActiveSlotData = CreateEmptySlotData(slotIndex);
            }
        }

        public void Save<T>(string key, T data)
        {
            if (data == null)
            {
                ActiveSlotData.entries[key] = string.Empty;
                return;
            }

            if (typeof(T) == typeof(string))
            {
                ActiveSlotData.entries[key] = data as string;
            }
            else if (typeof(T).IsPrimitive || typeof(T).IsEnum)
            {
                ActiveSlotData.entries[key] = Convert.ToString(data, CultureInfo.InvariantCulture);
            }
            else
            {
                ActiveSlotData.entries[key] = JsonUtility.ToJson(data);
            }
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            if (ActiveSlotData.entries.TryGetValue(key, out var rawValue))
            {
                if (string.IsNullOrEmpty(rawValue))
                {
                    return defaultValue;
                }

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)rawValue;
                }

                if (typeof(T).IsPrimitive || typeof(T).IsEnum)
                {
                    try
                    {
                        return (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                try
                {
                    return JsonUtility.FromJson<T>(rawValue);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public bool HasKey(string key)
        {
            return ActiveSlotData.entries.ContainsKey(key);
        }

        public void DeleteKey(string key)
        {
            ActiveSlotData.entries.Remove(key);
        }

        public void SaveAll()
        {
            EventBus.Raise(new GameSaveRequestedEvent());

            foreach (var saveable in _saveables)
            {
                saveable.OnSave(this);
            }

            Flush();
        }

        public void LoadAll()
        {
            LoadSlotFromDisk(ActiveSlotIndex);

            foreach (var saveable in _saveables)
            {
                saveable.OnLoad(this);
            }
        }

        public void Flush()
        {
            ActiveSlotData.gameVersion = CurrentGameVersion;
            ActiveSlotData.lastSaveTimestamp = DateTime.UtcNow.ToString("o");

            var json = JsonUtility.ToJson(ActiveSlotData, true);
            var directory = Path.Combine(Application.persistentDataPath, "saves");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var path = GetSlotPath(ActiveSlotIndex);
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);

#if USE_STEAMWORKS
            if (SteamClient.IsValid)
            {
                SteamRemoteStorage.FileWrite($"slot_{ActiveSlotIndex}.json", Encoding.UTF8.GetBytes(json));
            }
#endif
        }

        public SaveSlotData LoadSlotFromDisk(int slotIndex)
        {
            var path = GetSlotPath(slotIndex);

            if (!File.Exists(path))
            {
                ActiveSlotData = CreateEmptySlotData(slotIndex);
                return ActiveSlotData;
            }

            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveSlotData>(json);

            if (data == null)
            {
                ActiveSlotData = CreateEmptySlotData(slotIndex);
                return ActiveSlotData;
            }

            if (data.version == CurrentSaveVersion)
            {
                ActiveSlotData = data;
                return data;
            }

            if (data.version > CurrentSaveVersion)
            {
                EventBus.Raise(new SaveVersionTooNewEvent
                {
                    slotIndex = slotIndex,
                    fileVersion = data.version,
                    currentVersion = CurrentSaveVersion
                });
                return null;
            }

            ActiveSlotData = RunMigrationPipeline(data);
            return ActiveSlotData;
        }

        private SaveSlotData RunMigrationPipeline(SaveSlotData data)
        {
            var chain = _migrations
                .Where(m => m.FromVersion >= data.version)
                .OrderBy(m => m.FromVersion);

            foreach (var migration in chain)
            {
                if (data.version != migration.FromVersion)
                {
                    break;
                }

                migration.Migrate(data);
            }

            if (data.version != CurrentSaveVersion)
            {
                EventBus.Raise(new SaveMigrationFailedEvent { slotIndex = ActiveSlotIndex });
                return CreateEmptySlotData(ActiveSlotIndex);
            }

            Flush();
            return data;
        }

        private string GetSlotPath(int slotIndex)
        {
            return Path.Combine(Application.persistentDataPath, "saves", $"slot_{slotIndex}.json");
        }
    }
}
