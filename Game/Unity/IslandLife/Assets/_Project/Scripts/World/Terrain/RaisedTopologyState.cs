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

        /// <summary>
        /// The author's LEFT corner, Hills r3c4: a vertical left boundary turning east into the
        /// horizontal front. IL-WORLD-004S-R19.
        ///
        /// A corner is a composition in its own right, so it gets its own role rather than a row and
        /// slot combination. It sits on a logical Raised cell and never displaces a cliff south of
        /// itself.
        /// </summary>
        LEFT_CORNER = 4,

        /// <summary>
        /// The author's RIGHT corner, Hills r3c7: the horizontal front turning north into a vertical
        /// right boundary. IL-WORLD-004S-R19.
        /// </summary>
        RIGHT_CORNER = 5,

        /// <summary>
        /// The author's LEFT TOP corner, Hills r0c4: a vertical left boundary that reaches the top and
        /// turns east into the horizontal top structure. IL-WORLD-004S-R20.
        ///
        /// "Top" here is the project's own Grammar Map position name, kept as PM named it. It is the
        /// cell on the north edge of the mass, so its own face is a north-facing one and its art is a
        /// row r0 piece.
        /// </summary>
        LEFT_TOP_CORNER = 6,

        /// <summary>
        /// The author's RIGHT TOP corner, Hills r0c7: the horizontal top structure reaching its right
        /// end and turning south into a vertical right boundary. IL-WORLD-004S-R20.
        /// </summary>
        RIGHT_TOP_CORNER = 7,
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
            bool southIsEnclosedVoid,
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
            SouthIsEnclosedVoid = southIsEnclosedVoid;
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

        /// <summary>
        /// True when the cell directly south is a single-cell hole.
        ///
        /// IL-WORLD-004S-R16C. This is the fourth member of a family that already had three members,
        /// and it was the missing one. A hole is an INTERIOR boundary, so ground continues across it
        /// and it must never be mistaken for open sky. The author never draws an interior face, exactly
        /// as it never draws an east or west face, and the real FirstIsland O sample proved the
        /// consequence of leaving it out: the cell above a one-cell hole dropped its front cliff one row
        /// south and painted a block of soil INSIDE the hole.
        /// </summary>
        public bool SouthIsEnclosedVoid { get; }

        /// <summary>Which row of the author vertical stack this cell becomes.</summary>
        public RaisedSurfaceRole Role { get; }

        /// <summary>Which horizontal slot of that row.</summary>
        public HillColumnSlot Slot { get; }

        /// <summary>
        /// True when this cell is the author's left corner, Hills r3c4. IL-WORLD-004S-R19.
        /// </summary>
        public bool IsLeftCorner => Role == RaisedSurfaceRole.LEFT_CORNER;

        /// <summary>
        /// True when this cell is the author's right corner, Hills r3c7. IL-WORLD-004S-R19.
        /// </summary>
        public bool IsRightCorner => Role == RaisedSurfaceRole.RIGHT_CORNER;

        /// <summary>
        /// True when this cell is the author's left top corner, Hills r0c4. IL-WORLD-004S-R20.
        /// </summary>
        public bool IsLeftTopCorner => Role == RaisedSurfaceRole.LEFT_TOP_CORNER;

        /// <summary>
        /// True when this cell is the author's right top corner, Hills r0c7. IL-WORLD-004S-R20.
        /// </summary>
        public bool IsRightTopCorner => Role == RaisedSurfaceRole.RIGHT_TOP_CORNER;

        /// <summary>
        /// Solid means Raised, or an enclosed hole. A hole is an INTERIOR boundary, so ground continues
        /// across it and it must never be mistaken for open sky.
        /// </summary>
        public bool SolidNorth => ConnectedNorth || NorthIsEnclosedVoid;

        public bool SolidSouth => ConnectedSouth || SouthIsEnclosedVoid;

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
            bool southVoid = IsEnclosedVoid(grid, x, y - 1);

            HillColumnSlot slot = SlotFor(w || westVoid, e || eastVoid);

            // IL-WORLD-004S-R16C. A front wall must stay on ONE visual row. The author grammar decides
            // that row from the column's own thickness: a column three deep or more carries its wall
            // inside its own mask, a thinner one drops it one row south. Where a thick column and a thin
            // column meet on the same logical row the two rules disagree, and the front boundary steps
            // down by a row at the junction so the two soil bands never meet. The real FirstIsland L
            // sample showed exactly that: a four deep corner column with its wall inside its own mask,
            // and a one deep arm with its wall dropped a row below, leaving a gap between them.
            //
            // So if a horizontal neighbour on this same row is Raised, also has an exposed south, and
            // already carries its wall inside its own mask, this cell's wall belongs on that same row
            // too. It reads only the neighbour's own locally derivable thickness, and walks at most two
            // cells so the answer is bounded and no recursive Resolve is needed.
            bool frontContinues = !s && (FrontContinuesInMask(grid, x, y, 0));

            // IL-WORLD-004S-R19. The corner tests read the grid directly, so they are computed here and
            // handed to RoleFor rather than growing another parameter list in it.
            bool leftCorner = IsAuthorLeftCorner(grid, x, y);
            bool rightCorner = IsAuthorRightCorner(grid, x, y);

            // IL-WORLD-004S-R20. The two top corners, same arrangement.
            bool leftTopCorner = IsAuthorLeftTopCorner(grid, x, y);
            bool rightTopCorner = IsAuthorRightTopCorner(grid, x, y);

            RaisedSurfaceRole role = RoleFor(
                leftCorner, rightCorner, leftTopCorner, rightTopCorner,
                n || northVoid, !s, runDepth, offset, northVoid,
                slot == HillColumnSlot.NARROW, frontContinues);

            return new RaisedTopologyState(
                raw, canonical, n, s, e, w, runDepth, offset,
                northVoid, eastVoid, westVoid, southVoid, role, slot);
        }

        /// <summary>
        /// True when this cell's front wall must join a front row that a horizontal neighbour has already
        /// committed to. Bounded to <see cref="MaxFrontWalk"/> cells so it always terminates.
        /// </summary>
        private const int MaxFrontWalk = 2;

        private static bool FrontContinuesInMask(
            TerrainGridData grid, int x, int y, int depth)
        {
            if (depth >= MaxFrontWalk)
            {
                return false;
            }

            if (ThickNeighbourEndsTheRow(grid, x - 1, y, -1)
                || ThickNeighbourEndsTheRow(grid, x + 1, y, 1))
            {
                return true;
            }

            // One step further along the same row, so a thin arm two cells long still joins a thick
            // column's front instead of stepping down half way along.
            if (RaisedNeighborResolver.IsRaised(grid, x - 1, y)
                && FrontContinuesInMask(grid, x - 1, y, depth + 1))
            {
                return true;
            }

            return RaisedNeighborResolver.IsRaised(grid, x + 1, y)
                && FrontContinuesInMask(grid, x + 1, y, depth + 1);
        }

        /// <summary>
        /// Whether this thick neighbour carries its wall inside its own mask AND sits at the END of the
        /// front on this row rather than in the middle of it.
        ///
        /// This end test is what separates the two real cases that are otherwise identical in topology.
        /// In the real L the four deep corner column is at the END of its row and a thin arm butts up
        /// against it, so the arm's wall must join the corner's row or the front steps down a row and
        /// leaves a gap. In the R14 T the three deep branch column is in the MIDDLE of its row with
        /// thin cells on BOTH sides, and there the two runs must stay on their own row and terminate on
        /// the author's r2c2 and r2c0 terminals, which is the corner treatment R14 already proved.
        /// Joining a middle column would raise three cells of front instead of one and destroy it.
        /// </summary>
        private static bool ThickNeighbourEndsTheRow(
            TerrainGridData grid, int x, int y, int dirX)
        {
            if (!NeighbourCarriesWallInMask(grid, x, y))
            {
                return false;
            }

            return !EmitsFrontAnywhere(grid, x + dirX, y);
        }

        /// <summary>True when this cell has a south facing front at all, wherever the wall lands.</summary>
        private static bool EmitsFrontAnywhere(TerrainGridData grid, int x, int y)
        {
            if (!RaisedNeighborResolver.IsRaised(grid, x, y)
                || RaisedNeighborResolver.IsRaised(grid, x, y - 1))
            {
                return false;
            }

            return !IsEnclosedVoid(grid, x, y - 1);
        }

        /// <summary>
        /// Whether this cell, on its own thickness alone, carries its front wall inside its own mask.
        /// Deliberately the NON recursive half of the rule: thickness three or more, or ground above it
        /// that is an enclosed hole. That is enough to recognise a thick column without consulting the
        /// junction rule and so without recursing.
        /// </summary>
        private static bool NeighbourCarriesWallInMask(TerrainGridData grid, int x, int y)
        {
            if (!RaisedNeighborResolver.IsRaised(grid, x, y)
                || RaisedNeighborResolver.IsRaised(grid, x, y - 1))
            {
                return false;
            }

            bool northVoid = IsEnclosedVoid(grid, x, y + 1);
            if (!RaisedNeighborResolver.IsRaised(grid, x, y + 1) && !northVoid)
            {
                return false;
            }

            return RaisedNeighborResolver.MeasureRunDepth(grid, x, y) >= 3 || northVoid;
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
        /// <summary>
        /// True when the cell at (x,y) is genuinely ONE cell wide, i.e. nothing Raised immediately
        /// east or west of it. IL-WORLD-004S-R19.
        ///
        /// Deliberately asks about real Raised ground and not about <c>Solid</c>. A one-cell hole is an
        /// INTERIOR boundary: ground continues across it, so treating a hole as the open side of a
        /// corner would invent a corner against a void. Requiring genuine Raised keeps a corner
        /// recognition strictly on the outer boundary of the mass.
        /// </summary>
        private static bool IsOneWide(TerrainGridData grid, int x, int y)
        {
            if (!RaisedNeighborResolver.IsRaised(grid, x, y))
            {
                return false;
            }

            return !RaisedNeighborResolver.IsRaised(grid, x + 1, y)
                && !RaisedNeighborResolver.IsRaised(grid, x - 1, y);
        }

        /// <summary>
        /// The author's LEFT corner: the south-west end of a horizontal front, where a ONE WIDE
        /// vertical boundary on the west turns east into that front.
        ///
        /// IL-WORLD-004S-R19. Read from a 3x3 window only, with no shape name, no component id and no
        /// coordinate test, so the same rule fires on an L, on the leg of a U, on a staircase step or
        /// on any irregular outline with the same local adjacency.
        ///
        /// The four conditions are exactly the author's LOCKED meaning:
        ///   south open   - this cell is on the front, so it has a front at all
        ///   west open    - the boundary it turns FROM is the vertical one on the west
        ///   east Raised  - the front continues east, so the boundary turns rather than ends
        ///   north one wide - the vertical boundary is a genuine single-cell-wide edge, not the side
        ///                   of a two-wide-or-wider mass. THIS CONDITION IS LOAD BEARING: without it
        ///                   every plateau's south-west cell has the same three-way signature and the
        ///                   rule would paint corners along the bottom of every rectangle, which is
        ///                   measured to break the R11 rectangles and the R18 band grammar.
        /// </summary>
        private static bool IsAuthorLeftCorner(TerrainGridData grid, int x, int y)
        {
            return !RaisedNeighborResolver.IsRaised(grid, x, y - 1)
                && !RaisedNeighborResolver.IsRaised(grid, x - 1, y)
                && RaisedNeighborResolver.IsRaised(grid, x + 1, y)
                && IsOneWide(grid, x, y + 1);
        }

        /// <summary>
        /// The author's RIGHT corner: the south-east end of a horizontal front, where that front turns
        /// north into a ONE WIDE vertical boundary on the east.
        ///
        /// IL-WORLD-004S-R19. The mirror-image condition set, reached through its own adjacency, never
        /// by mirroring art: the emitted slice is the author's own r3c7 in its original orientation.
        /// </summary>
        private static bool IsAuthorRightCorner(TerrainGridData grid, int x, int y)
        {
            return !RaisedNeighborResolver.IsRaised(grid, x, y - 1)
                && !RaisedNeighborResolver.IsRaised(grid, x + 1, y)
                && RaisedNeighborResolver.IsRaised(grid, x - 1, y)
                && IsOneWide(grid, x, y + 1);
        }

        /// <summary>
        /// The author's LEFT TOP corner: the north-west cell of a horizontal top structure, where a ONE
        /// WIDE vertical boundary on the west reaches the top and turns east into that structure.
        ///
        /// IL-WORLD-004S-R20. This is the 180 degree counterpart of <see cref="IsAuthorLeftCorner"/>,
        /// reached through its own adjacency and never by rotating or mirroring art: the emitted slice
        /// is the author's own r0c4 in its original orientation.
        ///
        /// The conditions are exactly the PM-locked meaning:
        ///   north open     - this cell is ON the top, which is what makes it a top corner
        ///   west open      - the boundary it turns FROM is the vertical one on the west
        ///   east Raised    - the top structure continues east, so the boundary turns rather than ends
        ///   south one wide - the vertical boundary is a genuine single-cell-wide column continuing
        ///                   south. THIS CONDITION IS LOAD BEARING, and it was MEASURED rather than
        ///                   assumed: without it the north-west cell of EVERY rectangle has the same
        ///                   three-way signature, because a plateau's top cell is also its south-west
        ///                   corner, and the rule would paint top corners along the top of every
        ///                   rectangle and break the R11 rectangles, the R18 band grammar and the R19
        ///                   front corners at once.
        /// </summary>
        private static bool IsAuthorLeftTopCorner(TerrainGridData grid, int x, int y)
        {
            return !RaisedNeighborResolver.IsRaised(grid, x, y + 1)
                && !RaisedNeighborResolver.IsRaised(grid, x - 1, y)
                && RaisedNeighborResolver.IsRaised(grid, x + 1, y)
                && IsOneWide(grid, x, y - 1);
        }

        /// <summary>
        /// The author's RIGHT TOP corner: the north-east cell of a horizontal top structure, where that
        /// structure reaches its right end and turns south into a ONE WIDE vertical boundary on the east.
        ///
        /// IL-WORLD-004S-R20. The mirror-image condition set, reached through its own adjacency. The
        /// "south neighbour is one wide" guard is the same load-bearing condition as on the left, for
        /// the same measured reason.
        /// </summary>
        private static bool IsAuthorRightTopCorner(TerrainGridData grid, int x, int y)
        {
            return !RaisedNeighborResolver.IsRaised(grid, x, y + 1)
                && !RaisedNeighborResolver.IsRaised(grid, x + 1, y)
                && RaisedNeighborResolver.IsRaised(grid, x - 1, y)
                && IsOneWide(grid, x, y - 1);
        }

        private static RaisedSurfaceRole RoleFor(
            bool leftCorner,
            bool rightCorner,
            bool leftTopCorner,
            bool rightTopCorner,
            bool solidNorth,
            bool southExposed,
            int runDepth,
            int offsetFromRunBottom,
            bool northIsEnclosedVoid,
            bool isNarrow,
            bool frontContinuesInMask)
        {
            // IL-WORLD-004S-R20. The author TOP corner grammar, and it is decided FIRST, before the
            // R19 front corners, the R18 straight shapes and the junction rules below, so neither the
            // column's thickness nor the run's width can substitute a different piece.
            //
            // The four corner predicates cannot collide with one another. The two top corners require
            // a Raised SOUTH neighbour, the two front corners require an open one, so a single cell can
            // never satisfy a top corner and a front corner at the same time. That is checked by
            // assertion over all 512 neighbourhoods in the R16B matrix, not just argued here.
            if (leftTopCorner)
            {
                return RaisedSurfaceRole.LEFT_TOP_CORNER;
            }

            if (rightTopCorner)
            {
                return RaisedSurfaceRole.RIGHT_TOP_CORNER;
            }
            // IL-WORLD-004S-R19. The author corner grammar, and it is decided FIRST, before the R18
            // straight shapes and before the junction rules below, so neither the column's thickness
            // nor the run's width can substitute a different piece for the corner. That is the whole
            // point of a corner: the cell where the boundary turns is the same cell whatever the two
            // straight runs either side of it happen to be.
            //
            // Both roles are at or above FRONT_CLIFF, so the corner draws its own front inside its own
            // logical mask and never displaces a cliff one row south. The corner therefore occupies
            // exactly one visual cell, on the logical Raised cell itself.
            if (leftCorner)
            {
                return RaisedSurfaceRole.LEFT_CORNER;
            }

            if (rightCorner)
            {
                return RaisedSurfaceRole.RIGHT_CORNER;
            }
            // A front wall belongs inside the mask whenever there is ground above it to sit under:
            // either the plateau is at least three cells thick (the proven r0/r1/r2 stack), or the cell
            // above is an enclosed hole, in which case the ground continues all the way round and the
            // wall must not drop a row and leave a notch in the front of a ring.
            //
            // IL-WORLD-004S-R16B NOTE, recorded because it was measured and rejected. Treating ground
            // that the shape wraps around as a pocket, and pulling the front wall up into this cell,
            // is symmetric across all four orientations and passes the whole topology matrix, but it
            // turns a south facing platform front into dirt windows punched into the top surface. The
            // existing behaviour was rendered at every junction and inspected: the run already
            // terminates on the author terminal pieces, so the inner corner is already handled.
            //
            // IL-WORLD-004S-R16C adds the one case that real FirstIsland data proved was missing: a
            // thick column and a thin one meeting on the same row, where the front must not step.
            //
            // When the front continues, the support does not have to be directly overhead. In the real
            // L sample the cell at the inside of the bend has open ground to its north, which is the
            // notch, yet its neighbour on the same row carries the wall on the row this cell must join.
            // Requiring solid north as well would leave that cell behind and reintroduce the step, so a
            // continuing front supplies its own support. The only art given up is the rounded cap on
            // that one cell, and the author has no north facing soil face to replace it with anyway.
            // IL-WORLD-004S-R18. The author grammar for the most basic straight shapes, locked after
            // the source Basic Pack and Hills.png were inspected cell by cell in Unity.
            //
            //   ONE cell deep band -> the author's r3 row, carried INSIDE the logical mask:
            //                         r3c0 | r3c1 ... r3c1 | r3c2 across, and r3c3 for a single cell.
            //   TWO deep narrow col -> r0c3 over r2c3, both inside the logical mask.
            //
            // Both used to displace a second visual cell one row SOUTH of the logical Raised, so a
            // single logical cell occupied two visual cells and a one cell high band grew a cliff row
            // hanging outside its own mask. A one wide two deep column is restricted to the narrow c3
            // stack because that is the instance the author drew; the wide two deep plateau keeps the
            // composition R9 already proved.
            //
            // These are decided FIRST, on depth alone, so the basic straight shapes can never be
            // re-routed through the junction rules further down.
            if (runDepth == 1)
            {
                return southExposed
                    ? RaisedSurfaceRole.SECOND_FRONT_CLIFF
                    : RaisedSurfaceRole.TOP_SURFACE;
            }

            if (runDepth == 2 && isNarrow)
            {
                if (southExposed)
                {
                    return RaisedSurfaceRole.FRONT_CLIFF;
                }

                return offsetFromRunBottom == 1
                    ? RaisedSurfaceRole.TOP_SURFACE
                    : RaisedSurfaceRole.MIDDLE_SURFACE;
            }

            bool frontInMask = southExposed
                && (runDepth >= 3 || northIsEnclosedVoid || frontContinuesInMask)
                && (solidNorth || frontContinuesInMask);

            if (frontInMask)
            {
                return runDepth >= 4 && offsetFromRunBottom == 0 && !isNarrow
                    ? RaisedSurfaceRole.SECOND_FRONT_CLIFF
                    : RaisedSurfaceRole.FRONT_CLIFF;
            }

            if (runDepth >= 4)
            {
                // IL-WORLD-004S-R18. The one wide column is r0c3 over r1c3 repeated over r2c3 for
                // EVERY depth, so it never reaches the author's r3c3 second front row. The wide block
                // keeps the r0..r3 stack R4 reconstructed 1:1.
                if (isNarrow)
                {
                    if (offsetFromRunBottom == 0)
                    {
                        return RaisedSurfaceRole.FRONT_CLIFF;
                    }

                    return offsetFromRunBottom == runDepth - 1
                        ? RaisedSurfaceRole.TOP_SURFACE
                        : RaisedSurfaceRole.MIDDLE_SURFACE;
                }

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
                if (offsetFromRunBottom == 1)
                {
                    return RaisedSurfaceRole.TOP_SURFACE;
                }

                // IL-WORLD-004S-PJ. The bottom cell of a two-deep, two-or-more-wide column carries the
                // author front wall INSIDE its own mask, on row r2.
                //
                // It used to return MIDDLE_SURFACE, which is below FRONT_CLIFF, and that is the ONLY
                // condition under which RaisedTopologyState.DrawsFrontCliffBelow can fire. So this one
                // branch is the sole origin of every visual tile the projection ever places at a
                // coordinate other than its own logical cell: AuthorHillsLocalResolver.Resolve then
                // emits an extra cliff at (x, y-1), one row south of the mask.
                //
                // MEASURED, across all 256 neighbourhoods in two column contexts: 128 of 512
                // neighbourhoods emitted a displaced tile and 128 of 128 came from this branch, every one
                // with runDepth == 2 and isNarrow == false.
                //
                // REPRODUCED on the locked P fixture. BEFORE [XXX / X..] resolved to 4 logical, 4 visual,
                // 0 outside. Painting the fifth cell to give [XXX / XX.] produced 5 logical but 7 visual
                // and 2 outside the mask: (4,3) = Hills_r2c0 emitted by (4,4) and (5,3) = Hills_r2c2
                // emitted by (5,4). With this branch fixed the same fixture is 5 logical, 5 visual and
                // 0 outside, and every visual tile sits on its own logical cell.
                //
                // The wall is the author's own proven row r2, reached through the existing slot walk, so
                // nothing is mirrored, rotated, stretched or generated. The slot is decided by the same
                // 3x3 adjacency as before, which makes the west end of this front a genuine LEFT_TERMINAL
                // and therefore Hills_r2c0, the author left boundary plus front edge piece.
                //
                // HONEST SCOPE NOTE. The bottom row of a two-deep rectangle is locally indistinguishable
                // from this P front: its west front cell has the identical raw mask 0x16. No rule that
                // reads only a 3x3 neighbourhood can change one without changing the other, so the two
                // and three cell wide rectangles also stop displacing. That is a measured consequence,
                // not a choice, and their old outside-row oracles are marked
                // DEFERRED_LEGACY_EXPECTATION rather than silently re-pinned.
                return southExposed
                    ? RaisedSurfaceRole.FRONT_CLIFF
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
