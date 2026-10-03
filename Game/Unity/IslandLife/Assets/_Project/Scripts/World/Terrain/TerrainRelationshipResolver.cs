using System;

namespace IslandLife.World.Terrain
{
    public static class TerrainRelationshipResolver
    {
        public static TerrainRelationship Compare(TerrainType first, TerrainType second)
        {
            int firstPriority = GetPriority(first);
            int secondPriority = GetPriority(second);

            if (firstPriority == secondPriority)
            {
                return TerrainRelationship.Same;
            }

            return firstPriority > secondPriority
                ? TerrainRelationship.Higher
                : TerrainRelationship.Lower;
        }

        private static int GetPriority(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.Empty:
                    return 0;
                case TerrainType.Water:
                    return 1;
                case TerrainType.Sand:
                    return 2;
                case TerrainType.Grass:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(terrainType));
            }
        }
    }
}
