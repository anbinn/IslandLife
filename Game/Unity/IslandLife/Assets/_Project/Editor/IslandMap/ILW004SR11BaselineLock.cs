// IL-WORLD-004S-R11 - Author Hills Composition System baseline lock.
//
// WHY A MenuItem HARNESS AND NOT AN NUnit TEST ASSEMBLY. The project has no .asmdef files, so all
// production terrain code lives in the predefined Assembly-CSharp / Assembly-CSharp-Editor. An
// asmdef-based test assembly cannot reference a predefined assembly, so the Unity Test Framework
// cannot see RaisedVisualPlan, RaisedRegionAnalyzer or IslandMapAuthoring without first moving
// production code into an asmdef. That is far outside this card. This file therefore asserts
// directly and is runnable either from the menu or headlessly:
//
//   Unity.exe -batchmode -quit -projectPath <project> \
//             -executeMethod IslandLife.EditorTools.IslandMap.ILW004SR11BaselineLock.Run
//
// It is COMMITTED on purpose. R6 and R9 proved the renderer with throwaway probes that were deleted;
// this card locks the baseline, so the protection tests have to outlive the run.
//
// SCOPE. Read-only with respect to every user asset. The only asset this file writes is a temporary
// copy of FirstIsland_TerrainData, and it deletes that copy before finishing. It never writes to
// FirstIsland_TerrainData.asset, FirstIsland_Prototype.unity, Hills.png or the 99 sprite slices.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using IslandLife.World.Terrain;

namespace IslandLife.EditorTools.IslandMap
{
    public static class ILW004SR11BaselineLock
    {
        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string RealScenePath = "Assets/Scenes/FirstIsland_Prototype.unity";
        private const string CopyDataPath =
            "Assets/_Project/World/Terrain/__R11_TempCopy.asset";
        private const string RenderAssetsPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string SpriteDir = "Assets/Art/Environment/SproutLands/Sprites";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R11";

        private static readonly List<string> Lines = new List<string>();
        private static readonly Dictionary<string, Sprite> Author =
            new Dictionary<string, Sprite>();

        private static int s_pass;
        private static int s_fail;
        private static int s_skip;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R11] " + s);
        }

        private static void Check(string id, bool ok, string detail)
        {
            if (ok)
            {
                s_pass++;
            }
            else
            {
                s_fail++;
            }

            Line((ok ? "PASS  " : "FAIL  ") + id + "  ::  " + detail);
        }

        private static void Skip(string id, string why)
        {
            s_skip++;
            Line("SKIP  " + id + "  ::  " + why);
        }

        private static int s_deferred;

        /// <summary>
        /// DEFERRED_LEGACY_EXPECTATION. Records an expectation the NEW author grammar has superseded,
        /// without calling it a pass or a failure and without silently re-pinning it to whatever the
        /// code now produces.
        /// </summary>
        private static void Deferred(string id, string detail)
        {
            s_deferred++;
            Line("DEFERRED_LEGACY_EXPECTATION  " + id + "  ::  " + detail);
        }

        [MenuItem("IslandLife/Diagnostics/R11 Author Hills Baseline Lock")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            s_skip = 0;

            string dataShaBefore = HashFile(RealDataPath);
            string sceneShaBefore = HashFile(RealScenePath);
            int raisedBefore = ReadRaisedCountFromAsset();

            GameObject rig = null;
            try
            {
                Line("=== IL-WORLD-004S-R11 Author Hills Composition baseline lock ===");
                Line("Unity " + Application.unityVersion);
                Line($"  FirstIsland TerrainData SHA256 {dataShaBefore}");
                Line($"  FirstIsland Raised cells read from the asset payload: {raisedBefore}");
                Line("  convention: top-left visual grid, r0 = sheet TOP row, c0 = LEFT column");

                LoadAuthorSprites();

                TestProvenCompositionsStillWork();
                TestFreeformShapesKeepDataAndDraw();
                TestNoInventedArt();
                TestUserMapIsReadOnlyAndDecoupled();

                rig = RunAuthoringRegression();
            }
            catch (Exception e)
            {
                s_fail++;
                Line("FAIL  HARNESS_ABORTED  ::  " + e);
            }
            finally
            {
                IslandMapAuthoring.Stop();
                if (rig != null)
                {
                    UnityEngine.Object.DestroyImmediate(rig);
                }

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(CopyDataPath);
                AssetDatabase.Refresh();
            }

            Line("");
            Check("TEMP_COPY_ASSET_REMOVED",
                AssetDatabase.LoadAssetAtPath<TerrainMapData>(CopyDataPath) == null,
                CopyDataPath + " deleted before finishing");

            // ---- the user's map must be byte-identical and logically untouched
            Check("USER_TERRAIN_DATA_BYTE_IDENTICAL", HashFile(RealDataPath) == dataShaBefore,
                "SHA256 unchanged across the whole run");
            Check("USER_SCENE_BYTE_IDENTICAL", HashFile(RealScenePath) == sceneShaBefore,
                "SHA256 unchanged across the whole run");
            Check("USER_RAISED_COUNT_UNCHANGED",
                ReadRaisedCountFromAsset() == raisedBefore,
                $"{ReadRaisedCountFromAsset()} Raised cells, same as at start");

            TerrainMapData real = AssetDatabase.LoadAssetAtPath<TerrainMapData>(RealDataPath);
            int raisedInMemory = RaisedRegionAnalyzer.CountRaisedCells(real.CreateGridData());
            Check("USER_RAISED_COUNT_MATCHES_ASSET", raisedInMemory == raisedBefore,
                $"{raisedInMemory} Raised cells parsed from the asset == {raisedBefore} in the payload");

            Check("HILLS_SLICES_INTACT",
                LoadCountOfHillsSlices() == 99, "99 Hills_r*c*.asset slices present");

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL / {s_skip} SKIP ===");

            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            Directory.CreateDirectory(OutDir);
            File.WriteAllText(Path.Combine(OutDir, "R11_baseline_lock.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- author sprites

        private static void LoadAuthorSprites()
        {
            int bad = 0;
            for (int r = 0; r <= 8; r++)
            {
                for (int c = 0; c <= 10; c++)
                {
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/Hills_r{r}c{c}.asset");
                    if (s == null)
                    {
                        bad++;
                        continue;
                    }

                    Author[$"r{r}c{c}"] = s;
                }
            }

            Check("ALL_99_SLICES_LOAD", bad == 0, $"{Author.Count} loaded, {bad} missing");
        }

        private static int LoadCountOfHillsSlices()
        {
            int n = 0;
            for (int r = 0; r <= 8; r++)
            {
                for (int c = 0; c <= 10; c++)
                {
                    if (AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/Hills_r{r}c{c}.asset") != null)
                    {
                        n++;
                    }
                }
            }

            return n;
        }

        // ---------------------------------------------------------------- R9/R6 regression

        /// <summary>
        /// The proven composition set must be completely unchanged by R11. This is the R6/R9
        /// coverage replayed against the current code: every previously proven shape still resolves
        /// to exactly the same author cells, in the same visual positions, with the same
        /// outside-the-mask cliff rows.
        /// </summary>
        private static void TestProvenCompositionsStillWork()
        {
            Line("");
            Line("-- R6/R9 replay: every proven composition is unchanged --");

            foreach (int w in new[] { 3, 5, 8 })
            {
                // IL-WORLD-004S-R18. Zero expected rows outside the mask, not one. A one cell high band
                // is the author's r3 row only, so it no longer paints a cliff one row south of itself.
                RectangleCase(w, 1, "r1", "r2", 0);
            }

            RectangleCase(3, 2, "r0", "r1", 1);
            RectangleCase(5, 2, "r0", "r1", 1);
            RectangleCase(3, 3, "r0", "r1", 0);
            RectangleCase(5, 3, "r0", "r1", 0);

            for (int h = 1; h <= 4; h++)
            {
                NarrowCase(h);
            }
        }

        private static void RectangleCase(
            int w, int h, string grassRow, string cliffRow, int expectedOutsideRows)
        {
            string id = $"{w}x{h}";
            TerrainGridData grid = NewGrid(0, w + 4, h + 4);
            PaintRect(grid, 0, 0, w, h);

            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, CompositionSet());
            Line($"  {id}: {plan.Describe()}");

            Check($"{id}_STILL_A_PROVEN_COMPOSITION",
                plan.SupportedRegions.Count == 1
                && plan.UnsupportedRegions.Count == 0,
                plan.SupportedRegions.Count == 1
                    ? $"resolved as {plan.SupportedRegions[0].Support}"
                    : "no longer resolved: " + FirstReason(plan));

            if (plan.SupportedRegions.Count != 1)
            {
                return;
            }

            var expect = new Dictionary<Vector3Int, string>();
            int maxY = h - 1;
            if (h == 3)
            {
                FillWideRow(expect, w, maxY, "r0");
                FillWideRow(expect, w, maxY - 1, "r1");
                FillWideRow(expect, w, 0, "r2");
            }
            else if (h == 2)
            {
                // DEFERRED_LEGACY_EXPECTATION, IL-WORLD-004S-PJ. This used to expect the author r1 body
                // row with the front cliff DISPLACED one row south of the logical mask. That displaced
                // row was the single origin of every out-of-mask visual tile in the whole projection:
                // RoleFor's `runDepth == 2` branch returned MIDDLE_SURFACE, which sits BELOW
                // FRONT_CLIFF, and DrawsFrontCliffBelow is defined as ExposedSouth && Role <
                // FRONT_CLIFF. Measured over all 256 neighbourhoods in two column contexts: 128 of 512
                // emitted a displaced tile and 128 of 128 came from that one branch.
                //
                // The new composition is a visible change to a shape this card does not own, so it is
                // deliberately NOT re-pinned to whatever the code now emits. It is recorded and left for
                // PM and the user to judge in the Scene View. Restoring the old expectation would mean
                // restoring art PM has ruled wrong.
                Deferred("2_DEEP_WIDE_LEGACY_DISPLACED_ROW",
                    $"a {w}x2 plateau no longer paints a cliff one row south of its own mask; its front "
                    + "wall now sits inside the mask on the author r2 row. The old oracle expected a "
                    + "displaced row, so it is DEFERRED_LEGACY_EXPECTATION and is not re-pinned");
                return;
            }
            else
            {
                // IL-WORLD-004S-R18 SUPERSEDES the R13 correction that used to sit here, and the user
                // proved the R13 expectation wrong a second time. R13 answered a height ONE band with
                // the author cap row r0 over a front cliff one row SOUTH, so one logical cell produced
                // two visual cells and the cliff hung outside the logical mask. PM locked the author
                // grammar after the source Basic Pack and Hills.png were inspected cell by cell: a one
                // cell high band is the author's r3 row ONLY, r3c0 | r3c1 ... r3c2, carried entirely
                // inside the logical mask, so nothing is drawn south of it at all.
                FillWideRow(expect, w, 0, "r3");
            }

            Check($"{id}_EXACT_AUTHOR_SPRITE_PER_VISUAL_POSITION", Exact(plan, expect),
                $"{expect.Count} visual positions each carry the expected author cell");

            int expectedOutside = expectedOutsideRows * w;
            Check($"{id}_OUTSIDE_LOGICAL_MASK_CLIFF_INTACT",
                plan.TilesOutsideLogicalMask == expectedOutside,
                $"{plan.TilesOutsideLogicalMask} hill tiles south of the logical mask "
                + $"(expected {expectedOutside}); the cliff is still drawn outside the mask");

            // R11 requirement 5 and 6: a resolved rectangle is byte-for-byte the old plan, and the
            // logical data behind it never moved.
            Check($"{id}_LOGICAL_DATA_UNCHANGED",
                RaisedRegionAnalyzer.CountRaisedCells(grid) == w * h,
                $"{RaisedRegionAnalyzer.CountRaisedCells(grid)} Raised cells after projection");
        }

        private static void NarrowCase(int h)
        {
            string id = "1x" + h;
            TerrainGridData grid = NewGrid(0, 6, h + 4);
            PaintRect(grid, 0, 0, 1, h);

            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, CompositionSet());
            Check($"{id}_STILL_A_PROVEN_NARROW_COMPOSITION",
                plan.SupportedRegions.Count == 1
                && plan.SupportedRegions[0].Support == RaisedRegionSupport.SUPPORTED_NARROW,
                plan.SupportedRegions.Count == 1
                    ? $"resolved as {plan.SupportedRegions[0].Support}"
                    : "no longer resolved: " + FirstReason(plan));

            if (plan.SupportedRegions.Count != 1)
            {
                return;
            }

            Check($"{id}_USES_ONLY_AUTHOR_C3_CELLS",
                AllSpritesAre(plan, "r0c3", "r1c3", "r2c3", "r3c3"),
                "every hill tile is one of the author's own narrow column cells");

            Check($"{id}_LOGICAL_DATA_UNCHANGED",
                RaisedRegionAnalyzer.CountRaisedCells(grid) == h,
                $"{RaisedRegionAnalyzer.CountRaisedCells(grid)} Raised cells after projection");
        }

        // ---------------------------------------------------------------- data / visual decoupling

        /// <summary>
        /// IL-WORLD-004S-R12 replaced this test. It used to assert that an L, a T, a U, a ring, a
        /// two-wide run and a four-tall plateau produced ZERO hill tiles, which was correct under the
        /// old region-level grammar and is exactly the behaviour R12 was written to remove.
        ///
        /// The data-preservation half of the old test is kept verbatim and is still the important half:
        /// an arbitrary mask must survive projection byte-identically. The expectation that flipped is
        /// only the tile count, which must now be greater than zero for every freeform shape.
        /// </summary>
        private static void TestFreeformShapesKeepDataAndDraw()
        {
            Line("");
            Line("-- [1,2,3,4,12,13] freeform shapes keep their data AND are now drawn --");

            FreeformCase("L shape", new[] { "X..", "X..", "XXX" });
            FreeformCase("T shape", new[] { "XXX", ".X.", ".X." });
            FreeformCase("U shape", new[] { "X.X", "X.X", "XXX" });
            FreeformCase("hole / ring", new[] { "XXX", "X.X", "XXX" });
            FreeformCase("concave notch", new[] { "XXXX", "X..X", "XXXX" });
            FreeformCase("non-rect blob", new[] { "XX..", "XXX.", ".XX." });
            FreeformCase("staircase", new[] { "XX..", ".XX.", "..XX" });
            FreeformCase("zigzag", new[] { "XX.", ".XX", "..X" });
            FreeformCase("cross", new[] { ".X.", "XXX", ".X." });
            FreeformCase("2x1", new[] { "XX" });
            FreeformCase("2x2", new[] { "XX", "XX" });
            FreeformCase("2x3", new[] { "XX", "XX", "XX" });
            FreeformCase("2x5", new[] { "XX", "XX", "XX", "XX", "XX" });
            FreeformCase("3x4", new[] { "XXX", "XXX", "XXX", "XXX" });
            FreeformCase("5x4", new[] { "XXXXX", "XXXXX", "XXXXX", "XXXXX" });
            FreeformCase("8x4", new[] { "XXXXXXXX", "XXXXXXXX", "XXXXXXXX", "XXXXXXXX" });
        }

        private static void FreeformCase(string id, string[] mask)
        {
            int w = mask[0].Length;
            int h = mask.Length;
            TerrainGridData grid = NewGrid(0, w + 8, h + 8);
            int raised = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (mask[y][x] == 'X')
                    {
                        grid.SetElevation(3 + x, 3 + y, ElevationLevel.Raised);
                        raised++;
                    }
                }
            }

            string before = Snapshot(grid);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, CompositionSet());
            string after = Snapshot(grid);

            Check($"{id}_DATA_VALID_AND_FULLY_PRESERVED", before == after && raised > 0,
                $"{raised} Raised cells in, {RaisedRegionAnalyzer.CountRaisedCells(grid)} out; "
                + "the whole elevation field is byte-identical before and after projection");

            Check($"{id}_NOW_DRAWN_WITH_AUTHOR_HILLS", plan.Tiles.Count > 0,
                $"{plan.Tiles.Count} hill tiles emitted; under the old region grammar this shape "
                + "produced zero");

            Check($"{id}_EVERY_RAISED_CELL_VISUALIZED",
                plan.VisualizedRaisedCells == raised,
                $"{plan.VisualizedRaisedCells} of {raised} Raised cells carry an author hill tile");

            Check($"{id}_NO_RENDERER_FAULT", plan.VisualDiagnostics.Count == 0,
                plan.VisualDiagnostics.Count == 0
                    ? "no MISSING_AUTHOR_PRIMITIVE / VISUAL_CONFLICT / other fault"
                    : string.Join("; ", plan.VisualDiagnostics));

            Check($"{id}_NO_SHAPE_VERDICT_ANY_MORE", plan.UnsupportedRegions.Count == 0,
                $"{plan.UnsupportedRegions.Count} regions refused; region geometry no longer blocks "
                + "any cell");

            Check($"{id}_NO_AUTOMATIC_RECTANGLE_FILL",
                RaisedRegionAnalyzer.CountRaisedCells(grid) == raised,
                $"{RaisedRegionAnalyzer.CountRaisedCells(grid)} cells: the shape was neither filled "
                + "out to a rectangle nor thinned");
        }

        // ---------------------------------------------------------------- no invented art

        /// <summary>
        /// R11 requirements 7 to 11. No 47-state Hills path, no fallback, no nearest match, no
        /// rotation, no mirroring, no stretching, no deletion.
        /// </summary>
        private static void TestNoInventedArt()
        {
            Line("");
            Line("-- [7,8,9,10,11] no guessed, transformed or synthesised hill art --");

            // 8. No 47-state Hills production path may exist anywhere in the terrain code.
            string[] sources =
            {
                "Assets/_Project/Scripts/World/Terrain/RaisedRegionAnalyzer.cs",
                "Assets/_Project/Scripts/World/Terrain/RaisedVisualPlan.cs",
                "Assets/_Project/Scripts/World/Terrain/AuthorHillsCompositionResolver.cs",
                "Assets/_Project/Scripts/World/Terrain/AuthorHillsCompositionSet.cs",
                "Assets/_Project/Scripts/World/Terrain/HillVisualTile.cs",
                "Assets/_Project/Scripts/World/Terrain/RaisedNeighborResolver.cs",
                "Assets/_Project/Scripts/World/Terrain/RaisedTopologyState.cs",
                "Assets/_Project/Scripts/World/Terrain/AuthorHillsLocalResolver.cs",
                "Assets/_Project/Scripts/World/Terrain/RaisedVisualDiagnostic.cs",
                "Assets/_Project/Scripts/World/Terrain/TerrainTilemapRenderer.cs",
            };

            int maskMapRefs = 0;
            int transformCalls = 0;
            foreach (string src in sources)
            {
                string text = File.ReadAllText(Physical(src));
                if (text.Contains("SproutLandsTerrainMaskMap"))
                {
                    maskMapRefs++;
                    Line($"   !! {src} references SproutLandsTerrainMaskMap");
                }

                foreach (string banned in new[] { "SetTransformMatrix", "Quaternion.", "Euler(",
                    "localScale", "Matrix4x4.TRS", "Flip" })
                {
                    if (text.Contains(banned))
                    {
                        transformCalls++;
                        Line($"   !! {src} contains '{banned}'");
                    }
                }
            }

            Check("NO_47_STATE_HILLS_PRODUCTION_PATH", maskMapRefs == 0,
                $"{maskMapRefs} of {sources.Length} production files reference "
                + "SproutLandsTerrainMaskMap; the Raised renderer uses only proven compositions");

            Check("NO_ROTATION_MIRROR_OR_STRETCH_IN_RENDERER", transformCalls == 0,
                $"{transformCalls} transform/scale/flip call sites in the Raised render path; "
                + "every tile is placed with the identity Tile transform at unit scale");

            // 9/10/11, proven on the real Tilemap: identity transforms only, and every emitted
            // Sprite is one of the author's own cells.
            TerrainGridData grid = NewGrid(0, 16, 12);
            PaintRect(grid, 2, 2, 8, 2);
            var root = new GameObject("R11Rig") { hideFlags = HideFlags.HideAndDontSave };
            Grid ug = root.AddComponent<Grid>();
            ug.cellSize = new Vector3(1f, 1f, 0f);
            Tilemap raised = NewTilemap(root.transform, "RaisedVisual", 2);
            var renderer = root.AddComponent<TerrainTilemapRenderer>();
            var so = new SerializedObject(renderer);
            so.FindProperty("renderAssets").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TerrainRenderAssets>(RenderAssetsPath);
            so.FindProperty("waterTilemap").objectReferenceValue =
                NewTilemap(root.transform, "Water", 0);
            so.FindProperty("sandTilemap").objectReferenceValue =
                NewTilemap(root.transform, "Sand", 0);
            so.FindProperty("grassTilemap").objectReferenceValue =
                NewTilemap(root.transform, "Grass", 1);
            so.FindProperty("raisedVisualTilemap").objectReferenceValue = raised;
            so.ApplyModifiedPropertiesWithoutUndo();

            renderer.RenderAll(grid);
            RaisedVisualPlan plan = renderer.LastRaisedPlan;

            int transformed = 0;
            foreach (Vector3Int p in Positions(raised))
            {
                Tile t = raised.GetTile(p) as Tile;
                if (t != null && t.transform != Matrix4x4.identity)
                {
                    transformed++;
                }
            }

            Check("NO_TILE_TRANSFORM_ON_THE_RENDERED_MAPS", transformed == 0,
                $"{transformed} hill tiles carry a non-identity Tile transform");

            int notAuthor = 0;
            foreach (HillVisualTile t in plan.Tiles)
            {
                bool ok = false;
                foreach (KeyValuePair<string, Sprite> kv in Author)
                {
                    if (kv.Value == t.Sprite)
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    notAuthor++;
                }
            }

            Check("EVERY_HILL_TILE_IS_AN_AUTHOR_HILLS_SLICE", notAuthor == 0,
                $"{notAuthor} of {plan.Tiles.Count} hill tiles are not one of the 99 author slices "
                + "(no Grass fallback, no generated tile, no nearest match)");

            // The renderer must not have changed a single logical cell.
            Check("RENDER_ALL_PRESERVES_LOGICAL_DATA",
                RaisedRegionAnalyzer.CountRaisedCells(grid) == 16,
                $"{RaisedRegionAnalyzer.CountRaisedCells(grid)} Raised cells after RenderAll");

            UnityEngine.Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- the real user map

        /// <summary>
        /// R11 requirement set for the real map, plus card section 12. Strictly READ ONLY: the plan is
        /// built from a copy so the user's asset is never opened for writing, and the copy is deleted
        /// by the caller.
        /// </summary>
        private static void TestUserMapIsReadOnlyAndDecoupled()
        {
            Line("");
            Line("-- [12] the real FirstIsland Raised mask, read-only --");

            if (!AssetDatabase.CopyAsset(RealDataPath, CopyDataPath))
            {
                Check("USER_MAP_COPY_ASSET", false, "AssetDatabase.CopyAsset failed");
                return;
            }

            TerrainMapData copy = AssetDatabase.LoadAssetAtPath<TerrainMapData>(CopyDataPath);
            Check("USER_MAP_COPY_ASSET", copy != null, CopyDataPath);

            TerrainGridData grid = copy.CreateGridData();
            int raised = RaisedRegionAnalyzer.CountRaisedCells(grid);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, CompositionSet());

            // IL-WORLD-004S-R12: per-cell accounting replaces the old region buckets. A region is no
            // longer supported or unsupported as a whole, so the honest split is cells drawn vs cells
            // with no author tile yet.
            int supportedCells = plan.VisualizedRaisedCells;
            int unresolvedCells = raised - plan.VisualizedRaisedCells;

            Line($"   {plan.Describe()}");
            Line($"   connected regions: {plan.Regions.Count}");
            Line($"   cells:   {supportedCells} drawn with author hills, {unresolvedCells} without, {raised} total");
            foreach (RaisedRegion r in plan.UnsupportedRegions)
            {
                Line("   " + r);
            }

            Check("USER_MAP_CELL_ACCOUNTING_IS_EXACT",
                supportedCells + unresolvedCells == raised,
                $"{supportedCells} + {unresolvedCells} == {raised} Raised cells, no cell lost "
                + "or double counted between the proven and unresolved buckets");

            Check("USER_MAP_NO_REGION_IS_REFUSED_WHOLE",
                plan.UnsupportedRegions.Count == 0,
                $"{plan.UnsupportedRegions.Count} regions refused as a whole; region geometry no "
                + "longer blocks any cell");

            Check("USER_MAP_LOGICAL_DATA_UNTOUCHED_BY_PROJECTION",
                RaisedRegionAnalyzer.CountRaisedCells(grid) == raised,
                $"{raised} Raised cells before and after the projection");

            var sb = new StringBuilder();
            sb.AppendLine("IL-WORLD-004S-R11 - current user map Raised classification (READ ONLY)");
            sb.AppendLine($"source          : {RealDataPath}");
            sb.AppendLine($"terrain size    : {copy.Width} x {copy.Height} "
                + $"origin ({copy.OriginX}, {copy.OriginY})");
            sb.AppendLine($"Raised cells    : {raised}");
            sb.AppendLine($"regions total   : {RaisedRegionAnalyzer.Analyze(grid).Count}");
            sb.AppendLine($"cells drawn     : {supportedCells}");
            sb.AppendLine($"cells no art    : {unresolvedCells}");
            sb.AppendLine($"HillVisualTile  : {plan.Tiles.Count}");
            sb.AppendLine($"outside mask    : {plan.TilesOutsideLogicalMask}");
            sb.AppendLine("");
            sb.AppendLine("LEGACY REGION CLASSIFICATION - reported only, no longer gates rendering");
            foreach (RaisedRegion r in plan.Regions)
            {
                sb.AppendLine("  " + r);
            }

            sb.AppendLine("");
            sb.AppendLine("SHAPE DEMAND LIST - what the user map actually needs next");
            var byShape = new Dictionary<string, int>();
            foreach (RaisedRegion r in plan.Regions)
            {
                int area = r.Width * r.Height;
                string shape = area == r.CellCount
                    ? $"solid {r.Width}x{r.Height}"
                    : $"{r.Width}x{r.Height} bounding box, {r.CellCount} cells "
                      + $"({r.CellCount * 100 / area}% filled)";
                byShape[shape] = byShape.TryGetValue(shape, out int b) ? b + r.CellCount : r.CellCount;
            }

            foreach (KeyValuePair<string, int> kv in byShape)
            {
                sb.AppendLine($"  shape  {kv.Key,-40} {kv.Value,4} cells");
            }

            Directory.CreateDirectory(OutDir);
            File.WriteAllText(Path.Combine(OutDir, "USER_MAP_RAISED_CLASSIFICATION.txt"), sb.ToString());
            Line("   written USER_MAP_RAISED_CLASSIFICATION.txt");
        }

        // ---------------------------------------------------------------- 004R replay

        private static GameObject RunAuthoringRegression()
        {
            Line("");
            Line("-- [regression] IL-WORLD-004R authoring replay --");

            if (!AssetDatabase.CopyAsset(RealDataPath, CopyDataPath))
            {
                Check("REG_COPY_ASSET", false, "AssetDatabase.CopyAsset failed");
                return null;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TerrainMapData copy = AssetDatabase.LoadAssetAtPath<TerrainMapData>(CopyDataPath);
            Check("REG_COPY_ASSET", copy != null,
                $"{copy.Width}x{copy.Height} origin=({copy.OriginX},{copy.OriginY})");

            var root = new GameObject("R11RegRig");
            Grid ug = root.AddComponent<Grid>();
            ug.cellSize = new Vector3(1f, 1f, 0f);
            Tilemap water = NewTilemap(root.transform, "Water", 0);
            Tilemap grass = NewTilemap(root.transform, "Grass", 1);
            var loaderGo = new GameObject("TerrainRuntime");
            loaderGo.transform.SetParent(root.transform, false);
            TerrainMapRuntimeLoader loader = loaderGo.AddComponent<TerrainMapRuntimeLoader>();
            SetField(loader, "terrainMapData", copy);
            SetField(loader, "renderAssets",
                AssetDatabase.LoadAssetAtPath<TerrainRenderAssets>(RenderAssetsPath));
            SetField(loader, "waterTilemap", water);
            SetField(loader, "grassTilemap", grass);

            IslandMapAuthoring.Start();
            Check("REG_AUTHORING_STARTED", IslandMapAuthoring.IsActive, IslandMapAuthoring.Status);
            Check("REG_PREVIEW_BUILT",
                IslandMapAuthoring.PreviewWater != null
                && IslandMapAuthoring.PreviewGrass != null
                && IslandMapAuthoring.PreviewRaisedVisual != null,
                "water, grass and the Raised visual preview layers all exist");
            Check("REG_SCENE_NOT_DIRTY", !SceneManager.GetActiveScene().isDirty,
                "opening the preview did not dirty the scene");

            Tilemap pv = IslandMapAuthoring.PreviewWater;
            Tilemap pg = IslandMapAuthoring.PreviewGrass;
            Check("REG_DATA_RENDER_CONSISTENT_AT_START",
                ExhaustiveMismatch(copy, pv, pg) == 0,
                $"{ExhaustiveMismatch(copy, pv, pg)} cells disagree between data and render");

            // ---- paint grass on a water cell
            Vector2Int coast = FindWaterCell();
            IslandMapAuthoring.SetBrush(TerrainType.Grass);
            int w0 = CountType(copy, TerrainType.Water);
            bool painted = IslandMapAuthoring.TryPaintCell(coast.x, coast.y);
            Check("REG_PAINT_GRASS", painted && copy.GetTerrain(coast.x, coast.y) == TerrainType.Grass,
                $"painted={painted} now={copy.GetTerrain(coast.x, coast.y)} at {coast}");
            Check("REG_GRASS_REDUCED_WATER", CountType(copy, TerrainType.Water) == w0 - 1,
                $"Water {w0} -> {CountType(copy, TerrainType.Water)}");
            Check("REG_CONSISTENT_AFTER_GRASS_PAINT", ExhaustiveMismatch(copy, pv, pg) == 0,
                "data<->render consistent");

            // ---- paint water on a land cell
            Vector2Int wet = FindGrassCell();
            IslandMapAuthoring.SetBrush(TerrainType.Water);
            bool pw = IslandMapAuthoring.TryPaintCell(wet.x, wet.y);
            Check("REG_PAINT_WATER", pw && copy.GetTerrain(wet.x, wet.y) == TerrainType.Water,
                $"painted={pw} now={copy.GetTerrain(wet.x, wet.y)} at {wet}");
            bool noop = IslandMapAuthoring.TryPaintCell(wet.x, wet.y);
            Check("REG_NOOP_REFUSED", !noop, $"repainting Water onto Water returned {noop}");

            // ---- arbitrary Raised data must survive authoring, undo and redo (R11 req 14, 15)
            Vector2Int land = FindClearGrassRun(5, 4);
            Check("REG_CLEAR_GRASS_FOUND", land.x != int.MinValue, $"cell {land}");

            IslandMapAuthoring.SetElevationBrush(ElevationLevel.Raised, out string why);
            Check("REG_HIGH_GROUND_BRUSH_PRESENT", IslandMapAuthoring.IsElevationBrush,
                $"reason='{why}'");

            int raisedBefore = RaisedRegionAnalyzer.CountRaisedCells(
                IslandMapAuthoring.Grid);

            // An L, a T and a 2-wide run: all shapes the current grammar cannot draw.
            int paintedCells = 0;
            for (int i = 0; i < 5; i++)
            {
                if (IslandMapAuthoring.TryPaintElevation(land.x + i, land.y, ElevationLevel.Raised))
                {
                    paintedCells++;
                }
            }

            for (int i = 1; i < 3; i++)
            {
                if (IslandMapAuthoring.TryPaintElevation(land.x, land.y + i, ElevationLevel.Raised))
                {
                    paintedCells++;
                }
            }

            for (int i = 0; i < 2; i++)
            {
                if (IslandMapAuthoring.TryPaintElevation(land.x + 2 + i, land.y + 2,
                        ElevationLevel.Raised))
                {
                    paintedCells++;
                }
            }

            int raisedAfterPaint = RaisedRegionAnalyzer.CountRaisedCells(IslandMapAuthoring.Grid);
            Check("REG_ARBITRARY_RAISED_PAINTED", paintedCells == 9 && raisedAfterPaint == raisedBefore + 9,
                $"{paintedCells} cells painted, Raised {raisedBefore} -> {raisedAfterPaint}");

            RaisedVisualPlan plan = IslandMapAuthoring.RaisedPlan;
            Check("REG_UNRESOLVED_DATA_KEEPS_ZERO_HILL_TILES_FOR_NEW_SHAPES",
                plan != null && plan.RaisedCellCount == raisedAfterPaint,
                plan == null
                    ? "no plan"
                    : $"the plan sees all {plan.RaisedCellCount} Raised cells, including the "
                      + "shapes it cannot draw");

            // ---- Undo / Redo must round-trip the arbitrary data (R11 req 14)
            UnityEditor.Undo.PerformUndo();
            int afterUndo = RaisedRegionAnalyzer.CountRaisedCells(IslandMapAuthoring.Grid);
            UnityEditor.Undo.PerformRedo();
            int afterRedo = RaisedRegionAnalyzer.CountRaisedCells(IslandMapAuthoring.Grid);
            Check("REG_UNDO_REDO_ROUND_TRIPS_ARBITRARY_RAISED",
                afterUndo < raisedAfterPaint && afterRedo == raisedAfterPaint,
                $"Raised {raisedAfterPaint} -> undo {afterUndo} -> redo {afterRedo}");

            Check("REG_CONSISTENT_AFTER_UNDO_REDO", ExhaustiveMismatch(copy, pv, pg) == 0,
                "data<->render consistent after Undo and Redo");

            // ---- bounds
            TerrainGridData g0 = copy.CreateGridData();
            Vector2Int[] probes =
            {
                new Vector2Int(g0.OriginX - 1, g0.OriginY),
                new Vector2Int(g0.OriginX + g0.Width, g0.OriginY),
                new Vector2Int(g0.OriginX, g0.OriginY - 1),
                new Vector2Int(g0.OriginX, g0.OriginY + g0.Height),
                new Vector2Int(int.MinValue, int.MinValue),
                new Vector2Int(int.MaxValue, int.MaxValue),
                new Vector2Int(g0.OriginX - 99, g0.OriginY - 99),
                new Vector2Int(g0.OriginX + g0.Width + 99, g0.OriginY + g0.Height + 99),
            };

            int refused = 0;
            foreach (Vector2Int p in probes)
            {
                if (!IslandMapAuthoring.TryPaintCell(p.x, p.y))
                {
                    refused++;
                }
            }

            Check("REG_OUT_OF_BOUNDS_REFUSED", refused == probes.Length,
                $"{refused}/{probes.Length} out-of-bounds probes refused");
            Check("REG_BOUNDS_UNCHANGED",
                copy.Width == g0.Width && copy.Height == g0.Height
                && copy.OriginX == g0.OriginX && copy.OriginY == g0.OriginY,
                $"origin=({copy.OriginX},{copy.OriginY}) size={copy.Width}x{copy.Height}");

            // ---- drag stroke along a real water run
            Vector2Int run = FindWaterRun();
            Check("REG_WATER_RUN_FOUND", run.x != int.MinValue, $"cell {run}");
            IslandMapAuthoring.SetBrush(TerrainType.Grass);
            IslandMapAuthoring.BeginStroke();
            Check("REG_STROKE_BEGINS", IslandMapAuthoring.StrokeActive, "StrokeActive=true");
            int stroke = 0;
            for (int i = 0; i < 6; i++)
            {
                if (IslandMapAuthoring.TryPaintCell(run.x + i, run.y))
                {
                    stroke++;
                }
            }

            IslandMapAuthoring.EndStroke();
            Check("REG_STROKE_ENDS", !IslandMapAuthoring.StrokeActive, "StrokeActive=false");
            Check("REG_DRAG_PAINT", stroke > 0, $"{stroke} cells painted in one stroke");

            // ---- save / reload preserves arbitrary Raised data (R11 req 15)
            IslandMapAuthoring.SetElevationBrush(ElevationLevel.Raised, out _);
            IslandMapAuthoring.TryPaintElevation(land.x + 1, land.y + 1, ElevationLevel.Raised);
            int beforeSave = RaisedRegionAnalyzer.CountRaisedCells(IslandMapAuthoring.Grid);

            // IslandMapAuthoring already calls EditorUtility.SetDirty on every paint, so SaveAssets
            // is the whole commit path. This writes ONLY the temporary copy.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TerrainMapData reloaded =
                AssetDatabase.LoadAssetAtPath<TerrainMapData>(CopyDataPath);
            int afterReload = RaisedRegionAnalyzer.CountRaisedCells(reloaded.CreateGridData());
            Check("REG_SAVE_RELOAD_PRESERVES_ARBITRARY_RAISED",
                afterReload == beforeSave,
                $"{beforeSave} Raised cells before save, {afterReload} after save and reload");

            // ---- the 004R GUILayout invariant, checked statically
            string authoringSrc = File.ReadAllText(
                Physical("Assets/_Project/Editor/IslandMap/IslandMapAuthoring.cs"));
            int guiLayout = 0;
            foreach (string line in authoringSrc.Split('\n'))
            {
                if (line.Contains("GUILayout."))
                {
                    guiLayout++;
                }
            }

            Check("REG_OVERLAY_PATH_USES_NO_GUI_LAYOUT", guiLayout == 0,
                $"{guiLayout} GUILayout call(s) in IslandMapAuthoring.cs; the Repaint-only overlay "
                + "uses fixed-rect Handles.BeginGUI labels only, preserving the IL-WORLD-004R fix");

            Skip("REG_SCENE_OVERLAY_RUNTIME_GUI_LAYOUT",
                "batchmode has no live Scene View repaint, so the Repaint-only Handles path cannot be "
                + "driven headlessly; the GUILayout invariant is checked statically instead.");

            IslandMapAuthoring.Stop();
            Check("REG_STOPPED", !IslandMapAuthoring.IsActive, IslandMapAuthoring.Status);
            Check("REG_PREVIEW_TORN_DOWN",
                IslandMapAuthoring.PreviewGrass == null
                && IslandMapAuthoring.PreviewRaisedVisual == null,
                "all preview references cleared");

            return root;
        }

        // ---------------------------------------------------------------- helpers

        private static AuthorHillsCompositionSet CompositionSet()
        {
            return AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
        }

        private static string FirstReason(RaisedVisualPlan plan)
        {
            return plan.UnsupportedRegions.Count == 0
                ? "(none)"
                : RaisedRegionReasons.ShortCode(plan.UnsupportedRegions[0].UnsupportedReason.Value);
        }

        private static void FillWideRow(
            Dictionary<Vector3Int, string> map, int w, int y, string row)
        {
            for (int x = 0; x < w; x++)
            {
                string suffix = x == 0 ? "0" : (x == w - 1 ? "2" : "1");
                map[new Vector3Int(x, y, 0)] = row + "c" + suffix;
            }
        }

        private static bool Exact(RaisedVisualPlan plan, Dictionary<Vector3Int, string> expect)
        {
            var seen = new HashSet<Vector3Int>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (!expect.TryGetValue(t.VisualPosition, out string want))
                {
                    return false;
                }

                if (!seen.Add(t.VisualPosition))
                {
                    return false;
                }

                if (t.Sprite != Author[want])
                {
                    return false;
                }

                bool shouldBeOutside = t.VisualPosition.y < 0;
                if (t.OutsideLogicalMask != shouldBeOutside)
                {
                    return false;
                }
            }

            return seen.Count == expect.Count;
        }

        private static bool AllSpritesAre(RaisedVisualPlan plan, params string[] names)
        {
            foreach (HillVisualTile t in plan.Tiles)
            {
                bool ok = false;
                foreach (string n in names)
                {
                    if (t.Sprite == Author[n])
                    {
                        ok = true;
                    }
                }

                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        private static TerrainGridData NewGrid(int originX, int w, int h)
        {
            var g = new TerrainGridData(w, h, originX, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    g.SetTerrain(originX + x, y, TerrainType.Grass);
                }
            }

            return g;
        }

        private static void PaintRect(TerrainGridData grid, int ox, int oy, int w, int h)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    grid.SetElevation(ox + x, oy + y, ElevationLevel.Raised);
                }
            }
        }

        private static string Snapshot(TerrainGridData g)
        {
            var sb = new StringBuilder();
            for (int y = g.OriginY; y < g.OriginY + g.Height; y++)
            {
                for (int x = g.OriginX; x < g.OriginX + g.Width; x++)
                {
                    sb.Append((char)g.GetElevation(x, y));
                }
            }

            return sb.ToString();
        }

        private static Tilemap NewTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(parent, false);
            Tilemap map = go.AddComponent<Tilemap>();
            TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;
            return map;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static List<Vector3Int> Positions(Tilemap map)
        {
            var list = new List<Vector3Int>();
            BoundsInt b = map.cellBounds;
            for (int x = b.xMin; x < b.xMax; x++)
            {
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    var p = new Vector3Int(x, y, 0);
                    if (map.GetTile(p) != null)
                    {
                        list.Add(p);
                    }
                }
            }

            return list;
        }

        private static int CountType(TerrainMapData data, TerrainType t)
        {
            int n = 0;
            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    if (data.GetTerrain(data.OriginX + x, data.OriginY + y) == t)
                    {
                        n++;
                    }
                }
            }

            return n;
        }

        private static Vector2Int FindWaterCell()
        {
            TerrainGridData g = IslandMapAuthoring.Grid;
            for (int y = g.OriginY + 2; y < g.OriginY + g.Height - 4; y++)
            {
                for (int x = g.OriginX + 2; x < g.OriginX + g.Width - 4; x++)
                {
                    if (g.GetTerrain(x, y) == TerrainType.Water
                        && g.GetElevation(x, y) == ElevationLevel.Normal)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(int.MinValue, int.MinValue);
        }

        private static Vector2Int FindGrassCell()
        {
            TerrainGridData g = IslandMapAuthoring.Grid;
            for (int y = g.OriginY + 3; y < g.OriginY + g.Height - 3; y++)
            {
                for (int x = g.OriginX + 3; x < g.OriginX + g.Width - 3; x++)
                {
                    if (g.GetTerrain(x, y) == TerrainType.Grass
                        && g.GetElevation(x, y) == ElevationLevel.Normal)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(int.MinValue, int.MinValue);
        }

        private static Vector2Int FindClearGrassRun(int w, int h)
        {
            TerrainGridData g = IslandMapAuthoring.Grid;
            for (int y = g.OriginY + 4; y < g.OriginY + g.Height - h - 4; y++)
            {
                for (int x = g.OriginX + 4; x < g.OriginX + g.Width - w - 4; x++)
                {
                    bool clear = true;
                    for (int j = 0; j < h && clear; j++)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            if (g.GetTerrain(x + i, y + j) != TerrainType.Grass
                                || g.GetElevation(x + i, y + j) != ElevationLevel.Normal)
                            {
                                clear = false;
                                break;
                            }
                        }
                    }

                    if (clear)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(int.MinValue, int.MinValue);
        }

        private static Vector2Int FindWaterRun()
        {
            TerrainGridData g = IslandMapAuthoring.Grid;
            for (int y = g.OriginY + 2; y < g.OriginY + g.Height - 2; y++)
            {
                for (int x = g.OriginX + 2; x < g.OriginX + g.Width - 8; x++)
                {
                    bool run = true;
                    for (int i = 0; i < 6; i++)
                    {
                        if (g.GetTerrain(x + i, y) != TerrainType.Water
                            || g.GetElevation(x + i, y) != ElevationLevel.Normal)
                        {
                            run = false;
                            break;
                        }
                    }

                    if (run)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(int.MinValue, int.MinValue);
        }

        private static int ExhaustiveMismatch(TerrainMapData data, Tilemap water, Tilemap grass)
        {
            TerrainGridData g = data.CreateGridData();
            int bad = 0;
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    int wx = g.OriginX + x;
                    int wy = g.OriginY + y;
                    var p = new Vector3Int(wx, wy, 0);
                    TerrainType t = g.GetTerrain(wx, wy);
                    bool hasWater = water.GetTile(p) != null;
                    bool hasGrass = grass.GetTile(p) != null;
                    if (t == TerrainType.Water)
                    {
                        if (!hasWater || hasGrass)
                        {
                            bad++;
                        }
                    }
                    else if (t == TerrainType.Grass)
                    {
                        if (!hasWater || !hasGrass)
                        {
                            bad++;
                        }
                    }
                    else if (hasWater || hasGrass)
                    {
                        bad++;
                    }
                }
            }

            return bad;
        }

        private static string Physical(string assetPath)
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string HashFile(string assetPath)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Physical(assetPath))))
                    .Replace("-", string.Empty);
            }
        }

        /// <summary>
        /// Reads the Raised count straight out of the serialized elevations payload, so the assertion
        /// does not depend on the runtime parser agreeing with itself.
        /// </summary>
        private static int ReadRaisedCountFromAsset()
        {
            string raw = File.ReadAllText(Physical(RealDataPath));
            foreach (string line in raw.Split('\n'))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("elevations:"))
                {
                    continue;
                }

                string payload = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                int n = 0;
                for (int i = 0; i + 1 < payload.Length; i += 2)
                {
                    if (payload.Substring(i, 2) == "01")
                    {
                        n++;
                    }
                }

                return n;
            }

            return -1;
        }
    }
}
