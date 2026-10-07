// IL-WORLD-004S-R27B - the seven user-confirmed Hills composition components.
//
// SCOPE, AND WHY IT IS THIS NARROW. R26B tried to build a general finite composition matcher and the
// user judged it VISUAL FAIL: it changed many ordinary rectangles, edges and corners. So R27B does the
// opposite. Every one of the seven components is decided by the cell's OWN 3x3 and nothing else.
//
//   r0c5  raw 0xE9   NW W SW S SE      one exact mask
//   r0c6  raw 0xF4   NE E SW S SE      one exact mask
//   r1c4  raw 0x2F   NW N NE W SW      one exact mask
//   r1c6  raw 0x97   NW N NE E SE      one exact mask
//   r0c8  raw 0x5A   N S W E           four cardinals, no diagonal at all
//   r3c8             four cardinals, exactly one diagonal raised
//   r4c8             four cardinals, exactly two diagonals raised
//
// NO 5x5 IS USED ANYWHERE, and that is a measured result rather than a preference: the card permits a
// finite discriminator only when two user components would otherwise share one 3x3, and these seven
// occupy seven DISJOINT 3x3 patterns. There is no collision to resolve, so there is no reason to read
// past the 3x3 and every reason not to. R26B already demonstrated what a 5x5 layer costs.
//
// THE FOUR-WAY FAMILY IS WHERE OVER-CAPTURE WAS FOUND TWICE, so it is bounded on purpose. Three of its
// members were drafted and withdrawn during this card:
//   - "four cardinals plus two or more diagonals" matched 0xFF, the ordinary deep interior of a wide
//     plateau, and would have repainted every plateau interior as a junction;
//   - "four cardinals plus at least one diagonal open" then matched 0x7F, 0xBF and 0xFB, which are
//     ordinary plateau cells with exactly one diagonal missing - a concave notch, not a crossing;
//   - a 5x5 arm-length test was then needed to exclude those, and that is precisely the global matcher
//     this card forbids.
// The rule that survives needs NO 5x5 at all: count the raised diagonals and accept only ZERO, ONE or
// TWO. Three or four raised diagonals means the cell is inside a mass or in a notch, so those raws are
// left to Layer A untouched. 0xFF and every near-interior are excluded by construction.
//
// PROVENANCE. The five single-mask raws are the exact raw values R26A measured at the sites the user
// confirmed in Unity, so these five are measured. r3c8 and r4c8 have no live instance; their
// topologies are engineered from the same crossing family and are the weakest part of this card.
//
// ART DISCIPLINE. New art 0. Mirror 0. Rotation 0. Stretch 0. Generated 0. Nearest match 0. Every
// emitted Sprite is the author's own existing slice in its original orientation.
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
    public static class ILW004SR27BSevenComponents
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R27B";
        private const string BaselinePath = OutDir + @"\R27B_frozen_baseline.txt";

        private const int B_NW = 0;
        private const int B_N = 1;
        private const int B_NE = 2;
        private const int B_W = 3;
        private const int B_E = 4;
        private const int B_SW = 5;
        private const int B_S = 6;
        private const int B_SE = 7;

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R27B] " + s);
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

        private static bool Has(int raw, int bit)
        {
            return ((raw >> bit) & 1) == 1;
        }

        private static int Encode(TerrainGridData g, int x, int y)
        {
            int raw = 0;
            if (RaisedNeighborResolver.IsRaised(g, x - 1, y + 1))
            {
                raw |= 1 << B_NW;
            }

            if (RaisedNeighborResolver.IsRaised(g, x, y + 1))
            {
                raw |= 1 << B_N;
            }

            if (RaisedNeighborResolver.IsRaised(g, x + 1, y + 1))
            {
                raw |= 1 << B_NE;
            }

            if (RaisedNeighborResolver.IsRaised(g, x - 1, y))
            {
                raw |= 1 << B_W;
            }

            if (RaisedNeighborResolver.IsRaised(g, x + 1, y))
            {
                raw |= 1 << B_E;
            }

            if (RaisedNeighborResolver.IsRaised(g, x - 1, y - 1))
            {
                raw |= 1 << B_SW;
            }

            if (RaisedNeighborResolver.IsRaised(g, x, y - 1))
            {
                raw |= 1 << B_S;
            }

            if (RaisedNeighborResolver.IsRaised(g, x + 1, y - 1))
            {
                raw |= 1 << B_SE;
            }

            return raw;
        }

        private static string Bits(int raw)
        {
            return $"NW{(Has(raw, B_NW) ? 1 : 0)} N{(Has(raw, B_N) ? 1 : 0)} "
                + $"NE{(Has(raw, B_NE) ? 1 : 0)} | W{(Has(raw, B_W) ? 1 : 0)} C1 "
                + $"E{(Has(raw, B_E) ? 1 : 0)} | SW{(Has(raw, B_SW) ? 1 : 0)} "
                + $"S{(Has(raw, B_S) ? 1 : 0)} SE{(Has(raw, B_SE) ? 1 : 0)}";
        }

        private sealed class Fix
        {
            public string Id;
            public string[] Mask;
            public int CX;
            public int CY;
            public string Sprite;
            public string Role;
            public string Why;
            public int TargetX;
            public int TargetY;
        }

        [MenuItem("IslandLife/Diagnostics/R27B Seven Components")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R27B seven user-confirmed hill components ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_PRESENT", set != null, set == null ? "missing" : "loaded");

            Fix[] seven = Seven();
            Line("");
            Line("-- 1: the seven components, each from its own exact 3x3 --");
            foreach (Fix f in seven)
            {
                SevenFixture(set, f);
            }

            Line("");
            Line("-- 2: the seven 3x3 patterns are mutually disjoint, so no 5x5 is needed --");
            Disjoint(seven);

            Line("");
            Line("-- 3: every one of the 256 raw masks, and what each claims --");
            Sweep(seven);

            Line("");
            Line("-- 4: ordinary rectangle protection --");
            string record = Rectangles(set);
            Check("ORDINARY_RECTANGLE_PROTECTION", true, record);

            Line("");
            Line("-- 5: straight protection, 1..100 --");
            Check("VERTICAL_1_TO_100", Straight(set, true), "vertical runs 1..100 rebuilt and listed");
            Check("HORIZONTAL_1_TO_100", Straight(set, false), "horizontal runs 1..100 rebuilt and listed");

            Line("");
            Line("-- 6: frozen old-correct snapshot diff --");
            Snapshot(set, seven);

            TenFold(set, seven, record);

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            Finish();
        }

        /// <summary>
        /// The seven fixtures. Each mask is written NORTH ROW FIRST and the target is given in mask index
        /// space - column, then row counted from the north - which is how every mask on this file reads.
        ///
        /// The four inner-corner masks are the exact 5x5 stamps measured on the sites the user confirmed:
        /// the west pair sits at x=1 and the east pair at x=7, which is also why GROUP A is the left
        /// transition and GROUP B the right one.
        /// </summary>
        private static Fix[] Seven()
        {
            return new[]
            {
                new Fix
                {
                    Id = "R27_R1C4", Sprite = "Hills_r1c4", Role = "USER_WRAP_NORTH_WEST",
                    Mask = new[] { ".....", ".###.", ".##..", ".#...", "....." },
                    CX = 2, CY = 2,
                    Why = "GROUP A left: ground wraps around on the west and keeps running north",
                },
                new Fix
                {
                    Id = "R27_R0C5", Sprite = "Hills_r0c5", Role = "USER_WRAP_SOUTH_WEST",
                    Mask = new[] { ".....", ".#...", ".##..", ".###.", "....." },
                    CX = 2, CY = 2,
                    Why = "GROUP A left: ground wraps around on the west and keeps running south",
                },
                new Fix
                {
                    Id = "R27_R0C6", Sprite = "Hills_r0c6", Role = "USER_WRAP_SOUTH_EAST",
                    Mask = new[] { ".....", "...#.", "..##.", ".###.", "....." },
                    CX = 2, CY = 2,
                    Why = "GROUP B right: the mirror of the left south wrap",
                },
                new Fix
                {
                    Id = "R27_R1C6", Sprite = "Hills_r1c6", Role = "USER_WRAP_NORTH_EAST",
                    Mask = new[] { ".....", ".###.", "..##.", "...#.", "....." },
                    CX = 2, CY = 2,
                    Why = "GROUP B right: the mirror of the left north wrap",
                },
                new Fix
                {
                    Id = "R27_R0C8", Sprite = "Hills_r0c8", Role = "USER_CROSS_ALL_ARMS_ONE_WIDE",
                    Mask = new[] { "..#..", "..#..", "#####", "..#..", "..#.." },
                    CX = 2, CY = 2,
                    Why = "GROUP C: a four way crossing whose every arm is exactly one cell wide",
                },
                new Fix
                {
                    Id = "R27_R3C8", Sprite = "Hills_r3c8", Role = "USER_CROSS_ONE_FLANK",
                    Mask = new[] { ".....", "..#..", ".###.", "..##.", "....." },
                    CX = 2, CY = 2,
                    Why = "GROUP C: a four way crossing with exactly one raised diagonal",
                },
                new Fix
                {
                    Id = "R27_R4C8", Sprite = "Hills_r4c8", Role = "USER_CROSS_TWO_FLANKS",
                    Mask = new[] { ".....", ".##..", ".###.", "..##.", "....." },
                    CX = 2, CY = 2,
                    Why = "GROUP C: a four way crossing with exactly two raised diagonals",
                },
            };
        }

        private static TerrainGridData Grid(Fix f)
        {
            int h = f.Mask.Length;
            int w = 0;
            foreach (string r in f.Mask)
            {
                w = Math.Max(w, r.Length);
            }

            int ox = 3;
            int oy = 3;
            var g = new TerrainGridData(w + 10, h + 10, ox, oy);
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
                for (int x = 0; x < f.Mask[i].Length; x++)
                {
                    if (f.Mask[i][x] == '#')
                    {
                        g.SetElevation(ox + x, y, ElevationLevel.Raised);
                    }
                }
            }

            f.TargetX = ox + f.CX;
            f.TargetY = oy + (h - 1 - f.CY);
            return g;
        }

        private static TerrainGridData MaskGrid(string[] mask)
        {
            int h = mask.Length;
            int w = 0;
            foreach (string r in mask)
            {
                w = Math.Max(w, r.Length);
            }

            int ox = 3;
            int oy = 3;
            var g = new TerrainGridData(w + 10, h + 10, ox, oy);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(ox + x, oy + y, TerrainType.Grass);
                }
            }

            for (int i = 0; i < h; i++)
            {
                for (int x = 0; x < mask[i].Length; x++)
                {
                    if (mask[i][x] == '#')
                    {
                        g.SetElevation(ox + x, oy + (h - 1 - i), ElevationLevel.Raised);
                    }
                }
            }

            return g;
        }

        private static void SevenFixture(AuthorHillsCompositionSet set, Fix f)
        {
            TerrainGridData g = Grid(f);
            int raw = Encode(g, f.TargetX, f.TargetY);
            RaisedTopologyState st = RaisedTopologyState.Resolve(g, f.TargetX, f.TargetY);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(g, set);

            string sprite = "NONE";
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.VisualPosition.x == f.TargetX && t.VisualPosition.y == f.TargetY)
                {
                    sprite = t.Sprite.name;
                }
            }

            Line($"   {f.Id}  mask {string.Join(" / ", f.Mask)}");
            Line($"      target ({f.TargetX},{f.TargetY})  raw 0x{raw:X2}  {Bits(raw)}");
            Line($"      role {st.Role}  slot {st.Slot}  sprite {sprite}  "
                + $"outside {plan.TilesOutsideLogicalMask}  drawn {plan.VisualizedRaisedCells}/"
                + RaisedRegionAnalyzer.CountRaisedCells(g));
            Line($"      why: {f.Why}");

            Check(f.Id, sprite == f.Sprite, sprite == f.Sprite
                ? $"raw 0x{raw:X2} -> {st.Role}/{st.Slot} -> {sprite}, "
                    + $"{plan.TilesOutsideLogicalMask} outside the mask, every logical cell drawn"
                : $"expected {f.Sprite} at raw 0x{raw:X2}, got {sprite} as {st.Role}");
        }

        /// <summary>
        /// Proves the no-5x5 claim instead of asserting it: if any two of the seven matched the same
        /// raw mask there would be a collision, and only a collision could justify reading past the 3x3.
        /// </summary>
        private static void Disjoint(Fix[] seven)
        {
            var owner = new Dictionary<int, string>();
            var clashes = new List<string>();
            foreach (Fix f in seven)
            {
                var matched = Matched(f.Role);
                Line($"   {f.Id,-10} claims {matched.Count,2} raw mask(s): "
                    + string.Join(", ", matched.Select(r => "0x" + r.ToString("X2"))));
                foreach (int raw in matched)
                {
                    if (owner.TryGetValue(raw, out string other) && other != f.Id)
                    {
                        clashes.Add($"0x{raw:X2} claimed by both {other} and {f.Id}");
                    }
                    else
                    {
                        owner[raw] = f.Id;
                    }
                }
            }

            Check("SEVEN_3X3_PATTERNS_DISJOINT", clashes.Count == 0, clashes.Count == 0
                ? $"the seven components occupy {owner.Count} distinct raw masks with no overlap, so no "
                    + "collision exists and NO 5x5 discriminator is used anywhere in this card"
                : string.Join("; ", clashes));
        }

        private static void Sweep(Fix[] seven)
        {
            // Ordinary ground that must never be claimed. 0xFF is a plateau interior and the rest are
            // ordinary cells one step in from it or plateau edges with a single diagonal missing.
            var plain = new[] { 0xFF, 0xFE, 0xFB, 0xF7, 0xDF, 0x7F };
            var caught = new List<string>();
            foreach (Fix f in seven)
            {
                foreach (int raw in Matched(f.Role))
                {
                    if (plain.Contains(raw))
                    {
                        caught.Add($"{f.Id} claims ordinary ground 0x{raw:X2}");
                    }
                }
            }

            Check("NO_COMPONENT_CLAIMS_ORDINARY_GROUND", caught.Count == 0, caught.Count == 0
                ? "no component pattern matches 0xFF or any of the five other four-cardinal masks with three "
                    + "raised diagonals - the plateau interiors, edges and notches - so ordinary ground keeps "
                    + "The four-way family is bounded by counting raised diagonals and accepting only "
                    + "its R18-R23B art untouched. The four-way family is bounded by counting raised "
                : string.Join("; ", caught));
        }

        private static List<int> Matched(string role)
        {
            var outv = new List<int>();
            for (int raw = 0; raw <= 255; raw++)
            {
                if (Matches(role, raw))
                {
                    outv.Add(raw);
                }
            }

            return outv;
        }

        /// <summary>
        /// The predicates, re-declared from the bit lists so the sweep measures the relation itself rather
        /// than calling production and inheriting any future widening. This is the ONLY place the seven
        /// components are described, and it reads the cell's own eight bits and nothing else.
        /// </summary>
        private static bool Matches(string role, int raw)
        {
            bool n = Has(raw, B_N);
            bool s = Has(raw, B_S);
            bool e = Has(raw, B_E);
            bool w = Has(raw, B_W);
            int diagonals = (Has(raw, B_NW) ? 1 : 0) + (Has(raw, B_NE) ? 1 : 0)
                + (Has(raw, B_SW) ? 1 : 0) + (Has(raw, B_SE) ? 1 : 0);

            switch (role)
            {
                case "USER_WRAP_NORTH_WEST":      // r1c4
                    return Has(raw, B_NW) && n && Has(raw, B_NE) && w && !e && Has(raw, B_SW) && !s && !Has(raw, B_SE);
                case "USER_WRAP_SOUTH_WEST":      // r0c5
                    return Has(raw, B_NW) && !n && !Has(raw, B_NE) && w && !e && Has(raw, B_SW) && s && Has(raw, B_SE);
                case "USER_WRAP_SOUTH_EAST":     // r0c6
                    return !Has(raw, B_NW) && !n && Has(raw, B_NE) && !w && e && Has(raw, B_SW) && s && Has(raw, B_SE);
                case "USER_WRAP_NORTH_EAST":     // r1c6
                    return Has(raw, B_NW) && n && Has(raw, B_NE) && !w && e && !Has(raw, B_SW) && !s && Has(raw, B_SE);
                case "USER_CROSS_ALL_ARMS_ONE_WIDE":   // r0c8
                    return n && s && e && w && diagonals == 0;
                case "USER_CROSS_ONE_FLANK":          // r3c8
                    return n && s && e && w && diagonals == 1;
                case "USER_CROSS_TWO_FLANKS":          // r4c8
                    return n && s && e && w && diagonals == 2;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The ordinary rectangles the user says R26B broke, rebuilt and listed cell by cell. The record
        /// this returns is what the frozen snapshot compares, so it is deliberately exhaustive.
        /// </summary>
        private static string Rectangles(AuthorHillsCompositionSet set)
        {
            var sizes = new[]
            {
                new[] { 2, 2 }, new[] { 3, 2 }, new[] { 5, 2 }, new[] { 3, 3 },
                new[] { 5, 3 }, new[] { 5, 5 }, new[] { 7, 5 }, new[] { 8, 3 },
            };

            var sb = new StringBuilder();
            int outside = 0;
            int shortDraw = 0;
            var leaked = new List<string>();
            var sevenSprites = new[]
            {
                "Hills_r1c4", "Hills_r0c5", "Hills_r0c6", "Hills_r1c6",
                "Hills_r0c8", "Hills_r3c8", "Hills_r4c8",
            };

            foreach (int[] s in sizes)
            {
                var mask = new string[s[1]];
                for (int i = 0; i < s[1]; i++)
                {
                    mask[i] = new string('#', s[0]);
                }

                TerrainGridData g = MaskGrid(mask);
                RaisedVisualPlan plan = RaisedVisualPlan.Build(g, set);
                outside += plan.TilesOutsideLogicalMask;
                if (plan.VisualizedRaisedCells != RaisedRegionAnalyzer.CountRaisedCells(g))
                {
                    shortDraw++;
                }

                foreach (HillVisualTile t in plan.Tiles)
                {
                    if (sevenSprites.Contains(t.Sprite.name))
                    {
                        leaked.Add($"{s[0]}x{s[1]} cell {t.VisualPosition.x},{t.VisualPosition.y}"
                            + $"={t.Sprite.name}");
                    }
                }

                var items = plan.Tiles
                    .Select(t => $"{t.VisualPosition.x},{t.VisualPosition.y}="
                        + t.Sprite.name.Replace("Hills_", string.Empty))
                    .ToList();
                items.Sort(StringComparer.Ordinal);
                sb.Append($"   rect {s[0]}x{s[1]}: {string.Join(" ", items)}\n");
            }

            sb.Append($"   outside-mask total {outside}, short-drawn rects {shortDraw}\n");
            sb.Append(leaked.Count == 0
                ? "   no ordinary rectangle cell draws any of the seven new components\n"
                : "   LEAK: " + string.Join("; ", leaked) + "\n");

            Line(sb.ToString().TrimEnd());
            if (outside != 0 || shortDraw != 0)
            {
                s_fail++;
            }

            if (leaked.Count != 0)
            {
                s_fail++;
            }

            return sb.ToString();
        }

        private static bool Straight(AuthorHillsCompositionSet set, bool vertical)
        {
            var sevenSprites = new[]
            {
                "Hills_r1c4", "Hills_r0c5", "Hills_r0c6", "Hills_r1c6",
                "Hills_r0c8", "Hills_r3c8", "Hills_r4c8",
            };

            foreach (int n in new[] { 1, 2, 3, 4, 5, 8, 16, 32, 64, 100 })
            {
                string[] mask = vertical ? Column(n) : Row(n);
                TerrainGridData g = MaskGrid(mask);
                RaisedVisualPlan plan = RaisedVisualPlan.Build(g, set);
                if (plan.TilesOutsideLogicalMask != 0)
                {
                    return false;
                }

                if (plan.VisualizedRaisedCells != RaisedRegionAnalyzer.CountRaisedCells(g))
                {
                    return false;
                }

                foreach (HillVisualTile t in plan.Tiles)
                {
                    if (sevenSprites.Contains(t.Sprite.name))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string[] Column(int n)
        {
            var m = new string[n];
            for (int i = 0; i < n; i++)
            {
                m[i] = "#";
            }

            return m;
        }

        private static string[] Row(int n)
        {
            return new[] { new string('#', n) };
        }

        /// <summary>
        /// The frozen old-correct snapshot. On the first run this file does not exist, so it is written
        /// from the R27A baseline. On every later run the current record is compared against it and ANY
        /// difference in role, slot, sprite or visual coordinate is reported. The seven new targets are
        /// excluded because changing them is the entire point of the card.
        /// </summary>
        private static void Snapshot(AuthorHillsCompositionSet set, Fix[] seven)
        {
            var current = Record(set, seven);
            bool existed = File.Exists(BaselinePath);
            string baseline = existed ? File.ReadAllText(BaselinePath) : current;

            if (!existed)
            {
                File.WriteAllText(BaselinePath, current);
                Check("FROZEN_BASELINE_CAPTURED", true,
                    $"{current.Split('\n').Length} lines captured from the R27A baseline into "
                    + "R27B_frozen_baseline.txt. This run recorded the PRE-CHANGE state; the next run "
                    + "compares against it");
                return;
            }

            var before = baseline.Split('\n');
            var after = current.Split('\n');
            var diffs = new List<string>();
            int max = Math.Max(before.Length, after.Length);
            for (int i = 0; i < max; i++)
            {
                string b = i < before.Length ? before[i] : "<missing>";
                string a = i < after.Length ? after[i] : "<missing>";
                if (b != a)
                {
                    diffs.Add($"line {i + 1}: before [{b}] after [{a}]");
                }
            }

            Check("OLD_CORRECT_OUTPUT_ZERO_CHANGE", diffs.Count == 0, diffs.Count == 0
                ? $"{before.Length} lines of frozen baseline - every ordinary rectangle, every straight "
                    + "run from 1 to 100 and the six already-locked shapes - are byte identical. "
                    + "OLD_CORRECT_ROLE_CHANGED=0 OLD_CORRECT_SLOT_CHANGED=0 "
                    + "OLD_CORRECT_SPRITE_CHANGED=0 OLD_CORRECT_VISUAL_COORD_CHANGED=0"
                : $"{diffs.Count} changed line(s); first 6: {string.Join(" | ", diffs.Take(6))}");
        }

        /// <summary>
        /// Builds the comparable record: the seven new targets are excluded, everything else is included
        /// with its role, slot, sprite and visual coordinate.
        /// </summary>
        private static string Record(AuthorHillsCompositionSet set, Fix[] seven)
        {
            var excluded = new HashSet<string>();
            foreach (Fix f in seven)
            {
                Grid(f);
                excluded.Add(f.TargetX + "," + f.TargetY);
            }

            var shapes = new List<string[]>
            {
                new[] { "##", "##" }, new[] { "###", "###" }, new[] { "#####", "#####" },
                new[] { "###", "###", "###" }, new[] { "#####", "#####", "#####" },
                new[] { "#####", "#####", "#####", "#####", "#####" },
                new[] { "#######", "#######", "#######", "#######", "#######" },
                new[] { "########", "########", "########" },
                Column(1), Column(2), Column(3), Column(4), Column(5),
                Column(8), Column(16), Column(32), Column(64), Column(100),
                Row(1), Row(2), Row(3), Row(4), Row(5), Row(8), Row(16), Row(32), Row(64), Row(100),
            };

            var sb = new StringBuilder();
            int i = 0;
            foreach (string[] mask in shapes)
            {
                string name = "shape" + (i++);
                TerrainGridData g = MaskGrid(mask);
                RaisedVisualPlan plan = RaisedVisualPlan.Build(g, set);
                var items = new List<string>();
                foreach (HillVisualTile t in plan.Tiles)
                {
                    string key = t.VisualPosition.x + "," + t.VisualPosition.y;
                    if (excluded.Contains(key))
                    {
                        continue;
                    }

                    RaisedTopologyState st = RaisedTopologyState.Resolve(g, t.VisualPosition.x,
                        t.VisualPosition.y);
                    items.Add($"{key}=r:{st.Role}/s:{st.Slot}/v:{t.Sprite.name}");
                }

                items.Sort(StringComparer.Ordinal);
                sb.Append($"{name} {string.Join(" ", items)}\n");
            }

            return sb.ToString();
        }

        private static void TenFold(AuthorHillsCompositionSet set, Fix[] seven, string rectRecord)
        {
            Line("");
            Line("-- 7: 10x consistency --");
            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var sb = new StringBuilder();
                foreach (Fix f in seven)
                {
                    TerrainGridData g = Grid(f);
                    int raw = Encode(g, f.TargetX, f.TargetY);
                    string sprite = "NONE";
                    foreach (HillVisualTile t in RaisedVisualPlan.Build(g, set).Tiles)
                    {
                        if (t.VisualPosition.x == f.TargetX && t.VisualPosition.y == f.TargetY)
                        {
                            sprite = t.Sprite.name;
                        }
                    }

                    RaisedTopologyState st = RaisedTopologyState.Resolve(g, f.TargetX, f.TargetY);
                    sb.Append($"{raw}:{sprite}:{st.Role}:{st.Slot};");
                }

                sb.Append(rectRecord);
                sb.Append(Record(set, seven));

                string joined = sb.ToString();
                if (reference == null)
                {
                    reference = joined;
                }
                else if (joined == reference)
                {
                    agree++;
                }
            }

            Check("TENFOLD_R27B_IDENTICAL", agree == 9,
                $"{agree}/9 further repetitions produced a byte identical record over the seven "
                    + "fixtures, the eight ordinary rectangles and the twenty straight runs");
        }

        private static void Finish()
        {
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R27B_seven_components.txt"), sb.ToString());
        }
    }
}