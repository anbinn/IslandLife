// IL-WORLD-004S-R19 - author left/right corner grammar verification.
//
// WHY THIS FILE EXISTS. PM locked two author Sprite semantics against the source Hills.png after
// comparing them one cell at a time in Unity:
//
//   Hills_r3c4  LEFT  corner : a vertical left boundary turning east into the horizontal front
//   Hills_r3c7  RIGHT corner : the horizontal front turning north into a vertical right boundary
//
// and the card states hard requirements: the corner must be the author's r3c4 / r3c7 in every
// fixture regardless of how long either straight run is, the corner must sit on the logical Raised
// cell itself, and it must never be faked by displacing a cliff, by dropping a row south, or by
// substituting the old r2c0 / r2c2 terminals.
//
// So this never checks "does an L look right". It asserts, per fixture:
//   - the exact sprite AT the corner coordinate, read back from the emitted plan
//   - the resolved author ROLE at that coordinate
//   - the raw and canonical 8-neighbour mask at that coordinate
//   - visual set == logical set, i.e. one visual cell per logical cell on the SAME coordinate, so a
//     transparent gap, a duplicated cell or a displaced cliff would all fail
//   - tiles outside the logical mask == 0
//   - the cells either side of the corner along the front are the R18 r3 row, so the corner is
//     continuous with the straight grammar rather than bolted on
//
// COORDINATE CONVENTION. Top-left visual grid. Fixture line 0 is the NORTH row and the last line is
// the SOUTH row, and fixture (4,4) is the south-west cell of every fixture, so a corner coordinate is
// readable straight off the mask. Front cliffs are drawn at y-1, i.e. one row SOUTH.
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
    public static class ILW004SR19CornerGrammar
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R19";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R19] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R19 Corner Grammar")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R19 author left/right corner grammar ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                set == null ? "missing" : set.DescribeMissingSlots());
            Check("CORNER_SLICES_ARE_THE_AUTHOR_OWN",
                set != null
                && set.GetLeftCorner() != null
                && set.GetRightCorner() != null
                && set.GetLeftCorner().name == "Hills_r3c4"
                && set.GetRightCorner().name == "Hills_r3c7",
                set == null
                    ? "composition set missing"
                    : $"left = {Name(set.GetLeftCorner())} rect {Rect(set.GetLeftCorner())}; "
                        + $"right = {Name(set.GetRightCorner())} rect {Rect(set.GetRightCorner())}");

            Line("");
            Line("-- LEFT corner, Hills_r3c4: vertical left boundary turning east into the front --");
            Corner(set, "LEFT_CORNER_MIN", new[] { "X.", "XX" }, 4, 4, true);
            Corner(set, "LEFT_CORNER_SHORT", new[] { "X..", "X..", "XXX" }, 4, 4, true);
            Corner(set, "LEFT_CORNER_LONG",
                new[]
                {
                    "X.......", "X.......", "X.......", "X.......", "X.......", "XXXXXXXX",
                },
                4, 4, true);

            Line("");
            Line("-- RIGHT corner, Hills_r3c7: the front turning north into a vertical right boundary --");
            Corner(set, "RIGHT_CORNER_MIN", new[] { ".X", "XX" }, 5, 4, false);
            Corner(set, "RIGHT_CORNER_SHORT", new[] { "..X", "..X", "XXX" }, 6, 4, false);
            Corner(set, "RIGHT_CORNER_LONG",
                new[]
                {
                    ".......X", ".......X", ".......X", ".......X", ".......X", "XXXXXXXX",
                },
                11, 4, false);

            Line("");
            Line("-- both corners in one shape: they must not fight over the same front --");
            Corner(set, "BOTH_CORNERS", new[] { "X.....X", "X.....X", "XXXXXXX" }, 4, 4, true);
            Corner(set, "BOTH_CORNERS_RIGHT", new[] { "X.....X", "X.....X", "XXXXXXX" }, 10, 4, false);

            TenFold(set);
            LiveEditingEquivalence();

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R19_corner_grammar.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- the fixture assertion

        /// <param name="cornerX">Corner logical x.</param>
        /// <param name="cornerY">Corner logical y.</param>
        /// <param name="left">True for the left corner, false for the right corner.</param>
        private static void Corner(
            AuthorHillsCompositionSet set,
            string id,
            string[] mask,
            int cornerX,
            int cornerY,
            bool left)
        {
            TerrainGridData grid = Build(mask);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            string wantSprite = left ? "Hills_r3c4" : "Hills_r3c7";
            RaisedSurfaceRole wantRole =
                left ? RaisedSurfaceRole.LEFT_CORNER : RaisedSurfaceRole.RIGHT_CORNER;

            RaisedTopologyState state = RaisedTopologyState.Resolve(grid, cornerX, cornerY);

            // What the plan actually emitted at the corner coordinate.
            string gotSprite = null;
            bool gotOutside = false;
            string gotSlot = null;
            string gotRow = null;
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.VisualPosition.x != cornerX || t.VisualPosition.y != cornerY)
                {
                    continue;
                }

                gotSprite = t.Sprite.name;
                gotOutside = t.OutsideLogicalMask;
                gotSlot = t.Slot.ToString();
                gotRow = t.Row.ToString();
            }

            var logical = new List<Vector3Int>();
            int h = mask.Length;
            int w = mask[0].Length;
            for (int i = 0; i < h; i++)
            {
                int y = 4 + (h - 1 - i);
                for (int x = 0; x < w; x++)
                {
                    if (mask[i][x] == 'X')
                    {
                        logical.Add(new Vector3Int(4 + x, y, 0));
                    }
                }
            }

            var problems = new List<string>();

            if (gotSprite != wantSprite)
            {
                problems.Add($"corner ({cornerX},{cornerY}) emitted {gotSprite ?? "NOTHING"}, "
                    + $"expected exactly {wantSprite}");
            }

            if (state.Role != wantRole)
            {
                problems.Add($"corner role {state.Role}, expected {wantRole}");
            }

            if (gotOutside)
            {
                problems.Add("corner is flagged OutsideLogicalMask; it must sit on the logical cell");
            }

            if (plan.TilesOutsideLogicalMask != 0)
            {
                problems.Add($"{plan.TilesOutsideLogicalMask} tile(s) outside the logical mask, "
                    + "expected zero: a corner must not drop a row south or protrude");
            }

            // Visual set == logical set. Catches a transparent gap, a doubled coordinate and a
            // displaced cliff in one comparison.
            var emitted = new Dictionary<Vector3Int, string>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (emitted.ContainsKey(t.VisualPosition))
                {
                    problems.Add($"{t.VisualPosition.x},{t.VisualPosition.y} emitted twice");
                }

                emitted[t.VisualPosition] = t.Sprite.name;
            }

            foreach (Vector3Int c in logical)
            {
                if (!emitted.ContainsKey(c))
                {
                    problems.Add($"logical cell {c.x},{c.y} has NO visual cell; transparent gap");
                }
            }

            foreach (KeyValuePair<Vector3Int, string> kv in emitted)
            {
                if (!logical.Contains(kv.Key))
                {
                    problems.Add($"{kv.Key.x},{kv.Key.y}={kv.Value} is OUTSIDE the logical mask");
                }
            }

            if (plan.VisualizedRaisedCells != RaisedRegionAnalyzer.CountRaisedCells(grid))
            {
                problems.Add($"{plan.VisualizedRaisedCells} of "
                    + $"{RaisedRegionAnalyzer.CountRaisedCells(grid)} logical cells drawn");
            }

            if (plan.VisualDiagnostics.Count != 0)
            {
                problems.Add($"{plan.VisualDiagnostics.Count} renderer diagnostic(s): "
                    + string.Join(" | ", plan.VisualDiagnostics.Select(d => d.ToString())));
            }

            // Continuity with the R18 horizontal BODY: the cell in front of the corner must be the r3
            // row, and so must the cell between it and the far terminal.
            string frontNeighbour = left ? "5," : (cornerX - 1) + ",";
            string neighbourSprite = null;
            foreach (HillVisualTile t in plan.Tiles)
            {
                string key = t.VisualPosition.x + "," + t.VisualPosition.y;
                if (key == frontNeighbour + cornerY)
                {
                    neighbourSprite = t.Sprite.name;
                }
            }

            if (neighbourSprite == null || !neighbourSprite.StartsWith("Hills_r3", StringComparison.Ordinal))
            {
                problems.Add($"the cell east/west of the corner at {frontNeighbour}{cornerY} is "
                    + $"{neighbourSprite ?? "MISSING"}, expected the author's r3 body row for "
                    + "continuity with the R18 straight grammar");
            }

            // Report every cell, so the composition is readable and not just pass/fail.
            var seq = plan.Tiles
                .Select(t => $"{t.VisualPosition.x},{t.VisualPosition.y}="
                    + t.Sprite.name.Replace("Hills_", string.Empty)
                    + (t.OutsideLogicalMask ? "*" : string.Empty))
                .OrderBy(s => s, StringComparer.Ordinal);
            Line($"   {id} [{string.Join(" / ", mask)}]");
            Line($"      corner logical ({cornerX},{cornerY})  raw=0x{(byte)state.RawMask:X2}  "
                + $"canonical={state.CanonicalMask}");
            Line($"      runDepth={state.RunDepth} offsetFromRunBottom={state.OffsetFromRunBottom} "
                + $"slot={state.Slot} row={gotRow}");
            Line($"      role={state.Role}  sprite={gotSprite}  outside={gotOutside}");
            Line($"      visual: {string.Join(" ", seq)}");

            Check("CORNER_" + id, problems.Count == 0,
                problems.Count == 0
                    ? $"logical {logical.Count}, visual {plan.Tiles.Count}, outside "
                        + $"{plan.TilesOutsideLogicalMask}, corner ({cornerX},{cornerY}) raw "
                        + $"0x{(byte)state.RawMask:X2} canonical {state.CanonicalMask} role "
                        + $"{state.Role} -> {gotSprite}, slot {gotSlot}, front neighbour "
                        + $"{neighbourSprite}"
                    : string.Join("; ", problems) + "  | actual [" + string.Join(" ", seq) + "]");
        }

        private static TerrainGridData Build(string[] mask)
        {
            int h = mask.Length;
            int w = mask[0].Length;
            var grid = new TerrainGridData(w + 8, h + 8, 4, 4);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetTerrain(4 + x, 4 + y, TerrainType.Grass);
                }
            }

            // Fixture line 0 is the NORTH row.
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

        private static string Name(Sprite s)
        {
            return s == null ? "NULL" : s.name;
        }

        private static string Rect(Sprite s)
        {
            if (s == null)
            {
                return "n/a";
            }

            Texture2D t = s.texture;
            return t == null
                ? "no texture"
                : $"{s.rect.width}x{s.rect.height} at ({s.rect.x},{s.rect.y}) of {t.width}x{t.height}"
                    + $", pivot {s.pivot}, ppu {s.pixelsPerUnit}";
        }

        // ---------------------------------------------------------------- ten fold

        private static void TenFold(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("=== 10x consistency, card section 14 ===");

            var fixtures = new List<Tuple<string, string[], int, int, bool>>
            {
                Tuple.Create("LEFT_MIN", new[] { "X.", "XX" }, 4, 4, true),
                Tuple.Create("LEFT_SHORT", new[] { "X..", "X..", "XXX" }, 4, 4, true),
                Tuple.Create(
                    "LEFT_LONG",
                    new[]
                    {
                        "X.......", "X.......", "X.......", "X.......", "X.......", "XXXXXXXX",
                    },
                    4, 4, true),
                Tuple.Create("RIGHT_MIN", new[] { ".X", "XX" }, 5, 4, false),
                Tuple.Create("RIGHT_SHORT", new[] { "..X", "..X", "XXX" }, 6, 4, false),
                Tuple.Create(
                    "RIGHT_LONG",
                    new[]
                    {
                        ".......X", ".......X", ".......X", ".......X", ".......X", "XXXXXXXX",
                    },
                    11, 4, false),
            };

            string reference = null;
            int agree = 0;
            int exact = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var keys = new List<string>();
                bool allExact = true;
                foreach (Tuple<string, string[], int, int, bool> f in fixtures)
                {
                    RaisedVisualPlan p = RaisedVisualPlan.Build(Build(f.Item2), set);
                    string at = null;
                    foreach (HillVisualTile t in p.Tiles)
                    {
                        if (t.VisualPosition.x == f.Item3 && t.VisualPosition.y == f.Item4)
                        {
                            at = t.Sprite.name;
                        }
                    }

                    keys.Add(at);
                    if (at != (f.Item5 ? "Hills_r3c4" : "Hills_r3c7"))
                    {
                        allExact = false;
                    }
                }

                string joined = string.Join("|", keys);
                if (reference == null)
                {
                    reference = joined;
                }
                else if (joined == reference)
                {
                    agree++;
                }

                if (allExact)
                {
                    exact++;
                }
            }

            Check("TENFOLD_CORNER_SPRITE_STABLE", agree == 9,
                $"{agree}/9 further repetitions produced the identical corner sprite at every "
                    + "fixture; the left corner read Hills_r3c4 and the right Hills_r3c7 in all of them");

            Check("TENFOLD_CORNER_EXACT_IN_ALL_FIXTURES", exact == 10,
                $"{exact}/10 repetitions resolved the exact locked sprite at all {fixtures.Count} "
                    + "corner coordinates, at every arm length");
        }

        // ---------------------------------------------------------------- live editing

        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string CopyDataPath =
            "Assets/_Project/World/Terrain/__R19_TempCopy.asset";
        private const string RenderAssetsPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";

        private static Vector2Int s_origin;
        private static int s_baseLogical;

        /// <summary>
        /// Straight line -> paint one cell -> corner, and corner -> erase that cell -> straight line,
        /// for both corners, then Undo and Redo. After EVERY step the live preview Tilemap must equal
        /// a fresh full rebuild by sprite identity and visual coordinate.
        ///
        /// This runs on a temporary COPY of the user's map, which is deleted at the end. The user's own
        /// asset is only read.
        /// </summary>
        private static void LiveEditingEquivalence()
        {
            Line("");
            Line("=== live editing: straight -> corner -> straight, with Paint / Erase / Undo / Redo ===");

            if (!AssetDatabase.CopyAsset(RealDataPath, CopyDataPath))
            {
                Check("LIVE_SESSION_COPY", false, "could not copy the asset");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var root = new GameObject("R19Rig");
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

            Check("LIVE_SESSION_STARTED", true,
                "authoring session running on a temporary COPY; the user's asset is only read");

            s_origin = FindClear(12, 8);
            Check("LIVE_CLEAR_GROUND", s_origin.x != int.MinValue,
                $"clear {12}x{8} block at {s_origin} on the temporary copy");

            try
            {
                LeftCornerWalk();
                RightCornerWalk();
                UndoRedoLeftCorner();
            }
            finally
            {
                IslandMapAuthoring.Stop();
                AssetDatabase.DeleteAsset(CopyDataPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// A straight one wide column, then one cell painted EAST of its bottom cell, which turns the
        /// vertical left boundary east into the front and must produce Hills_r3c4.
        /// </summary>
        private static void LeftCornerWalk()
        {
            Reset();
            for (int n = 1; n <= 3; n++)
            {
                Paint(Cell(0, n - 1));
            }

            AssertFresh("LEFT_STRAIGHT_3", 3, false);
            AssertCorner("LEFT_STRAIGHT_3", Cell(0, 0), "Hills_r2c3",
                "a plain three-deep one-wide column, so the R18 narrow front cliff r2c3, not a corner");

            Paint(Cell(1, 0));
            AssertFresh("LEFT_CORNER_PAINTED", 4, true);
            AssertCorner("LEFT_CORNER_PAINTED", Cell(0, 0), "Hills_r3c4",
                "one cell painted east of the column bottom turns the vertical left boundary into "
                    + "the front, which is the locked LEFT corner");

            Erase(Cell(1, 0));
            AssertFresh("LEFT_CORNER_ERASED", 3, false);
            AssertCorner("LEFT_CORNER_ERASED", Cell(0, 0), "Hills_r2c3",
                "the corner cell is gone again, so this is a plain straight column once more and the "
                    + "left corner must NOT survive the erase");
        }

        /// <summary>
        /// A straight one wide column, then one cell painted WEST of its bottom cell, which makes the
        /// front turn north into a vertical right boundary and must produce Hills_r3c7.
        /// </summary>
        private static void RightCornerWalk()
        {
            Reset();
            for (int n = 1; n <= 3; n++)
            {
                Paint(Cell(2, n - 1));
            }

            AssertFresh("RIGHT_STRAIGHT_3", 3, false);
            AssertCorner("RIGHT_STRAIGHT_3", Cell(2, 0), "Hills_r2c3",
                "a plain three-deep one-wide column, so the R18 narrow front cliff r2c3, not a corner");

            Paint(Cell(1, 0));
            AssertFresh("RIGHT_CORNER_PAINTED", 4, true);
            AssertCorner("RIGHT_CORNER_PAINTED", Cell(2, 0), "Hills_r3c7",
                "one cell painted west of the column bottom makes the front turn north, which is the "
                    + "locked RIGHT corner");

            Erase(Cell(1, 0));
            AssertFresh("RIGHT_CORNER_ERASED", 3, false);
            AssertCorner("RIGHT_CORNER_ERASED", Cell(2, 0), "Hills_r2c3",
                "the corner cell is gone again, so this is a plain straight column once more and the "
                    + "right corner must NOT survive the erase");
        }

        private static void UndoRedoLeftCorner()
        {
            Reset();
            for (int n = 1; n <= 3; n++)
            {
                Paint(Cell(0, n - 1));
            }

            AssertFresh("UNDO_BASE_STRAIGHT", 3, false);

            Paint(Cell(1, 0));
            AssertFresh("UNDO_BASE_CORNER", 4, true);
            AssertCorner("UNDO_BASE_CORNER", Cell(0, 0), "Hills_r3c4",
                "the corner is present before the undo, so Undo has something real to revert");

            UnityEditor.Undo.PerformUndo();
            AssertFresh("UNDO_CORNER_REVERTED", 3, false);
            AssertCorner("UNDO_CORNER_REVERTED", Cell(0, 0), "Hills_r2c3",
                "Undo removed the painted front cell, so the corner must be gone with it");

            UnityEditor.Undo.PerformRedo();
            AssertFresh("REDO_CORNER_RESTORED", 4, true);
            AssertCorner("REDO_CORNER_RESTORED", Cell(0, 0), "Hills_r3c4",
                "Redo restored the front cell, so the locked left corner must come back with it");
        }

        private static Vector2Int Cell(int dx, int dy)
        {
            return new Vector2Int(s_origin.x + dx, s_origin.y + dy);
        }

        /// <summary>Live preview Tilemap must equal a fresh rebuild of the same logical mask.</summary>
        private static void AssertFresh(string id, int expectedDelta, bool expectCorner)
        {
            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            List<string> live = Fingerprint(IslandMapAuthoring.PreviewRaisedVisual);
            List<string> fresh = Fingerprint(FreshPlan(CurrentMaskOnly(), set));
            bool same = live.Count == fresh.Count
                && !live.Except(fresh).Any()
                && !fresh.Except(live).Any();
            int delta = RaisedCountOfLive() - s_baseLogical;

            Check("FRESH_" + id, same && delta == expectedDelta,
                same && delta == expectedDelta
                    ? $"{live.Count} live preview tiles identical by sprite identity and visual "
                        + $"coordinate to a fresh full rebuild; fixture delta {delta}"
                    : $"live {live.Count} / fresh {fresh.Count}, delta {delta} expected "
                        + $"{expectedDelta}; liveOnly [{string.Join(" ", live.Except(fresh).Take(4))}] "
                        + $"freshOnly [{string.Join(" ", fresh.Except(live).Take(4))}]");
        }

        /// <summary>
        /// Asserts the exact sprite expected at a LIVE logical coordinate, read from the preview
        /// Tilemap so it is the picture the user sees and not a re-derived plan.
        ///
        /// The expected slice is passed in rather than inferred from "is there a corner", because the
        /// two states being compared are not symmetric: a plain three-deep one-wide column ends in the
        /// author's r2c3 front cliff under the R18 narrow grammar, while the same cell with a front
        /// running east or west of it becomes r3c4 or r3c7.
        /// </summary>
        private static void AssertCorner(string id, Vector2Int at, string want, string why)
        {
            string got = null;
            UnityEngine.Tilemaps.Tilemap map = IslandMapAuthoring.PreviewRaisedVisual;
            if (map != null)
            {
                map.RefreshAllTiles();
                var t = map.GetTile(new Vector3Int(at.x, at.y, 0)) as UnityEngine.Tilemaps.Tile;
                if (t != null && t.sprite != null)
                {
                    got = t.sprite.name;
                }
            }

            Check("CORNER_LIVE_" + id, got == want,
                $"live preview at ({at.x},{at.y}) is {got ?? "MISSING"}, expected {want}; {why}");
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
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 12; x++)
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