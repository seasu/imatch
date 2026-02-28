using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Game.Core
{
    public interface IMatch3Game
    {
        GameState State { get; }
        void Initialize(LevelConfig level, uint seed);
        SwapResult TrySwap(Vector2Int a, Vector2Int b);
        bool UseBooster(BoosterType booster, Vector2Int? cell = null);
        IReadOnlyList<LogicEvent> ConsumeEvents();
        EndResult GetEndResult(); // null if game not over
        int RemainingMoves { get; }
        int BossHp { get; }       // -1 for non-boss levels
    }
}
