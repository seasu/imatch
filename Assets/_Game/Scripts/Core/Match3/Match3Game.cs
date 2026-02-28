using System;
using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Game.Core
{
    /// Pure-logic Match-3 simulation. No UnityEngine usage except Vector2Int.
    public class Match3Game : IMatch3Game
    {
        // ── State ──────────────────────────────────────────────────────────
        public GameState State { get; private set; } = GameState.Idle;
        public int RemainingMoves { get; private set; }
        public int BossHp { get; private set; } = -1;

        private Board _board;
        private LevelConfig _level;
        private SeededRandom _rng;
        private MatchDetector _detector = new();
        private GoalTracker _goals;

        private readonly List<LogicEvent> _eventBuffer = new();
        private EndResult _endResult;

        private TileColor[] _spawnColors;
        private int[] _spawnWeights;

        private int _movesUsed;
        private int _boostersUsed;
        private float _startTime; // set by caller or left 0
        private int _bossMaxHp;

        // Boss tuning (injected from RemoteTuningConfig)
        public int DamagePerTile = 1;
        public int RocketDamageBonus = 6;
        public int BombDamageBonus = 10;

        // ── IMatch3Game ────────────────────────────────────────────────────
        public void Initialize(LevelConfig level, uint seed)
        {
            _level = level;
            _rng = new SeededRandom(seed);
            _board = new Board(level.Board.Width, level.Board.Height);
            _goals = new GoalTracker(level.Goals ?? new List<GoalConfig>());
            RemainingMoves = level.Moves;
            _movesUsed = 0;
            _boostersUsed = 0;
            _eventBuffer.Clear();
            _endResult = null;
            State = GameState.Running;

            if (level.Type == LevelType.BossAttack && level.Boss != null)
            {
                BossHp = level.Boss.Hp;
                _bossMaxHp = BossHp;
            }
            else
            {
                BossHp = -1;
            }

            BuildSpawnTable();
            FillBoard();
        }

        public SwapResult TrySwap(Vector2Int a, Vector2Int b)
        {
            if (State != GameState.Running) return SwapResult.Rejected;
            if (!_board.InBounds(a) || !_board.InBounds(b)) return SwapResult.Rejected;

            // Must be adjacent
            if (Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) != 1) return SwapResult.Rejected;

            var cellA = _board.Get(a);
            var cellB = _board.Get(b);

            if (!cellA.IsSwappable || !cellB.IsSwappable) return SwapResult.Rejected;

            // Special activation: if either tile is special, swap is allowed
            bool specialActivation = cellA.Kind != TileKind.Normal || cellB.Kind != TileKind.Normal;

            _board.Swap(a, b);
            var matches = _detector.FindMatches(_board);

            if (matches.Count == 0 && !specialActivation)
            {
                _board.Swap(a, b); // revert
                return SwapResult.Rejected;
            }

            _eventBuffer.Add(new SwapEvent { A = a, B = b });

            // Handle special-special combos first
            if (cellA.Kind != TileKind.Normal && cellB.Kind != TileKind.Normal)
            {
                HandleSpecialCombo(a, b, cellA, cellB);
            }
            else if (cellA.Kind != TileKind.Normal)
            {
                ActivateSpecial(a, cellA);
            }
            else if (cellB.Kind != TileKind.Normal)
            {
                ActivateSpecial(b, cellB);
            }

            if (matches.Count > 0)
                ProcessMatches(matches, a, b);

            Cascade();

            RemainingMoves--;
            _movesUsed++;
            CheckEndConditions();
            return SwapResult.Accepted;
        }

        public bool UseBooster(BoosterType booster, Vector2Int? cell = null)
        {
            if (State != GameState.Running) return false;

            switch (booster)
            {
                case BoosterType.Hammer:
                    if (!cell.HasValue || !_board.InBounds(cell.Value)) return false;
                    var target = _board.Get(cell.Value);
                    if (target.IsEmpty) return false;
                    RemoveTile(cell.Value, "Special");
                    Cascade();
                    _boostersUsed++;
                    CheckEndConditions();
                    return true;
            }
            return false;
        }

        public IReadOnlyList<LogicEvent> ConsumeEvents()
        {
            var copy = new List<LogicEvent>(_eventBuffer);
            _eventBuffer.Clear();
            return copy;
        }

        public EndResult GetEndResult() => _endResult;

        // ── Private: match processing ──────────────────────────────────────
        private void ProcessMatches(List<MatchGroup> groups, Vector2Int swapA, Vector2Int swapB)
        {
            foreach (var group in groups)
            {
                int len = group.Cells.Count;
                bool isLT = group.IsHorizontal && group.IsVertical;

                // Determine special to create
                TileKind special = TileKind.Normal;
                Vector2Int specialCell = group.Cells[len / 2];

                if (isLT || (len == 4))
                {
                    special = isLT ? TileKind.Bomb : TileKind.Rocket;
                }
                else if (len >= 5)
                {
                    special = TileKind.Disco;
                }

                var clearEvent = new MatchClearEvent { Source = "Match" };
                var matchColor = _board.Get(group.Cells[0]).Color;

                foreach (var pos in group.Cells)
                {
                    var cell = _board.Get(pos);
                    if (pos == specialCell && special != TileKind.Normal)
                        continue; // will be replaced by special

                    clearEvent.Cells.Add(pos);

                    if (cell.Kind == TileKind.Normal)
                        _goals.OnTileCollected(cell.Color);

                    // Damage adjacent overlays/obstacles
                    DamageAdjacent(pos);
                    cell.Clear();

                    if (_level.Type == LevelType.BossAttack)
                        ApplyBossDamage(DamagePerTile);
                }
                _eventBuffer.Add(clearEvent);

                if (special != TileKind.Normal)
                {
                    var orientation = group.IsHorizontal ? RocketOrientation.Horizontal : RocketOrientation.Vertical;
                    _board.Get(specialCell).SetSpecial(special, matchColor, orientation);
                    _eventBuffer.Add(new SpecialCreatedEvent { Cell = specialCell, Kind = special });
                }
            }
        }

        private void ActivateSpecial(Vector2Int pos, BoardCell cell)
        {
            switch (cell.Kind)
            {
                case TileKind.Rocket:
                    ActivateRocket(pos, cell.RocketOrientation);
                    break;
                case TileKind.Bomb:
                    ActivateBomb(pos);
                    break;
                case TileKind.Disco:
                    ActivateDisco(pos, cell.Color);
                    break;
            }
        }

        private void ActivateRocket(Vector2Int pos, RocketOrientation orientation)
        {
            var clearEvent = new MatchClearEvent { Source = "Special" };
            _board.Get(pos).Clear();

            if (orientation == RocketOrientation.Horizontal)
            {
                for (int x = 0; x < _board.Width; x++)
                    ClearCell(new Vector2Int(x, pos.y), clearEvent, isSpecial: true);
            }
            else
            {
                for (int y = 0; y < _board.Height; y++)
                    ClearCell(new Vector2Int(pos.x, y), clearEvent, isSpecial: true);
            }
            _eventBuffer.Add(clearEvent);

            if (_level.Type == LevelType.BossAttack)
                ApplyBossDamage(DamagePerTile * (_board.Width > _board.Height ? _board.Width : _board.Height) + RocketDamageBonus);
        }

        private void ActivateBomb(Vector2Int center)
        {
            var clearEvent = new MatchClearEvent { Source = "Special" };
            _board.Get(center).Clear();

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    ClearCell(new Vector2Int(center.x + dx, center.y + dy), clearEvent, isSpecial: true);

            _eventBuffer.Add(clearEvent);

            if (_level.Type == LevelType.BossAttack)
                ApplyBossDamage(DamagePerTile * 9 + BombDamageBonus);
        }

        private void ActivateDisco(Vector2Int pos, TileColor targetColor)
        {
            var clearEvent = new MatchClearEvent { Source = "Special" };
            _board.Get(pos).Clear();
            int count = 0;

            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.Get(x, y);
                    if (cell.Kind == TileKind.Normal && cell.Color == targetColor)
                    {
                        ClearCell(new Vector2Int(x, y), clearEvent, isSpecial: true);
                        count++;
                    }
                }
            _eventBuffer.Add(clearEvent);

            if (_level.Type == LevelType.BossAttack)
                ApplyBossDamage(DamagePerTile * count);
        }

        private void HandleSpecialCombo(Vector2Int posA, Vector2Int posB, BoardCell cellA, BoardCell cellB)
        {
            _board.Get(posA).Clear();
            _board.Get(posB).Clear();

            if ((cellA.Kind == TileKind.Rocket && cellB.Kind == TileKind.Rocket) ||
                (cellA.Kind == TileKind.Rocket && cellB.Kind == TileKind.Bomb) ||
                (cellA.Kind == TileKind.Bomb && cellB.Kind == TileKind.Rocket))
            {
                // Cross clear: both row and column through posA
                ActivateRocket(posA, RocketOrientation.Horizontal);
                ActivateRocket(posA, RocketOrientation.Vertical);
            }
            else if (cellA.Kind == TileKind.Disco || cellB.Kind == TileKind.Disco)
            {
                var otherCell = cellA.Kind == TileKind.Disco ? cellB : cellA;
                ActivateDisco(posA, otherCell.Color);
            }
            else
            {
                ActivateSpecial(posA, cellA);
                ActivateSpecial(posB, cellB);
            }
        }

        private void ClearCell(Vector2Int pos, MatchClearEvent evt, bool isSpecial)
        {
            if (!_board.InBounds(pos)) return;
            var cell = _board.Get(pos);
            if (cell.IsEmpty) return;

            if (cell.IsObstacle)
            {
                DamageObstacle(pos, cell);
                return;
            }

            if (cell.HasOverlay)
            {
                cell.OverlayHp--;
                if (cell.OverlayHp <= 0) cell.Overlay = null;
                return;
            }

            if (cell.Kind != TileKind.Empty)
            {
                if (cell.Kind == TileKind.Normal)
                    _goals.OnTileCollected(cell.Color);
                evt.Cells.Add(pos);
                cell.Clear();
            }
        }

        private void RemoveTile(Vector2Int pos, string source)
        {
            if (!_board.InBounds(pos)) return;
            var cell = _board.Get(pos);
            if (cell.IsEmpty) return;

            var evt = new MatchClearEvent { Source = source };
            evt.Cells.Add(pos);
            _eventBuffer.Add(evt);

            if (cell.IsObstacle)
            {
                DamageObstacle(pos, cell);
                return;
            }
            if (cell.Kind == TileKind.Normal)
                _goals.OnTileCollected(cell.Color);
            cell.Clear();

            if (_level.Type == LevelType.BossAttack)
                ApplyBossDamage(DamagePerTile);
        }

        private void DamageObstacle(Vector2Int pos, BoardCell cell)
        {
            cell.ObstacleHp--;
            _eventBuffer.Add(new ObstacleDamagedEvent
            {
                Cell = pos, Obstacle = cell.ObstacleKind, RemainingHp = cell.ObstacleHp
            });
            if (cell.ObstacleHp <= 0)
            {
                _goals.OnObstacleDestroyed(cell.ObstacleKind);
                cell.Clear();
            }
        }

        private void DamageAdjacent(Vector2Int pos)
        {
            foreach (var n in _board.GetNeighbours(pos))
            {
                var cell = _board.Get(n);
                if (cell.IsObstacle)
                    DamageObstacle(n, cell);
                else if (cell.HasOverlay)
                {
                    cell.OverlayHp--;
                    if (cell.OverlayHp <= 0) cell.Overlay = null;
                }
            }
        }

        private void ApplyBossDamage(int amount)
        {
            if (BossHp < 0) return;
            BossHp = Math.Max(0, BossHp - amount);
            _eventBuffer.Add(new BossDamagedEvent { Amount = amount, RemainingHp = BossHp });
        }

        // ── Cascade (gravity + spawn) ──────────────────────────────────────
        private void Cascade()
        {
            bool changed;
            do
            {
                ApplyGravity();
                SpawnNewTiles();
                var matches = _detector.FindMatches(_board);
                if (matches.Count > 0)
                {
                    ProcessMatches(matches, default, default);
                    changed = true;
                }
                else changed = false;
            } while (changed);
        }

        private void ApplyGravity()
        {
            for (int x = 0; x < _board.Width; x++)
            {
                // Compact column: move non-empty cells to bottom
                int writeY = 0;
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.Get(x, y);
                    if (!cell.IsEmpty && !cell.IsObstacle)
                    {
                        if (writeY != y)
                        {
                            _board.Get(x, writeY).Kind = cell.Kind;
                            _board.Get(x, writeY).Color = cell.Color;
                            _board.Get(x, writeY).RocketOrientation = cell.RocketOrientation;
                            _eventBuffer.Add(new TileFellEvent
                            {
                                From = new Vector2Int(x, y),
                                To = new Vector2Int(x, writeY)
                            });
                            cell.Clear();
                        }
                        writeY++;
                    }
                    else if (cell.IsObstacle)
                    {
                        writeY = y + 1; // obstacles don't fall
                    }
                }
            }
        }

        private void SpawnNewTiles()
        {
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.Get(x, y);
                    if (cell.IsEmpty)
                    {
                        var color = PickColor();
                        cell.SetNormal(color);
                        _eventBuffer.Add(new TileSpawnedEvent
                        {
                            At = new Vector2Int(x, y), Color = color
                        });
                    }
                }
        }

        // ── Board init ─────────────────────────────────────────────────────
        private void FillBoard()
        {
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.Get(x, y);
                    cell.SetNormal(PickColor());
                }

            // Ensure no immediate matches at start by shuffling problem tiles
            int maxRetries = 20;
            while (_detector.FindMatches(_board).Count > 0 && maxRetries-- > 0)
            {
                for (int x = 0; x < _board.Width; x++)
                    for (int y = 0; y < _board.Height; y++)
                        _board.Get(x, y).SetNormal(PickColor());
            }

            // Apply layout overrides (obstacles/specials from level config)
            if (_level.Layout?.Cells != null)
                ApplyLayout();
        }

        private void ApplyLayout()
        {
            foreach (var entry in _level.Layout.Cells)
            {
                var parts = entry.Split(',');
                if (parts.Length < 3) continue;
                if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)) continue;
                if (!_board.InBounds(x, y)) continue;

                var cell = _board.Get(x, y);
                switch (parts[2])
                {
                    case "Crate": cell.SetObstacle(ObstacleKind.Crate, 1); break;
                    case "Stone":
                        int hp = parts.Length > 3 && int.TryParse(parts[3], out var h) ? h : 3;
                        cell.SetObstacle(ObstacleKind.Stone, hp);
                        break;
                    case "Ice":
                        cell.Overlay = OverlayKind.Ice;
                        cell.OverlayHp = parts.Length > 3 && int.TryParse(parts[3], out var il) ? il : 1;
                        break;
                }
            }
        }

        // ── Spawn table ────────────────────────────────────────────────────
        private void BuildSpawnTable()
        {
            var colors = _level.Board.Colors;
            _spawnColors = new TileColor[colors.Count];
            _spawnWeights = new int[colors.Count];

            for (int i = 0; i < colors.Count; i++)
            {
                if (System.Enum.TryParse<TileColor>(colors[i], out var c))
                    _spawnColors[i] = c;
                _level.Board.SpawnWeights.TryGetValue(colors[i], out var w);
                _spawnWeights[i] = w > 0 ? w : 1;
            }
        }

        private TileColor PickColor()
        {
            int idx = _rng.WeightedSelect(_spawnWeights);
            return _spawnColors[idx];
        }

        // ── End conditions ─────────────────────────────────────────────────
        private void CheckEndConditions()
        {
            if (State != GameState.Running) return;

            bool win = false;
            if (_level.Type == LevelType.BossAttack)
                win = BossHp == 0;
            else
                win = _goals.AllGoalsMet();

            if (win)
            {
                State = GameState.Won;
                _endResult = new EndResult
                {
                    Result = LevelResult.Win,
                    MovesUsed = _movesUsed,
                    BossHpRemaining = _level.Type == LevelType.BossAttack ? (int?)BossHp : null,
                    CoinsGained = _level.Rewards?.Coins ?? 0,
                    BoostersUsed = _boostersUsed
                };
                return;
            }

            if (RemainingMoves <= 0)
            {
                State = GameState.Lost;
                _endResult = new EndResult
                {
                    Result = LevelResult.Lose,
                    MovesUsed = _movesUsed,
                    BossHpRemaining = _level.Type == LevelType.BossAttack ? (int?)BossHp : null,
                    CoinsGained = 0,
                    BoostersUsed = _boostersUsed
                };
            }
        }
    }
}
