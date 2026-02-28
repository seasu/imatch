using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity
{
    using Game.Core;

    public class HUDView : MonoBehaviour
    {
        [SerializeField] private Text _movesText;
        [SerializeField] private Text _goalText;
        [SerializeField] private Slider _bossHpBar;
        [SerializeField] private GameObject _bossHpContainer;
        [SerializeField] private Text _bossHpLabel;
        [SerializeField] private Button _hammerButton;
        [SerializeField] private Text _hammerCountText;

        private Match3Controller _match3;
        private IEconomyService _economy;
        private int _bossMaxHp;

        public void Initialize(LevelConfig level)
        {
            _match3  = FindObjectOfType<Match3Controller>();
            _economy = ServiceLocator.Get<IEconomyService>();

            bool isBoss = level.Type == LevelType.BossAttack;
            if (_bossHpContainer) _bossHpContainer.SetActive(isBoss);

            if (isBoss && level.Boss != null)
            {
                _bossMaxHp = level.Boss.Hp;
                if (_bossHpBar) _bossHpBar.maxValue = _bossMaxHp;
            }

            SetupGoalText(level);
            UpdateHammerButton();

            if (_hammerButton)
                _hammerButton.onClick.AddListener(OnHammerButtonClicked);
        }

        private void SetupGoalText(LevelConfig level)
        {
            if (_goalText == null || level.Goals == null) return;
            var sb = new System.Text.StringBuilder();
            foreach (var g in level.Goals)
                sb.AppendLine(g.Kind switch
                {
                    GoalKind.CollectColor     => $"Collect {g.Color}: {g.Count}",
                    GoalKind.DestroyObstacles => $"Destroy {g.Obstacle}: {g.Count}",
                    GoalKind.ClearTiles       => $"Clear tiles: {g.Count}",
                    GoalKind.Score            => $"Score: {g.Count}",
                    _                         => ""
                });
            _goalText.text = sb.ToString().TrimEnd();
        }

        public void UpdateMoves(int remaining)
        {
            if (_movesText) _movesText.text = $"Moves: {remaining}";
        }

        public void UpdateBossHp(int hp)
        {
            if (_bossHpBar)   _bossHpBar.value = hp;
            if (_bossHpLabel) _bossHpLabel.text = $"{hp}/{_bossMaxHp}";
        }

        private void UpdateHammerButton()
        {
            int count = _economy?.GetBoosterCount(BoosterType.Hammer) ?? 0;
            if (_hammerCountText) _hammerCountText.text = count.ToString();
            if (_hammerButton)    _hammerButton.interactable = count > 0;
        }

        private void OnHammerButtonClicked()
        {
            // Hammer targeting: select center cell as placeholder.
            // In real implementation, let player tap a cell.
            _match3?.OnHammerBooster(new Vector2Int(4, 4));
            UpdateHammerButton();
        }
    }
}
