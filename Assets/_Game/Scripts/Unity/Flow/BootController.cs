using System;
using System.Collections;
using UnityEngine;

namespace Game.Unity
{
    using Game.Core;
    using Game.Content;
    using Game.Persistence;
    using Game.Analytics;
    using Game.QA;

    /// Attach to a GameObject in Boot.unity.
    /// Orchestrates startup: Addressables -> configs -> save -> route.
    public class BootController : MonoBehaviour
    {
        [SerializeField] private GameObject _routerPrefab;
        [SerializeField] private UnityEngine.UI.Text _statusText;

        private IGameRouter _router;

        private IEnumerator Start()
        {
            SetStatus("Booting…");

            // Router
            if (GameRouter.Instance == null && _routerPrefab != null)
                Instantiate(_routerPrefab);
            _router = ServiceLocator.Get<IGameRouter>();

            // Services
            var analyticsLogger = new AnalyticsLogger();
            ServiceLocator.Register<IAnalytics>(analyticsLogger);

            var diagnostics = new DiagnosticsService();
            ServiceLocator.Register<IDiagnosticsService>(diagnostics);

            // Content / Addressables
            SetStatus("Loading configs…");
            var contentService = new ContentService();
            ServiceLocator.Register<IContentService>(contentService);

            yield return StartCoroutine(LoadConfigsCoroutine(contentService));

            // Validate configs (fail-fast in dev builds)
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            try
            {
                ConfigValidators.ValidateEconomy(contentService.GetEconomyConfig());
                ConfigValidators.ValidateRemoteTuning(contentService.GetRemoteTuningConfig());
                ConfigValidators.ValidateKingdomNodes(contentService.GetKingdomNodesConfig());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Boot] Config validation failed: {e.Message}");
                SetStatus($"Config error:\n{e.Message}");
                yield break;
            }
#endif

            // Save
            SetStatus("Loading save…");
            var saveService = new SaveService();
            saveService.LoadOrCreate();
            ServiceLocator.Register<ISaveService>(saveService);

            // Economy
            var economyCfg = contentService.GetEconomyConfig();
            var economyService = new EconomyService();
            economyService.Initialize(economyCfg, saveService.Current.Economy);
            ServiceLocator.Register<IEconomyService>(economyService);

            // Kingdom
            var kingdomService = new KingdomService();
            kingdomService.Initialize(
                contentService.GetKingdomNodesConfig(),
                saveService.Current,
                economyService);
            ServiceLocator.Register<IKingdomService>(kingdomService);

            // Replay
            var replayService = new ReplayService();
            ServiceLocator.Register<IReplayService>(replayService);

            // Analytics: app_start + crash detection
            analyticsLogger.Track(AnalyticsEvents.AppStart);
            analyticsLogger.Flush();

            // Route
            SetStatus("Starting…");
            yield return null;

            var progress = saveService.Current.Progress;
            if (!string.IsNullOrEmpty(progress.CurrentLevelId))
                _router.GoToMatch3(progress.CurrentLevelId);
            else
                _router.GoToKingdom();
        }

        private IEnumerator LoadConfigsCoroutine(ContentService contentService)
        {
            var task = contentService.PreloadCoreConfigsAsync();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                Debug.LogError($"[Boot] Config load failed: {task.Exception?.InnerException?.Message}");
                SetStatus("Config load failed!");
            }
        }

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
            Debug.Log($"[Boot] {msg}");
        }
    }
}
