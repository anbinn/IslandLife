using System;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// The 8-neighbour Raised topology of one logical cell. IL-WORLD-004S-R12.
    ///
    /// This class is the ONLY place the Raised mask is read for visual purposes, and it reads it
    /// strictly read-only. It answers: "which of this cell's eight neighbours are also Raised?".
    ///
    /// It deliberately does NOT decide anything about art. It does not know what a Sprite is, it
    /// does not map a canonical mask to a sheet coordinate, and it contains no second normalisation
    /// rule and no second 47-state table: diagonal normalisation is delegated to the existing
    /// <see cref="TerrainMaskNormalizer"/>, which is shared with the normal Grass/Sand autotile
    /// system and is left exactly as it was.
    /// </summary>
    public static class RaisedNeighborResolver
    {
        /// <summary>Offsets of the eight neighbours, in the bit order of TerrainNeighborMask.</summary>
        private static readonly Vector2Int[] NeighbourOffsets =
        {
            new Vector2Int(-1, 1),  // NorthWest = 1 << 0
            new Vector2Int(0, 1),   // North    = 1 << 1
            new Vector2Int(1, 1),   // NorthEast = 1 << 2
            new Vector2Int(-1, 0),  // West     = 1 << 3
            new Vector2Int(1, 0),   // East     = 1 << 4
            new Vector2Int(-1, -1), // SouthWest = 1 << 5
            new Vector2Int(0, -1),  // South    = 1 << 6
            new Vector2Int(1, -1),  // SouthEast = 1 << 7
        };

        /// <summary>
        /// True only for a cell inside the grid whose elevation is Raised. Every other cell, including
        /// every cell outside the grid, reads as "not Raised", which is what lets the mask run off the
        /// edge of the map and produce a correct open outline there.
        /// </summary>
        public static bool IsRaised(TerrainGridData grid, int x, int y)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            return grid.IsInside(x, y) && grid.GetElevation(x, y) == ElevationLevel.Raised;
        }

        /// <summary>
        /// The raw 8-neighbour mask of Raised cells around (x, y). The centre itself is not required
        /// to be Raised by this method; the caller decides that.
        /// </summary>
        public static TerrainNeighborMask ResolveRaisedMask(TerrainGridData grid, int x, int y)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            TerrainNeighborMask mask = TerrainNeighborMask.None;
            for (int i = 0; i < NeighbourOffsets.Length; i++)
            {
                Vector2Int o = NeighbourOffsets[i];
                if (IsRaised(grid, x + o.x, y + o.y))
                {
                    mask |= (TerrainNeighborMask)(1 << i);
                }
            }

            return mask;
        }

        /// <summary>
        /// The canonical topology mask, produced by the SHARED normalizer. There is deliberately no
        /// second normalisation rule anywhere in the Raised path.
        /// </summary>
        public static TerrainNeighborMask ResolveCanonicalMask(TerrainGridData grid, int x, int y)
        {
            return TerrainMaskNormalizer.Normalize(ResolveRaisedMask(grid, x, y));
        }

        /// <summary>Convenience overload used by the 256-mask coverage test.</summary>
        public static TerrainNeighborMask Normalize(TerrainNeighborMask raw)
        {
            return TerrainMaskNormalizer.Normalize(raw);
        }

        /// <summary>
        /// Size of the maximal vertical run of Raised cells that contains (x, y), counted in both
        /// directions. Returns 0 when the cell is not Raised.
        ///
        /// This is the "plateau thickness" reading, and it is a purely structural query: it reads the
        /// mask, never writes it, and it is per column-run rather than per connected region. A freeform
        /// region with a thick middle and thin arms gets a different thickness per column, which is
        /// exactly what the visual needs.
        /// </summary>
        public static int MeasureRunDepth(TerrainGridData grid, int x, int y)
        {
            if (!IsRaised(grid, x, y))
            {
                return 0;
            }

            int depth = 1;
            for (int yy = y + 1; IsRaised(grid, x, yy); yy++)
            {
                depth++;
            }

            for (int yy = y - 1; IsRaised(grid, x, yy); yy--)
            {
                depth++;
            }

            return depth;
        }

        /// <summary>
        /// The y of the lowest Raised cell in the vertical run containing (x, y).
        /// </summary>
        public static int MeasureRunBottom(TerrainGridData grid, int x, int y)
        {
            if (!IsRaised(grid, x, y))
            {
                return y;
            }

            int bottom = y;
            while (IsRaised(grid, x, bottom - 1))
            {
                bottom--;
            }

            return bottom;
        }
    }
}
