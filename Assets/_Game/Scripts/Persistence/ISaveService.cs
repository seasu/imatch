namespace Game.Core
{
    public interface ISaveService
    {
        SaveData Current { get; }
        void LoadOrCreate();
        void Save();
        void ResetForDebug();
    }

    public interface ISaveMigration
    {
        bool CanMigrate(int fromVersion);
        SaveData Migrate(SaveData old);
    }

    public interface ICloudSaveService
    {
        System.Threading.Tasks.Task PushAsync(SaveData data);
        System.Threading.Tasks.Task<SaveData> PullAsync();
    }
}
