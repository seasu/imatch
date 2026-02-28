using NUnit.Framework;
using System.Collections.Generic;
using Game.Core;

namespace Game.Tests
{
    [TestFixture]
    public class BossAttackTests
    {
        private LevelConfig MakeBossLevel(int bossHp = 50, int moves = 30)
        {
            return new LevelConfig
            {
                Id = "boss_test",
                Version = 1,
                Type = LevelType.BossAttack,
                Board = new BoardConfig
                {
                    Width = 9, Height = 9,
                    Colors = new List<string> { "Red","Blue","Green","Yellow","Purple" },
                    SpawnWeights = new Dictionary<string, int> { ["Red"]=1,["Blue"]=1,["Green"]=1,["Yellow"]=1,["Purple"]=1 }
                },
                Moves = moves,
                Goals = new List<GoalConfig>(),
                Boss = new BossConfig { Hp = bossHp },
                Layout = new LayoutConfig { Cells = new List<string>() },
                Rewards = new RewardsConfig { Coins = 100 }
            };
        }

        // ── TC: Boss HP initialized correctly ────────────────────────────
        [Test]
        public void Boss_Hp_InitializedFromConfig()
        {
            var level = MakeBossLevel(bossHp: 120);
            var game = new Match3Game { DamagePerTile = 1 };
            game.Initialize(level, 42);

            Assert.AreEqual(120, game.BossHp);
        }

        // ── TC: Boss damage accumulates on moves ─────────────────────────
        [Test]
        public void Boss_Damage_AccumulatesExpected()
        {
            var level = MakeBossLevel(bossHp: 9999, moves: 30);
            var game = new Match3Game { DamagePerTile = 1 };
            game.Initialize(level, 42);

            int hpBefore = game.BossHp;
            Assert.AreEqual(9999, hpBefore);

            // Make at least one accepted swap
            bool swapped = false;
            for (int x = 0; x < 9 && !swapped; x++)
                for (int y = 0; y < 8 && !swapped; y++)
                {
                    var r = game.TrySwap(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                    if (r == SwapResult.Accepted) swapped = true;
                }

            if (swapped)
            {
                int hpAfter = game.BossHp;
                Assert.Less(hpAfter, hpBefore, "Boss HP should decrease after a move");
                Assert.GreaterOrEqual(hpAfter, 0, "Boss HP should not go negative");
            }
        }

        // ── TC: Boss win condition ─────────────────────────────────────────
        [Test]
        public void Boss_GameWins_WhenHpReachesZero()
        {
            // Create a level with 1 HP boss and DamagePerTile=9999
            var level = MakeBossLevel(bossHp: 1, moves: 30);
            var game = new Match3Game { DamagePerTile = 9999 };
            game.Initialize(level, 42);

            bool swapped = false;
            for (int x = 0; x < 9 && !swapped; x++)
                for (int y = 0; y < 8 && !swapped; y++)
                {
                    var r = game.TrySwap(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                    if (r == SwapResult.Accepted) swapped = true;
                }

            if (swapped)
            {
                Assert.AreEqual(0, game.BossHp, "Boss HP should be 0");
                Assert.AreEqual(GameState.Won, game.State, "Game should be Won when boss HP reaches 0");
                var result = game.GetEndResult();
                Assert.IsNotNull(result);
                Assert.AreEqual(LevelResult.Win, result.Result);
                Assert.AreEqual(0, result.BossHpRemaining);
            }
        }

        // ── TC: Boss lose condition ────────────────────────────────────────
        [Test]
        public void Boss_GameLoses_WhenMovesExhaustedAndBossAlive()
        {
            var level = MakeBossLevel(bossHp: 99999, moves: 1);
            var game = new Match3Game { DamagePerTile = 1 };
            game.Initialize(level, 42);

            bool swapped = false;
            for (int x = 0; x < 9 && !swapped; x++)
                for (int y = 0; y < 8 && !swapped; y++)
                {
                    var r = game.TrySwap(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                    if (r == SwapResult.Accepted) swapped = true;
                }

            if (swapped)
            {
                Assert.AreEqual(GameState.Lost, game.State, "Game should be Lost when moves run out with boss alive");
                var result = game.GetEndResult();
                Assert.IsNotNull(result);
                Assert.AreEqual(LevelResult.Lose, result.Result);
                Assert.Greater(result.BossHpRemaining ?? 0, 0, "Boss should still have HP remaining");
            }
        }

        // ── TC: PRNG seeded RNG determinism ──────────────────────────────
        [Test]
        public void PRNG_SameSeed_SameWeightedSelect()
        {
            var rng1 = new SeededRandom(12345);
            var rng2 = new SeededRandom(12345);
            var weights = new[] { 1, 2, 3, 1, 2 };

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(rng1.WeightedSelect(weights), rng2.WeightedSelect(weights),
                    $"Iteration {i}: RNGs with same seed must produce same output");
        }
    }
}
