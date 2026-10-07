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

        /// <summary>
        /// A corner/junction where a vertical Raised boundary meets a Raised platform running east, and
        /// the boundary does NOT continue above this cell. IL-WORLD-004S-R21.
        ///
        /// R21_SUPERSEDED, IL-WORLD-004S-R23B. RETAINED BUT UNREACHABLE. No production rule produces
        /// this role any more, and none may: proving a junction needs <c>(x, y-2)</c>, <c>(x+2, y)</c>
        /// and <c>(x+2, y-1)</c>, all outside a 3x3 neighbourhood. R21 was itself REOPENED after the
        /// user reported a visual FAIL, so its component was never an oracle; it is historical evidence
        /// only. The author slice <c>Hills_r3c5</c> stays wired in the composition set so it is never
        /// silently missing, and the junction topology is reported UNRESOLVED for PM.
        /// </summary>
        CORNER_NO_UPPER_CONTINUATION = 8,

        /// <summary>
        /// The SAME corner/junction, but the vertical boundary DOES continue above this cell, so this cell
        /// carries a Raised NORTH neighbour. IL-WORLD-004S-R21.
        ///
        /// R21_SUPERSEDED, IL-WORLD-004S-R23B. RETAINED BUT UNREACHABLE, for the same reason and with
        /// the same provenance as
        /// <see cref="RaisedSurfaceRole.CORNER_NO_UPPER_CONTINUATION"/>. North occupancy was the sole
        /// discriminator between the two R21 states, which is a genuine local fact, but the junction
        /// TEST itself was not local, so neither state survives under that name.
        ///
        /// The SLICE this role pointed at is not lost: the author's r2c4 junction is now expressed by
        /// <see cref="JUNCTION_VERTICAL_CONTINUATION"/>, which proves the same author component from
        /// local topology alone. R21's component was right; only its test was not local.
        /// </summary>
        CORNER_WITH_UPPER_CONTINUATION = 9,

        /// <summary>
        /// The author's junction continuation component, Hills <c>r2c4</c>. IL-WORLD-004S-R24.
        ///
        /// LOCKED_USER_VISUAL_ORACLE. The user confirmed on screen, by screenshot, that one junction cell
        /// on the live map must draw the author's r2c4, and that it was drawing the plain body row
        /// <c>r1c0</c> instead. It is therefore no longer UNRESOLVED and no longer a guess.
        ///
        /// THE AUTHOR MEANING. A vertical Raised boundary on the WEST continues north past this cell,
        /// meets a Raised platform on the EAST, and that platform STEPS AWAY to the north while HOLDING
        /// to the south. The junction is where the vertical boundary hands over to the platform, and the
        /// author drew one piece for that handover: r2c4.
        ///
        /// PROVEN LOCAL. The whole condition is six bits of this cell's own raw 3x3 - see
        /// <see cref="IsAuthorJunctionContinuation"/>. Measured over all 256 raw masks at eight reaches:
        /// the condition selects exactly the four NW/SW variants of raw 0xD2, every one of which draws
        /// the author's r1c0 today, and the twelve other raw masks that draw r1c0 are left alone. So the
        /// junction is split OUT of the vertical body and the body is not otherwise disturbed.
        ///
        /// The discriminator is one bit. NE open with SE raised says the platform steps away north and
        /// holds south; NE raised says the platform is simply a wide plateau edge and the body row is
        /// correct. No depth, no offset, no walk, no width, no height, no shape name, no x+/-2 and no
        /// y+/-2 appear in the test.
        /// </summary>
        JUNCTION_VERTICAL_CONTINUATION = 10,

        /// <summary>
        /// IL-WORLD-004S-R26B, PROVISIONAL_USER_VISUAL_ORACLE -> STATIC_VERIFIED.
        ///
        /// The author's MIRROR handover, the author's r2c7. The same relation as
        /// <see cref="JUNCTION_VERTICAL_CONTINUATION"/> with the boundary on the EAST instead of the west:
        /// the vertical boundary sits on the east side, continues north, and the platform attached to the
        /// west steps away to the north-west while holding to the south-west.
        ///
        /// PROVEN BY OCCUPANCY, NOT BY GUESS. R25A measured this family's core raw as 0x6A and proved
        /// A-vs-B differ in exactly W, E, SW and SE - all swapped - so the pair is a strict OCCUPANCY
        /// mirror. On the live map it matches exactly one cell, (8,-10), which currently draws the plain
        /// body row r1c2. No Sprite is mirrored: the author drew both pieces and each is emitted as-is.
        /// </summary>
        JUNCTION_VERTICAL_CONTINUATION_MIRROR = 11,

        /// <summary>
        /// IL-WORLD-004S-R26B, PROVISIONAL_USER_VISUAL_ORACLE -> STATIC_VERIFIED.
        ///
        /// The author's handover where the vertical boundary does NOT continue north: the author's r3c5.
        /// It is the handover relation with ONE BIT removed - nothing is Raised above this cell, so this is
        /// where the boundary ENDS rather than runs on. R21 recorded exactly this pair, with north
        /// occupancy as the sole discriminator between its r3c5 and its r2c4, so the relation is
        /// historical evidence rather than a new guess.
        ///
        /// ITS 3x3 IS NOT ENOUGH, AND THAT IS MEASURED. R25 found this raw at five live cells, and R26A
        /// found that the 3x3 rectangle's top-left corner has the IDENTICAL raw 0xD0, which R11 locks to
        /// the author's r0c0. A finite 5x5 read separates them, and this role REQUIRES that separation:
        /// the platform must step away to the south-east while the boundary keeps running south.
        /// See <see cref="IsJunctionEndedWest"/>.
        /// </summary>
        JUNCTION_WEST_ENDED = 12,

        /// <summary>
        /// IL-WORLD-004S-R26B, PROVISIONAL_USER_VISUAL_ORACLE -> STATIC_VERIFIED.
        ///
        /// The mirror of <see cref="JUNCTION_WEST_ENDED"/>, the author's r3c6: the east-side boundary
        /// ends, the west platform steps away to the south-west, and the boundary keeps running south.
        /// </summary>
        JUNCTION_EAST_ENDED = 13,

        /// <summary>
        /// IL-WORLD-004S-R26B, PROVISIONAL_USER_VISUAL_ORACLE -> STATIC_VERIFIED.
        ///
        /// The author's inner corner where ground wraps around on the WEST and continues SOUTH: the
        /// author's r0c5. All eight neighbours are decided - north, north-east and east are open because
        /// the ground wraps away there, while north-west, west, south-west, south and south-east are
        /// Raised.
        ///
        /// The relation it names is "GROUND WRAPS AROUND ME AND KEEPS GOING SOUTH". Its raw 0xE9 was
        /// measured on the live map at (1,-10), where it currently draws the author's r0c2.
        /// </summary>
        INNER_CORNER_WRAPS_SOUTH_WEST = 14,

        /// <summary>IL-WORLD-004S-R26B, PROVISIONAL. The author's r0c6, the mirror of
        /// <see cref="INNER_CORNER_WRAPS_SOUTH_WEST"/>. Live raw 0xF4 at (7,-10), currently r0c0.</summary>
        INNER_CORNER_WRAPS_SOUTH_EAST = 15,

        /// <summary>IL-WORLD-004S-R26B, PROVISIONAL. The author's r1c4: ground wraps around on the WEST
        /// and keeps going NORTH. Live raw 0x2F at (1,-6), currently r2c2.</summary>
        INNER_CORNER_WRAPS_NORTH_WEST = 16,

        /// <summary>IL-WORLD-004S-R26B, PROVISIONAL. The author's r1c6, the mirror of
        /// <see cref="INNER_CORNER_WRAPS_NORTH_WEST"/>. Live raw 0x97 at (7,-6), currently r2c0.</summary>
        INNER_CORNER_WRAPS_NORTH_EAST = 17,

        /// <summary>IL-WORLD-004S-R26B, PROVISIONAL. The author's r0c8: a FOUR-WAY crossing in which
        /// every arm is exactly one cell wide, so all four diagonals are open. Live raw 0x5A at (-18,-5),
        /// which currently draws the plain body row r1c1 - plainly wrong in the middle of a crossing.</summary>
        FOUR_WAY_CROSS_ONE_WIDE = 18,

        /// <summary>IL-WORLD-004S-R26B, PROVISIONAL. The author's r3c8: a four-way crossing that is not
        /// the one-wide flavour, whose arm along one axis runs two or more cells.</summary>
        FOUR_WAY_CROSS_LONG_ARM = 19,

        /// <summary>IL-WORLD-004S-R26B, PROVISIONAL. The author's r4c8: the remaining four-way crossing
        /// flavour, told apart by the finite 5x5 context rather than by the cell's own eight bits.</summary>
        FOUR_WAY_CROSS_OTHER = 20,
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
        /// True when this cell is the R21 corner/junction whose vertical boundary does NOT continue
        /// above it. IL-WORLD-004S-R21.
        /// </summary>
        public bool IsCornerNoUpperContinuation =>
            Role == RaisedSurfaceRole.CORNER_NO_UPPER_CONTINUATION;

        /// <summary>
        /// True when this cell is the R21 corner/junction whose vertical boundary DOES continue above
        /// it. IL-WORLD-004S-R21.
        /// </summary>
        public bool IsCornerWithUpperContinuation =>
            Role == RaisedSurfaceRole.CORNER_WITH_UPPER_CONTINUATION;

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

        /// <summary>
        /// Reads the local topology of one logical cell. Never writes to the grid.
        ///
        /// IL-WORLD-004S-R23B. This is now LAYER A of the author grammar and it is strictly LOCAL: the
        /// only thing it reads is this cell's own 3x3, read through
        /// <see cref="RaisedNeighborResolver.ResolveRaisedMask"/> plus the four corner predicates, and
        /// every one of those predicates is itself expressed in 3x3 terms. No branch below reads
        /// <c>x +/- 2</c>, <c>y +/- 2</c>, walks a run to an edge, or compares a run length.
        ///
        /// What was REMOVED from the semantic decision, and why each removal was legal:
        ///
        ///   <c>runDepth == 1 / 2 / 3 / &gt;= 4</c>  -> replaced by the cell's own N and S occupancy.
        ///     A cell with both N and S open is the top AND the front of its own ground, which is
        ///     exactly what "one deep" meant; a cell with N solid and S open is a front row; a cell
        ///     with N open and S solid is a top row; a cell with both solid is an interior body row.
        ///     No length appears anywhere.
        ///
        ///   <c>offsetFromRunBottom</c>             -> not consulted. It survives only as a LAYOUT
        ///     property on this struct for reporting, and it decides no role and no slot.
        ///
        ///   <c>MaxFrontWalk</c> and the recursive front walk -> REMOVED ENTIRELY, with no replacement.
        ///     This is a real loss and it is recorded as one: the R16C rule let a front wall step down
        ///     to join a thick neighbour's row, and nothing local can do that, because the whole point
        ///     of the rule was that the thick column was TWO or more cells away. A local replacement was
        ///     written and MEASURED, and it broke the frozen R19 oracle in 8 of 33 fixtures by turning
        ///     the cell beside a front corner into an r2 terminal, so it was withdrawn rather than kept.
        ///     The R16C behaviour is therefore NOT reproduced; see the card report for what that means
        ///     for a concave L, and note R16C was never an accepted visual result.
        ///
        ///   <c>IsAuthorCornerJunction</c> (R21)   -> removed, see R21_SUPERSEDED below.
        ///
        /// <see cref="RunDepth"/> and <see cref="OffsetFromRunBottom"/> are still measured, but ONLY as
        /// LAYOUT / repetition information. They are reported and they are available to Layer C, and no
        /// role, no slot and no sprite is derived from either of them.
        /// </summary>
        public static RaisedTopologyState Resolve(TerrainGridData grid, int x, int y)
        {
            TerrainNeighborMask raw = RaisedNeighborResolver.ResolveRaisedMask(grid, x, y);
            TerrainNeighborMask canonical = RaisedNeighborResolver.Normalize(raw);

            bool n = (raw & TerrainNeighborMask.North) != 0;
            bool s = (raw & TerrainNeighborMask.South) != 0;
            bool e = (raw & TerrainNeighborMask.East) != 0;
            bool w = (raw & TerrainNeighborMask.West) != 0;

            // LAYOUT ONLY, IL-WORLD-004S-R23B. Reported so Layer C can stack BODY repetition, and
            // deliberately NOT passed to RoleForLocal. See the class note for why length may not decide
            // an author sprite.
            int runDepth = RaisedNeighborResolver.MeasureRunDepth(grid, x, y);
            int offset = y - RaisedNeighborResolver.MeasureRunBottom(grid, x, y);

            // Informational only. A void is an INTERIOR boundary, so these four facts are still
            // reported through Solid*/Exposed* for diagnostics and for the R16C/R17A reporting, but
            // none of them reaches RoleForLocal or SlotFor any more.
            bool northVoid = IsEnclosedVoid(grid, x, y + 1);
            bool eastVoid = IsEnclosedVoid(grid, x + 1, y);
            bool westVoid = IsEnclosedVoid(grid, x - 1, y);
            bool southVoid = IsEnclosedVoid(grid, x, y - 1);

            // IL-WORLD-004S-R23B. The slot comes from the cell's OWN raw cardinals. A void is no longer
            // admitted as the open side of a slot, because a one cell hole is an interior boundary and
            // treating it as open sky is what put the author's narrow c3 stack on the inside of a ring.
            // This is what makes "E only -> LEFT_TERMINAL, W+E -> BODY, W only -> RIGHT_TERMINAL" hold as
            // an identity of the raw 3x3.
            HillColumnSlot slot = SlotFor(w, e);

            // IL-WORLD-004S-R19 / R20. Both corner families are already expressed in 3x3 terms and are
            // unchanged: IsOneWide reads the cell's own N/NE/NW (front corners) or S/SE/SW (top corners).
            bool leftCorner = IsAuthorLeftCorner(grid, x, y);
            bool rightCorner = IsAuthorRightCorner(grid, x, y);
            bool leftTopCorner = IsAuthorLeftTopCorner(grid, x, y);
            bool rightTopCorner = IsAuthorRightTopCorner(grid, x, y);

            // IL-WORLD-004S-R24. The author's junction continuation, decided from the RAW MASK ALONE and
            // therefore strictly inside this cell's own 3x3. It is computed here rather than inside
            // RoleForLocal for the same reason the corners are: the predicate is already a pure function
            // of the mask, so passing it in keeps RoleForLocal's signature about the cardinal ladder only.
            bool junctionContinuation = IsAuthorJunctionContinuation(raw);

            // A reflective guard, not a runtime cost worth worrying about: if this predicate ever grows
            // a grid parameter it can no longer be decided from the mask alone, and the R23B harness
            // asserts that no decision method takes a non-coordinate integer. See the harness.

            // IL-WORLD-004S-R26B, LEVEL 2. The finite author composition context, consulted ONLY where
            // Layer A's own topology is genuinely ambiguous. It runs AFTER the straight ladder so a cell
            // that is plainly a top row, a front row, an interior body row or a band keeps its R18/R23B
            // answer untouched, and it can only ever REFINE a cell - never widen Layer A.
            //
            // The one exception is the one-wide four-way crossing, which Layer A would call an interior
            // body row: that is precisely the reported defect, a plain r1c1 in the middle of a crossing.
            if (!TryComposition(grid, x, y, raw, out RaisedSurfaceRole composed))
            {
                composed = RoleForLocal(
                    leftCorner, rightCorner, leftTopCorner, rightTopCorner,
                    junctionContinuation, n, s);
            }

            RaisedSurfaceRole role = composed;

            return new RaisedTopologyState(
                raw, canonical, n, s, e, w, runDepth, offset,
                northVoid, eastVoid, westVoid, southVoid, role, slot);
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
        /// LEVEL 2 - FINITE AUTHOR COMPOSITION CONTEXT. IL-WORLD-004S-R26B.
        ///
        /// WHY THIS LAYER EXISTS AT ALL. R25 measured that some author compositions cannot be decided by
        /// one cell's own 3x3: raw 0xD0 occurs both at a plain 3x3 rectangle corner, where R11 locks the
        /// author's r0c0, and at a composition site the user has confirmed. That is a real limit of the
        /// 3x3, and the honest response is a FINITE LOCAL context, not an unbounded one.
        ///
        /// EVERY PREDICATE HERE IS FINITE AND NAMED. Each one states an author relation in words and is
        /// then expressed in bits. The radius never exceeds 5x5. There is no run walk, no run depth, no
        /// whole-region size, no absolute coordinate, no fixture name and no shape name anywhere in this
        /// file, and the R23B harness proves the decision methods take no non-coordinate integer.
        ///
        /// THE ORDER MATTERS and is a deliberate priority, exactly like the corner ladder above it. A
        /// cell that satisfies two relations is resolved by the more specific one first:
        ///
        ///   1. the one-wide four-way crossing, because it is the only four-way case with no raised
        ///      diagonal at all and is therefore the most specific statement available;
        ///   2. the two handovers whose boundary CONTINUES north;
        ///   3. the two handovers whose boundary ENDS, which need the 5x5 step-away test;
        ///   4. the four wrap-around inner corners;
        ///   5. the remaining four-way crossings.
        ///
        /// WHY THE FOUR-WAY FAMILY CANNOT OVER-CAPTURE, WHICH WAS MEASURED AND NOT ASSUMED. A first
        /// attempt asked only for "four cardinals Raised plus two or more diagonals Raised" and that
        /// matched ELEVEN raw masks - including 0xFF, which is the ordinary deep interior of a wide
        /// plateau and extremely common. That would have painted every plateau interior as a junction.
        /// Every four-way predicate here therefore additionally requires AT LEAST ONE DIAGONAL OPEN,
        /// which is exactly the condition that says "I am on a crossing, not inside a mass". 0xFF is
        /// excluded by construction.
        /// </summary>
        private static bool TryComposition(
            TerrainGridData grid,
            int x,
            int y,
            TerrainNeighborMask raw,
            out RaisedSurfaceRole composition)
        {
            composition = RaisedSurfaceRole.MIDDLE_SURFACE;

            bool n = (raw & TerrainNeighborMask.North) != 0;
            bool s = (raw & TerrainNeighborMask.South) != 0;
            bool e = (raw & TerrainNeighborMask.East) != 0;
            bool w = (raw & TerrainNeighborMask.West) != 0;
            bool nw = (raw & TerrainNeighborMask.NorthWest) != 0;
            bool ne = (raw & TerrainNeighborMask.NorthEast) != 0;
            bool sw = (raw & TerrainNeighborMask.SouthWest) != 0;
            bool se = (raw & TerrainNeighborMask.SouthEast) != 0;

            // 1. A four-way crossing whose every arm is exactly one cell wide: all four cardinals Raised
            //    and all four diagonals open. Nothing else in the grammar is that specific.
            if (n && s && w && e && !nw && !ne && !sw && !se)
            {
                composition = RaisedSurfaceRole.FOUR_WAY_CROSS_ONE_WIDE;
                return true;
            }

            // 2. The two handovers whose boundary CONTINUES north. Each is decided on the cell's own
            //    eight bits plus ONE finite 5x5 clause, and that clause is what keeps them honest.
            //
            //    THE CLAUSE. The relation is not only "the boundary continues north"; it is "the
            //    boundary continues AND the platform it meets ENDS". Without the second half these
            //    predicates also match the outer edge of an ordinary plateau. MEASURED, not suspected:
            //    a 7x7 plateau with one cell notched out of its side produces an edge cell with raw
            //    0x6A - the mirror handover's exact raw - which would repaint plain plateau edge as a
            //    junction. A plateau's edge is still solid two cells north-west; a real handover's
            //    platform has ended there. That single cell is the whole difference.
            if (!w && n && s && e && !ne && !sw && se
                && !RaisedNeighborResolver.IsRaised(grid, x + 2, y + 1))
            {
                composition = RaisedSurfaceRole.JUNCTION_VERTICAL_CONTINUATION;
                return true;
            }

            if (!e && n && s && w && !nw && sw && !se
                && !RaisedNeighborResolver.IsRaised(grid, x - 2, y + 1))
            {
                composition = RaisedSurfaceRole.JUNCTION_VERTICAL_CONTINUATION_MIRROR;
                return true;
            }

            // 3. The two handovers whose boundary ENDS: no Raised ground north. These are the ones that
            //    a 3x3 CANNOT decide, so the finite 5x5 step-away test is mandatory here.
            if (!w && !n && s && e && !ne && se && SteppedAwayEast(grid, x, y))
            {
                composition = RaisedSurfaceRole.JUNCTION_WEST_ENDED;
                return true;
            }

            if (!e && !n && s && w && !nw && sw && SteppedAwayWest(grid, x, y))
            {
                composition = RaisedSurfaceRole.JUNCTION_EAST_ENDED;
                return true;
            }

            // 4. The four wrap-around inner corners. Each names the direction the ground keeps going.
            if (nw && !n && !ne && w && !e && sw && s && se)
            {
                composition = RaisedSurfaceRole.INNER_CORNER_WRAPS_SOUTH_WEST;
                return true;
            }

            if (!nw && !n && ne && !w && e && sw && s && se)
            {
                composition = RaisedSurfaceRole.INNER_CORNER_WRAPS_SOUTH_EAST;
                return true;
            }

            if (nw && n && ne && w && !e && sw && !s && !se)
            {
                composition = RaisedSurfaceRole.INNER_CORNER_WRAPS_NORTH_WEST;
                return true;
            }

            if (nw && n && ne && !w && e && !sw && !s && se)
            {
                composition = RaisedSurfaceRole.INNER_CORNER_WRAPS_NORTH_EAST;
                return true;
            }

            // 5. The remaining four-way crossings. TWO guards, and the second one was added because the
            //    over-capture sweep caught the first version stealing plateau ground.
            //
            //    Guard one - at least one diagonal must be OPEN. This is what says "I am on a crossing,
            //    not inside a mass": 0xFF, the ordinary deep interior of a wide plateau, has all four
            //    diagonals Raised and is excluded by construction.
            //
            //    Guard two - MEASURED, NOT ASSUMED. Guard one alone was not enough. It also matched
            //    0x7F, 0xBF and 0xFB, which are plateau cells with exactly ONE diagonal missing - an
            //    ordinary concave corner or notch, not a crossing - and those are common. So a genuine
            //    crossing must ALSO have exactly one axis whose arm carries on for a second cell while
            //    the other axis stops after one. On a plateau concave corner every second cell is
            //    Raised, both axes run long, and neither rule fires, so those raws are excluded.
            if (n && s && w && e && (!nw || !ne || !sw || !se))
            {
                bool verticalArmRunsLong = RaisedNeighborResolver.IsRaised(grid, x, y + 2)
                    || RaisedNeighborResolver.IsRaised(grid, x, y - 2);
                bool horizontalArmRunsLong = RaisedNeighborResolver.IsRaised(grid, x + 2, y)
                    || RaisedNeighborResolver.IsRaised(grid, x - 2, y);

                // Exactly one axis may run long. BOTH long means this is not a one-wide crossing at all
                // but a wide mass with a notch in it, and it belongs to Layer A, not here.
                if (verticalArmRunsLong && !horizontalArmRunsLong)
                {
                    composition = RaisedSurfaceRole.FOUR_WAY_CROSS_LONG_ARM;
                    return true;
                }

                if (horizontalArmRunsLong && !verticalArmRunsLong)
                {
                    composition = RaisedSurfaceRole.FOUR_WAY_CROSS_OTHER;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The finite 5x5 test that separates an ENDED west handover from an ordinary corner whose raw is
        /// identical at 3x3.
        ///
        /// THE AUTHOR RELATION: "the platform stops here and the boundary keeps running". Both halves are
        /// needed and neither is enough:
        ///
        ///   two cells EAST, on this cell's own row, is RAISED - the bar this cell belongs to is at
        ///                                                 least three cells wide, so this really is the
        ///                                                 end of a bar, not the corner of a narrow block.
        ///   two cells EAST, one cell SOUTH is OPEN     - that bar has ended by the next row down.
        ///   two cells SOUTH, on this cell's own column, is RAISED - the vertical boundary itself
        ///                                                 carries on, which is what makes this a handover
        ///                                                 and not an ordinary corner.
        ///                                                 and not an ordinary corner.
        ///
        /// MEASURED AGAINST THE COUNTEREXAMPLE. The R11-locked 3x3 rectangle's top-left corner has the
        /// IDENTICAL 3x3 raw 0xD0 but fails BOTH halves: its bar has not ended, and it has no wall
        /// running south. The 5x5 stamps recorded by R26A are
        ///     ring corner    ..... / ..... / ..### / ..##. / ..#..
        ///     3x3 rectangle  ..... / ..... / ..### / ..### / ..###
        /// and the two rows that differ are exactly the two this test reads.
        ///
        /// A FIRST REVISION READ THE WRONG CELL and simply did not fire on the live map at all: it asked
        /// whether the cell one WEST and two SOUTH was Raised, which is outside the mass on a west wall.
        /// The boundary is the column THIS cell stands on, so the question belongs at (x, y-2).
        /// </summary>

        /// THE THIRD CLAUSE WAS ADDED BECAUSE A TWO-BY-THREE BLOCK WAS BEING CAPTURED. With only the
        /// bar-has-ended and wall-carries-on halves, the top-left cell of a plain two-wide three-tall
        /// block satisfied both: its bar had ended and its wall ran on. R23B ORACLE_R24_PLAIN_BODY_2x3
        /// caught it and reported r3c5 where the author r0c0 is locked. Requiring the bar to be three
        /// cells wide is what separates a real handover from an ordinary block.
        private static bool SteppedAwayEast(TerrainGridData grid, int x, int y)
        {
            return RaisedNeighborResolver.IsRaised(grid, x + 2, y)
                && !RaisedNeighborResolver.IsRaised(grid, x + 2, y - 1)
                && RaisedNeighborResolver.IsRaised(grid, x, y - 2);
        }

        /// <summary>The mirror of <see cref="SteppedAwayEast"/>: the bar ends to the WEST and the
        /// vertical boundary carries on.</summary>
        private static bool SteppedAwayWest(TerrainGridData grid, int x, int y)
        {
            return RaisedNeighborResolver.IsRaised(grid, x - 2, y)
                && !RaisedNeighborResolver.IsRaised(grid, x - 2, y - 1)
                && RaisedNeighborResolver.IsRaised(grid, x, y - 2);
        }


        /// <summary>
        /// The author's junction continuation: the vertical boundary on the WEST continues north past
        /// this cell and hands over to a Raised platform on the EAST, and that platform STEPS AWAY to the
        /// north while HOLDING to the south. The author's component for this handover is
        /// <c>Hills_r2c4</c>. IL-WORLD-004S-R24.
        ///
        /// READ FROM THIS CELL'S OWN 3x3 AND NOTHING ELSE. Six bits, each one a direct statement about a
        /// single neighbour:
        ///
        ///   W open     the vertical boundary is on the west, so this cell sits on it
        ///   N raised   the boundary CONTINUES north above this cell - this is the "continuation"
        ///   S raised   the boundary also continues south below this cell, so this is where it hands over
        ///   E raised   the platform is attached on the east, so the handover has something to hand to
        ///   SE raised  the platform HOLDS to the south-east
        ///   NE open    the platform STEPS AWAY to the north-east
        ///
        /// WHY "NE OPEN WITH SE RAISED" IS THE DISCRIMINATOR, MEASURED. Sixteen raw masks draw the
        /// author's r1c0. Four of them - 0xD2, 0xD3, 0xF2 and 0xF3, which are just the NW and SW variants
        /// of each other - are this junction. The other twelve split into two groups that are BOTH plainly
        /// a vertical body and must keep r1c0:
        ///
        ///   0xD6 0xD7 0xF6 0xF7                     NE RAISED. The platform continues north too, so
        ///                                              this is a wide plateau edge, not a handover,
        ///                                              and the plain body row is right.
        ///   0x52 0x53 0x56 0x57 0x72 0x73 0x76 0x77  SE OPEN. The platform does not hold to the south,
        ///                                              so this is the top of the boundary with nothing
        ///                                              to hand over to.
        ///
        /// So NE open together with SE raised is exactly the handover, and it is a SINGLE BIT away from the
        /// plain body. That is why the rule can be 3x3-only: the difference between junction and body is
        /// one neighbour one cell away, not a measurement of how far the ground runs.
        ///
        /// NW and SW are deliberately NOT constrained. They say whether the west boundary is exactly one
        /// cell wide at that corner, which is a fact about the surroundings rather than about this
        /// handover, and the author art is the same either way - so the four variants are one topology and
        /// one rule names it. Pinning those two bits would assert that the junction only exists where the
        /// boundary happens to be one wide, which is a shape assumption about the surroundings rather than
        /// an author fact.
        ///
        /// NO x+/-2, NO y+/-2, NO runDepth, NO offset, NO walk, NO width or height, NO shape name. Six bits.
        ///
        /// The live map carries this junction at (0,-10), raw 0xD2, and the user's screenshot confirmed it
        /// must draw the author's r2c4.
        /// </summary>
        private static bool IsAuthorJunctionContinuation(TerrainNeighborMask raw)
        {
            return (raw & TerrainNeighborMask.West) == 0
                && (raw & TerrainNeighborMask.North) != 0
                && (raw & TerrainNeighborMask.South) != 0
                && (raw & TerrainNeighborMask.East) != 0
                && (raw & TerrainNeighborMask.SouthEast) != 0
                && (raw & TerrainNeighborMask.NorthEast) == 0;
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

        /// <summary>
        /// R21_SUPERSEDED, IL-WORLD-004S-R23B. The corner/junction rule is GONE from production.
        ///
        /// R21 required three reads that are provably outside a 3x3 neighbourhood: <c>(x, y-2)</c> to
        /// prove the boundary ends below this cell, <c>(x+2, y)</c> to prove the platform is at least
        /// three wide, and <c>(x+2, y-1)</c> to prove it steps away. The card forbids exactly those
        /// reads, and it also forbids re-adding them to keep the old R21 test passing.
        ///
        /// So the two R21 roles, <see cref="RaisedSurfaceRole.CORNER_NO_UPPER_CONTINUATION"/> and
        /// <see cref="RaisedSurfaceRole.CORNER_WITH_UPPER_CONTINUATION"/>, and their author slices
        /// <c>Hills_r3c5</c> and <c>Hills_r2c4</c> are retained in the enum, in
        /// <see cref="AuthorHillsCompositionSet"/> and in the composition asset, but NO rule produces
        /// them any more, because no rule may read that far.
        ///
        /// R21 was itself REOPENED after the user reported a visual FAIL, so its two components are
        /// historical evidence only and never an oracle. No replacement junction rule is invented here:
        /// the junction topology is reported as UNRESOLVED for PM to lock against the author sheet.
        /// </summary>

/// <summary>
        /// The author's structural role for one cell, from LAYER A inputs only. IL-WORLD-004S-R23B.
        ///
        /// Every input is a fact about this cell's OWN 3x3: its four corner decisions, its N occupancy
        /// and its S occupancy. There is no run length, no offset, no thickness threshold, no walk and
        /// no lookahead parameter left in the signature, so no branch below can be reached by changing
        /// how far the surrounding ground happens to extend.
        ///
        /// The decision order is: the two R20 top corners, then the two R19 front corners, then the
        /// straight ladder. The two families cannot collide, because a top corner requires a Raised
        /// SOUTH neighbour and a front corner requires an open one.
        ///
        /// The straight ladder, and the R18/R11 oracles each line reproduces:
        ///
        ///   junction         -> JUNCTION_VERTICAL_CONTINUATION  the author's handover piece, Hills r2c4.
        ///       IL-WORLD-004S-R24, decided from the raw mask alone. It must be tested BEFORE the straight
        ///       ladder, because a junction cell has N and S both solid and would otherwise be swallowed by
        ///       the MIDDLE_SURFACE body row and drawn as the plain r1c0 - which is exactly the defect the
        ///       user's screenshot reported.
        ///
        ///   N open, S solid  -> TOP_SURFACE      the author rounded cap, Hills r0.
        ///       R11 3x3 and 5x3 rectangles: their top row is r0c0 | r0c1 ... | r0c2.
        ///       R18 1x2 .. 1x100 columns: their top cell is r0c3.
        ///
        ///   N solid, S open  -> FRONT_CLIFF      the author front wall, Hills r2, inside its own mask.
        ///       R11 3x3 and 5x3 rectangles: their bottom row is r2c0 | r2c1 ... | r2c2.
        ///       R18 1x2 .. 1x100 columns: their bottom cell is r2c3.
        ///
        ///   N solid, S solid  -> MIDDLE_SURFACE  the author body row, Hills r1, self repeating.
        ///       R11 3x3 and 5x3 rectangles: their middle row is r1c0 | r1c1 ... | r1c2.
        ///       R18 1x3 .. 1x100 columns: every interior cell is r1c3, and r1c3 repeats however deep
        ///       the column is. That repetition is the whole reason BODY cannot be a length decision.
        ///
        ///   N open, S open    -> SECOND_FRONT_CLIFF  the author one-cell-high band, Hills r3.
        ///       R11 and R18 w x 1 bands: the entire band is r3c0 | r3c1 ... | r3c2.
        ///       R18 1x1: r3c3.
        ///       There is deliberately NO exception here. An earlier revision added one, reading whether a
        ///       horizontal neighbour was a front row, and it was measured to break the frozen R19
        ///       oracle in 8 of 33 fixtures by turning the cell beside a front corner into an r2
        ///       terminal. See the branch itself for that measurement.
        ///
        /// SCOPE NOTE, MEASURED, AND THE ONE CONCESSION THIS LADDER MAKES. The author's own wide block
        /// is four rows tall (r0..r3), so a mass four or more cells deep used to paint its bottom two
        /// rows as r2 then r3. Under this ladder they paint as r1 then r2 instead.
        ///
        /// That concession is FORCED, not chosen. The two cells in question are the bottom row of a
        /// three deep mass and the bottom row of a four deep mass, and their raw 3x3 is byte identical
        /// (N, NE, NW, E, W Raised; S, SE, SW open): the difference between "three deep" and "four
        /// deep" is at (x, y+2), outside the 3x3 by construction. R11 locks the three deep case to r2
        /// and has never had a failing assertion, so r2 is the only answer a purely local Layer A can
        /// give. No existing oracle covers the four deep wide case, so no oracle breaks; what changes is
        /// the VISUAL of deep wide masses, which is a PM / Scene View judgement and is reported as
        /// UNITY_VISUAL_PENDING rather than asserted here.
        /// </summary>
        private static RaisedSurfaceRole RoleForLocal(
            bool leftCorner,
            bool rightCorner,
            bool leftTopCorner,
            bool rightTopCorner,
            bool junctionContinuation,
            bool northOccupied,
            bool southOccupied)
        {
            // IL-WORLD-004S-R20. Decided FIRST, before the R19 front corners and before the straight
            // ladder, so neither the cell's neighbours nor the extent of the ground can substitute a
            // different piece for a top corner.
            if (leftTopCorner)
            {
                return RaisedSurfaceRole.LEFT_TOP_CORNER;
            }

            if (rightTopCorner)
            {
                return RaisedSurfaceRole.RIGHT_TOP_CORNER;
            }

            // IL-WORLD-004S-R19. Also decided before the straight ladder. Both roles are at or above
            // FRONT_CLIFF, so a front corner draws its own wall inside its own logical mask and never
            // displaces a cliff one row south; it occupies exactly one visual cell, on its own cell.
            if (leftCorner)
            {
                return RaisedSurfaceRole.LEFT_CORNER;
            }

            if (rightCorner)
            {
                return RaisedSurfaceRole.RIGHT_CORNER;
            }

            // IL-WORLD-004S-R24. The junction handover, decided BEFORE the straight ladder for the same
            // reason: the junction cell has N and S both Raised, so the ladder would classify it as an
            // interior MIDDLE_SURFACE body and emit the plain r1c0. Testing it here is what makes the
            // author's r2c4 reachable at all, and it is still a pure function of the cell's own raw 3x3.
            if (junctionContinuation)
            {
                return RaisedSurfaceRole.JUNCTION_VERTICAL_CONTINUATION;
            }

            // The straight ladder. Four cases, four distinct cardinal patterns, no length anywhere.
            if (!northOccupied && southOccupied)
            {
                return RaisedSurfaceRole.TOP_SURFACE;
            }

            if (northOccupied && !southOccupied)
            {
                return RaisedSurfaceRole.FRONT_CLIFF;
            }

            // MEASURED, NOT GUESSED: this branch returns SECOND_FRONT_CLIFF, the author's r3 row, for a
            // cell that is its own top and front at once. An earlier revision of this card tried to make
            // it FRONT_CLIFF whenever a horizontal neighbour happened to be a front row, and that broke
            // the frozen R19 oracle: the cell immediately beside a front corner then became an r2
            // terminal instead of the author's r3 body, in 8 of 33 fixtures. The corner itself never
            // moved - it is the NEIGHBOUR that moved - which is exactly the kind of silent coupling the
            // run walk used to hide. So the exception is REMOVED and a cell is either its own top and
            // front, or it has ground above it. Four cases, four cardinal patterns, nothing else.
            if (!northOccupied && !southOccupied)
            {
                return RaisedSurfaceRole.SECOND_FRONT_CLIFF;
            }

            return RaisedSurfaceRole.MIDDLE_SURFACE;
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
