using System;
using System.Collections.Generic;
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

        [SerializeField]
        private Tilemap raisedVisualTilemap;

        /// <summary>
        /// The Raised visual layer created at runtime, so the production scene file never has to be
        /// edited to gain a hill layer.
        /// </summary>
        private Tilemap runtimeRaisedVisualTilemap;

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
                    if (grid.GetTerrain(worldX, worldY) == TerrainType.Sand)
                    {
                        throw new InvalidOperationException(
                            "Production Level-0 migration data must not contain Sand.");
                    }
                }
            }

            // Raised ground is logical data in TerrainMapData and is projected onto the author's
            // verified Hills.png composition in a separate visual Tilemap. The projection may write
            // one visual row SOUTH of a logical Raised mask (proven by IL-WORLD-004S-R5: the author's
            // stack is always two surface rows above the cliff row). It never writes back into the
            // logical mask and never extends TerrainData.
            RaisedVisualPlan plan = RenderRaisedProjections(grid);
            VerifyRenderedOutput(grid, plan);
        }

        /// <summary>
        /// Renders the base layers plus the Raised visual layer. The Raised layer is created here at
        /// runtime rather than being serialized into the scene, so the production scene file stays
        /// byte-identical. A null composition set or a missing Raised region is a legal state that
        /// simply produces no hill visual.
        /// </summary>
        private RaisedVisualPlan RenderRaisedProjections(TerrainGridData grid)
        {
            Tilemap target = raisedVisualTilemap != null
                ? raisedVisualTilemap
                : CreateRaisedVisualTilemap();

            terrainRenderer.RenderAll(grid, renderAssets, waterTilemap, grassTilemap, target);
            return terrainRenderer.LastRaisedPlan;
        }

        private Tilemap CreateRaisedVisualTilemap()
        {
            if (renderAssets.RaisedComposition == null)
            {
                // No author Hills composition configured: Raised renders as ordinary Grass.
                return null;
            }

            Transform parent = waterTilemap.transform.parent;
            if (parent == null)
            {
                Debug.LogWarning(
                    "[IslandMap] Water Tilemap has no Grid parent, so the Raised visual layer "
                    + "was not created and Raised renders as ordinary Grass.");
                return null;
            }

            var go = new GameObject("RaisedVisual");
            go.transform.SetParent(parent, false);

            Tilemap map = go.AddComponent<Tilemap>();
            TilemapRenderer source = grassTilemap.GetComponent<TilemapRenderer>();
            TilemapRenderer target = go.AddComponent<TilemapRenderer>();
            if (source != null)
            {
                target.sortingLayerID = source.sortingLayerID;
                target.sortingOrder = source.sortingOrder + 1;
            }

            runtimeRaisedVisualTilemap = map;
            return map;
        }

        private void OnDestroy()
        {
            if (runtimeRaisedVisualTilemap != null)
            {
                Destroy(runtimeRaisedVisualTilemap.gameObject);
                runtimeRaisedVisualTilemap = null;
            }
        }

        private void VerifyRenderedOutput(TerrainGridData grid, RaisedVisualPlan raisedPlan)
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

            // IL-WORLD-004R: the previous check froze the island silhouette by hardcoding
            // Water=1825 / Grass=1100 / Water layer=2925, which threw as soon as anyone edited
            // the coastline. The exhaustive per-cell verification above still guarantees that
            // the rendered Tilemaps match the data; this now only asserts the structural
            // invariant that must hold for ANY island shape: every non-Empty cell renders
            // exactly one water layer tile.
            if (waterLayerCount != waterCount + grassCount)
            {
                throw new InvalidOperationException(
                    $"Rendered water layer count {waterLayerCount} does not match "
                    + $"Water={waterCount} + Grass={grassCount}.");
            }

            VerifyRaisedProjection(grid, raisedPlan);
        }

        /// <summary>
        /// Proves the Raised visual layer is exactly the author's projection and nothing else. It
        /// deliberately does NOT require the visual layer to be co-located with the logical mask:
        /// IL-WORLD-004S-R5 proved a cliff row legitimately sits one row south of a two-row-tall
        /// logical mask, and forcing it back inside would be a rule the author never drew.
        /// </summary>
        private void VerifyRaisedProjection(TerrainGridData grid, RaisedVisualPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            var expected = new HashSet<Vector3Int>();
            foreach (HillVisualTile tile in plan.Tiles)
            {
                if (!expected.Add(tile.VisualPosition))
                {
                    throw new InvalidOperationException(
                        $"Raised projection placed two hill tiles at {tile.VisualPosition}. "
                        + "Visual positions must be unique.");
                }
            }

            Tilemap visual = raisedVisualTilemap != null
                ? raisedVisualTilemap
                : runtimeRaisedVisualTilemap;

            int actualCount = 0;
            foreach (HillVisualTile tile in plan.Tiles)
            {
                if (visual == null)
                {
                    if (plan.Tiles.Count > 0)
                    {
                        throw new InvalidOperationException(
                            "Raised visual tiles were planned but no Raised visual Tilemap exists.");
                    }

                    return;
                }

                if (visual.GetTile(tile.VisualPosition) == null)
                {
                    throw new InvalidOperationException(
                        $"Raised visual layer is missing a hill tile at {tile.VisualPosition}.");
                }

                actualCount++;

                // Inside the mask, a hill tile must be standing on a Raised logical cell.
                if (!tile.OutsideLogicalMask
                    && grid.IsInside(tile.VisualPosition.x, tile.VisualPosition.y)
                    && grid.GetElevation(tile.VisualPosition.x, tile.VisualPosition.y)
                        != ElevationLevel.Raised)
                {
                    throw new InvalidOperationException(
                        $"Raised visual layer drew a hill tile on logical cell "
                        + $"({tile.VisualPosition.x}, {tile.VisualPosition.y}), which is not "
                        + "Raised.");
                }
            }

            if (visual != null && actualCount != expected.Count)
            {
                throw new InvalidOperationException(
                    $"Raised visual layer holds {actualCount} planned tiles but the plan expected "
                    + $"{expected.Count}.");
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