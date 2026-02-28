using System.Collections.Generic;

namespace Game.Core
{
    // ── Level ──────────────────────────────────────────────────────────────
    public class LevelConfig
    {
        public int Version;
        public string Id;
        public LevelType Type;
        public BoardConfig Board;
        public int Moves;
        public List<GoalConfig> Goals = new();
        public BossConfig Boss;           // null for Normal levels
        public LayoutConfig Layout;
        public RewardsConfig Rewards;
    }

    public class BoardConfig
    {
        public int Width;
        public int Height;
        public List<string> Colors = new();
        public Dictionary<string, int> SpawnWeights = new();
    }

    public class GoalConfig
    {
        public GoalKind Kind;
        public string Color;      // for CollectColor
        public string Obstacle;   // for DestroyObstacles
        public int Count;
    }

    public class BossConfig
    {
        public int Hp;
    }

    public class LayoutConfig
    {
        // Each entry: "x,y,kind[,param]" — empty means fully random.
        public List<string> Cells = new();
    }

    public class RewardsConfig
    {
        public int Coins;
    }

    // ── Economy ────────────────────────────────────────────────────────────
    public class EconomyConfig
    {
        public int Version;
        public int StartingCoins;
        public int StartingGems;
        public int MaxLives;
        public Dictionary<string, BoosterShopEntry> Boosters = new();
    }

    public class BoosterShopEntry
    {
        public int StartingCount;
        public int CoinPrice;
    }

    // ── Remote tuning ──────────────────────────────────────────────────────
    public class RemoteTuningConfig
    {
        public int Version;
        public Dictionary<string, int> SpawnWeightsDefault = new();
        public BossTuning Boss = new();
    }

    public class BossTuning
    {
        public int DamagePerTile;
        public int RocketDamageBonus;
        public int BombDamageBonus;
    }

    // ── Kingdom nodes ──────────────────────────────────────────────────────
    public class KingdomNodesConfig
    {
        public int Version;
        public List<KingdomNodeConfig> Nodes = new();
    }

    public class KingdomNodeConfig
    {
        public string Id;
        public NodeType Type;
        public string Title;
        public int CostCoins;      // BuildNode
        public string LevelId;     // PuzzleGate
        public List<string> Prereq = new();
    }
}
