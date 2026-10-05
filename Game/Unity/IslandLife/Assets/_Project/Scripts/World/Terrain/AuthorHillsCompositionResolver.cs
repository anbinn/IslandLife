using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// Turns a classified Raised region into the author's Hills visual tiles.
    /// IL-WORLD-004S-R6.
    ///
    /// The mapping is explicit and one-way:
    ///     logical Raised region -> author composition -> visual tile positions
    ///
    /// It never writes to the logical mask, never picks a Sprite for an unsupported region, and
    /// never stretches, mirrors, rotates or resamples author art. Each emitted tile is exactly one
    /// of the 13 author Sprites held by <see cref="AuthorHillsCompositionSet"/>.
    ///
    /// Layout, straight from R5's verified author compositions:
    ///
    ///   Wide, height 3 (all inside the logical mask; this is the author's own r0..r2):
    ///     logical y = MaxY     -> TOP_SURFACE     r0c0 | r0c1 x(W-2) | r0c2
    ///     logical y = MaxY - 1 -> MIDDLE_SURFACE  r1c0 | r1c1 x(W-2) | r1c2
    ///     logical y = MinY     -> FRONT_CLIFF     r2c0 | r2c1 x(W-2) | r2c2
    ///
    ///   Wide, height 2 (the cliff is drawn one visual row SOUTH of the logical mask):
    ///     logical y = MaxY     -> TOP_SURFACE     r0c0 | r0c1 x(W-2) | r0c2
    ///     logical y = MinY     -> MIDDLE_SURFACE  r1c0 | r1c1 x(W-2) | r1c2
    ///     logical y = MinY - 1 -> FRONT_CLIFF     r2c0 | r2c1 x(W-2) | r2c2   OUTSIDE THE MASK
    ///
    ///   Narrow, the author's own c3 column, height 1..4:
    ///     height 1 -> r0c3 at MaxY,                 r2c3 at MinY - 1   (OUTSIDE THE MASK)
    ///     height 2 -> r0c3 at MaxY, r1c3 at MinY,   r2c3 at MinY - 1   (OUTSIDE THE MASK)
    ///     height 3 -> r0c3 at MaxY, r1c3 at MaxY-1, r2c3 at MinY
    ///     height 4 -> r0c3 at MaxY, r1c3 at MaxY-1, r2c3 at MinY+1, r3c3 at MinY
    /// </summary>
    public static class AuthorHillsCompositionResolver
    {
        /// <summary>
        /// Appends the author visual tiles for one region. Returns false, and appends nothing, when
        /// the region is not supported or the composition set is incomplete. A failure is a reported
        /// state, never a guess and never an exception, so a single odd region cannot break editing
        /// of the rest of the map.
        /// </summary>
        public static bool TryResolve(
            RaisedRegion region,
            AuthorHillsCompositionSet compositionSet,
            List<HillVisualTile> output,
            out string failure)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (compositionSet == null)
            {
                throw new ArgumentNullException(nameof(compositionSet));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (!region.IsSupported)
            {
                failure = $"UNSUPPORTED_AUTHOR_GRAMMAR {region.UnsupportedReason}";
                return false;
            }

            if (!compositionSet.IsComplete())
            {
                failure =
                    $"COMPOSITION_SET_INCOMPLETE missing {compositionSet.DescribeMissingSlots()}";
                return false;
            }

            if (region.Support == RaisedRegionSupport.SUPPORTED_NARROW)
            {
                ResolveNarrow(region, compositionSet, output);
            }
            else
            {
                ResolveRectangle(region, compositionSet, output);
            }

            failure = null;
            return true;
        }

        /// <summary>
        /// Visual footprint a supported region will occupy, without resolving Sprites. Rows are
        /// counted southward from the region's top, so a region that draws below its own mask still
        /// reports the extra row honestly.
        /// </summary>
        public static bool TryGetVisualFootprint(
            RaisedRegion region,
            out int visualWidth,
            out int visualHeight,
            out int rowsBelowLogicalMask)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            visualWidth = 0;
            visualHeight = 0;
            rowsBelowLogicalMask = 0;

            if (!region.IsSupported)
            {
                return false;
            }

            visualWidth = region.Width;
            visualHeight = region.Height;
            rowsBelowLogicalMask = 0;

            if (region.Support == RaisedRegionSupport.SUPPORTED_NARROW)
            {
                switch (region.Height)
                {
                    case 1:
                        visualHeight = 2;
                        rowsBelowLogicalMask = 1;
                        return true;
                    case 2:
                        visualHeight = 3;
                        rowsBelowLogicalMask = 1;
                        return true;
                    case 3:
                        visualHeight = 3;
                        rowsBelowLogicalMask = 0;
                        return true;
                    default:
                        visualHeight = 4;
                        rowsBelowLogicalMask = 0;
                        return true;
                }
            }

            if (region.Height == 2)
            {
                visualHeight = 3;
                rowsBelowLogicalMask = 1;
            }

            return true;
        }

        private static void ResolveRectangle(
            RaisedRegion region,
            AuthorHillsCompositionSet compositionSet,
            List<HillVisualTile> output)
        {
            int topSurfaceY;
            int middleSurfaceY;
            int cliffY;

            if (region.Height == 3)
            {
                topSurfaceY = region.MaxY;
                middleSurfaceY = region.MaxY - 1;
                cliffY = region.MinY;
            }
            else
            {
                // Height 2. The cliff is drawn one row south of the logical mask and is NOT Raised.
                topSurfaceY = region.MaxY;
                middleSurfaceY = region.MinY;
                cliffY = region.MinY - 1;
            }

            EmitRow(region, compositionSet, output, topSurfaceY,
                HillCompositionRow.TOP_SURFACE);
            EmitRow(region, compositionSet, output, middleSurfaceY,
                HillCompositionRow.MIDDLE_SURFACE);
            EmitRow(region, compositionSet, output, cliffY, HillCompositionRow.FRONT_CLIFF);
        }

        private static void EmitRow(
            RaisedRegion region,
            AuthorHillsCompositionSet compositionSet,
            List<HillVisualTile> output,
            int visualY,
            HillCompositionRow row)
        {
            bool outsideMask = visualY < region.MinY;

            for (int x = region.MinX; x <= region.MaxX; x++)
            {
                HillColumnSlot slot;
                if (x == region.MinX)
                {
                    slot = HillColumnSlot.LEFT_TERMINAL;
                }
                else if (x == region.MaxX)
                {
                    slot = HillColumnSlot.RIGHT_TERMINAL;
                }
                else
                {
                    slot = HillColumnSlot.BODY;
                }

                output.Add(new HillVisualTile(
                    new Vector3Int(x, visualY, 0),
                    outsideMask,
                    compositionSet.GetWide(row, slot),
                    row,
                    slot));
            }
        }

        private static void ResolveNarrow(
            RaisedRegion region,
            AuthorHillsCompositionSet compositionSet,
            List<HillVisualTile> output)
        {
            int x = region.MinX;

            switch (region.Height)
            {
                case 1:
                    // Only the author's top surface plus its front cliff. The author drew no
                    // two-row-tall narrow instance, so the middle surface row is omitted rather
                    // than invented.
                    EmitNarrow(region, compositionSet, output, 0, region.MaxY);
                    EmitNarrow(region, compositionSet, output, 2, region.MinY - 1);
                    return;

                case 2:
                    EmitNarrow(region, compositionSet, output, 0, region.MaxY);
                    EmitNarrow(region, compositionSet, output, 1, region.MinY);
                    EmitNarrow(region, compositionSet, output, 2, region.MinY - 1);
                    return;

                case 3:
                    EmitNarrow(region, compositionSet, output, 0, region.MaxY);
                    EmitNarrow(region, compositionSet, output, 1, region.MaxY - 1);
                    EmitNarrow(region, compositionSet, output, 2, region.MinY);
                    return;

                case 4:
                    // The author's own c3 r0..r3 column, mapped one to one.
                    EmitNarrow(region, compositionSet, output, 0, region.MaxY);
                    EmitNarrow(region, compositionSet, output, 1, region.MaxY - 1);
                    EmitNarrow(region, compositionSet, output, 2, region.MinY + 1);
                    EmitNarrow(region, compositionSet, output, 3, region.MinY);
                    return;

                default:
                    throw new InvalidOperationException(
                        $"A narrow Raised region of height {region.Height} should have been "
                        + "classified as HEIGHT_NOT_PROVEN before reaching the resolver.");
            }
        }

        private static void EmitNarrow(
            RaisedRegion region,
            AuthorHillsCompositionSet compositionSet,
            List<HillVisualTile> output,
            int offsetFromTop,
            int visualY)
        {
            bool outsideMask = visualY < region.MinY;
            HillCompositionRow row = offsetFromTop < 2
                ? (offsetFromTop == 0
                    ? HillCompositionRow.TOP_SURFACE
                    : HillCompositionRow.MIDDLE_SURFACE)
                : HillCompositionRow.FRONT_CLIFF;

            output.Add(new HillVisualTile(
                new Vector3Int(region.MinX, visualY, 0),
                outsideMask,
                compositionSet.GetNarrow(offsetFromTop),
                row,
                HillColumnSlot.NARROW));
        }
    }
}