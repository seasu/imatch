using System.Collections.Generic;

namespace Game.Core
{
    public class KingdomNodeState
    {
        public KingdomNodeConfig Config;
        public NodeState State;
    }

    public interface IKingdomService
    {
        IReadOnlyList<KingdomNodeState> GetNodes();
        bool TryCompleteBuild(string nodeId);
        bool TryStartPuzzle(string nodeId, out string levelId);
        void OnLevelCompleted(string levelId);
        void Initialize(KingdomNodesConfig config, SaveData save, IEconomyService economy);
    }
}
