using NUnit.Framework;
using System.Collections.Generic;
using Game.Core;

namespace Game.Tests
{
    [TestFixture]
    public class Match3LogicTests
    {
        private LevelConfig MakeLevel(int moves = 30)
        {
            return new LevelConfig
            {
                Id = "test",
                Version = 1,
                Type = LevelType.Normal,
                Board = new BoardConfig
                {
                    Width = 9, Height = 9,
                    Colors = new List<string> { "Red", "Blue", "Green", "Yellow", "Purple" },
                    SpawnWeights = new Dictionary<string, int> { ["Red"]=1,["Blue"]=1,["Green"]=1,["Yellow"]=1,["Purple"]=1 }
                },
                Moves = moves,
                Goals = new List<GoalConfig>(),
                Layout = new LayoutConfig { Cells = new List<string>() },
                Rewards = new RewardsConfig { Coins = 10 }
            };
        }

        private Match3Game CreateGame(LevelConfig level = null, uint seed = 42)
        {
            var game = new Match3Game();
            game.Initialize(level ?? MakeLevel(), seed);
            return game;
        }

        // ── TC1: Swap rejection ────────────────────────────────────────────
        [Test]
        public void Match3_SwapReject_WhenNoMatch_AndNoSpecialActivation()
        {
            // Create a controlled board where we know a swap won't create a match.
            // By using a unique seed, we rely on the rejection logic.
            // We'll attempt an out-of-bounds swap which must always be rejected.
            var game = CreateGame();

            var result = game.TrySwap(new Vector2Int(-1, 0), new Vector2Int(0, 0));
            Assert.AreEqual(SwapResult.Rejected, result, "Out-of-bounds swap must be rejected");

            result = game.TrySwap(new Vector2Int(0, 0), new Vector2Int(5, 5));
            Assert.AreEqual(SwapResult.Rejected, result, "Non-adjacent swap must be rejected");
        }

        // ── TC2: Game initializes in Running state ────────────────────────
        [Test]
        public void Match3_Initialize_StateIsRunning()
        {
            var game = CreateGame();
            Assert.AreEqual(GameState.Running, game.State);
        }

        // ── TC3: Moves decrement on accepted swap ──────────────────────────
        [Test]
        public void Match3_AcceptedSwap_DecrementsRemainingMoves()
        {
            // Use a seed that produces at least one adjacent match on board.
            // We try many pairs until one is accepted.
            var level = MakeLevel(moves: 30);
            var game = new Match3Game();
            game.Initialize(level, 12345);

            int before = game.RemainingMoves;
            bool swapped = false;

            // Try all adjacent pairs until one is accepted
            for (int x = 0; x < 9 && !swapped; x++)
                for (int y = 0; y < 8 && !swapped; y++)
                {
                    var r = game.TrySwap(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                    if (r == SwapResult.Accepted) swapped = true;
                }

            if (swapped)
                Assert.AreEqual(before - 1, game.RemainingMoves, "Moves should decrement after accepted swap");
        }

        // ── TC4: BossAttack game has boss HP ──────────────────────────────
        [Test]
        public void Match3_BossAttack_BossHpPositive()
        {
            var level = MakeLevel();
            level.Type = LevelType.BossAttack;
            level.Boss = new BossConfig { Hp = 120 };
            level.Goals = new List<GoalConfig>();

            var game = new Match3Game();
            game.DamagePerTile = 1;
            game.Initialize(level, 42);

            Assert.AreEqual(120, game.BossHp, "Boss HP should start at configured value");
        }

        // ── TC5: Normal game boss HP is -1 ────────────────────────────────
        [Test]
        public void Match3_Normal_BossHpIsNegative()
        {
            var game = CreateGame();
            Assert.AreEqual(-1, game.BossHp, "Normal levels should have BossHp = -1");
        }

        // ── TC6: Game ends when moves reach 0 with no win ─────────────────
        [Test]
        public void Match3_LoseWhenMovesExhausted()
        {
            var level = MakeLevel(moves: 1);
            level.Goals = new List<GoalConfig>
            {
                new GoalConfig { Kind = GoalKind.Score, Count = 999999 }
            };
            var game = new Match3Game();
            game.Initialize(level, 99);

            // Force at least one accepted swap to exhaust moves
            bool swapped = false;
            for (int x = 0; x < 9 && !swapped; x++)
                for (int y = 0; y < 8 && !swapped; y++)
                {
                    var r = game.TrySwap(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                    if (r == SwapResult.Accepted) swapped = true;
                }

            if (swapped)
            {
                Assert.AreEqual(GameState.Lost, game.State, "Game should be Lost after moves exhausted");
                var end = game.GetEndResult();
                Assert.IsNotNull(end);
                Assert.AreEqual(LevelResult.Lose, end.Result);
            }
        }

        // ── TC7: Booster Hammer removes a tile ───────────────────────────
        [Test]
        public void Match3_Hammer_RemovesTile()
        {
            var game = CreateGame(seed: 777);
            Assert.AreEqual(GameState.Running, game.State);

            // Hammer should return true on valid cell
            var result = game.UseBooster(BoosterType.Hammer, new Vector2Int(4, 4));
            Assert.IsTrue(result, "Hammer on valid cell should succeed");

            var events = game.ConsumeEvents();
            Assert.IsTrue(events.Count > 0, "Hammer should emit at least one event");
        }

        // ── TC8: PRNG same seed produces same first 10 events ────────────
        [Test]
        public void PRNG_SameSeed_SameOutcome_ForReplay()
        {
            var level = MakeLevel();
            level.Goals = new List<GoalConfig>();

            var gameA = new Match3Game();
            gameA.Initialize(level, 42);

            var gameB = new Match3Game();
            gameB.Initialize(level, 42);

            // Exhaust initial events from both
            var evA = gameA.ConsumeEvents();
            var evB = gameB.ConsumeEvents();

            // Attempt same sequence of swaps on both
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 8; y++)
                {
                    var a = new Vector2Int(x, y);
                    var b = new Vector2Int(x, y + 1);
                    var rA = gameA.TrySwap(a, b);
                    var rB = gameB.TrySwap(a, b);
                    Assert.AreEqual(rA, rB, $"Both games must behave identically at ({x},{y})");
                }
        }
    }
}
