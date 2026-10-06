using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// Splits the logical Raised mask into connected regions and decides, per region, whether a
    /// pixel-verified Author Hills Composition exists for it. IL-WORLD-004S-R6.
    ///
    /// ROLE, LOCKED BY IL-WORLD-004S-R11. This class is the CURRENT_PROVEN_COMPOSITION_CLASSIFIER.
    /// It answers exactly one question: "is this connected region inside the set of shapes we have
    /// already proven we can draw with the author's own Hills cells?" It is NOT the author's Hills
    /// grammar, and it is NOT a validator of the user's map.
    ///
    /// What a null classification does and does not mean:
    ///   - It does NOT mean the Raised data is invalid. The cells are valid, are stored in
    ///     TerrainMapData, and are never deleted, snapped, filled or rejected.
    ///   - It does NOT mean the author forbade the shape. It means no composition has been proven
    ///     yet. Extending the grammar means proving more compositions, not relaxing this check.
    ///
    /// This class never picks a Sprite and never inspects a pixel. It reads only the logical Raised
    /// mask, and it is the single place where a region is classified as proven or unresolved.
    ///
    /// Proven limits, each traceable to IL-WORLD-004S-R5:
    ///   - width &gt;= 3: LEFT_TERMINAL + BODY x (W-2) + RIGHT_TERMINAL, verified at W = 3, 5, 7,
    ///     and by the author's own 4-wide band at Hills r3c4..r3c7.
    ///   - height 1, 2 and 3 place the cliff deterministically. Height 1 was added by R9 after R8
    ///     proved the composition pixel-exactly at widths 3, 5 and 8.
    ///   - width 1: the author's own narrow column Hills c3 r0..r3, exactly 4 cells tall.
    /// </summary>
    public static class RaisedRegionAnalyzer
    {
        /// <summary>Narrowest proven rectangle. Author-wide cliff rows are terminal + body + terminal.</summary>
        public const int MinimumRectangleWidth = 3;

        /// <summary>
        /// Shortest proven rectangle.
        ///
        /// IL-WORLD-004S-R9 lowered this from 2 to 1. IL-WORLD-004S-R8 proved the height-1
        /// composition directly: a logical one-row band projects to the author's grass BODY row r1
        /// over the author's front CLIFF row r2, the cliff one visual row south of the logical mask,
        /// at widths 3, 5 and 8, matching Hills.png exactly (1536/1536, 2560/2560, 4096/4096 px)
        /// with zero seam steps. Height 4 stays excluded on purpose: R8 found the author's own
        /// 3-wide x 4-tall instance, but it carries a 1px seam defect and its r3 tier does not
        /// self-repeat, so the evidence standard is deliberately not lowered for coverage.
        /// </summary>
        public const int MinimumRectangleHeight = 1;

        /// <summary>
        /// Tallest proven rectangle. R5 proved a 3-tall plateau in-mask (author r0..r2). A fourth
        /// row would have to repeat the r3 tier body, and R5 measured that body as NOT
        /// self-repeating (soil col0 = 8 vs col15 = 7), so height 4+ is left unresolved rather
        /// than invented. This is a coverage limit, not a statement about the author.
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

            // Not a solid rectangle, so the outline turns, notches or encloses a hole. This single
            // check catches L, T, notch, U and ring without needing to name the shape, because no
            // turn or notch composition has been pixel-verified yet. The Raised cells are kept.
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