using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Content
{
    using Game.Core;

    public class ContentService : IContentService
    {
        private EconomyConfig _economy;
        private RemoteTuningConfig _tuning;
        private KingdomNodesConfig _kingdomNodes;
        private readonly Dictionary<string, object> _cache = new();

        public async Task PreloadCoreConfigsAsync()
        {
            _economy      = await LoadJsonAsync<EconomyConfig>("config:economy");
            _tuning       = await LoadJsonAsync<RemoteTuningConfig>("config:remote_tuning");
            _kingdomNodes = await LoadJsonAsync<KingdomNodesConfig>("config:kingdom_nodes");
        }

        public Task<LevelConfig> LoadLevelAsync(string levelId) =>
            LoadJsonAsync<LevelConfig>($"level:{levelId}");

        public EconomyConfig      GetEconomyConfig()      => _economy;
        public RemoteTuningConfig GetRemoteTuningConfig() => _tuning;
        public KingdomNodesConfig GetKingdomNodesConfig() => _kingdomNodes;

        public async Task<T> LoadJsonAsync<T>(string address)
        {
            if (_cache.TryGetValue(address, out var cached))
                return (T)cached;

            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"[ContentService] Failed to load: {address}");

            var json = handle.Result.text;
            Addressables.Release(handle);

            // Use Unity's JsonUtility for basic deserialization
            var result = JsonUtility.FromJson<T>(json);
            _cache[address] = result;
            return result;
        }
    }
}
