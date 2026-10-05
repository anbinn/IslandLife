namespace IslandLife.World.Terrain
{
    /// <summary>
    /// Vertical layer of a terrain cell, independent of its base <see cref="TerrainType"/>.
    ///
    /// A cell is a base terrain (Grass / Water / Sand / Empty) PLUS an elevation. A lowered pit
    /// is still Grass underneath, so elevation must not be encoded as a TerrainType value.
    ///
    /// Serialized as sbyte so the three states stay compact and stable:
    ///   Lowered = -1, Normal = 0, Raised = +1
    /// </summary>
    public enum ElevationLevel : sbyte
    {
        Lowered = -1,
        Normal = 0,
        Raised = 1,
    }
}