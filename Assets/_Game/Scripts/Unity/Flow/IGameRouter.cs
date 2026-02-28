namespace Game.Unity
{
    public interface IGameRouter
    {
        void GoToKingdom();
        void GoToMatch3(string levelId);
        void ReloadCurrent();
        void GoToReplayRunner(string replayId);
    }
}
