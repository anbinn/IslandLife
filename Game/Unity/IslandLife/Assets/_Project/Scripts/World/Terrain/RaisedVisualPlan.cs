using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// The complete Raised visual projection for one grid. IL-WORLD-004S-R6, freeform from R12.
    ///
    /// R12 CHANGED THE PRODUCTION MODEL. The projection used to run
    /// RaisedRegionAnalyzer -> AuthorHillsCompositionResolver, so one connected region that was not a
    /// proven rectangle or column produced zero hill tiles for ALL of its cells. That is what left 235
    /// of the 283 Raised cells on the user's map with no visual at all.
    ///
    /// It now runs AuthorHillsLocalResolver, which is per cell: every Raised cell projects from its own
    /// 8-neighbour topology. RaisedRegionAnalyzer is still called, but only to REPORT the legacy
    /// region classification. It no longer decides whether anything is drawn, and its reasons are no
    /// longer diagnostics the user sees.
    ///
    /// The projection is pure and one-directional. It only reads the logical elevation, so an arbitrary
    /// user mask survives untouched: nothing is snapped, filled, thinned, deleted or normalised, whether
    /// or not it happens to be drawable.
    /// </summary>
    public sealed class RaisedVisualPlan
    {
        private RaisedVisualPlan(
            List<HillVisualTile> tiles,
            List<RaisedRegion> regions,
            List<RaisedVisualDiagnostic> diagnostics,
            int raisedCellCount,
            int visualizedRaisedCells,
            int tilesOutsideLogicalMask)
        {
            Tiles = tiles;
            Regions = regions;
            VisualDiagnostics = diagnostics;
            RaisedCellCount = raisedCellCount;
            VisualizedRaisedCells = visualizedRaisedCells;
            TilesOutsideLogicalMask = tilesOutsideLogicalMask;
        }

        /// <summary>Author visual tiles, in a fixed deterministic order. Never contains a guess.</summary>
        public IReadOnlyList<HillVisualTile> Tiles { get; }

        /// <summary>
        /// The legacy per-region classification, kept for diagnostics and reporting only. It has no
        /// effect on <see cref="Tiles"/>.
        /// </summary>
        public IReadOnlyList<RaisedRegion> Regions { get; }

        /// <summary>
        /// Real implementation problems only. Empty for every freeform shape: an L, T, U, notch, ring,
        /// two-wide run or tall plateau is drawn, not reported.
        /// </summary>
        public IReadOnlyList<RaisedVisualDiagnostic> VisualDiagnostics { get; }

        /// <summary>Logical Raised cells in the grid. This number is never changed by the renderer.</summary>
        public int RaisedCellCount { get; }

        /// <summary>Logical Raised cells that produced at least one author hill tile.</summary>
        public int VisualizedRaisedCells { get; }

        /// <summary>Tiles drawn south of their own logical mask. Proven necessary by R5.</summary>
        public int TilesOutsideLogicalMask { get; }

        /// <summary>
        /// Retained so existing callers keep compiling. R12 no longer refuses a region as a whole, so
        /// these are always empty: a region is not "unsupported" any more, and the shape of a region
        /// never blocks a tile.
        /// </summary>
        public IReadOnlyList<RaisedRegion> UnsupportedRegions => System.Array.Empty<RaisedRegion>();

        /// <summary>Retained for compatibility; always empty for the same reason as above.</summary>
        public IReadOnlyList<string> UnsupportedDiagnostics => System.Array.Empty<string>();

        /// <summary>Retained for compatibility; always empty for the same reason as above.</summary>
        public IReadOnlyList<RaisedRegion> SupportedRegions => Regions;

        /// <summary>
        /// Builds the whole projection. Never throws for a shape; only a null argument or an unusable
        /// composition set is a programming error.
        /// </summary>
        public static RaisedVisualPlan Build(
            TerrainGridData grid,
            AuthorHillsCompositionSet compositionSet)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (compositionSet == null)
            {
                throw new ArgumentNullException(nameof(compositionSet));
            }

            var tiles = new List<HillVisualTile>();
            AuthorHillsLocalResolver.Report report =
                AuthorHillsLocalResolver.Resolve(grid, compositionSet, tiles);

            // Legacy region analysis. Reported, never consulted for eligibility.
            List<RaisedRegion> regions = RaisedRegionAnalyzer.Analyze(grid);

            int visualized = 0;
            var seen = new HashSet<Vector3Int>();
            foreach (HillVisualTile tile in tiles)
            {
                if (RaisedNeighborResolver.IsRaised(grid, tile.VisualPosition.x, tile.VisualPosition.y))
                {
                    if (seen.Add(tile.VisualPosition))
                    {
                        visualized++;
                    }
                }
            }

            return new RaisedVisualPlan(
                tiles,
                regions,
                report.Diagnostics,
                report.RaisedCells,
                visualized,
                report.TilesOutsideLogicalMask);
        }

        /// <summary>One-line summary for the editor status bar and for diagnostics.</summary>
        public string Describe()
        {
            string text =
                $"Raised cells {RaisedCellCount}; {VisualizedRaisedCells} of them drawn with author "
                + $"Hills, {RaisedCellCount - VisualizedRaisedCells} without a hill tile; "
                + $"{Tiles.Count} hill visual tiles, {TilesOutsideLogicalMask} drawn south of their own "
                + $"logical mask; {Regions.Count} connected regions; "
                + $"{VisualDiagnostics.Count} visual diagnostics. "
                + "All Raised logical data is preserved.";

            foreach (RaisedVisualDiagnostic d in VisualDiagnostics)
            {
                text += $" | {d}";
            }

            return text;
        }
    }
}
