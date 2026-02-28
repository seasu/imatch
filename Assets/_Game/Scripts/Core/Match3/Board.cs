using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Game.Core
{
    public class Board
    {
        public readonly int Width;
        public readonly int Height;
        private readonly BoardCell[,] _cells;

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new BoardCell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[x, y] = new BoardCell();
        }

        public BoardCell Get(int x, int y) => _cells[x, y];
        public BoardCell Get(Vector2Int p) => _cells[p.x, p.y];

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public bool InBounds(Vector2Int p) => InBounds(p.x, p.y);

        /// Returns all 4 orthogonal neighbours that are in bounds.
        public List<Vector2Int> GetNeighbours(Vector2Int p)
        {
            var result = new List<Vector2Int>(4);
            TryAdd(p.x - 1, p.y, result);
            TryAdd(p.x + 1, p.y, result);
            TryAdd(p.x, p.y - 1, result);
            TryAdd(p.x, p.y + 1, result);
            return result;
        }

        private void TryAdd(int x, int y, List<Vector2Int> list)
        {
            if (InBounds(x, y)) list.Add(new Vector2Int(x, y));
        }

        public void Swap(Vector2Int a, Vector2Int b)
        {
            var tmp = _cells[a.x, a.y];
            _cells[a.x, a.y] = _cells[b.x, b.y];
            _cells[b.x, b.y] = tmp;
        }
    }
}
