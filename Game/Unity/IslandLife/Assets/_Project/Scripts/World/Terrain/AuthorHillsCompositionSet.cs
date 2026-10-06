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
    /// The alternative author 4-wide band (r3c4..r3c7) is deliberately NOT wired in: it is a
    /// second proof of repeatability, not a second composition, and R6 must not pick between two
    /// author bands for the same logical shape.
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

            return missing.Count == 0 ? "none" : string.Join(", ", missing);
        }
    }
}