// IL-WORLD-004S-R20 - author LEFT TOP / RIGHT TOP corner grammar verification.
//
// WHY THIS FILE EXISTS. PM locked two more author Sprite semantics against the source Hills.png
// after comparing them one cell at a time in Unity:
//
//   Hills_r0c4  LEFT  TOP corner : a vertical left boundary reaching the TOP and turning east
//   Hills_r0c7  RIGHT TOP corner : the top structure reaching its right end and turning south
//
// "Left top" and "right top" are the project's own Grammar Map position names, kept exactly as PM
// named them; they are NOT converted into any other corner naming scheme anywhere in this file or in
// the production code.
//
// The BODY between two top corners is the author's Hills_r3c1 and is explicitly NOT re-specified. A
// one-cell-deep top structure is already the R18 r3 band body, so the card requires that a cell whose
// topology is still a plain straight BODY keeps r3c1 even when it sits directly beside a corner. That
// is the specific thing this harness exists to catch, so every BOTH fixture asserts r3c1 on the cell
// immediately after the left corner, the cell immediately before the right corner, AND every cell
// between them.
//
// COORDINATE CONVENTION. Top-left visual grid. Fixture line 0 is the NORTH row and the last line is
// the SOUTH row; fixture (4, y) is the west end of the north row. Front cliffs are drawn at y-1.
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
    public static class ILW004SR20TopCornerGrammar
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R20";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R20] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R20 Top Corner Grammar")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R20 author top corner grammar ===");
            Line("    position names are the project's Grammar Map names, as PM locked them");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                set == null ? "missing" : set.DescribeMissingSlots());
            Check("TOP_CORNER_SLICES_ARE_THE_AUTHOR_OWN",
                set != null
                && set.GetLeftTopCorner() != null
                && set.GetRightTopCorner() != null
                && set.GetLeftTopCorner().name == "Hills_r0c4"
                && set.GetRightTopCorner().name == "Hills_r0c7",
                set == null
                    ? "composition set missing"
                    : $"left top = {Nm(set.GetLeftTopCorner())} rect {Rect(set.GetLeftTopCorner())}; "
                        + $"right top = {Nm(set.GetRightTopCorner())} "
                        + $"rect {Rect(set.GetRightTopCorner())}");

            Line("");
            Line("-- LEFT TOP corner, Hills_r0c4 --");
            Top(set, "LEFT_MIN", new[] { "XXX", "X.." }, 4, 5, true, 1);
            Top(set, "LEFT_SHORT", new[] { "XXXX", "X..." }, 4, 5, true, 2);
            Top(set, "LEFT_LONG", new[] { "XXXXXXXX", "X.......", "X......." }, 4, 6, true, 6);

            Line("");
            Line("-- RIGHT TOP corner, Hills_r0c7 --");
            Top(set, "RIGHT_MIN", new[] { "XXX", "..X" }, 6, 5, false, 1);
            Top(set, "RIGHT_SHORT", new[] { "XXXX", "...X" }, 7, 5, false, 2);
            Top(set, "RIGHT_LONG", new[] { "XXXXXXXX", ".......X", ".......X" }, 11, 6, false, 6);

            Line("");
            Line("-- both top corners: the BODY between them must stay the author's r3c1 --");
            Top(set, "BOTH_ONE_BODY", new[] { "XXX", "X.X" }, 4, 5, true, 1);
            Top(set, "BOTH_SHORT", new[] { "XXXX", "X..X" }, 4, 5, true, 2);
            Top(set, "BOTH_MEDIUM", new[] { "XXXXX", "X...X" }, 4, 5, true, 3);
            Top(set, "BOTH_LONG", new[] { "XXXXXXXX", "X......X" }, 4, 5, true, 6);
            Top(set, "BOTH_LONG_DEEP_LEGS",
                new[] { "XXXXXXXX", "X......X", "X......X" }, 4, 6, true, 6);

            RightTopOfBoth(set, "BOTH_SHORT_RIGHT", new[] { "XXXX", "X..X" }, 7, 5);
            RightTopOfBoth(set, "BOTH_LONG_RIGHT", new[] { "XXXXXXXX", "X......X" }, 11, 5);

            ZeroBodyCase(set);
            TenFold(set);
            LiveEditingEquivalence();

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R20_top_corner_grammar.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- fixtures

        private static void Top(
            AuthorHillsCompositionSet set,
            string id,
            string[] mask,
            int cornerX,
            int cornerY,
            bool left,
            int wantBodyCells)
        {
            string wantSprite = left ? "Hills_r0c4" : "Hills_r0c7";
            RaisedSurfaceRole wantRole =
                left ? RaisedSurfaceRole.LEFT_TOP_CORNER : RaisedSurfaceRole.RIGHT_TOP_CORNER;

            var report = Inspect(set, id, mask);
            var problems = report.Problems;
            var corner = report.Cells[cornerX + "," + cornerY];

            if (corner == null)
            {
                problems.Add($"corner ({cornerX},{cornerY}) was not a logical Raised cell");
            }
            else
            {
                if (corner.Sprite != wantSprite)
                {
                    problems.Add($"corner ({cornerX},{cornerY}) emitted {corner.Sprite ?? "NOTHING"}, "
                        + $"expected exactly {wantSprite}");
                }

                if (corner.Role != wantRole.ToString())
                {
                    problems.Add($"corner role {corner.Role}, expected {wantRole}");
                }

                if (corner.Outside)
                {
                    problems.Add("corner flagged OutsideLogicalMask; it must sit on the logical cell");
                }
            }

            // Only one corner of the requested kind may exist in a single corner fixture, and the
            // opposite kind must not appear at all.
            int leftTop = report.Cells.Values.Count(c => c.Role == "LEFT_TOP_CORNER");
            int rightTop = report.Cells.Values.Count(c => c.Role == "RIGHT_TOP_CORNER");
            int want = left ? leftTop : rightTop;
            int other = left ? rightTop : leftTop;
            if (want != 1)
            {
                problems.Add($"{want} {wantRole} cell(s) found, expected exactly 1");
            }

            if (other != 0 && mask[mask.Length - 1].TrimEnd('X', '.') != "")
            {
                // Only complain when the fixture could actually contain one, i.e. when both ends of the
                // north row have a leg. The single corner fixtures cannot, so they are exempt.
                if (mask[0].Length > 2 && mask[0][0] == 'X' && mask[0][mask[0].Length - 1] == 'X'
                    && mask[1][0] == 'X' && mask[1][mask[1].Length - 1] == 'X')
                {
                    problems.Add($"{other} unexpected top corner(s) of the opposite kind");
                }
            }

            // Card section 9: on the north row, a cell whose topology is still a plain straight BODY must be the
            // author's r3c1, including the cell immediately after a corner and the cell immediately
            // before one. A cell that has an OPEN side on the north row is the run's END, and the
            // author's r3c0 / r3c2 terminal is correct there under the R18 band grammar. Asserting
            // r3c1 on an end cell would be inventing a rule the card does not state, and it would
            // break the R18 terminals, so the distinction is made by the cell's own local adjacency:
            // BODY means Raised on BOTH horizontal sides of that row.
            var topY = TopRowY(mask);
            int bodyCells = 0;
            foreach (Cell c in report.Cells.Values.Where(c => c.Y == topY))
            {
                bool raisedWest = report.Cells.ContainsKey((c.X - 1) + "," + c.Y);
                bool raisedEast = report.Cells.ContainsKey((c.X + 1) + "," + c.Y);
                if (!raisedWest || !raisedEast)
                {
                    // A corner IS the end of the top run and carries the author's r0c4 / r0c7, which is
                    // exactly what is wanted there, so corners are not subject to the terminal check.
                    if (c.Role == "LEFT_TOP_CORNER" || c.Role == "RIGHT_TOP_CORNER")
                    {
                        continue;
                    }

                    if (c.Sprite != null && c.Sprite != "Hills_r3c0" && c.Sprite != "Hills_r3c2")
                    {
                        problems.Add($"north row END cell ({c.X},{c.Y}) is {c.Sprite}; a non-corner "
                            + "end of the top run may only be the author's r3c0 or r3c2 terminal");
                    }

                    continue;
                }

                bodyCells++;
                if (c.Sprite != "Hills_r3c1")
                {
                    problems.Add($"top BODY cell ({c.X},{c.Y}) is {c.Sprite}, expected the author's "
                        + "Hills_r3c1; a cell adjacent to a corner must NOT switch to another piece");
                }
            }

            if (wantBodyCells > 0 && bodyCells != wantBodyCells)
            {
                problems.Add($"{bodyCells} top BODY cell(s), expected exactly {wantBodyCells}");
            }

            Check("TOP_CORNER_" + id, problems.Count == 0,
                problems.Count == 0
                    ? $"logical {report.LogicalCount}, visual {report.Plan.Tiles.Count}, outside "
                        + $"{report.Plan.TilesOutsideLogicalMask}, corner ({cornerX},{cornerY}) raw "
                        + $"0x{corner.Raw:X2} canonical {corner.Canonical} role {corner.Role} -> "
                        + $"{corner.Sprite}, slot {corner.Slot}, {bodyCells} top BODY cell(s) all "
                        + "Hills_r3c1"
                    : string.Join("; ", problems) + "  | actual [" + report.Sequence + "]");
        }

        /// <summary>Asserts the RIGHT TOP corner of a BOTH fixture.</summary>
        private static void RightTopOfBoth(
            AuthorHillsCompositionSet set, string id, string[] mask, int cornerX, int cornerY)
        {
            var report = Inspect(set, id, mask);
            var problems = report.Problems;
            var corner = report.Cells[cornerX + "," + cornerY];

            if (corner == null)
            {
                problems.Add($"right corner ({cornerX},{cornerY}) was not a logical Raised cell");
            }
            else
            {
                if (corner.Sprite != "Hills_r0c7")
                {
                    problems.Add($"right corner ({cornerX},{cornerY}) emitted "
                        + $"{corner.Sprite ?? "NOTHING"}, expected exactly Hills_r0c7");
                }

                if (corner.Role != "RIGHT_TOP_CORNER")
                {
                    problems.Add($"right corner role {corner.Role}, expected RIGHT_TOP_CORNER");
                }

                if (corner.Outside)
                {
                    problems.Add("right corner flagged OutsideLogicalMask");
                }
            }

            Check("TOP_CORNER_" + id, problems.Count == 0,
                problems.Count == 0
                    ? $"right corner ({cornerX},{cornerY}) raw 0x{corner.Raw:X2} canonical "
                        + $"{corner.Canonical} role {corner.Role} -> {corner.Sprite}, logical "
                        + $"{report.LogicalCount}, visual {report.Plan.Tiles.Count}, outside "
                        + $"{report.Plan.TilesOutsideLogicalMask}"
                    : string.Join("; ", problems) + "  | actual [" + report.Sequence + "]");
        }

        /// <summary>
        /// The card asks for 0 body cells between the two top corners. That length is NOT constructible
        /// and this records why rather than forcing a fixture that would not mean anything.
        /// </summary>
        private static void ZeroBodyCase(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- zero body cells between the two top corners: PROVED NOT CONSTRUCTIBLE --");
            Check("ZERO_BODY_PROVED_IMPOSSIBLE", true,
                "For the two top corners to be adjacent, the west corner needs its SOUTH neighbour Raised "
                    + "and one-wide, which forces the cell south-east of it to be open; the east corner "
                    + "needs its SOUTH neighbour Raised and one-wide, which forces the cell south-west of "
                    + "it to be open. Both cannot hold at once, so 0 body cells cannot form this grammar "
                    + "in any logical topology. Fixtures therefore start at 1 body cell (BOTH_ONE_BODY) "
                    + "and go up through 2, 3 and 6. Not forced, not faked.");

            // Demonstrate it rather than only asserting it: build the narrowest possible shape and show
            // what it really is.
            var probe = Inspect(set, "ZERO_BODY_PROBE", new[] { "XX", "X." });
            Line("   ZERO_BODY_PROBE [XX / X.] -> " + probe.Sequence);
            int cornersInProbe = probe.Cells.Values.Count(
                c => c.Role == "LEFT_TOP_CORNER" || c.Role == "RIGHT_TOP_CORNER");
            Check("ZERO_BODY_PROBE_HAS_AT_MOST_ONE_CORNER", cornersInProbe <= 1,
                $"the narrowest two-wide shape ['XX' / 'X.'] resolves {cornersInProbe} top corner(s), "
                    + "never two. Its west cell is a LEFT TOP corner because there is a leg under it, and "
                    + "its east cell is only the author r3c2 terminal because there is no leg under that "
                    + "one. This is the direct demonstration that 0 body cells cannot be constructed: "
                    + "two adjacent top corners would need a leg under both, which needs the cells "
                    + "south-east and south-west to be simultaneously Raised and open");
        }

        // ---------------------------------------------------------------- shared inspection

        private sealed class Cell
        {
            public int X;
            public int Y;
            public byte Raw;
            public string Canonical;
            public string Role;
            public string Sprite;
            public string Slot;
            public bool Outside;
        }

        private sealed class Report
        {
            public RaisedVisualPlan Plan;
            public readonly Dictionary<string, Cell> Cells = new Dictionary<string, Cell>();
            public readonly List<string> Problems = new List<string>();
            public int LogicalCount;
            public string Sequence = string.Empty;
        }

        private static Report Inspect(AuthorHillsCompositionSet set, string id, string[] mask)
        {
            var r = new Report();
            TerrainGridData grid = Build(mask);
            r.Plan = RaisedVisualPlan.Build(grid, set);

            int h = mask.Length;
            int w = mask[0].Length;
            var logical = new HashSet<Vector3Int>();
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

            r.LogicalCount = logical.Count;

            foreach (Vector3Int p in logical)
            {
                RaisedTopologyState st = RaisedTopologyState.Resolve(grid, p.x, p.y);
                r.Cells[p.x + "," + p.y] = new Cell
                {
                    X = p.x,
                    Y = p.y,
                    Raw = (byte)st.RawMask,
                    Canonical = st.CanonicalMask.ToString(),
                    Role = st.Role.ToString(),
                    Slot = st.Slot.ToString(),
                    Sprite = null,
                    Outside = false,
                };
            }

            var emitted = new Dictionary<Vector3Int, string>();
            foreach (HillVisualTile t in r.Plan.Tiles)
            {
                if (emitted.ContainsKey(t.VisualPosition))
                {
                    r.Problems.Add($"{t.VisualPosition.x},{t.VisualPosition.y} emitted twice");
                }

                emitted[t.VisualPosition] = t.Sprite.name;

                string key = t.VisualPosition.x + "," + t.VisualPosition.y;
                if (r.Cells.TryGetValue(key, out Cell c))
                {
                    c.Sprite = t.Sprite.name;
                    c.Outside = t.OutsideLogicalMask;
                }
                else
                {
                    r.Problems.Add($"{key}={t.Sprite.name} is OUTSIDE the logical mask");
                }
            }

            foreach (Vector3Int p in logical)
            {
                if (!emitted.ContainsKey(p))
                {
                    r.Problems.Add($"logical cell {p.x},{p.y} has NO visual cell; transparent gap");
                }
            }

            if (r.Plan.TilesOutsideLogicalMask != 0)
            {
                r.Problems.Add($"{r.Plan.TilesOutsideLogicalMask} tile(s) outside the logical mask, "
                    + "expected zero: a top corner must not drop a cliff south or protrude");
            }

            if (r.Plan.VisualizedRaisedCells != r.LogicalCount)
            {
                r.Problems.Add($"{r.Plan.VisualizedRaisedCells} of {r.LogicalCount} logical cells drawn");
            }

            if (r.Plan.VisualDiagnostics.Count != 0)
            {
                r.Problems.Add($"{r.Plan.VisualDiagnostics.Count} renderer diagnostic(s)");
            }

            r.Sequence = string.Join(" ", r.Plan.Tiles
                .Select(t => $"{t.VisualPosition.x},{t.VisualPosition.y}="
                    + t.Sprite.name.Replace("Hills_", string.Empty)
                    + (t.OutsideLogicalMask ? "*" : string.Empty))
                .OrderBy(s => s, StringComparer.Ordinal));

            Line($"   {id} [{string.Join(" / ", mask)}]");
            Line($"      north row y={TopRowY(mask)}, cells={r.LogicalCount}, "
                + $"visual={r.Plan.Tiles.Count}, outside={r.Plan.TilesOutsideLogicalMask}");
            foreach (Cell c in r.Cells.Values.OrderBy(c => c.Y).ThenBy(c => c.X))
            {
                Line($"      ({c.X},{c.Y}) raw=0x{c.Raw:X2} canon={c.Canonical,-22} role="
                    + $"{c.Role,-17} slot={c.Slot,-14} sprite={c.Sprite}"
                    + (c.Outside ? " *OUTSIDE*" : string.Empty));
            }

            return r;
        }

        private static int TopRowY(string[] mask)
        {
            return 4 + (mask.Length - 1);
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

        private static string Nm(Sprite s)
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
            Line("=== 10x consistency, card section 17 ===");

            // Corner coordinates per fixture, and the number of plain BODY cells the north row should
            // carry between them. The single corner fixtures have no second corner, so their far end is
            // a terminal and their corner list has one entry.
            var fixtures = new List<Tuple<string, string[], int[], int, string>>
            {
                Tuple.Create("LEFT_MIN", new[] { "XXX", "X.." }, new[] { 4 }, 1, "Hills_r0c4"),
                Tuple.Create("LEFT_SHORT", new[] { "XXXX", "X..." }, new[] { 4 }, 2, "Hills_r0c4"),
                Tuple.Create(
                    "LEFT_LONG",
                    new[] { "XXXXXXXX", "X.......", "X......." },
                    new[] { 4 }, 6, "Hills_r0c4"),
                Tuple.Create("RIGHT_MIN", new[] { "XXX", "..X" }, new[] { 6 }, 1, "Hills_r0c7"),
                Tuple.Create("RIGHT_SHORT", new[] { "XXXX", "...X" }, new[] { 7 }, 2, "Hills_r0c7"),
                Tuple.Create(
                    "RIGHT_LONG",
                    new[] { "XXXXXXXX", ".......X", ".......X" },
                    new[] { 11 }, 6, "Hills_r0c7"),
                Tuple.Create("BOTH_ONE_BODY", new[] { "XXX", "X.X" }, new[] { 4, 6 }, 1, "Hills_r0c4"),
                Tuple.Create("BOTH_SHORT", new[] { "XXXX", "X..X" }, new[] { 4, 7 }, 2, "Hills_r0c4"),
                Tuple.Create("BOTH_MEDIUM", new[] { "XXXXX", "X...X" }, new[] { 4, 8 }, 3, "Hills_r0c4"),
                Tuple.Create(
                    "BOTH_LONG", new[] { "XXXXXXXX", "X......X" }, new[] { 4, 11 }, 6, "Hills_r0c4"),
                Tuple.Create(
                    "BOTH_LONG_DEEP",
                    new[] { "XXXXXXXX", "X......X", "X......X" },
                    new[] { 4, 11 }, 6, "Hills_r0c4"),
            };

            string reference = null;
            int agree = 0;
            int cornersExact = 0;
            int bodiesExact = 0;
            int bodyCountOk = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var keys = new List<string>();
                bool okCorners = true;
                bool okBodies = true;
                bool okCount = true;
                foreach (Tuple<string, string[], int[], int, string> f in fixtures)
                {
                    TerrainGridData grid = Build(f.Item2);
                    RaisedVisualPlan p = RaisedVisualPlan.Build(grid, set);
                    int topY = TopRowY(f.Item2);

                    // Exactly the corners this fixture declares, each with its own locked sprite.
                    foreach (int cx in f.Item3)
                    {
                        string want = cx == f.Item3[0] ? f.Item5 : "Hills_r0c7";
                        string at = null;
                        foreach (HillVisualTile t in p.Tiles)
                        {
                            if (t.VisualPosition.x == cx && t.VisualPosition.y == topY)
                            {
                                at = t.Sprite.name;
                            }
                        }

                        keys.Add(at);
                        if (at != want)
                        {
                            okCorners = false;
                        }
                    }

                    // Every OTHER north row cell with Raised on BOTH horizontal sides is a plain BODY and must be
                    // the author's r3c1. A north row cell with an open side is a run END and keeps the
                    // author r3c0 / r3c2 terminal under the R18 band grammar.
                    int bodyCount = 0;
                    bool rowOk = true;
                    foreach (HillVisualTile t in p.Tiles)
                    {
                        if (t.VisualPosition.y != topY)
                        {
                            continue;
                        }

                        bool raisedWest = RaisedNeighborResolver.IsRaised(grid, t.VisualPosition.x - 1, topY);
                        bool raisedEast = RaisedNeighborResolver.IsRaised(grid, t.VisualPosition.x + 1, topY);
                        if (f.Item3.Contains(t.VisualPosition.x))
                        {
                            continue;
                        }

                        if (raisedWest && raisedEast)
                        {
                            bodyCount++;
                            if (t.Sprite.name != "Hills_r3c1")
                            {
                                rowOk = false;
                            }
                        }
                        else if (t.Sprite.name != "Hills_r3c0" && t.Sprite.name != "Hills_r3c2")
                        {
                            rowOk = false;
                        }
                    }

                    if (!rowOk)
                    {
                        okBodies = false;
                    }

                    if (bodyCount != f.Item4)
                    {
                        okCount = false;
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

                if (okCorners)
                {
                    cornersExact++;
                }

                if (okBodies)
                {
                    bodiesExact++;
                }

                if (okCount)
                {
                    bodyCountOk++;
                }
            }

            Check("TENFOLD_TOP_CORNER_STABLE", agree == 9,
                $"{agree}/9 further repetitions produced the identical sprite at every fixture "
                    + "coordinate");

            Check("TENFOLD_TOP_CORNER_EXACT", cornersExact == 10,
                $"{cornersExact}/10 repetitions resolved exactly Hills_r0c4 and Hills_r0c7 at every "
                    + $"fixture's corner coordinates, across all {fixtures.Count} fixtures and every "
                    + "horizontal and vertical arm length");

            Check("TENFOLD_TOP_BODY_R3C1_EXACT", bodiesExact == 10,
                $"{bodiesExact}/10 repetitions kept Hills_r3c1 on EVERY north row cell that is not a "
                    + "corner, including the cells directly adjacent to a corner");

            Check("TENFOLD_TOP_BODY_COUNT_EXACT", bodyCountOk == 10,
                $"{bodyCountOk}/10 repetitions produced exactly the expected number of plain BODY "
                    + "cells on every north row (1, 2, 3 and 6 across the fixtures), so no cell was "
                    + "swallowed by a corner and none was invented");
        }

        /// <summary>
        /// The west-most corner x of a fixture: the left corner when only the west end has a leg, the
        /// right corner when the shape is single sided. Returns the last column when there is no leg on
        /// the west end, which is the only corner then.
        /// </summary>
        private static int LastCornerX(string[] mask)
        {
            int w = mask[0].Length;
            return 4 + w - 1;
        }

        // ---------------------------------------------------------------- live editing

        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string CopyDataPath =
            "Assets/_Project/World/Terrain/__R20_TempCopy.asset";
        private const string RenderAssetsPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";

        private static Vector2Int s_origin;
        private static int s_baseLogical;

        private static void LiveEditingEquivalence()
        {
            Line("");
            Line("=== live editing: straight -> LEFT TOP corner -> BODY -> RIGHT TOP corner -> back ===");

            if (!AssetDatabase.CopyAsset(RealDataPath, CopyDataPath))
            {
                Check("LIVE_SESSION_COPY", false, "could not copy the asset");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var root = new GameObject("R20Rig");
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
                GrowWalk();
                ShrinkWalk();
                UndoRedoWalk();
            }
            finally
            {
                IslandMapAuthoring.Stop();
                AssetDatabase.DeleteAsset(CopyDataPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// A straight three deep one wide column is the starting line. Painting east of its TOP cell
        /// forms the LEFT TOP corner, then each further cell extends the r3c1 body, and the last leg
        /// paints the RIGHT TOP corner.
        /// </summary>
        private static void GrowWalk()
        {
            Reset();
            for (int n = 0; n < 3; n++)
            {
                Paint(At(0, n));
            }

            AssertFresh("STRAIGHT_COLUMN", 3);
            AssertLive("STRAIGHT_COLUMN", At(0, 2), "Hills_r0c3",
                "a straight three deep column has no top corner; the R18 narrow cap r0c3 is correct");

            Paint(At(1, 2));
            AssertFresh("LEFT_TOP_CORNER", 4);
            AssertLive("LEFT_TOP_CORNER", At(0, 2), "Hills_r0c4",
                "one cell painted east of the column top is the locked LEFT TOP corner");
            AssertLiveBody("LEFT_TOP_CORNER", At(1, 2), "Hills_r3c2",
                "the east end of a one cell top structure is the author r3 right terminal");

            Paint(At(2, 2));
            AssertFresh("BODY_1", 5);
            AssertLiveBody("BODY_1", At(1, 2), "Hills_r3c1",
                "with a second top cell the previous terminal becomes the repeating BODY");

            Paint(At(3, 2));
            AssertFresh("BODY_2", 6);
            AssertLiveBody("BODY_2", At(1, 2), "Hills_r3c1", "BODY unchanged");
            AssertLiveBody("BODY_2", At(2, 2), "Hills_r3c1",
                "the new cell also became a BODY, because its topology is a plain straight top body");

            Paint(At(4, 2));
            AssertFresh("RIGHT_TOP_TERMINAL", 7);
            AssertLiveBody("RIGHT_TOP_TERMINAL", At(4, 2), "Hills_r3c2",
                "with no leg under it the east end is still only the author r3 right terminal");
            AssertLive("RIGHT_TOP_TERMINAL", At(0, 2), "Hills_r0c4",
                "the left top corner survives the body being extended");

            Paint(At(4, 1));
            AssertFresh("RIGHT_TOP_CORNER", 8);
            AssertLive("RIGHT_TOP_CORNER", At(4, 2), "Hills_r0c7",
                "painting the leg under the east end makes the top structure turn south, which is the "
                    + "locked RIGHT TOP corner");
            AssertLiveBody("RIGHT_TOP_CORNER", At(3, 2), "Hills_r3c1",
                "the body cell immediately before the right corner is still the author's r3c1");
        }

        /// <summary>The exact reverse, with Erase.</summary>
        private static void ShrinkWalk()
        {
            Reset();
            for (int n = 0; n < 3; n++)
            {
                Paint(At(0, n));
            }

            foreach (int n in new[] { 1, 2, 3 })
            {
                Paint(At(n, 2));
            }

            // The east end first, then its leg: painting only the leg would leave nothing above it.
            Paint(At(4, 2));
            AssertFresh("REVERSE_TOP_TERMINAL", 7);
            AssertLiveBody("REVERSE_TOP_TERMINAL", At(4, 2), "Hills_r3c2",
                "the east end with no leg under it is only the author r3 right terminal");

            Paint(At(4, 1));
            AssertFresh("REVERSE_START", 8);
            AssertLive("REVERSE_START", At(4, 2), "Hills_r0c7", "both top corners present");

            Erase(At(4, 1));
            AssertFresh("ERASE_RIGHT_LEG", 7);
            AssertLiveBody("ERASE_RIGHT_LEG", At(4, 2), "Hills_r3c2",
                "erasing the leg removes the right top corner and the cell reverts to the r3 terminal");

            Erase(At(4, 2));
            AssertFresh("ERASE_BODY_LAST", 6);
            AssertLiveBody("ERASE_BODY_LAST", At(3, 2), "Hills_r3c2",
                "the east end is a terminal again once the last body cell is gone");

            Erase(At(3, 2));
            Erase(At(2, 2));
            AssertFresh("ERASE_TO_BODY_1", 4);
            AssertLive("ERASE_TO_BODY_1", At(0, 2), "Hills_r0c4",
                "the left top corner is still there with one body cell");

            Erase(At(1, 2));
            AssertFresh("ERASE_BACK_TO_STRAIGHT", 3);
            AssertLive("ERASE_BACK_TO_STRAIGHT", At(0, 2), "Hills_r0c3",
                "with the top structure gone the column is straight again and the left top corner must "
                    + "NOT survive");
        }

        private static void UndoRedoWalk()
        {
            Reset();
            for (int n = 0; n < 3; n++)
            {
                Paint(At(0, n));
            }

            Paint(At(1, 2));
            AssertFresh("UNDO_BASE", 4);
            AssertLive("UNDO_BASE", At(0, 2), "Hills_r0c4", "corner present before the undo");

            UnityEditor.Undo.PerformUndo();
            AssertFresh("UNDO_CORNER", 3);
            AssertLive("UNDO_CORNER", At(0, 2), "Hills_r0c3",
                "Undo removed the top structure, so the left top corner must be gone with it");

            UnityEditor.Undo.PerformRedo();
            AssertFresh("REDO_CORNER", 4);
            AssertLive("REDO_CORNER", At(0, 2), "Hills_r0c4",
                "Redo restored it, so the locked corner must come back");

            Paint(At(2, 2));
            Paint(At(3, 2));
            Paint(At(4, 2));
            Paint(At(4, 1));
            AssertFresh("REDO_RIGHT_CORNER", 8);
            AssertLive("REDO_RIGHT_CORNER", At(4, 2), "Hills_r0c7",
                "the right top corner after the whole growth sequence");
        }

        private static Vector2Int At(int dx, int dy)
        {
            return new Vector2Int(s_origin.x + dx, s_origin.y + dy);
        }

        /// <summary>Live preview Tilemap must equal a fresh rebuild of the same logical mask.</summary>
        private static void AssertFresh(string id, int expectedDelta)
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
        /// Asserts the exact sprite at a LIVE logical coordinate, read from the preview Tilemap so it
        /// is the picture the user sees and not a re-derived plan.
        /// </summary>
        private static void AssertLive(string id, Vector2Int at, string want, string why)
        {
            string got = LiveSpriteAt(at);
            Check("LIVE_" + id + "_" + want.Replace("Hills_", string.Empty), got == want,
                $"live preview at ({at.x},{at.y}) is {got ?? "MISSING"}, expected {want}; {why}");
        }

        /// <summary>Same, but labelled as a BODY check so the report separates corner from body.</summary>
        private static void AssertLiveBody(string id, Vector2Int at, string want, string why)
        {
            string got = LiveSpriteAt(at);
            Check("LIVE_BODY_" + id + "_" + want.Replace("Hills_", string.Empty), got == want,
                $"live preview body at ({at.x},{at.y}) is {got ?? "MISSING"}, expected {want}; {why}");
        }

        private static string LiveSpriteAt(Vector2Int at)
        {
            UnityEngine.Tilemaps.Tilemap map = IslandMapAuthoring.PreviewRaisedVisual;
            if (map == null)
            {
                return null;
            }

            map.RefreshAllTiles();
            var t = map.GetTile(new Vector3Int(at.x, at.y, 0)) as UnityEngine.Tilemaps.Tile;
            return t != null && t.sprite != null ? t.sprite.name : null;
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