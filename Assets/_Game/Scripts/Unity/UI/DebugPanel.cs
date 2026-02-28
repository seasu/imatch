using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity
{
    using Game.Core;
    using Game.QA;

    /// In-game debug panel. Toggle via 5 taps on version label.
    /// Attach to a Canvas in any scene.
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _versionLabel;
        [SerializeField] private InputField _levelIdInput;
        [SerializeField] private InputField _nodeIdInput;
        [SerializeField] private InputField _replayIdInput;
        [SerializeField] private Text _diagnosticsOutput;

        private int _tapCount;
        private float _tapWindowEnd;
        private const int TapsRequired = 5;

        private void Start()
        {
            if (_panel) _panel.SetActive(false);
            if (_versionLabel)
                _versionLabel.text = $"v{Application.version} | {SystemInfo.deviceModel}";
        }

        // Called by EventTrigger on version label
        public void OnVersionLabelTap()
        {
            float now = Time.unscaledTime;
            if (now > _tapWindowEnd) { _tapCount = 0; _tapWindowEnd = now + 2f; }
            _tapCount++;
            if (_tapCount >= TapsRequired)
            {
                _tapCount = 0;
                TogglePanel();
            }
        }

        private void TogglePanel()
        {
            if (_panel) _panel.SetActive(!_panel.activeSelf);
        }

        // ── Buttons ────────────────────────────────────────────────────────
        public void OnAddCoins()
        {
            if (ServiceLocator.TryGet<IEconomyService>(out var eco))
            {
                eco.AddCoins(500);
                if (ServiceLocator.TryGet<ISaveService>(out var save)) save.Save();
                Debug.Log("[Debug] +500 coins");
            }
        }

        public void OnAddLives()
        {
            if (ServiceLocator.TryGet<IEconomyService>(out var eco))
            {
                eco.AddLife(5);
                if (ServiceLocator.TryGet<ISaveService>(out var save)) save.Save();
                Debug.Log("[Debug] +5 lives");
            }
        }

        public void OnJumpToLevel()
        {
            var id = _levelIdInput?.text;
            if (string.IsNullOrWhiteSpace(id)) return;
            if (ServiceLocator.TryGet<IGameRouter>(out var router))
                router.GoToMatch3(id);
        }

        public void OnMarkNodeComplete()
        {
            var id = _nodeIdInput?.text;
            if (string.IsNullOrWhiteSpace(id)) return;
            if (ServiceLocator.TryGet<ISaveService>(out var save))
            {
                if (!save.Current.Kingdom.CompletedNodeIds.Contains(id))
                    save.Current.Kingdom.CompletedNodeIds.Add(id);
                save.Save();
                Debug.Log($"[Debug] Node marked complete: {id}");
            }
        }

        public void OnExportAnalytics()
        {
            if (ServiceLocator.TryGet<IAnalytics>(out var analytics))
            {
                analytics.Flush();
                if (analytics is Game.Analytics.AnalyticsLogger logger)
                    Debug.Log($"[Debug] Analytics exported to: {logger.GetLogPath()}");
            }
        }

        public void OnResetSave()
        {
            if (ServiceLocator.TryGet<ISaveService>(out var save))
            {
                save.ResetForDebug();
                Debug.Log("[Debug] Save reset");
            }
        }

        public void OnLoadRunReplay()
        {
            var id = _replayIdInput?.text;
            if (string.IsNullOrWhiteSpace(id)) return;
            if (ServiceLocator.TryGet<IGameRouter>(out var router))
                router.GoToReplayRunner(id);
        }

        public void OnDiagnosticsSnapshot()
        {
            if (ServiceLocator.TryGet<IDiagnosticsService>(out var diag))
            {
                var snap = diag.GetSnapshot();
                var msg = $"FPS={snap.FpsAvg:F1} MaxFrame={snap.MaxFrameTimeMs:F1}ms " +
                          $"Mem={snap.MemoryUsedBytes / 1024 / 1024}MB " +
                          $"Device={snap.DeviceModel} Platform={snap.Platform} Res={snap.Resolution}";
                if (_diagnosticsOutput) _diagnosticsOutput.text = msg;
                Debug.Log($"[Debug] {msg}");
            }
        }
    }
}
