using System.Collections.Generic;
using UnityEngine;

namespace Parcial2.Editor.Levels
{
    internal sealed class WallOutline
    {
        private readonly IReadOnlyList<Rect> walkable;
        private readonly List<float> xs;
        private readonly List<float> zs;
        private readonly float thickness;

        private WallOutline(IReadOnlyList<Rect> walkable, float thickness)
        {
            this.walkable = walkable;
            this.thickness = thickness;
            xs = SortedEdges(walkable, true);
            zs = SortedEdges(walkable, false);
        }

        public static List<Rect> Build(IReadOnlyList<Rect> walkable, float thickness) =>
            new WallOutline(walkable, thickness).Build();

        private List<Rect> Build()
        {
            var walls = new List<Rect>();
            for (int j = 0; j < zs.Count; j++)
                AddHorizontalRuns(j, walls);
            for (int i = 0; i < xs.Count; i++)
                AddVerticalRuns(i, walls);
            return walls;
        }

        private void AddHorizontalRuns(int j, List<Rect> walls)
        {
            int columns = xs.Count - 1;
            int i = 0;
            while (i < columns)
            {
                int side = Side(IsWalkable(i, j - 1), IsWalkable(i, j));
                if (side == 0)
                {
                    i++;
                    continue;
                }

                int start = i;
                while (i < columns && Side(IsWalkable(i, j - 1), IsWalkable(i, j)) == side)
                    i++;

                int solidRow = side > 0 ? j : j - 1;
                float xMin = xs[start] - (IsWalkable(start - 1, solidRow) ? 0f : thickness);
                float xMax = xs[i] + (IsWalkable(i, solidRow) ? 0f : thickness);
                float zMin = side > 0 ? zs[j] : zs[j] - thickness;
                walls.Add(Rect.MinMaxRect(xMin, zMin, xMax, zMin + thickness));
            }
        }

        private void AddVerticalRuns(int i, List<Rect> walls)
        {
            int rows = zs.Count - 1;
            int j = 0;
            while (j < rows)
            {
                int side = Side(IsWalkable(i - 1, j), IsWalkable(i, j));
                if (side == 0)
                {
                    j++;
                    continue;
                }

                int start = j;
                while (j < rows && Side(IsWalkable(i - 1, j), IsWalkable(i, j)) == side)
                    j++;

                int solidColumn = side > 0 ? i : i - 1;
                float zMin = zs[start] - (IsWalkable(solidColumn, start - 1) ? 0f : thickness);
                float zMax = zs[j] + (IsWalkable(solidColumn, j) ? 0f : thickness);
                float xMin = side > 0 ? xs[i] : xs[i] - thickness;
                walls.Add(Rect.MinMaxRect(xMin, zMin, xMin + thickness, zMax));
            }
        }

        private static int Side(bool before, bool after)
        {
            if (before == after)
                return 0;
            return before ? 1 : -1;
        }

        private bool IsWalkable(int column, int row)
        {
            if (column < 0 || row < 0 || column >= xs.Count - 1 || row >= zs.Count - 1)
                return false;

            var center = new Vector2((xs[column] + xs[column + 1]) * 0.5f, (zs[row] + zs[row + 1]) * 0.5f);
            foreach (Rect rect in walkable)
            {
                if (rect.Contains(center))
                    return true;
            }

            return false;
        }

        private static List<float> SortedEdges(IReadOnlyList<Rect> rects, bool alongX)
        {
            var edges = new SortedSet<float>();
            foreach (Rect rect in rects)
            {
                edges.Add(alongX ? rect.xMin : rect.yMin);
                edges.Add(alongX ? rect.xMax : rect.yMax);
            }

            return new List<float>(edges);
        }
    }
}
