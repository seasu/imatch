using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Game.Core
{
    public class GoalTracker
    {
        private readonly List<GoalConfig> _goals;
        private readonly Dictionary<string, int> _colorCollected = new();
        private readonly Dictionary<string, int> _obstaclesDestroyed = new();
        private int _tilesCleared;
        private int _score;

        public GoalTracker(List<GoalConfig> goals)
        {
            _goals = goals;
        }

        public void OnTileCollected(TileColor color)
        {
            var key = color.ToString();
            _colorCollected.TryGetValue(key, out var cur);
            _colorCollected[key] = cur + 1;
            _tilesCleared++;
        }

        public void OnObstacleDestroyed(ObstacleKind kind)
        {
            var key = kind.ToString();
            _obstaclesDestroyed.TryGetValue(key, out var cur);
            _obstaclesDestroyed[key] = cur + 1;
        }

        public void AddScore(int amount) => _score += amount;

        public bool AllGoalsMet()
        {
            foreach (var goal in _goals)
            {
                switch (goal.Kind)
                {
                    case GoalKind.CollectColor:
                        _colorCollected.TryGetValue(goal.Color, out var collected);
                        if (collected < goal.Count) return false;
                        break;
                    case GoalKind.DestroyObstacles:
                        _obstaclesDestroyed.TryGetValue(goal.Obstacle, out var destroyed);
                        if (destroyed < goal.Count) return false;
                        break;
                    case GoalKind.ClearTiles:
                        if (_tilesCleared < goal.Count) return false;
                        break;
                    case GoalKind.Score:
                        if (_score < goal.Count) return false;
                        break;
                }
            }
            return true;
        }

        public int GetColorProgress(string color)
        {
            _colorCollected.TryGetValue(color, out var v); return v;
        }
        public int GetObstacleProgress(string kind)
        {
            _obstaclesDestroyed.TryGetValue(kind, out var v); return v;
        }
    }
}
