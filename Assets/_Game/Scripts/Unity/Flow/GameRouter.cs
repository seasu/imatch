using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Unity
{
    public class GameRouter : MonoBehaviour, IGameRouter
    {
        public static GameRouter Instance { get; private set; }

        [SerializeField] private string _kingdomScene  = "Kingdom";
        [SerializeField] private string _match3Scene   = "Match3";
        [SerializeField] private string _replayScene   = "QA_ReplayRunner";

        public static string PendingLevelId  { get; private set; }
        public static string PendingReplayId { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<IGameRouter>(this);
        }

        public void GoToKingdom()                    => SceneManager.LoadScene(_kingdomScene);
        public void GoToReplayRunner(string replayId){ PendingReplayId = replayId; SceneManager.LoadScene(_replayScene); }
        public void ReloadCurrent()                  => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        public void GoToMatch3(string levelId)
        {
            PendingLevelId = levelId;
            SceneManager.LoadScene(_match3Scene);
        }
    }
}
