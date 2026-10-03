using System;

namespace IslandLife.World.Terrain
{
    public static class TerrainNeighborResolver
    {
        public static TerrainNeighborMask ResolveSameTerrainMask(
            TerrainGridData grid,
            int x,
            int y)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!grid.IsInside(x, y))
            {
                return TerrainNeighborMask.None;
            }

            TerrainType center = grid.GetTerrain(x, y);
            if (center == TerrainType.Empty)
            {
                return TerrainNeighborMask.None;
            }

            TerrainNeighborMask mask = TerrainNeighborMask.None;

            if (grid.IsInside(x - 1, y + 1) && grid.GetTerrain(x - 1, y + 1) == center)
            {
                mask |= TerrainNeighborMask.NorthWest;
            }

            if (grid.IsInside(x, y + 1) && grid.GetTerrain(x, y + 1) == center)
            {
                mask |= TerrainNeighborMask.North;
            }

            if (grid.IsInside(x + 1, y + 1) && grid.GetTerrain(x + 1, y + 1) == center)
            {
                mask |= TerrainNeighborMask.NorthEast;
            }

            if (grid.IsInside(x - 1, y) && grid.GetTerrain(x - 1, y) == center)
            {
                mask |= TerrainNeighborMask.West;
            }

            if (grid.IsInside(x + 1, y) && grid.GetTerrain(x + 1, y) == center)
            {
                mask |= TerrainNeighborMask.East;
            }

            if (grid.IsInside(x - 1, y - 1) && grid.GetTerrain(x - 1, y - 1) == center)
            {
                mask |= TerrainNeighborMask.SouthWest;
            }

            if (grid.IsInside(x, y - 1) && grid.GetTerrain(x, y - 1) == center)
            {
                mask |= TerrainNeighborMask.South;
            }

            if (grid.IsInside(x + 1, y - 1) && grid.GetTerrain(x + 1, y - 1) == center)
            {
                mask |= TerrainNeighborMask.SouthEast;
            }

            return mask;
        }
    }
}
