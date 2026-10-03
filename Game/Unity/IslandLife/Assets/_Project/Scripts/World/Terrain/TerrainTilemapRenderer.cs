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
                    waterTilemap.SetTile(position, renderAssets.Water);
                    SetResolvedLandSand(grid, x, y, position);
                    return;

                case TerrainType.Grass:
                    waterTilemap.SetTile(position, renderAssets.Water);
                    SetResolvedLandSand(grid, x, y, position);
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

        private void SetResolvedLandSand(
            TerrainGridData grid,
            int x,
            int y,
            Vector3Int position)
        {
            TerrainNeighborMask rawMask =
                TerrainNeighborResolver.ResolveLandMask(grid, x, y);
            TerrainNeighborMask canonicalMask =
                TerrainMaskNormalizer.Normalize(rawMask);

            if (!SproutLandsTerrainMaskMap.TryGetCoordinate(
                    TerrainType.Sand,
                    canonicalMask,
                    out TerrainSpriteCoordinate coordinate))
            {
                throw new InvalidOperationException(
                    $"Could not resolve the Sand land-underlay coordinate at ({x}, {y}).");
            }

            if (!renderAssets.Sand.TryGetSprite(coordinate, out Sprite sprite)
                || sprite == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve the Sand land-underlay Sprite at "
                    + $"({coordinate.Row}, {coordinate.Column}).");
            }

            sandTilemap.SetTile(position, GetOrCreateTransientTile(sprite));
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
