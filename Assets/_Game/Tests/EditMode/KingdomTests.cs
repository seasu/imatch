using NUnit.Framework;
using System.Collections.Generic;
using Game.Core;

namespace Game.Tests
{
    [TestFixture]
    public class KingdomTests
    {
        private KingdomNodesConfig MakeConfig()
        {
            return new KingdomNodesConfig
            {
                Version = 1,
                Nodes = new List<KingdomNodeConfig>
                {
                    new KingdomNodeConfig { Id = "n001", Type = NodeType.BuildNode,  Title = "Gate",  CostCoins = 300, Prereq = new List<string>() },
                    new KingdomNodeConfig { Id = "n002", Type = NodeType.PuzzleGate, Title = "Puzzle1", LevelId = "0001", Prereq = new List<string> { "n001" } },
                    new KingdomNodeConfig { Id = "n003", Type = NodeType.BuildNode,  Title = "Tower", CostCoins = 400, Prereq = new List<string> { "n002" } }
                }
            };
        }

        private EconomyService MakeEconomy(int coins = 1000)
        {
            var eco = new EconomyService();
            eco.Initialize(
                new EconomyConfig { MaxLives = 5, Boosters = new Dictionary<string, BoosterShopEntry>() },
                new EconomySaveData { Coins = coins, Lives = 5 });
            return eco;
        }

        // ── TC: Node availability after prereqs ────────────────────────────
        [Test]
        public void Kingdom_Prereq_UnlocksCorrectly()
        {
            var config = MakeConfig();
            var save = new SaveData();
            var eco = MakeEconomy();
            var svc = new KingdomService();
            svc.Initialize(config, save, eco);

            var nodes = svc.GetNodes();
            Assert.AreEqual(NodeState.Available, nodes[0].State, "n001 should be Available (no prereqs)");
            Assert.AreEqual(NodeState.Locked,    nodes[1].State, "n002 should be Locked (n001 not complete)");
            Assert.AreEqual(NodeState.Locked,    nodes[2].State, "n003 should be Locked");
        }

        [Test]
        public void Kingdom_CompleteBuild_UnlocksNext()
        {
            var config = MakeConfig();
            var save = new SaveData();
            var eco = MakeEconomy(coins: 1000);
            var svc = new KingdomService();
            svc.Initialize(config, save, eco);

            bool built = svc.TryCompleteBuild("n001");
            Assert.IsTrue(built, "Should successfully build n001");
            Assert.AreEqual(700, eco.Coins, "300 coins should be spent");

            var nodes = svc.GetNodes();
            Assert.AreEqual(NodeState.Completed, nodes[0].State, "n001 should be Completed");
            Assert.AreEqual(NodeState.Available, nodes[1].State, "n002 should now be Available");
            Assert.AreEqual(NodeState.Locked,    nodes[2].State, "n003 should still be Locked");
        }

        [Test]
        public void Kingdom_CompleteBuild_FailsWithInsufficientCoins()
        {
            var config = MakeConfig();
            var save = new SaveData();
            var eco = MakeEconomy(coins: 100); // not enough for 300
            var svc = new KingdomService();
            svc.Initialize(config, save, eco);

            bool built = svc.TryCompleteBuild("n001");
            Assert.IsFalse(built, "Should fail to build n001 with insufficient coins");
            Assert.AreEqual(100, eco.Coins, "Coins should be unchanged");
        }

        [Test]
        public void Kingdom_PuzzleGate_StartPuzzle_ReturnsLevelId()
        {
            var config = MakeConfig();
            var save = new SaveData();
            save.Kingdom.CompletedNodeIds.Add("n001"); // prereq met
            var eco = MakeEconomy();
            var svc = new KingdomService();
            svc.Initialize(config, save, eco);

            bool started = svc.TryStartPuzzle("n002", out var levelId);
            Assert.IsTrue(started);
            Assert.AreEqual("0001", levelId);
        }

        [Test]
        public void Kingdom_PuzzleGate_LockedNode_CannotStartPuzzle()
        {
            var config = MakeConfig();
            var save = new SaveData();
            var eco = MakeEconomy();
            var svc = new KingdomService();
            svc.Initialize(config, save, eco);

            bool started = svc.TryStartPuzzle("n002", out var levelId);
            Assert.IsFalse(started, "Cannot start locked puzzle");
            Assert.IsNull(levelId);
        }

        [Test]
        public void Kingdom_OnLevelCompleted_MarksNodeComplete()
        {
            var config = MakeConfig();
            var save = new SaveData();
            save.Kingdom.CompletedNodeIds.Add("n001");
            var eco = MakeEconomy();
            var svc = new KingdomService();
            svc.Initialize(config, save, eco);

            svc.OnLevelCompleted("0001");

            var nodes = svc.GetNodes();
            Assert.AreEqual(NodeState.Completed, nodes[1].State, "n002 should be Completed after level 0001");
            Assert.AreEqual(NodeState.Available, nodes[2].State, "n003 should be Available after n002 completed");
        }
    }
}
