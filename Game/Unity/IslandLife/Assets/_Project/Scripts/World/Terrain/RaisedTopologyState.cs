using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// Which part of the author vertical Hills stack a logical cell is drawn as.
    /// IL-WORLD-004S-R12.
    ///
    /// This is a structural term, not a Sprite. It says "this cell is the rounded top of a plateau"
    /// or "this cell is the bottom soil row", and nothing about which sheet cell that becomes.
    /// </summary>
    public enum RaisedSurfaceRole
    {
        /// <summary>The author rounded top edge, Hills row r0.</summary>
        TOP_SURFACE = 0,

        /// <summary>An interior surface row, Hills row r1.</summary>
        MIDDLE_SURFACE = 1,

        /// <summary>The author front cliff, Hills row r2.</summary>
        FRONT_CLIFF = 2,

        /// <summary>The author second front row, Hills row r3.</summary>
        SECOND_FRONT_CLIFF = 3,
    }

    /// <summary>
    /// The local topology of one logical Raised cell, and the visual role that follows from it.
    /// IL-WORLD-004S-R12.
    ///
    /// This is deliberately NOT a sprite and NOT a sheet coordinate. It answers structural questions
    /// only: which sides are open, is this a one-wide cell, is this an isolated cell, is this the
    /// bottom of a notch, how thick is the plateau here, and which row of the author vertical stack
    /// does this cell become.
    ///
    /// Turning a role into author art is <see cref="AuthorHillsLocalResolver"/> job, and it uses only
    /// the author own slices in their original orientation.
    /// </summary>
    public readonly struct RaisedTopologyState
    {
        private RaisedTopologyState(
            TerrainNeighborMask rawMask,
            TerrainNeighborMask canonicalMask,
            bool connectedNorth,
            bool connectedSouth,
            bool connectedEast,
            bool connectedWest,
            int runDepth,
            int offsetFromRunBottom,
            bool northIsEnclosedVoid,
            bool eastIsEnclosedVoid,
            bool westIsEnclosedVoid,
            RaisedSurfaceRole role,
            HillColumnSlot slot)
        {
            RawMask = rawMask;
            CanonicalMask = canonicalMask;
            ConnectedNorth = connectedNorth;
            ConnectedSouth = connectedSouth;
            ConnectedEast = connectedEast;
            ConnectedWest = connectedWest;
            RunDepth = runDepth;
            OffsetFromRunBottom = offsetFromRunBottom;
            NorthIsEnclosedVoid = northIsEnclosedVoid;
            EastIsEnclosedVoid = eastIsEnclosedVoid;
            WestIsEnclosedVoid = westIsEnclosedVoid;
            Role = role;
            Slot = slot;
        }

        /// <summary>The raw 8-neighbour mask straight from the logical Raised mask.</summary>
        public TerrainNeighborMask RawMask { get; }

        /// <summary>The canonical mask from the shared normalizer.</summary>
        public TerrainNeighborMask CanonicalMask { get; }

        public bool ConnectedNorth { get; }

        public bool ConnectedSouth { get; }

        public bool ConnectedEast { get; }

        public bool ConnectedWest { get; }

        /// <summary>Thickness of the plateau in this column.</summary>
        public int RunDepth { get; }

        /// <summary>0 for the lowest cell of the run, RunDepth-1 for the highest.</summary>
        public int OffsetFromRunBottom { get; }

        /// <summary>True when the cell directly north is a single-cell hole.</summary>
        public bool NorthIsEnclosedVoid { get; }

        /// <summary>True when the cell directly east is a single-cell hole.</summary>
        public bool EastIsEnclosedVoid { get; }

        /// <summary>True when the cell directly west is a single-cell hole.</summary>
        public bool WestIsEnclosedVoid { get; }

        /// <summary>Which row of the author vertical stack this cell becomes.</summary>
        public RaisedSurfaceRole Role { get; }

        /// <summary>Which horizontal slot of that row.</summary>
        public HillColumnSlot Slot { get; }

        /// <summary>
        /// Solid means Raised, or an enclosed hole. A hole is an INTERIOR boundary, so ground continues
        /// across it and it must never be mistaken for open sky.
        /// </summary>
        public bool SolidNorth => ConnectedNorth || NorthIsEnclosedVoid;

        public bool SolidSouth => ConnectedSouth;

        public bool SolidEast => ConnectedEast || EastIsEnclosedVoid;

        public bool SolidWest => ConnectedWest || WestIsEnclosedVoid;

        public bool ExposedNorth => !SolidNorth;

        public bool ExposedSouth => !SolidSouth;

        public bool ExposedEast => !SolidEast;

        public bool ExposedWest => !SolidWest;

        /// <summary>One cell wide at this row. This is the author c3 column case, never a two-wide run.</summary>
        public bool IsNarrow => ExposedEast && ExposedWest;

        /// <summary>Open on all four sides: an isolated single cell.</summary>
        public bool IsIsolated => ExposedNorth && ExposedSouth && ExposedEast && ExposedWest;

        /// <summary>
        /// Open north and south while still joined east and west: the bottom of a notch, a U, or the
        /// bridge under the hole of a ring. The hole itself stays Normal and is never filled.
        /// </summary>
        public bool IsNotchCell => ExposedNorth && ExposedSouth && SolidEast && SolidWest;

        /// <summary>
        /// True when this cell draws its own front cliff inside the logical mask rather than one row
        /// south of it.
        /// </summary>
        public bool DrawsFrontCliffInMask => ExposedSouth
            && Role >= RaisedSurfaceRole.FRONT_CLIFF;

        /// <summary>True when a front cliff tile must additionally be drawn one row south.</summary>
        public bool DrawsFrontCliffBelow => ExposedSouth
            && Role < RaisedSurfaceRole.FRONT_CLIFF;

        /// <summary>
        /// The topology of a front-cliff tile drawn one row SOUTH of its own logical cell, because a
        /// plateau thinner than three cells has no room for the cliff inside its mask.
        ///
        /// The slot is inherited from the cell the cliff belongs to, so a left-terminated column gets a
        /// left-terminated cliff. That inheritance is what preserves the R6 proven front corners exactly.
        /// </summary>
        public static RaisedTopologyState ForFrontCliffBelow(
            RaisedTopologyState source,
            TerrainGridData grid,
            int x,
            int cliffY)
        {
            TerrainNeighborMask raw = RaisedNeighborResolver.ResolveRaisedMask(grid, x, cliffY);
            return new RaisedTopologyState(
                raw,
                RaisedNeighborResolver.Normalize(raw),
                source.ConnectedNorth,
                source.ConnectedSouth,
                source.ConnectedEast,
                source.ConnectedWest,
                1,
                0,
                false,
                false,
                false,
                RaisedSurfaceRole.FRONT_CLIFF,
                source.Slot);
        }

        /// <summary>Reads the local topology of one logical cell. Never writes to the grid.</summary>
        public static RaisedTopologyState Resolve(TerrainGridData grid, int x, int y)
        {
            TerrainNeighborMask raw = RaisedNeighborResolver.ResolveRaisedMask(grid, x, y);
            TerrainNeighborMask canonical = RaisedNeighborResolver.Normalize(raw);

            bool n = (raw & TerrainNeighborMask.North) != 0;
            bool s = (raw & TerrainNeighborMask.South) != 0;
            bool e = (raw & TerrainNeighborMask.East) != 0;
            bool w = (raw & TerrainNeighborMask.West) != 0;

            int runDepth = RaisedNeighborResolver.MeasureRunDepth(grid, x, y);
            int runBottom = RaisedNeighborResolver.MeasureRunBottom(grid, x, y);
            int offset = y - runBottom;

            bool northVoid = IsEnclosedVoid(grid, x, y + 1);
            bool eastVoid = IsEnclosedVoid(grid, x + 1, y);
            bool westVoid = IsEnclosedVoid(grid, x - 1, y);

            HillColumnSlot slot = SlotFor(w || westVoid, e || eastVoid);
            RaisedSurfaceRole role = RoleFor(
                n || northVoid, !s, runDepth, offset, northVoid, slot == HillColumnSlot.NARROW);

            return new RaisedTopologyState(
                raw, canonical, n, s, e, w, runDepth, offset,
                northVoid, eastVoid, westVoid, role, slot);
        }

        /// <summary>
        /// The horizontal slot. Left and right terminals come straight from the open sides, and a cell
        /// open on BOTH sides is the author one-wide column, never a two-wide run. A two-wide run has
        /// one open side per cell, so each end gets a terminal and nothing butts two terminals together
        /// except at the proved rows.
        /// </summary>
        private static HillColumnSlot SlotFor(bool solidWest, bool solidEast)
        {
            bool openWest = !solidWest;
            bool openEast = !solidEast;

            if (openWest && openEast)
            {
                return HillColumnSlot.NARROW;
            }

            if (openWest)
            {
                return HillColumnSlot.LEFT_TERMINAL;
            }

            return openEast ? HillColumnSlot.RIGHT_TERMINAL : HillColumnSlot.BODY;
        }

        /// <summary>
        /// The vertical role. Every branch is traceable to a composition R6 or R9 already proved:
        ///
        ///   depth 1, wide        -> MIDDLE here, front cliff one row SOUTH.
        ///                           R8 proved r1 over r2 at widths 3, 5 and 8 and refused to skip r1,
        ///                           so a one-deep band never gets a cap directly above a cliff.
        ///   depth 1, one wide    -> TOP, the author c3 cap, front cliff one row SOUTH.
        ///   depth 2              -> TOP, MIDDLE, front cliff one row SOUTH.
        ///   depth 3              -> TOP, MIDDLE, FRONT_CLIFF inside the mask. The author r0..r2.
        ///   depth 4 or more      -> TOP, MIDDLE x (depth-3), FRONT_CLIFF, SECOND_FRONT_CLIFF.
        ///                           The author own r0..r3 stack, which R4 reconstructed 1:1.
        ///
        /// Region height is nowhere in this decision, which is what removes the HEIGHT_NOT_PROVEN
        /// refusal for 3x4, 5x4, 8x4 and anything taller.
        /// </summary>
        private static RaisedSurfaceRole RoleFor(
            bool solidNorth,
            bool southExposed,
            int runDepth,
            int offsetFromRunBottom,
            bool northIsEnclosedVoid,
            bool isNarrow)
        {
            // A front wall belongs inside the mask whenever there is ground above it to sit under:
            // either the plateau is at least three cells thick (the proven r0/r1/r2 stack), or the cell
            // above is an enclosed hole, in which case the ground continues all the way round and the
            // wall must not drop a row and leave a notch in the front of a ring.
            bool frontInMask = southExposed && solidNorth && (runDepth >= 3 || northIsEnclosedVoid);

            if (frontInMask)
            {
                return runDepth >= 4 && offsetFromRunBottom == 0
                    ? RaisedSurfaceRole.SECOND_FRONT_CLIFF
                    : RaisedSurfaceRole.FRONT_CLIFF;
            }

            if (runDepth >= 4)
            {
                if (offsetFromRunBottom == 0)
                {
                    return RaisedSurfaceRole.SECOND_FRONT_CLIFF;
                }

                if (offsetFromRunBottom == 1)
                {
                    return RaisedSurfaceRole.FRONT_CLIFF;
                }

                return offsetFromRunBottom == runDepth - 1
                    ? RaisedSurfaceRole.TOP_SURFACE
                    : RaisedSurfaceRole.MIDDLE_SURFACE;
            }

            if (runDepth == 3)
            {
                switch (offsetFromRunBottom)
                {
                    case 0:
                        return RaisedSurfaceRole.FRONT_CLIFF;
                    case 2:
                        return RaisedSurfaceRole.TOP_SURFACE;
                    default:
                        return RaisedSurfaceRole.MIDDLE_SURFACE;
                }
            }

            if (runDepth == 2)
            {
                return offsetFromRunBottom == 1
                    ? RaisedSurfaceRole.TOP_SURFACE
                    : RaisedSurfaceRole.MIDDLE_SURFACE;
            }

            // Depth 1. The cell is at once the top and the front of its column, so it has room for
            // exactly one tile and the cliff must go below the mask.
            //
            // IL-WORLD-004S-R13. This branch used to return MIDDLE for a wide run and TOP only for the
            // one-wide column, and the user proved that wrong on screen: two isolated Raised cells each
            // rendered as a properly closed little plateau using the author rounded cap, but painting
            // the cell between them turned the pair into a band whose top edge used the SQUARE body
            // row, so the north exterior boundary vanished and the plateau read as a U shaped trough.
            //
            // The cause is exactly what must never happen: the north exterior disappeared not because a
            // neighbour had become Raised, but because the cell had stopped being isolated. The fix is
            // the exposed-neighbour invariant, not a special case for horizontal runs. A cell with no
            // Raised neighbour to its north HAS a north exterior boundary, and the only author art that
            // carries one is the cap row r0. So every north-exposed cell gets the cap, one-wide or wide,
            // and the horizontal slot alone decides which slice is used.
            return RaisedSurfaceRole.TOP_SURFACE;
        }

        /// <summary>
        /// A single-cell hole: not Raised, but Raised on all four orthogonal sides. Only single-cell
        /// holes are recognised this way. A wider cavity simply reads as open ground, which costs a
        /// cosmetic step at its front and never touches the logical data.
        /// </summary>
        private static bool IsEnclosedVoid(TerrainGridData grid, int x, int y)
        {
            if (!grid.IsInside(x, y) || grid.GetElevation(x, y) == ElevationLevel.Raised)
            {
                return false;
            }

            return RaisedNeighborResolver.IsRaised(grid, x - 1, y)
                && RaisedNeighborResolver.IsRaised(grid, x + 1, y)
                && RaisedNeighborResolver.IsRaised(grid, x, y - 1)
                && RaisedNeighborResolver.IsRaised(grid, x, y + 1);
        }

        public override string ToString()
        {
            return $"raw={RawMask} canon={CanonicalMask} "
                + $"open(N{(ExposedNorth ? 1 : 0)} S{(ExposedSouth ? 1 : 0)} "
                + $"E{(ExposedEast ? 1 : 0)} W{(ExposedWest ? 1 : 0)}) "
                + $"depth={RunDepth} off={OffsetFromRunBottom} {Role}/{Slot}"
                + (IsNarrow ? " NARROW" : string.Empty)
                + (IsIsolated ? " ISOLATED" : string.Empty)
                + (IsNotchCell ? " NOTCH" : string.Empty)
                + (NorthIsEnclosedVoid ? " HOLE-N" : string.Empty);
        }
    }
}
