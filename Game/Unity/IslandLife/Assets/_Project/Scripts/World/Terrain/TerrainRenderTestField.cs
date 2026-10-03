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
            VerifyGroundMasks(grid);
            VerifyGroundSprites(grid);
            VerifyDistinctTerrainTopology(grid);
            VerifyLocalRefresh(grid);
            VerifyAllTerrainMasks();
            Debug.Log("TERRAIN-09A GROUND-MODEL PASS");
            Debug.Log("TERRAIN-08B MAPPING PASS");
            VerifyOriginSupport();
            Debug.Log("TERRAIN-07B ORIGIN PASS");
            Debug.Log("TERRAIN-05D 47-MASK PASS");
            Debug.Log("TERRAIN-05C PASS");
        }

        private static void VerifyOriginSupport()
        {
            TerrainGridData grid = new TerrainGridData(5, 4, -2, -1);
            RequireOrigin(grid.OriginX == -2, "OriginX must be -2.");
            RequireOrigin(grid.OriginY == -1, "OriginY must be -1.");

            RequireOrigin(grid.IsInside(-2, -1), "Lower-left corner must be inside.");
            RequireOrigin(grid.IsInside(2, -1), "Lower-right corner must be inside.");
            RequireOrigin(grid.IsInside(-2, 2), "Upper-left corner must be inside.");
            RequireOrigin(grid.IsInside(2, 2), "Upper-right corner must be inside.");

            RequireOrigin(!grid.IsInside(-3, -1), "Cell left of the grid must be outside.");
            RequireOrigin(!grid.IsInside(3, -1), "Cell right of the grid must be outside.");
            RequireOrigin(!grid.IsInside(-2, -2), "Cell below the grid must be outside.");
            RequireOrigin(!grid.IsInside(-2, 3), "Cell above the grid must be outside.");

            grid.SetTerrain(-2, -1, TerrainType.Water);
            grid.SetTerrain(2, -1, TerrainType.Sand);
            grid.SetTerrain(-2, 2, TerrainType.Grass);
            grid.SetTerrain(2, 2, TerrainType.Water);
            grid.SetTerrain(-1, 0, TerrainType.Grass);
            grid.SetTerrain(-1, 1, TerrainType.Grass);
            grid.SetTerrain(-2, 0, TerrainType.Grass);
            grid.SetTerrain(0, 1, TerrainType.Grass);
            grid.SetTerrain(-2, 1, TerrainType.Grass);

            RequireOrigin(
                grid.GetTerrain(-2, -1) == TerrainType.Water,
                "Negative valid coordinate did not retain its terrain.");
            RequireOrigin(
                grid.GetTerrain(2, -1) == TerrainType.Sand,
                "Positive valid coordinate did not retain its terrain.");
            RequireOrigin(
                grid.GetTerrain(-2, 2) == TerrainType.Grass,
                "Upper negative valid coordinate did not retain its terrain.");
            RequireOrigin(
                grid.GetTerrain(2, 2) == TerrainType.Water,
                "Upper positive valid coordinate did not retain its terrain.");

            RequireOrigin(
                grid.GetTerrain(-3, -1) == TerrainType.Empty,
                "Out-of-bounds GetTerrain must return Empty.");
            RequireOrigin(
                grid.GetTerrain(3, -1) == TerrainType.Empty,
                "Out-of-bounds GetTerrain must return Empty.");

            grid.SetTerrain(-3, -1, TerrainType.Sand);
            grid.SetTerrain(3, -1, TerrainType.Grass);
            grid.SetTerrain(-2, -2, TerrainType.Sand);
            grid.SetTerrain(-2, 3, TerrainType.Water);
            RequireOrigin(
                grid.GetTerrain(-2, -1) == TerrainType.Water
                    && grid.GetTerrain(2, -1) == TerrainType.Sand
                    && grid.GetTerrain(-2, 2) == TerrainType.Grass
                    && grid.GetTerrain(2, 2) == TerrainType.Water,
                "Out-of-bounds SetTerrain changed an in-bounds cell.");

            TerrainNeighborMask expectedMask =
                TerrainNeighborMask.NorthWest
                | TerrainNeighborMask.North
                | TerrainNeighborMask.NorthEast
                | TerrainNeighborMask.West;
            TerrainNeighborMask actualMask =
                TerrainNeighborResolver.ResolveSameTerrainMask(grid, -1, 0);
            RequireOrigin(
                actualMask == expectedMask,
                $"Negative-origin neighbor mask was {actualMask}, expected {expectedMask}.");

            TerrainGridData maximumOriginGrid =
                new TerrainGridData(2, 2, int.MaxValue, int.MaxValue);
            maximumOriginGrid.SetTerrain(
                int.MaxValue,
                int.MaxValue,
                TerrainType.Grass);
            RequireOrigin(
                maximumOriginGrid.IsInside(int.MaxValue, int.MaxValue)
                    && maximumOriginGrid.GetTerrain(int.MaxValue, int.MaxValue)
                    == TerrainType.Grass
                    && !maximumOriginGrid.IsInside(int.MinValue, int.MaxValue),
                "Bounds overflowed for a grid at the maximum integer origin.");

            TerrainGridData minimumOriginGrid =
                new TerrainGridData(2, 2, int.MinValue, int.MinValue);
            minimumOriginGrid.SetTerrain(
                int.MinValue,
                int.MinValue,
                TerrainType.Water);
            RequireOrigin(
                minimumOriginGrid.IsInside(int.MinValue, int.MinValue)
                    && minimumOriginGrid.GetTerrain(int.MinValue, int.MinValue)
                    == TerrainType.Water
                    && !minimumOriginGrid.IsInside(int.MaxValue, int.MinValue),
                "Bounds overflowed for a grid at the minimum integer origin.");
        }

        private static void RequireOrigin(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
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

            HashSet<int> grassCoordinateIdentities = new HashSet<int>();
            HashSet<int> sandCoordinateIdentities = new HashSet<int>();

            foreach (TerrainNeighborMask canonicalMask in canonicalMasks)
            {
                int raw = representativeRawMasks[canonicalMask];

                if (TerrainMaskNormalizer.Normalize(canonicalMask) != canonicalMask)
                {
                    throw new InvalidOperationException(
                        $"Canonical mask is not idempotent: raw={(byte)raw}, "
                        + $"canonical={(byte)canonicalMask}.");
                }

                VerifyTerrainMaskMapping(
                    TerrainType.Grass,
                    canonicalMask,
                    raw,
                    grassCoordinateIdentities,
                    renderAssets.Grass);
                VerifyTerrainMaskMapping(
                    TerrainType.Sand,
                    canonicalMask,
                    raw,
                    sandCoordinateIdentities,
                    renderAssets.Sand);
            }

            if (grassCoordinateIdentities.Count != 47
                || sandCoordinateIdentities.Count != 47)
            {
                throw new InvalidOperationException(
                    $"Expected 47 unique coordinates per terrain, but found "
                    + $"Grass={grassCoordinateIdentities.Count}, "
                    + $"Sand={sandCoordinateIdentities.Count}.");
            }

            VerifySpecificTerrainCoordinate(TerrainType.Grass, 2, 2, 3);
            VerifySpecificTerrainCoordinate(TerrainType.Grass, 64, 0, 3);
            VerifySpecificTerrainCoordinate(TerrainType.Grass, 18, 3, 4);
            VerifySpecificTerrainCoordinate(TerrainType.Grass, 80, 0, 4);
            VerifySpecificTerrainCoordinate(TerrainType.Grass, 22, 2, 0);
            VerifySpecificTerrainCoordinate(TerrainType.Grass, 208, 0, 0);
            VerifySpecificTerrainCoordinate(TerrainType.Grass, 126, 0, 9);
            VerifySpecificTerrainCoordinate(TerrainType.Grass, 219, 1, 9);
            VerifySpecificTerrainCoordinate(TerrainType.Sand, 2, 2, 3);
            VerifySpecificTerrainCoordinate(TerrainType.Sand, 64, 0, 3);
            VerifySpecificTerrainCoordinate(TerrainType.Sand, 18, 3, 4);
            VerifySpecificTerrainCoordinate(TerrainType.Sand, 80, 0, 4);
            VerifySpecificTerrainCoordinate(TerrainType.Sand, 126, 1, 9);
            VerifySpecificTerrainCoordinate(TerrainType.Sand, 219, 0, 9);

            if (SproutLandsTerrainMaskMap.TryGetCoordinate(
                    TerrainType.Empty,
                    TerrainNeighborMask.None,
                    out _)
                || SproutLandsTerrainMaskMap.TryGetCoordinate(
                    TerrainType.Water,
                    TerrainNeighborMask.None,
                    out _)
                || SproutLandsTerrainMaskMap.TryGetCoordinate(
                    (TerrainType)byte.MaxValue,
                    TerrainNeighborMask.None,
                    out _))
            {
                throw new InvalidOperationException(
                    "Unsupported terrain types must not resolve a Sprite coordinate.");
            }
        }

        private static void VerifyTerrainMaskMapping(
            TerrainType terrainType,
            TerrainNeighborMask canonicalMask,
            int raw,
            HashSet<int> coordinateIdentities,
            TerrainSpriteSet spriteSet)
        {
            if (!TryGetExpectedCoordinate(
                    terrainType,
                    canonicalMask,
                    out TerrainSpriteCoordinate expectedCoordinate))
            {
                throw new InvalidOperationException(
                    $"No independent expected coordinate for {terrainType}, "
                    + $"raw={(byte)raw}, canonical={(byte)canonicalMask}.");
            }

            if (!SproutLandsTerrainMaskMap.TryGetCoordinate(
                    terrainType,
                    canonicalMask,
                    out TerrainSpriteCoordinate actualCoordinate))
            {
                throw new InvalidOperationException(
                    $"No coordinate for {terrainType}, raw={(byte)raw}, "
                    + $"canonical={(byte)canonicalMask}.");
            }

            if (actualCoordinate.Row != expectedCoordinate.Row
                || actualCoordinate.Column != expectedCoordinate.Column)
            {
                throw new InvalidOperationException(
                    $"Incorrect {terrainType} coordinate for raw={(byte)raw}, "
                    + $"canonical={(byte)canonicalMask}: "
                    + $"expected ({expectedCoordinate.Row}, {expectedCoordinate.Column}), "
                    + $"actual ({actualCoordinate.Row}, {actualCoordinate.Column}).");
            }

            int coordinateIdentity =
                actualCoordinate.Row * 100 + actualCoordinate.Column;
            if (!coordinateIdentities.Add(coordinateIdentity))
            {
                throw new InvalidOperationException(
                    $"{terrainType} coordinate ({actualCoordinate.Row}, "
                    + $"{actualCoordinate.Column}) is duplicated for raw={(byte)raw}, "
                    + $"canonical={(byte)canonicalMask}.");
            }

            if (!spriteSet.TryGetSprite(actualCoordinate, out Sprite sprite)
                || sprite == null)
            {
                throw new InvalidOperationException(
                    $"{terrainType} Sprite resolution failed for raw={(byte)raw}, "
                    + $"canonical={(byte)canonicalMask}, "
                    + $"coordinate=({actualCoordinate.Row}, {actualCoordinate.Column}).");
            }
        }

        private static bool TryGetExpectedCoordinate(
            TerrainType terrainType,
            TerrainNeighborMask canonicalMask,
            out TerrainSpriteCoordinate coordinate)
        {
            if (terrainType != TerrainType.Grass && terrainType != TerrainType.Sand)
            {
                coordinate = default;
                return false;
            }

            if (terrainType == TerrainType.Sand)
            {
                switch (canonicalMask)
                {
                    case (TerrainNeighborMask)126:
                        coordinate = new TerrainSpriteCoordinate(1, 9);
                        return true;
                    case (TerrainNeighborMask)219:
                        coordinate = new TerrainSpriteCoordinate(0, 9);
                        return true;
                }
            }

            switch (canonicalMask)
            {
                case (TerrainNeighborMask)22:
                    coordinate = new TerrainSpriteCoordinate(2, 0);
                    return true;
                case (TerrainNeighborMask)31:
                    coordinate = new TerrainSpriteCoordinate(2, 1);
                    return true;
                case (TerrainNeighborMask)11:
                    coordinate = new TerrainSpriteCoordinate(2, 2);
                    return true;
                case (TerrainNeighborMask)2:
                    coordinate = new TerrainSpriteCoordinate(2, 3);
                    return true;
                case (TerrainNeighborMask)18:
                    coordinate = new TerrainSpriteCoordinate(3, 4);
                    return true;
                case (TerrainNeighborMask)27:
                    coordinate = new TerrainSpriteCoordinate(3, 5);
                    return true;
                case (TerrainNeighborMask)30:
                    coordinate = new TerrainSpriteCoordinate(3, 6);
                    return true;
                case (TerrainNeighborMask)10:
                    coordinate = new TerrainSpriteCoordinate(3, 7);
                    return true;
                case (TerrainNeighborMask)26:
                    coordinate = new TerrainSpriteCoordinate(3, 8);
                    return true;
                case (TerrainNeighborMask)126:
                    coordinate = new TerrainSpriteCoordinate(0, 9);
                    return true;
                case (TerrainNeighborMask)214:
                    coordinate = new TerrainSpriteCoordinate(1, 0);
                    return true;
                case TerrainNeighborMask.All:
                    coordinate = new TerrainSpriteCoordinate(1, 1);
                    return true;
                case (TerrainNeighborMask)107:
                    coordinate = new TerrainSpriteCoordinate(1, 2);
                    return true;
                case (TerrainNeighborMask)66:
                    coordinate = new TerrainSpriteCoordinate(1, 3);
                    return true;
                case (TerrainNeighborMask)210:
                    coordinate = new TerrainSpriteCoordinate(2, 4);
                    return true;
                case (TerrainNeighborMask)251:
                    coordinate = new TerrainSpriteCoordinate(2, 5);
                    return true;
                case (TerrainNeighborMask)254:
                    coordinate = new TerrainSpriteCoordinate(2, 6);
                    return true;
                case (TerrainNeighborMask)106:
                    coordinate = new TerrainSpriteCoordinate(2, 7);
                    return true;
                case (TerrainNeighborMask)250:
                    coordinate = new TerrainSpriteCoordinate(2, 8);
                    return true;
                case (TerrainNeighborMask)219:
                    coordinate = new TerrainSpriteCoordinate(1, 9);
                    return true;
                case (TerrainNeighborMask)208:
                    coordinate = new TerrainSpriteCoordinate(0, 0);
                    return true;
                case (TerrainNeighborMask)248:
                    coordinate = new TerrainSpriteCoordinate(0, 1);
                    return true;
                case (TerrainNeighborMask)104:
                    coordinate = new TerrainSpriteCoordinate(0, 2);
                    return true;
                case (TerrainNeighborMask)64:
                    coordinate = new TerrainSpriteCoordinate(0, 3);
                    return true;
                case (TerrainNeighborMask)86:
                    coordinate = new TerrainSpriteCoordinate(1, 4);
                    return true;
                case (TerrainNeighborMask)127:
                    coordinate = new TerrainSpriteCoordinate(1, 5);
                    return true;
                case (TerrainNeighborMask)223:
                    coordinate = new TerrainSpriteCoordinate(1, 6);
                    return true;
                case (TerrainNeighborMask)75:
                    coordinate = new TerrainSpriteCoordinate(1, 7);
                    return true;
                case (TerrainNeighborMask)95:
                    coordinate = new TerrainSpriteCoordinate(1, 8);
                    return true;
                case (TerrainNeighborMask)94:
                    coordinate = new TerrainSpriteCoordinate(3, 9);
                    return true;
                case (TerrainNeighborMask)91:
                    coordinate = new TerrainSpriteCoordinate(3, 10);
                    return true;
                case (TerrainNeighborMask)16:
                    coordinate = new TerrainSpriteCoordinate(3, 0);
                    return true;
                case (TerrainNeighborMask)24:
                    coordinate = new TerrainSpriteCoordinate(3, 1);
                    return true;
                case (TerrainNeighborMask)8:
                    coordinate = new TerrainSpriteCoordinate(3, 2);
                    return true;
                case TerrainNeighborMask.None:
                    coordinate = new TerrainSpriteCoordinate(3, 3);
                    return true;
                case (TerrainNeighborMask)80:
                    coordinate = new TerrainSpriteCoordinate(0, 4);
                    return true;
                case (TerrainNeighborMask)120:
                    coordinate = new TerrainSpriteCoordinate(0, 5);
                    return true;
                case (TerrainNeighborMask)216:
                    coordinate = new TerrainSpriteCoordinate(0, 6);
                    return true;
                case (TerrainNeighborMask)72:
                    coordinate = new TerrainSpriteCoordinate(0, 7);
                    return true;
                case (TerrainNeighborMask)88:
                    coordinate = new TerrainSpriteCoordinate(0, 8);
                    return true;
                case (TerrainNeighborMask)218:
                    coordinate = new TerrainSpriteCoordinate(2, 9);
                    return true;
                case (TerrainNeighborMask)122:
                    coordinate = new TerrainSpriteCoordinate(2, 10);
                    return true;
                case (TerrainNeighborMask)82:
                    coordinate = new TerrainSpriteCoordinate(4, 4);
                    return true;
                case (TerrainNeighborMask)123:
                    coordinate = new TerrainSpriteCoordinate(4, 5);
                    return true;
                case (TerrainNeighborMask)222:
                    coordinate = new TerrainSpriteCoordinate(4, 6);
                    return true;
                case (TerrainNeighborMask)74:
                    coordinate = new TerrainSpriteCoordinate(4, 7);
                    return true;
                case (TerrainNeighborMask)90:
                    coordinate = new TerrainSpriteCoordinate(4, 8);
                    return true;
                default:
                    coordinate = default;
                    return false;
            }
        }

        private static void VerifySpecificTerrainCoordinate(
            TerrainType terrainType,
            int mask,
            int expectedRow,
            int expectedColumn)
        {
            if (!SproutLandsTerrainMaskMap.TryGetCoordinate(
                    terrainType,
                    (TerrainNeighborMask)mask,
                    out TerrainSpriteCoordinate coordinate)
                || coordinate.Row != expectedRow
                || coordinate.Column != expectedColumn)
            {
                throw new InvalidOperationException(
                    $"High-signal {terrainType} mask {mask} mapping was not "
                    + $"({expectedRow}, {expectedColumn}).");
            }
        }

        private TerrainGridData CreateTestGrid()
        {
            TerrainGridData grid = new TerrainGridData(40, 32);

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetTerrain(x, y, TerrainType.Water);
                }
            }

            AddShapeCases(grid, TerrainType.Sand, 4);
            AddShapeCases(grid, TerrainType.Grass, 12);
            AddRingCase(grid, TerrainType.Sand, 30, 3);
            AddRingCase(grid, TerrainType.Grass, 34, 11);
            AddIrregularIsland(grid, TerrainType.Grass);
            AddIrregularIsland(grid, TerrainType.Sand);

            grid.SetTerrain(35, 29, TerrainType.Grass);
            grid.SetTerrain(36, 29, TerrainType.Grass);
            grid.SetTerrain(37, 29, TerrainType.Sand);
            grid.SetTerrain(38, 29, TerrainType.Sand);
            grid.SetTerrain(39, 31, TerrainType.Empty);

            return grid;
        }

        private static void AddRingCase(
            TerrainGridData grid,
            TerrainType terrainType,
            int minX,
            int minY)
        {
            FillRectangle(grid, minX, minY, minX + 3, minY + 3, terrainType);
            FillRectangle(
                grid,
                minX + 1,
                minY + 1,
                minX + 2,
                minY + 2,
                TerrainType.Water);
        }

        private static void AddIrregularIsland(
            TerrainGridData grid,
            TerrainType terrainType)
        {
            int firstX = terrainType == TerrainType.Grass ? 2 : 29;
            int firstY = 19;
            int[] minOffsets = { 2, 1, 0, 0, 1, 2, 3 };
            int[] maxOffsets = { 6, 7, 8, 8, 7, 6, 4 };

            for (int row = 0; row < minOffsets.Length; row++)
            {
                int minX = firstX + minOffsets[row];
                int maxX = firstX + maxOffsets[row];
                for (int x = minX; x <= maxX; x++)
                {
                    if (row == 3 && x == firstX + 4)
                    {
                        continue;
                    }

                    grid.SetTerrain(x, firstY + row, terrainType);
                }
            }
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
                            Require(
                                water == renderAssets.Water && sand != null && grass == null,
                                x,
                                y,
                                terrainType);
                            break;

                        case TerrainType.Grass:
                            Require(
                                water == renderAssets.Water && sand == null && grass != null,
                                x,
                                y,
                                terrainType);
                            break;

                        default:
                            throw new InvalidOperationException(
                                $"Unexpected terrain type {terrainType} at ({x}, {y}).");
                    }
                }
            }
        }

        private void VerifyGroundMasks(TerrainGridData grid)
        {
            TerrainNeighborMask horizontalEnd = TerrainNeighborMask.East;
            TerrainNeighborMask horizontalMiddle =
                TerrainNeighborMask.West | TerrainNeighborMask.East;
            TerrainNeighborMask verticalEnd = TerrainNeighborMask.North;
            TerrainNeighborMask verticalMiddle =
                TerrainNeighborMask.North | TerrainNeighborMask.South;
            TerrainNeighborMask rectangleCorner =
                TerrainNeighborMask.North | TerrainNeighborMask.East
                | TerrainNeighborMask.NorthEast;

            RequireGroundMask(
                grid,
                TerrainType.Grass,
                1,
                12,
                TerrainNeighborMask.None,
                "isolated Grass surrounded by Water");
            RequireGroundMask(grid, TerrainType.Grass, 4, 12, horizontalEnd, "Grass strip end");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                6,
                12,
                horizontalMiddle,
                "Grass strip middle");
            RequireGroundMask(grid, TerrainType.Grass, 11, 11, verticalEnd, "Grass vertical cap");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                11,
                13,
                verticalMiddle,
                "Grass vertical strip middle");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                14,
                11,
                rectangleCorner,
                "Grass rectangle outer corner");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                20,
                15,
                TerrainNeighborMask.South,
                "Grass L-shape end");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                27,
                11,
                rectangleCorner,
                "Grass 2x2 outer corner");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                34,
                12,
                TerrainNeighborMask.North | TerrainNeighborMask.South
                    | TerrainNeighborMask.SouthEast,
                "Grass hole inner corner");

            RequireGroundMask(
                grid,
                TerrainType.Sand,
                1,
                4,
                TerrainNeighborMask.None,
                "isolated Tilled Dirt");
            RequireGroundMask(grid, TerrainType.Sand, 4, 4, horizontalEnd, "Dirt strip end");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                6,
                4,
                horizontalMiddle,
                "Dirt strip middle");
            RequireGroundMask(grid, TerrainType.Sand, 11, 3, verticalEnd, "Dirt vertical cap");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                11,
                5,
                verticalMiddle,
                "Dirt vertical strip middle");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                14,
                3,
                rectangleCorner,
                "Dirt rectangle outer corner");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                20,
                7,
                TerrainNeighborMask.South,
                "Dirt L-shape end");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                27,
                3,
                rectangleCorner,
                "Dirt 2x2 outer corner");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                30,
                4,
                TerrainNeighborMask.North | TerrainNeighborMask.South
                    | TerrainNeighborMask.SouthEast,
                "Dirt hole inner corner");

            Require(
                grid.GetTerrain(35, 12) == TerrainType.Water
                    && grid.GetTerrain(36, 12) == TerrainType.Water,
                35,
                12,
                TerrainType.Water);
            RequireIrregularIsland(grid, TerrainType.Grass);
            RequireIrregularIsland(grid, TerrainType.Sand);
        }

        private void RequireIrregularIsland(TerrainGridData grid, TerrainType terrainType)
        {
            int minX = terrainType == TerrainType.Grass ? 2 : 29;
            int maxX = terrainType == TerrainType.Grass ? 10 : 37;
            int groundCells = 0;

            for (int y = 19; y <= 25; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (grid.GetTerrain(x, y) == terrainType)
                    {
                        groundCells++;
                    }
                }
            }

            if (groundCells < 20)
            {
                throw new InvalidOperationException(
                    $"{terrainType} irregular island is missing test cells.");
            }
        }

        private void RequireGroundMask(
            TerrainGridData grid,
            TerrainType terrainType,
            int x,
            int y,
            TerrainNeighborMask expectedMask,
            string caseName)
        {
            if (grid.GetTerrain(x, y) != terrainType)
            {
                throw new InvalidOperationException(
                    $"{caseName} is not {terrainType} at ({x}, {y}).");
            }

            TerrainNeighborMask actualMask =
                TerrainNeighborResolver.ResolveSameTerrainMask(grid, x, y);
            if (actualMask != expectedMask)
            {
                throw new InvalidOperationException(
                    $"{caseName} mask was {actualMask}, expected {expectedMask}.");
            }
        }

        private void VerifyGroundSprites(TerrainGridData grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    VerifyRenderedCell(grid, x, y);
                }
            }
        }

        private void VerifyRenderedCell(TerrainGridData grid, int x, int y)
        {
            Vector3Int position = new Vector3Int(x, y, 0);
            TerrainType terrainType = grid.GetTerrain(x, y);
            TileBase water = waterTilemap.GetTile(position);
            TileBase dirt = sandTilemap.GetTile(position);
            TileBase grass = grassTilemap.GetTile(position);

            switch (terrainType)
            {
                case TerrainType.Empty:
                    Require(water == null && dirt == null && grass == null, x, y, terrainType);
                    return;
                case TerrainType.Water:
                    Require(
                        water == renderAssets.Water && dirt == null && grass == null,
                        x,
                        y,
                        terrainType);
                    return;
                case TerrainType.Sand:
                    Require(
                        water == renderAssets.Water && dirt != null && grass == null,
                        x,
                        y,
                        terrainType);
                    VerifyResolvedTerrainSprite(
                        grid,
                        x,
                        y,
                        renderAssets.Sand,
                        sandTilemap);
                    return;
                case TerrainType.Grass:
                    Require(
                        water == renderAssets.Water && dirt == null && grass != null,
                        x,
                        y,
                        terrainType);
                    VerifyResolvedTerrainSprite(
                        grid,
                        x,
                        y,
                        renderAssets.Grass,
                        grassTilemap);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Unexpected terrain type {terrainType} at ({x}, {y}).");
            }
        }

        private static void VerifyResolvedTerrainSprite(
            TerrainGridData grid,
            int x,
            int y,
            TerrainSpriteSet spriteSet,
            Tilemap tilemap)
        {
            if (!TerrainSpriteResolver.TryResolve(
                    grid,
                    x,
                    y,
                    spriteSet,
                    out Sprite expectedSprite)
                || expectedSprite == null
                || tilemap.GetSprite(new Vector3Int(x, y, 0)) != expectedSprite)
            {
                throw new InvalidOperationException(
                    $"Rendered {spriteSet.TerrainType} Sprite did not match its own "
                    + $"neighbor topology at ({x}, {y}).");
            }
        }

        private void VerifyDistinctTerrainTopology(TerrainGridData grid)
        {
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                36,
                29,
                TerrainNeighborMask.West,
                "Grass beside Dirt");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                37,
                29,
                TerrainNeighborMask.East,
                "Dirt beside Grass");
            VerifyRenderedCell(grid, 36, 29);
            VerifyRenderedCell(grid, 37, 29);
        }

        private void VerifyLocalRefresh(TerrainGridData grid)
        {
            const int grassLeftX = 35;
            const int grassRightX = 36;
            const int grassBottomY = 8;
            const int grassTopY = 9;
            const int dirtLeftX = 35;
            const int dirtRightX = 36;
            const int dirtBottomY = 3;
            const int dirtTopY = 4;
            Vector3Int farPosition = new Vector3Int(1, 12, 0);
            TileBase farWaterBefore = waterTilemap.GetTile(farPosition);
            TileBase farDirtBefore = sandTilemap.GetTile(farPosition);
            TileBase farGrassBefore = grassTilemap.GetTile(farPosition);

            grid.SetTerrain(grassLeftX, grassBottomY, TerrainType.Grass);
            grid.SetTerrain(grassLeftX, grassTopY, TerrainType.Grass);
            grid.SetTerrain(grassRightX, grassTopY, TerrainType.Grass);
            terrainRenderer.RefreshCell(grid, grassLeftX, grassBottomY);
            VerifyRenderedCell(grid, grassLeftX, grassBottomY);
            VerifyRenderedCell(grid, grassLeftX, grassTopY);
            VerifyRenderedCell(grid, grassRightX, grassTopY);

            grid.SetTerrain(grassRightX, grassBottomY, TerrainType.Grass);
            terrainRenderer.RefreshCell(grid, grassRightX, grassBottomY);
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                grassLeftX,
                grassBottomY,
                TerrainNeighborMask.North | TerrainNeighborMask.East
                    | TerrainNeighborMask.NorthEast,
                "Grass 3x3 refresh outer corner");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                grassRightX,
                grassBottomY,
                TerrainNeighborMask.North | TerrainNeighborMask.West
                    | TerrainNeighborMask.NorthWest,
                "Grass refresh changed Water to Grass");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                grassLeftX,
                grassTopY,
                TerrainNeighborMask.East | TerrainNeighborMask.South
                    | TerrainNeighborMask.SouthEast,
                "Grass 3x3 refresh inner corner");
            RequireGroundMask(
                grid,
                TerrainType.Grass,
                grassRightX,
                grassTopY,
                TerrainNeighborMask.West | TerrainNeighborMask.South
                    | TerrainNeighborMask.SouthWest,
                "Grass 3x3 refresh diagonal corner");

            grid.SetTerrain(grassRightX, grassBottomY, TerrainType.Water);
            terrainRenderer.RefreshCell(grid, grassRightX, grassBottomY);
            VerifyRenderedCell(grid, grassRightX, grassBottomY);
            VerifyRenderedCell(grid, grassLeftX, grassBottomY);
            VerifyRenderedCell(grid, grassLeftX, grassTopY);
            VerifyRenderedCell(grid, grassRightX, grassTopY);

            grid.SetTerrain(dirtLeftX, dirtBottomY, TerrainType.Sand);
            grid.SetTerrain(dirtLeftX, dirtTopY, TerrainType.Sand);
            grid.SetTerrain(dirtRightX, dirtTopY, TerrainType.Sand);
            terrainRenderer.RefreshCell(grid, dirtLeftX, dirtBottomY);
            VerifyRenderedCell(grid, dirtLeftX, dirtBottomY);
            VerifyRenderedCell(grid, dirtLeftX, dirtTopY);
            VerifyRenderedCell(grid, dirtRightX, dirtTopY);

            grid.SetTerrain(dirtRightX, dirtBottomY, TerrainType.Sand);
            terrainRenderer.RefreshCell(grid, dirtRightX, dirtBottomY);
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                dirtLeftX,
                dirtBottomY,
                TerrainNeighborMask.North | TerrainNeighborMask.East
                    | TerrainNeighborMask.NorthEast,
                "Dirt 3x3 refresh outer corner");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                dirtRightX,
                dirtBottomY,
                TerrainNeighborMask.North | TerrainNeighborMask.West
                    | TerrainNeighborMask.NorthWest,
                "Dirt refresh changed Water to Dirt");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                dirtLeftX,
                dirtTopY,
                TerrainNeighborMask.East | TerrainNeighborMask.South
                    | TerrainNeighborMask.SouthEast,
                "Dirt 3x3 refresh inner corner");
            RequireGroundMask(
                grid,
                TerrainType.Sand,
                dirtRightX,
                dirtTopY,
                TerrainNeighborMask.West | TerrainNeighborMask.South
                    | TerrainNeighborMask.SouthWest,
                "Dirt 3x3 refresh diagonal corner");

            grid.SetTerrain(dirtRightX, dirtBottomY, TerrainType.Water);
            terrainRenderer.RefreshCell(grid, dirtRightX, dirtBottomY);
            VerifyRenderedCell(grid, dirtRightX, dirtBottomY);
            VerifyRenderedCell(grid, dirtLeftX, dirtBottomY);
            VerifyRenderedCell(grid, dirtLeftX, dirtTopY);
            VerifyRenderedCell(grid, dirtRightX, dirtTopY);

            grid.SetTerrain(grassLeftX, grassBottomY, TerrainType.Water);
            grid.SetTerrain(grassLeftX, grassTopY, TerrainType.Water);
            grid.SetTerrain(grassRightX, grassTopY, TerrainType.Water);
            terrainRenderer.RefreshCell(grid, grassLeftX, grassBottomY);
            grid.SetTerrain(dirtLeftX, dirtBottomY, TerrainType.Water);
            grid.SetTerrain(dirtLeftX, dirtTopY, TerrainType.Water);
            grid.SetTerrain(dirtRightX, dirtTopY, TerrainType.Water);
            terrainRenderer.RefreshCell(grid, dirtLeftX, dirtBottomY);

            if (waterTilemap.GetTile(farPosition) != farWaterBefore
                || sandTilemap.GetTile(farPosition) != farDirtBefore
                || grassTilemap.GetTile(farPosition) != farGrassBefore)
            {
                throw new InvalidOperationException(
                    "A local 3x3 refresh changed an unrelated terrain cell.");
            }

            VerifyGroundSprites(grid);
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
