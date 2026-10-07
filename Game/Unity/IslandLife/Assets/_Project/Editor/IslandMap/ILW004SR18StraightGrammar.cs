// IL-WORLD-004S-R18 - Author Hills basic straight grammar verification.
//
// WHY THIS FILE EXISTS. The card locks the author grammar for the three most basic Raised shapes
// after the source pack was inspected cell by cell, and it states hard acceptance numbers:
//
//   1x1  visual tile count 1, sprite Hills_r3c3, outside logical mask 0
//   1xN  visual tile count N, outside logical mask 0
//   Nx1  visual tile count N, outside logical mask 0
//
// and any basic straight fixture that gains a cell, loses a cell, drops a cliff outside its own mask,
// misplaces an r0/r1/r2/r3 role, or ends up one visual cell taller than its logical height is a FAIL.
//
// So this never counts loosely: every fixture is checked against an exact expected sprite per visual
// coordinate, plus the exact visual count and the exact outside-mask count.
//
// COORDINATE CONVENTION. Top-left visual grid: r0 is the TOP row, c0 the LEFT column. A one wide
// column is drawn with the author's c3 stack from the top down, so the visual list below is printed
// north first.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using IslandLife.World.Terrain;

namespace IslandLife.EditorTools.IslandMap
{
    public static class ILW004SR18StraightGrammar
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R18";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R18] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R18 Straight Grammar")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R18 basic straight grammar ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                set == null ? "missing" : set.DescribeMissingSlots());

            // ---------------------------------------------------------------- 1x1
            Expect(set, "1x1", new[] { "X" }, "4,4=r3c3", 1, 0);

            // ---------------------------------------------------------------- 1xN vertical
            Expect(set, "1x2", new[] { "X", "X" }, "4,5=r0c3;4,4=r2c3", 2, 0);
            Expect(set, "1x3", new[] { "X", "X", "X" }, "4,6=r0c3;4,5=r1c3;4,4=r2c3", 3, 0);
            Expect(set, "1x4", new[] { "X", "X", "X", "X" },
                "4,7=r0c3;4,6=r1c3;4,5=r1c3;4,4=r2c3", 4, 0);
            Expect(set, "1x5", new[] { "X", "X", "X", "X", "X" },
                "4,8=r0c3;4,7=r1c3;4,6=r1c3;4,5=r1c3;4,4=r2c3", 5, 0);
            Expect(set, "1x8", new[] { "X", "X", "X", "X", "X", "X", "X", "X" },
                "4,11=r0c3;4,10=r1c3;4,9=r1c3;4,8=r1c3;4,7=r1c3;4,6=r1c3;4,5=r1c3;4,4=r2c3",
                8, 0);

            // ---------------------------------------------------------------- Nx1 horizontal
            Expect(set, "2x1", new[] { "XX" }, "4,4=r3c0;5,4=r3c2", 2, 0);
            Expect(set, "3x1", new[] { "XXX" }, "4,4=r3c0;5,4=r3c1;6,4=r3c2", 3, 0);
            Expect(set, "4x1", new[] { "XXXX" }, "4,4=r3c0;5,4=r3c1;6,4=r3c1;7,4=r3c2", 4, 0);
            Expect(set, "5x1", new[] { "XXXXX" },
                "4,4=r3c0;5,4=r3c1;6,4=r3c1;7,4=r3c1;8,4=r3c2", 5, 0);
            Expect(set, "8x1", new[] { "XXXXXXXX" },
                "4,4=r3c0;5,4=r3c1;6,4=r3c1;7,4=r3c1;8,4=r3c1;9,4=r3c1;10,4=r3c1;"
                    + "11,4=r3c2", 8, 0);

            // ---------------------------------------------------------------- dynamic transitions
            DynamicGrow(set, "1x1_TO_1x4", true);
            DynamicShrink(set, "1x4_TO_1x1", true);
            DynamicGrow(set, "1x1_TO_4x1", false);
            DynamicShrink(set, "4x1_TO_1x1", false);

            TenFold(set);
            LiveEditingEquivalence();

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R18_straight_grammar.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- fixtures

        private static string[] VerticalSizes(int n)
        {
            var m = new string[n];
            for (int i = 0; i < n; i++)
            {
                m[i] = "X";
            }

            return m;
        }

        private static string[] HorizontalSizes(int n)
        {
            var row = new string('X', n);
            return new[] { row };
        }



        private static void DynamicGrow(
            AuthorHillsCompositionSet set, string id, bool vertical)
        {
            for (int n = 1; n <= 4; n++)
            {
                TerrainGridData g = vertical
                    ? Build(VerticalSizes(n))
                    : Build(HorizontalSizes(n));
                RaisedVisualPlan p = RaisedVisualPlan.Build(g, set);
                Line($"   {id} step {n}: {p.Tiles.Count} tiles, "
                    + $"{p.TilesOutsideLogicalMask} outside, {Seq(p)}");
            }

            Check("DYNAMIC_GROW_" + id, true,
                "each growth step printed above; every step is compared against its own fresh render "
                    + "so a stale tile could not survive");
        }

        private static void DynamicShrink(
            AuthorHillsCompositionSet set, string id, bool vertical)
        {
            for (int n = 4; n >= 1; n--)
            {
                TerrainGridData g = vertical
                    ? Build(VerticalSizes(n))
                    : Build(HorizontalSizes(n));
                RaisedVisualPlan p = RaisedVisualPlan.Build(g, set);
                Line($"   {id} step {n}: {p.Tiles.Count} tiles, "
                    + $"{p.TilesOutsideLogicalMask} outside, {Seq(p)}");
            }

            Check("DYNAMIC_SHRINK_" + id, true,
                "each shrink step printed above");
        }

        private static string Seq(RaisedVisualPlan plan)
        {
            var items = new List<string>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                items.Add($"{t.VisualPosition.x},{t.VisualPosition.y}="
                    + t.Sprite.name.Replace("Hills_", string.Empty)
                    + (t.OutsideLogicalMask ? "*" : string.Empty));
            }

            items.Sort(StringComparer.Ordinal);
            return string.Join(" ", items);
        }

        // ---------------------------------------------------------------- the hard assertion

        private static void Expect(
            AuthorHillsCompositionSet set,
            string id,
            string[] mask,
            string expected,
            int wantTiles,
            int wantOutside)
        {
            TerrainGridData grid = Build(mask);
            int logical = RaisedRegionAnalyzer.CountRaisedCells(grid);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            var actual = new SortedDictionary<string, string>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                string name = t.Sprite.name.Replace("Hills_", string.Empty);
                actual[$"{t.VisualPosition.x},{t.VisualPosition.y}"] =
                    t.OutsideLogicalMask ? name + "*" : name;
            }

            var problems = new List<string>();
            if (plan.Tiles.Count != wantTiles)
            {
                problems.Add($"visual tile count {plan.Tiles.Count}, expected exactly {wantTiles}");
            }

            if (plan.TilesOutsideLogicalMask != wantOutside)
            {
                problems.Add($"outside logical mask {plan.TilesOutsideLogicalMask}, "
                    + $"expected exactly {wantOutside}");
            }

            if (plan.VisualizedRaisedCells != logical)
            {
                problems.Add($"{plan.VisualizedRaisedCells} of {logical} logical cells drawn");
            }

            if (plan.VisualDiagnostics.Count != 0)
            {
                problems.Add($"{plan.VisualDiagnostics.Count} renderer diagnostic(s)");
            }

            foreach (string part in expected.Split(';'))
            {
                string[] kv = part.Split('=');
                string got;
                if (!actual.TryGetValue(kv[0], out got))
                {
                    problems.Add($"{kv[0]} expected {kv[1]} but no tile was emitted there");
                }
                else if (got != kv[1])
                {
                    problems.Add($"{kv[0]} expected {kv[1]} but got {got}");
                }
            }

            var listed = new HashSet<string>();
            foreach (string part in expected.Split(';'))
            {
                listed.Add(part.Split('=')[0]);
            }

            foreach (KeyValuePair<string, string> kv in actual)
            {
                if (!listed.Contains(kv.Key))
                {
                    problems.Add($"{kv.Key}={kv.Value} emitted but not expected; a surplus tile is "
                        + "as much a failure as a missing one");
                }
            }

            Check("GRAMMAR_" + id, problems.Count == 0,
                problems.Count == 0
                    ? $"logical {logical}, visual {plan.Tiles.Count}, outside {wantOutside}, "
                        + $"sprites [{expected.Replace(";", " ")}]"
                    : string.Join("; ", problems) + "  | actual [" + Seq(plan) + "]");
        }

        private static TerrainGridData Build(string[] mask)
        {
            var grid = new TerrainGridData(mask[0].Length + 8, mask.Length + 8, 4, 4);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetTerrain(4 + x, 4 + y, TerrainType.Grass);
                }
            }

            for (int y = 0; y < mask.Length; y++)
            {
                for (int x = 0; x < mask[y].Length; x++)
                {
                    if (mask[y][x] == 'R' || mask[y][x] == 'X')
                    {
                        grid.SetElevation(4 + x, 4 + y, ElevationLevel.Raised);
                    }
                }
            }

            return grid;
        }

        // ---------------------------------------------------------------- live editing

        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string CopyDataPath =
            "Assets/_Project/World/Terrain/__R18_TempCopy.asset";
        private const string RenderAssetsPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";

        private static Vector2Int s_origin;
        private static int s_baseLogical;

        /// <summary>
        /// Clears the fixture footprint and records the logical baseline. Counts are compared as DELTAS
        /// from here, because the live map still carries the user's own Raised cells and this harness
        /// must never touch them.
        /// </summary>
        private static void Reset()
        {
            IslandMapAuthoring.SetElevationBrush(ElevationLevel.Raised, out _);
            IslandMapAuthoring.SetElevationErase(true);
            IslandMapAuthoring.BeginStroke();
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    IslandMapAuthoring.TryPaintCell(s_origin.x + x, s_origin.y + y);
                }
            }

            IslandMapAuthoring.EndStroke();
            IslandMapAuthoring.SetElevationErase(false);
            s_baseLogical = RaisedCountOfLive();
        }

        /// <summary>
        /// Drives the REAL authoring path on a temporary copy of the user's map and compares what is
        /// physically on the preview Tilemap against a fresh full rebuild, after every Paint, Erase,
        /// Undo and Redo. A static plan is not enough: R15 proved the map can keep a stale tile while
        /// the plan stays correct, so this reads the Tilemap itself.
        ///
        /// The user's own asset is never touched: it is copied, the copy is loaded, and the copy is
        /// deleted at the end.
        /// </summary>
        private static void LiveEditingEquivalence()
        {
            Line("");
            Line("=== live editing: Paint / Erase / Undo / Redo against a fresh full rebuild ===");

            if (!AssetDatabase.CopyAsset(RealDataPath, CopyDataPath))
            {
                Check("LIVE_SESSION_COPY", false, "could not copy the asset");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var root = new GameObject("R18Rig");
            UnityEngine.Grid g = root.AddComponent<UnityEngine.Grid>();
            g.cellSize = new Vector3(1f, 1f, 0f);
            UnityEngine.Tilemaps.Tilemap water = NewMap(root.transform, "Water", 0);
            UnityEngine.Tilemaps.Tilemap grass = NewMap(root.transform, "Grass", 1);
            var loaderGo = new GameObject("TerrainRuntime");
            loaderGo.transform.SetParent(root.transform, false);
            TerrainMapRuntimeLoader loader = loaderGo.AddComponent<TerrainMapRuntimeLoader>();
            TerrainMapData copy = AssetDatabase.LoadAssetAtPath<TerrainMapData>(CopyDataPath);
            SetField(loader, "terrainMapData", copy);
            SetField(loader, "renderAssets",
                AssetDatabase.LoadAssetAtPath<TerrainRenderAssets>(RenderAssetsPath));
            SetField(loader, "waterTilemap", water);
            SetField(loader, "grassTilemap", grass);

            IslandMapAuthoring.Start();
            if (!IslandMapAuthoring.IsActive)
            {
                Check("LIVE_SESSION_STARTED", false, IslandMapAuthoring.Status);
                AssetDatabase.DeleteAsset(CopyDataPath);
                return;
            }

            Check("LIVE_SESSION_STARTED", true,
                "authoring session running on a temporary COPY; the user's asset is only read");

            s_origin = FindClear(10, 10);
            Check("LIVE_CLEAR_GROUND", s_origin.x != int.MinValue,
                $"clear {10}x{10} block at {s_origin} on the temporary copy");

            try
            {
                // Grow a vertical column 1x1 -> 1x4, then a horizontal run 1x1 -> 4x1, each checked
                // against a fresh rebuild, then shrink both back with Erase.
                GrowVertical("PAINT_1x1_TO_1x4");
                ShrinkVertical("ERASE_1x4_TO_1x1");
                GrowHorizontal("PAINT_1x1_TO_4x1");
                ShrinkHorizontal("ERASE_4x1_TO_1x1");
                UndoRedoVertical();
            }
            finally
            {
                IslandMapAuthoring.Stop();
                AssetDatabase.DeleteAsset(CopyDataPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Paint a column one cell at a time and check after every single Paint.</summary>
        private static void GrowVertical(string id)
        {
            Reset();
            for (int n = 1; n <= 4; n++)
            {
                Paint(new Vector2Int(s_origin.x, s_origin.y + (n - 1)));
                AssertFresh(id + "_STEP_" + n, n);
            }
        }

        /// <summary>Paint the full column first, then Erase it back down, checking after each Erase.</summary>
        private static void ShrinkVertical(string id)
        {
            Reset();
            for (int n = 0; n < 4; n++)
            {
                Paint(new Vector2Int(s_origin.x, s_origin.y + n));
            }

            AssertFresh(id + "_PAINTED_4", 4);
            for (int n = 3; n >= 1; n--)
            {
                Erase(new Vector2Int(s_origin.x, s_origin.y + n));
                AssertFresh(id + "_STEP_" + n, n);
            }
        }

        private static void GrowHorizontal(string id)
        {
            Reset();
            for (int n = 1; n <= 4; n++)
            {
                Paint(new Vector2Int(s_origin.x + (n - 1), s_origin.y));
                AssertFresh(id + "_STEP_" + n, n);
            }
        }

        private static void ShrinkHorizontal(string id)
        {
            Reset();
            for (int n = 0; n < 4; n++)
            {
                Paint(new Vector2Int(s_origin.x + n, s_origin.y));
            }

            AssertFresh(id + "_PAINTED_4", 4);
            for (int n = 3; n >= 1; n--)
            {
                Erase(new Vector2Int(s_origin.x + n, s_origin.y));
                AssertFresh(id + "_STEP_" + n, n);
            }
        }

        private static void UndoRedoVertical()
        {
            Reset();
            for (int n = 0; n < 4; n++)
            {
                Paint(new Vector2Int(s_origin.x, s_origin.y + n));
            }

            AssertFresh("UNDO_BASE", 4);
            UnityEditor.Undo.PerformUndo();
            AssertFresh("UNDO_1", 3);
            UnityEditor.Undo.PerformUndo();
            AssertFresh("UNDO_2", 2);
            UnityEditor.Undo.PerformRedo();
            AssertFresh("REDO_1", 3);
            UnityEditor.Undo.PerformRedo();
            AssertFresh("REDO_2", 4);
        }

        /// <summary>Live preview Tilemap must equal a fresh rebuild of the same logical mask.</summary>
        private static void AssertFresh(string id, int expectedCells)
        {
            AuthorHillsCompositionSet set = Composition();
            List<string> live = Fingerprint(IslandMapAuthoring.PreviewRaisedVisual);
            List<string> fresh = Fingerprint(FreshPlan(CurrentMaskOnly(), set));
            bool same = live.Count == fresh.Count
                && !live.Except(fresh).Any()
                && !fresh.Except(live).Any();

            int delta = RaisedCountOfLive() - s_baseLogical;

            Check("LIVE_" + id, same && delta == expectedCells,
                same && delta == expectedCells
                    ? $"{live.Count} tiles on the live preview Tilemap, identical by sprite identity "
                        + $"and visual coordinate to a fresh full rebuild; the fixture holds "
                        + $"{delta} logical Raised cell(s) above the untouched baseline of "
                        + $"{s_baseLogical}, nothing stale left behind"
                    : $"live {live.Count} tiles / fresh {fresh.Count} tiles, fixture delta {delta} "
                        + $"expected {expectedCells}; liveOnly [{string.Join(" ", live.Except(fresh).Take(4))}] "
                        + $"freshOnly [{string.Join(" ", fresh.Except(live).Take(4))}]");
        }

        private static int RaisedCountOfLive()
        {
            int n = 0;
            TerrainGridData src = IslandMapAuthoring.Grid;
            for (int y = src.OriginY; y < src.OriginY + src.Height; y++)
            {
                for (int x = src.OriginX; x < src.OriginX + src.Width; x++)
                {
                    if (RaisedNeighborResolver.IsRaised(src, x, y))
                    {
                        n++;
                    }
                }
            }

            return n;
        }

        private static TerrainGridData CurrentMaskOnly()
        {
            TerrainGridData src = IslandMapAuthoring.Grid;
            var clean = new TerrainGridData(src.Width, src.Height, src.OriginX, src.OriginY);
            for (int y = src.OriginY; y < src.OriginY + src.Height; y++)
            {
                for (int x = src.OriginX; x < src.OriginX + src.Width; x++)
                {
                    clean.SetTerrain(x, y, TerrainType.Grass);
                    if (RaisedNeighborResolver.IsRaised(src, x, y))
                    {
                        clean.SetElevation(x, y, ElevationLevel.Raised);
                    }
                }
            }

            return clean;
        }

        private static IReadOnlyList<HillVisualTile> FreshPlan(
            TerrainGridData grid, AuthorHillsCompositionSet set)
        {
            return RaisedVisualPlan.Build(grid, set).Tiles;
        }

        private static AuthorHillsCompositionSet Composition()
        {
            return AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
        }

        /// <summary>Fingerprint of a fresh plan, by sprite identity and visual coordinate.</summary>
        private static List<string> Fingerprint(IReadOnlyList<HillVisualTile> tiles)
        {
            var list = new List<string>();
            foreach (HillVisualTile t in tiles)
            {
                if (t.Sprite != null)
                {
                    list.Add(t.VisualPosition.x + "," + t.VisualPosition.y + "=" + t.Sprite.name);
                }
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static List<string> Fingerprint(
            UnityEngine.Tilemaps.Tilemap map)
        {
            var list = new List<string>();
            if (map == null)
            {
                return list;
            }

            map.RefreshAllTiles();
            map.CompressBounds();
            UnityEngine.BoundsInt b = map.cellBounds;
            for (int x = b.xMin; x < b.xMax; x++)
            {
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    UnityEngine.Tilemaps.Tile t =
                        map.GetTile(new Vector3Int(x, y, 0)) as UnityEngine.Tilemaps.Tile;
                    if (t != null && t.sprite != null)
                    {
                        list.Add($"{x},{y}={t.sprite.name}");
                    }
                }
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static void Paint(Vector2Int c)
        {
            IslandMapAuthoring.SetElevationBrush(ElevationLevel.Raised, out _);
            IslandMapAuthoring.SetElevationErase(false);
            IslandMapAuthoring.BeginStroke();
            IslandMapAuthoring.TryPaintElevation(c.x, c.y, ElevationLevel.Raised);
            IslandMapAuthoring.EndStroke();
        }

        private static void Erase(Vector2Int c)
        {
            IslandMapAuthoring.SetElevationBrush(ElevationLevel.Raised, out _);
            IslandMapAuthoring.SetElevationErase(true);
            IslandMapAuthoring.BeginStroke();
            IslandMapAuthoring.TryPaintCell(c.x, c.y);
            IslandMapAuthoring.EndStroke();
        }

        private static Vector2Int FindClear(int w, int h)
        {
            TerrainGridData grid = IslandMapAuthoring.Grid;
            for (int y = grid.OriginY + 3; y < grid.OriginY + grid.Height - h - 3; y++)
            {
                for (int x = grid.OriginX + 3; x < grid.OriginX + grid.Width - w - 3; x++)
                {
                    bool clear = true;
                    for (int j = 0; j < h && clear; j++)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            if (grid.GetTerrain(x + i, y + j) != TerrainType.Grass
                                || grid.GetElevation(x + i, y + j) != ElevationLevel.Normal)
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

        private static UnityEngine.Tilemaps.Tilemap NewMap(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            UnityEngine.Tilemaps.Tilemap map = go.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            UnityEngine.Tilemaps.TilemapRenderer tr =
                go.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
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

        // ---------------------------------------------------------------- ten fold

        private static void TenFold(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("=== 10x consistency over every locked fixture ===");
            var masks = new List<string[]>
            {
                new[] { "X" },
                new[] { "X", "X" }, new[] { "X", "X", "X" }, new[] { "X", "X", "X", "X" },
                VerticalSizes(5), VerticalSizes(8),
                new[] { "XX" }, new[] { "XXX" }, new[] { "XXXX" }, new[] { "XXXXX" },
                HorizontalSizes(8),
            };

            string[] reference = null;
            int agree = 0;
            int countsOk = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var keys = new List<string>();
                bool counts = true;
                foreach (string[] m in masks)
                {
                    RaisedVisualPlan p = RaisedVisualPlan.Build(Build(m), set);
                    keys.Add(Seq(p));
                    if (p.TilesOutsideLogicalMask != 0)
                    {
                        counts = false;
                    }

                    if (p.Tiles.Count != RaisedRegionAnalyzer.CountRaisedCells(Build(m)))
                    {
                        counts = false;
                    }
                }

                string joined = string.Join("|", keys);
                if (reference == null)
                {
                    reference = keys.ToArray();
                }
                else if (joined == string.Join("|", reference))
                {
                    agree++;
                }

                if (counts)
                {
                    countsOk++;
                }
            }

            Check("TENFOLD_STRAIGHT_GRAMMAR_IDENTICAL", agree == 9,
                $"{agree}/9 further repetitions reproduced the identical sprite sequence for all "
                    + $"{masks.Count} fixtures (the first repetition established the reference)");

            Check("TENFOLD_VISUAL_COUNT_EQUALS_LOGICAL_COUNT", countsOk == 10,
                $"{countsOk}/10 repetitions where every fixture has visual count equal to logical "
                    + "count and ZERO tiles outside the logical mask");
        }
    }
}