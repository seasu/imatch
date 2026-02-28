using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity
{
    using Game.Core;

    public class KingdomNodeButton : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _stateText;
        [SerializeField] private Button _button;

        private KingdomNodeState _node;
        private Action<KingdomNodeState> _onClick;

        public void Setup(KingdomNodeState node, Action<KingdomNodeState> onClick)
        {
            _node = node;
            _onClick = onClick;

            _titleText.text = node.Config.Title;

            string stateLabel = node.State switch
            {
                NodeState.Locked    => "Locked",
                NodeState.Completed => "Completed",
                NodeState.Available when node.Config.Type == NodeType.BuildNode
                    => $"Build ({node.Config.CostCoins} coins)",
                NodeState.Available when node.Config.Type == NodeType.PuzzleGate
                    => $"Play Level {node.Config.LevelId}",
                _ => node.State.ToString()
            };
            _stateText.text = stateLabel;

            _button.interactable = node.State != NodeState.Locked;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onClick?.Invoke(_node));
        }
    }
}
