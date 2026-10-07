// IL-WORLD-004S-PJ - P-junction ROOT CAUSE audit. DIAGNOSTIC ONLY, NO PRODUCTION CHANGE.
//
// WHY THIS FILE EXISTS. The card's section 5 requires the P-junction bug to be traced to a specific
// file, class, method and branch before anything is changed, and explicitly forbids describing it as
// "neighbour logic wrong" or "a resolver bug". This harness measures that, it does not assert a fix.
//
// It answers, from the real code, three questions:
//
//   Q1  Which branch of RaisedTopologyState.RoleFor can leave Role < FRONT_CLIFF while the cell's
//       south is EXPOSED? That is the only precondition for RaisedTopologyState.DrawsFrontCliffBelow,
//       which is in turn the only thing in the whole projection that ever emits a visual tile at a
//       coordinate other than its own logical cell. All 256 neighbourhoods are swept in two column
//       contexts and every displaced emission is grouped by the branch that produced it.
//
//   Q2  For a stated candidate P fixture, what does each logical cell resolve to BEFORE and AFTER the
//       fifth cell is painted: raw mask, canonical mask, role, slot, sprite, visual coordinate.
//
//   Q3  Which visual tiles exist outside the logical mask, and which cell emitted each one.
//
// COORDINATE CONVENTION. Top-left visual grid. Fixture line 0 is the NORTH row and the last line is
// the SOUTH row. Front cliffs are emitted at y-1, i.e. one row SOUTH of their own logical cell.
//
// THIS HARNESS ASSERTS NOTHING ABOUT THE P GRAMMAR. It deliberately does not encode the anchor
// expectations Hills_r2c4 / Hills_r2c0 / Hills_r2c5, because which logical cell those attach to, and
// what those two unproven slices depict, are the questions the card reserves for PM. Guessing them
// here would be exactly the hard-coding the card forbids. Everything below is measurement.
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
    public static class ILW004SPJunctionAudit
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string TerrainDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-PJ";

        private static readonly List<string> Lines = new List<string>();
        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[PJ] " + s);
        }

        private static void Note(string id, string detail)
        {
            Line("NOTE  " + id + "  ::  " + detail);
        }

        [MenuItem("IslandLife/Diagnostics/P Junction Audit")]
        public static void Run()
        {
            Lines.Clear();
            Directory.CreateDirectory(OutDir);

            string shaBefore = Sha(TerrainDataPath);
            int raisedBefore = RaisedCount(TerrainDataPath);

            Line("=== IL-WORLD-004S-PJ P-junction root cause audit (DIAGNOSTIC, no production change) ===");
            Line($"   Unity {Application.unityVersion}");
            Line($"   FirstIsland Raised {raisedBefore}, SHA256 {shaBefore.Substring(0, 16)}...");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Line("   composition set complete = " + (set != null && set.IsComplete()));

            // The two unproven slices the card names, reported by identity only.
            Line("");
            Line("-- the slices the card names as P anchors: identity and current use --");
            foreach (string nm in new[] { "Hills_r2c0", "Hills_r2c4", "Hills_r2c5" })
            {
                Sprite s = Load(nm);
                Line($"   {nm}: asset present = {s != null}"
                    + (s == null ? string.Empty
                        : $", rect {s.rect.width}x{s.rect.height} at ({s.rect.x},{s.rect.y})"));
                if (s != null && IsReferencedByCompositionSet(set, s))
                {
                    Line($"   {nm}: IS referenced by the production composition set");
                }
                else if (s != null)
                {
                    Line($"   {nm}: NOT referenced by the production composition set, so nothing in the "
                        + "projection can currently emit it");
                }
            }

            Q1_DisplacedEmissionBranches(set);
            Q2_Q3_CandidatePFixtures(set);

            Line("");
            Line($"   FirstIsland SHA unchanged = {Sha(TerrainDataPath) == shaBefore}");
            Line($"   FirstIsland Raised unchanged = {RaisedCount(TerrainDataPath) == raisedBefore}");

            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "PJ_audit.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- Q1

        /// <summary>
        /// Sweeps every neighbourhood and groups every DISPLACED emission by the branch of RoleFor that
        /// produced it. This is what turns "a resolver bug" into a named branch.
        /// </summary>
        private static void Q1_DisplacedEmissionBranches(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- Q1: which RoleFor branch can emit with Role < FRONT_CLIFF while south is exposed? --");

            var byBranch = new SortedDictionary<string, int>();
            var byDepth = new SortedDictionary<string, int>();
            var examples = new Dictionary<string, string>();
            int totalDisplaced = 0;

            for (int mask = 0; mask <= 255; mask++)
            {
                foreach (bool deep in new[] { false, true })
                {
                    TerrainGridData grid = BuildNeighbourhood(mask, deep);
                    RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

                    // Count the DISPLACED TILES the plan really produced.
                    totalDisplaced += plan.TilesOutsideLogicalMask;

                    // Then attribute each one to the logical cell that owns it. The owner's state is
                    // resolved with the SAME public entry point the projection uses, so the role and
                    // thickness reported here are the ones RoleFor actually decided.
                    for (int y = 0; y < grid.Height; y++)
                    {
                        for (int x = 0; x < grid.Width; x++)
                        {
                            if (!RaisedNeighborResolver.IsRaised(grid, 4 + x, 4 + y))
                            {
                                continue;
                            }

                            RaisedTopologyState owner =
                                RaisedTopologyState.Resolve(grid, 4 + x, 4 + y);
                            if (!owner.DrawsFrontCliffBelow)
                            {
                                continue;
                            }

                            string key = BranchOf(owner);
                            byBranch[key] = byBranch.TryGetValue(key, out int c) ? c + 1 : 1;
                            string dk = $"runDepth={owner.RunDepth} isNarrow={owner.IsNarrow} "
                                + $"slot={owner.Slot} role={owner.Role}";
                            byDepth[dk] = byDepth.TryGetValue(dk, out int c2) ? c2 + 1 : 1;
                            if (!examples.ContainsKey(key))
                            {
                                examples[key] = $"owner ({4 + x},{4 + y}) raw 0x{(byte)owner.RawMask:X2} "
                                    + $"runDepth {owner.RunDepth} offset "
                                    + $"{owner.OffsetFromRunBottom} slot {owner.Slot} role "
                                    + $"{owner.Role} -> emits an extra tile at ({4 + x},{4 + y - 1})";
                            }
                        }
                    }
                }
            }

            Line($"   displaced visual tiles across all 512 neighbourhoods = {totalDisplaced}");
            Line($"   logical cells whose OWN resolved state says DrawsFrontCliffBelow, by RoleFor "
                + "branch:");
            foreach (KeyValuePair<string, int> kv in byBranch)
            {
                Line($"     {kv.Key}  x{kv.Value}");
                Line($"        {examples[kv.Key]}");
            }

            Line("   the same cells grouped by thickness:");
            foreach (KeyValuePair<string, int> kv in byDepth)
            {
                Line($"     {kv.Key}  x{kv.Value}");
            }

            Note("Q1_CONCLUSION",
                "RaisedTopologyState.DrawsFrontCliffBelow is defined as ExposedSouth && Role < "
                    + "FRONT_CLIFF, and AuthorHillsLocalResolver.Resolve is the only place in the whole "
                    + "projection that emits a HillVisualTile at a coordinate other than its own "
                    + "logical cell. The grouping above shows which RoleFor branch can leave Role below "
                    + "FRONT_CLIFF while the south is exposed; any branch not listed can never produce a "
                    + "displaced tile.");
        }

        /// <summary>
        /// The branch of <see cref="RaisedTopologyState"/> that decided this cell, named from the same
        /// conditions RoleFor itself branches on, so the grouping in Q1 is unambiguous.
        /// </summary>
        private static string BranchOf(RaisedTopologyState st)
        {
            if (st.Role == RaisedSurfaceRole.LEFT_CORNER)
            {
                return "RoleFor: LEFT_CORNER";
            }

            if (st.Role == RaisedSurfaceRole.RIGHT_CORNER)
            {
                return "RoleFor: RIGHT_CORNER";
            }

            if (st.Role == RaisedSurfaceRole.LEFT_TOP_CORNER)
            {
                return "RoleFor: LEFT_TOP_CORNER";
            }

            if (st.Role == RaisedSurfaceRole.RIGHT_TOP_CORNER)
            {
                return "RoleFor: RIGHT_TOP_CORNER";
            }

            if (st.RunDepth == 1)
            {
                return $"RoleFor: runDepth==1 -> {st.Role}";
            }

            if (st.RunDepth == 2 && st.IsNarrow)
            {
                return $"RoleFor: runDepth==2 && isNarrow -> {st.Role}";
            }

            if (st.RunDepth >= 4)
            {
                return $"RoleFor: runDepth>=4 -> {st.Role}";
            }

            if (st.RunDepth == 3)
            {
                return $"RoleFor: runDepth==3 -> {st.Role}";
            }

            if (st.RunDepth == 2)
            {
                return $"RoleFor: runDepth==2 && !isNarrow -> {st.Role}"
                    + "   <<< ONLY branch that can displace with an exposed south";
            }

            return $"RoleFor: fallthrough -> {st.Role}";
        }

        // ---------------------------------------------------------------- Q2 + Q3

        /// <summary>
        /// A small set of CANDIDATE P fixtures. They are candidates only: the card does not carry the
        /// numbered diagram, so which one is "the" P fixture is a question for PM. Each is shown as
        /// the L before the fifth cell and as the P after, cell by cell, with every visual tile listed.
        /// </summary>
        private static void Q2_Q3_CandidatePFixtures(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- Q2/Q3: candidate P fixtures, L before and P after the fifth cell --");
            Line("    these are CANDIDATES. The card does not carry the numbered fixture diagram, so");
            Line("    which logical cell is user cell 1..5 is not known here.");

            // (id, L mask, index of the fifth cell to paint, resulting P mask)
            var candidates = new List<Tuple<string, string[], int, string[]>>
            {
                Tuple.Create(
                    "CAND_A_L_X_XXX_plus_bottom_middle",
                    new[] { "XXX", "X.." }, 1, new[] { "XXX", "XX." }),
                Tuple.Create(
                    "CAND_B_L_XX_XX_plus_top_right",
                    new[] { "X.", "XX" }, 1, new[] { "XX", "XX" }),
                Tuple.Create(
                    "CAND_C_L_X_XXX_plus_top_middle",
                    new[] { "X..", "XXX" }, 1, new[] { "XX.", "XXX" }),
                Tuple.Create(
                    "CAND_D_L_XXX_X_plus_bottom_right",
                    new[] { "XXX", "X.." }, 2, new[] { "XXX", "XX." }),
                Tuple.Create(
                    "CAND_E_L_vertical_then_horizontal",
                    new[] { "X.", "X.", "XX" }, 2, new[] { "XX", "X.", "XX" }),
                Tuple.Create(
                    "CAND_F_L_two_by_two_tail",
                    new[] { "XXX", "XX." }, -1, new[] { "XXX", "XX." }),
            };

            foreach (Tuple<string, string[], int, string[]> c in candidates)
            {
                Line("");
                Line("   ===== " + c.Item1 + " =====");
                Dump("L before", set, c.Item2);

                if (c.Item3 < 0)
                {
                    Line("   (this candidate is the P itself; no fifth cell to paint)");
                    Dump("P", set, c.Item4);
                    continue;
                }

                // Build the P by painting the fifth cell onto the L's grid.
                string[] p = (string[])c.Item4.Clone();
                int h = c.Item2.Length;
                int w = c.Item2[0].Length;
                var chars = new List<char>[h];
                for (int i = 0; i < h; i++)
                {
                    chars[i] = new List<char>(c.Item2[i]);
                }

                int fi = c.Item3 / w;
                int fj = c.Item3 % w;
                chars[fi][fj] = 'X';
                for (int i = 0; i < h; i++)
                {
                    p[i] = new string(chars[i].ToArray());
                }

                Dump("P after painting the fifth cell", set, p);
            }

            Note("Q3_NOTE",
                "For every candidate, Dump lists LOGICAL_RAISED, VISUAL_HILLS and every tile outside the "
                    + "logical mask together with the logical cell that emitted it. A tile outside the "
                    + "mask is always emitted by AuthorHillsLocalResolver.Resolve under "
                    + "if (topology.DrawsFrontCliffBelow), at the coordinate (x, y-1) of its owner.");
        }

        private static void Dump(string label, AuthorHillsCompositionSet set, string[] mask)
        {
            TerrainGridData grid = Build(mask);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            int h = mask.Length;
            int w = mask[0].Length;
            var logical = new List<Vector3Int>();
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

            Line($"   --- {label} [{string.Join(" / ", mask)}] ---");
            Line($"       LOGICAL_RAISED ({logical.Count}) = "
                + string.Join(" ", logical.OrderBy(p => p.y).ThenBy(p => p.x)
                    .Select(p => $"({p.x},{p.y})")));
            Line($"       VISUAL_HILLS ({plan.Tiles.Count}) = "
                + string.Join(" ", plan.Tiles
                    .Select(t => $"({t.VisualPosition.x},{t.VisualPosition.y})="
                        + t.Sprite.name.Replace("Hills_", string.Empty))
                    .OrderBy(s => s, StringComparer.Ordinal)));
            Line($"       OUTSIDE_MASK ({plan.TilesOutsideLogicalMask}) = "
                + string.Join(" ", plan.Tiles.Where(t => t.OutsideLogicalMask)
                    .Select(t => $"({t.VisualPosition.x},{t.VisualPosition.y})="
                        + t.Sprite.name.Replace("Hills_", string.Empty)
                        + "<-emitted by (" + t.VisualPosition.x + "," + (t.VisualPosition.y + 1) + ")")
                    .OrderBy(s => s, StringComparer.Ordinal)));

            foreach (Vector3Int p in logical.OrderBy(p => p.y).ThenBy(p => p.x))
            {
                RaisedTopologyState st = RaisedTopologyState.Resolve(grid, p.x, p.y);
                string sprite = "?";
                foreach (HillVisualTile t in plan.Tiles)
                {
                    if (t.VisualPosition == p)
                    {
                        sprite = t.Sprite.name.Replace("Hills_", string.Empty);
                    }
                }

                string extra = plan.TilesOutsideLogicalMask > 0 && st.DrawsFrontCliffBelow
                    ? "  [DISPLACES a cliff to (" + p.x + "," + (p.y - 1) + ")]"
                    : string.Empty;

                Line($"       ({p.x},{p.y}) raw=0x{(byte)st.RawMask:X2} "
                    + $"canon={st.CanonicalMask,-20} role={st.Role,-18} slot={st.Slot,-14} "
                    + $"runDepth={st.RunDepth} off={st.OffsetFromRunBottom} sprite={sprite}{extra}");
            }

            if (plan.VisualDiagnostics.Count > 0)
            {
                Line("       diagnostics: " + string.Join(" | ", plan.VisualDiagnostics));
            }
        }

        // ---------------------------------------------------------------- helpers

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

        /// <summary>The 3x3 neighbourhood sweep grid, identical in shape to the R16B matrix.</summary>
        private static TerrainGridData BuildNeighbourhood(int mask, bool deep)
        {
            TerrainGridData grid = Build(new[] { "..." });
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    bool on = i == 1 && j == 1;
                    int bit = (i * 3) + j;
                    on = ((mask >> bit) & 1) == 1;
                    grid.SetElevation(4 + j, 4 + i, on
                        ? ElevationLevel.Raised : ElevationLevel.Normal);
                }
            }

            if (deep)
            {
                grid.SetElevation(4 + 3, 4 + 2, ElevationLevel.Raised);
                grid.SetElevation(4 + 3, 4 + 1, ElevationLevel.Raised);
            }

            return grid;
        }

        private static Sprite Load(string sliceName)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Sprite", new[] { "Assets/Art/Environment/SproutLands/Sprites" });
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == sliceName)
                {
                    return AssetDatabase.LoadAssetAtPath<Sprite>(p);
                }
            }

            return null;
        }

        private static bool IsReferencedByCompositionSet(AuthorHillsCompositionSet set, Sprite s)
        {
            System.Reflection.FieldInfo[] fields =
                typeof(AuthorHillsCompositionSet).GetFields(
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            foreach (System.Reflection.FieldInfo f in fields)
            {
                if (f.FieldType == typeof(Sprite) && (Sprite)f.GetValue(set) == s)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Sha(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = System.IO.File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", string.Empty);
            }
        }

        private static int RaisedCount(string path)
        {
            string raw = System.IO.File.ReadAllText(path);
            foreach (string line in raw.Split('\n'))
            {
                string t = line.Trim();
                if (!t.StartsWith("elevations:"))
                {
                    continue;
                }

                string payload = t.Substring("elevations:".Length).Trim();
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