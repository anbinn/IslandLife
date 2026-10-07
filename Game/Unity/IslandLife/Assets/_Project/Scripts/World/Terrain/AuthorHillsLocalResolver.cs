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

                    // The slot must be settled BEFORE the Sprite is chosen, because the slot decides which
                    // author slice is used. IL-WORLD-004S-R14.
                    HillColumnSlot selfSlot = topology.Slot;
                    if (topology.DrawsFrontCliffInMask)
                    {
                        // A front cliff that sits inside its own mask still ends wherever the cliff run
                        // ends, so its slot comes from the run extent too.
                        selfSlot = CliffSlotFor(grid, x, y, selfSlot, y);
                    }

                    // The cell's own tile.
                    Sprite sprite = PickSprite(
                        compositionSet, topology, selfSlot, diagnostics, x, y);
                    if (sprite == null)
                    {
                        continue;
                    }

                    if (!TryClaim(
                            claimed, new Vector3Int(x, y, 0), topology, sprite, selfSlot, diagnostics))
                    {
                        continue;
                    }

                    output.Add(new HillVisualTile(
                        new Vector3Int(x, y, 0),
                        false,
                        sprite,
                        TileRow(topology),
                        selfSlot));

                    // The front cliff, when the plateau is too thin to hold it inside its own mask.
                    if (topology.DrawsFrontCliffBelow)
                    {
                        var cliffPosition = new Vector3Int(x, y - 1, 0);
                        RaisedTopologyState cliffTopology =
                            RaisedTopologyState.ForFrontCliffBelow(topology, grid, x, y - 1);

                        HillColumnSlot cliffSlot = CliffSlotFor(
                            grid, x, y, cliffTopology.Slot, y - 1);

                        Sprite cliffSprite = PickSprite(
                            compositionSet, cliffTopology, cliffSlot, diagnostics, x, y - 1);
                        if (cliffSprite == null)
                        {
                            continue;
                        }

                        if (!TryClaim(
                                claimed, cliffPosition, cliffTopology, cliffSprite, cliffSlot,
                                diagnostics))
                        {
                            continue;
                        }

                        output.Add(new HillVisualTile(
                            cliffPosition,
                            true,
                            cliffSprite,
                            TileRow(cliffTopology),
                            CliffSlotFor(grid, x, y, cliffTopology.Slot, y - 1)));
                        outside++;
                    }
                }
            }

            return new Report(raisedCells, output.Count, outside, diagnostics);
        }

        /// <summary>
        /// The slot of one front-cliff tile, decided by how far the cliff RUN that tile belongs to
        /// actually extends.
        ///
        /// IL-WORLD-004S-R14. This is the inner-corner fix, and it is derived purely from local
        /// adjacency. A cliff exists for every Raised cell whose south is open, so where a vertical
        /// branch meets a horizontal front boundary the branch cell has NO cliff and the horizontal run
        /// simply stops. Previously the tile at that last column still asked its own owner cell for a
        /// slot, and because the owner had Raised neighbours on both sides it answered BODY: a straight
        /// cliff band ran head-on into the branch and left the square gap the user reported.
        ///
        /// Instead the run is walked west and east through neighbours that emit a cliff onto the SAME
        /// visual row. The two ends of that run are the author terminal pieces, so the horizontal front
        /// boundary leaves the straight band and turns into the side of the branch. No shape name is
        /// involved, no T is detected, and the same walk handles U, notch, staircase and anything else
        /// with the same local topology.
        ///
        /// A run only one cell wide keeps the slot it inherited, which is what preserves the author
        /// one-wide c3 column and every proven narrow composition byte for byte.
        /// </summary>
        private static HillColumnSlot CliffSlotFor(
            TerrainGridData grid,
            int x,
            int y,
            HillColumnSlot fallback,
            int cliffVisualY)
        {
            int start = x;
            int end = x;

            while (RunContinuesThrough(grid, start - 1, y, cliffVisualY))
            {
                start--;
            }

            while (RunContinuesThrough(grid, end + 1, y, cliffVisualY))
            {
                end++;
            }

            if (end - start + 1 < 2)
            {
                return fallback;
            }

            if (x == start)
            {
                return HillColumnSlot.LEFT_TERMINAL;
            }

            return x == end ? HillColumnSlot.RIGHT_TERMINAL : HillColumnSlot.BODY;
        }

        private static bool EmitsAnyCliff(TerrainGridData grid, int x, int y)
        {
            return CliffVisualRow(grid, x, y) != int.MinValue;
        }

        /// <summary>
        /// Whether the cliff run continues through this cell on the same visual row.
        ///
        /// IL-WORLD-004S-R20. A cell normally joins the run only when it emits a cliff of its own on
        /// that row. An author CORNER is the exception and it is load bearing: a top corner has a leg
        /// below it, so its own south is NOT exposed, it emits no cliff, and the walk used to stop
        /// there. The body cell immediately inside a corner was therefore made a run END and drawn as
        /// the author's r3c0 or r3c2 terminal instead of the author's r3c1 BODY, which put an
        /// unconfirmed piece in the middle of a plain straight top run.
        ///
        /// A corner is a whole-cell composition that occupies its own cell in the same visual row, so
        /// the run genuinely does continue through it. The corner's own sprite is chosen from its ROLE
        /// in <see cref="PickSprite"/> and never from this walk, so nothing here can overwrite it.
        ///
        /// This is a no-op for the two R19 front corners, which already emit a cliff on that row and so
        /// already extended the run; it is measured by R19 staying at 33 PASS / 0 FAIL.
        /// </summary>
        private static bool RunContinuesThrough(TerrainGridData grid, int x, int y, int cliffVisualY)
        {
            if (EmitsAnyCliff(grid, x, y))
            {
                return CliffVisualRow(grid, x, y) == cliffVisualY;
            }

            if (!RaisedNeighborResolver.IsRaised(grid, x, y))
            {
                return false;
            }

            RaisedTopologyState t = RaisedTopologyState.Resolve(grid, x, y);
            return t.IsLeftTopCorner || t.IsRightTopCorner
                || t.IsLeftCorner || t.IsRightCorner;
        }

        /// <summary>
        /// The visual row this cell's front cliff lands on, or int.MinValue when it emits no cliff.
        /// Read straight from the same topology the projection uses, so the run walk can never disagree
        /// with what is actually emitted.
        /// </summary>
        private static int CliffVisualRow(TerrainGridData grid, int x, int y)
        {
            if (!RaisedNeighborResolver.IsRaised(grid, x, y))
            {
                return int.MinValue;
            }

            RaisedTopologyState topology = RaisedTopologyState.Resolve(grid, x, y);
            if (topology.DrawsFrontCliffInMask)
            {
                return y;
            }

            return topology.DrawsFrontCliffBelow ? y - 1 : int.MinValue;
        }
        private static Sprite PickSprite(
            AuthorHillsCompositionSet set,
            RaisedTopologyState topology,
            HillColumnSlot slot,
            List<RaisedVisualDiagnostic> diagnostics,
            int x,
            int y)
        {
            Sprite sprite;

            // IL-WORLD-004S-R19. The author corners are whole-cell compositions addressed directly by
            // role, checked BEFORE the slot dispatch. A corner cell is frequently NARROW or a
            // TERMINAL by slot, and letting the slot choose would substitute r3c3 or an r2 terminal
            // for the author's real corner piece.
            if (topology.Role == RaisedSurfaceRole.LEFT_CORNER)
            {
                sprite = set.GetLeftCorner();
            }
            else if (topology.Role == RaisedSurfaceRole.RIGHT_CORNER)
            {
                sprite = set.GetRightCorner();
            }
            else if (topology.Role == RaisedSurfaceRole.LEFT_TOP_CORNER)
            {
                sprite = set.GetLeftTopCorner();
            }
            else if (topology.Role == RaisedSurfaceRole.RIGHT_TOP_CORNER)
            {
                sprite = set.GetRightTopCorner();
            }
            else if (slot == HillColumnSlot.NARROW)
            {
                sprite = set.GetNarrow(NarrowOffset(topology.Role));
            }
            else
            {
                sprite = set.GetWide(ToCompositionRow(topology.Role), slot);
            }

            if (sprite == null)
            {
                diagnostics.Add(new RaisedVisualDiagnostic(
                    RaisedVisualDiagnosticCodes.MissingAuthorPrimitive,
                    new Vector3Int(x, y, 0),
                    $"no author slice for {topology.Role}/{slot}"));
            }

            return sprite;
        }

        private static bool TryClaim(
            Dictionary<Vector3Int, HillVisualTile> claimed,
            Vector3Int position,
            RaisedTopologyState topology,
            Sprite sprite,
            HillColumnSlot slot,
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
                position, false, sprite, TileRow(topology), slot);
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
            // IL-WORLD-004S-R19. Both author corners live on sheet row r3, so they are tagged
            // SECOND_FRONT_CLIFF even though a narrow corner cell would otherwise fall through to the
            // historical narrow tagging below. The tag now matches the slice actually emitted.
            if (topology.Role == RaisedSurfaceRole.LEFT_CORNER
                || topology.Role == RaisedSurfaceRole.RIGHT_CORNER)
            {
                return HillCompositionRow.SECOND_FRONT_CLIFF;
            }

            // IL-WORLD-004S-R20. The two TOP corners are the author's row r0 cells, so they are tagged
            // TOP_SURFACE. This matters for more than bookkeeping: a top corner cell is never given a
            // slot, and without this it would fall through to the narrow tagging branch and be
            // reported on the wrong row.
            if (topology.Role == RaisedSurfaceRole.LEFT_TOP_CORNER
                || topology.Role == RaisedSurfaceRole.RIGHT_TOP_CORNER)
            {
                return HillCompositionRow.TOP_SURFACE;
            }

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
