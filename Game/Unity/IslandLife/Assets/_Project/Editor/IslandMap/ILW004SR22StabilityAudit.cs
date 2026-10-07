// IL-WORLD-004S-R22 - Raised grammar STABILITY AUDIT. DIAGNOSTIC ONLY. NO PRODUCTION CHANGE.
//
// WHY THIS FILE EXISTS. Card section 1 asks a system question, not a shape question: when the user only
// EXTENDS an existing Raised edge, why do cells that did not gain a neighbour change their author role
// or Sprite? This harness measures exactly that and classifies every observed change, so the answer is
// evidence rather than opinion.
//
// It sweeps lengths 1,2,3,4,5,6,7,8,16,32,64,100 on a plain vertical line and a plain horizontal line,
// then on the arms of a known-correct L, then on the arm of the junction the user reported, then in all
// eight directions. At each step it diffs EVERY previously existing cell and buckets each change as
//
//   EXPECTED_LOCAL_CHANGE   the cell itself gained or lost a direct neighbour, so its local topology
//                           genuinely changed. A former terminal becoming a body is the canonical case
//                           and the card explicitly allows it.
//   UNEXPECTED_REMOTE_CHANGE the cell's own 3x3 occupancy is UNCHANGED and only its column got longer,
//                           yet its role, slot, sprite or visual coordinate moved. This is the
//                           distance-dependent pollution the card is hunting.
//
// NOTHING here asserts a sprite or a role. It only records what changed and why, and it never edits
// production. The card forbids fixing anything this round.
//
// COORDINATE CONVENTION. Top-left visual grid, as everywhere in 004S. Fixture line 0 is the NORTH row
// and the last line is the SOUTH row, so (4,4) is the south-west cell.
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
    public static class ILW004SR22StabilityAudit
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R22";

        private static readonly int[] Lengths = { 1, 2, 3, 4, 5, 6, 7, 8, 16, 32, 64, 100 };

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R22] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R22 Raised Grammar Stability Audit")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R22 Raised grammar stability audit (DIAGNOSTIC, no production change) ===");
            Line("   purpose: find every rule by which merely LENGTHENING an existing edge changes cells");
            Line("   that gained no neighbour. Lengths swept: "
                + string.Join(",", Lengths.Select(l => l.ToString())));

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            if (set == null || !set.IsComplete())
            {
                Line("   composition set unusable: " + (set == null ? "null" : set.DescribeMissingSlots()));
                return;
            }

            SweepVerticalLine(set);
            SweepHorizontalLine(set);
            SweepLArms(set);
            SweepJunctionArm(set);
            SweepEightDirections(set);
            TenFold(set);
            DependencyGraph();

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL (diagnostic observations, not assertions) ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R22_stability_audit.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- cell snapshot

        private sealed class Cell
        {
            public int X;
            public int Y;
            public byte Raw;
            public string Canon;
            public int RunDepth;
            public int Offset;
            public string Role;
            public string Slot;
            public string Sprite;
            public int VisualX;
            public int VisualY;
            public bool Outside;

            public string Key => X + "," + Y;
            public string Sig =>
                Raw.ToString("X2") + "/" + Canon + "/" + RunDepth + "/" + Offset + "/" + Role
                + "/" + Slot + "/" + Sprite + "/" + VisualX + "," + VisualY + "/" + Outside;
            public string MaskSig => Raw.ToString("X2") + "/" + Canon;
        }

        private static Dictionary<string, Cell> Snapshot(AuthorHillsCompositionSet set, string[] mask)
        {
            var d = new Dictionary<string, Cell>();
            TerrainGridData grid = Build(mask);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
            foreach (HillVisualTile t in plan.Tiles)
            {
                RaisedTopologyState st =
                    RaisedTopologyState.Resolve(grid, t.VisualPosition.x, t.VisualPosition.y);
                d[t.VisualPosition.x + "," + t.VisualPosition.y] = new Cell
                {
                    X = t.VisualPosition.x,
                    Y = t.VisualPosition.y,
                    Raw = (byte)st.RawMask,
                    Canon = st.CanonicalMask.ToString(),
                    RunDepth = st.RunDepth,
                    Offset = st.OffsetFromRunBottom,
                    Role = st.Role.ToString(),
                    Slot = t.Slot.ToString(),
                    Sprite = t.Sprite.name,
                    VisualX = t.VisualPosition.x,
                    VisualY = t.VisualPosition.y,
                    Outside = t.OutsideLogicalMask,
                };
            }

            return d;
        }

        /// <summary>
        /// Diffs every cell present in BOTH snapshots and buckets each change. A cell whose 3x3 mask is
        /// unchanged but whose output moved is UNEXPECTED_REMOTE_CHANGE, which is the whole point.
        /// </summary>
        private static void Diff(string label, Dictionary<string, Cell> before,
            Dictionary<string, Cell> after, bool verbose)
        {
            var expected = new List<string>();
            var unexpected = new List<string>();
            var vanished = new List<string>();

            foreach (KeyValuePair<string, Cell> kv in before)
            {
                // The BEFORE cell is the dictionary entry. An earlier version took it from the AFTER
                // dictionary as well, which made every comparison trivially equal and reported zero
                // changes of any kind, including ones the snapshot dump proved had happened.
                Cell b = kv.Value;
                if (!after.TryGetValue(kv.Key, out Cell a))
                {
                    vanished.Add(kv.Key + " disappeared from the visual set");
                    continue;
                }

                if (a.Sig == b.Sig)
                {
                    continue;
                }

                string rec = $"({kv.Key}) mask {b.MaskSig} -> {a.MaskSig} | "
                    + $"runDepth {b.RunDepth}->{a.RunDepth} off {b.Offset}->{a.Offset} | "
                    + $"role {b.Role}->{a.Role} | slot {b.Slot}->{a.Slot} | "
                    + $"sprite {b.Sprite}->{a.Sprite} | visual {b.VisualX},{b.VisualY}->{a.VisualX},{a.VisualY}"
                    + $" | outside {b.Outside}->{a.Outside}";
                if (a.MaskSig == b.MaskSig)
                {
                    unexpected.Add(rec + "   <<< MASK UNCHANGED");
                }
                else
                {
                    expected.Add(rec);
                }
            }

            Line("   " + label);
            Line($"      EXPECTED_LOCAL_CHANGE   = {expected.Count}");
            Line($"      UNEXPECTED_REMOTE_CHANGE = {unexpected.Count}");
            Line($"      disappeared              = {vanished.Count}");
            if (verbose)
            {
                foreach (string r in expected)
                {
                    Line("      + " + r);
                }
            }

            foreach (string r in unexpected)
            {
                Line("      ! " + r);
            }

            foreach (string r in vanished)
            {
                Line("      x " + r);
            }
        }

        // ---------------------------------------------------------------- sweeps

        /// <summary>A plain one wide vertical column of the given height.</summary>
        private static string[] VerticalLine(int n)
        {
            var rows = new List<string>();
            for (int i = 0; i < n; i++)
            {
                rows.Add("X");
            }

            return rows.ToArray();
        }

        private static void SweepVerticalLine(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 1D: plain VERTICAL line, length 1..100 --");
            foreach (int n in new[] { 1, 2, 3, 4 })
            {
                var dbg = Snapshot(set, VerticalLine(n));
                Line("   RAW n=" + n + " cells=" + dbg.Count + " : "
                    + string.Join("  ", dbg.Values.OrderBy(c => c.Y).ThenBy(c => c.X)
                        .Select(c => "(" + c.Key + ")" + c.Sig)));
            }

            Dictionary<string, Cell> prev = null;
            foreach (int n in Lengths)
            {
                var cur = Snapshot(set, VerticalLine(n));
                if (prev != null)
                {
                    Diff("vertical line " + (n - 1) + " -> " + n, prev, cur, n <= 5);
                }

                prev = cur;
            }
        }

        private static string[] HorizontalLine(int n)
        {
            return new[] { new string('X', n) };
        }

        private static void SweepHorizontalLine(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 1D: plain HORIZONTAL line, length 1..100 --");
            Dictionary<string, Cell> prev = null;
            foreach (int n in Lengths)
            {
                var cur = Snapshot(set, HorizontalLine(n));
                if (prev != null)
                {
                    Diff("horizontal line " + (n - 1) + " -> " + n, prev, cur, n <= 5);
                }

                prev = cur;
            }
        }

        /// <summary>
        /// A known-correct L, [XX / X.], with one arm lengthened while the other is pinned. The vertical
        /// arm is the west column; the horizontal arm is the south row. Both arms are swept.
        /// </summary>
        private static string[] LArm(int vertical, int horizontal)
        {
            var rows = new List<string>();
            for (int i = 1; i < vertical; i++)
            {
                rows.Add("X.");
            }

            rows.Add(new string('X', horizontal));
            return rows.ToArray();
        }

        private static void SweepLArms(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- L ARMS: lengthen the vertical arm, horizontal pinned at 2 --");
            Dictionary<string, Cell> pv = null;
            foreach (int n in Lengths)
            {
                var cur = Snapshot(set, LArm(n, 2));
                if (pv != null)
                {
                    Diff("L vertical arm -> " + n, pv, cur, n <= 5);
                }

                pv = cur;
            }

            Line("");
            Line("-- L ARMS: lengthen the horizontal arm, vertical pinned at 2 --");
            Dictionary<string, Cell> ph = null;
            foreach (int n in Lengths)
            {
                var cur = Snapshot(set, LArm(2, n));
                if (ph != null)
                {
                    Diff("L horizontal arm -> " + n, ph, cur, n <= 5);
                }

                ph = cur;
            }
        }

        /// <summary>
        /// The junction the user reported, [XXX / XX.], with its vertical boundary extended upward, i.e.
        /// the exact R21 fixture. This sweep deliberately records WHY the junction output moves rather
        /// than asserting which slice is right.
        /// </summary>
        private static string[] JunctionArm(int height)
        {
            var rows = new List<string>();
            for (int i = 2; i < height; i++)
            {
                rows.Insert(0, "X..");
            }

            rows.Add("XXX");
            rows.Add("XX.");
            return rows.ToArray();
        }

        private static void SweepJunctionArm(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- JUNCTION arm: [XXX / XX.] with the west vertical boundary extended 1..100 --");
            Dictionary<string, Cell> prev = null;
            foreach (int n in Lengths)
            {
                var cur = Snapshot(set, JunctionArm(n));
                if (prev != null)
                {
                    Diff("junction arm -> " + n, prev, cur, n <= 5);
                }

                prev = cur;
            }
        }

        // ---------------------------------------------------------------- 2D extension

        private static readonly string[] Dirs =
            { "N", "S", "E", "W", "NE", "NW", "SE", "SW" };

        private static (int dx, int dy) Delta(string d)
        {
            switch (d)
            {
                case "N": return (0, 1);
                case "S": return (0, -1);
                case "E": return (1, 0);
                case "W": return (-1, 0);
                case "NE": return (1, 1);
                case "NW": return (-1, 1);
                case "SE": return (1, -1);
                default: return (-1, -1);
            }
        }

        /// <summary>
        /// Fixes a correct corner/junction at (4,4) inside a small block and then extends a single edge
        /// FAR away in one direction, starting two cells away so the added cell never touches the target.
        /// This is the hidden-coupling probe: if the target's sprite moves, remote length reaches local
        /// output.
        /// </summary>
        private static void SweepEightDirections(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 2D: remote extension in each direction, never touching the target (4,4) --");

            foreach (string dir in Dirs)
            {
                (int dx, int dy) = Delta(dir);
                Dictionary<string, Cell> before = Snapshot(set, RemoteFixture(dir, 0));
                int changed = 0;
                foreach (int dist in new[] { 1, 2, 3, 4, 8, 16, 32, 64 })
                {
                    var after = Snapshot(set, RemoteFixture(dir, dist));
                    Cell b;
                    Cell a = null;
                    before.TryGetValue("4,4", out b);
                    after.TryGetValue("4,4", out a);
                    if (b != null && a != null && a.Sig != b.Sig)
                    {
                        changed++;
                        Line($"   target (4,4) CHANGED when extending {dir} at distance {dist}: "
                            + $"mask {b.MaskSig}->{a.MaskSig} runDepth {b.RunDepth}->{a.RunDepth} "
                            + $"role {b.Role}->{a.Role} sprite {b.Sprite}->{a.Sprite} "
                            + $"visual {b.VisualX},{b.VisualY}->{a.VisualX},{a.VisualY}"
                            + (b.MaskSig == a.MaskSig ? "   <<< MASK UNCHANGED" : string.Empty));
                    }
                }

                if (changed == 0)
                {
                    Line($"   direction {dir}: target (4,4) output stable across every remote distance "
                        + "1,2,3,4,8,16,32,64");
                }
            }
        }

        /// <summary>
        /// A 5x5 block whose top-left corner is the target, plus a run of Raised cells placed `dist`
        /// steps away in direction `dir`, never adjacent to the block.
        /// </summary>
        private static string[] RemoteFixture(string dir, int dist)
        {
            const int size = 5;
            var cells = new HashSet<Vector2Int>();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    cells.Add(new Vector2Int(j, i));
                }
            }

            (int dx, int dy) = Delta(dir);
            // Start clear of the block so the probe cells are never direct neighbours of it.
            int x = dx == 0 ? size / 2 : (dx > 0 ? size + dist : -1 - dist);
            int y = dy == 0 ? size / 2 : (dy > 0 ? size + dist : -1 - dist);
            for (int i = 0; i < 6; i++)
            {
                cells.Add(new Vector2Int(x + (dx == 0 ? i : 0), y + (dy == 0 ? i : 0)));
            }

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (Vector2Int c in cells)
            {
                minX = Math.Min(minX, c.x);
                maxX = Math.Max(maxX, c.x);
                minY = Math.Min(minY, c.y);
                maxY = Math.Max(maxY, c.y);
            }

            var rows = new char[maxY - minY + 1][];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = new char[maxX - minX + 1];
                for (int j = 0; j < rows[i].Length; j++)
                {
                    rows[i][j] = cells.Contains(new Vector2Int(minX + j, minY + i)) ? 'X' : '.';
                }
            }

            // line 0 is NORTH
            var lines = new List<string>();
            for (int i = rows.Length - 1; i >= 0; i--)
            {
                lines.Add(new string(rows[i]));
            }

            return lines.ToArray();
        }

        // ---------------------------------------------------------------- ten fold

        private static void TenFold(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 10x consistency of the whole audit --");
            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var sig = new List<string>();
                foreach (int n in Lengths)
                {
                    var v = Snapshot(set, VerticalLine(n));
                    var h = Snapshot(set, HorizontalLine(n));
                    var j = Snapshot(set, JunctionArm(n));
                    sig.Add("V" + n + ":" + string.Join(",", v.Values
                        .OrderBy(c => c.Y).ThenBy(c => c.X).Select(c => c.Sig)));
                    sig.Add("H" + n + ":" + string.Join(",", h.Values
                        .OrderBy(c => c.Y).ThenBy(c => c.X).Select(c => c.Sig)));
                    sig.Add("J" + n + ":" + string.Join(",", j.Values
                        .OrderBy(c => c.Y).ThenBy(c => c.X).Select(c => c.Sig)));
                }

                string joined = string.Join(";", sig);
                if (reference == null)
                {
                    reference = joined;
                }
                else if (joined == reference)
                {
                    agree++;
                }
            }

            Check("TENFOLD_AUDIT_STABLE", agree == 9,
                agree + "/9 further repetitions produced a byte-identical dependency signature "
                    + "across every fixture and every length");
        }

        // ---------------------------------------------------------------- dependency graph

        private static void DependencyGraph()
        {
            Line("");
            Line("-- RAISED_GRAMMAR_DEPENDENCY_GRAPH, with the REMOTE_COUPLING edges marked --");
            Line("   Paint");
            Line("    -> logical Raised mask (TerrainMapData)");
            Line("    -> RaisedNeighborResolver.ResolveRaisedMask            raw 3x3");
            Line("    -> TerrainMaskNormalizer.Normalize                    canonical 3x3");
            Line("    -> RaisedNeighborResolver.MeasureRunDepth             WHOLE COLUMN, unbounded   <<< REMOTE_COUPLING");
            Line("    -> RaisedNeighborResolver.MeasureRunBottom            WHOLE COLUMN, unbounded   <<< REMOTE_COUPLING");
            Line("       -> offsetFromRunBottom = y - runBottom");
            Line("    -> RaisedTopologyState.IsEnclosedVoid                 4 cardinal                LOCAL");
            Line("    -> SlotFor(solidWest, solidEast)                                            LOCAL");
            Line("    -> IsAuthorLeftCorner / IsAuthorRightCorner          3x3 + north isOneWide     LOCAL");
            Line("    -> IsAuthorLeftTopCorner / IsAuthorRightTopCorner    3x3 + south isOneWide     LOCAL");
            Line("    -> IsAuthorCornerJunction                           3x3 + SE + 2 steps east  <<< REGION_SIZE_DEPENDENT");
            Line("    -> FrontContinuesInMask  (MaxFrontWalk = 2)          walks up to 2 cells        <<< REMOTE_COUPLING");
            Line("       -> ThickNeighbourEndsTheRow -> NeighbourCarriesWallInMask");
            Line("          -> MeasureRunDepth(neighbour) >= 3           NEIGHBOUR COLUMN LENGTH    <<< REMOTE_COUPLING");
            Line("    -> RoleFor(..., runDepth, offset, frontContinues)    THE JOIN                   <<< REMOTE_COUPLING");
            Line("    -> Role");
            Line("    -> CliffSlotFor / RunContinuesThrough               row walk                   LOCAL-ish");
            Line("    -> AuthorHillsLocalResolver.PickSprite (role, slot)");
            Line("    -> Sprite + HillVisualTile");
            Line("    -> DrawsFrontCliffBelow -> extra tile at (x, y-1)                              <<< REMOTE_COUPLING");
            Line("");
            Line("   REMOTE_COUPLING edges, each able to change a cell that gained NO neighbour:");
            Line("     1. MeasureRunDepth   : whole column length into RoleFor");
            Line("     2. MeasureRunBottom  : whole column bottom into offsetFromRunBottom");
            Line("     3. MaxFrontWalk      : a 2-cell radius decides whether a wall moves");
            Line("     4. NeighbourCarriesWallInMask : the NEIGHBOUR's column length >= 3");
            Line("     5. IsAuthorCornerJunction     : a 2-step-east probe makes it REGION dependent");
            Line("     6. DrawsFrontCliffBelow        : threshold crossing moves a VISUAL COORDINATE");
        }

        // ---------------------------------------------------------------- helpers

        private static TerrainGridData Build(string[] mask)
        {
            int h = mask.Length;
            int w = 0;
            foreach (string r in mask)
            {
                if (r.Length > w)
                {
                    w = r.Length;
                }
            }

            var grid = new TerrainGridData(w + 12, h + 12, 6, 6);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetTerrain(6 + x, 6 + y, TerrainType.Grass);
                }
            }

            for (int i = 0; i < h; i++)
            {
                int y = 6 + (h - 1 - i);
                for (int x = 0; x < w; x++)
                {
                    if (x < mask[i].Length && mask[i][x] == 'X')
                    {
                        grid.SetElevation(6 + x, y, ElevationLevel.Raised);
                    }
                }
            }

            return grid;
        }
    }
}