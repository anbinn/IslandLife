using System;
using System.Collections.Generic;
using IslandLife.World.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace IslandLife.Editor
{
    public static class FirstIslandTerrainMigration
    {
        private const string ScenePath = "Assets/Scenes/FirstIsland_Prototype.unity";
        private const string TerrainDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string RenderAssetsPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";
        private const string WaterTilePath =
            "Assets/Art/Environment/SproutLands/Tiles/AnimatedTile_Water.asset";
        private const string GrassTilePath =
            "Assets/Art/Environment/SproutLands/Tiles/AutoTile_Grass.asset";

        private const int OriginX = -32;
        private const int OriginY = -22;
        private const int Width = 65;
        private const int Height = 45;
        private const int ExpectedWaterCount = 1825;
        private const int ExpectedGrassCount = 1100;
        private const int ExpectedBeachCount = 51;

        [MenuItem("IslandLife/Terrain/Migrate FirstIsland Ground _F9")]
        private static void Migrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before migrating production terrain.");
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty && activeScene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Save or close the modified active scene before migration.");
            }

            Scene scene = activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Tilemap groundTilemap = FindTilemap(scene, "Ground");
            Tilemap beachTilemap = FindTilemap(scene, "Beach");
            TileBase waterSource = AssetDatabase.LoadAssetAtPath<TileBase>(WaterTilePath);
            TileBase grassSource = AssetDatabase.LoadAssetAtPath<TileBase>(GrassTilePath);
            TerrainRenderAssets renderAssets =
                AssetDatabase.LoadAssetAtPath<TerrainRenderAssets>(RenderAssetsPath);

            if (waterSource == null || grassSource == null || renderAssets == null)
            {
                throw new InvalidOperationException(
                    "Required Water, Grass, or terrain render assets could not be loaded.");
            }

            if (AssetDatabase.LoadAssetAtPath<TerrainMapData>(TerrainDataPath) != null)
            {
                throw new InvalidOperationException(
                    "FirstIsland_TerrainData already exists; refusing to overwrite it.");
            }

            if (scene.GetRootGameObjects().Length == 0
                || FindSceneObject(scene, "TerrainRuntime") != null
                || FindSceneObject(scene, "Water") != null)
            {
                throw new InvalidOperationException(
                    "Production terrain runtime objects already exist or the scene is invalid.");
            }

            BoundsInt bounds = groundTilemap.cellBounds;
            if (bounds.min.x != OriginX || bounds.min.y != OriginY
                || bounds.size.x != Width || bounds.size.y != Height)
            {
                throw new InvalidOperationException(
                    $"Ground bounds were origin=({bounds.min.x}, {bounds.min.y}), "
                    + $"size=({bounds.size.x}, {bounds.size.y}); expected "
                    + $"origin=({OriginX}, {OriginY}), size=({Width}, {Height}).");
            }

            Dictionary<Vector3Int, TerrainType> legacyGround =
                ReadLegacyGround(groundTilemap, waterSource, grassSource);
            CountTerrain(
                legacyGround,
                out int legacyWaterCount,
                out int legacyGrassCount);
            if (legacyWaterCount != ExpectedWaterCount
                || legacyGrassCount != ExpectedGrassCount
                || legacyGround.Count != ExpectedWaterCount + ExpectedGrassCount)
            {
                throw new InvalidOperationException(
                    $"BLOCKED_PM_PRODUCTION_COUNT_MISMATCH: Water={legacyWaterCount}, "
                    + $"Grass={legacyGrassCount}, Total={legacyGround.Count}.");
            }

            Dictionary<Vector3Int, TileBase> beachSnapshot = SnapshotTiles(beachTilemap);
            if (beachSnapshot.Count != ExpectedBeachCount)
            {
                throw new InvalidOperationException(
                    $"BLOCKED_PM_PRODUCTION_COUNT_MISMATCH: Beach="
                    + $"{beachSnapshot.Count}, expected {ExpectedBeachCount}.");
            }

            TerrainType[] cells = new TerrainType[checked(Width * Height)];
            foreach (KeyValuePair<Vector3Int, TerrainType> cell in legacyGround)
            {
                int localX = cell.Key.x - OriginX;
                int localY = cell.Key.y - OriginY;
                int index = checked(localY * Width + localX);
                cells[index] = cell.Value;
            }

            EnsureFolder("Assets/_Project/World");
            EnsureFolder("Assets/_Project/World/Terrain");
            TerrainMapData terrainMapData = ScriptableObject.CreateInstance<TerrainMapData>();
            terrainMapData.SetData(OriginX, OriginY, Width, Height, cells);
            AssetDatabase.CreateAsset(terrainMapData, TerrainDataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            terrainMapData = AssetDatabase.LoadAssetAtPath<TerrainMapData>(TerrainDataPath);
            if (terrainMapData == null)
            {
                throw new InvalidOperationException(
                    "Could not reload the persisted FirstIsland terrain data asset.");
            }

            TerrainGridData grid = terrainMapData.CreateGridData();
            if (!TerrainGridValidator.IsValid(grid))
            {
                throw new InvalidOperationException(
                    "Persisted production terrain data failed grid validation.");
            }

            CountTerrainData(
                terrainMapData,
                out int dataWaterCount,
                out int dataGrassCount,
                out int dataEmptyCount);
            VerifyCoordinateExactMatch(legacyGround, terrainMapData);

            if (dataWaterCount != ExpectedWaterCount
                || dataGrassCount != ExpectedGrassCount
                || dataEmptyCount != 0)
            {
                throw new InvalidOperationException(
                    $"BLOCKED_PM_MIGRATION_DATA_MISMATCH: data Water={dataWaterCount}, "
                    + $"Grass={dataGrassCount}, Empty={dataEmptyCount}.");
            }

            Transform visualTerrain = FindSceneObject(scene, "VisualTerrain").transform;
            TilemapRenderer groundRenderer =
                groundTilemap.GetComponent<TilemapRenderer>();
            if (groundRenderer == null)
            {
                throw new InvalidOperationException(
                    "Ground TilemapRenderer is missing.");
            }

            GameObject waterObject = new GameObject(
                "Water",
                typeof(Tilemap),
                typeof(TilemapRenderer));
            waterObject.transform.SetParent(groundTilemap.transform.parent, false);
            Tilemap waterTilemap = waterObject.GetComponent<Tilemap>();
            TilemapRenderer waterRenderer = waterObject.GetComponent<TilemapRenderer>();
            waterRenderer.sortingLayerID = groundRenderer.sortingLayerID;
            waterRenderer.sortingOrder = groundRenderer.sortingOrder - 1;

            GameObject runtimeObject = new GameObject("TerrainRuntime");
            runtimeObject.transform.SetParent(visualTerrain, false);
            TerrainTilemapRenderer terrainRenderer =
                runtimeObject.AddComponent<TerrainTilemapRenderer>();
            TerrainMapRuntimeLoader loader =
                runtimeObject.AddComponent<TerrainMapRuntimeLoader>();

            SetReference(terrainRenderer, "renderAssets", renderAssets);
            SetReference(terrainRenderer, "waterTilemap", waterTilemap);
            SetReference(terrainRenderer, "sandTilemap", null);
            SetReference(terrainRenderer, "grassTilemap", groundTilemap);
            SetReference(loader, "terrainMapData", terrainMapData);
            SetReference(loader, "renderAssets", renderAssets);
            SetReference(loader, "terrainRenderer", terrainRenderer);
            SetReference(loader, "waterTilemap", waterTilemap);
            SetReference(loader, "grassTilemap", groundTilemap);

            groundTilemap.ClearAllTiles();
            VerifyTileSnapshot(beachTilemap, beachSnapshot);
            if (groundTilemap.GetUsedTilesCount() != 0
                || waterTilemap.GetUsedTilesCount() != 0)
            {
                throw new InvalidOperationException(
                    "Legacy Ground content was not fully cleared before runtime rendering.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "Could not save the migrated FirstIsland production scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"FirstIsland terrain migration PASS: Water={dataWaterCount}, "
                + $"Grass={dataGrassCount}, Empty={dataEmptyCount}, "
                + $"Beach={beachSnapshot.Count}, bounds=({OriginX},{OriginY}) "
                + $"{Width}x{Height}; coordinates exact.");
        }

        private static Dictionary<Vector3Int, TerrainType> ReadLegacyGround(
            Tilemap tilemap,
            TileBase waterSource,
            TileBase grassSource)
        {
            Dictionary<Vector3Int, TerrainType> cells =
                new Dictionary<Vector3Int, TerrainType>();

            foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
            {
                if (position.z != 0)
                {
                    continue;
                }

                TileBase tile = tilemap.GetTile(position);
                if (tile == null)
                {
                    continue;
                }

                TerrainType terrainType;
                if (tile == waterSource)
                {
                    terrainType = TerrainType.Water;
                }
                else if (tile == grassSource)
                {
                    terrainType = TerrainType.Grass;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"BLOCKED_PM_UNEXPECTED_TILE_REFERENCE: Ground tile "
                        + $"'{tile.name}' at ({position.x}, {position.y}, {position.z}).");
                }

                cells.Add(position, terrainType);
            }

            return cells;
        }

        private static Dictionary<Vector3Int, TileBase> SnapshotTiles(Tilemap tilemap)
        {
            Dictionary<Vector3Int, TileBase> snapshot =
                new Dictionary<Vector3Int, TileBase>();
            foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = tilemap.GetTile(position);
                if (tile != null)
                {
                    snapshot.Add(position, tile);
                }
            }

            return snapshot;
        }

        private static void VerifyTileSnapshot(
            Tilemap tilemap,
            Dictionary<Vector3Int, TileBase> expected)
        {
            Dictionary<Vector3Int, TileBase> actual = SnapshotTiles(tilemap);
            if (actual.Count != expected.Count)
            {
                throw new InvalidOperationException(
                    $"Beach changed during migration: expected {expected.Count} cells, "
                    + $"found {actual.Count}.");
            }

            foreach (KeyValuePair<Vector3Int, TileBase> cell in expected)
            {
                if (!actual.TryGetValue(cell.Key, out TileBase actualTile)
                    || actualTile != cell.Value)
                {
                    throw new InvalidOperationException(
                        $"Beach tile changed at ({cell.Key.x}, {cell.Key.y}).");
                }
            }
        }

        private static void CountTerrain(
            Dictionary<Vector3Int, TerrainType> cells,
            out int waterCount,
            out int grassCount)
        {
            waterCount = 0;
            grassCount = 0;
            foreach (TerrainType terrainType in cells.Values)
            {
                if (terrainType == TerrainType.Water)
                {
                    waterCount++;
                }
                else if (terrainType == TerrainType.Grass)
                {
                    grassCount++;
                }
            }
        }

        private static void CountTerrainData(
            TerrainMapData data,
            out int waterCount,
            out int grassCount,
            out int emptyCount)
        {
            waterCount = 0;
            grassCount = 0;
            emptyCount = 0;
            for (int localY = 0; localY < data.Height; localY++)
            {
                for (int localX = 0; localX < data.Width; localX++)
                {
                    int worldX = checked(data.OriginX + localX);
                    int worldY = checked(data.OriginY + localY);
                    switch (data.GetTerrain(worldX, worldY))
                    {
                        case TerrainType.Empty:
                            emptyCount++;
                            break;
                        case TerrainType.Water:
                            waterCount++;
                            break;
                        case TerrainType.Grass:
                            grassCount++;
                            break;
                        default:
                            throw new InvalidOperationException(
                                "Production terrain data contains an unsupported terrain type.");
                    }
                }
            }
        }

        private static void VerifyCoordinateExactMatch(
            Dictionary<Vector3Int, TerrainType> legacyGround,
            TerrainMapData data)
        {
            for (int localY = 0; localY < data.Height; localY++)
            {
                for (int localX = 0; localX < data.Width; localX++)
                {
                    int worldX = checked(data.OriginX + localX);
                    int worldY = checked(data.OriginY + localY);
                    Vector3Int position = new Vector3Int(worldX, worldY, 0);
                    TerrainType expected = legacyGround.TryGetValue(
                        position,
                        out TerrainType terrainType)
                        ? terrainType
                        : TerrainType.Empty;

                    if (data.GetTerrain(worldX, worldY) != expected)
                    {
                        throw new InvalidOperationException(
                            $"BLOCKED_PM_MIGRATION_DATA_MISMATCH at "
                            + $"({worldX}, {worldY}): expected {expected}, "
                            + $"found {data.GetTerrain(worldX, worldY)}.");
                    }
                }
            }

            if (legacyGround.Count != ExpectedWaterCount + ExpectedGrassCount)
            {
                throw new InvalidOperationException(
                    "Legacy source count changed during coordinate verification.");
            }
        }

        private static Tilemap FindTilemap(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tilemap.gameObject.name == objectName)
                    {
                        return tilemap;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Could not find {objectName} Tilemap in {ScenePath}.");
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform transform in transforms)
                {
                    if (transform.gameObject.name == objectName)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int separator = folderPath.LastIndexOf('/');
            string parent = folderPath.Substring(0, separator);
            string folderName = folderPath.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object reference)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized property '{propertyName}' on {target.name}.");
            }

            property.objectReferenceValue = reference;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
