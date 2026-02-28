using NUnit.Framework;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.Content;

namespace Game.Tests
{
    [TestFixture]
    public class ConfigValidatorTests
    {
        // ── Economy ────────────────────────────────────────────────────────
        [Test]
        public void Config_Validators_Economy_Valid_Passes()
        {
            var cfg = new EconomyConfig { MaxLives = 5, StartingCoins = 500 };
            Assert.DoesNotThrow(() => ConfigValidators.ValidateEconomy(cfg));
        }

        [Test]
        public void Config_Validators_Economy_MaxLivesZero_Throws()
        {
            var cfg = new EconomyConfig { MaxLives = 0, StartingCoins = 500 };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateEconomy(cfg));
        }

        [Test]
        public void Config_Validators_Economy_NegativeCoins_Throws()
        {
            var cfg = new EconomyConfig { MaxLives = 5, StartingCoins = -1 };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateEconomy(cfg));
        }

        [Test]
        public void Config_Validators_Economy_Null_Throws()
        {
            Assert.Throws<Exception>(() => ConfigValidators.ValidateEconomy(null));
        }

        // ── RemoteTuning ────────────────────────────────────────────────────
        [Test]
        public void Config_Validators_RemoteTuning_Valid_Passes()
        {
            var cfg = new RemoteTuningConfig
            {
                SpawnWeightsDefault = new Dictionary<string, int> { ["Red"]=1,["Blue"]=1 },
                Boss = new BossTuning { DamagePerTile = 1 }
            };
            Assert.DoesNotThrow(() => ConfigValidators.ValidateRemoteTuning(cfg));
        }

        [Test]
        public void Config_Validators_RemoteTuning_NegativeDamage_Throws()
        {
            var cfg = new RemoteTuningConfig
            {
                SpawnWeightsDefault = new Dictionary<string, int> { ["Red"]=1 },
                Boss = new BossTuning { DamagePerTile = -1 }
            };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateRemoteTuning(cfg));
        }

        [Test]
        public void Config_Validators_RemoteTuning_ZeroSumWeights_Throws()
        {
            var cfg = new RemoteTuningConfig
            {
                SpawnWeightsDefault = new Dictionary<string, int> { ["Red"]=0 },
                Boss = new BossTuning { DamagePerTile = 1 }
            };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateRemoteTuning(cfg));
        }

        // ── Level ────────────────────────────────────────────────────────────
        [Test]
        public void Config_Validators_Level_Valid_Passes()
        {
            var cfg = new LevelConfig
            {
                Id = "0001",
                Board = new BoardConfig
                {
                    Width = 9, Height = 9,
                    Colors = new List<string> { "Red","Blue","Green" }
                },
                Moves = 20,
                Goals = new List<GoalConfig> { new GoalConfig { Count = 10 } }
            };
            Assert.DoesNotThrow(() => ConfigValidators.ValidateLevel(cfg));
        }

        [Test]
        public void Config_Validators_Level_MovesTooHigh_Throws()
        {
            var cfg = new LevelConfig
            {
                Id = "x",
                Board = new BoardConfig
                {
                    Width = 9, Height = 9,
                    Colors = new List<string> { "Red","Blue","Green" }
                },
                Moves = 61,
                Goals = new List<GoalConfig>()
            };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateLevel(cfg));
        }

        [Test]
        public void Config_Validators_Level_TooFewColors_Throws()
        {
            var cfg = new LevelConfig
            {
                Id = "x",
                Board = new BoardConfig
                {
                    Width = 9, Height = 9,
                    Colors = new List<string> { "Red","Blue" } // only 2
                },
                Moves = 20,
                Goals = new List<GoalConfig>()
            };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateLevel(cfg));
        }

        [Test]
        public void Config_Validators_Level_GoalCountZero_Throws()
        {
            var cfg = new LevelConfig
            {
                Id = "x",
                Board = new BoardConfig
                {
                    Width = 9, Height = 9,
                    Colors = new List<string> { "Red","Blue","Green" }
                },
                Moves = 20,
                Goals = new List<GoalConfig> { new GoalConfig { Count = 0 } }
            };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateLevel(cfg));
        }

        // ── Kingdom nodes ────────────────────────────────────────────────────
        [Test]
        public void Config_Validators_KingdomNodes_Valid_Passes()
        {
            var cfg = new KingdomNodesConfig
            {
                Nodes = new List<KingdomNodeConfig>
                {
                    new KingdomNodeConfig { Id = "n001", Prereq = new List<string>() },
                    new KingdomNodeConfig { Id = "n002", Prereq = new List<string> { "n001" } }
                }
            };
            Assert.DoesNotThrow(() => ConfigValidators.ValidateKingdomNodes(cfg));
        }

        [Test]
        public void Config_Validators_KingdomNodes_DuplicateId_Throws()
        {
            var cfg = new KingdomNodesConfig
            {
                Nodes = new List<KingdomNodeConfig>
                {
                    new KingdomNodeConfig { Id = "n001", Prereq = new List<string>() },
                    new KingdomNodeConfig { Id = "n001", Prereq = new List<string>() }
                }
            };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateKingdomNodes(cfg));
        }

        [Test]
        public void Config_Validators_KingdomNodes_UnknownPrereq_Throws()
        {
            var cfg = new KingdomNodesConfig
            {
                Nodes = new List<KingdomNodeConfig>
                {
                    new KingdomNodeConfig { Id = "n001", Prereq = new List<string> { "n999" } }
                }
            };
            Assert.Throws<Exception>(() => ConfigValidators.ValidateKingdomNodes(cfg));
        }
    }
}
