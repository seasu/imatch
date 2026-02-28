using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity
{
    using Game.Core;

    /// Manages visual representation of the board.
    /// Attach to a GameObject in Match3.unity.
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private float _tileSize = 1f;

        private int _width, _height;
        private TileView[,] _tileViews;

        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;
            _tileViews = new TileView[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var pos = GridToWorld(x, y);
                    var tv = Instantiate(_tilePrefab, pos, Quaternion.identity, transform);
                    tv.name = $"Tile_{x}_{y}";
                    _tileViews[x, y] = tv;
                }
        }

        public void ProcessEvents(IReadOnlyList<LogicEvent> events)
        {
            foreach (var evt in events)
            {
                switch (evt)
                {
                    case TileSpawnedEvent spawn:
                        if (InBounds(spawn.At))
                            _tileViews[spawn.At.x, spawn.At.y].ShowColor(spawn.Color);
                        break;

                    case MatchClearEvent clear:
                        foreach (var pos in clear.Cells)
                            if (InBounds(pos))
                                _tileViews[pos.x, pos.y].PlayPop();
                        break;

                    case TileFellEvent fell:
                        if (InBounds(fell.From) && InBounds(fell.To))
                        {
                            var tv = _tileViews[fell.From.x, fell.From.y];
                            _tileViews[fell.To.x, fell.To.y] = tv;
                            _tileViews[fell.From.x, fell.From.y] = null;
                            tv.MoveTo(GridToWorld(fell.To.x, fell.To.y));
                        }
                        break;

                    case SwapEvent swap:
                        if (InBounds(swap.A) && InBounds(swap.B))
                        {
                            var ta = _tileViews[swap.A.x, swap.A.y];
                            var tb = _tileViews[swap.B.x, swap.B.y];
                            _tileViews[swap.A.x, swap.A.y] = tb;
                            _tileViews[swap.B.x, swap.B.y] = ta;
                            ta?.MoveTo(GridToWorld(swap.B.x, swap.B.y));
                            tb?.MoveTo(GridToWorld(swap.A.x, swap.A.y));
                        }
                        break;

                    case SpecialCreatedEvent special:
                        if (InBounds(special.Cell))
                            _tileViews[special.Cell.x, special.Cell.y].ShowSpecial(special.Kind);
                        break;
                }
            }
        }

        private Vector3 GridToWorld(int x, int y)
        {
            float offsetX = (_width  - 1) * _tileSize * 0.5f;
            float offsetY = (_height - 1) * _tileSize * 0.5f;
            return new Vector3(x * _tileSize - offsetX, y * _tileSize - offsetY, 0f);
        }

        private bool InBounds(Vector2Int p) => p.x >= 0 && p.x < _width && p.y >= 0 && p.y < _height;
    }
}
