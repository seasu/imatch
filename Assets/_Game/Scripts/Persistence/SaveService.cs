using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Persistence
{
    using Game.Core;

    public class SaveService : ISaveService
    {
        private const string FileName = "save.json";
        private const int CurrentVersion = 1;

        private readonly List<ISaveMigration> _migrations;
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public SaveData Current { get; private set; }

        public SaveService(List<ISaveMigration> migrations = null)
        {
            _migrations = migrations ?? new List<ISaveMigration>();
        }

        public void LoadOrCreate()
        {
            if (!File.Exists(FilePath))
            {
                Current = new SaveData();
                Current.SessionSeed = (ulong)DateTime.UtcNow.Ticks;
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(FilePath);
                Current = JsonUtility.FromJson<SaveData>(json);
                Current = RunMigrations(Current);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Failed to load save, creating new. Reason: {e.Message}");
                Current = new SaveData();
                Save();
            }
        }

        public void Save()
        {
            try
            {
                Current.Timestamps.LastSaveUtc = DateTime.UtcNow.ToString("o");
                var json = JsonUtility.ToJson(Current, prettyPrint: false);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Save failed: {e.Message}");
            }
        }

        public void ResetForDebug()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            LoadOrCreate();
        }

        private SaveData RunMigrations(SaveData data)
        {
            while (data.SaveVersion < CurrentVersion)
            {
                bool migrated = false;
                foreach (var m in _migrations)
                {
                    if (m.CanMigrate(data.SaveVersion))
                    {
                        data = m.Migrate(data);
                        migrated = true;
                        break;
                    }
                }
                if (!migrated) break; // no migration available; stop
            }
            return data;
        }
    }

    /// Placeholder migration v1 -> v2.
    public class SaveMigrationV1ToV2 : ISaveMigration
    {
        public bool CanMigrate(int fromVersion) => fromVersion == 1;

        public SaveData Migrate(SaveData old)
        {
            old.SaveVersion = 2;
            // Add any v1->v2 field transformations here.
            return old;
        }
    }

    /// No-op cloud save stub.
    public class NullCloudSaveService : ICloudSaveService
    {
        public System.Threading.Tasks.Task PushAsync(SaveData data) =>
            System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<SaveData> PullAsync() =>
            System.Threading.Tasks.Task.FromResult<SaveData>(null);
    }
}
