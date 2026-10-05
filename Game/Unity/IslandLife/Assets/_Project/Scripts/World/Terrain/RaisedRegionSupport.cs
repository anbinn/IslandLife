using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// How well the author's Hills.png grammar expresses a connected region of Raised cells.
    /// IL-WORLD-004S-R6. Only these values may be produced; there is deliberately no "fallback".
    /// </summary>
    public enum RaisedRegionSupport
    {
        /// <summary>Solid rectangle, width &gt;= 3. Expressible with the verified terminal/body rows.</summary>
        SUPPORTED_RECTANGLE,

        /// <summary>Single-column region, height &lt;= 4. Expressible with the author's own narrow column.</summary>
        SUPPORTED_NARROW,
    }

    /// <summary>
    /// Why a connected Raised region cannot be drawn with author material. Every value here is a
    /// decision the author never made, not a defect in the implementation. No value is ever used to
    /// guess a Sprite.
    /// </summary>
    public enum RaisedRegionUnsupportedReason
    {
        /// <summary>Two cells wide. The author drew no 2-wide instance; terminal|terminal is not an author adjacency.</summary>
        TWO_CELL_WIDTH,

        /// <summary>Cell count does not fill its bounding box, so the outline turns or notches.</summary>
        NON_RECTANGULAR_REGION,

        /// <summary>Rectangle taller than the author's proven stack.</summary>
        HEIGHT_NOT_PROVEN,

        /// <summary>Width the author never drew.</summary>
        WIDTH_NOT_PROVEN,
    }

    /// <summary>
    /// A short, stable, uppercase code for each refusal, suitable for a Scene View label.
    /// Kept next to the enum so the editor overlay and any diagnostic can never disagree about
    /// why a region was refused. IL-WORLD-004S-R9.
    /// </summary>
    public static class RaisedRegionReasons
    {
        public static string ShortCode(RaisedRegionUnsupportedReason reason)
        {
            switch (reason)
            {
                case RaisedRegionUnsupportedReason.TWO_CELL_WIDTH:
                    return "WIDTH_2";
                case RaisedRegionUnsupportedReason.NON_RECTANGULAR_REGION:
                    return "NON_RECTANGULAR";
                case RaisedRegionUnsupportedReason.HEIGHT_NOT_PROVEN:
                    return "HEIGHT_NOT_SUPPORTED";
                case RaisedRegionUnsupportedReason.WIDTH_NOT_PROVEN:
                    return "WIDTH_NOT_SUPPORTED";
                default:
                    return "UNSUPPORTED";
            }
        }

        /// <summary>One line a user can act on, without prescribing a change to their map.</summary>
        public static string Explain(RaisedRegionUnsupportedReason reason)
        {
            switch (reason)
            {
                case RaisedRegionUnsupportedReason.TWO_CELL_WIDTH:
                    return "two cells wide - the author's Hills.png has no 2-wide instance";
                case RaisedRegionUnsupportedReason.NON_RECTANGULAR_REGION:
                    return "the outline is not a solid rectangle - it has a notch or a step";
                case RaisedRegionUnsupportedReason.HEIGHT_NOT_PROVEN:
                    return "taller than the author's proven vertical stack";
                case RaisedRegionUnsupportedReason.WIDTH_NOT_PROVEN:
                    return "wider than the author's proven run";
                default:
                    return "not expressible with the author's verified Hills cells";
            }
        }
    }
    /// <summary>One connected group of Raised logical cells, plus its classification.</summary>
    public sealed class RaisedRegion
    {
        private readonly List<Vector2Int> cells;

        internal RaisedRegion(
            List<Vector2Int> cells,
            int minX,
            int minY,
            int maxX,
            int maxY,
            RaisedRegionSupport? support,
            RaisedRegionUnsupportedReason? reason)
        {
            this.cells = cells;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            Support = support;
            UnsupportedReason = reason;
        }

        public int MinX { get; }

        public int MinY { get; }

        public int MaxX { get; }

        public int MaxY { get; }

        public int Width => MaxX - MinX + 1;

        public int Height => MaxY - MinY + 1;

        public int CellCount => cells.Count;

        /// <summary>Every Raised logical cell in this region, in flood-fill discovery order.</summary>
        public IReadOnlyList<Vector2Int> Cells => cells;

        /// <summary>Non-null when the author's grammar can express this region.</summary>
        public RaisedRegionSupport? Support { get; }

        /// <summary>Non-null exactly when <see cref="Support"/> is null.</summary>
        public RaisedRegionUnsupportedReason? UnsupportedReason { get; }

        public bool IsSupported => Support.HasValue;

        public override string ToString()
        {
            string shape = $"region x{MinX}..{MaxX} y{MinY}..{MaxY} {Width}x{Height} cells={CellCount}";
            if (Support.HasValue)
            {
                return $"{shape} SUPPORTED {Support.Value}";
            }

            return $"{shape} UNSUPPORTED_AUTHOR_GRAMMAR {UnsupportedReason}";
        }
    }
}