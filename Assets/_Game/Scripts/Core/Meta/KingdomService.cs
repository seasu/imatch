using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    public class KingdomService : IKingdomService
    {
        private KingdomNodesConfig _config;
        private SaveData _save;
        private IEconomyService _economy;
        private List<KingdomNodeState> _nodeStates = new();

        public void Initialize(KingdomNodesConfig config, SaveData save, IEconomyService economy)
        {
            _config = config;
            _save = save;
            _economy = economy;
            RefreshStates();
        }

        public IReadOnlyList<KingdomNodeState> GetNodes() => _nodeStates;

        public bool TryCompleteBuild(string nodeId)
        {
            var state = GetState(nodeId);
            if (state == null || state.State != NodeState.Available) return false;
            if (state.Config.Type != NodeType.BuildNode) return false;

            if (!_economy.TrySpendCoins(state.Config.CostCoins)) return false;

            if (!_save.Kingdom.BuiltNodeIds.Contains(nodeId))
                _save.Kingdom.BuiltNodeIds.Add(nodeId);
            if (!_save.Kingdom.CompletedNodeIds.Contains(nodeId))
                _save.Kingdom.CompletedNodeIds.Add(nodeId);

            RefreshStates();
            return true;
        }

        public bool TryStartPuzzle(string nodeId, out string levelId)
        {
            levelId = null;
            var state = GetState(nodeId);
            if (state == null || state.State == NodeState.Locked) return false;
            if (state.Config.Type != NodeType.PuzzleGate) return false;

            levelId = state.Config.LevelId;
            return true;
        }

        public void OnLevelCompleted(string levelId)
        {
            foreach (var ns in _nodeStates)
            {
                if (ns.Config.Type == NodeType.PuzzleGate &&
                    ns.Config.LevelId == levelId &&
                    ns.State != NodeState.Completed)
                {
                    if (!_save.Kingdom.CompletedNodeIds.Contains(ns.Config.Id))
                        _save.Kingdom.CompletedNodeIds.Add(ns.Config.Id);
                }
            }
            RefreshStates();
        }

        private void RefreshStates()
        {
            _nodeStates.Clear();
            foreach (var cfg in _config.Nodes)
            {
                var ns = new KingdomNodeState { Config = cfg };

                if (_save.Kingdom.CompletedNodeIds.Contains(cfg.Id))
                {
                    ns.State = NodeState.Completed;
                }
                else
                {
                    bool prereqsMet = cfg.Prereq.All(p => _save.Kingdom.CompletedNodeIds.Contains(p));
                    ns.State = prereqsMet ? NodeState.Available : NodeState.Locked;
                }

                _nodeStates.Add(ns);
            }
        }

        private KingdomNodeState GetState(string nodeId) =>
            _nodeStates.Find(n => n.Config.Id == nodeId);
    }
}
