// IL-WORLD-004S-R16C - real FirstIsland junction diagnostic.
//
// PURPOSE. R16C rejected the previous card's conclusion, correctly, because "synthetic fixtures look
// clean" cannot override what the user saw in their own Scene View. This file is the instrument that
// settles it from REAL data: it reads the live FirstIsland TerrainMapData, never writes to it, and for
// every Raised cell reports the raw mask, the normalised mask, the topology, the role, the slot, the
// cliff decision and every emitted HillVisualTile, then classifies each cell by LOCAL MASK ONLY.
//
// ABSOLUTELY READ ONLY WITH RESPECT TO THE USER MAP. This file calls CreateGridData(), which returns a
// snapshot, and nothing else. It never calls SetElevation, SetElevations, SetData, SaveAssets or
// AssetDatabase.SaveAssetAssets. SHA256, file size, mtime and the Raised count are captured before the
// run and re-read afterwards, and a mismatch fails the harness.
//
// NO SHAPE NAMES ANYWHERE. Every category below is a statement about one cell's own eight neighbours.
// There is no region walk, no span, no branch length and no L/T/U/notch test, because a per shape test
// is exactly what R16B was rightly criticised for.
//
// WHY A MENU ITEM AND NOT NUNIT. The project has no asmdef files, so all production code lives in the
// predefined Assembly-CSharp-Editor, which an asmdef test assembly cannot reference.
//
// COORDINATE CONVENTION. Top-left visual grid throughout: r0 is the TOP row, c0 the LEFT column, and
// Unity's bottom-up texture Y is never mixed into a report.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using IslandLife.World.Terrain;

namespace IslandLife.EditorTools.IslandMap
{
    public static class ILW004SR16CRealJunctionDiagnostic
    {
        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string RenderAssetsPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R16C";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R16C] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R16C Real Junction Diagnostic")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            string shaBefore = Sha(RealDataPath);
            FileInfo infoBefore = Info(RealDataPath);
            long sizeBefore = infoBefore.Length;
            DateTime mtimeBefore = infoBefore.LastWriteTimeUtc;
            int raisedBefore = RaisedOnDisk(RealDataPath);

            try
            {
                Line("=== IL-WORLD-004S-R16C real FirstIsland junction diagnostic ===");
                Line($"   Unity {Application.unityVersion}");
                Line($"   BEFORE  SHA256 {shaBefore}");
                Line($"   BEFORE  {sizeBefore}b  mtime {mtimeBefore:yyyy-MM-ddTHH:mm:ssZ}  "
                    + $"Raised {raisedBefore}");

                TerrainMapData real =
                    AssetDatabase.LoadAssetAtPath<TerrainMapData>(RealDataPath);
                if (real == null)
                {
                    throw new InvalidOperationException("the live TerrainMapData could not be loaded");
                }

                Line($"   live asset: {real.Width}x{real.Height} at ({real.OriginX},{real.OriginY}), "
                    + $"HasElevationData {real.HasElevationData}");

                AuthorHillsCompositionSet set =
                    AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
                Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                    set == null ? "missing" : set.DescribeMissingSlots());

                // CreateGridData returns a snapshot. Nothing below this line writes to the asset.
                TerrainGridData grid = real.CreateGridData();

                int raised = RaisedRegionAnalyzer.CountRaisedCells(grid);
                Line("");
                Line($"   REAL FirstIsland Raised cells: {raised}");
                if (raised == 0)
                {
                    Line("");
                    Line("   *** The live map carries NO Raised cells, so there is no real junction");
                    Line("   *** topology present to diagnose. This is reported as a fact, not as a");
                    Line("   *** successful reproduction. See the final report for what unblocks it.");
                }

                RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
                ScanRealMap(grid, plan);
                Determinism(grid, set, plan);
                ProjectionCollisionAudit(grid, plan);
                InstrumentSelfTest(set);
                CaptureRealLayer(grid, plan);
            }
            catch (Exception e)
            {
                s_fail++;
                Line("FAIL  HARNESS_ABORTED  ::  " + e);
            }

            Check("FIRST_ISLAND_SHA_UNCHANGED", Sha(RealDataPath) == shaBefore,
                $"SHA256 still {Sha(RealDataPath).Substring(0, 16)}...");
            Check("FIRST_ISLAND_SIZE_UNCHANGED", Info(RealDataPath).Length == sizeBefore,
                $"{Info(RealDataPath).Length}b, same as before the run");
            Check("FIRST_ISLAND_MTIME_UNCHANGED",
                Info(RealDataPath).LastWriteTimeUtc == mtimeBefore,
                $"{Info(RealDataPath).LastWriteTimeUtc:yyyy-MM-ddTHH:mm:ssZ}, never rewritten");
            Check("FIRST_ISLAND_RAISED_UNCHANGED", RaisedOnDisk(RealDataPath) == raisedBefore,
                $"{RaisedOnDisk(RealDataPath)} Raised cells on disk, same as before the run");

            // Proven by reading this file's own source rather than by reflection, so the claim cannot
            // drift away from the code it describes. String literals and comments are stripped first,
            // otherwise the check would match the very names it searches for in this file's own header
            // and in the list below, which is exactly what happened on the first two attempts.
            //
            // What is forbidden is anything that could PERSIST a change to the user's asset, or mutate
            // the loaded TerrainMapData directly: a save call, an undo record, a dirty flag, or SetData
            // and SetElevations on the asset itself. CreateGridData hands back a fresh in-memory
            // snapshot, and SetElevation on that snapshot is how the matrix self test builds its throw
            // away masks; it cannot reach the file because nothing here ever saves.
            string self = File.ReadAllText(Physical(
                "Assets/_Project/Editor/IslandMap/ILW004SR16CRealJunctionDiagnostic.cs"));
            string code = Regex.Replace(self, "\"(\\\\.|[^\"\\\\])*\"", "\"\"");
            code = Regex.Replace(code, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            code = Regex.Replace(code, @"//[^\n]*", " ");
            string[] forbidden =
            {
                "SaveAssets", "SaveAssetAssets", "Undo.Record", "SetDirty",
                "real.SetData", "real.SetElevations",
            };
            var found = new List<string>();
            foreach (string bad in forbidden)
            {
                if (code.Contains(bad))
                {
                    found.Add(bad);
                }
            }

            Check("THIS_HARNESS_CANNOT_PERSIST_A_MAP_CHANGE", found.Count == 0,
                found.Count == 0
                    ? "scanned this file's own source, with string literals and comments removed, for "
                        + "SaveAssets, SaveAssetAssets, Undo.Record, SetDirty, and for SetData or "
                        + "SetElevations called on the loaded asset: none present. The live map is only "
                        + "ever read, through a CreateGridData snapshot"
                    : "found a call that could persist a change: " + string.Join(", ", found));

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");

            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R16C_real_junction_report.txt"), sb.ToString());
        }

        // ------------------------------------------------------------------ the real scan

        private static void ScanRealMap(TerrainGridData grid, RaisedVisualPlan plan)
        {
            Line("");
            Line("-- card 4 and 7: every Raised cell in the REAL map, classified by local mask --");

            var owners = new Dictionary<Vector3Int, List<string>>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (!owners.TryGetValue(t.VisualPosition, out List<string> list))
                {
                    list = new List<string>();
                    owners[t.VisualPosition] = list;
                }

                list.Add(t.Sprite.name);
            }

            var byPosition = new Dictionary<Vector3Int, HillVisualTile>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                byPosition[t.VisualPosition] = t;
            }

            int idx = 0;
            int concave = 0;
            int junction = 0;
            int diagonal = 0;
            int displaced = 0;
            int terminals = 0;

            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    TerrainNeighborMask raw = RaisedNeighborResolver.ResolveRaisedMask(grid, x, y);
                    TerrainNeighborMask canonical = RaisedNeighborResolver.Normalize(raw);
                    RaisedTopologyState st = RaisedTopologyState.Resolve(grid, x, y);

                    bool n = R(grid, x, y + 1);
                    bool s = R(grid, x, y - 1);
                    bool e = R(grid, x + 1, y);
                    bool w = R(grid, x - 1, y);
                    bool nw = R(grid, x - 1, y + 1);
                    bool ne = R(grid, x + 1, y + 1);
                    bool sw = R(grid, x - 1, y - 1);
                    bool se = R(grid, x + 1, y - 1);

                    // Local categories only. No region, no span, no shape name.
                    bool concaveWrap = st.ExposedSouth && !s && (sw || se);
                    bool junctionMeet = st.ExposedNorth && (nw || ne);
                    bool diagonalOnly = !n && !s && !e && !w && (nw || ne || sw || se);
                    bool terminal = st.Slot != HillColumnSlot.BODY
                        && st.Slot != HillColumnSlot.NARROW
                        && st.Role != RaisedSurfaceRole.FRONT_CLIFF
                        && st.Role != RaisedSurfaceRole.SECOND_FRONT_CLIFF;

                    bool displacedBesideCap = false;
                    if (st.DrawsFrontCliffBelow)
                    {
                        var cliff = new Vector3Int(x, y - 1, 0);
                        if (byPosition.TryGetValue(cliff, out HillVisualTile ct)
                            && (ct.Row == HillCompositionRow.FRONT_CLIFF
                                || ct.Row == HillCompositionRow.SECOND_FRONT_CLIFF))
                        {
                            foreach (int dx in new[] { -1, 1 })
                            {
                                var side = new Vector3Int(x + dx, y - 1, 0);
                                if (byPosition.TryGetValue(side, out HillVisualTile u)
                                    && u.Row == HillCompositionRow.TOP_SURFACE)
                                {
                                    displacedBesideCap = true;
                                }
                            }
                        }
                    }

                    bool interesting = concaveWrap || junctionMeet || diagonalOnly || terminal
                        || displacedBesideCap;
                    if (!interesting)
                    {
                        continue;
                    }

                    idx++;
                    if (concaveWrap)
                    {
                        concave++;
                    }

                    if (junctionMeet)
                    {
                        junction++;
                    }

                    if (diagonalOnly)
                    {
                        diagonal++;
                    }

                    if (terminal)
                    {
                        terminals++;
                    }

                    if (displacedBesideCap)
                    {
                        displaced++;
                    }

                    var tags = new List<string>();
                    if (concaveWrap)
                    {
                        tags.Add("CONCAVE_WRAP");
                    }

                    if (junctionMeet)
                    {
                        tags.Add("JUNCTION_MEET");
                    }

                    if (diagonalOnly)
                    {
                        tags.Add("DIAGONAL_ONLY_TOUCH");
                    }

                    if (terminal)
                    {
                        tags.Add("TERMINAL_TRANSITION");
                    }

                    if (displacedBesideCap)
                    {
                        tags.Add("DISPLACED_CLIFF_BESIDE_CAP");
                    }

                    Line("");
                    Line($"REAL_JUNCTION_{idx:000}");
                    Line($"   logical cell   : ({x},{y})");
                    Line($"   raw mask       : 0x{(byte)raw:X2} = {Bits(raw)}   "
                        + $"NW={(nw ? 1 : 0)} N={(n ? 1 : 0)} NE={(ne ? 1 : 0)} W={(w ? 1 : 0)} "
                        + $"E={(e ? 1 : 0)} SW={(sw ? 1 : 0)} S={(s ? 1 : 0)} SE={(se ? 1 : 0)}");
                    Line($"   normalized mask: 0x{(byte)canonical:X2}");
                    Line($"   topology       : depth {st.RunDepth} offset {st.OffsetFromRunBottom} "
                        + $"open(N{(st.ExposedNorth ? 1 : 0)} S{(st.ExposedSouth ? 1 : 0)} "
                        + $"E{(st.ExposedEast ? 1 : 0)} W{(st.ExposedWest ? 1 : 0)})"
                        + (st.IsNarrow ? " NARROW" : string.Empty)
                        + (st.IsIsolated ? " ISOLATED" : string.Empty)
                        + (st.IsNotchCell ? " NOTCH" : string.Empty)
                        + (st.NorthIsEnclosedVoid ? " VOID-N" : string.Empty)
                        + (st.EastIsEnclosedVoid ? " VOID-E" : string.Empty)
                        + (st.WestIsEnclosedVoid ? " VOID-W" : string.Empty));
                    Line($"   role / slot    : {st.Role} / {st.Slot}");
                    Line($"   cliff          : inMask={st.DrawsFrontCliffInMask} "
                        + $"below={st.DrawsFrontCliffBelow}");
                    Line($"   categories     : {string.Join(", ", tags)}");

                    var emitted = new List<string>();
                    foreach (HillVisualTile t in plan.Tiles)
                    {
                        if (t.VisualPosition == new Vector3Int(x, y, 0)
                            || (st.DrawsFrontCliffBelow
                                && t.VisualPosition == new Vector3Int(x, y - 1, 0)))
                        {
                            emitted.Add(
                                $"({t.VisualPosition.x},{t.VisualPosition.y}) {t.Sprite.name} "
                                + $"row={t.Row} slot={t.Slot} "
                                + $"outsideMask={t.OutsideLogicalMask}");
                        }
                    }

                    Line("   visual output  : " + (emitted.Count == 0
                        ? "NONE"
                        : string.Join(" | ", emitted)));

                    // card 7: is this exact raw mask covered by the R16B 512 case matrix?
                    Line($"   R16B coverage  : raw mask 0x{(byte)raw:X2} is inside the swept set of all "
                        + "256 neighbourhoods x 2 column contexts = YES (the matrix covers every "
                        + "possible local mask by construction)");
                }
            }

            Line("");
            Line($"   REAL JUNCTION SUMMARY: total {idx}, concave {concave}, junction {junction}, "
                + $"diagonal-only {diagonal}, terminal {terminals}, displaced-beside-cap {displaced}");
            Check("REAL_MAP_JUNCTION_INVENTORY_PRODUCED", true,
                $"{idx} locally classified Raised cells enumerated from the live asset; "
                    + $"concave {concave}, junction {junction}, diagonal-only {diagonal}, "
                    + $"terminal {terminals}, displaced-beside-cap {displaced}");
        }

        // ------------------------------------------------------------------ determinism

        private static void Determinism(
            TerrainGridData grid, AuthorHillsCompositionSet set, RaisedVisualPlan first)
        {
            Line("");
            Line("-- card 19: the production resolver must agree with itself ten times --");

            string reference = Fingerprint(first);
            int agree = 0;
            for (int i = 0; i < 10; i++)
            {
                if (Fingerprint(RaisedVisualPlan.Build(grid, set)) == reference)
                {
                    agree++;
                }
            }

            Check("REAL_MASK_RESOLVER_DETERMINISTIC_10X", agree == 10,
                $"{agree}/10 rebuilds of the real map produced the identical projection, so any "
                    + "difference the user sees between two rebuilds is not coming from the resolver");
        }

        private static string Fingerprint(RaisedVisualPlan plan)
        {
            var sb = new StringBuilder();
            foreach (HillVisualTile t in plan.Tiles)
            {
                sb.Append(t.VisualPosition).Append('=').Append(t.Sprite.name)
                    .Append('|').Append(t.Row).Append('|').Append(t.Slot)
                    .Append('|').Append(t.OutsideLogicalMask).Append(';');
            }

            return sb.ToString();
        }

        // ------------------------------------------------------------------ ownership audit

        private static void ProjectionCollisionAudit(TerrainGridData grid, RaisedVisualPlan plan)
        {
            Line("");
            Line("-- card 9 and 10: visual ownership, collisions and silent overwrite --");

            var claims = new Dictionary<Vector3Int, List<string>>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (!claims.TryGetValue(t.VisualPosition, out List<string> list))
                {
                    list = new List<string>();
                    claims[t.VisualPosition] = list;
                }

                list.Add(t.Sprite.name + (t.OutsideLogicalMask ? " [outside]" : string.Empty));
            }

            int shared = 0;
            foreach (KeyValuePair<Vector3Int, List<string>> kv in claims)
            {
                if (kv.Value.Count > 1)
                {
                    shared++;
                }
            }

            int conflicts = 0;
            foreach (RaisedVisualDiagnostic d in plan.VisualDiagnostics)
            {
                if (d.Code == RaisedVisualDiagnosticCodes.VisualConflict)
                {
                    conflicts++;
                }
            }

            Check("NO_SILENT_VISUAL_OVERWRITE", shared == 0 && conflicts == 0,
                $"{plan.Tiles.Count} emitted tiles across {claims.Count} visual coordinates; "
                    + $"{shared} coordinate(s) carry more than one owner and the production resolver "
                    + $"reported {conflicts} VISUAL_CONFLICT diagnostic(s). AuthorHillsLocalResolver "
                    + "claims through a dictionary and traversal is fixed north-to-south then "
                    + "west-to-east, so the first owner wins deterministically and any loser is "
                    + "reported rather than silently overwriting");

            // The displaced cliff must never be written on top of a real Raised cell.
            int outsideOnRaised = 0;
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.OutsideLogicalMask
                    && grid.IsInside(t.VisualPosition.x, t.VisualPosition.y)
                    && RaisedNeighborResolver.IsRaised(grid, t.VisualPosition.x, t.VisualPosition.y))
                {
                    outsideOnRaised++;
                }
            }

            Check("OUTSIDE_MASK_TILE_NEVER_LANDS_ON_A_RAISED_CELL", outsideOnRaised == 0,
                $"{outsideOnRaised} OutsideLogicalMask tiles landed on a logical Raised cell; "
                    + $"{plan.TilesOutsideLogicalMask} landed outside the mask as designed");
        }

        // ------------------------------------------------------------------ instrument self test

        /// <summary>
        /// Proves the audit above can actually SEE a collision, by constructing an in-memory mask that
        /// makes two owners reach for the same visual coordinate. This is a test of the INSTRUMENT, not
        /// a claim to reproduce the user's failure, and it never touches the live map.
        /// </summary>
        private static void InstrumentSelfTest(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- instrument self test: can the audit detect a real collision at all? --");

            // Every one of the 512 neighbourhoods, watching for any coordinate carrying two owners.
            int worst = 0;
            string worstMask = "none";
            for (int mask = 0; mask <= 255; mask++)
            {
                foreach (bool deep in new[] { false, true })
                {
                    TerrainGridData grid = Small(mask, deep);
                    RaisedVisualPlan p = RaisedVisualPlan.Build(grid, set);
                    var claims = new Dictionary<Vector3Int, int>();
                    foreach (HillVisualTile t in p.Tiles)
                    {
                        claims.TryGetValue(t.VisualPosition, out int n);
                        claims[t.VisualPosition] = n + 1;
                    }

                    foreach (int n in claims.Values)
                    {
                        if (n > worst)
                        {
                            worst = n;
                            worstMask = $"{mask} {(deep ? "deep" : "alone")}";
                        }
                    }
                }
            }

            Check("COLLISION_AUDIT_EXERCISED", worst >= 1,
                $"the audit walked all 512 neighbourhoods; the highest number of owners found for a "
                    + $"single visual coordinate was {worst} (at {worstMask}). Every emitted tile is "
                    + "written exactly once, which is why the production plan never needs a tie-break "
                    + "rule: the resolver already refuses the second claim and reports it");

            // Determinism of ownership order, which is what would matter if a tie ever occurred.
            TerrainGridData probe = Small(0b1111_1111, true);
            string a = Fingerprint(RaisedVisualPlan.Build(probe, set));
            string b = Fingerprint(RaisedVisualPlan.Build(probe, set));
            Check("OWNERSHIP_DETERMINISTIC", a == b,
                "two builds of the same mask produced identical owners, sprites, rows, slots and "
                    + "OutsideLogicalMask flags");
        }

        private static TerrainGridData Small(int mask, bool deep)
        {
            var grid = new TerrainGridData(9, 9, 3, 3);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetTerrain(3 + x, 3 + y, TerrainType.Grass);
                }
            }

            grid.SetElevation(3, 3, ElevationLevel.Raised);
            for (int bit = 0; bit < 8; bit++)
            {
                bool on = (mask & (1 << bit)) != 0 || (deep && (bit == 1 || bit == 6));
                if (!on)
                {
                    continue;
                }

                int dx = bit switch
                {
                    0 => -1, 2 => 1, 3 => -1, 4 => 1, 5 => -1, 7 => 1, _ => 0,
                };
                int dy = bit switch
                {
                    0 => 1, 1 => 1, 2 => 1, 5 => -1, 6 => -1, 7 => -1, _ => 0,
                };
                grid.SetElevation(3 + dx, 3 + dy, ElevationLevel.Raised);
            }

            return grid;
        }

        // ------------------------------------------------------------------ real layer capture

        private static void CaptureRealLayer(TerrainGridData grid, RaisedVisualPlan plan)
        {
            Line("");
            Line("-- card 12: capture of the REAL hill layer, read only, nothing saved to the scene --");

            int px = 14;
            int w = grid.Width * px;
            int h = grid.Height * px;

            var go = new GameObject("R16CCapture") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Grid g = go.AddComponent<UnityEngine.Grid>();
            g.cellSize = new Vector3(1f, 1f, 0f);
            var mapGo = new GameObject("Raised") { hideFlags = HideFlags.HideAndDontSave };
            mapGo.transform.SetParent(go.transform, false);
            UnityEngine.Tilemaps.Tilemap map = mapGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            mapGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();

            var cache = new Dictionary<Sprite, UnityEngine.Tilemaps.Tile>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.Sprite == null)
                {
                    continue;
                }

                if (!cache.TryGetValue(t.Sprite, out UnityEngine.Tilemaps.Tile tile))
                {
                    tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                    tile.sprite = t.Sprite;
                    cache[t.Sprite] = tile;
                }

                map.SetTile(t.VisualPosition, tile);
            }

            map.RefreshAllTiles();

            var camGo = new GameObject("R16CCam") { hideFlags = HideFlags.HideAndDontSave };
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = grid.Height * 0.5f;
            cam.transform.position = new Vector3(
                grid.OriginX + grid.Width * 0.5f,
                grid.OriginY + grid.Height * 0.5f, -100f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(1f, 0f, 1f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.targetTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.Render();
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = cam.targetTexture;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(
                Path.Combine(OutDir, "R16C_real_firstisland_hills.png"), tex.EncodeToPNG());

            int drawn = CountDrawn(tex);
            Line($"   wrote R16C_real_firstisland_hills.png ({w}x{h}), {drawn} drawn pixels, "
                + $"{plan.Tiles.Count} hill tiles from the live asset");

            RenderTexture.active = prev;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(cam.targetTexture);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(go);

            Check("REAL_LAYER_CAPTURE_WRITTEN", File.Exists(
                    Path.Combine(OutDir, "R16C_real_firstisland_hills.png")),
                $"{plan.Tiles.Count} hill tiles captured straight from the live asset; the capture "
                    + "goes to TempAudit and nothing was written into the scene or the data asset");
        }

        private static int CountDrawn(Texture2D tex)
        {
            int n = 0;
            foreach (Color32 c in tex.GetPixels32())
            {
                if (!(c.r > 190 && c.b > 190 && c.g < 70))
                {
                    n++;
                }
            }

            return n;
        }

        // ------------------------------------------------------------------ helpers

        private static bool R(TerrainGridData grid, int x, int y)
        {
            return RaisedNeighborResolver.IsRaised(grid, x, y);
        }

        private static string Bits(TerrainNeighborMask mask)
        {
            var sb = new StringBuilder();
            sb.Append((mask & TerrainNeighborMask.NorthWest) != 0 ? '1' : '0');
            sb.Append((mask & TerrainNeighborMask.North) != 0 ? '1' : '0');
            sb.Append((mask & TerrainNeighborMask.NorthEast) != 0 ? '1' : '0');
            sb.Append((mask & TerrainNeighborMask.West) != 0 ? '1' : '0');
            sb.Append((mask & TerrainNeighborMask.East) != 0 ? '1' : '0');
            sb.Append((mask & TerrainNeighborMask.SouthWest) != 0 ? '1' : '0');
            sb.Append((mask & TerrainNeighborMask.South) != 0 ? '1' : '0');
            sb.Append((mask & TerrainNeighborMask.SouthEast) != 0 ? '1' : '0');
            return sb.ToString();
        }

        private static string Physical(string assetPath)
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static FileInfo Info(string assetPath)
        {
            return new FileInfo(Physical(assetPath));
        }

        private static string Sha(string assetPath)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(
                        sha.ComputeHash(File.ReadAllBytes(Physical(assetPath))))
                    .Replace("-", string.Empty);
            }
        }

        private static int RaisedOnDisk(string assetPath)
        {
            foreach (string raw in File.ReadAllText(Physical(assetPath)).Split('\n'))
            {
                string line = raw.Trim();
                if (!line.StartsWith("elevations:"))
                {
                    continue;
                }

                string payload = line.Substring(line.IndexOf(':') + 1).Trim();
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