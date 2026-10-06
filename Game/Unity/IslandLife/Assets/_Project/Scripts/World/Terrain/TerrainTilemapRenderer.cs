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

        /// <summary>
        /// Pure visual layer holding the author's Hills cells for Raised ground. It is a projection
        /// cache, never a data source: nothing reads Raised state back out of this Tilemap. It may
        /// hold tiles one row SOUTH of a logical Raised mask, which R5 proved is required.
        /// A null reference is legal and means "no hill visual is configured".
        /// </summary>
        [SerializeField]
        private Tilemap raisedVisualTilemap;

        [NonSerialized]
        private Dictionary<Sprite, Tile> transientTilesBySprite;

        /// <summary>
        /// The most recent Raised projection, for editor diagnostics. Null until something is
        /// rendered and a Raised composition set is configured.
        /// </summary>
        public RaisedVisualPlan LastRaisedPlan { get; private set; }

        public void RenderAll(TerrainGridData grid)
        {
            ValidateInputs(grid);

            waterTilemap.ClearAllTiles();
            ClearSandTiles();
            grassTilemap.ClearAllTiles();
            if (raisedVisualTilemap != null)
            {
                raisedVisualTilemap.ClearAllTiles();
            }

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

            RenderRaisedVisuals(grid);
        }

        public void RenderAll(
            TerrainGridData grid,
            TerrainRenderAssets assets,
            Tilemap water,
            Tilemap grass)
        {
            RenderAll(grid, assets, water, grass, null);
        }

        public void RenderAll(
            TerrainGridData grid,
            TerrainRenderAssets assets,
            Tilemap water,
            Tilemap grass,
            Tilemap raisedVisual)
        {
            renderAssets = assets;
            waterTilemap = water;
            sandTilemap = null;
            grassTilemap = grass;
            raisedVisualTilemap = raisedVisual;
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

            // A Raised projection is REGION-based, not cell-based: painting or clearing one cell can
            // merge two regions or split one in two, so a 3x3 neighbourhood refresh cannot know the
            // new boundary. The Raised layer is therefore rebuilt in full on every edit. It is a
            // handful of cells per region, so this stays cheap, and it guarantees a region can
            // never leave a stale hill tile behind.
            RenderRaisedVisuals(grid);
        }

        /// <summary>
        /// Sand is not paintable yet, so the Sand layer is legitimately optional. The null-conditional
        /// operator is NOT safe for that: a null reference assigned through SerializedObject is a
        /// Unity "fake null" that it does not short-circuit, which throws
        /// UnassignedReferenceException. Comparing against null directly uses UnityEngine.Object's
        /// operator, which does handle it.
        /// </summary>
        private void ClearSandTiles()
        {
            if (sandTilemap == null)
            {
                return;
            }

            sandTilemap.ClearAllTiles();
        }

        private void RenderRaisedVisuals(TerrainGridData grid)
        {
            AuthorHillsCompositionSet composition = renderAssets.RaisedComposition;
            if (composition == null || raisedVisualTilemap == null)
            {
                // No hill visual configured: Raised cells simply render as ordinary Grass. This is a
                // legal configuration and must never invent art.
                LastRaisedPlan = null;
                return;
            }

            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, composition);

            // IL-WORLD-004S-R15. THE STALE-VISUAL FIX.
            //
            // This method is the incremental path: RefreshCell re-renders only the local neighbourhood
            // and then calls straight into here, which is what every paint and every erase goes
            // through. It used to write the new projection with SetTile and never removed what the
            // PREVIOUS projection had drawn, so every hill tile that dropped out of the plan survived
            // on the map forever. Erase therefore left the user with pillars of old cliff, severed
            // front faces and corners that belonged to a topology that no longer existed. A full
            // RenderAll cleared the layer first and was correct, which is exactly why the bug only
            // appeared while editing.
            //
            // The Raised visual layer is a pure cache derived entirely from the logical elevation, and
            // RaisedVisualPlan.Build only ever reads that elevation. So rebuilding it in full is both
            // correct and safe: it cannot lose anything that is not recomputed immediately afterwards,
            // and a plateau of a few hundred cells costs nothing in the editor. Correctness wins over
            // a narrower dirty range; a stale hill tile is a wrong picture of the user's own map.
            raisedVisualTilemap.ClearAllTiles();

            foreach (HillVisualTile tile in plan.Tiles)
            {
                if (tile.Sprite == null)
                {
                    throw new InvalidOperationException(
                        $"Author Hills composition produced a null Sprite at "
                        + $"{tile.VisualPosition}. Check the composition set.");
                }

                raisedVisualTilemap.SetTile(
                    tile.VisualPosition, GetOrCreateTransientTile(tile.Sprite));
            }

            LastRaisedPlan = plan;

            // Grass is deliberately NOT erased. The author's interior hill cells are 256/256 opaque
            // (measured in IL-WORLD-004S-R5), so drawing the hill layer above grass is the REPLACE
            // relation without touching TerrainData's grass logic. The transparent pixels in the
            // author's outline cells are meant to reveal the ground behind them.
            Debug.Log(
                "[IslandMap] Raised visual projection: " + plan.Describe()
                + (plan.VisualDiagnostics.Count == 0
                    ? string.Empty
                    : " | renderer faults, no hill art was invented: " + plan.VisualDiagnostics.Count));
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
            if (sandTilemap != null)
            {
                sandTilemap.SetTile(position, null);
            }

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