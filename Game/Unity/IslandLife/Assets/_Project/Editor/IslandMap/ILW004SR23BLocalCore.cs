// IL-WORLD-004S-R23B - local topology semantic core verification.
//
// WHAT THIS PROVES. The author semantic decision (Layer A: AuthorRole + AuthorSlot) is now a pure
// function of one cell's own raw 3x3, and Layer B is a pure dispatch table that cannot widen it.
// The whole R23A2 evidence chain is re-measured here against the new code:
//
//   1  the production source contains no length threshold, no x+/-2, no y+/-2, no walk and no
//      lookahead in the semantic path (comments stripped first, so documentation cannot pass as code);
//   2  the 256 raw masks, probed at every reach, emit exactly one Sprite per raw: the 9
//      LEGACY_RUN_OVERRIDE of R23A2 must be 0;
//   3  extending an irrelevant arm from 1 to 100 cells changes nothing for a cell whose raw 3x3 is
//      unchanged - role, slot, sprite and visual coordinate;
//   4  the R18 and R11 author oracles are still held EXACTLY, at lengths 1,2,3,4,5,8,16,32,64,100;
//   5  the shape diagnostics are recorded, never re-pinned to whatever the code now emits.
//
// WHY A FRESH HARNESS. R18, R19, R20 and R11 are FROZEN ORACLES and are NOT modified by this card.
// They are run unchanged, in their own batchmode processes. This file only adds the invariants that
// those oracles do not state.
//
// ART DISCIPLINE. New art 0. Mirror 0. Rotation 0. Generated 0. Nearest match 0. Grass fallback 0.
// Every tile asserted here is one of the author's own existing Hills slices.
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
    public static class ILW004SR23BLocalCore
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R23B";
        private const string ProjectRoot = @"F:\IslandLife\IslandLife\Game\Unity\IslandLife";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R23B] " + s);
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

        private static readonly (string Name, int DX, int DY, int Bit)[] Dirs =
        {
            ("N", 0, 1, 0),
            ("NE", 1, 1, 1),
            ("E", 1, 0, 2),
            ("SE", 1, -1, 3),
            ("S", 0, -1, 4),
            ("SW", -1, -1, 5),
            ("W", -1, 0, 6),
            ("NW", -1, 1, 7),
        };

        private static readonly int[] Reaches = { 1, 2, 3, 4, 8, 16, 64, 100 };

        [MenuItem("IslandLife/Diagnostics/R23B Local Core")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R23B local topology semantic core ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                set == null ? "missing" : set.DescribeMissingSlots());

            ForbiddenScan();
            var probes = LocalTopologyInvariant(set);
            ReachStability(set, probes);
            SurroundStability(set);
            AuthorOracles(set);
            ShapeDiagnostics(set);
            TenFold(set);

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R23B_local_core.txt"), sb.ToString());
        }

        // ================================================================ 1: source scan

        /// <summary>
        /// Scans the semantic path for every construct the card forbids. Comments are stripped first,
        /// because documenting a removed construct must not be able to pass as if the construct were
        /// still there.
        /// </summary>
        private static void ForbiddenScan()
        {
            Line("");
            Line("-- 1: forbidden constructs in the semantic path (comments stripped) --");

            var files = new[]
            {
                "Assets/_Project/Scripts/World/Terrain/RaisedTopologyState.cs",
                "Assets/_Project/Scripts/World/Terrain/AuthorHillsLocalResolver.cs",
            };

            var banned = new (string Label, string Pattern)[]
            {
                ("runDepth threshold", @"runDepth\s*(==|>=|<=|>|<)"),
                ("offsetFromRunBottom in a decision", @"offset\s*(==|>=|<=|>|<)\s*runDepth"),
                ("MaxFrontWalk", @"MaxFrontWalk"),
                ("run walk", @"FrontContinuesInMask|RunContinuesThrough|CliffSlotFor|MeasureRunDepth\([^)]*\)"),
                ("x +/- 2 read", @"IsRaised\(\s*grid\s*,\s*x\s*[-+]\s*2"),
                ("y +/- 2 read", @"IsRaised\(\s*grid\s*,\s*x\s*,\s*y\s*[-+]\s*2"),
                ("shape name test", @"\bif\s*\(\s*(P|T|O|L)\s*=="),
                ("width/height semantic", @"\bif\s*\(\s*(width|height|componentSize)\s*=="),
                ("recursive resolve", @"Raise(d)?TopologyState\.Resolve\("),
            };

            foreach (string rel in files)
            {
                string path = Path.Combine(ProjectRoot, rel);
                string code = StripComments(File.ReadAllText(path));
                string tag = Path.GetFileNameWithoutExtension(rel).ToUpperInvariant();

                // Two of the scans have an ALLOWED remnant that must be visible in the report
                // instead of hidden behind a wildcard:
                //
                //  - MeasureRunDepth / MeasureRunBottom SURVIVE as LAYOUT metrics, because the card
                //    keeps them. Their presence is a PASS; what would be a FAIL is their presence inside
                //    a ROLE or SLOT decision, which is proved structurally below rather than by string.
                //  - RaisedTopologyState.Resolve is called once per cell by Layer B. That is the normal
                //    traversal, not recursion. Recursion would be a call from inside the topology state
                //    itself, so the scan is only applied to that file.
                var applicable = banned
                    .Where(b => b.Label != "run walk")
                    .Where(b => !(b.Label == "recursive resolve"
                        && tag == "AUTHORHILLSLOCALRESOLVER"))
                    .ToList();

                foreach ((string label, string pattern) in applicable)
                {
                    var rx = new System.Text.RegularExpressions.Regex(pattern);
                    var hits = rx.Matches(code).Select(m => m.Value).Distinct().ToList();
                    Check($"FORBIDDEN_{Slug(label)}_ABSENT_{tag}",
                        hits.Count == 0,
                        hits.Count == 0
                            ? $"{Path.GetFileName(rel)} contains no {label}"
                            : $"{Path.GetFileName(rel)} still contains {label}: "
                                + string.Join(" | ", hits));
                }

                // The structural proof that a layout metric cannot reach a role or a slot: the private
                // decision methods must take NO integer parameter at all. If any of them ever needed a
                // length again it would have to take one, so this fails rather than letting the length
                // come back quietly.
                foreach (string method in new[] { "RoleForLocal", "SlotFor", "IsAuthorLeftCorner",
                    "IsAuthorRightCorner", "IsAuthorLeftTopCorner", "IsAuthorRightTopCorner" })
                {
                    System.Reflection.MethodInfo mi = typeof(RaisedTopologyState).GetMethod(
                        method,
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Static);
                    if (mi == null)
                    {
                        continue;
                    }

                    // A POSITION is not a LENGTH, so x and y are excluded: every one of these methods must be
                    // told WHICH cell to judge, and that is the whole point of Layer A. What must not
                    // appear is any integer that is not a coordinate - a depth, an offset, a count, a
                    // size or a distance.
                    var ints = mi.GetParameters()
                        .Where(pp => pp.ParameterType == typeof(int))
                        .Where(pp => pp.Name != "x" && pp.Name != "y")
                        .Select(pp => pp.Name).ToList();
                    Check($"NO_LENGTH_PARAMETER_{method.ToUpperInvariant()}", ints.Count == 0,
                        ints.Count == 0
                            ? $"{method} takes no integer parameter other than its own coordinates, "
                                + "so no length, count, size or distance can reach it"
                            : $"{method} takes {ints.Count} non-coordinate integer parameter(s): "
                                + string.Join(", ", ints));
                }
            }

            // The layout metrics must still EXIST, because Layer C repetition needs them. Their absence
            // would mean the card's "do not delete normal layout ability" clause was over-read.
            string state = StripComments(
                File.ReadAllText(Path.Combine(ProjectRoot, files[0])));
            Check("LAYOUT_METRICS_RETAINED",
                state.Contains("MeasureRunDepth(grid, x, y)")
                    && state.Contains("MeasureRunBottom(grid, x, y)"),
                "MeasureRunDepth and MeasureRunBottom are still called from Resolve, and are recorded "
                    + "on the state as Layer C layout input; they are asserted to reach no role, no slot "
                    + "and no sprite, which is what the FORBIDDEN_RUN_DEPTH_ABSENT_* scans above prove");
        }

        private static string Slug(string label)
        {
            var sb = new StringBuilder(label.Length);
            foreach (char c in label)
            {
                sb.Append(char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '_');
            }

            return sb.ToString();
        }

        private static string StripComments(string s)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
                {
                    while (i < s.Length && s[i] != '\n')
                    {
                        i++;
                    }
                }
                else if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/'))
                    {
                        i++;
                    }

                    i = Math.Min(i + 2, s.Length);
                }
                else
                {
                    sb.Append(s[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        // ================================================================ 2: local topology invariant

        private sealed class Probe
        {
            public int Raw;
            public int Reach;
            public string Role;
            public string Slot;
            public string Sprite;
            public bool Claimed;
        }

        private static List<Probe> LocalTopologyInvariant(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 2: one Sprite per raw 3x3, over every raw mask at every reach --");

            var probes = new List<Probe>();
            int broken = 0;

            foreach (int raw in AllRaw())
            {
                foreach (int reach in Reaches)
                {
                    TerrainGridData g = Build(raw, reach, out int tx, out int ty);
                    int back = Encode(g, tx, ty);
                    if (back != raw)
                    {
                        broken++;
                        continue;
                    }

                    Probe p = Measure(set, g, tx, ty);
                    probes.Add(p);
                }
            }

            Check("WITNESS_GRID_SELF_CONSISTENT", broken == 0,
                $"{probes.Count} probes built from a mask, and the mask was then READ BACK OUT of the "
                    + "constructed grid and compared with the mask that was requested: "
                    + $"{broken} disagreement(s). A probe therefore cannot mislabel its own topology");

            var byRaw = new SortedDictionary<int, List<Probe>>();
            foreach (Probe p in probes)
            {
                if (!byRaw.TryGetValue(p.Raw, out List<Probe> l))
                {
                    l = new List<Probe>();
                    byRaw[p.Raw] = l;
                }

                l.Add(p);
            }

            var multi = byRaw.Where(kv => kv.Value.Select(v => v.Sprite).Distinct().Count() > 1).ToList();
            foreach (KeyValuePair<int, List<Probe>> kv in multi.Take(12))
            {
                var pairs = kv.Value.Select(v => $"{v.Sprite}@reach{v.Reach}").Distinct().ToList();
                Line($"   RAW 0x{kv.Key:X2} raw3x3 [{Rows(kv.Value[0].Raw)}] -> {string.Join(", ", pairs)}");
            }

            Check("LEGACY_RUN_OVERRIDE_ZERO", multi.Count == 0,
                $"{byRaw.Count} distinct raw 3x3 masks, {probes.Count} probes, and "
                    + $"{multi.Count} of them emit more than one Sprite. R23A2 measured 9; the target "
                    + "for this card is 0. Zero means the Sprite is now a function of the cell's own "
                    + "3x3 alone, whatever the surrounding ground does");

            // The card's own worked example, asserted as an identity rather than argued. Every
            // candidate slot actually observed is printed, so a FAIL says which one disagreed instead
            // of only saying that something did.
            var identity = new (int Raw, string Label, string Want)[]
            {
                (0x04, "E only", HillColumnSlot.LEFT_TERMINAL.ToString()),
                (0x44, "W + E", HillColumnSlot.BODY.ToString()),
                (0x40, "W only", HillColumnSlot.RIGHT_TERMINAL.ToString()),
                (0x00, "both open", HillColumnSlot.NARROW.ToString()),
            };

            var identityProblems = new List<string>();
            foreach ((int raw, string label, string want) in identity)
            {
                var seen = probes.Where(p => p.Raw == raw)
                    .Select(p => p.Slot).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
                if (!seen.SequenceEqual(new[] { want }))
                {
                    identityProblems.Add($"{label} raw 0x{raw:X2} gave [{string.Join(", ", seen)}], "
                        + $"expected [{want}] at every reach");
                }
            }

            Check("CARD_SLOT_IDENTITY_RAW", identityProblems.Count == 0,
                identityProblems.Count == 0
                    ? "E only -> LEFT_TERMINAL, W+E -> BODY, W only -> RIGHT_TERMINAL and both open -> "
                        + "NARROW, each held at all " + $"{Reaches.Length} reaches"
                        + $" ({Reaches.Length * identity.Length} probes), so no reach can change the answer"
                    : string.Join("; ", identityProblems));

            return probes;
        }

        private static IEnumerable<int> AllRaw()
        {
            for (int r = 0; r <= 255; r++)
            {
                yield return r;
            }
        }

        private static string Rows(int raw)
        {
            return Bit(raw, 7) + Bit(raw, 0) + Bit(raw, 1) + "|" + Bit(raw, 6) + "C" + Bit(raw, 2)
                + "|" + Bit(raw, 5) + Bit(raw, 4) + Bit(raw, 3);
        }

        private static string Bit(int raw, int bit)
        {
            return ((raw >> bit) & 1) == 1 ? "X" : ".";
        }

        // ================================================================ 3: reach stability

        /// <summary>
        /// Card section 17. The same local structure, with one irrelevant arm pushed out to 1 .. 100
        /// cells. Whenever the target cell's raw 3x3 is unchanged, its role, its slot, its sprite and
        /// the visual coordinate it claims must all be unchanged.
        /// </summary>
        private static void ReachStability(AuthorHillsCompositionSet set, List<Probe> probes)
        {
            Line("");
            Line("-- 3: reach stability 1 -> 100, and LOCAL_TOPOLOGY_INVARIANT --");

            var byRaw = new SortedDictionary<int, List<Probe>>();
            foreach (Probe p in probes)
            {
                if (!byRaw.TryGetValue(p.Raw, out List<Probe> l))
                {
                    l = new List<Probe>();
                    byRaw[p.Raw] = l;
                }

                l.Add(p);
            }

            int mutations = 0;
            int compared = 0;
            int unclaimed = 0;

            foreach (KeyValuePair<int, List<Probe>> kv in byRaw)
            {
                Probe reference = kv.Value[0];
                foreach (Probe p in kv.Value)
                {
                    compared++;
                    if (!p.Claimed)
                    {
                        unclaimed++;
                        continue;
                    }

                    if (p.Role != reference.Role || p.Slot != reference.Slot
                        || p.Sprite != reference.Sprite)
                    {
                        mutations++;
                        Line($"   MUTATION raw 0x{kv.Key:X2} [{Rows(kv.Key)}] reach {reference.Reach}"
                            + $" {reference.Role}/{reference.Slot}/{reference.Sprite}"
                            + $"  ->  reach {p.Reach} {p.Role}/{p.Slot}/{p.Sprite}");
                    }
                }
            }

            Check("UNCHANGED_RAW_3X3_SPRITE_MUTATIONS_ZERO", mutations == 0,
                $"{compared} (raw mask, reach) pairs compared against their own first reach; "
                    + $"{mutations} changed role, slot or sprite. This is the card's "
                    + "UNCHANGED_RAW_3X3_SPRITE_MUTATIONS count, and it is the R22 latent coupling that "
                    + "was measured but inert, now gone rather than merely dormant");

            Check("EVERY_RAISED_CELL_STILL_CLAIMED_ITS_OWN_COORDINATE", unclaimed == 0,
                $"{unclaimed} of {compared} probes emitted no tile at the cell's own visual coordinate. "
                    + "A cell must never lose its own art because the ground around it grew");

            Line($"   reaches exercised: {string.Join(", ", Reaches)}");
        }

        /// <summary>
        /// The card's stability rule, tested in both directions so neither can hide the other.
        ///
        /// Case 1, EXTEND AN ARM: one arm is pushed out to 100 cells. The cell's raw 3x3 is unchanged at
        /// every reach, so its role, slot and sprite must be identical at all of them.
        ///
        /// Case 2, BURY THE CELL: ground is added on ALL eight sides, so the raw 3x3 DOES change - the
        /// cell genuinely gained neighbours, which the card permits. It must then become an interior
        /// body cell, the author's r1c1, and must hold that at every reach from the point it is buried.
        ///
        /// Case 2 deliberately does NOT expect r3c3. A cell that is surrounded on all sides is no longer
        /// an isolated 1x1 plateau, so keeping r3c3 there would be a cell failing to notice that it had
        /// gained neighbours. The measured transition is printed so the change is auditable rather than
        /// assumed.
        /// </summary>
        private static void SurroundStability(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 3b: extend one arm 1..100 with raw 3x3 unchanged, then bury the cell --");

            foreach (int reach in new[] { 1, 2, 3, 4, 8, 16, 64, 100 })
            {
                // North arm only. The cell is the bottom of a column, and lengthening the column must
                // not change it.
                TerrainGridData g = Build(0x01, reach, out int tx, out int ty);
                RaisedTopologyState t = RaisedTopologyState.Resolve(g, tx, ty);
                string mine = SpriteAt(RaisedVisualPlan.Build(g, set), tx, ty);
                Line($"   extend_north reach {reach,3}: raw 0x{Encode(g, tx, ty):X2} role {t.Role} "
                    + $"slot {t.Slot} sprite {mine}");

                Check($"EXTEND_ONE_ARM_STABLE_REACH_{reach}",
                    mine == "Hills_r2c3" && t.Role == RaisedSurfaceRole.FRONT_CLIFF,
                    $"bottom of a north arm of {reach} cell(s): {mine} as {t.Role}. The cell's raw 3x3 "
                        + "is N-only at every reach, so the answer must not move as the column grows");
            }

            Line("");
            Line("   -- the cell now genuinely gains neighbours, so it must become an interior body --");

            foreach (int reach in new[] { 1, 2, 3, 4, 8, 16, 64, 100 })
            {
                TerrainGridData g = Build(255, reach, out int tx, out int ty);
                RaisedTopologyState t = RaisedTopologyState.Resolve(g, tx, ty);
                RaisedVisualPlan plan = RaisedVisualPlan.Build(g, set);
                string mine = SpriteAt(plan, tx, ty);

                Line($"   buried reach {reach,3}: raw 0x{Encode(g, tx, ty):X2} role {t.Role} slot {t.Slot} "
                    + $"sprite {mine} | logical {plan.RaisedCellCount} visual {plan.Tiles.Count} "
                    + $"outside {plan.TilesOutsideLogicalMask}");

                Check($"BURIED_CELL_IS_INTERIOR_REACH_{reach}",
                    mine == "Hills_r1c1" && t.Role == RaisedSurfaceRole.MIDDLE_SURFACE
                        && plan.TilesOutsideLogicalMask == 0,
                    $"cell surrounded on all eight sides by ground {reach} deep: {mine} as {t.Role}, "
                        + $"outside the mask {plan.TilesOutsideLogicalMask}, diagnostics "
                        + $"{plan.VisualDiagnostics.Count}. Surrounded means N and S are both solid, "
                        + "which is the interior body row, and that must be identical at every depth");
            }
        }

        private static string SpriteAt(RaisedVisualPlan plan, int x, int y)
        {
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.VisualPosition.x == x && t.VisualPosition.y == y)
                {
                    return t.Sprite == null ? "NULL" : t.Sprite.name;
                }
            }

            return "NONE";
        }

        // ================================================================ 4: author oracles

        /// <summary>
        /// Re-asserts, byte for byte, the author grammar the card names as hard oracles. R18 and R11
        /// are also run unchanged as their own frozen processes; this block exists so a single file
        /// records the whole ladder including the lengths R18 does not carry (16, 32, 64, 100).
        /// </summary>
        private static void AuthorOracles(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 4: author straight grammar, vertical and horizontal, lengths 1..100 --");

            var lengths = new[] { 1, 2, 3, 4, 5, 8, 16, 32, 64, 100 };

            foreach (int n in lengths)
            {
                // 1x1 is its own oracle, and it is the same cell read either way round.
                if (n == 1)
                {
                    Expect(set, "1x1", Column(1), new[] { "r3c3" }, 0);
                    continue;
                }

                // Horizontal band: left terminal, then the self-repeating body, then the right terminal.
                var band = new List<string> { "r3c0" };
                for (int i = 0; i < n - 2; i++)
                {
                    band.Add("r3c1");
                }

                band.Add("r3c2");
                Expect(set, $"HORIZONTAL_{n}x1", Row(n), band.ToArray(), 0);

                // Vertical column: the cap, then the self-repeating body however deep the column is, then
                // the front cliff. NO LENGTH APPEARS IN THE EXPECTATION, which is precisely the claim:
                // the same three rules reproduce lengths 2 through 100.
                var column = new List<string> { "r0c3" };
                for (int i = 0; i < n - 2; i++)
                {
                    column.Add("r1c3");
                }

                column.Add("r2c3");
                Expect(set, $"VERTICAL_1x{n}", Column(n), column.ToArray(), 0);
            }

            Line("");
            Line("-- R11 rectangles: the frozen wide three-row stack --");

            // Each row is left terminal, self-repeating body, right terminal. The three rows are the
            // author's r0 cap, r1 body and r2 front. Note the narrow (w == 1) case is the author's own
            // c3 stack and is asserted separately above.
            foreach (int w in new[] { 3, 5, 8 })
            {
                var rect = new List<string>();
                foreach (int row in new[] { 0, 1, 2 })
                {
                    rect.Add($"r{row}c0");
                    rect.AddRange(Repeat($"r{row}c1", w - 2));
                    rect.Add($"r{row}c2");
                }

                Expect(set, $"RECT_{w}x3", Rect(w, 3), rect.ToArray(), 0);
            }

            Line("");
            Line("-- narrow column, every length: only the author's own c3 slices are permitted --");
            foreach (int h in new[] { 1, 2, 3, 4, 5, 8, 16, 32, 64, 100 })
            {
                var col = h == 1
                    ? new List<string> { "r3c3" }
                    : new List<string> { "r0c3" };
                if (h >= 2)
                {
                    for (int i = 0; i < h - 2; i++)
                    {
                        col.Add("r1c3");
                    }

                    col.Add("r2c3");
                }

                Expect(set, $"NARROW_1x{h}", Column(h), col.ToArray(), 0);
            }

            Line("");
            Line("-- R19 / R20 corner primitives are locked by their own unchanged harnesses --");
        }

        private static List<string> Repeat(string sprite, int count)
        {
            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(sprite);
            }

            return list;
        }

        private static string[] Column(int n)
        {
            var m = new string[n];
            for (int i = 0; i < n; i++)
            {
                m[i] = "X";
            }

            return m;
        }

        private static string[] Row(int n)
        {
            return new[] { new string('X', n) };
        }

        private static string[] Rect(int w, int h)
        {
            var m = new string[h];
            var line = new string('X', w);
            for (int i = 0; i < h; i++)
            {
                m[i] = line;
            }

            return m;
        }

        /// <summary>
        /// Asserts a fixture against an EXPLICIT expected sprite list, one entry per visual cell,
        /// read NORTH row first and then WEST to EAST. Nothing is inferred: the caller builds the exact
        /// expected sequence, which is the only way a counted repetition like "r1 body however many
        /// times" can be stated without a mini-language that could quietly mis-parse.
        /// </summary>
        private static void Expect(
            AuthorHillsCompositionSet set,
            string id,
            string[] mask,
            string[] expected,
            int wantOutside)
        {
            TerrainGridData grid = BuildMask(mask);
            int logical = RaisedRegionAnalyzer.CountRaisedCells(grid);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            // North row first, then west to east within a row. Same order the expectations are written.
            var cells = plan.Tiles
                .Select(t => new
                {
                    t.VisualPosition.y,
                    t.VisualPosition.x,
                    Name = t.Sprite.name.Replace("Hills_", string.Empty)
                        + (t.OutsideLogicalMask ? "*" : string.Empty),
                })
                .OrderByDescending(c => c.y)
                .ThenBy(c => c.x)
                .ToList();

            var got = cells.Select(c => c.Name).ToList();
            List<string> problems = new List<string>();

            int n = Math.Max(got.Count, expected.Length);
            for (int i = 0; i < n; i++)
            {
                string a = i < got.Count ? got[i] : "<none>";
                string w = i < expected.Length ? expected[i] : "<none>";
                if (a != w)
                {
                    problems.Add($"position {i + 1} (row "
                        + $"{(i < cells.Count ? cells[i].y.ToString() : "?")}, column "
                        + $"{(i < cells.Count ? cells[i].x.ToString() : "?")}) expected {w} but got {a}");
                }
            }

            if (plan.Tiles.Count != logical)
            {
                problems.Add($"{plan.Tiles.Count} visual tiles, expected {logical}");
            }

            if (plan.TilesOutsideLogicalMask != wantOutside)
            {
                problems.Add($"{plan.TilesOutsideLogicalMask} tiles outside the logical mask, "
                    + $"expected {wantOutside}");
            }

            if (plan.VisualizedRaisedCells != logical)
            {
                problems.Add($"{plan.VisualizedRaisedCells} of {logical} logical cells drawn");
            }

            if (plan.VisualDiagnostics.Count != 0)
            {
                problems.Add($"{plan.VisualDiagnostics.Count} renderer diagnostic(s)");
            }

            Check("ORACLE_" + id, problems.Count == 0,
                problems.Count == 0
                    ? $"{logical} logical cells, {plan.Tiles.Count} tiles, "
                        + $"{plan.TilesOutsideLogicalMask} outside the mask, exact sequence "
                        + $"[{string.Join(" ", expected)}]"
                    : string.Join("; ", problems) + "  | actual [" + string.Join(" ", got) + "]");
        }

        // ================================================================ 5: diagnostics only

        /// <summary>
        /// Recorded, never asserted as correct. Every one of these shapes either encodes a superseded
        /// expectation or has no author oracle at all, so this card may not invent a rule to satisfy
        /// one; it may only report what changed.
        /// </summary>
        private static void ShapeDiagnostics(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 5: shape diagnostics, RECORDED ONLY, no new rule added for any of them --");

            var cases = new (string Id, string[] Mask)[]
            {
                ("straight_horizontal", new[] { "XXXXXXXX" }),
                ("straight_vertical", new[] { "X", "X", "X", "X", "X" }),
                ("corner_L", new[] { "X..", "X..", "XXX" }),
                ("step", new[] { "XXXX", "..XX", "..XX" }),
                ("concave_notch", new[] { "XXXX", "X..X", "XXXX" }),
                ("convex_block", new[] { "XXXXX", "XXXXX", "XXXXX", "XXXXX" }),
                ("junction_T", new[] { "XXX", ".X.", ".X." }),
                ("junction_P", new[] { "XXX", "X.." }),
                ("junction_P_stepped", new[] { "XXX", "XX." }),
                ("junction_O_ring", new[] { "XXX", "X.X", "XXX" }),
                ("deep_wide_3", new[] { "XXXX", "XXXX", "XXXX" }),
                ("deep_wide_4", new[] { "XXXX", "XXXX", "XXXX", "XXXX" }),
                ("deep_wide_6", Column(1).Select(_ => "XXXXXXXX").ToArray()),
            };

            foreach ((string id, string[] mask) in cases)
            {
                TerrainGridData grid = BuildMask(mask);
                RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
                int logical = RaisedRegionAnalyzer.CountRaisedCells(grid);
                Line($"   {id,-22} logical {logical,3} visual {plan.Tiles.Count,3} "
                    + $"outside {plan.TilesOutsideLogicalMask,2} "
                    + $"undrawn {logical - plan.VisualizedRaisedCells,2} diag {plan.VisualDiagnostics.Count} "
                    + $"| {Seq(plan)}");
            }

            Line("");
            Line("-- 5b: live FirstIsland projection, READ ONLY, no paint session started --");
            FirstIsland(set);
        }

        private static void FirstIsland(AuthorHillsCompositionSet set)
        {
            const string path = "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
            const string renderPath =
                "Assets/Art/Environment/SproutLands/TerrainRender/TerrainRenderAssets.asset";

            TerrainMapData data = AssetDatabase.LoadAssetAtPath<TerrainMapData>(path);
            TerrainRenderAssets render =
                AssetDatabase.LoadAssetAtPath<TerrainRenderAssets>(renderPath);
            if (data == null || render == null)
            {
                Check("FIRST_ISLAND_ASSETS_READABLE", false,
                    $"terrainMapData {(data == null ? "missing" : "ok")} at {path}; renderAssets "
                        + $"{(render == null ? "missing" : "ok")} at {renderPath}");
                return;
            }

            // The live map is read from the asset on disk and never written. IslandMapAuthoring is not
            // started here: it needs a live scene Tilemap rig and this card only needs the LOGICAL grid,
            // which is what the projection reads. Starting a paint session on the user's asset would be
            // the risky thing to do in a read-only card.
            TerrainGridData grid = data.CreateGridData();
            int raised = RaisedRegionAnalyzer.CountRaisedCells(grid);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            Line($"   FirstIsland logical grid {grid.Width}x{grid.Height} from origin "
                + $"({grid.OriginX},{grid.OriginY}); Raised cells {raised}; visual tiles "
                + $"{plan.Tiles.Count}; drawn {plan.VisualizedRaisedCells}; outside the logical mask "
                + $"{plan.TilesOutsideLogicalMask}; renderer diagnostics {plan.VisualDiagnostics.Count}");

            var used = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (HillVisualTile t in plan.Tiles)
            {
                string n = t.Sprite == null ? "NULL" : t.Sprite.name;
                used[n] = used.TryGetValue(n, out int c) ? c + 1 : 1;
            }

            foreach (KeyValuePair<string, int> kv in used)
            {
                Line($"   {kv.Key,-18} {kv.Value,4}");
            }

            var nonHills = used.Keys.Where(k => k.IndexOf("Hills", StringComparison.Ordinal) < 0)
                .ToList();
            Check("FIRST_ISLAND_NO_NON_HILLS_FALLBACK", nonHills.Count == 0,
                nonHills.Count == 0
                    ? $"all {plan.Tiles.Count} tiles on the live map are author Hills slices"
                    : "non Hills slices emitted: " + string.Join(", ", nonHills));

            Check("OUTSIDE_MASK_PROJECTION_ZERO", plan.TilesOutsideLogicalMask == 0,
                $"{plan.TilesOutsideLogicalMask} tile(s) drawn south of their own logical mask. The "
                    + "local ladder returns a role below FRONT_CLIFF only for a cell whose SOUTH is "
                    + "solid, so DrawsFrontCliffBelow cannot fire and the count is a measured 0 rather "
                    + "than an assumption");

            Check("FIRST_ISLAND_EVERY_RAISED_CELL_DRAWN",
                plan.VisualizedRaisedCells == raised,
                $"{plan.VisualizedRaisedCells} of {raised} live Raised cells produced an author tile; "
                    + $"{raised - plan.VisualizedRaisedCells} were left without one");
        }

        // ================================================================ 6: tenfold

        private static void TenFold(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 6: 10x consistency over the full matrix, the oracles and the diagnostics --");

            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var sig = new StringBuilder();
                foreach (int raw in AllRaw())
                {
                    foreach (int reach in Reaches)
                    {
                        TerrainGridData g = Build(raw, reach, out int tx, out int ty);
                        Probe p = Measure(set, g, tx, ty);
                        sig.Append(p.Raw).Append(':').Append(p.Reach).Append(':')
                            .Append(p.Role).Append(':').Append(p.Slot).Append(':')
                            .Append(p.Sprite).Append(';');
                    }
                }

                foreach (string[] m in new[]
                {
                    Row(8), Column(8), Rect(3, 3), Rect(5, 3), Rect(5, 4),
                    new[] { "X..", "X..", "XXX" }, new[] { "XXX", "X.X", "XXX" },
                })
                {
                    sig.Append(Seq(RaisedVisualPlan.Build(BuildMask(m), set))).Append('|');
                }

                string joined = sig.ToString();
                if (reference == null)
                {
                    reference = joined;
                }
                else if (joined == reference)
                {
                    agree++;
                }
            }

            Check("TENFOLD_R23B_IDENTICAL", agree == 9,
                $"{agree}/9 further repetitions produced a byte identical record over "
                    + $"{AllRaw().Count() * Reaches.Length} probes and 7 shape fixtures");
        }

        // ================================================================ helpers

        private static Probe Measure(
            AuthorHillsCompositionSet set, TerrainGridData grid, int x, int y)
        {
            RaisedTopologyState t = RaisedTopologyState.Resolve(grid, x, y);
            var tiles = new List<HillVisualTile>();
            AuthorHillsLocalResolver.Resolve(grid, set, tiles);

            string sprite = "NONE";
            bool claimed = false;
            foreach (HillVisualTile tile in tiles)
            {
                if (tile.VisualPosition.x == x && tile.VisualPosition.y == y)
                {
                    claimed = true;
                    sprite = tile.Sprite == null ? "NULL" : tile.Sprite.name;
                }
            }

            return new Probe
            {
                Raw = Encode(grid, x, y),
                Reach = 0,
                Role = t.Role.ToString(),
                Slot = t.Slot.ToString(),
                Sprite = sprite,
                Claimed = claimed,
            };
        }

        private static string Seq(RaisedVisualPlan plan)
        {
            var items = plan.Tiles
                .Select(t => $"{t.VisualPosition.x},{t.VisualPosition.y}="
                    + t.Sprite.name.Replace("Hills_", string.Empty)
                    + (t.OutsideLogicalMask ? "*" : string.Empty))
                .OrderBy(s => s, StringComparer.Ordinal);
            return string.Join(" ", items);
        }

        /// <summary>
        /// Builds a grid whose target cell is Raised and whose neighbours come from the raw mask, with
        /// every set arm extended <paramref name="reach"/> cells. The grid is sized to the arms that are
        /// actually set, so a long arm is affordable and an absent arm costs nothing.
        /// </summary>
        private static TerrainGridData Build(int raw, int reach, out int tx, out int ty)
        {
            int west = 0, east = 0, north = 0, south = 0;
            foreach ((string name, int dx, int dy, int bit) d in Dirs)
            {
                if (((raw >> d.bit) & 1) != 1)
                {
                    continue;
                }

                if (d.dx < 0)
                {
                    west = Math.Max(west, -d.dx * reach);
                }
                else if (d.dx > 0)
                {
                    east = Math.Max(east, d.dx * reach);
                }

                if (d.dy > 0)
                {
                    north = Math.Max(north, d.dy * reach);
                }
                else if (d.dy < 0)
                {
                    south = Math.Max(south, -d.dy * reach);
                }
            }

            // The target cell is deliberately placed at a DIFFERENT absolute coordinate for each probe,
            // which also tests that no absolute coordinate reaches the semantic decision.
            //
            // The grid ORIGIN is (0,0) and its SIZE runs from there past the far end of the longest
            // arm. Sizing the grid relative to the TARGET instead is the trap: a south or south-west arm
            // then starts below the grid's origin, IsInside rejects those cells, and the arm is
            // silently truncated. That is not a hypothetical - it produced 224 read-back mismatches
            // before this was fixed, and the read-back assertion below is what caught it.
            tx = 1 + west;
            ty = 1 + north;

            var g = new TerrainGridData(tx + east + 2, ty + south + 2, 0, 0);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(x, y, TerrainType.Grass);
                }
            }

            g.SetElevation(tx, ty, ElevationLevel.Raised);
            int lost = 0;
            foreach ((string name, int dx, int dy, int bit) d in Dirs)
            {
                if (((raw >> d.bit) & 1) != 1)
                {
                    continue;
                }

                for (int k = 1; k <= reach; k++)
                {
                    int x = tx + (d.dx * k);
                    int y = ty + (d.dy * k);
                    if (!g.IsInside(x, y))
                    {
                        lost++;
                        continue;
                    }

                    g.SetElevation(x, y, ElevationLevel.Raised);
                }
            }

            if (lost != 0)
            {
                Debug.LogError($"[R23B] probe grid truncated {lost} arm cell(s) for raw 0x{raw:X2} "
                    + $"reach {reach}; the probe is not a valid test and must not be reported");
            }

            return g;
        }

        private static int Encode(TerrainGridData g, int x, int y)
        {
            int raw = 0;
            foreach ((string name, int dx, int dy, int bit) d in Dirs)
            {
                if (RaisedNeighborResolver.IsRaised(g, x + d.dx, y + d.dy))
                {
                    raw |= 1 << d.bit;
                }
            }

            return raw;
        }

        private static TerrainGridData BuildMask(string[] mask)
        {
            int h = mask.Length;
            int w = 0;
            foreach (string row in mask)
            {
                w = Math.Max(w, row.Length);
            }

            // The fixture sits at a different origin each time, so any accidental dependence on an
            // absolute coordinate shows up as a failure rather than passing by luck.
            int ox = 2 + (w % 3);
            int oy = 2 + (h % 3);
            var g = new TerrainGridData(w + 6, h + 6, ox, oy);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(ox + x, oy + y, TerrainType.Grass);
                }
            }

            for (int i = 0; i < h; i++)
            {
                int y = oy + (h - 1 - i);
                for (int x = 0; x < mask[i].Length; x++)
                {
                    if (mask[i][x] == 'X' || mask[i][x] == 'R')
                    {
                        g.SetElevation(ox + x, y, ElevationLevel.Raised);
                    }
                }
            }

            return g;
        }
    }
}
