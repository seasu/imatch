using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Game.Core
{
    public struct MatchGroup
    {
        public List<Vector2Int> Cells;
        public bool IsHorizontal;
        public bool IsVertical;
        // L/T shape: has both
    }

    public class MatchDetector
    {
        public List<MatchGroup> FindMatches(Board board)
        {
            var matched = new HashSet<Vector2Int>();
            var groups = new List<MatchGroup>();

            // Horizontal runs
            for (int y = 0; y < board.Height; y++)
            {
                int x = 0;
                while (x < board.Width)
                {
                    var cell = board.Get(x, y);
                    if (!CanMatch(cell)) { x++; continue; }

                    int runEnd = x + 1;
                    while (runEnd < board.Width && CanMatchSameColor(board, x, y, runEnd, y))
                        runEnd++;

                    int len = runEnd - x;
                    if (len >= 3)
                    {
                        var cells = new List<Vector2Int>();
                        for (int i = x; i < runEnd; i++)
                            cells.Add(new Vector2Int(i, y));
                        groups.Add(new MatchGroup { Cells = cells, IsHorizontal = true });
                        foreach (var c in cells) matched.Add(c);
                    }
                    x = runEnd;
                }
            }

            // Vertical runs
            for (int x = 0; x < board.Width; x++)
            {
                int y = 0;
                while (y < board.Height)
                {
                    var cell = board.Get(x, y);
                    if (!CanMatch(cell)) { y++; continue; }

                    int runEnd = y + 1;
                    while (runEnd < board.Height && CanMatchSameColor(board, x, y, x, runEnd))
                        runEnd++;

                    int len = runEnd - y;
                    if (len >= 3)
                    {
                        var cells = new List<Vector2Int>();
                        for (int i = y; i < runEnd; i++)
                            cells.Add(new Vector2Int(x, i));
                        groups.Add(new MatchGroup { Cells = cells, IsVertical = true });
                        foreach (var c in cells) matched.Add(c);
                    }
                    y = runEnd;
                }
            }

            // Merge overlapping groups (L/T detection)
            return MergeAndMarkLT(groups);
        }

        private bool CanMatch(BoardCell cell) =>
            cell.Kind == TileKind.Normal && !cell.HasOverlay;

        private bool CanMatchSameColor(Board board, int x1, int y1, int x2, int y2)
        {
            var a = board.Get(x1, y1);
            var b = board.Get(x2, y2);
            return CanMatch(a) && CanMatch(b) && a.Color == b.Color;
        }

        private List<MatchGroup> MergeAndMarkLT(List<MatchGroup> groups)
        {
            // Look for groups that share cells – mark them as L/T shapes
            for (int i = 0; i < groups.Count; i++)
            {
                for (int j = i + 1; j < groups.Count; j++)
                {
                    if (SharesCell(groups[i], groups[j]))
                    {
                        // Merge into single L/T group
                        var merged = new HashSet<Vector2Int>(groups[i].Cells);
                        foreach (var c in groups[j].Cells) merged.Add(c);
                        var g = new MatchGroup
                        {
                            Cells = new List<Vector2Int>(merged),
                            IsHorizontal = groups[i].IsHorizontal || groups[j].IsHorizontal,
                            IsVertical = groups[i].IsVertical || groups[j].IsVertical
                        };
                        groups[i] = g;
                        groups.RemoveAt(j);
                        j--;
                    }
                }
            }
            return groups;
        }

        private bool SharesCell(MatchGroup a, MatchGroup b)
        {
            var setA = new HashSet<Vector2Int>(a.Cells);
            foreach (var c in b.Cells)
                if (setA.Contains(c)) return true;
            return false;
        }
    }
}
