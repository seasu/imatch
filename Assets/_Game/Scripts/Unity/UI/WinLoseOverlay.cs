using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity
{
    using Game.Core;

    public class WinLoseOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _continueButton;

        private void Awake()
        {
            if (_panel) _panel.SetActive(false);
        }

        public void Show(EndResult result, Action onRetry, Action onContinue)
        {
            if (_panel) _panel.SetActive(true);

            if (_titleText)
                _titleText.text = result.Result == LevelResult.Win ? "Victory!" : "Defeat";

            if (_coinsText)
                _coinsText.text = result.Result == LevelResult.Win
                    ? $"+{result.CoinsGained} coins"
                    : "No coins earned";

            if (_retryButton)
            {
                _retryButton.gameObject.SetActive(result.Result == LevelResult.Lose);
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() => onRetry?.Invoke());
            }

            if (_continueButton)
            {
                _continueButton.onClick.RemoveAllListeners();
                _continueButton.onClick.AddListener(() => onContinue?.Invoke());
            }
        }
    }
}
