using System;

namespace IslandLife.World.Terrain
{
    public class TerrainGridData
    {
        private readonly TerrainType[,] terrainTypes;

        public int Width { get; }

        public int Height { get; }

        public int OriginX { get; }

        public int OriginY { get; }

        public TerrainGridData(int width, int height)
            : this(width, height, 0, 0)
        {
        }

        public TerrainGridData(int width, int height, int originX, int originY)
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
            OriginX = originX;
            OriginY = originY;
            terrainTypes = new TerrainType[width, height];
        }

        public bool IsInside(int x, int y)
        {
            long localX = (long)x - OriginX;
            long localY = (long)y - OriginY;
            return localX >= 0 && localX < Width && localY >= 0 && localY < Height;
        }

        public TerrainType GetTerrain(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return TerrainType.Empty;
            }

            int localX = (int)((long)x - OriginX);
            int localY = (int)((long)y - OriginY);
            return terrainTypes[localX, localY];
        }

        public void SetTerrain(int x, int y, TerrainType terrainType)
        {
            if (!IsInside(x, y))
            {
                return;
            }

            int localX = (int)((long)x - OriginX);
            int localY = (int)((long)y - OriginY);
            terrainTypes[localX, localY] = terrainType;
        }
    }
}
