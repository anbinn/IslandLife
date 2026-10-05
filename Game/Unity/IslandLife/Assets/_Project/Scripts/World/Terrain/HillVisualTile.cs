using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>
    /// One author Sprite to draw at one visual Tilemap position. IL-WORLD-004S-R6.
    ///
    /// <see cref="VisualPosition"/> is a VISUAL position and is deliberately not required to be a
    /// logical Raised cell. <see cref="OutsideLogicalMask"/> records exactly that case, which R5
    /// proved is real: the author's stack is always two surface rows above the cliff row, so a
    /// two-row-tall plateau must draw its cliff one visual row south of its logical mask. The
    /// logical Raised data is never extended to make the art fit.
    /// </summary>
    public readonly struct HillVisualTile
    {
        public HillVisualTile(
            Vector3Int visualPosition,
            bool outsideLogicalMask,
            Sprite sprite,
            HillCompositionRow row,
            HillColumnSlot slot)
        {
            VisualPosition = visualPosition;
            OutsideLogicalMask = outsideLogicalMask;
            Sprite = sprite;
            Row = row;
            Slot = slot;
        }

        /// <summary>Tilemap position the author Sprite is drawn at. Not necessarily a Raised cell.</summary>
        public Vector3Int VisualPosition { get; }

        /// <summary>True when this position is south of the region's logical Raised mask.</summary>
        public bool OutsideLogicalMask { get; }

        public Sprite Sprite { get; }

        /// <summary>Which surface row of the author composition this came from.</summary>
        public HillCompositionRow Row { get; }

        /// <summary>Which slot within that row. <see cref="HillColumnSlot.NARROW"/> for the c3 column.</summary>
        public HillColumnSlot Slot { get; }

        /// <summary>Traceable description used by the test harness, e.g. "r2c1 / FRONT_CLIFF / BODY".</summary>
        public override string ToString()
        {
            return $"{VisualPosition} {(OutsideLogicalMask ? "OUTSIDE-MASK" : "in-mask")} "
                + $"{Row}/{Slot} sprite='{Sprite.name}'";
        }
    }
}