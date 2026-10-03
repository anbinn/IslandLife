namespace IslandLife.World.Terrain
{
    public static class TerrainMaskNormalizer
    {
        public static TerrainNeighborMask Normalize(TerrainNeighborMask raw)
        {
            TerrainNeighborMask normalized = raw
                & (TerrainNeighborMask.North
                   | TerrainNeighborMask.West
                   | TerrainNeighborMask.East
                   | TerrainNeighborMask.South);

            if ((raw & (TerrainNeighborMask.NorthWest
                        | TerrainNeighborMask.North
                        | TerrainNeighborMask.West))
                == (TerrainNeighborMask.NorthWest
                    | TerrainNeighborMask.North
                    | TerrainNeighborMask.West))
            {
                normalized |= TerrainNeighborMask.NorthWest;
            }

            if ((raw & (TerrainNeighborMask.NorthEast
                        | TerrainNeighborMask.North
                        | TerrainNeighborMask.East))
                == (TerrainNeighborMask.NorthEast
                    | TerrainNeighborMask.North
                    | TerrainNeighborMask.East))
            {
                normalized |= TerrainNeighborMask.NorthEast;
            }

            if ((raw & (TerrainNeighborMask.SouthWest
                        | TerrainNeighborMask.South
                        | TerrainNeighborMask.West))
                == (TerrainNeighborMask.SouthWest
                    | TerrainNeighborMask.South
                    | TerrainNeighborMask.West))
            {
                normalized |= TerrainNeighborMask.SouthWest;
            }

            if ((raw & (TerrainNeighborMask.SouthEast
                        | TerrainNeighborMask.South
                        | TerrainNeighborMask.East))
                == (TerrainNeighborMask.SouthEast
                    | TerrainNeighborMask.South
                    | TerrainNeighborMask.East))
            {
                normalized |= TerrainNeighborMask.SouthEast;
            }

            return normalized;
        }
    }
}
