using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IslandLife.World.Terrain
{
    public sealed class TerrainMapRuntimeLoader : MonoBehaviour
    {
        [SerializeField]
        private TerrainMapData terrainMapData;

        [SerializeField]
        private TerrainRenderAssets renderAssets;

        [SerializeField]
        private TerrainTilemapRenderer terrainRenderer;

        [SerializeField]
        private Tilemap waterTilemap;

        [SerializeField]
        private Tilemap grassTilemap;

        private void Start()
        {
            if (terrainMapData == null)
            {
                throw new InvalidOperationException("Terrain map data is not configured.");
            }

            if (renderAssets == null)
            {
                throw new InvalidOperationException("Terrain render assets are not configured.");
            }

            if (terrainRenderer == null)
            {
                throw new InvalidOperationException("Terrain renderer is not configured.");
            }

            if (waterTilemap == null || grassTilemap == null)
            {
                throw new InvalidOperationException(
                    "Production Water and Grass Tilemaps must be configured.");
            }

            TerrainGridData grid = terrainMapData.CreateGridData();
            if (!TerrainGridValidator.IsValid(grid))
            {
                throw new InvalidOperationException("Production terrain map data is invalid.");
            }

            for (int localY = 0; localY < grid.Height; localY++)
            {
                for (int localX = 0; localX < grid.Width; localX++)
                {
                    int worldX = checked(grid.OriginX + localX);
                    int worldY = checked(grid.OriginY + localY);
                    TerrainType terrainType = grid.GetTerrain(worldX, worldY);
                    if (terrainType == TerrainType.Sand)
                    {
                        throw new InvalidOperationException(
                            "Production Level-0 migration data must not contain Sand.");
                    }
                }
            }

            terrainRenderer.RenderAll(grid, renderAssets, waterTilemap, grassTilemap);
            VerifyRenderedOutput(grid);
        }

        private void VerifyRenderedOutput(TerrainGridData grid)
        {
            int waterCount = 0;
            int grassCount = 0;
            int waterLayerCount = 0;

            for (int localY = 0; localY < grid.Height; localY++)
            {
                for (int localX = 0; localX < grid.Width; localX++)
                {
                    int worldX = checked(grid.OriginX + localX);
                    int worldY = checked(grid.OriginY + localY);
                    Vector3Int position = new Vector3Int(worldX, worldY, 0);
                    TerrainType terrainType = grid.GetTerrain(worldX, worldY);
                    bool hasWater = waterTilemap.GetTile(position) != null;
                    bool hasGrass = grassTilemap.GetTile(position) != null;

                    switch (terrainType)
                    {
                        case TerrainType.Empty:
                            Require(!hasWater && !hasGrass, worldX, worldY);
                            break;
                        case TerrainType.Water:
                            Require(
                                waterTilemap.GetTile(position) == renderAssets.Water
                                    && !hasGrass,
                                worldX,
                                worldY);
                            waterCount++;
                            waterLayerCount++;
                            break;
                        case TerrainType.Grass:
                            if (waterTilemap.GetTile(position) != renderAssets.Water
                                || !hasGrass
                                || grassTilemap.GetTile(position)
                                    != renderAssets.GrassAutoTile)
                            {
                                throw new InvalidOperationException(
                                    $"Rendered Grass does not match terrain data at "
                                    + $"({worldX}, {worldY}).");
                            }

                            waterLayerCount++;
                            grassCount++;
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Unsupported production terrain type {terrainType}.");
                    }
                }
            }

            if (waterCount != 1825 || grassCount != 1100 || waterLayerCount != 2925)
            {
                throw new InvalidOperationException(
                    $"Production terrain counts were Water={waterCount}, "
                    + $"Grass={grassCount}, Water layer={waterLayerCount}; expected "
                    + "Water=1825, Grass=1100, Water layer=2925.");
            }
        }

        private void Require(bool condition, int x, int y)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    $"Rendered terrain does not match data at ({x}, {y}).");
            }
        }
    }
}
