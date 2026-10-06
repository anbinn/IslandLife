using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// Whether a connected region of Raised cells has a pixel-verified Author Hills Composition.
    /// IL-WORLD-004S-R6, semantics locked by IL-WORLD-004S-R11.
    ///
    /// This describes OUR PROVEN COVERAGE of Hills.png. It does not describe the author's intent and
    /// it does not judge the user's data. A null Support means "no verified composition yet", never
    /// "invalid terrain" and never "the author forbade this".
    ///
    /// Only these values may be produced; there is deliberately no "fallback".
    /// </summary>
    public enum RaisedRegionSupport
    {
        /// <summary>
        /// Solid rectangle, width &gt;= 3. Expressible with the verified terminal/body rows.
        /// PROVEN by IL-WORLD-004S-R5 and shipped by R6/R9.
        /// </summary>
        SUPPORTED_RECTANGLE,

        /// <summary>
        /// Single-column region, height &lt;= 4. Expressible with the author's own narrow column.
        /// PROVEN by IL-WORLD-004S-R5 and shipped by R6/R9.
        /// </summary>
        SUPPORTED_NARROW,
    }

    /// <summary>
    /// Why a connected Raised region currently has no pixel-verified Author Hills Composition.
    ///
    /// READ THIS BEFORE CHANGING ANY WORDING HERE. IL-WORLD-004S-R11 locks the semantics:
    ///   - This is a statement about our PROOF COVERAGE, not about the author.
    ///     NOT_PROVEN does NOT mean "the author never drew this".
    ///   - It is not a statement about the user's data. The Raised cells are valid, are stored, and
    ///     are never removed, snapped, filled or rejected because of one of these reasons.
    ///   - No value is ever used to guess, approximate, mirror, rotate or stretch a Sprite.
    ///
    /// The status name shown to the user is UNRESOLVED_HILLS_COMPOSITION; the values below are the
    /// secondary technical detail.
    /// </summary>
    public enum RaisedRegionUnsupportedReason
    {
        /// <summary>
        /// Two cells wide. No 2-wide composition has been pixel-verified yet. A terminal|terminal
        /// join has not been proven to be an author adjacency, so nothing is emitted.
        /// </summary>
        TWO_CELL_WIDTH,

        /// <summary>
        /// Cell count does not fill its bounding box, so the outline turns or notches. Turn and notch
        /// compositions (convex and concave) are NOT_PROVEN; see the capability report.
        /// </summary>
        NON_RECTANGULAR_REGION,

        /// <summary>Rectangle taller than the vertical stack that has been pixel-verified.</summary>
        HEIGHT_NOT_PROVEN,

        /// <summary>Width beyond the runs that have been pixel-verified.</summary>
        WIDTH_NOT_PROVEN,
    }

    /// <summary>
    /// User-facing vocabulary for a region whose Raised data is valid but whose hills visual has no
    /// verified composition. IL-WORLD-004S-R11.
    ///
    /// The PRIMARY message is always <see cref="StatusCode"/>, which says the data is fine and only
    /// the visual is unresolved. The technical reason is secondary detail for whoever extends the
    /// grammar later. Keeping both in one place stops the editor overlay, the status bar and any
    /// future tool from disagreeing about what is unresolved and why.
    /// </summary>
    public static class RaisedRegionReasons
    {
        /// <summary>
        /// The single primary status for "Raised logical data is valid, hills visual unresolved".
        /// Deliberately does not contain the words "invalid", "illegal" or "unsupported shape".
        /// </summary>
        public const string StatusCode = "UNRESOLVED_HILLS_COMPOSITION";

        /// <summary>The secondary technical reason. Named after the enum so the two cannot drift.</summary>
        public static string ShortCode(RaisedRegionUnsupportedReason reason)
        {
            switch (reason)
            {
                case RaisedRegionUnsupportedReason.TWO_CELL_WIDTH:
                    return "TWO_CELL_WIDTH";
                case RaisedRegionUnsupportedReason.NON_RECTANGULAR_REGION:
                    return "NON_RECTANGULAR_REGION";
                case RaisedRegionUnsupportedReason.HEIGHT_NOT_PROVEN:
                    return "HEIGHT_NOT_PROVEN";
                case RaisedRegionUnsupportedReason.WIDTH_NOT_PROVEN:
                    return "WIDTH_NOT_PROVEN";
                default:
                    return "UNRESOLVED_HILLS_COMPOSITION";
            }
        }

        /// <summary>
        /// One line describing the gap in our PROOF COVERAGE, phrased so it cannot be read as a
        /// verdict on the author's art or on the user's map. Never prescribes a change to the map.
        /// </summary>
        public static string Explain(RaisedRegionUnsupportedReason reason)
        {
            switch (reason)
            {
                case RaisedRegionUnsupportedReason.TWO_CELL_WIDTH:
                    return "no 2-cell-wide composition verified yet; your Raised cells are kept";
                case RaisedRegionUnsupportedReason.NON_RECTANGULAR_REGION:
                    return "turn, notch and hole compositions not verified yet; "
                        + "your Raised cells are kept";
                case RaisedRegionUnsupportedReason.HEIGHT_NOT_PROVEN:
                    return "taller than the verified vertical stack; your Raised cells are kept";
                case RaisedRegionUnsupportedReason.WIDTH_NOT_PROVEN:
                    return "wider than the verified run; your Raised cells are kept";
                default:
                    return "no verified composition yet; your Raised cells are kept";
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

        /// <summary>Non-null when a pixel-verified Author Hills Composition exists for this region.</summary>
        public RaisedRegionSupport? Support { get; }

        /// <summary>
        /// Non-null exactly when <see cref="Support"/> is null, i.e. when the Raised data is valid
        /// but no verified hills composition exists. IL-WORLD-004S-R11.
        /// </summary>
        public RaisedRegionUnsupportedReason? UnsupportedReason { get; }

        /// <summary>True when a pixel-verified Author Hills Composition exists for this region.</summary>
        public bool IsSupported => Support.HasValue;

        public override string ToString()
        {
            string shape = $"region x{MinX}..{MaxX} y{MinY}..{MaxY} {Width}x{Height} cells={CellCount}";
            if (Support.HasValue)
            {
                return $"{shape} PROVEN_COMPOSITION {Support.Value}";
            }

            return $"{shape} {RaisedRegionReasons.StatusCode} "
                + $"{RaisedRegionReasons.ShortCode(UnsupportedReason.Value)}";
        }
    }
}