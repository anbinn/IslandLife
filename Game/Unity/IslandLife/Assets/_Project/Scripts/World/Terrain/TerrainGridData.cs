using System;

namespace IslandLife.World.Terrain
{
    public class TerrainGridData
    {
        private readonly TerrainType[,] terrainTypes;
        private readonly ElevationLevel[,] elevations;

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
            elevations = new ElevationLevel[width, height];
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

        /// <summary>Cells outside the grid, and any cell with no authored elevation, read Normal.</summary>
        public ElevationLevel GetElevation(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return ElevationLevel.Normal;
            }

            int localX = (int)((long)x - OriginX);
            int localY = (int)((long)y - OriginY);
            return elevations[localX, localY];
        }

        public void SetElevation(int x, int y, ElevationLevel elevation)
        {
            if (!IsInside(x, y))
            {
                return;
            }

            int localX = (int)((long)x - OriginX);
            int localY = (int)((long)y - OriginY);
            elevations[localX, localY] = elevation;
        }

        /// <summary>
        /// Elevation is only meaningful on solid land. Water is always Normal, and any non-land
        /// base terrain cannot carry an elevation, so this collapses the pair to a legal state.
        /// </summary>
        public static void ConstrainElevation(TerrainType terrainType, ref ElevationLevel elevation)
        {
            if (terrainType == TerrainType.Grass)
            {
                return;
            }

            elevation = ElevationLevel.Normal;
        }
    }
}