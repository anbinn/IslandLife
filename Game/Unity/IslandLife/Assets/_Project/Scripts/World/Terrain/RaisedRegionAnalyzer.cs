using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// Splits the logical Raised mask into connected regions and decides, per region, whether the
    /// author's Hills.png grammar can express it. IL-WORLD-004S-R6.
    ///
    /// This class never picks a Sprite and never inspects a pixel. It reads only the logical
    /// Raised mask, and it is the single place where SUPPORTED / UNSUPPORTED is decided.
    ///
    /// Proven limits, each traceable to IL-WORLD-004S-R5:
    ///   - width &gt;= 3: LEFT_TERMINAL + BODY x (W-2) + RIGHT_TERMINAL, verified at W = 3, 5, 7,
    ///     and by the author's own 4-wide band at Hills r3c4..r3c7.
    ///   - the author's stack is always 2 surface rows above the cliff row, so height 2 and 3 are
    ///     the only heights that place the cliff deterministically. Height 4+ would need a body
    ///     repeat that R5 did not prove, so it is refused rather than guessed.
    ///   - width 1: the author's own narrow column Hills c3 r0..r3, exactly 4 cells tall.
    ///   - width 2: the author drew no 2-wide instance and terminal|terminal is not an author
    ///     adjacency, so it is refused.
    /// </summary>
    public static class RaisedRegionAnalyzer
    {
        /// <summary>Narrowest proven rectangle. Author-wide cliff rows are terminal + body + terminal.</summary>
        public const int MinimumRectangleWidth = 3;

        /// <summary>Shortest proven rectangle: the two surface rows above the cliff row.</summary>
        public const int MinimumRectangleHeight = 2;

        /// <summary>
        /// Tallest proven rectangle. R5 proved a 3-tall plateau in-mask (author r0..r2). A fourth
        /// row would have to repeat the r3 tier body, and R5 measured that body as NOT
        /// self-repeating (soil col0 = 8 vs col15 = 7), so this is refused instead of invented.
        /// </summary>
        public const int MaximumRectangleHeight = 3;

        /// <summary>Tallest proven narrow region: the author's c3 column is exactly r0..r3.</summary>
        public const int MaximumNarrowHeight = 4;

        /// <summary>
        /// Finds every 4-connected region of Raised cells, ordered by descending cell count then by
        /// position so results are deterministic.
        /// </summary>
        public static List<RaisedRegion> Analyze(TerrainGridData grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var regions = new List<RaisedRegion>();
            var visited = new HashSet<Vector2Int>();

            for (int localY = 0; localY < grid.Height; localY++)
            {
                for (int localX = 0; localX < grid.Width; localX++)
                {
                    int worldX = checked(grid.OriginX + localX);
                    int worldY = checked(grid.OriginY + localY);
                    if (grid.GetElevation(worldX, worldY) != ElevationLevel.Raised)
                    {
                        continue;
                    }

                    var seed = new Vector2Int(worldX, worldY);
                    if (visited.Contains(seed))
                    {
                        continue;
                    }

                    regions.Add(FloodFill(grid, seed, visited));
                }
            }

            regions.Sort(Compare);
            return regions;
        }

        /// <summary>
        /// Total Raised cell count across all regions. Used by tests to prove the renderer never
        /// changes logical data.
        /// </summary>
        public static int CountRaisedCells(TerrainGridData grid)
        {
            int total = 0;
            for (int localY = 0; localY < grid.Height; localY++)
            {
                for (int localX = 0; localX < grid.Width; localX++)
                {
                    int worldX = checked(grid.OriginX + localX);
                    int worldY = checked(grid.OriginY + localY);
                    if (grid.GetElevation(worldX, worldY) == ElevationLevel.Raised)
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        private static int Compare(RaisedRegion a, RaisedRegion b)
        {
            int bySize = b.CellCount.CompareTo(a.CellCount);
            if (bySize != 0)
            {
                return bySize;
            }

            int byY = a.MinY.CompareTo(b.MinY);
            return byY != 0 ? byY : a.MinX.CompareTo(b.MinX);
        }

        private static RaisedRegion FloodFill(
            TerrainGridData grid,
            Vector2Int seed,
            HashSet<Vector2Int> visited)
        {
            var cells = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(seed);
            visited.Add(seed);

            int minX = seed.x;
            int maxX = seed.x;
            int minY = seed.y;
            int maxY = seed.y;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                cells.Add(current);

                if (current.x < minX)
                {
                    minX = current.x;
                }
                else if (current.x > maxX)
                {
                    maxX = current.x;
                }

                if (current.y < minY)
                {
                    minY = current.y;
                }
                else if (current.y > maxY)
                {
                    maxY = current.y;
                }

                TryEnqueue(grid, current.x + 1, current.y, queue, visited);
                TryEnqueue(grid, current.x - 1, current.y, queue, visited);
                TryEnqueue(grid, current.x, current.y + 1, queue, visited);
                TryEnqueue(grid, current.x, current.y - 1, queue, visited);
            }

            Classify(cells, minX, minY, maxX, maxY, out RaisedRegionSupport? support,
                out RaisedRegionUnsupportedReason? reason);

            return new RaisedRegion(cells, minX, minY, maxX, maxY, support, reason);
        }

        private static void TryEnqueue(
            TerrainGridData grid,
            int x,
            int y,
            Queue<Vector2Int> queue,
            HashSet<Vector2Int> visited)
        {
            if (!grid.IsInside(x, y)
                || grid.GetElevation(x, y) != ElevationLevel.Raised)
            {
                return;
            }

            var cell = new Vector2Int(x, y);
            if (!visited.Add(cell))
            {
                return;
            }

            queue.Enqueue(cell);
        }

        private static void Classify(
            List<Vector2Int> cells,
            int minX,
            int minY,
            int maxX,
            int maxY,
            out RaisedRegionSupport? support,
            out RaisedRegionUnsupportedReason? reason)
        {
            support = null;
            reason = null;

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            // Not a solid rectangle. This is the check that catches L, T, notch and any outline
            // that turns, without needing to name the shape.
            if (cells.Count != width * height)
            {
                reason = RaisedRegionUnsupportedReason.NON_RECTANGULAR_REGION;
                return;
            }

            if (width == 1)
            {
                if (height <= MaximumNarrowHeight)
                {
                    support = RaisedRegionSupport.SUPPORTED_NARROW;
                }
                else
                {
                    reason = RaisedRegionUnsupportedReason.HEIGHT_NOT_PROVEN;
                }

                return;
            }

            if (width == 2)
            {
                reason = RaisedRegionUnsupportedReason.TWO_CELL_WIDTH;
                return;
            }

            if (width < MinimumRectangleWidth)
            {
                reason = RaisedRegionUnsupportedReason.WIDTH_NOT_PROVEN;
                return;
            }

            if (height < MinimumRectangleHeight)
            {
                reason = RaisedRegionUnsupportedReason.HEIGHT_NOT_PROVEN;
                return;
            }

            if (height > MaximumRectangleHeight)
            {
                reason = RaisedRegionUnsupportedReason.HEIGHT_NOT_PROVEN;
                return;
            }

            support = RaisedRegionSupport.SUPPORTED_RECTANGLE;
        }
    }
}