using System;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    [CreateAssetMenu(
        fileName = "TerrainMapData",
        menuName = "IslandLife/World/Terrain Map Data")]
    public sealed class TerrainMapData : ScriptableObject
    {
        [SerializeField]
        private int originX;

        [SerializeField]
        private int originY;

        [SerializeField]
        private int width;

        [SerializeField]
        private int height;

        [SerializeField]
        private TerrainType[] cells = Array.Empty<TerrainType>();

        public int OriginX => originX;

        public int OriginY => originY;

        public int Width => width;

        public int Height => height;

        public void SetData(
            int dataOriginX,
            int dataOriginY,
            int dataWidth,
            int dataHeight,
            TerrainType[] terrainCells)
        {
            if (dataWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dataWidth));
            }

            if (dataHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dataHeight));
            }

            if (terrainCells == null)
            {
                throw new ArgumentNullException(nameof(terrainCells));
            }

            int expectedLength = checked(dataWidth * dataHeight);
            if (terrainCells.Length != expectedLength)
            {
                throw new ArgumentException(
                    $"Expected {expectedLength} terrain cells, got {terrainCells.Length}.",
                    nameof(terrainCells));
            }

            for (int i = 0; i < terrainCells.Length; i++)
            {
                if (!Enum.IsDefined(typeof(TerrainType), terrainCells[i]))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(terrainCells),
                        $"Unsupported terrain value at index {i}.");
                }
            }

            originX = dataOriginX;
            originY = dataOriginY;
            width = dataWidth;
            height = dataHeight;
            cells = (TerrainType[])terrainCells.Clone();
        }

        public TerrainType GetTerrain(int worldX, int worldY)
        {
            ValidateData();

            long localX = (long)worldX - originX;
            long localY = (long)worldY - originY;
            if (localX < 0 || localX >= width || localY < 0 || localY >= height)
            {
                return TerrainType.Empty;
            }

            int index = checked((int)(localY * width + localX));
            return cells[index];
        }

        public TerrainGridData CreateGridData()
        {
            ValidateData();

            TerrainGridData grid = new TerrainGridData(width, height, originX, originY);
            for (int localY = 0; localY < height; localY++)
            {
                for (int localX = 0; localX < width; localX++)
                {
                    int index = checked(localY * width + localX);
                    int worldX = checked(originX + localX);
                    int worldY = checked(originY + localY);
                    grid.SetTerrain(worldX, worldY, cells[index]);
                }
            }

            return grid;
        }

        private void ValidateData()
        {
            if (width <= 0 || height <= 0 || cells == null
                || cells.Length != checked(width * height))
            {
                throw new InvalidOperationException(
                    "Terrain map data has invalid dimensions or cell storage.");
            }
        }
    }
}
