using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity
{
    using Game.Core;

    /// Attach to a GameObject in Kingdom.unity.
    /// Spawns node buttons in a vertical list.
    public class KingdomViewController : MonoBehaviour
    {
        [SerializeField] private KingdomNodeButton _nodePrefab;
        [SerializeField] private Transform _nodeContainer;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _livesText;
        [SerializeField] private Text _gemsText;

        private IKingdomService _kingdom;
        private IEconomyService _economy;
        private IGameRouter _router;
        private IAnalytics _analytics;
        private ISaveService _save;

        private readonly List<KingdomNodeButton> _buttons = new();

        private void Start()
        {
            _kingdom   = ServiceLocator.Get<IKingdomService>();
            _economy   = ServiceLocator.Get<IEconomyService>();
            _router    = ServiceLocator.Get<IGameRouter>();
            _analytics = ServiceLocator.Get<IAnalytics>();
            _save      = ServiceLocator.Get<ISaveService>();

            Refresh();
        }

        private void Refresh()
        {
            // Clear existing
            foreach (var b in _buttons) Destroy(b.gameObject);
            _buttons.Clear();

            var nodes = _kingdom.GetNodes();
            foreach (var node in nodes)
            {
                var btn = Instantiate(_nodePrefab, _nodeContainer);
                btn.Setup(node, OnNodeClicked);
                _buttons.Add(btn);

                _analytics.Track(AnalyticsEvents.KingdomNodeView,
                    new System.Collections.Generic.Dictionary<string, object>
                    { ["nodeId"] = node.Config.Id, ["state"] = node.State.ToString() });
            }

            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (_coinsText) _coinsText.text = $"Coins: {_economy.Coins}";
            if (_livesText) _livesText.text = $"Lives: {_economy.Lives}/{_economy.MaxLives}";
            if (_gemsText)  _gemsText.text  = $"Gems: {_economy.Gems}";
        }

        private void OnNodeClicked(KingdomNodeState node)
        {
            switch (node.State)
            {
                case NodeState.Locked:
                    Debug.Log($"[Kingdom] Node locked: {node.Config.Id}");
                    break;

                case NodeState.Available when node.Config.Type == NodeType.BuildNode:
                    if (_kingdom.TryCompleteBuild(node.Config.Id))
                    {
                        _analytics.Track(AnalyticsEvents.KingdomNodeComplete,
                            new System.Collections.Generic.Dictionary<string, object>
                            { ["nodeId"] = node.Config.Id, ["cost"] = node.Config.CostCoins });
                        _save.Save();
                        Refresh();
                    }
                    else
                    {
                        Debug.Log($"[Kingdom] Not enough coins for: {node.Config.Id}");
                    }
                    break;

                case NodeState.Available when node.Config.Type == NodeType.PuzzleGate:
                case NodeState.Completed when node.Config.Type == NodeType.PuzzleGate:
                    if (_kingdom.TryStartPuzzle(node.Config.Id, out var levelId))
                    {
                        _save.Current.Progress.CurrentLevelId = levelId;
                        _save.Save();
                        _router.GoToMatch3(levelId);
                    }
                    break;
            }
        }
    }
}
