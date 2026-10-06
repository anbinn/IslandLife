using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// The freeform Raised visual projection. IL-WORLD-004S-R12.
    ///
    /// This REPLACES region-level eligibility. It no longer asks "what shape is this connected
    /// region", so a 201-cell irregular mass, an L, a T, a U, a ring with a hole, a two-wide run and
    /// an eight-by-four plateau all project from their own local topology instead of being refused as
    /// a whole.
    ///
    /// The pipeline is:
    ///
    ///     logical Raised mask
    ///       -> per-cell 8-neighbour Raised topology   (RaisedNeighborResolver)
    ///       -> canonical topology                      (the shared TerrainMaskNormalizer)
    ///       -> local structural role                  (RaisedTopologyState, no art involved)
    ///       -> author Hills primitives                (this class)
    ///       -> HillVisualTile
    ///
    /// Invariants this class guarantees, all asserted by the R12 suite:
    ///   - it only READS the logical elevation; TerrainData is never mutated;
    ///   - every emitted tile is one of the author's own Hills slices, in its original orientation;
    ///     nothing is mirrored, rotated, stretched or resampled, and nothing is generated;
    ///   - no fallback and no nearest match: if a primitive is missing the cell is reported and no
    ///     tile is invented;
    ///   - emission order is fixed and a visual coordinate can never be claimed twice, so the output
    ///     is deterministic regardless of traversal order.
    /// </summary>
    public static class AuthorHillsLocalResolver
    {
        /// <summary>Outcome of one freeform projection.</summary>
        public sealed class Report
        {
            internal Report(
                int raisedCells,
                int emittedTiles,
                int tilesOutsideLogicalMask,
                List<RaisedVisualDiagnostic> diagnostics)
            {
                RaisedCells = raisedCells;
                EmittedTiles = emittedTiles;
                TilesOutsideLogicalMask = tilesOutsideLogicalMask;
                Diagnostics = diagnostics;
            }

            /// <summary>Logical Raised cells seen. This is the number that must never change.</summary>
            public int RaisedCells { get; }

            public int EmittedTiles { get; }

            /// <summary>Tiles drawn south of their own logical mask. Proven necessary by R5.</summary>
            public int TilesOutsideLogicalMask { get; }

            /// <summary>
            /// Only genuine implementation problems, never a shape verdict. Allowed codes are
            /// MISSING_AUTHOR_PRIMITIVE, MISSING_CANONICAL_TOPOLOGY, VISUAL_CONFLICT and
            /// INVALID_ASSET_REFERENCE.
            /// </summary>
            public List<RaisedVisualDiagnostic> Diagnostics { get; }

            public bool HasDiagnostics => Diagnostics.Count > 0;
        }

        /// <summary>
        /// Every canonical state reachable through the SHARED normalizer, derived at runtime from
        /// TerrainMaskNormalizer over all 256 raw masks. This is not a second table: it is the
        /// existing normalizer's own output, used only to detect an unreachable topology.
        /// </summary>
        private static readonly HashSet<TerrainNeighborMask> CanonicalStates = BuildCanonicalStates();

        private static HashSet<TerrainNeighborMask> BuildCanonicalStates()
        {
            var set = new HashSet<TerrainNeighborMask>();
            for (int raw = 0; raw <= byte.MaxValue; raw++)
            {
                set.Add(RaisedNeighborResolver.Normalize((TerrainNeighborMask)raw));
            }

            return set;
        }

        /// <summary>
        /// Projects the whole grid. Never throws for a shape; a shape simply produces whatever its own
        /// local topology says it should.
        /// </summary>
        public static Report Resolve(
            TerrainGridData grid,
            AuthorHillsCompositionSet compositionSet,
            List<HillVisualTile> output)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (compositionSet == null)
            {
                throw new ArgumentNullException(nameof(compositionSet));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            var diagnostics = new List<RaisedVisualDiagnostic>();
            var claimed = new Dictionary<Vector3Int, HillVisualTile>();

            int raisedCells = RaisedRegionAnalyzer.CountRaisedCells(grid);
            int outside = 0;

            if (!compositionSet.IsComplete())
            {
                diagnostics.Add(new RaisedVisualDiagnostic(
                    RaisedVisualDiagnosticCodes.InvalidAssetReference,
                    new Vector3Int(grid.OriginX, grid.OriginY, 0),
                    $"composition set incomplete: {compositionSet.DescribeMissingSlots()}"));
            }

            // Fixed traversal: north to south, then west to east. The result does not depend on this
            // order because a visual coordinate can only be claimed once, and that is checked below.
            for (int localY = grid.Height - 1; localY >= 0; localY--)
            {
                for (int localX = 0; localX < grid.Width; localX++)
                {
                    int x = grid.OriginX + localX;
                    int y = grid.OriginY + localY;

                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    RaisedTopologyState topology = RaisedTopologyState.Resolve(grid, x, y);

                    if (!CanonicalStates.Contains(topology.CanonicalMask))
                    {
                        diagnostics.Add(new RaisedVisualDiagnostic(
                            RaisedVisualDiagnosticCodes.MissingCanonicalTopology,
                            new Vector3Int(x, y, 0),
                            $"canonical mask {topology.CanonicalMask} has no projection rule"));
                        continue;
                    }

                    // The cell's own tile.
                    Sprite sprite = PickSprite(compositionSet, topology, diagnostics, x, y);
                    if (sprite == null)
                    {
                        continue;
                    }

                    if (!TryClaim(claimed, new Vector3Int(x, y, 0), topology, sprite, diagnostics))
                    {
                        continue;
                    }

                    output.Add(new HillVisualTile(
                        new Vector3Int(x, y, 0),
                        false,
                        sprite,
                        TileRow(topology),
                        topology.Slot));

                    // The front cliff, when the plateau is too thin to hold it inside its own mask.
                    if (topology.DrawsFrontCliffBelow)
                    {
                        var cliffPosition = new Vector3Int(x, y - 1, 0);
                        RaisedTopologyState cliffTopology =
                            RaisedTopologyState.ForFrontCliffBelow(topology, grid, x, y - 1);

                        Sprite cliffSprite =
                            PickSprite(compositionSet, cliffTopology, diagnostics, x, y - 1);
                        if (cliffSprite == null)
                        {
                            continue;
                        }

                        if (!TryClaim(
                                claimed, cliffPosition, cliffTopology, cliffSprite, diagnostics))
                        {
                            continue;
                        }

                        output.Add(new HillVisualTile(
                            cliffPosition,
                            true,
                            cliffSprite,
                            TileRow(cliffTopology),
                            cliffTopology.Slot));
                        outside++;
                    }
                }
            }

            return new Report(raisedCells, output.Count, outside, diagnostics);
        }

        private static Sprite PickSprite(
            AuthorHillsCompositionSet set,
            RaisedTopologyState topology,
            List<RaisedVisualDiagnostic> diagnostics,
            int x,
            int y)
        {
            Sprite sprite;
            if (topology.Slot == HillColumnSlot.NARROW)
            {
                sprite = set.GetNarrow(NarrowOffset(topology.Role));
            }
            else
            {
                sprite = set.GetWide(ToCompositionRow(topology.Role), topology.Slot);
            }

            if (sprite == null)
            {
                diagnostics.Add(new RaisedVisualDiagnostic(
                    RaisedVisualDiagnosticCodes.MissingAuthorPrimitive,
                    new Vector3Int(x, y, 0),
                    $"no author slice for {topology.Role}/{topology.Slot}"));
            }

            return sprite;
        }

        private static bool TryClaim(
            Dictionary<Vector3Int, HillVisualTile> claimed,
            Vector3Int position,
            RaisedTopologyState topology,
            Sprite sprite,
            List<RaisedVisualDiagnostic> diagnostics)
        {
            if (claimed.TryGetValue(position, out HillVisualTile existing))
            {
                diagnostics.Add(new RaisedVisualDiagnostic(
                    RaisedVisualDiagnosticCodes.VisualConflict,
                    position,
                    $"claimed by both {existing.Sprite.name} and {sprite.name}; {topology}"));
                return false;
            }

            claimed[position] = new HillVisualTile(
                position, false, sprite, TileRow(topology), topology.Slot);
            return true;
        }

        /// <summary>
        /// The composition-row tag written onto the emitted tile.
        ///
        /// For the author's one-wide c3 column this deliberately reproduces the historical tagging:
        /// HillCompositionRow only had three values when R6 shipped the narrow composition, so BOTH of
        /// its cliff rows were tagged FRONT_CLIFF even though the sprites are the author's distinct r2c3
        /// and r3c3. The topology still knows the difference - SECOND_FRONT_CLIFF is what
        /// RaisedTopologyState reports - but the emitted metadata matches the proven composition exactly,
        /// because IL-WORLD-004S-R18 makes that composition the oracle.
        /// </summary>
        private static HillCompositionRow TileRow(RaisedTopologyState topology)
        {
            if (topology.Slot != HillColumnSlot.NARROW)
            {
                return ToCompositionRow(topology.Role);
            }

            int offset = NarrowOffset(topology.Role);
            return offset < 2
                ? (offset == 0 ? HillCompositionRow.TOP_SURFACE : HillCompositionRow.MIDDLE_SURFACE)
                : HillCompositionRow.FRONT_CLIFF;
        }
        private static HillCompositionRow ToCompositionRow(RaisedSurfaceRole role)
        {
            switch (role)
            {
                case RaisedSurfaceRole.TOP_SURFACE:
                    return HillCompositionRow.TOP_SURFACE;
                case RaisedSurfaceRole.MIDDLE_SURFACE:
                    return HillCompositionRow.MIDDLE_SURFACE;
                case RaisedSurfaceRole.FRONT_CLIFF:
                    return HillCompositionRow.FRONT_CLIFF;
                default:
                    return HillCompositionRow.SECOND_FRONT_CLIFF;
            }
        }

        private static int NarrowOffset(RaisedSurfaceRole role)
        {
            return role == RaisedSurfaceRole.TOP_SURFACE ? 0
                : role == RaisedSurfaceRole.MIDDLE_SURFACE ? 1
                : role == RaisedSurfaceRole.FRONT_CLIFF ? 2
                : 3;
        }
    }
}
