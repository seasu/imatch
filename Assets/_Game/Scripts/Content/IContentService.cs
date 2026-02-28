using System.Threading.Tasks;

namespace Game.Core
{
    public interface IContentService
    {
        Task PreloadCoreConfigsAsync();
        Task<LevelConfig> LoadLevelAsync(string levelId);
        Task<T> LoadJsonAsync<T>(string address);
        EconomyConfig GetEconomyConfig();
        RemoteTuningConfig GetRemoteTuningConfig();
        KingdomNodesConfig GetKingdomNodesConfig();
    }
}
