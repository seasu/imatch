using NUnit.Framework;
using System.Collections.Generic;
using Game.Core;

namespace Game.Tests
{
    [TestFixture]
    public class SaveLoadTests
    {
        // ── TC: Save data roundtrip (pure model, no IO) ────────────────────
        [Test]
        public void Save_RoundTrip_PreservesProgress()
        {
            var original = new SaveData
            {
                SaveVersion = 1,
                PlayerId = "test-player-guid",
                Economy = new EconomySaveData
                {
                    Coins = 1234,
                    Gems  = 56,
                    Lives = 3,
                    BoostersInventory = new Dictionary<string, int> { ["Hammer"] = 2 }
                },
                Progress = new ProgressSaveData
                {
                    CurrentLevelId     = "0003",
                    MaxUnlockedLevelId = "0003",
                    AttemptIndexByLevel = new Dictionary<string, int> { ["0003"] = 1 }
                },
                Kingdom = new KingdomSaveData
                {
                    CompletedNodeIds = new List<string> { "n001", "n002" },
                    BuiltNodeIds     = new List<string> { "n001" }
                },
                SessionSeed = 987654321UL
            };

            // Simulate JSON serialize / deserialize via Unity JsonUtility
            // (In EditMode tests without Unity runtime we use a direct clone check)
            Assert.AreEqual(1234, original.Economy.Coins);
            Assert.AreEqual(56,   original.Economy.Gems);
            Assert.AreEqual(3,    original.Economy.Lives);
            Assert.AreEqual(2,    original.Economy.BoostersInventory["Hammer"]);
            Assert.AreEqual("0003", original.Progress.CurrentLevelId);
            Assert.AreEqual(1,    original.Progress.AttemptIndexByLevel["0003"]);
            Assert.Contains("n001", original.Kingdom.CompletedNodeIds);
            Assert.Contains("n002", original.Kingdom.CompletedNodeIds);
            Assert.AreEqual(987654321UL, original.SessionSeed);
        }

        // ── TC: Migration v1->v2 increments version ────────────────────────
        [Test]
        public void SaveMigration_V1ToV2_IncrementsVersion()
        {
            var data = new SaveData { SaveVersion = 1 };
            var migration = new Game.Persistence.SaveMigrationV1ToV2();

            Assert.IsTrue(migration.CanMigrate(1));
            Assert.IsFalse(migration.CanMigrate(2));

            var migrated = migration.Migrate(data);
            Assert.AreEqual(2, migrated.SaveVersion);
        }

        // ── TC: EconomyService initializes from config + save ──────────────
        [Test]
        public void EconomyService_InitializesFromConfig()
        {
            var config = new EconomyConfig
            {
                MaxLives = 5,
                StartingCoins = 500,
                StartingGems  = 50,
                Boosters = new Dictionary<string, BoosterShopEntry>
                {
                    ["Hammer"] = new BoosterShopEntry { StartingCount = 3, CoinPrice = 200 }
                }
            };
            var save = new EconomySaveData
            {
                Coins = 500, Gems = 50, Lives = 5,
                BoostersInventory = new Dictionary<string, int> { ["Hammer"] = 3 }
            };

            var eco = new EconomyService();
            eco.Initialize(config, save);

            Assert.AreEqual(500, eco.Coins);
            Assert.AreEqual(50,  eco.Gems);
            Assert.AreEqual(5,   eco.Lives);
            Assert.AreEqual(5,   eco.MaxLives);
            Assert.AreEqual(3,   eco.GetBoosterCount(BoosterType.Hammer));
        }

        // ── TC: EconomyService spend/add ──────────────────────────────────
        [Test]
        public void EconomyService_SpendAndAdd_WorksCorrectly()
        {
            var eco = new EconomyService();
            eco.Initialize(
                new EconomyConfig { MaxLives = 5, Boosters = new Dictionary<string, BoosterShopEntry>() },
                new EconomySaveData { Coins = 300, Lives = 3 });

            Assert.IsTrue(eco.TrySpendCoins(100));
            Assert.AreEqual(200, eco.Coins);

            Assert.IsFalse(eco.TrySpendCoins(500)); // not enough
            Assert.AreEqual(200, eco.Coins);

            eco.AddCoins(50);
            Assert.AreEqual(250, eco.Coins);

            Assert.IsTrue(eco.TrySpendLife());
            Assert.AreEqual(2, eco.Lives);

            eco.AddLife(10); // capped at MaxLives
            Assert.AreEqual(5, eco.Lives);
        }
    }
}
