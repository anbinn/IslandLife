using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IslandLife.World.Terrain
{
    public sealed class TerrainTilemapRenderer : MonoBehaviour
    {
        [SerializeField]
        private TerrainRenderAssets renderAssets;

        [SerializeField]
        private Tilemap waterTilemap;

        [SerializeField]
        private Tilemap sandTilemap;

        [SerializeField]
        private Tilemap grassTilemap;

        [NonSerialized]
        private Dictionary<Sprite, Tile> transientTilesBySprite;

        public void RenderAll(TerrainGridData grid)
        {
            ValidateInputs(grid);

            waterTilemap.ClearAllTiles();
            sandTilemap?.ClearAllTiles();
            grassTilemap.ClearAllTiles();

            long endY = (long)grid.OriginY + grid.Height;
            long endX = (long)grid.OriginX + grid.Width;
            for (long y = grid.OriginY; y < endY; y++)
            {
                if (y > int.MaxValue)
                {
                    break;
                }

                for (long x = grid.OriginX; x < endX; x++)
                {
                    if (x > int.MaxValue)
                    {
                        break;
                    }

                    RenderCell(grid, (int)x, (int)y);
                }
            }
        }

        public void RenderAll(
            TerrainGridData grid,
            TerrainRenderAssets assets,
            Tilemap water,
            Tilemap grass)
        {
            renderAssets = assets;
            waterTilemap = water;
            sandTilemap = null;
            grassTilemap = grass;
            RenderAll(grid);
        }

        public void RefreshCell(TerrainGridData grid, int x, int y)
        {
            ValidateInputs(grid);

            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                long cellY = (long)y + offsetY;
                if (cellY < int.MinValue || cellY > int.MaxValue
                    || !grid.IsInside(x, (int)cellY))
                {
                    continue;
                }

                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    long cellX = (long)x + offsetX;
                    if (cellX < int.MinValue || cellX > int.MaxValue
                        || !grid.IsInside((int)cellX, (int)cellY))
                    {
                        continue;
                    }

                    RenderCell(grid, (int)cellX, (int)cellY);
                }
            }
        }

        private void ValidateInputs(TerrainGridData grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (renderAssets == null)
            {
                throw new InvalidOperationException("Terrain render assets are not configured.");
            }

            if (waterTilemap == null)
            {
                throw new InvalidOperationException("Water Tilemap is not configured.");
            }

            if (grassTilemap == null)
            {
                throw new InvalidOperationException("Grass Tilemap is not configured.");
            }

            if (renderAssets.GrassAutoTile == null)
            {
                throw new InvalidOperationException("Grass AutoTile render asset is not configured.");
            }

            if (renderAssets.Water == null)
            {
                throw new InvalidOperationException("Water terrain render asset is not configured.");
            }

            if (ContainsSand(grid)
                && (sandTilemap == null || renderAssets.Sand == null))
            {
                throw new InvalidOperationException(
                    "Sand terrain requires configured Sand render assets and a Tilemap.");
            }
        }

        private static bool ContainsSand(TerrainGridData grid)
        {
            for (int localY = 0; localY < grid.Height; localY++)
            {
                for (int localX = 0; localX < grid.Width; localX++)
                {
                    int worldX = checked(grid.OriginX + localX);
                    int worldY = checked(grid.OriginY + localY);
                    if (grid.GetTerrain(worldX, worldY) == TerrainType.Sand)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void RenderCell(TerrainGridData grid, int x, int y)
        {
            Vector3Int position = new Vector3Int(x, y, 0);
            waterTilemap.SetTile(position, null);
            sandTilemap?.SetTile(position, null);
            grassTilemap.SetTile(position, null);

            TerrainType terrainType = grid.GetTerrain(x, y);
            switch (terrainType)
            {
                case TerrainType.Empty:
                    return;

                case TerrainType.Water:
                    waterTilemap.SetTile(position, renderAssets.Water);
                    return;

                case TerrainType.Sand:
                    waterTilemap.SetTile(position, renderAssets.Water);
                    if (sandTilemap == null)
                    {
                        throw new InvalidOperationException(
                            "Sand terrain requires a configured Sand Tilemap.");
                    }

                    SetResolvedSprite(
                        grid,
                        x,
                        y,
                        renderAssets.Sand,
                        sandTilemap,
                        position);
                    return;

                case TerrainType.Grass:
                    waterTilemap.SetTile(position, renderAssets.Water);
                    grassTilemap.SetTile(position, renderAssets.GrassAutoTile);
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(terrainType),
                        terrainType,
                        "Unsupported terrain type.");
            }
        }

        private void SetResolvedSprite(
            TerrainGridData grid,
            int x,
            int y,
            TerrainSpriteSet spriteSet,
            Tilemap tilemap,
            Vector3Int position)
        {
            if (!TerrainSpriteResolver.TryResolve(
                    grid,
                    x,
                    y,
                    spriteSet,
                    out Sprite sprite))
            {
                throw new InvalidOperationException(
                    $"Could not resolve a terrain Sprite at ({x}, {y}).");
            }

            tilemap.SetTile(position, GetOrCreateTransientTile(sprite));
        }

        private Tile GetOrCreateTransientTile(Sprite sprite)
        {
            if (transientTilesBySprite == null)
            {
                transientTilesBySprite = new Dictionary<Sprite, Tile>();
            }

            if (transientTilesBySprite.TryGetValue(sprite, out Tile tile))
            {
                return tile;
            }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.hideFlags = HideFlags.HideAndDontSave;
            transientTilesBySprite.Add(sprite, tile);
            return tile;
        }

        private void OnDestroy()
        {
            if (transientTilesBySprite == null)
            {
                return;
            }

            foreach (Tile tile in transientTilesBySprite.Values)
            {
                if (tile == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(tile);
                }
                else
                {
                    DestroyImmediate(tile);
                }
            }

            transientTilesBySprite.Clear();
        }
    }
}
