using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// The complete Raised visual projection for one grid: every author visual tile that will be
    /// drawn, plus a plain-text reason for every region that will not be drawn. IL-WORLD-004S-R6.
    ///
    /// This is pure projection. Building it does not touch the logical Raised mask, does not touch
    /// TerrainData, and cannot throw for an unsupported shape: an unsupported region produces a
    /// diagnostic line and zero tiles, which is what keeps the map editable.
    /// </summary>
    public sealed class RaisedVisualPlan
    {
        private RaisedVisualPlan(
            List<HillVisualTile> tiles,
            List<RaisedRegion> supportedRegions,
            List<RaisedRegion> unsupportedRegions,
            List<string> unsupportedDiagnostics)
        {
            Tiles = tiles;
            SupportedRegions = supportedRegions;
            UnsupportedRegions = unsupportedRegions;
            UnsupportedDiagnostics = unsupportedDiagnostics;
        }

        /// <summary>Author visual tiles, in emission order. Never contains a guess.</summary>
        public IReadOnlyList<HillVisualTile> Tiles { get; }

        public IReadOnlyList<RaisedRegion> SupportedRegions { get; }

        /// <summary>One line per unsupported region: bounds, size, cell count and the reason.</summary>
        public IReadOnlyList<string> UnsupportedDiagnostics { get; }

        /// <summary>
        /// The same unsupported regions as objects, with their real bounds. The Scene View
        /// diagnostic needs coordinates to draw an outline; a preformatted string cannot supply
        /// those. IL-WORLD-004S-R9.
        /// </summary>
        public IReadOnlyList<RaisedRegion> UnsupportedRegions { get; }

        public int RaisedCellCount { get; private set; }

        /// <summary>Tiles drawn south of their own region's logical mask. Proven necessary by R5.</summary>
        public int TilesOutsideLogicalMask { get; private set; }

        /// <summary>
        /// Builds the whole projection. Never throws for an unsupported shape; only a null argument
        /// or a missing composition set is a programming error.
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

            List<RaisedRegion> regions = RaisedRegionAnalyzer.Analyze(grid);

            var tiles = new List<HillVisualTile>();
            var supported = new List<RaisedRegion>();
            var unsupportedRegions = new List<RaisedRegion>();
            var unsupported = new List<string>();

            foreach (RaisedRegion region in regions)
            {
                if (!AuthorHillsCompositionResolver.TryResolve(
                        region, compositionSet, tiles, out string failure))
                {
                    unsupportedRegions.Add(region);
                    unsupported.Add(region + "  ::  " + failure);
                    continue;
                }

                supported.Add(region);
            }

            int outsideMask = 0;
            foreach (HillVisualTile tile in tiles)
            {
                if (tile.OutsideLogicalMask)
                {
                    outsideMask++;
                }
            }

            return new RaisedVisualPlan(tiles, supported, unsupportedRegions, unsupported)
            {
                RaisedCellCount = RaisedRegionAnalyzer.CountRaisedCells(grid),
                TilesOutsideLogicalMask = outsideMask,
            };
        }

        /// <summary>One-line summary for the editor status bar and for diagnostics.</summary>
        public string Describe()
        {
            string text =
                $"Raised cells {RaisedCellCount} in {SupportedRegions.Count} supported + "
                + $"{UnsupportedDiagnostics.Count} unsupported regions; "
                + $"{Tiles.Count} hill visual tiles, {TilesOutsideLogicalMask} drawn south of "
                + "their own logical mask.";

            foreach (string line in UnsupportedDiagnostics)
            {
                text += " | UNSUPPORTED: " + line;
            }

            return text;
        }
    }
}