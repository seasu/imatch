using System;
using System.Collections.Generic;

namespace Game.Core
{
    public class SaveData
    {
        public int SaveVersion = 1;
        public string PlayerId = Guid.NewGuid().ToString();
        public EconomySaveData Economy = new();
        public ProgressSaveData Progress = new();
        public KingdomSaveData Kingdom = new();
        public ulong SessionSeed;
        public TimestampData Timestamps = new();
    }

    public class EconomySaveData
    {
        public int Coins;
        public int Gems;
        public int Lives;
        public Dictionary<string, int> BoostersInventory = new();
    }

    public class ProgressSaveData
    {
        public string CurrentLevelId;
        public string MaxUnlockedLevelId;
        public Dictionary<string, int> AttemptIndexByLevel = new();
    }

    public class KingdomSaveData
    {
        public List<string> CompletedNodeIds = new();
        public List<string> BuiltNodeIds = new();
    }

    public class TimestampData
    {
        public string LastSaveUtc;
    }
}
