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

        /// <summary>
        /// Per-cell vertical layer, parallel to <see cref="cells"/>.
        ///
        /// Backward compatibility is deliberate and load-time only: maps authored before elevation
        /// existed have no serialized elevations at all, and those must keep loading byte-identical
        /// as all-Normal without any migration step or asset rewrite. An absent, empty, or
        /// wrong-length array therefore reads as all Normal, and is only written back once the user
        /// actually authors an elevation.
        /// </summary>
        [SerializeField]
        private ElevationLevel[] elevations = Array.Empty<ElevationLevel>();

        public int OriginX => originX;

        public int OriginY => originY;

        public int Width => width;

        public int Height => height;

        /// <summary>True when this map has never had elevation authored on it.</summary>
        public bool HasElevationData => elevations != null && elevations.Length == cells?.Length;

        public void SetData(
            int dataOriginX,
            int dataOriginY,
            int dataWidth,
            int dataHeight,
            TerrainType[] terrainCells)
        {
            ValidateBaseData(dataOriginX, dataOriginY, dataWidth, dataHeight, terrainCells);

            originX = dataOriginX;
            originY = dataOriginY;
            width = dataWidth;
            height = dataHeight;
            cells = (TerrainType[])terrainCells.Clone();
        }

        /// <summary>
        /// Bulk elevation write used by tooling and tests. Passing an all-Normal array stores it
        /// explicitly; pass null to clear back to the implicit all-Normal state.
        /// </summary>
        public void SetElevations(ElevationLevel[] dataElevations)
        {
            if (dataElevations == null)
            {
                elevations = Array.Empty<ElevationLevel>();
                return;
            }

            if (dataElevations.Length != cells.Length)
            {
                throw new ArgumentException(
                    $"Expected {cells.Length} elevation cells, got {dataElevations.Length}.",
                    nameof(dataElevations));
            }

            for (int i = 0; i < dataElevations.Length; i++)
            {
                ValidateElevation(dataElevations[i], i);
            }

            elevations = (ElevationLevel[])dataElevations.Clone();
        }

        public TerrainType GetTerrain(int worldX, int worldY)
        {
            if (!IsInside(worldX, worldY))
            {
                return TerrainType.Empty;
            }

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

        private bool IsInside(int worldX, int worldY)
        {
            long localX = (long)worldX - originX;
            long localY = (long)worldY - originY;
            return localX >= 0 && localX < width && localY >= 0 && localY < height;
        }

        public ElevationLevel GetElevation(int worldX, int worldY)
        {
            if (!IsElevationAuthored())
            {
                return ElevationLevel.Normal;
            }

            long localX = (long)worldX - originX;
            long localY = (long)worldY - originY;
            if (localX < 0 || localX >= width || localY < 0 || localY >= height)
            {
                return ElevationLevel.Normal;
            }

            int index = checked((int)(localY * width + localX));
            return elevations[index];
        }

        public TerrainGridData CreateGridData()
        {
            ValidateBaseData(originX, originY, width, height, cells);

            TerrainGridData grid = new TerrainGridData(width, height, originX, originY);
            bool hasElevation = IsElevationAuthored();

            for (int localY = 0; localY < height; localY++)
            {
                for (int localX = 0; localX < width; localX++)
                {
                    int index = checked(localY * width + localX);
                    int worldX = checked(originX + localX);
                    int worldY = checked(originY + localY);

                    TerrainType terrainType = cells[index];
                    grid.SetTerrain(worldX, worldY, terrainType);

                    if (!hasElevation)
                    {
                        continue;
                    }

                    ElevationLevel elevation = elevations[index];
                    TerrainGridData.ConstrainElevation(terrainType, ref elevation);
                    grid.SetElevation(worldX, worldY, elevation);
                }
            }

            return grid;
        }

        private bool IsElevationAuthored()
        {
            return elevations != null && cells != null && elevations.Length == cells.Length;
        }

        private void ValidateData()
        {
            ValidateBaseData(originX, originY, width, height, cells);

            if (IsElevationAuthored())
            {
                for (int i = 0; i < elevations.Length; i++)
                {
                    ValidateElevation(elevations[i], i);
                }
            }
        }

        private void ValidateBaseData(
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
        }

        private static void ValidateElevation(ElevationLevel elevation, int index)
        {
            if (!Enum.IsDefined(typeof(ElevationLevel), elevation))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elevations),
                    $"Unsupported elevation value at index {index}.");
            }
        }
    }
}