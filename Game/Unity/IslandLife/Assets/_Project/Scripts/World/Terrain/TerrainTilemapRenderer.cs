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
            sandTilemap.ClearAllTiles();
            grassTilemap.ClearAllTiles();

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    RenderCell(grid, x, y);
                }
            }
        }

        public void RefreshCell(TerrainGridData grid, int x, int y)
        {
            ValidateInputs(grid);

            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                long cellY = (long)y + offsetY;
                if (cellY < 0 || cellY >= grid.Height)
                {
                    continue;
                }

                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    long cellX = (long)x + offsetX;
                    if (cellX < 0 || cellX >= grid.Width)
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

            if (sandTilemap == null)
            {
                throw new InvalidOperationException("Sand Tilemap is not configured.");
            }

            if (grassTilemap == null)
            {
                throw new InvalidOperationException("Grass Tilemap is not configured.");
            }

            if (renderAssets.Grass == null)
            {
                throw new InvalidOperationException("Grass terrain render assets are not configured.");
            }

            if (renderAssets.Sand == null)
            {
                throw new InvalidOperationException("Sand terrain render assets are not configured.");
            }

            if (renderAssets.Water == null)
            {
                throw new InvalidOperationException("Water terrain render asset is not configured.");
            }
        }

        private void RenderCell(TerrainGridData grid, int x, int y)
        {
            Vector3Int position = new Vector3Int(x, y, 0);
            waterTilemap.SetTile(position, null);
            sandTilemap.SetTile(position, null);
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
                    SetResolvedSprite(
                        grid,
                        x,
                        y,
                        renderAssets.Sand,
                        sandTilemap,
                        position);
                    return;

                case TerrainType.Grass:
                    SetResolvedSprite(
                        grid,
                        x,
                        y,
                        renderAssets.Grass,
                        grassTilemap,
                        position);
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
