// IL-WORLD-004S-PJ - P-junction projection verification.
//
// WHY THIS FILE EXISTS. PM locked the P fixture and accepted the measured root cause:
//
//   BEFORE [XXX / X..]   4 logical, 4 visual, 0 outside the mask   (visually correct)
//   AFTER  [XXX / XX.]   5 logical, 7 visual, 2 outside the mask   (the reported bug)
//
// and the two bad tiles were (4,3) = Hills_r2c0 emitted by (4,4) and (5,3) = Hills_r2c2 emitted by
// (5,4), i.e. an owner projecting an extra visual cell one row SOUTH of its own logical mask.
//
// The hard acceptance is therefore: after painting the fifth cell, logical == 5, NOTHING outside the
// logical mask, and (4,3) and (5,3) must not exist. Every visual tile must sit on its own logical cell
// and trace to a logical cell plus an author role plus an author Sprite. This harness asserts exactly
// that, plus that the length variants only change the repeating straight body and never change the
// junction identity, plus Paint / Erase / Undo / Redo against a fresh full rebuild.
//
// COORDINATE CONVENTION. Top-left visual grid. Fixture line 0 is the NORTH row and the last line is
// the SOUTH row, so fixture (4,4) is the south-west cell of every fixture here.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using IslandLife.World.Terrain;

namespace IslandLife.EditorTools.IslandMap
{
    public static class ILW004SPJunctionGrammar
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-PJ";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[PJG] " + s);
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

        // The three locked fixtures: minimal, and two length variants that must not move the junction.
        private static readonly string[] PMinBefore = { "XXX", "X.." };
        private static readonly string[] PMinAfter = { "XXX", "XX." };
        private static readonly string[] PVertical = { "XXXXXX", "XX...." };
        private static readonly string[] PHorizontal = { "XXXXXXXX", "XXX....." };

        [MenuItem("IslandLife/Diagnostics/PJ P-Junction Grammar")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-PJ P-junction projection ===");
            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                set == null ? "missing" : set.DescribeMissingSlots());
            Check("R2C4_R2C5_WIRED_INTO_THE_SET",
                set != null && set.GetR2C4() != null && set.GetR2C5() != null,
                set == null
                    ? "composition set missing"
                    : $"r2c4 = {set.GetR2C4()?.name}, r2c5 = {set.GetR2C5()?.name}; both slices are "
                        + "wired in and can no longer be silently missing");

            Line("");
            Line("-- the locked BEFORE / AFTER pair --");
            Fixture(set, "P_MIN_BEFORE", PMinBefore, 4, 4);
            Fixture(set, "P_MIN_AFTER", PMinAfter, 5, 4);

            Line("");
            Line("-- length variants: only the straight body may repeat, never the junction --");
            Fixture(set, "P_VERTICAL_EXTENDED", PVertical, 8, 4);
            Fixture(set, "P_HORIZONTAL_EXTENDED", PHorizontal, 11, 4);

            TenFold(set);
            LiveEditingEquivalence();

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "PJ_grammar.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- fixture

        private static void Fixture(
            AuthorHillsCompositionSet set, string id, string[] mask, int expectLogical, int y0)
        {
            TerrainGridData grid = Build(mask);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            var problems = new List<string>();

            int logical = RaisedRegionAnalyzer.CountRaisedCells(grid);
            if (logical != expectLogical)
            {
                problems.Add($"{logical} logical Raised cells, expected exactly {expectLogical}");
            }

            if (plan.TilesOutsideLogicalMask != 0)
            {
                problems.Add($"{plan.TilesOutsideLogicalMask} visual tile(s) OUTSIDE the logical "
                    + "mask, expected 0: an owner must never project one row south of itself");
            }

            // The two specific tiles PM named as the symptom.
            foreach (HillVisualTile t in plan.Tiles)
            {
                if ((t.VisualPosition.x == 4 && t.VisualPosition.y == 3)
                    || (t.VisualPosition.x == 5 && t.VisualPosition.y == 3))
                {
                    problems.Add($"({t.VisualPosition.x},{t.VisualPosition.y}) = {t.Sprite.name} still "
                        + "exists; this is one of the two tiles PM named as the bug");
                }
            }

            // Every visual tile on its own logical cell, each traceable to a role and a Sprite.
            if (plan.Tiles.Count != logical)
            {
                problems.Add($"{plan.Tiles.Count} visual tiles for {logical} logical cells; they must "
                    + "be one for one");
            }

            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.OutsideLogicalMask)
                {
                    problems.Add($"({t.VisualPosition.x},{t.VisualPosition.y}) flagged "
                        + "OutsideLogicalMask");
                }
            }

            if (plan.VisualizedRaisedCells != logical)
            {
                problems.Add($"{plan.VisualizedRaisedCells} of {logical} logical cells drawn");
            }

            if (plan.VisualDiagnostics.Count != 0)
            {
                problems.Add($"{plan.VisualDiagnostics.Count} renderer diagnostic(s)");
            }

            var seq = plan.Tiles
                .Select(t => $"({t.VisualPosition.x},{t.VisualPosition.y})="
                    + t.Sprite.name.Replace("Hills_", string.Empty)
                    + (t.OutsideLogicalMask ? "*" : string.Empty))
                .OrderBy(s => s, StringComparer.Ordinal);

            Line($"   {id} [{string.Join(" / ", mask)}]");
            Line($"      logical {logical}, visual {plan.Tiles.Count}, outside "
                + $"{plan.TilesOutsideLogicalMask}");
            foreach (HillVisualTile t in plan.Tiles
                .OrderBy(t => t.VisualPosition.y).ThenBy(t => t.VisualPosition.x))
            {
                RaisedTopologyState st =
                    RaisedTopologyState.Resolve(grid, t.VisualPosition.x, t.VisualPosition.y);
                Line($"      ({t.VisualPosition.x},{t.VisualPosition.y}) raw=0x{(byte)st.RawMask:X2} "
                    + $"canon={st.CanonicalMask,-22} role={st.Role,-18} slot={st.Slot,-14} "
                    + $"sprite={t.Sprite.name}"
                    + (t.OutsideLogicalMask ? " *OUTSIDE*" : string.Empty));
            }

            Check("FIXTURE_" + id, problems.Count == 0,
                problems.Count == 0
                    ? $"logical {logical}, visual {plan.Tiles.Count}, outside "
                        + $"{plan.TilesOutsideLogicalMask}; every visual tile sits on its own logical "
                        + $"cell and traces to an author role and Sprite"
                    : string.Join("; ", problems) + "  | actual [" + string.Join(" ", seq) + "]");
        }

        private static TerrainGridData Build(string[] mask)
        {
            int h = mask.Length;
            int w = mask[0].Length;
            var grid = new TerrainGridData(w + 10, h + 10, 4, 4);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetTerrain(4 + x, 4 + y, TerrainType.Grass);
                }
            }

            for (int i = 0; i < h; i++)
            {
                int y = 4 + (h - 1 - i);
                for (int x = 0; x < w; x++)
                {
                    if (mask[i][x] == 'X')
                    {
                        grid.SetElevation(4 + x, y, ElevationLevel.Raised);
                    }
                }
            }

            return grid;
        }

        // ---------------------------------------------------------------- ten fold

        private static void TenFold(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("=== 10x consistency ===");
            string[] masks = { string.Join("|", PMinAfter), string.Join("|", PVertical),
                string.Join("|", PHorizontal) };
            string reference = null;
            int agree = 0;
            int outsideZero = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var keys = new List<string>();
                bool zero = true;
                foreach (string m in masks)
                {
                    RaisedVisualPlan p = RaisedVisualPlan.Build(Build(m.Split('|')), set);
                    keys.Add(string.Join(",", p.Tiles
                        .Select(t => $"{t.VisualPosition.x},{t.VisualPosition.y}="
                            + t.Sprite.name)
                        .OrderBy(s => s, StringComparer.Ordinal)));
                    if (p.TilesOutsideLogicalMask != 0)
                    {
                        zero = false;
                    }
                }

                string joined = string.Join(";", keys);
                if (reference == null)
                {
                    reference = joined;
                }
                else if (joined == reference)
                {
                    agree++;
                }

                if (zero)
                {
                    outsideZero++;
                }
            }

            Check("TENFOLD_P_STABLE", agree == 9,
                $"{agree}/9 further repetitions produced the identical sprite sequence at all three P "
                    + "fixtures");

            Check("TENFOLD_P_OUTSIDE_MASK_ZERO", outsideZero == 10,
                $"{outsideZero}/10 repetitions had ZERO visual tiles outside the logical mask on all "
                    + "three P fixtures");
        }

        // ---------------------------------------------------------------- live editing

        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string CopyDataPath =
            "Assets/_Project/World/Terrain/__PJ_TempCopy.asset";
        private const string RenderAssetsPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";

        private static Vector2Int s_origin;
        private static int s_baseLogical;

        private static void LiveEditingEquivalence()
        {
            Line("");
            Line("=== live editing: L -> paint the fifth cell -> P -> erase -> L ===");
            if (!AssetDatabase.CopyAsset(RealDataPath, CopyDataPath))
            {
                Check("LIVE_SESSION_COPY", false, "could not copy the asset");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var root = new GameObject("PJRig");
            UnityEngine.Grid g = root.AddComponent<UnityEngine.Grid>();
            g.cellSize = new Vector3(1f, 1f, 0f);
            UnityEngine.Tilemaps.Tilemap water = NewMap(root.transform, "Water", 0);
            UnityEngine.Tilemaps.Tilemap grass = NewMap(root.transform, "Grass", 1);
            var loaderGo = new GameObject("TerrainRuntime");
            loaderGo.transform.SetParent(root.transform, false);
            TerrainMapRuntimeLoader loader = loaderGo.AddComponent<TerrainMapRuntimeLoader>();
            SetField(loader, "terrainMapData",
                AssetDatabase.LoadAssetAtPath<TerrainMapData>(CopyDataPath));
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

            Check("LIVE_SESSION_STARTED", true, "temporary COPY; the user's asset is only read");
            s_origin = FindClear(14, 8);
            Check("LIVE_CLEAR_GROUND", s_origin.x != int.MinValue,
                $"clear 14x8 block at {s_origin}");

            try
            {
                // Build the L of the locked fixture in place: a three wide top row and one cell below
                // its west end, then paint the fifth cell.
                Reset();
                Paint(At(0, 2));
                Paint(At(1, 2));
                Paint(At(2, 2));
                Paint(At(0, 1));
                AssertNoOutside("L", 4);
                List<string> lSet = new List<string>();
                foreach (KeyValuePair<Vector2Int, string> kv in LiveTiles())
                {
                    lSet.Add($"{kv.Key.x},{kv.Key.y}={kv.Value}");
                }

                lSet.Sort(StringComparer.Ordinal);
                AssertNoOutside("L", 4);

                Paint(At(1, 1));
                AssertNoOutside("P_PAINTED", 5);
                AssertNoOutside("P_PAINTED", 5);
                AssertBadTileGone("P_PAINTED", At(0, 0));
                AssertBadTileGone("P_PAINTED", At(1, 0));

                Erase(At(1, 1));
                AssertNoOutside("P_ERASED", 4);
                AssertNoOutside("P_ERASED", 4);
                List<string> back = new List<string>();
                foreach (KeyValuePair<Vector2Int, string> kv in LiveTiles())
                {
                    back.Add($"{kv.Key.x},{kv.Key.y}={kv.Value}");
                }

                back.Sort(StringComparer.Ordinal);
                Check("ERASE_RESTORES_L_EXACTLY", joined(back) == joined(lSet),
                    joined(back) == joined(lSet)
                        ? $"after Erase the live preview is identical to the pre-Paint L by sprite "
                            + $"identity and visual coordinate ({lSet.Count} tiles)"
                        : $"before [{joined(lSet)}] after [{joined(back)}]");

                // Undo/Redo on a FRESH walk. After the Erase above the top of the undo stack is
                // the Erase itself, so a PerformRedo there would re-apply the Erase instead of
                // restoring the P. Rebuild the L, then test Undo and Redo around the Paint only.
                Reset();
                Paint(At(0, 2));
                Paint(At(1, 2));
                Paint(At(2, 2));
                Paint(At(0, 1));
                AssertNoOutside("UNDO_BASE_L", 4);
                List<string> undoBase = new List<string>();
                foreach (KeyValuePair<Vector2Int, string> kv in LiveTiles())
                {
                    undoBase.Add($"{kv.Key.x},{kv.Key.y}={kv.Value}");
                }

                undoBase.Sort(StringComparer.Ordinal);

                Paint(At(1, 1));
                AssertNoOutside("UNDO_BASE_P", 5);
                AssertBadTileGone("UNDO_BASE_P", At(0, 0));
                AssertBadTileGone("UNDO_BASE_P", At(1, 0));
                List<string> pFirst = new List<string>();
                foreach (KeyValuePair<Vector2Int, string> kv in LiveTiles())
                {
                    pFirst.Add($"{kv.Key.x},{kv.Key.y}={kv.Value}");
                }

                pFirst.Sort(StringComparer.Ordinal);

                UnityEditor.Undo.PerformUndo();
                AssertNoOutside("UNDO_BACK_TO_L", 4);
                List<string> undone = new List<string>();
                foreach (KeyValuePair<Vector2Int, string> kv in LiveTiles())
                {
                    undone.Add($"{kv.Key.x},{kv.Key.y}={kv.Value}");
                }

                undone.Sort(StringComparer.Ordinal);
                Check("UNDO_RESTORES_L_EXACTLY", joined(undone) == joined(undoBase),
                    joined(undone) == joined(undoBase)
                        ? "Undo of the fifth cell returned exactly the original L, tile for tile"
                        : $"original [{joined(undoBase)}] after Undo [{joined(undone)}]");

                UnityEditor.Undo.PerformRedo();
                AssertNoOutside("UNDO_REDO_P", 5);
                List<string> redone = new List<string>();
                foreach (KeyValuePair<Vector2Int, string> kv in LiveTiles())
                {
                    redone.Add($"{kv.Key.x},{kv.Key.y}={kv.Value}");
                }

                redone.Sort(StringComparer.Ordinal);
                Check("REDO_REPRODUCES_P_EXACTLY", joined(redone) == joined(pFirst),
                    joined(redone) == joined(pFirst)
                        ? "Redo reproduced the first P exactly, tile for tile"
                        : $"first P [{joined(pFirst)}] after Redo [{joined(redone)}]");

            }
            finally
            {
                IslandMapAuthoring.Stop();
                AssetDatabase.DeleteAsset(CopyDataPath);
                AssetDatabase.Refresh();
            }
        }

        private static string joined(List<string> l)
        {
            return string.Join(" ", l);
        }

        private static Dictionary<Vector2Int, string> LiveTiles()
        {
            var d = new Dictionary<Vector2Int, string>();
            UnityEngine.Tilemaps.Tilemap map = IslandMapAuthoring.PreviewRaisedVisual;
            if (map == null)
            {
                return d;
            }

            map.RefreshAllTiles();
            map.CompressBounds();
            UnityEngine.BoundsInt b = map.cellBounds;
            for (int x = b.xMin; x < b.xMax; x++)
            {
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    var t = map.GetTile(new Vector3Int(x, y, 0)) as UnityEngine.Tilemaps.Tile;
                    if (t != null && t.sprite != null)
                    {
                        d[new Vector2Int(x, y)] = t.sprite.name;
                    }
                }
            }

            return d;
        }

        /// <summary>Asserts no live preview tile sits in the fixture footprint's footprint ring.</summary>
        private static void AssertNoOutside(string id, int expectDelta)
        {
            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            List<string> live = Fingerprint(IslandMapAuthoring.PreviewRaisedVisual);
            List<string> fresh = Fingerprint(FreshPlan(CurrentMaskOnly(), set));
            bool same = live.Count == fresh.Count
                && !live.Except(fresh).Any()
                && !fresh.Except(live).Any();
            int delta = RaisedCountOfLive() - s_baseLogical;
            Check("FRESH_" + id, same && delta == expectDelta,
                same && delta == expectDelta
                    ? $"{live.Count} live preview tiles identical to a fresh full rebuild by sprite "
                        + $"identity and visual coordinate; fixture delta {delta}"
                    : $"live {live.Count} / fresh {fresh.Count}, delta {delta} expected "
                        + $"{expectDelta}");
        }

        private static void AssertBadTileGone(string id, Vector2Int southOf)
        {
            UnityEngine.Tilemaps.Tilemap map = IslandMapAuthoring.PreviewRaisedVisual;
            string got = null;
            if (map != null)
            {
                map.RefreshAllTiles();
                var t = map.GetTile(new Vector3Int(southOf.x, southOf.y, 0))
                    as UnityEngine.Tilemaps.Tile;
                if (t != null && t.sprite != null)
                {
                    got = t.sprite.name;
                }
            }

            Check("NO_DISPLACED_" + id + "_" + southOf.x + "_" + southOf.y, got == null,
                got == null
                    ? $"no tile at ({southOf.x},{southOf.y}), the row south of the fixture footprint"
                    : $"({southOf.x},{southOf.y}) still carries {got}");
        }

        private static Vector2Int At(int dx, int dy)
        {
            return new Vector2Int(s_origin.x + dx, s_origin.y + dy);
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

        private static List<string> Fingerprint(UnityEngine.Tilemaps.Tilemap map)
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
                    var t = map.GetTile(new Vector3Int(x, y, 0)) as UnityEngine.Tilemaps.Tile;
                    if (t != null && t.sprite != null)
                    {
                        list.Add($"{x},{y}={t.sprite.name}");
                    }
                }
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static List<string> Fingerprint(IReadOnlyList<HillVisualTile> tiles)
        {
            var list = new List<string>();
            foreach (HillVisualTile t in tiles)
            {
                if (t.Sprite != null)
                {
                    list.Add($"{t.VisualPosition.x},{t.VisualPosition.y}={t.Sprite.name}");
                }
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static void Reset()
        {
            IslandMapAuthoring.SetElevationBrush(ElevationLevel.Raised, out _);
            IslandMapAuthoring.SetElevationErase(true);
            IslandMapAuthoring.BeginStroke();
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 14; x++)
                {
                    IslandMapAuthoring.TryPaintCell(s_origin.x + x, s_origin.y + y);
                }
            }

            IslandMapAuthoring.EndStroke();
            IslandMapAuthoring.SetElevationErase(false);
            s_baseLogical = RaisedCountOfLive();
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
            UnityEngine.Tilemaps.TilemapRenderer tr = go.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
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
    }
}
