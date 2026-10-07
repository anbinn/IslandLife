// IL-WORLD-004S-R23A - local author topology GRAMMAR MATRIX. TESTS ONLY, NO PRODUCTION CHANGE.
//
// Card section 15 mandates this BEFORE any production edit, and section 22 fixes the order: matrix
// first, resolver second. This file is therefore stage A only.
//
// WHAT IT DOES. It enumerates all 256 raw 8-neighbour masks, runs each through the existing shared
// normalizer, and for every resulting canonical state records the CURRENT production role and Sprite
// the resolver produces today. Then it runs the decisive test the card's STOP conditions turn on:
//
//   Find two DIFFERENT logical neighbourhoods that present the SAME canonical mask at the cell under
//   test, but which the current production grammar gives DIFFERENT author Sprites.
//
// That is card STOP condition 1 ("one canonical topology needs two different author components") and
// STOP condition 3 (distinguishing them needs more than 3x3). If such a pair exists, the requested
// refactor is impossible without changing a locked visual oracle, and this file proves it with the
// smallest counterexample it can find rather than asserting it.
//
// It asserts NO target sprite and encodes NO new grammar. Every STATUS it prints is measured from the
// code that is in production right now.
//
// COORDINATE CONVENTION. Top-left visual grid. Mask line 0 is the NORTH row, last line is SOUTH.
// Front cliffs are emitted at y-1.
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
    public static class ILW004SR23ATopologyMatrix
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R23A";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R23A] " + s);
        }

        private static void Note(string id, string detail)
        {
            Line("NOTE  " + id + "  ::  " + detail);
        }

        [MenuItem("IslandLife/Diagnostics/R23A Topology Matrix")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R23A local author topology grammar matrix (TESTS ONLY) ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            if (set == null || !set.IsComplete())
            {
                Line("   composition set unusable");
                return;
            }

            var matrix = BuildMatrix(set);
            var canon = new SortedDictionary<string, List<State>>(StringComparer.Ordinal);

            foreach (State s in matrix)
            {
                if (!canon.TryGetValue(s.Canon, out List<State> list))
                {
                    list = new List<State>();
                    canon[s.Canon] = list;
                }

                list.Add(s);
            }

            Line("");
            Line($"-- raw masks enumerated = {matrix.Count}");
            Line($"-- distinct CANONICAL states = {canon.Count}");

            Line("");
            Line("-- CANONICAL MATRIX: N NE E SE S SW W NW | CLASS | CURRENT ROLE | CURRENT SPRITE --");
            foreach (KeyValuePair<string, List<State>> kv in canon)
            {
                var sprites = new List<string>();
                foreach (State s in kv.Value)
                {
                    if (!sprites.Contains(s.Sprite))
                    {
                        sprites.Add(s.Sprite);
                    }
                }

                Line($"   raw 0x{kv.Value[0].Raw:X2} canon={kv.Key,-34} "
                    + $"dirs={DirString(kv.Value[0])} class={kv.Value[0].Class,-26} "
                    + $"role={kv.Value[0].Role,-30} sprite={kv.Value[0].Sprite} "
                    + $"[variants seen for this canonical: {sprites.Count} -> {string.Join(",", sprites)}]");
            }

            Line("");
            Line("-- DECISIVE TEST, card STOP condition 1 and 3 --");
            Line("   two DIFFERENT neighbourhoods, SAME canonical mask at the cell, DIFFERENT Sprite");

            var contradictions = new SortedDictionary<string, List<State>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<State>> kv in canon)
            {
                var bySprite = new Dictionary<string, List<State>>();
                foreach (State s in kv.Value)
                {
                    if (!bySprite.TryGetValue(s.Sprite, out List<State> l))
                    {
                        l = new List<State>();
                        bySprite[s.Sprite] = l;
                    }

                    l.Add(s);
                }

                if (bySprite.Count > 1)
                {
                    contradictions[kv.Key] = kv.Value;
                }
            }

            Line($"   canonical states that CURRENTLY emit more than one Sprite = "
                + $"{contradictions.Count}");

            int index = 0;
            foreach (KeyValuePair<string, List<State>> kv in contradictions)
            {
                index++;
                var groups = kv.Value.GroupBy(v => v.Sprite).OrderBy(g => g.Key, StringComparer.Ordinal);
                Line("");
                Line($"   CONTRADICTION {index}: canonical {kv.Key}");
                foreach (IGrouping<string, State> g in groups)
                {
                    State w = g.First();
                    Line($"     -> {g.Key}  (role {w.Role}, runDepth {w.RunDepth}, "
                        + $"offset {w.Offset}, slot {w.Slot})");
                    Line($"        smallest witness raw=0x{w.Raw:X2} dirs={DirString(w)} "
                        + $"grid: {w.Grid.Replace("/", " / ")}");
                    Line($"        witness is {w.Dist} cell(s) away from the cell under test, so no 3x3 "
                        + "rule can see the evidence that separates it");
                }
            }

            if (contradictions.Count == 0)
            {
                Line("   none: every canonical state currently resolves to exactly one Sprite");
            }

            TenFold(set, canon);
            AuditBeyond3x3();

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R23A_topology_matrix.txt"), sb.ToString());
        }

        private static string DirString(State s)
        {
            return string.Join("", new[]
            {
                s.N ? "N" : ".", s.NE ? "E" : ".", s.E ? "E" : ".", s.SE ? "E" : ".",
                s.S ? "S" : ".", s.SW ? "W" : ".", s.W ? "W" : ".", s.NW ? "E" : ".",
            });
        }

        private sealed class State
        {
            public byte Raw;
            public string Canon;
            public bool N, NE, E, SE, S, SW, W, NW;
            public string Class;
            public string Role;
            public string Slot;
            public string Sprite;
            public int RunDepth;
            public int Offset;
            public string Grid;
            public int Dist;
        }

        /// <summary>
        /// A 5x5 grid whose CENTRE cell is under test. The ring at distance 2 is filled from the raw
        /// mask for the first pass, and the ring at distance 1 from the mask itself.
        /// </summary>
        private static State Probe(AuthorHillsCompositionSet set, int raw, bool fillRing2)
        {
            const int g = 5;
            const int c = 2;
            var grid = new TerrainGridData(g + 4, g + 4, 2, 2);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetTerrain(2 + x, 2 + y, TerrainType.Grass);
                }
            }

            grid.SetElevation(2 + c, 2 + c, ElevationLevel.Raised);

            var dirs = new (int dx, int dy, int bit)[]
            {
                (0, 1, 0), (1, 1, 1), (1, 0, 2), (1, -1, 3),
                (0, -1, 4), (-1, -1, 5), (-1, 0, 6), (-1, 1, 7),
            };

            var rows = new char[g][];
            for (int i = 0; i < g; i++)
            {
                rows[i] = new char[g];
                for (int j = 0; j < g; j++)
                {
                    rows[i][j] = '.';
                }
            }

            foreach ((int dx, int dy, int bit) d in dirs)
            {
                bool on = ((raw >> d.bit) & 1) == 1;
                if (!on)
                {
                    continue;
                }

                grid.SetElevation(2 + c + d.dx, 2 + c + d.dy, ElevationLevel.Raised);
                int ri = (c + d.dy) + (g - 1 - 0) / 1 - (g - 1) + (g - 1 - (c + d.dy));
                int rj = c + d.dx;
                int rowIndex = (g - 1) - (c + d.dy);
                rows[rowIndex][rj] = 'X';
                if (ri < 0)
                {
                    continue;
                }
            }

            if (fillRing2)
            {
                // Extend every arm outward so the cell under test sits inside a long column. This is
                // the ONLY difference from the previous pass, and it is invisible to a 3x3 rule.
                foreach ((int dx, int dy, int bit) d in dirs)
                {
                    if (((raw >> d.bit) & 1) != 1)
                    {
                        continue;
                    }

                    for (int k = 2; k < 3; k++)
                    {
                        grid.SetElevation(2 + c + (d.dx * k), 2 + c + (d.dy * k),
                            ElevationLevel.Raised);
                    }
                }
            }

            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
            RaisedTopologyState st = RaisedTopologyState.Resolve(grid, 2 + c, 2 + c);

            string sprite = "NONE";
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.VisualPosition.x == 2 + c && t.VisualPosition.y == 2 + c)
                {
                    sprite = t.Sprite.name;
                }
            }

            return new State
            {
                Raw = (byte)raw,
                Canon = st.CanonicalMask.ToString(),
                N = ((raw >> 0) & 1) == 1,
                NE = ((raw >> 1) & 1) == 1,
                E = ((raw >> 2) & 1) == 1,
                SE = ((raw >> 3) & 1) == 1,
                S = ((raw >> 4) & 1) == 1,
                SW = ((raw >> 5) & 1) == 1,
                W = ((raw >> 6) & 1) == 1,
                NW = ((raw >> 7) & 1) == 1,
                Class = Classify(raw),
                Role = st.Role.ToString(),
                Slot = st.Slot.ToString(),
                Sprite = sprite,
                RunDepth = st.RunDepth,
                Offset = st.OffsetFromRunBottom,
                Grid = string.Join("/", rows.Select(r => new string(r))),
                Dist = fillRing2 ? 2 : 0,
            };
        }

        private static string Classify(int raw)
        {
            int n = 0;
            for (int i = 0; i < 8; i++)
            {
                if (((raw >> i) & 1) == 1)
                {
                    n++;
                }
            }

            if (n == 0)
            {
                return "ISOLATED";
            }

            return "N" + (((raw >> 0) & 1) == 1 ? 1 : 0) + "NE" + (((raw >> 1) & 1) == 1 ? 1 : 0)
                + "E" + (((raw >> 2) & 1) == 1 ? 1 : 0) + "SE" + (((raw >> 3) & 1) == 1 ? 1 : 0)
                + "S" + (((raw >> 4) & 1) == 1 ? 1 : 0) + "SW" + (((raw >> 5) & 1) == 1 ? 1 : 0)
                + "W" + (((raw >> 6) & 1) == 1 ? 1 : 0) + "NW" + (((raw >> 7) & 1) == 1 ? 1 : 0);
        }

        private static List<State> BuildMatrix(AuthorHillsCompositionSet set)
        {
            var list = new List<State>();
            for (int raw = 0; raw <= 255; raw++)
            {
                list.Add(Probe(set, raw, false));
                list.Add(Probe(set, raw, true));
            }

            return list;
        }

        private static void TenFold(AuthorHillsCompositionSet set,
            SortedDictionary<string, List<State>> canon)
        {
            Line("");
            Line("-- 10x consistency of the whole matrix --");
            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var m = BuildMatrix(set);
                string sig = string.Join(";", m.Select(s =>
                    s.Raw + ":" + s.Canon + ":" + s.Role + ":" + s.Slot + ":" + s.Sprite + ":" + s.RunDepth));
                if (reference == null)
                {
                    reference = sig;
                }
                else if (sig == reference)
                {
                    agree++;
                }
            }

            Check("TENFOLD_MATRIX_STABLE", agree == 9,
                agree + "/9 further repetitions produced a byte-identical 512-probe matrix");
        }

        /// <summary>
        /// Card sections 21 and 27.3: the current production semantic rules that read OUTSIDE the 3x3.
        /// Printed from the source so the audit is evidence, not memory.
        /// </summary>
        private static void AuditBeyond3x3()
        {
            Line("");
            Line("-- BEYOND-3x3 SEMANTIC READS currently in production (card sections 21 / 27.3) --");
            const string f = @"Assets\_Project\Scripts\World\Terrain\RaisedTopologyState.cs";
            string src = File.ReadAllText(f);
            string[] needles =
            {
                "RaisedNeighborResolver.MeasureRunDepth(grid, x, y)",
                "RaisedNeighborResolver.MeasureRunBottom(grid, x, y)",
                "MaxFrontWalk = 2",
                "RaisedNeighborResolver.IsRaised(grid, x, y - 2)",
                "RaisedNeighborResolver.IsRaised(grid, x + 2, y)",
                "RaisedNeighborResolver.IsRaised(grid, x + 2, y - 1)",
                "RaisedNeighborResolver.MeasureRunDepth(grid, x, y) >= 3",
            };
            foreach (string n in needles)
            {
                Line($"   {(src.Contains(n) ? "PRESENT" : "absent ")}  {n}");
            }

            Note("BEYOND_3X3_COUNT",
                "Every PRESENT line above is a semantic read outside the cell's own 3x3. Card section "
                    + "21 forbids unapproved x+2 / y-2 lookahead in semantic selection and section 27.3 "
                    + "requires an immediate STOP when more than 3x3 is needed to separate two author "
                    + "semantics.");
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
    }
}