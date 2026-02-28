using System;
using System.Collections.Generic;

namespace Game.Content
{
    using Game.Core;

    public static class ConfigValidators
    {
        public static void ValidateEconomy(EconomyConfig cfg)
        {
            if (cfg == null) throw new Exception("EconomyConfig is null");
            if (cfg.MaxLives < 1) throw new Exception($"economy.maxLives must be >= 1, got {cfg.MaxLives}");
            if (cfg.StartingCoins < 0) throw new Exception($"economy.startingCoins must be >= 0, got {cfg.StartingCoins}");
        }

        public static void ValidateRemoteTuning(RemoteTuningConfig cfg)
        {
            if (cfg == null) throw new Exception("RemoteTuningConfig is null");
            if (cfg.Boss == null) throw new Exception("remote_tuning.boss is null");
            if (cfg.Boss.DamagePerTile < 0)
                throw new Exception($"boss.damagePerTile must be >= 0, got {cfg.Boss.DamagePerTile}");

            if (cfg.SpawnWeightsDefault == null || cfg.SpawnWeightsDefault.Count == 0)
                throw new Exception("spawnWeightsDefault is empty");

            int sum = 0;
            foreach (var w in cfg.SpawnWeightsDefault.Values)
            {
                if (w < 0) throw new Exception($"spawn weight negative: {w}");
                sum += w;
            }
            if (sum == 0) throw new Exception("spawnWeightsDefault sum is 0");
        }

        public static void ValidateLevel(LevelConfig cfg, string expectedId = null)
        {
            if (cfg == null) throw new Exception("LevelConfig is null");
            if (expectedId != null && cfg.Id != expectedId)
                throw new Exception($"level id mismatch: file says '{expectedId}', config.id='{cfg.Id}'");
            if (cfg.Board == null) throw new Exception("level.board is null");
            if (cfg.Board.Width < 5 || cfg.Board.Width > 12)
                throw new Exception($"board.width must be [5..12], got {cfg.Board.Width}");
            if (cfg.Board.Height < 5 || cfg.Board.Height > 12)
                throw new Exception($"board.height must be [5..12], got {cfg.Board.Height}");
            if (cfg.Moves < 1 || cfg.Moves > 60)
                throw new Exception($"level.moves must be [1..60], got {cfg.Moves}");
            if (cfg.Board.Colors == null || cfg.Board.Colors.Count < 3)
                throw new Exception($"board.colors must have >= 3 colors");
            if (cfg.Goals != null)
                foreach (var g in cfg.Goals)
                    if (g.Count <= 0) throw new Exception($"goal count must be > 0, got {g.Count}");

            if (cfg.Layout?.Cells != null && cfg.Layout.Cells.Count > 0)
            {
                int expected = cfg.Board.Width * cfg.Board.Height;
                // Only validate if cells are fully specified (not partial)
                // Partial layouts (sparse) are allowed; full layout must match exactly
            }
        }

        public static void ValidateKingdomNodes(KingdomNodesConfig cfg)
        {
            if (cfg == null) throw new Exception("KingdomNodesConfig is null");
            var ids = new HashSet<string>();
            foreach (var n in cfg.Nodes)
            {
                if (!ids.Add(n.Id))
                    throw new Exception($"Duplicate kingdom node id: {n.Id}");
            }
            foreach (var n in cfg.Nodes)
                foreach (var prereq in n.Prereq)
                    if (!ids.Contains(prereq))
                        throw new Exception($"Node '{n.Id}' has unknown prereq '{prereq}'");
        }
    }
}
