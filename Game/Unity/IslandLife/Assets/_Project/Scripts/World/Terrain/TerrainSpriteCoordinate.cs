namespace IslandLife.World.Terrain
{
    public readonly struct TerrainSpriteCoordinate
    {
        public int Row { get; }

        public int Column { get; }

        public TerrainSpriteCoordinate(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }
}
