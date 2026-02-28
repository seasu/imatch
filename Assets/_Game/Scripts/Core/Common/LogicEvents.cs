using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Game.Core
{
    public abstract class LogicEvent { }

    public sealed class SwapEvent : LogicEvent
    {
        public Vector2Int A;
        public Vector2Int B;
    }

    public sealed class MatchClearEvent : LogicEvent
    {
        public List<Vector2Int> Cells = new();
        public string Source; // "Match" | "Special"
    }

    public sealed class SpecialCreatedEvent : LogicEvent
    {
        public Vector2Int Cell;
        public TileKind Kind;
    }

    public sealed class TileFellEvent : LogicEvent
    {
        public Vector2Int From;
        public Vector2Int To;
    }

    public sealed class TileSpawnedEvent : LogicEvent
    {
        public Vector2Int At;
        public TileColor Color;
    }

    public sealed class ObstacleDamagedEvent : LogicEvent
    {
        public Vector2Int Cell;
        public ObstacleKind Obstacle;
        public int RemainingHp;
    }

    public sealed class BossDamagedEvent : LogicEvent
    {
        public int Amount;
        public int RemainingHp;
    }
}
