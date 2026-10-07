using System;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>The three surface rows of an author Hills composition.</summary>
    public enum HillCompositionRow
    {
        /// <summary>The author's rounded top edge, Hills sheet row r0.</summary>
        TOP_SURFACE = 0,

        /// <summary>The author's second surface row, Hills sheet row r1.</summary>
        MIDDLE_SURFACE = 1,

        /// <summary>The author's front cliff, Hills sheet row r2.</summary>
        FRONT_CLIFF = 2,

        /// <summary>
        /// The author's second front row, Hills sheet row r3. Added by IL-WORLD-004S-R12.
        /// IL-WORLD-004S-R4 reconstructed the author's own c0..c2 r0..r3 block 1:1, so a plateau four
        /// cells thick has proven art for all four of its rows. It is a completion of the same author
        /// stack, not a new composition and not an invented extension.
        /// </summary>
        SECOND_FRONT_CLIFF = 3,
    }

    /// <summary>
    /// Which part of a composition row a visual cell is. One real 16x16 author Sprite is emitted
    /// per cell; nothing is ever stretched, mirrored, rotated or resampled.
    /// </summary>
    public enum HillColumnSlot
    {
        LEFT_TERMINAL = 0,
        BODY = 1,
        RIGHT_TERMINAL = 2,

        /// <summary>The author's single-column variant, Hills c3.</summary>
        NARROW = 3,
    }

    /// <summary>
    /// The author's own Hills.png cells that IL-WORLD-004S-R4 and R5 verified, addressed by their
    /// top-left visual grid position on the sheet (r0 = sheet TOP row, c0 = sheet LEFT column).
    ///
    /// R5 measured the sheet geometry and proved the joins, so this asset is a table of proven
    /// author art, not a hand-authored interpretation. Slots:
    ///
    ///   wide, Hills c0..c2:
    ///     row r0  top surface     r0c0 left terminal | r0c1 body (self-repeating) | r0c2 right terminal
    ///     row r1  middle surface  r1c0 left terminal | r1c1 body (self-repeating) | r1c2 right terminal
    ///     row r2  front cliff     r2c0 left terminal | r2c1 body (self-repeating) | r2c2 right terminal
    ///     row r3  second front    r3c0 left terminal | r3c1 body (self-repeating) | r3c2 right terminal
    ///                             (IL-WORLD-004S-R12; R4 reconstructed the author's own c0..c2 r0..r3
    ///                              block 1:1, so this row is proven author art, not an extension)
    ///
    ///   narrow, Hills column c3, the author's own 1-wide instance, exactly 4 cells tall:
    ///     r0c3 top surface | r1c3 middle surface | r2c3 front cliff | r3c3 second cliff row
    ///
    ///   the two author CORNER primitives, IL-WORLD-004S-R19:
    ///     r3c4 left  corner  - a vertical left boundary turning east into the horizontal front
    ///     r3c7 right corner  - the horizontal front turning north into a vertical right boundary
    ///
    ///   the two author TOP CORNER primitives, IL-WORLD-004S-R20:
    ///     r0c4 left  top corner - a vertical left boundary reaching the TOP and turning east into
    ///                               the horizontal top structure
    ///     r0c7 right top corner - the horizontal top structure reaching its right end and turning
    ///                               south into a vertical right boundary
    ///
    /// IL-WORLD-004S-R19 CORRECTION. This set used to carry the note that r3c4..r3c7 is "an
    /// alternative author 4-wide band ... deliberately NOT wired in", i.e. a second proof of
    /// repeatability rather than a second composition. PM and the user then compared those cells
    /// against the source one Sprite at a time in Unity and locked their real meaning, and the two
    /// corners are what they are. The old reading was wrong and is superseded; it was never
    /// re-measured after the source was inspected directly, and it is recorded here so it is not
    /// re-derived from the sheet layout alone. Both cells are real author slices of the same 16x16
    /// grid, same row r3, same pivot and same PPU as the rest of the family, and neither is
    /// mirrored, rotated, stretched or resampled to reach its role.
    ///
    /// IL-WORLD-004S-R20 adds the row r0 pair on exactly the same basis: r0c4 is rect (64,128) and
    /// r0c7 is rect (112,128), the same 16x16 grid, pivot and PPU as the locked r0c1 at (16,128).
    /// The BODY between them is the author's r3c1 and is NOT re-specified: a one-cell-deep top
    /// structure is already the R18 r3 band body, so no new BODY slice was introduced and none of
    /// the R18 or R19 cells changed meaning.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AuthorHillsCompositionSet",
        menuName = "IslandLife/World/Author Hills Composition Set")]
    public sealed class AuthorHillsCompositionSet : ScriptableObject
    {
        [Header("Wide composition, Hills c0..c2 (row r0, top surface)")]
        [SerializeField]
        private Sprite hillsR0C0;

        [SerializeField]
        private Sprite hillsR0C1;

        [SerializeField]
        private Sprite hillsR0C2;

        [Header("Wide composition, Hills c0..c2 (row r1, middle surface)")]
        [SerializeField]
        private Sprite hillsR1C0;

        [SerializeField]
        private Sprite hillsR1C1;

        [SerializeField]
        private Sprite hillsR1C2;

        [Header("Wide composition, Hills c0..c2 (row r2, front cliff)")]
        [SerializeField]
        private Sprite hillsR2C0;

        [SerializeField]
        private Sprite hillsR2C1;

        [SerializeField]
        private Sprite hillsR2C2;

        [Header("Wide composition, Hills c0..c2 (row r3, second front row, IL-WORLD-004S-R4)")]
        [SerializeField]
        private Sprite hillsR3C0;

        [SerializeField]
        private Sprite hillsR3C1;

        [SerializeField]
        private Sprite hillsR3C2;

        [Header("Narrow composition, Hills column c3, the author's own 1-wide instance")]
        [SerializeField]
        private Sprite hillsR0C3;

        [SerializeField]
        private Sprite hillsR1C3;

        [SerializeField]
        private Sprite hillsR2C3;

        [SerializeField]
        private Sprite hillsR3C3;

        [Header("Author corner primitives, Hills row r3 (IL-WORLD-004S-R19)")]
        [SerializeField]
        private Sprite hillsR3C4;

        [SerializeField]
        private Sprite hillsR3C7;

        [Header("Author TOP corner primitives, Hills row r0 (IL-WORLD-004S-R20)")]
        [SerializeField]
        private Sprite hillsR0C4;

        [SerializeField]
        private Sprite hillsR0C7;

        /// <summary>
        /// The author's LEFT corner: a vertical left boundary turning east into the horizontal front.
        ///
        /// IL-WORLD-004S-R19. It is a whole-cell composition in its own right, not a terminal and not
        /// a body, so it is addressed directly rather than through <see cref="GetWide"/>'s row and
        /// slot lookup.
        /// </summary>
        public Sprite GetLeftCorner()
        {
            return hillsR3C4;
        }

        /// <summary>
        /// The author's RIGHT corner: the horizontal front turning north into a vertical right boundary.
        /// IL-WORLD-004S-R19.
        /// </summary>
        public Sprite GetRightCorner()
        {
            return hillsR3C7;
        }

        [Header("Author row r2 corner-wall pieces (IL-WORLD-004S-PJ)")]
        [SerializeField]
        private Sprite hillsR2C4;

        [SerializeField]
        private Sprite hillsR2C5;

        [Header("Author corner/junction components (IL-WORLD-004S-R21)")]
        [SerializeField]
        private Sprite hillsR3C5;

        /// <summary>
        /// The author's r2c4 left corner-wall piece. IL-WORLD-004S-PJ.
        ///
        /// R24 SUPERSEDES THE OLD "NOT REACHABLE" NOTE, and the correction is worth stating because the
        /// two notes disagreed. PJ said the P fixture's front was two separate runs so "which front cell
        /// takes r2c4" could not be derived from 3x3. R24 found the real junction instead: not a cell in
        /// the middle of a front, but the HANDOVER where a vertical boundary meets a platform that steps
        /// away. That topology is one bit - NE open against SE raised - and it IS derivable from 3x3.
        ///
        /// So the slice was never in doubt and it is now reachable: see
        /// <see cref="GetJunctionContinuation"/>, which returns this same field as a
        /// LOCKED_USER_VISUAL_ORACLE. <c>r2c5</c> remains unwired from any rule, unchanged.
        /// </summary>
        public Sprite GetR2C4()
        {
            return hillsR2C4;
        }

        /// <summary>The author's r2c5 internal connection piece. IL-WORLD-004S-PJ. See GetR2C4.</summary>
        public Sprite GetR2C5()
        {
            return hillsR2C5;
        }

        /// <summary>
        /// The author's junction continuation component: a vertical boundary handing over to a platform
        /// that steps away north and holds south. IL-WORLD-004S-R24.
        ///
        /// LOCKED_USER_VISUAL_ORACLE. The user confirmed this slice on screen by screenshot, so it is no
        /// longer HISTORICAL or UNRESOLVED. It is the SAME author slice that
        /// <see cref="GetCornerWithUpperContinuation"/> returns - R21 identified the right component and
        /// only its TEST was not local. The status change is on the semantics, not on the art: the same
        /// 16x16 slice, same rect, same pivot, same PPU, nothing added, mirrored, rotated or generated.
        ///
        /// Reachability changed from UNRESOLVED to REACHABLE. r2c4 was previously wired but emitted by no
        /// rule at all, because every rule that tried needed (x, y-2) or (x+2, y). It is now emitted from
        /// the cell's own raw 3x3 by
        /// <c>RaisedTopologyState.IsAuthorJunctionContinuation</c>, which reads six bits and nothing else.
        /// </summary>
        public Sprite GetJunctionContinuation()
        {
            return hillsR2C4;
        }

        /// <summary>
        /// The author's corner/junction component for a vertical boundary that does NOT continue upward.
        /// IL-WORLD-004S-R21. Slice rect (80,80), sheet row r3. PM locked this identity against the
        /// source sheet for the junction at (4,5) with raw mask 0xD0, canonical "East, South,
        /// SouthEast".
        /// </summary>
        public Sprite GetCornerNoUpperContinuation()
        {
            return hillsR3C5;
        }

        /// <summary>
        /// The author's corner/junction component for a vertical boundary that DOES continue upward.
        /// IL-WORLD-004S-R21. Slice rect (64,96), sheet row r2. PM locked this identity for the same
        /// junction with raw mask 0xD2, canonical "North, East, South, SouthEast".
        /// </summary>
        public Sprite GetCornerWithUpperContinuation()
        {
            return hillsR2C4;
        }

        /// <summary>
        /// The author's LEFT TOP corner: a vertical left boundary reaching the top and turning east
        /// into the horizontal top structure. IL-WORLD-004S-R20.
        /// </summary>
        public Sprite GetLeftTopCorner()
        {
            return hillsR0C4;
        }

        /// <summary>
        /// The author's RIGHT TOP corner: the horizontal top structure reaching its right end and
        /// turning south into a vertical right boundary. IL-WORLD-004S-R20.
        /// </summary>
        public Sprite GetRightTopCorner()
        {
            return hillsR0C7;
        }

        /// <summary>Returns the author Sprite for one cell of a wide composition row.</summary>
        public Sprite GetWide(HillCompositionRow row, HillColumnSlot slot)
        {
            switch (row)
            {
                case HillCompositionRow.TOP_SURFACE:
                    return slot == HillColumnSlot.LEFT_TERMINAL ? hillsR0C0
                        : slot == HillColumnSlot.RIGHT_TERMINAL ? hillsR0C2
                        : hillsR0C1;

                case HillCompositionRow.MIDDLE_SURFACE:
                    return slot == HillColumnSlot.LEFT_TERMINAL ? hillsR1C0
                        : slot == HillColumnSlot.RIGHT_TERMINAL ? hillsR1C2
                        : hillsR1C1;

                case HillCompositionRow.FRONT_CLIFF:
                    return slot == HillColumnSlot.LEFT_TERMINAL ? hillsR2C0
                        : slot == HillColumnSlot.RIGHT_TERMINAL ? hillsR2C2
                        : hillsR2C1;

                case HillCompositionRow.SECOND_FRONT_CLIFF:
                    return slot == HillColumnSlot.LEFT_TERMINAL ? hillsR3C0
                        : slot == HillColumnSlot.RIGHT_TERMINAL ? hillsR3C2
                        : hillsR3C1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(row), row, "Unknown composition row.");
            }
        }

        /// <summary>
        /// The author's narrow column cell at a given offset from the region's TOP edge. Offset 0 is
        /// the rounded top surface, 2 is the front cliff, 3 is the second cliff row.
        /// </summary>
        public Sprite GetNarrow(int offsetFromTop)
        {
            switch (offsetFromTop)
            {
                case 0:
                    return hillsR0C3;
                case 1:
                    return hillsR1C3;
                case 2:
                    return hillsR2C3;
                case 3:
                    return hillsR3C3;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(offsetFromTop),
                        offsetFromTop,
                        "The author's narrow column is 4 cells tall (Hills c3 r0..r3).");
            }
        }

        /// <summary>
        /// True when every slot the resolver can reach is assigned. A partially assigned set must
        /// fail loudly rather than silently drop cells.
        /// </summary>
        public bool IsComplete()
        {
            for (int row = 0; row <= 3; row++)
            {
                for (int slot = 0; slot <= 2; slot++)
                {
                    if (GetWide((HillCompositionRow)row, (HillColumnSlot)slot) == null)
                    {
                        return false;
                    }
                }
            }

            for (int offset = 0; offset <= 3; offset++)
            {
                if (GetNarrow(offset) == null)
                {
                    return false;
                }
            }

            if (GetLeftCorner() == null || GetRightCorner() == null)
            {
                return false;
            }

            if (GetLeftTopCorner() == null || GetRightTopCorner() == null)
            {
                return false;
            }

            if (GetR2C4() == null || GetR2C5() == null)
            {
                return false;
            }

            if (GetCornerNoUpperContinuation() == null
                || GetCornerWithUpperContinuation() == null)
            {
                return false;
            }

            // IL-WORLD-004S-R24. The junction continuation must be assigned, because it is now emitted
            // from production. It shares the r2c4 field with GetCornerWithUpperContinuation, so this is
            // a second route to a slice that was already checked above; it is asserted anyway, because a
            // reachable role whose slice is null would silently drop the cell rather than report it.
            if (GetJunctionContinuation() == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>Human-readable audit of which slots are still unassigned.</summary>
        public string DescribeMissingSlots()
        {
            var missing = new System.Collections.Generic.List<string>();
            for (int row = 0; row <= 3; row++)
            {
                for (int slot = 0; slot <= 2; slot++)
                {
                    if (GetWide((HillCompositionRow)row, (HillColumnSlot)slot) == null)
                    {
                        missing.Add($"wide r{row} slot{slot}");
                    }
                }
            }

            for (int offset = 0; offset <= 3; offset++)
            {
                if (GetNarrow(offset) == null)
                {
                    missing.Add($"narrow offset{offset}");
                }
            }

            if (GetLeftCorner() == null)
            {
                missing.Add("left corner r3c4");
            }

            if (GetRightCorner() == null)
            {
                missing.Add("right corner r3c7");
            }

            if (GetLeftTopCorner() == null)
            {
                missing.Add("left top corner r0c4");
            }

            if (GetRightTopCorner() == null)
            {
                missing.Add("right top corner r0c7");
            }

            if (GetR2C4() == null)
            {
                missing.Add("r2 left corner-wall r2c4");
            }

            if (GetR2C5() == null)
            {
                missing.Add("r2 internal connection r2c5");
            }

            if (GetCornerNoUpperContinuation() == null)
            {
                missing.Add("corner no upper continuation r3c5");
            }

            if (GetCornerWithUpperContinuation() == null)
            {
                missing.Add("corner with upper continuation r2c4");
            }

            if (GetJunctionContinuation() == null)
            {
                missing.Add("junction continuation r2c4");
            }

            return missing.Count == 0 ? "none" : string.Join(", ", missing);
        }
    }
}