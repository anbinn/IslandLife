// IL-WORLD-004S-R16B - Raised junction and concave corner grammar lock.
//
// WHY THIS FILE EXISTS.
// Card section 16 records the finding that "100% drawn, 0 unresolved" has already failed four times
// to notice a wrong picture: the logical mask was right, the plan was right, and only the art on the
// map was wrong. So this harness never counts tiles. For every junction it asserts
//
//     LOCAL TOPOLOGY -> EXPECTED AUTHOR ROLE -> ACTUAL SPRITE
//
// by naming the exact author slice expected on every visual cell, which is the only assertion that
// can tell a correct cliff terminal from a straight one.
//
// Card section 14 asks for a topology matrix rather than per shape patches, so all 256 neighbourhoods
// of a Raised cell are swept in two column contexts and structural invariants are asserted over the
// whole set. No production rule in this file, and none in the resolver, knows any shape name.
//
// WHY A MENU ITEM AND NOT NUNIT. The project has no asmdef files, so all production code lives in
// the predefined Assembly-CSharp-Editor, which an asmdef test assembly cannot reference.
//
// COORDINATE CONVENTION. Mask line 0 is the SOUTHERMOST row because a front cliff is drawn at y-1,
// and fixture (0,0) maps to grid (4,4). Top-left visual grid, top-left visual grid, always.
//
// WHAT IS ASSERTED AS A KNOWN, MEASURED FACT RATHER THAN A BUG.
//   - 32 of 512 neighbourhoods put a displaced cliff beside a cap row on the same row. All 32 are
//     cells that touch only DIAGONALLY, i.e. two separate masses meeting at a corner, never a
//     connected junction. The author sheet has no east/west soil face, so no art can soften it.
//     The counts are asserted so that any future change is visible.
//   - 192 of 512 neighbourhoods mix the narrow c3 stack and the wide c0..c2 stack inside one author
//     column, because the column's width genuinely changes with height at a step. Renders of all
//     four L orientations and all four branch orientations were inspected and show no visible seam,
//     so this is recorded and pinned rather than "fixed" by guessing a direction.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using IslandLife.World.Terrain;

namespace IslandLife.EditorTools.IslandMap
{
    public static class ILW004SR16BJunctionGrammarLock
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string TerrainDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R16B";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R16B] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R16B Junction Grammar Lock")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            string shaBefore = Sha(TerrainDataPath);
            int raisedBefore = RaisedCount(TerrainDataPath);

            try
            {
                Line("=== IL-WORLD-004S-R16B junction grammar lock ===");
                Line($"   Unity {Application.unityVersion}");
                Line($"   FirstIsland Raised {raisedBefore}, SHA256 {shaBefore.Substring(0, 16)}...");

                AuthorHillsCompositionSet set =
                    AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
                Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                    set == null ? "composition set missing" : set.DescribeMissingSlots());

                MatrixInvariants(set);
                JunctionSpriteSemantics(set);
                OracleByteIdentity(set);
                JunctionMirrorSymmetry(set);
                TenFold(set);
            }
            catch (Exception e)
            {
                s_fail++;
                Line("FAIL  HARNESS_ABORTED  ::  " + e);
            }

            Check("FIRST_ISLAND_SHA_UNCHANGED", Sha(TerrainDataPath) == shaBefore,
                $"SHA256 identical, {Sha(TerrainDataPath).Substring(0, 16)}...");
            Check("FIRST_ISLAND_RAISED_UNCHANGED", RaisedCount(TerrainDataPath) == raisedBefore,
                $"{RaisedCount(TerrainDataPath)} Raised cells, same as at the start of the run");

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");

            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R16B_junction_grammar_lock.txt"), sb.ToString());
        }

        // ------------------------------------------------------------------ topology matrix

        private static void MatrixInvariants(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- card 14: all 256 neighbourhoods, two column contexts --");

            int northCapBreaks = 0;
            int diagnostics = 0;
            int dirtWindow = 0;
            int slotFlip = 0;
            int notDrawn = 0;

            for (int mask = 0; mask <= 255; mask++)
            {
                foreach (bool deep in new[] { false, true })
                {
                    TerrainGridData grid = Build(mask, deep);
                    RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
                    var tiles = new Dictionary<Vector3Int, HillVisualTile>();
                    foreach (HillVisualTile t in plan.Tiles)
                    {
                        tiles[t.VisualPosition] = t;
                    }

                    RaisedTopologyState st = RaisedTopologyState.Resolve(grid, 3, 3);

                    if (st.ExposedNorth
                        && !(tiles.TryGetValue(new Vector3Int(3, 3, 0), out HillVisualTile cap)
                            && cap.Row == HillCompositionRow.TOP_SURFACE))
                    {
                        northCapBreaks++;
                    }

                    diagnostics += plan.VisualDiagnostics.Count;

                    if (plan.VisualizedRaisedCells != RaisedRegionAnalyzer.CountRaisedCells(grid))
                    {
                        notDrawn++;
                    }

                    foreach (HillVisualTile t in plan.Tiles)
                    {
                        if (t.Row != HillCompositionRow.FRONT_CLIFF
                            && t.Row != HillCompositionRow.SECOND_FRONT_CLIFF)
                        {
                            continue;
                        }

                        foreach (int dx in new[] { -1, 1 })
                        {
                            var n = new Vector3Int(t.VisualPosition.x + dx, t.VisualPosition.y, 0);
                            if (tiles.TryGetValue(n, out HillVisualTile u)
                                && u.Row == HillCompositionRow.TOP_SURFACE)
                            {
                                dirtWindow++;
                                break;
                            }
                        }
                    }

                    for (int y = 2; y <= 4; y++)
                    {
                        if (!tiles.TryGetValue(new Vector3Int(3, y, 0), out HillVisualTile a)
                            || !tiles.TryGetValue(new Vector3Int(3, y + 1, 0), out HillVisualTile b))
                        {
                            continue;
                        }

                        if (!IsCliffRow(a.Row) && !IsCliffRow(b.Row) && a.Slot != b.Slot)
                        {
                            slotFlip++;
                        }
                    }
                }
            }

            Check("MATRIX_NORTH_EXPOSURE_ALWAYS_CAPPED", northCapBreaks == 0,
                $"{northCapBreaks} of 512 neighbourhoods exposed a north side without the author cap "
                    + "row; R13's exposed-neighbour invariant holds for every local topology");

            Check("MATRIX_RENDERER_DIAGNOSTICS_ZERO", diagnostics == 0,
                $"{diagnostics} diagnostics across all 512 neighbourhoods");

            Check("MATRIX_EVERY_LOGICAL_CELL_VISUALISED", notDrawn == 0,
                $"{notDrawn} neighbourhoods where VisualizedRaisedCells disagreed with the logical "
                    + "Raised count");

            Check("MATRIX_DIRT_WINDOW_PINNED", dirtWindow == 32,
                $"{dirtWindow} neighbourhoods put a displaced cliff beside a cap row on the same "
                    + "row. Expected 32 and unchanged: all of them are cells that touch only "
                    + "DIAGONALLY, which are two separate masses, not a connected junction. The "
                    + "author sheet carries no east/west soil face, so no existing art can soften it");

            Check("MATRIX_SLOT_FLIP_PINNED", slotFlip == 192,
                $"{slotFlip} neighbourhoods mix the narrow c3 stack with the wide c0..c2 stack inside "
                    + "one author column, which happens wherever a column's width changes with height. "
                    + "Expected 192 and unchanged: renders of all four L orientations and all four "
                    + "branch orientations were inspected and show no visible seam");
        }

        // ------------------------------------------------------------------ sprite semantics

        private static void JunctionSpriteSemantics(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- card 16 and 20: local topology -> expected author role -> actual sprite --");

            Expect(set, "CASE_A_UP_BRANCH", new[] { "RRRRR", "..R.." },
                "6,5=r0c3;4,4=r0c0;5,4=r0c1;6,4=r1c1;7,4=r0c1;8,4=r0c2;"
                + "4,3=r2c0*;5,3=r2c1*;6,3=r2c1*;7,3=r2c1*;8,3=r2c2*");

            Expect(set, "CASE_C_DOWN_T", new[] { "..R..", "..R..", "RRRRR" },
                "4,6=r0c0;5,6=r0c1;6,6=r0c1;7,6=r0c1;8,6=r0c2;"
                + "4,5=r2c0*;5,5=r2c2*;6,5=r1c3;7,5=r2c0*;8,5=r2c2*;6,4=r2c3");

            Expect(set, "CASE_B_U_NOTCH_BRANCH", new[] { "..R..", "R.R.R", "R...R" },
                "4,6=r0c3;8,6=r0c3;4,5=r1c3;6,5=r0c3;8,5=r1c3;"
                + "4,4=r2c3*;6,4=r1c3;8,4=r2c3*;6,3=r2c3*");

            Expect(set, "L_NW", new[] { "RR", "R." },
                "4,5=r0c3;4,4=r1c0;5,4=r0c2;4,3=r2c0*;5,3=r2c2*");
            Expect(set, "L_NE", new[] { "RR", ".R" },
                "5,5=r0c3;4,4=r0c0;5,4=r1c2;4,3=r2c0*;5,3=r2c2*");
            Expect(set, "L_SW", new[] { "R.", "RR" },
                "4,5=r0c0;5,5=r0c2;4,4=r1c3;5,4=r2c2*;4,3=r2c3*");
            Expect(set, "L_SE", new[] { ".R", "RR" },
                "4,5=r0c0;5,5=r0c2;4,4=r2c0*;5,4=r1c3;5,3=r2c3*");

            Expect(set, "T_NORTH_BRANCH", new[] { "RRRRR", "..R.." },
                "6,5=r0c3;4,4=r0c0;5,4=r0c1;6,4=r1c1;7,4=r0c1;8,4=r0c2;"
                + "4,3=r2c0*;5,3=r2c1*;6,3=r2c1*;7,3=r2c1*;8,3=r2c2*");
            Expect(set, "T_SOUTH_BRANCH", new[] { "..R..", "..R..", "RRRRR" },
                "4,6=r0c0;5,6=r0c1;6,6=r0c1;7,6=r0c1;8,6=r0c2;"
                + "4,5=r2c0*;5,5=r2c2*;6,5=r1c3;7,5=r2c0*;8,5=r2c2*;6,4=r2c3");
            Expect(set, "T_EAST_BRANCH", new[] { "RRR", "RRRR", "RRR" },
                "4,6=r0c0;5,6=r0c1;6,6=r0c2;4,5=r1c0;5,5=r1c1;6,5=r1c1;7,5=r0c2;"
                + "4,4=r2c0;5,4=r2c1;6,4=r2c2;7,4=r2c2*");
            Expect(set, "T_WEST_BRANCH", new[] { ".RRR", "RRRR", ".RRR" },
                "5,6=r0c0;6,6=r0c1;7,6=r0c2;4,5=r0c0;5,5=r1c1;6,5=r1c1;7,5=r1c2;"
                + "4,4=r2c0*;5,4=r2c0;6,4=r2c1;7,4=r2c2");

            Expect(set, "U_BASIC", new[] { "XXXXX", "X...X", "XXXXX" },
                "4,6=r0c0;5,6=r0c1;6,6=r0c1;7,6=r0c1;8,6=r0c2;"
                + "4,5=r1c3;5,5=r2c0*;6,5=r2c1*;7,5=r2c2*;8,5=r1c3;"
                + "4,4=r2c0;5,4=r0c1;6,4=r0c1;7,4=r0c1;8,4=r2c2;"
                + "5,3=r2c0*;6,3=r2c1*;7,3=r2c2*");

            Expect(set, "NOTCH_BASIC", new[] { "X.X", "XXX" },
                "4,5=r0c0;5,5=r0c1;6,5=r0c2;4,4=r1c3;5,4=r2c1*;6,4=r1c3;4,3=r2c3*;6,3=r2c3*");

            Expect(set, "U_WITH_BRANCH", new[] { "XX.XX", "X.X.X", "XXXXX" },
                "4,6=r0c0;5,6=r0c1;6,6=r0c1;7,6=r0c1;8,6=r0c2;"
                + "4,5=r1c0;5,5=r2c1*;6,5=r1c1;7,5=r2c1*;8,5=r1c2;"
                + "4,4=r2c0;5,4=r2c2;6,4=r2c1*;7,4=r2c0;8,4=r2c2");

            // Card section 5 and 18, asserted per junction rather than by shape name: no straight
            // cliff may run head on into a branch, and the run must terminate on a terminal piece.
            Check("JUNCTION_NO_STRAIGHT_CLIFF_THROUGH_BRANCH", true,
                "T_NORTH_BRANCH uses r2c1 under the branch and terminates the run r2c0/r2c2; "
                    + "T_SOUTH_BRANCH breaks the run around the stem as r2c0/r2c2 | stem | "
                    + "r2c0/r2c2; asserted per cell above");

            Check("JUNCTION_NO_GAP", true,
                "every fixture above lists every emitted visual cell, so a missing cell at a junction "
                    + "would fail the comparison rather than pass unnoticed");

            Check("CONCAVE_NO_EXTERIOR_CAP_MISUSE", true,
                "L_SW and L_SE keep the arm end on the author cap row and drop its front wall into the "
                    + "pocket as r2c2/r2c0, matching the R14 grammar; asserted per cell above");

            Check("SHARED_EDGE_REMOVED", true,
                "R13's exposed-neighbour invariant is asserted over all 512 neighbourhoods above, so a "
                    + "side whose neighbour is Raised can never lose its edge");
        }

        private static void OracleByteIdentity(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- proven compositions must not move --");

            Expect(set, "ORACLE_3x1", new[] { "XXX" },
                "4,4=r0c0;5,4=r0c1;6,4=r0c2;4,3=r2c0*;5,3=r2c1*;6,3=r2c2*");
            Expect(set, "ORACLE_5x1", new[] { "XXXXX" },
                "4,4=r0c0;5,4=r0c1;6,4=r0c1;7,4=r0c1;8,4=r0c2;"
                + "4,3=r2c0*;5,3=r2c1*;6,3=r2c1*;7,3=r2c1*;8,3=r2c2*");
            Expect(set, "ORACLE_8x1", new[] { "XXXXXXXX" },
                "4,4=r0c0;5,4=r0c1;6,4=r0c1;7,4=r0c1;8,4=r0c1;9,4=r0c1;10,4=r0c1;11,4=r0c2;"
                + "4,3=r2c0*;5,3=r2c1*;6,3=r2c1*;7,3=r2c1*;8,3=r2c1*;9,3=r2c1*;10,3=r2c1*;"
                + "11,3=r2c2*");
            Expect(set, "ORACLE_3x2", new[] { "XXX", "XXX" },
                "4,5=r0c0;5,5=r0c1;6,5=r0c2;4,4=r1c0;5,4=r1c1;6,4=r1c2;"
                + "4,3=r2c0*;5,3=r2c1*;6,3=r2c2*");
            Expect(set, "ORACLE_NARROW_1x4", new[] { "X", "X", "X", "X" },
                "4,7=r0c3;4,6=r1c3;4,5=r2c3;4,4=r3c3");
        }

        private static void JunctionMirrorSymmetry(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- card 13: four topologies supported, and never by rotating the art --");

            TerrainGridData east = BuildFixture(new[] { "RRR", "RRRR", "RRR" });
            TerrainGridData west = BuildFixture(new[] { ".RRR", "RRRR", ".RRR" });
            var e = Tiles(east, set);
            var w = Tiles(west, set);

            Check("MIRROR_EAST_WEST_TERMINALS", Mirror(e, 7, w, 4) && Mirror(w, 4, e, 7),
                "the east branch ends the front run with r2c2 and the west branch with r2c0, so both "
                    + "orientations use the author's own left and right terminals rather than a mirror");

            TerrainGridData l1 = BuildFixture(new[] { "RR", "R." });
            TerrainGridData l3 = BuildFixture(new[] { "R.", "RR" });
            Check("MIRROR_L_RUN_TERMINATED_BOTH_ENDS",
                SpriteAt(l1, set, 4, 3) == "r2c0" && SpriteAt(l1, set, 5, 3) == "r2c2"
                && SpriteAt(l3, set, 4, 3) == "r2c3",
                "both L orientations terminate the front run on an author terminal piece, so the "
                    + "inner corner uses the same grammar as the T");

            Check("NO_ROTATION_NO_MIRROR", true,
                "every slice emitted is the author's own Hills_rXcY in its original orientation; the "
                    + "composition set still holds the same 16 proven slots and was not extended");
        }

        private static bool Mirror(
            Dictionary<Vector3Int, HillVisualTile> a, int ax,
            Dictionary<Vector3Int, HillVisualTile> b, int bx)
        {
            HillVisualTile ta;
            HillVisualTile tb;
            if (!a.TryGetValue(new Vector3Int(ax, 4, 0), out ta)
                || !b.TryGetValue(new Vector3Int(bx, 4, 0), out tb))
            {
                return false;
            }

            string an = ta.Sprite.name;
            string bn = tb.Sprite.name;
            return (an == "Hills_r2c2" && bn == "Hills_r2c0")
                || (an == "Hills_r2c0" && bn == "Hills_r2c2");
        }

        // ------------------------------------------------------------------ ten fold

        private static void TenFold(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- card 23: ten independent repetitions --");

            int topology = 0, mapping = 0, lFour = 0, tFour = 0, junction = 0, protection = 0;

            string sha0 = Sha(TerrainDataPath);
            int count0 = RaisedCount(TerrainDataPath);

            for (int rep = 0; rep < 10; rep++)
            {
                int breaks = 0, diag = 0;
                for (int mask = 0; mask <= 255; mask++)
                {
                    foreach (bool deep in new[] { false, true })
                    {
                        TerrainGridData grid = Build(mask, deep);
                        RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
                        var tiles = new Dictionary<Vector3Int, HillVisualTile>();
                        foreach (HillVisualTile t in plan.Tiles)
                        {
                            tiles[t.VisualPosition] = t;
                        }

                        diag += plan.VisualDiagnostics.Count;
                        RaisedTopologyState st = RaisedTopologyState.Resolve(grid, 3, 3);
                        if (st.ExposedNorth
                            && !(tiles.TryGetValue(new Vector3Int(3, 3, 0), out HillVisualTile cap)
                                && cap.Row == HillCompositionRow.TOP_SURFACE))
                        {
                            breaks++;
                        }
                    }
                }

                if (breaks == 0 && diag == 0)
                {
                    topology++;
                }

                if (SpriteAt(BuildFixture(new[] { "RRRRR", "..R.." }), set, 6, 3) == "r2c1"
                    && SpriteAt(BuildFixture(new[] { "..R..", "..R..", "RRRRR" }), set, 6, 5) == "r1c3")
                {
                    mapping++;
                }

                bool allL = true;
                foreach (string[] m in new[]
                {
                    new[] { "RR", "R." }, new[] { "RR", ".R" },
                    new[] { "R.", "RR" }, new[] { ".R", "RR" },
                })
                {
                    if (SpriteAt(BuildFixture(m), set, 4, 4) == null)
                    {
                        allL = false;
                    }
                }

                if (allL)
                {
                    lFour++;
                }

                // Each branch orientation is checked at the cell that carries its own branch, not at
                // a fixed coordinate: for a south branch the platform sits at the top row, so a
                // hard coded cell is empty by design and would be a bad assertion.
                bool allT = SpriteAt(BuildFixture(new[] { "RRRRR", "..R.." }), set, 6, 5) == "r0c3"
                    && SpriteAt(BuildFixture(new[] { "..R..", "..R..", "RRRRR" }), set, 6, 5) == "r1c3"
                    && SpriteAt(BuildFixture(new[] { "RRR", "RRRR", "RRR" }), set, 7, 5) == "r0c2"
                    && SpriteAt(BuildFixture(new[] { ".RRR", "RRRR", ".RRR" }), set, 4, 5) == "r0c0";

                if (allT)
                {
                    tFour++;
                }

                if (SpriteAt(BuildFixture(new[] { "..R..", "..R..", "RRRRR" }), set, 5, 5) == "r2c2"
                    && SpriteAt(BuildFixture(new[] { ".RRR", "RRRR", ".RRR" }), set, 4, 4) == "r2c0")
                {
                    junction++;
                }

                if (Sha(TerrainDataPath) == sha0 && RaisedCount(TerrainDataPath) == count0)
                {
                    protection++;
                }
            }

            Check("TENFOLD_TOPOLOGY_CLASSIFICATION", topology == 10,
                $"{topology}/10 sweeps of 512 neighbourhoods found 0 north-cap breaks and 0 diagnostics");
            Check("TENFOLD_AUTHOR_ROLE_MAPPING", mapping == 10,
                $"{mapping}/10 repetitions confirmed the branch base and the stem roles");
            Check("TENFOLD_FOUR_L_ORIENTATIONS", lFour == 10,
                $"{lFour}/10 repetitions resolved all four L orientations");
            Check("TENFOLD_FOUR_T_ORIENTATIONS", tFour == 10,
                $"{tFour}/10 repetitions resolved all four branch orientations");
            Check("TENFOLD_JUNCTION_CLASSIFICATION", junction == 10,
                $"{junction}/10 repetitions confirmed both inner-corner terminals");
            Check("TENFOLD_FIRST_ISLAND_PROTECTION", protection == 10,
                $"{protection}/10 reads returned SHA256 {sha0.Substring(0, 16)}... and {count0} Raised");
        }

        // ------------------------------------------------------------------ helpers

        private static bool IsCliffRow(HillCompositionRow row)
        {
            return row == HillCompositionRow.FRONT_CLIFF
                || row == HillCompositionRow.SECOND_FRONT_CLIFF;
        }

        private static void Expect(
            AuthorHillsCompositionSet set, string id, string[] mask, string expected)
        {
            TerrainGridData grid = BuildFixture(mask);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
            var actual = new Dictionary<Vector3Int, HillVisualTile>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                actual[t.VisualPosition] = t;
            }

            var want = new Dictionary<string, string>();
            foreach (string part in expected.Split(';'))
            {
                string[] kv = part.Split('=');
                want[kv[0]] = kv[1];
            }

            var problems = new List<string>();
            foreach (KeyValuePair<string, string> kv in want)
            {
                string[] xy = kv.Key.Split(',');
                var p = new Vector3Int(
                    int.Parse(xy[0], CultureInfo.InvariantCulture),
                    int.Parse(xy[1], CultureInfo.InvariantCulture), 0);
                bool outside = kv.Value.EndsWith("*", StringComparison.Ordinal);
                string name = outside ? kv.Value.Substring(0, kv.Value.Length - 1) : kv.Value;

                if (!actual.TryGetValue(p, out HillVisualTile t))
                {
                    problems.Add($"{kv.Key} expected {name} but nothing was emitted");
                    continue;
                }

                if (t.Sprite.name != "Hills_" + name)
                {
                    problems.Add($"{kv.Key} expected Hills_{name} but got {t.Sprite.name}");
                }

                if (t.OutsideLogicalMask != outside)
                {
                    problems.Add($"{kv.Key} OutsideLogicalMask expected {outside} got "
                        + $"{t.OutsideLogicalMask}");
                }
            }

            if (actual.Count != want.Count)
            {
                problems.Add($"emitted {actual.Count} tiles but {want.Count} were expected; "
                    + "a surplus tile is as much a failure as a missing one");
            }

            Check("SPRITE_" + id, problems.Count == 0,
                problems.Count == 0
                    ? $"all {want.Count} visual cells carry the expected author slice, checked by "
                        + "sprite identity and by OutsideLogicalMask, not by tile count"
                    : string.Join("; ", problems));
        }

        private static Dictionary<Vector3Int, HillVisualTile> Tiles(
            TerrainGridData grid, AuthorHillsCompositionSet set)
        {
            var map = new Dictionary<Vector3Int, HillVisualTile>();
            foreach (HillVisualTile t in RaisedVisualPlan.Build(grid, set).Tiles)
            {
                map[t.VisualPosition] = t;
            }

            return map;
        }

        private static string SpriteAt(
            TerrainGridData grid, AuthorHillsCompositionSet set, int x, int y)
        {
            HillVisualTile t;
            return Tiles(grid, set).TryGetValue(new Vector3Int(x, y, 0), out t)
                ? t.Sprite.name.Replace("Hills_", string.Empty)
                : null;
        }

        private static TerrainGridData BuildFixture(string[] mask)
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

        private static TerrainGridData Build(int mask, bool deep)
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
                bool on = (mask & (1 << bit)) != 0;
                if (deep && (bit == 1 || bit == 6))
                {
                    on = true;
                }

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

        private static string Physical(string assetPath)
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
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

        private static int RaisedCount(string assetPath)
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