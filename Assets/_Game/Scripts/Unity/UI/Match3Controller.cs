using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity
{
    using Game.Core;
    using Game.Analytics;

    /// Top-level controller in Match3.unity.
    /// Wires input -> IMatch3Game -> events -> BoardView.
    public class Match3Controller : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private HUDView _hud;
        [SerializeField] private WinLoseOverlay _overlay;

        private IMatch3Game _game;
        private IGameRouter _router;
        private ISaveService _save;
        private IEconomyService _economy;
        private IKingdomService _kingdom;
        private IAnalytics _analytics;
        private IReplayService _replay;
        private LevelConfig _levelConfig;

        private float _levelStartTime;
        private bool _gameOver;
        private int _boostersUsed;

        private IEnumerator Start()
        {
            _router   = ServiceLocator.Get<IGameRouter>();
            _save     = ServiceLocator.Get<ISaveService>();
            _economy  = ServiceLocator.Get<IEconomyService>();
            _kingdom  = ServiceLocator.Get<IKingdomService>();
            _analytics = ServiceLocator.Get<IAnalytics>();

            if (ServiceLocator.TryGet<IReplayService>(out var rs)) _replay = rs;

            var content = ServiceLocator.Get<IContentService>();
            var levelId = GameRouter.PendingLevelId;
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogError("[Match3] No pending level id");
                _router.GoToKingdom();
                yield break;
            }

            var task = content.LoadLevelAsync(levelId);
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                Debug.LogError($"[Match3] Level load failed: {task.Exception?.InnerException?.Message}");
                _router.GoToKingdom();
                yield break;
            }

            _levelConfig = task.Result;

            // Compute seed
            var progress = _save.Current.Progress;
            progress.AttemptIndexByLevel.TryGetValue(levelId, out var attempt);
            uint seed = (uint)(_save.Current.SessionSeed ^ (ulong)levelId.GetHashCode() ^ (ulong)attempt);

            // Start replay recording
            _replay?.StartRecording(levelId, seed, attempt);

            // Init game
            _game = new Match3Game();
            var remTuning = ServiceLocator.Get<IContentService>().GetRemoteTuningConfig();
            if (_game is Match3Game mg)
            {
                mg.DamagePerTile     = remTuning.Boss.DamagePerTile;
                mg.RocketDamageBonus = remTuning.Boss.RocketDamageBonus;
                mg.BombDamageBonus   = remTuning.Boss.BombDamageBonus;
            }
            _game.Initialize(_levelConfig, seed);

            // Setup view
            _boardView.Initialize(_levelConfig.Board.Width, _levelConfig.Board.Height);
            _hud.Initialize(_levelConfig);
            _gameOver = false;
            _levelStartTime = Time.time;

            // Analytics
            _analytics.Track(AnalyticsEvents.LevelStart, new Dictionary<string, object>
            {
                ["levelId"] = levelId,
                ["attempt"] = attempt,
                ["type"]    = _levelConfig.Type.ToString()
            });
        }

        private void Update()
        {
            if (_game == null || _gameOver) return;

            // Consume and dispatch events to view
            var events = _game.ConsumeEvents();
            if (events.Count > 0)
                _boardView.ProcessEvents(events);

            _hud.UpdateMoves(_game.RemainingMoves);
            if (_levelConfig?.Type == LevelType.BossAttack)
                _hud.UpdateBossHp(_game.BossHp);

            // Check end
            var end = _game.GetEndResult();
            if (end != null && !_gameOver)
            {
                _gameOver = true;
                OnGameEnd(end);
            }
        }

        private void OnGameEnd(EndResult result)
        {
            var levelId = _levelConfig.Id;
            float duration = Time.time - _levelStartTime;

            // Stop recording
            var replayRecord = _replay?.StopRecording();
            replayRecord?.Actions.Clear(); // trim; actual recording handled by input

            // Analytics
            _analytics.Track(AnalyticsEvents.LevelEnd, new Dictionary<string, object>
            {
                ["levelId"]         = levelId,
                ["attempt"]         = _save.Current.Progress.AttemptIndexByLevel.GetValueOrDefault(levelId),
                ["result"]          = result.Result.ToString(),
                ["movesUsed"]       = result.MovesUsed,
                ["durationSec"]     = duration,
                ["boostersUsed"]    = result.BoostersUsed,
                ["bossHpRemaining"] = result.BossHpRemaining ?? -1
            });
            _analytics.Flush();

            if (result.Result == LevelResult.Win)
            {
                // Reward
                _economy.AddCoins(result.CoinsGained);

                // Kingdom progress
                _kingdom.OnLevelCompleted(levelId);
                _save.Current.Progress.CurrentLevelId = null;
            }
            else
            {
                // Lose: decrement life
                _economy.TrySpendLife();
                var progress = _save.Current.Progress;
                progress.AttemptIndexByLevel.TryGetValue(levelId, out var att);
                progress.AttemptIndexByLevel[levelId] = att + 1;
            }

            _save.Save();

            StartCoroutine(ShowOverlay(result));
        }

        private IEnumerator ShowOverlay(EndResult result)
        {
            yield return new WaitForSeconds(0.5f);
            _overlay.Show(result,
                onRetry:    () => _router.ReloadCurrent(),
                onContinue: () => _router.GoToKingdom());
        }

        /// Called by BoardView/InputHandler when a swap gesture is detected.
        public void OnSwapInput(Vector2Int a, Vector2Int b)
        {
            if (_game == null || _gameOver) return;
            var result = _game.TrySwap(a, b);
            if (result == SwapResult.Accepted)
            {
                _replay?.RecordAction(new ReplayAction
                {
                    Type = "swap",
                    A = new[] { a.x, a.y },
                    B = new[] { b.x, b.y }
                });
            }
        }

        /// Called by HUDView hammer button.
        public void OnHammerBooster(Vector2Int cell)
        {
            if (_game == null || _gameOver) return;
            if (!_economy.TrySpendBooster(BoosterType.Hammer)) return;

            _game.UseBooster(BoosterType.Hammer, cell);
            _replay?.RecordAction(new ReplayAction
            {
                Type = "hammer",
                Cell = new[] { cell.x, cell.y }
            });
            _analytics.Track(AnalyticsEvents.BoosterUse, new Dictionary<string, object>
            {
                ["boosterType"] = "Hammer", ["context"] = "in_level"
            });
            _boostersUsed++;
        }
    }
}
