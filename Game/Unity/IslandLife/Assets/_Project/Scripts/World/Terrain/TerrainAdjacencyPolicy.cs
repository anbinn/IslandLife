using System;

namespace IslandLife.World.Terrain
{
    public static class TerrainAdjacencyPolicy
    {
        public static bool CanTouch(TerrainType first, TerrainType second)
        {
            ValidateTerrainType(first, nameof(first));
            ValidateTerrainType(second, nameof(second));

            return !((first == TerrainType.Grass && second == TerrainType.Water)
                     || (first == TerrainType.Water && second == TerrainType.Grass));
        }

        private static void ValidateTerrainType(TerrainType terrainType, string parameterName)
        {
            switch (terrainType)
            {
                case TerrainType.Empty:
                case TerrainType.Water:
                case TerrainType.Grass:
                case TerrainType.Sand:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
