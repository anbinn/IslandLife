using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IslandLife.World.Terrain
{
    public sealed class TerrainRenderTestField : MonoBehaviour
    {
        [SerializeField]
        private TerrainTilemapRenderer terrainRenderer;

        [SerializeField]
        private TerrainRenderAssets renderAssets;

        [SerializeField]
        private Tilemap waterTilemap;

        [SerializeField]
        private Tilemap sandTilemap;

        [SerializeField]
        private Tilemap grassTilemap;

        private void Start()
        {
            TerrainGridData grid = CreateTestGrid();
            if (!TerrainGridValidator.IsValid(grid))
            {
                throw new InvalidOperationException("The deterministic terrain test grid is invalid.");
            }

            terrainRenderer.RenderAll(grid);
            VerifyInitialRender(grid);
            VerifyLocalRefresh(grid);
            VerifyAllTerrainMasks();
            Debug.Log("TERRAIN-05D 47-MASK PASS");
            Debug.Log("TERRAIN-05C PASS");
        }

        private void VerifyAllTerrainMasks()
        {
            HashSet<TerrainNeighborMask> canonicalMasks =
                new HashSet<TerrainNeighborMask>();
            Dictionary<TerrainNeighborMask, int> representativeRawMasks =
                new Dictionary<TerrainNeighborMask, int>();

            for (int raw = 0; raw <= byte.MaxValue; raw++)
            {
                TerrainNeighborMask rawMask = (TerrainNeighborMask)(byte)raw;
                TerrainNeighborMask canonicalMask =
                    TerrainMaskNormalizer.Normalize(rawMask);

                if (canonicalMasks.Add(canonicalMask))
                {
                    representativeRawMasks.Add(canonicalMask, raw);
                }
            }

            if (canonicalMasks.Count != 47)
            {
                throw new InvalidOperationException(
                    $"Expected 47 canonical terrain masks from 256 raw masks, "
                    + $"but found {canonicalMasks.Count}.");
            }

            HashSet<int> coordinateIdentities = new HashSet<int>();

            foreach (TerrainNeighborMask canonicalMask in canonicalMasks)
            {
                int raw = representativeRawMasks[canonicalMask];

                if (TerrainMaskNormalizer.Normalize(canonicalMask) != canonicalMask)
                {
                    throw new InvalidOperationException(
                        $"Canonical mask is not idempotent: raw={(byte)raw}, "
                        + $"canonical={(byte)canonicalMask}.");
                }

                if (!SproutLandsTerrainMaskMap.TryGetCoordinate(
                        canonicalMask,
                        out TerrainSpriteCoordinate coordinate))
                {
                    throw new InvalidOperationException(
                        $"No coordinate for raw={(byte)raw}, "
                        + $"canonical={(byte)canonicalMask}.");
                }

                int coordinateIdentity = coordinate.Row * 100 + coordinate.Column;
                if (!coordinateIdentities.Add(coordinateIdentity))
                {
                    throw new InvalidOperationException(
                        $"Coordinate ({coordinate.Row}, {coordinate.Column}) is duplicated "
                        + $"for raw={(byte)raw}, canonical={(byte)canonicalMask}.");
                }

                if (!renderAssets.Grass.TryGetSprite(coordinate, out Sprite grassSprite)
                    || grassSprite == null)
                {
                    throw new InvalidOperationException(
                        $"Grass Sprite resolution failed for raw={(byte)raw}, "
                        + $"canonical={(byte)canonicalMask}, "
                        + $"coordinate=({coordinate.Row}, {coordinate.Column}).");
                }

                if (!renderAssets.Sand.TryGetSprite(coordinate, out Sprite sandSprite)
                    || sandSprite == null)
                {
                    throw new InvalidOperationException(
                        $"Sand Sprite resolution failed for raw={(byte)raw}, "
                        + $"canonical={(byte)canonicalMask}, "
                        + $"coordinate=({coordinate.Row}, {coordinate.Column}).");
                }
            }

            if (coordinateIdentities.Count != 47)
            {
                throw new InvalidOperationException(
                    $"Expected 47 unique terrain Sprite coordinates, "
                    + $"but found {coordinateIdentities.Count}.");
            }
        }

        private TerrainGridData CreateTestGrid()
        {
            TerrainGridData grid = new TerrainGridData(40, 32);

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    grid.SetTerrain(x, y, TerrainType.Water);
                }

                for (int x = 10; x < 14; x++)
                {
                    grid.SetTerrain(x, y, TerrainType.Sand);
                }

                for (int x = 14; x < 40; x++)
                {
                    grid.SetTerrain(x, y, TerrainType.Grass);
                }
            }

            AddShapeCases(grid, TerrainType.Grass, 12);
            AddShapeCases(grid, TerrainType.Sand, 22);

            grid.SetTerrain(0, 18, TerrainType.Grass);
            grid.SetTerrain(1, 18, TerrainType.Grass);
            grid.SetTerrain(0, 19, TerrainType.Grass);

            grid.SetTerrain(36, 16, TerrainType.Grass);
            grid.SetTerrain(37, 17, TerrainType.Water);

            return grid;
        }

        private static void AddShapeCases(TerrainGridData grid, TerrainType terrainType, int row)
        {
            grid.SetTerrain(1, row, terrainType);

            for (int x = 4; x <= 8; x++)
            {
                grid.SetTerrain(x, row, terrainType);
            }

            for (int y = row - 1; y <= row + 3; y++)
            {
                grid.SetTerrain(11, y, terrainType);
            }

            FillRectangle(grid, 14, row - 1, 17, row + 2, terrainType);

            for (int y = row - 1; y <= row + 3; y++)
            {
                grid.SetTerrain(20, y, terrainType);
            }

            for (int x = 21; x <= 23; x++)
            {
                grid.SetTerrain(x, row - 1, terrainType);
            }

            FillRectangle(grid, 27, row - 1, 28, row, terrainType);
        }

        private static void FillRectangle(
            TerrainGridData grid,
            int minX,
            int minY,
            int maxX,
            int maxY,
            TerrainType terrainType)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    grid.SetTerrain(x, y, terrainType);
                }
            }
        }

        private void VerifyInitialRender(TerrainGridData grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TerrainType terrainType = grid.GetTerrain(x, y);
                    TileBase water = waterTilemap.GetTile(position);
                    TileBase sand = sandTilemap.GetTile(position);
                    TileBase grass = grassTilemap.GetTile(position);

                    switch (terrainType)
                    {
                        case TerrainType.Empty:
                            Require(water == null && sand == null && grass == null, x, y, terrainType);
                            break;

                        case TerrainType.Water:
                            Require(
                                water == renderAssets.Water && sand == null && grass == null,
                                x,
                                y,
                                terrainType);
                            break;

                        case TerrainType.Sand:
                            Require(water == null && sand != null && grass == null, x, y, terrainType);
                            break;

                        case TerrainType.Grass:
                            Require(water == null && sand == null && grass != null, x, y, terrainType);
                            break;

                        default:
                            throw new InvalidOperationException(
                                $"Unexpected terrain type {terrainType} at ({x}, {y}).");
                    }
                }
            }
        }

        private void VerifyLocalRefresh(TerrainGridData grid)
        {
            const int changedX = 15;
            const int changedY = 22;
            const int adjacentX = 16;
            const int adjacentY = 22;
            const int farX = 12;
            const int farY = 3;

            Vector3Int changedPosition = new Vector3Int(changedX, changedY, 0);
            Vector3Int adjacentPosition = new Vector3Int(adjacentX, adjacentY, 0);
            Vector3Int farPosition = new Vector3Int(farX, farY, 0);

            TileBase adjacentBefore = sandTilemap.GetTile(adjacentPosition);
            TileBase farWaterBefore = waterTilemap.GetTile(farPosition);
            TileBase farSandBefore = sandTilemap.GetTile(farPosition);
            TileBase farGrassBefore = grassTilemap.GetTile(farPosition);

            Require(grid.GetTerrain(changedX, changedY) == TerrainType.Sand, changedX, changedY, TerrainType.Sand);
            Require(adjacentBefore != null, adjacentX, adjacentY, TerrainType.Sand);

            grid.SetTerrain(changedX, changedY, TerrainType.Empty);
            if (!TerrainGridValidator.IsValid(grid))
            {
                throw new InvalidOperationException("The locally modified terrain test grid is invalid.");
            }

            terrainRenderer.RefreshCell(grid, changedX, changedY);

            Require(
                waterTilemap.GetTile(changedPosition) == null
                    && sandTilemap.GetTile(changedPosition) == null
                    && grassTilemap.GetTile(changedPosition) == null,
                changedX,
                changedY,
                TerrainType.Empty);

            Require(
                sandTilemap.GetTile(adjacentPosition) != null,
                adjacentX,
                adjacentY,
                TerrainType.Sand);

            Require(
                waterTilemap.GetTile(farPosition) == farWaterBefore
                    && sandTilemap.GetTile(farPosition) == farSandBefore
                    && grassTilemap.GetTile(farPosition) == farGrassBefore,
                farX,
                farY,
                grid.GetTerrain(farX, farY));
        }

        private static void Require(bool condition, int x, int y, TerrainType expectedTerrain)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    $"Rendered tiles did not match expected terrain {expectedTerrain} at ({x}, {y}).");
            }
        }
    }
}
