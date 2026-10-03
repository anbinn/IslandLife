using System;

namespace IslandLife.World.Terrain
{
    public class TerrainGridData
    {
        private readonly TerrainType[,] terrainTypes;

        public int Width { get; }

        public int Height { get; }

        public TerrainGridData(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            terrainTypes = new TerrainType[width, height];
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public TerrainType GetTerrain(int x, int y)
        {
            return IsInside(x, y) ? terrainTypes[x, y] : TerrainType.Empty;
        }

        public void SetTerrain(int x, int y, TerrainType terrainType)
        {
            if (IsInside(x, y))
            {
                terrainTypes[x, y] = terrainType;
            }
        }
    }
}
