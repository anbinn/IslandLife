using System;

namespace IslandLife.World.Terrain
{
    public static class TerrainGridValidator
    {
        public static bool IsValid(TerrainGridData grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    TerrainType centerTerrain = grid.GetTerrain(x, y);

                    if (grid.IsInside(x, y + 1)
                        && !TerrainAdjacencyPolicy.CanTouch(
                            centerTerrain,
                            grid.GetTerrain(x, y + 1)))
                    {
                        return false;
                    }

                    if (grid.IsInside(x + 1, y)
                        && !TerrainAdjacencyPolicy.CanTouch(
                            centerTerrain,
                            grid.GetTerrain(x + 1, y)))
                    {
                        return false;
                    }

                    if (grid.IsInside(x, y - 1)
                        && !TerrainAdjacencyPolicy.CanTouch(
                            centerTerrain,
                            grid.GetTerrain(x, y - 1)))
                    {
                        return false;
                    }

                    if (grid.IsInside(x - 1, y)
                        && !TerrainAdjacencyPolicy.CanTouch(
                            centerTerrain,
                            grid.GetTerrain(x - 1, y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
