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
        private static Tilemap s_PreviewRaisedVisual;
        private static GameObject s_PreviewRoot;
        private static bool s_StrokeActive;
        private static ElevationLevel s_ElevationBrush = ElevationLevel.Normal;
        private static int s_Edits;
        private static Vector2Int s_Hover = new Vector2Int(int.MinValue, int.MinValue);
        private static string s_Status = "Press \"Start Authoring\" to preview the map in Edit Mode.";

        public static bool IsActive => s_Active;

        public static TerrainType Brush => s_Brush;

        /// <summary>Elevation brush selection; Normal means the base-terrain brush is active.</summary>
        public static ElevationLevel ElevationBrush => s_ElevationBrush;

        public static bool IsElevationBrush => s_ElevationBrush != ElevationLevel.Normal;

        public static string Status => s_Status;

        public static int Edits => s_Edits;

        public static TerrainMapData Data => s_Data;

        public static TerrainGridData Grid => s_Grid;

        public static Vector2Int Hover => s_Hover;

        public static bool StrokeActive => s_StrokeActive;

        public static Tilemap PreviewWater => s_PreviewWater;

        public static Tilemap PreviewGrass => s_PreviewGrass;

        /// <summary>
        /// The preview's pure-visual Raised/Hills layer. It is a projection of the logical Raised
        /// mask, never a data source, and it may legitimately hold tiles one row south of a Raised
        /// mask.
        /// </summary>
        public static Tilemap PreviewRaisedVisual => s_PreviewRaisedVisual;

        /// <summary>Diagnostics for the most recent Raised projection, or null when none is active.</summary>
        public static RaisedVisualPlan RaisedPlan => s_PreviewRenderer?.LastRaisedPlan;

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
            s_ElevationBrush = ElevationLevel.Normal;
            SceneView.RepaintAll();
        }

        /// <summary>Selects an elevation brush. Normal returns to the base-terrain palette.</summary>
        public static bool SetElevationBrush(ElevationLevel level, out string reason)
        {
            if (level == ElevationLevel.Normal)
            {
                s_ElevationBrush = ElevationLevel.Normal;
                reason = string.Empty;
                SceneView.RepaintAll();
                return true;
            }

            // Low Ground is not shippable. The author ships no pit or depression art, so the
            // editor refuses to author it rather than inventing a look out of unrelated tiles.
            // The ElevationLevel.Lowered value itself is untouched and still loads, but a cell
            // holding it draws as flat Normal ground.
            if (level != ElevationLevel.Raised)
            {
                reason =
                    "Low Ground (-1) is not available: the Sprout Lands author art has no pit "
                    + "or depression tiles. Only High Ground (+1) and Normal Ground (0) ship.";
                return false;
            }

            // The High Ground brush authors real logical data that is stored, loaded and kept for
            // future collision / occupancy queries. Since IL-WORLD-004S-R6 it also produces a hill
            // VISUAL: RaisedRegionAnalyzer classifies the connected region and
            // AuthorHillsCompositionResolver projects it onto the author's verified Hills.png cells
            // in a separate pure-visual Tilemap. A region the author never drew - a 2-wide run, a
            // turning outline, a concave notch, a rectangle taller than 3 - is reported as
            // UNSUPPORTED_AUTHOR_GRAMMAR and draws no hill art at all; nothing is ever guessed.
            s_ElevationBrush = level;
            reason = string.Empty;
            SceneView.RepaintAll();
            return true;
        }

        /// <summary>Elevation can only be authored where the base terrain is land.</summary>
        public static bool CanPaintElevationHere(int x, int y)
        {
            return s_Grid != null && s_Grid.IsInside(x, y)
                && s_Grid.GetTerrain(x, y) == TerrainType.Grass;
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

                // Pure-visual Raised/Hills layer, sorted one step above Grass so the author's opaque
                // hill cells read as REPLACE rather than as a second grass image. It is transient and
                // is destroyed with the rest of the preview; it never becomes a data source.
                s_PreviewRaisedVisual = CreatePreviewTilemap("RaisedVisual", s_PreviewGrass, 1);
                if (s_PreviewWater == null || s_PreviewGrass == null || s_PreviewSand == null
                    || s_PreviewRaisedVisual == null)
                {
                    return false;
                }

                s_PreviewRenderer = s_PreviewRoot.AddComponent<TerrainTilemapRenderer>();
                var rso = new SerializedObject(s_PreviewRenderer);
                rso.FindProperty("renderAssets").objectReferenceValue = s_RenderAssets;
                rso.FindProperty("waterTilemap").objectReferenceValue = s_PreviewWater;
                rso.FindProperty("sandTilemap").objectReferenceValue = s_PreviewSand;
                rso.FindProperty("grassTilemap").objectReferenceValue = s_PreviewGrass;
                rso.FindProperty("raisedVisualTilemap").objectReferenceValue = s_PreviewRaisedVisual;
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

        private static Tilemap CreatePreviewTilemap(string name, Tilemap real, int sortOffset = 0)
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
                if (sortOffset != 0)
                {
                    var order = new SerializedObject(dst);
                    SerializedProperty property = order.FindProperty("m_SortOrder");
                    if (property == null)
                    {
                        property = order.FindProperty("m_SortingOrder");
                    }

                    if (property != null)
                    {
                        property.intValue = src.sortingOrder + sortOffset;
                        order.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
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
            s_PreviewRaisedVisual = null;

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
        ///
        /// Elevation brushes use the same entry point; see <see cref="s_ElevationBrush"/>.
        /// </summary>
        public static bool TryPaintCell(int x, int y)
        {
            if (!s_Active || s_Data == null || s_Grid == null || s_PreviewRenderer == null)
            {
                return false;
            }

            if (s_ElevationBrush != ElevationLevel.Normal)
            {
                return TryPaintElevation(x, y, s_ElevationBrush);
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

            long index = CellIndex(x, y);
            if (index < 0 || index >= cells.arraySize)
            {
                return false;
            }

            cells.GetArrayElementAtIndex((int)index).intValue = (int)s_Brush;
            so.ApplyModifiedProperties();

            s_Grid.SetTerrain(x, y, s_Brush);

            // Water is always flat ground. Resetting an existing elevation here is legitimate, but
            // a legacy map that has never had elevation data must NOT gain an elevations array as a
            // side effect of a plain Grass/Water paint, so the array is only touched when one
            // already exists.
            if (s_Brush != TerrainType.Grass)
            {
                ResetElevationIfPresent(x, y);
            }

            // The existing renderer + resolver chain handles adjacency (3x3 neighbourhood).
            s_PreviewRenderer.RefreshCell(s_Grid, x, y);

            EditorUtility.SetDirty(s_Data);
            SceneView.RepaintAll();
            s_Edits++;
            return true;
        }

        /// <summary>
        /// Paints only the elevation of a legal land cell. The base terrain is never changed by an
        /// elevation brush, which is what keeps Raised independent of Grass.
        ///
        /// Only Raised and Normal are accepted. Lowered is refused here as well as in
        /// <see cref="SetElevationBrush"/>, so no code path in the editor can author low ground.
        /// </summary>
        internal static bool TryPaintElevation(int x, int y, ElevationLevel level)
        {
            if (!s_Grid.IsInside(x, y))
            {
                return false;
            }

            // Conservative rule: elevation only exists on land, so a High Ground brush must never
            // create a floating plateau out of open water.
            if (s_Grid.GetTerrain(x, y) != TerrainType.Grass)
            {
                return false;
            }

            // No author pit art exists, so Low Ground is never authored. Only clearing an existing
            // Lowered value back to Normal is meaningful.
            if (level != ElevationLevel.Raised && level != ElevationLevel.Normal)
            {
                return false;
            }

            if (s_Grid.GetElevation(x, y) == level)
            {
                return false;
            }

            // Undo is recorded before any mutation, and the elevation array is grown only here -
            // inside the single real-edit path - so reading, hovering and no-op paints never
            // dirty or re-serialize a legacy map that has no elevation data yet.
            Undo.RecordObject(s_Data, "Paint " + level);

            if (!EnsureElevationArrayLength(s_Grid.Width * s_Grid.Height))
            {
                return false;
            }

            var so = new SerializedObject(s_Data);
            SerializedProperty elevations = so.FindProperty("elevations");
            long index = CellIndex(x, y);
            if (elevations == null || index < 0 || index >= elevations.arraySize)
            {
                return false;
            }

            elevations.GetArrayElementAtIndex((int)index).intValue = (int)level;
            so.ApplyModifiedProperties();

            s_Grid.SetElevation(x, y, level);
            s_PreviewRenderer.RefreshCell(s_Grid, x, y);

            EditorUtility.SetDirty(s_Data);
            SceneView.RepaintAll();
            s_Edits++;
            return true;
        }

        private static long CellIndex(int x, int y)
        {
            return ((long)y - s_Grid.OriginY) * s_Grid.Width + ((long)x - s_Grid.OriginX);
        }

        /// <summary>
        /// Clears a cell's elevation only if the map already carries elevation data. Used when the
        /// base terrain changes to Water, so a legacy all-Normal map stays legacy.
        /// </summary>
        private static void ResetElevationIfPresent(int x, int y)
        {
            var so = new SerializedObject(s_Data);
            SerializedProperty elevations = so.FindProperty("elevations");
            if (elevations == null || !elevations.isArray
                || elevations.arraySize != s_Grid.Width * s_Grid.Height)
            {
                return;
            }

            long index = CellIndex(x, y);
            if (index < 0 || index >= elevations.arraySize)
            {
                return;
            }

            if (elevations.GetArrayElementAtIndex((int)index).intValue == (int)ElevationLevel.Normal)
            {
                return;
            }

            elevations.GetArrayElementAtIndex((int)index).intValue = (int)ElevationLevel.Normal;
            so.ApplyModifiedProperties();
            s_Grid.SetElevation(x, y, ElevationLevel.Normal);
        }

        /// <summary>
        /// Grows the serialized elevation array to match the cell array. Maps that have never had
        /// elevation authored have no such array at all; this materialises it lazily on the first
        /// real edit, so untouched legacy maps are never rewritten.
        /// </summary>
        private static bool EnsureElevationArrayLength(int required)
        {
            var so = new SerializedObject(s_Data);
            SerializedProperty elevations = so.FindProperty("elevations");
            if (elevations == null || !elevations.isArray)
            {
                return false;
            }

            if (elevations.arraySize == required)
            {
                return true;
            }

            int previous = elevations.arraySize;
            elevations.arraySize = required;
            if (required > previous)
            {
                for (int i = previous; i < required; i++)
                {
                    elevations.GetArrayElementAtIndex(i).intValue = (int)ElevationLevel.Normal;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>
        /// The exact text drawn over an unsupported Raised region. Extracted so the diagnostic can be
        /// asserted by tests without needing a live Scene View repaint. IL-WORLD-004S-R9.
        /// </summary>
        internal static string BuildUnsupportedRaisedLabel(RaisedRegion region)
        {
            RaisedRegionUnsupportedReason reason =
                region.UnsupportedReason ?? RaisedRegionUnsupportedReason.WIDTH_NOT_PROVEN;
            return string.Format(
                "Unsupported Raised Shape  [{0}]  {1}  ({2}x{3}, {4} cells)",
                RaisedRegionReasons.ShortCode(reason),
                RaisedRegionReasons.Explain(reason),
                region.Width, region.Height, region.CellCount);
        }
        /// <summary>
        /// IL-WORLD-004S-R9. Draws the Raised cells the author CANNOT express, so that "no hill art"
        /// never reads as "your work vanished".
        ///
        /// This is an EDITOR DIAGNOSTIC ONLY. It draws nothing into the runtime terrain, generates no
        /// hill Sprite, and never mutates TerrainMapData: no modal, no blocked painting, no undo of the
        /// user's stroke, no automatic shape change. Every refused region keeps its logical Raised
        /// cells and simply gets a visible outline plus a reason.
        /// </summary>
        private static void DrawUnsupportedRaisedDiagnostics()
        {
            RaisedVisualPlan plan = RaisedPlan;
            if (plan == null || plan.UnsupportedRegions.Count == 0 || s_Grid == null
                || s_PreviewGrass == null)
            {
                return;
            }

            float w = s_PreviewGrass.cellSize.x * 0.5f;
            float h = s_PreviewGrass.cellSize.y * 0.5f;
            Color fill = new Color(1f, 0.25f, 0.2f, 0.35f);
            Color edge = new Color(1f, 0.15f, 0.1f, 1f);

            foreach (RaisedRegion region in plan.UnsupportedRegions)
            {
                // One translucent cell per Raised cell, so the user sees exactly what they painted.
                foreach (Vector2Int cell in region.Cells)
                {
                    if (!s_Grid.IsInside(cell.x, cell.y))
                    {
                        continue;
                    }

                    Vector3 c = CellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                    Handles.DrawSolidRectangleWithOutline(
                        new[]
                        {
                            c + new Vector3(-w, -h, 0f), c + new Vector3(w, -h, 0f),
                            c + new Vector3(w, h, 0f), c + new Vector3(-w, h, 0f),
                        },
                        fill, edge);
                }

                // One label per region, anchored above its bounding box.
                Vector3 top = CellCenterWorld(new Vector3Int(region.MinX, region.MaxY, 0));
                Handles.Label(
                    top + new Vector3(0f, h + 0.35f, 0f),
                    BuildUnsupportedRaisedLabel(region),
                    EditorStyles.whiteBoldLabel);
            }
        }
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
                if (inside && s_Grid.GetTerrain(s_Hover.x, s_Hover.y) == TerrainType.Grass)
                {
                    type += " / " + s_Grid.GetElevation(s_Hover.x, s_Hover.y);
                }

                Handles.Label(
                    c + new Vector3(0f, -h - 0.4f, 0f),
                    string.Format("({0}, {1})  {2}", s_Hover.x, s_Hover.y, type));
            }

            DrawUnsupportedRaisedDiagnostics();

            // IL-WORLD-004R fix: this overlay is drawn from a Repaint-only code path.
            // GUILayout cannot be used here. GUILayout controls are positioned from the layout
            // groups built during EventType.Layout; a Repaint-only path never registers any
            // control, so Unity raises
            //   ArgumentException: Getting control 0's position in a group with only 0
            //   controls when doing repaint
            // on every Scene View repaint. Fixed-rect GUI controls carry their own position and
            // need no Layout pass, so they are safe to draw from Repaint alone.
            Handles.BeginGUI();
            int unsupportedCount = 0;
            RaisedVisualPlan planForPanel = RaisedPlan;
            if (planForPanel != null)
            {
                unsupportedCount = planForPanel.UnsupportedRegions.Count;
            }

            var area = new Rect(12f, 12f, 460f, unsupportedCount > 0 ? 94f : 74f);
            GUI.Box(area, GUIContent.none);
            GUI.Label(
                new Rect(area.x + 6f, area.y + 4f, area.width - 12f, 18f),
                "ISLAND MAP EDITOR  -  AUTHORING",
                EditorStyles.boldLabel);
            GUI.Label(
                new Rect(area.x + 6f, area.y + 23f, area.width - 12f, 18f),
                string.Format(
                    "Brush: {0}     Brush Size = 1     Edits: {1}",
                    IsElevationBrush ? s_ElevationBrush + " ground" : s_Brush.ToString(),
                    s_Edits));
            GUI.Label(
                new Rect(area.x + 6f, area.y + 42f, area.width - 12f, 18f),
                "Left click / drag to paint.  Ctrl+Z undo, Ctrl+Y redo.  Preview only - save the "
                + "TerrainMapData asset.");

            if (unsupportedCount > 0)
            {
                // Fixed-rect label only. GUILayout is deliberately not used anywhere in this
                // Repaint-only path; see the IL-WORLD-004R note above.
                var warn = new Rect(area.x + 6f, area.y + 62f, area.width - 12f, 30f);
                GUI.Box(warn, GUIContent.none);
                GUI.Label(warn, string.Format(
                        "{0} Raised region(s) cannot be drawn with the author's Hills art and are "
                        + "outlined in red. Your Raised cells are kept; no hill art is invented.",
                        unsupportedCount),
                    EditorStyles.wordWrappedMiniLabel);
            }

            Handles.EndGUI();
        }
    }
}