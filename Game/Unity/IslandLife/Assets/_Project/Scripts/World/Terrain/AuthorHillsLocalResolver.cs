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
    /// IL-WORLD-004S-R23B: THE THREE LAYERS ARE NOW EXPLICIT.
    ///
    ///   LAYER A  local semantics.   RaisedTopologyState.Resolve reads this cell's own 3x3 and returns
    ///                               AuthorRole + AuthorSlot. Strictly 3x3: no x+/-2, no y+/-2, no run
    ///                               length, no walk, no lookahead.
    ///   LAYER B  author component.  THIS CLASS. It is a pure dispatch table: role -> the PM-locked
    ///                               author Hills slice. It may not widen, narrow or re-derive either
    ///                               Layer A output, and it no longer does. CliffSlotFor and its run
    ///                               walk are deleted, so the slot reaches Layer B untouched.
    ///   LAYER C  repetition/layout. RunDepth and OffsetFromRunBottom still exist on the state struct,
    ///                               but they are read by NOTHING here and they decide no role, no slot
    ///                               and no sprite.
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

                    // IL-WORLD-004S-R23B, LAYER B. The slot is now taken STRAIGHT FROM LAYER A.
                    //
                    // CliffSlotFor used to overwrite it here by walking west and east until the cliff run
                    // stopped, which is the unbounded run walk the card forbids: it decided an author
                    // SLOT, and therefore an author SPRITE, from cells further than 3x3 away. It is gone,
                    // and with it RunContinuesThrough, EmitsAnyCliff and CliffVisualRow, which existed
                    // only to feed it.
                    //
                    // Layer A already reads the answer off the cell's own cardinals: east open and west
                    // solid is a LEFT_TERMINAL, both solid is a BODY, west open and east solid is a
                    // RIGHT_TERMINAL, and both open is the author's one-wide c3 column. Nothing here may
                    // widen that decision.
                    HillColumnSlot selfSlot = topology.Slot;

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

                    // IL-WORLD-004S-R23B. Reachable only while DrawsFrontCliffBelow can be true, which under the
                    // local Layer A it no longer can be: the straight ladder only returns a role BELOW
                    // FRONT_CLIFF (TOP_SURFACE or MIDDLE_SURFACE) for a cell whose SOUTH is solid, so
                    // ExposedSouth is false and the condition can never hold. The path is KEPT, not
                    // deleted, so the outside-mask invariant stays checkable rather than becoming an
                    // unreachable assumption, and it stays a measured 0.
                    if (topology.DrawsFrontCliffBelow)
                    {
                        var cliffPosition = new Vector3Int(x, y - 1, 0);
                        RaisedTopologyState cliffTopology =
                            RaisedTopologyState.ForFrontCliffBelow(topology, grid, x, y - 1);

                        HillColumnSlot cliffSlot = cliffTopology.Slot;

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
                            cliffSlot));
                        outside++;
                    }
                }
            }

            return new Report(raisedCells, output.Count, outside, diagnostics);
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
            else if (topology.Role == RaisedSurfaceRole.JUNCTION_VERTICAL_CONTINUATION)
            {
                // IL-WORLD-004S-R24. The user's screenshot-confirmed junction, LOCKED_USER_VISUAL_ORACLE.
                // This is the author's own r2c4 slice, in its original orientation; nothing is mirrored,
                // rotated, stretched, generated or nearest-matched to reach it.
                sprite = set.GetJunctionContinuation();
            }
            else if (topology.Role == RaisedSurfaceRole.CORNER_WITH_UPPER_CONTINUATION)
            {
                // R21_SUPERSEDED, IL-WORLD-004S-R23B. Unreachable: no rule produces this role, because
                // proving a junction needs cells beyond 3x3. Kept wired so the author slice stays
                // reachable-in-principle and IsComplete keeps holding. The SLICE is not lost - the same
                // author's r2c4 is now reached through JUNCTION_VERTICAL_CONTINUATION above, which proves
                // it from local topology alone.
                sprite = set.GetCornerWithUpperContinuation();
            }
            else if (topology.Role == RaisedSurfaceRole.CORNER_NO_UPPER_CONTINUATION)
            {
                // R21_SUPERSEDED, IL-WORLD-004S-R23B. Unreachable, same reason as above.
                sprite = set.GetCornerNoUpperContinuation();
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

            // IL-WORLD-004S-R21. The two junction components live on the author sheet at row r3 (r3c5)
            // and row r2 (r2c4), so each is tagged with its OWN row. Reporting them as one shared row
            // would hide which of the two locked pieces was actually chosen.
            if (topology.Role == RaisedSurfaceRole.CORNER_NO_UPPER_CONTINUATION)
            {
                return HillCompositionRow.SECOND_FRONT_CLIFF;
            }

            if (topology.Role == RaisedSurfaceRole.CORNER_WITH_UPPER_CONTINUATION)
            {
                return HillCompositionRow.FRONT_CLIFF;
            }

            // IL-WORLD-004S-R24. The junction continuation is the author's r2c4, so it is tagged with its
            // OWN row r2 and not lumped in with the unreachable R21 roles above. Reporting the row it
            // actually came from is the whole point of the tag.
            if (topology.Role == RaisedSurfaceRole.JUNCTION_VERTICAL_CONTINUATION)
            {
                return HillCompositionRow.FRONT_CLIFF;
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
