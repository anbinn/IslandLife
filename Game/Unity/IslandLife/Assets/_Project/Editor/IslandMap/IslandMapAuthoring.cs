using System;
using System.Collections.Generic;
using IslandLife.World.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace IslandLife.EditorTools.IslandMap
{
    /// <summary>
    /// Island Map Authoring session (IL-WORLD-004R).
    ///
    /// Single source of truth: the scene's <see cref="TerrainMapData"/> asset. The Tilemaps the
    /// user sees while authoring are a *preview* rebuilt from that data. They are created with
    /// HideFlags.HideAndDontSave, so they are never serialised into the scene and opening the
    /// Map Editor can never produce a scene diff.
    ///
    /// Island-agnostic: everything is discovered from the open scene's TerrainMapRuntimeLoader,
    /// so a second or third island works with no code change.
    ///
    /// The existing render chain is reused unchanged: <see cref="TerrainTilemapRenderer"/>,
    /// TerrainSpriteResolver / TerrainRelationshipResolver and the existing Grass AutoTile.
    /// </summary>
    public static class IslandMapAuthoring
    {
        // ---------------------------------------------------------------- state

        private static bool s_Active;
        private static TerrainType s_Brush = TerrainType.Grass;
        private static TerrainMapData s_Data;
        private static TerrainGridData s_Grid;
        private static TerrainRenderAssets s_RenderAssets;
        private static TerrainTilemapRenderer s_PreviewRenderer;
        private static Tilemap s_PreviewWater;
        private static Tilemap s_PreviewGrass;
        private static Tilemap s_PreviewSand;
        private static GameObject s_PreviewRoot;
        private static bool s_StrokeActive;
        private static int s_Edits;
        private static Vector2Int s_Hover = new Vector2Int(int.MinValue, int.MinValue);
        private static string s_Status = "Press \"Start Authoring\" to preview the map in Edit Mode.";

        public static bool IsActive => s_Active;

        public static TerrainType Brush => s_Brush;

        public static string Status => s_Status;

        public static int Edits => s_Edits;

        public static TerrainMapData Data => s_Data;

        public static TerrainGridData Grid => s_Grid;

        public static Vector2Int Hover => s_Hover;

        public static bool StrokeActive => s_StrokeActive;

        public static Tilemap PreviewWater => s_PreviewWater;

        public static Tilemap PreviewGrass => s_PreviewGrass;

        public static TerrainRenderAssets RenderAssets => s_RenderAssets;

        // ---------------------------------------------------------------- terrain palette

        /// <summary>
        /// Which TerrainTypes are production-paintable right now. Extensible: once a type's
        /// render chain is production ready, flipping its case here is the only change needed.
        /// </summary>
        public static bool IsPaintable(TerrainType type, out string reason)
        {
            switch (type)
            {
                case TerrainType.Grass:
                    reason = string.Empty;
                    return true;
                case TerrainType.Water:
                    reason = string.Empty;
                    return true;
                case TerrainType.Sand:
                    reason = "Not production ready: TerrainMapRuntimeLoader rejects Sand cells and the "
                           + "scene has no Sand Tilemap assigned. Infrastructure exists, painting does not.";
                    return false;
                case TerrainType.Empty:
                    reason = "Not part of coastline authoring (punches holes in the rendered map).";
                    return false;
                default:
                    reason = "Unsupported terrain type.";
                    return false;
            }
        }

        // ---------------------------------------------------------------- lifecycle

        [InitializeOnLoadMethod]
        private static void InstallHooks()
        {
            EditorSceneManager.sceneOpened -= OnSceneChanged;
            EditorSceneManager.sceneOpened += OnSceneChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnDomainReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnDomainReload;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private static void OnDomainReload()
        {
            TearDownPreview();
        }

        private static void OnSceneChanged(Scene scene, OpenSceneMode mode)
        {
            // The bound data belongs to the previous scene; drop the session rather than
            // silently retargeting it.
            TearDownPreview();
            s_Active = false;
            s_Data = null;
            s_Grid = null;
            s_RenderAssets = null;
            s_Hover = new Vector2Int(int.MinValue, int.MinValue);
            s_Status = "Scene changed - press \"Start Authoring\" to bind the new map.";
        }

        private static void OnUndoRedo()
        {
            if (!s_Active)
            {
                return;
            }

            RefreshFromData();
        }

        public static void Start()
        {
            TearDownPreview();

            TerrainMapRuntimeLoader loader =
                UnityEngine.Object.FindAnyObjectByType<TerrainMapRuntimeLoader>();
            if (loader == null)
            {
                s_Status = "No TerrainMapRuntimeLoader in this scene. Open a map scene that uses the "
                           + "terrain system.";
                s_Active = false;
                return;
            }

            var so = new SerializedObject(loader);
            s_Data = so.FindProperty("terrainMapData").objectReferenceValue as TerrainMapData;
            s_RenderAssets = so.FindProperty("renderAssets").objectReferenceValue as TerrainRenderAssets;
            Tilemap realWater = so.FindProperty("waterTilemap").objectReferenceValue as Tilemap;
            Tilemap realGrass = so.FindProperty("grassTilemap").objectReferenceValue as Tilemap;

            if (s_Data == null || s_RenderAssets == null || realWater == null || realGrass == null)
            {
                s_Status = "Terrain wiring incomplete (data / render assets / water / grass tilemap).";
                s_Active = false;
                return;
            }

            s_Grid = s_Data.CreateGridData();

            if (!BuildPreview(realWater, realGrass))
            {
                s_Status = "Could not build the Edit Mode preview rig.";
                s_Active = false;
                return;
            }

            s_PreviewRenderer.RenderAll(s_Grid);
            s_Active = true;
            s_Edits = 0;
            s_Hover = new Vector2Int(int.MinValue, int.MinValue);
            s_Status = "Authoring active. The visible Tilemaps are a preview; only TerrainMapData is "
                     + "saved. Left click / drag to paint, Ctrl+Z undo.";
            SceneView.RepaintAll();
        }

        public static void Stop()
        {
            EndStroke();
            TearDownPreview();
            s_Active = false;
            s_Grid = null;
            s_Hover = new Vector2Int(int.MinValue, int.MinValue);
            s_Status = "Stopped. Preview removed; the scene file was never modified.";
            SceneView.RepaintAll();
        }

        public static void SetBrush(TerrainType type)
        {
            if (!IsPaintable(type, out _))
            {
                return;
            }

            s_Brush = type;
            SceneView.RepaintAll();
        }

        // ---------------------------------------------------------------- preview rig

        /// <summary>
        /// Builds Grid + Water/Grass Tilemaps + a TerrainTilemapRenderer inside a
        /// HideAndDontSave hierarchy, mirroring the production sorting so the preview looks the
        /// same as the running game. Nothing here is ever written to the scene.
        /// </summary>
        private static bool BuildPreview(Tilemap realWater, Tilemap realGrass)
        {
            try
            {
                s_PreviewRoot = new GameObject("~IslandMapAuthoringPreview")
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };

                Grid grid = s_PreviewRoot.AddComponent<Grid>();

                // Match the production grid so preview cells line up with scene content.
                Grid realGrid = realWater.GetComponentInParent<Grid>();
                if (realGrid != null)
                {
                    CopySerializedProperties(realGrid, grid, GridProperties);
                    s_PreviewRoot.transform.position = realGrid.transform.position;
                }
                else
                {
                    // GridLayout cell properties are read-only on the public API in 6000.3.
                    SetSerialized(grid, "m_CellSize", new Vector3(1f, 1f, 0f));
                    SetSerialized(grid, "m_CellGap", Vector3.zero);
                }

                s_PreviewWater = CreatePreviewTilemap("Water", realWater);
                s_PreviewGrass = CreatePreviewTilemap("Grass", realGrass);

                // RenderAll unconditionally calls sandTilemap.ClearAllTiles(). A null assigned
                // through SerializedObject is a "fake null" that does not short-circuit the
                // null-conditional, so the field must hold a real Tilemap. Sand is not
                // paintable yet, so this sandbox Tilemap always stays empty.
                s_PreviewSand = CreateSandboxTilemap();
                if (s_PreviewWater == null || s_PreviewGrass == null || s_PreviewSand == null)
                {
                    return false;
                }

                s_PreviewRenderer = s_PreviewRoot.AddComponent<TerrainTilemapRenderer>();
                var rso = new SerializedObject(s_PreviewRenderer);
                rso.FindProperty("renderAssets").objectReferenceValue = s_RenderAssets;
                rso.FindProperty("waterTilemap").objectReferenceValue = s_PreviewWater;
                rso.FindProperty("sandTilemap").objectReferenceValue = s_PreviewSand;
                rso.FindProperty("grassTilemap").objectReferenceValue = s_PreviewGrass;
                rso.ApplyModifiedPropertiesWithoutUndo();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[IslandMap] preview build failed: " + e);
                TearDownPreview();
                return false;
            }
        }

        // Serialized names, used because the GridLayout/Tilemap/TilemapRenderer public property
        // surface is partly read-only or renamed in Unity 6000.3. Copying the serialized fields
        // keeps the preview visually identical to production without hardcoding any island value.
        private static readonly string[] GridProperties =
        {
            "m_CellSize", "m_CellGap", "m_CellLayout", "m_CellSwizzle",
        };

        private static readonly string[] TilemapProperties =
        {
            "m_TileAnchor", "m_TileOrientation", "m_Color", "m_Origin", "m_Size",
        };

        private static readonly string[] TilemapRendererProperties =
        {
            "m_Mode", "m_SortingLayer", "m_SortingLayerID", "m_SortOrder", "m_SortingOrder",
            "m_Materials", "m_MaskInteraction", "m_DetectChunkCullingBounds",
        };

        private static void CopySerializedProperties(UnityEngine.Object source,
            UnityEngine.Object destination, string[] names)
        {
            var from = new SerializedObject(source);
            var to = new SerializedObject(destination);
            foreach (string name in names)
            {
                SerializedProperty p = from.FindProperty(name);
                if (p != null && to.FindProperty(name) != null)
                {
                    to.CopyFromSerializedProperty(p);
                }
            }

            to.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerialized(UnityEngine.Object target, string name, Vector3 value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
            {
                return;
            }

            p.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Tilemap CreatePreviewTilemap(string name, Tilemap real)
        {
            var go = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.transform.SetParent(s_PreviewRoot.transform, false);
            go.transform.position = real.transform.position;
            go.transform.localScale = real.transform.localScale;

            Tilemap map = go.AddComponent<Tilemap>();
            CopySerializedProperties(real, map, TilemapProperties);

            TilemapRenderer src = real.GetComponent<TilemapRenderer>();
            TilemapRenderer dst = go.AddComponent<TilemapRenderer>();
            if (src != null)
            {
                CopySerializedProperties(src, dst, TilemapRendererProperties);
            }

            return map;
        }

        private static Tilemap CreateSandboxTilemap()
        {
            var go = new GameObject("SandSandbox")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.transform.SetParent(s_PreviewRoot.transform, false);
            return go.AddComponent<Tilemap>();
        }

        private static void TearDownPreview()
        {
            s_PreviewRenderer = null;
            s_PreviewWater = null;
            s_PreviewGrass = null;
            s_PreviewSand = null;

            if (s_PreviewRoot != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(s_PreviewRoot);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(s_PreviewRoot);
                }
            }

            s_PreviewRoot = null;
        }

        // ---------------------------------------------------------------- painting

        /// <summary>
        /// Paints one cell in the authoritative TerrainMapData and refreshes the preview
        /// neighbourhood. Returns false - and changes nothing - for out-of-bounds, non-paintable
        /// and no-op cases.
        /// </summary>
        public static bool TryPaintCell(int x, int y)
        {
            if (!s_Active || s_Data == null || s_Grid == null || s_PreviewRenderer == null)
            {
                return false;
            }

            if (!IsPaintable(s_Brush, out _))
            {
                return false;
            }

            // Bounds guard: never grow the map, never move the origin.
            if (!s_Grid.IsInside(x, y))
            {
                return false;
            }

            if (s_Grid.GetTerrain(x, y) == s_Brush)
            {
                return false;
            }

            Undo.RecordObject(s_Data, "Paint " + s_Brush);

            var so = new SerializedObject(s_Data);
            SerializedProperty cells = so.FindProperty("cells");
            if (cells == null || !cells.isArray)
            {
                return false;
            }

            long index = ((long)y - s_Grid.OriginY) * s_Grid.Width + ((long)x - s_Grid.OriginX);
            if (index < 0 || index >= cells.arraySize)
            {
                return false;
            }

            cells.GetArrayElementAtIndex((int)index).intValue = (int)s_Brush;
            so.ApplyModifiedProperties();

            s_Grid.SetTerrain(x, y, s_Brush);

            // The existing renderer + resolver chain handles adjacency (3x3 neighbourhood).
            s_PreviewRenderer.RefreshCell(s_Grid, x, y);

            EditorUtility.SetDirty(s_Data);
            SceneView.RepaintAll();
            s_Edits++;
            return true;
        }

        public static void BeginStroke()
        {
            s_StrokeActive = true;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Paint " + s_Brush);
        }

        public static void EndStroke()
        {
            s_StrokeActive = false;
            SceneView.RepaintAll();
        }

        /// <summary>Rebuilds the working grid and the whole preview from TerrainMapData.</summary>
        public static void RefreshFromData()
        {
            if (!s_Active || s_Data == null || s_PreviewRenderer == null)
            {
                return;
            }

            s_Grid = s_Data.CreateGridData();
            s_PreviewRenderer.RenderAll(s_Grid);
            SceneView.RepaintAll();
        }

        // ---------------------------------------------------------------- picking / hover

        /// <summary>Resolves a world-space ray onto a terrain cell. Returns false when the ray
        /// does not hit the map plane at all.</summary>
        internal static bool TryRayToCell(Ray ray, out int x, out int y)
        {
            x = 0;
            y = 0;

            Tilemap map = s_PreviewWater != null ? s_PreviewWater : s_PreviewGrass;
            if (map == null || s_Grid == null)
            {
                return false;
            }

            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, map.transform.position.z));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3Int cell = map.WorldToCell(ray.GetPoint(distance));
            x = cell.x;
            y = cell.y;
            return true;
        }

        internal static bool TryCellFromMouse(Event e, out int x, out int y)
        {
            x = 0;
            y = 0;
            if (s_PreviewWater == null || s_Grid == null)
            {
                return false;
            }

            return TryRayToCell(HandleUtility.GUIPointToWorldRay(e.mousePosition), out x, out y);
        }

        internal static void SetHover(int x, int y)
        {
            s_Hover = new Vector2Int(x, y);
        }

        internal static void ClearHover()
        {
            s_Hover = new Vector2Int(int.MinValue, int.MinValue);
        }

        internal static Vector3 CellCenterWorld(Vector3Int cell)
        {
            Tilemap map = s_PreviewGrass != null ? s_PreviewGrass : s_PreviewWater;
            Vector3 size = map != null ? map.cellSize : Vector3.one;
            Vector3 origin = map != null ? map.transform.position : Vector3.zero;
            return origin + new Vector3((cell.x + 0.5f) * size.x, (cell.y + 0.5f) * size.y, 0f);
        }

        // ---------------------------------------------------------------- scene input

        /// <summary>
        /// Handles one Scene View event. Returns true when the event was consumed by the tool,
        /// so the window knows to repaint. Contains no SceneView dependency, which keeps the
        /// interaction logic independently testable.
        /// </summary>
        internal static bool HandleSceneEvent(Event e)
        {
            if (!s_Active)
            {
                return false; // never consume normal Scene View input while inactive
            }

            switch (e.type)
            {
                case EventType.MouseMove:
                case EventType.MouseDrag:
                    if (TryCellFromMouse(e, out int hx, out int hy))
                    {
                        SetHover(hx, hy);
                    }

                    if (e.type == EventType.MouseDrag && s_StrokeActive && e.button == 0 && !e.alt
                        && TryCellFromMouse(e, out int px, out int py))
                    {
                        TryPaintCell(px, py);
                    }

                    return true;

                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt && TryCellFromMouse(e, out int dx, out int dy))
                    {
                        SetHover(dx, dy);
                        BeginStroke();
                        TryPaintCell(dx, dy);
                        e.Use();
                        return true;
                    }

                    return false;

                case EventType.MouseUp:
                    if (e.button == 0 && s_StrokeActive)
                    {
                        EndStroke();
                        e.Use();
                        return true;
                    }

                    return false;

                default:
                    return false;
            }
        }

        // ---------------------------------------------------------------- scene overlay

        internal static void DrawSceneOverlay()
        {
            if (!s_Active || s_Grid == null || s_PreviewGrass == null)
            {
                return;
            }

            Handles.zTest = CompareFunction.LessEqual;

            if (s_Hover.x != int.MinValue)
            {
                bool inside = s_Grid.IsInside(s_Hover.x, s_Hover.y);
                Color fill = inside ? new Color(1f, 0.9f, 0.3f, 0.30f) : new Color(1f, 0.3f, 0.3f, 0.30f);
                Color edge = inside ? new Color(1f, 0.65f, 0f, 1f) : new Color(1f, 0.2f, 0.2f, 1f);

                Vector3 c = CellCenterWorld(new Vector3Int(s_Hover.x, s_Hover.y, 0));
                float w = s_PreviewGrass.cellSize.x * 0.5f;
                float h = s_PreviewGrass.cellSize.y * 0.5f;
                Handles.DrawSolidRectangleWithOutline(
                    new[]
                    {
                        c + new Vector3(-w, -h, 0f), c + new Vector3(w, -h, 0f),
                        c + new Vector3(w, h, 0f), c + new Vector3(-w, h, 0f),
                    },
                    fill, edge);

                string type = inside ? s_Grid.GetTerrain(s_Hover.x, s_Hover.y).ToString()
                    : "OUT OF BOUNDS";
                Handles.Label(
                    c + new Vector3(0f, -h - 0.4f, 0f),
                    string.Format("({0}, {1})  {2}", s_Hover.x, s_Hover.y, type));
            }

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12f, 12f, 460f, 74f), GUI.skin.box);
            GUILayout.Label("ISLAND MAP EDITOR  -  AUTHORING", EditorStyles.boldLabel);
            GUILayout.Label(string.Format("Brush: {0}     Brush Size = 1     Edits: {1}", s_Brush, s_Edits));
            GUILayout.Label(
                "Left click / drag to paint.  Ctrl+Z undo, Ctrl+Y redo.  Preview only - save the TerrainMapData asset.");
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}