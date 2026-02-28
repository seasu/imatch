using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Core
{
    public class ReplayAction
    {
        public int T;        // action index
        public string Type;  // "swap" | "hammer"
        public int[] A;      // swap from
        public int[] B;      // swap to
        public int[] Cell;   // hammer cell
    }

    public class ReplayExpected
    {
        public string Result;
        public int MovesUsed;
        public int? BossHpRemaining;
        public int? CoinsGained;
        public string Note;
    }

    public class ReplayRecord
    {
        public int Version = 1;
        public string LevelId;
        public uint Seed;
        public int AttemptIndex;
        public List<ReplayAction> Actions = new();
        public ReplayExpected Expected;
    }

    public class ReplayRunResult
    {
        public bool Passed;
        public string FailReason;
        public EndResult ActualResult;
    }

    public interface IReplayService
    {
        ReplayRecord StartRecording(string levelId, uint seed, int attemptIndex);
        void RecordAction(ReplayAction action);
        ReplayRecord StopRecording();
        Task<ReplayRecord> LoadReplayAsync(string replayId);
        ReplayRunResult RunReplay(IMatch3Game game, ReplayRecord replay, LevelConfig level);
    }

    public class DeviceDiagnostics
    {
        public float FpsAvg;
        public float MaxFrameTimeMs;
        public long MemoryUsedBytes;
        public string DeviceModel;
        public string Platform;
        public string Resolution;
    }

    public interface IDiagnosticsService
    {
        DeviceDiagnostics GetSnapshot();
        void RecordFrame(float deltaTime);
        void Reset();
    }
}
