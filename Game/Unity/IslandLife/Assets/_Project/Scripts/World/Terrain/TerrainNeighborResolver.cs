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

        public static TerrainNeighborMask ResolveLandMask(
            TerrainGridData grid,
            int x,
            int y)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            TerrainType center = grid.GetTerrain(x, y);
            if (!IsLand(center))
            {
                return TerrainNeighborMask.None;
            }

            TerrainNeighborMask mask = TerrainNeighborMask.None;

            if (IsLandAt(grid, (long)x - 1, (long)y + 1))
            {
                mask |= TerrainNeighborMask.NorthWest;
            }

            if (IsLandAt(grid, x, (long)y + 1))
            {
                mask |= TerrainNeighborMask.North;
            }

            if (IsLandAt(grid, (long)x + 1, (long)y + 1))
            {
                mask |= TerrainNeighborMask.NorthEast;
            }

            if (IsLandAt(grid, (long)x - 1, y))
            {
                mask |= TerrainNeighborMask.West;
            }

            if (IsLandAt(grid, (long)x + 1, y))
            {
                mask |= TerrainNeighborMask.East;
            }

            if (IsLandAt(grid, (long)x - 1, (long)y - 1))
            {
                mask |= TerrainNeighborMask.SouthWest;
            }

            if (IsLandAt(grid, x, (long)y - 1))
            {
                mask |= TerrainNeighborMask.South;
            }

            if (IsLandAt(grid, (long)x + 1, (long)y - 1))
            {
                mask |= TerrainNeighborMask.SouthEast;
            }

            return mask;
        }

        private static bool IsLandAt(TerrainGridData grid, long x, long y)
        {
            return x >= int.MinValue
                && x <= int.MaxValue
                && y >= int.MinValue
                && y <= int.MaxValue
                && IsLand(grid.GetTerrain((int)x, (int)y));
        }

        private static bool IsLand(TerrainType terrainType)
        {
            return terrainType == TerrainType.Sand || terrainType == TerrainType.Grass;
        }
    }
}
