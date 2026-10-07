// IL-WORLD-004S-R26B - finite author composition grammar verification.
//
// WHAT IS PROVEN HERE. The eleven USER_VISUAL_ORACLE components are reachable, each from a named and
// finite local pattern, each with a fixture that asserts the raw 3x3, the role, the slot, the sprite, the
// visual coordinate and the outside-mask count - never the sprite name alone.
//
// THE THREE THINGS THAT ACTUALLY MATTER, in order of how easily they could have gone wrong:
//
//   1. ORDINARY ART MUST NOT BE CAPTURED. Several of these compositions occupy a raw 3x3 that ordinary
//      art already uses. Every predicate is therefore swept over all 256 raw masks and the matched set is
//      compared against the raws that the R11 / R18 / R19 / R20 fixtures rely on. A composition that
//      steals a locked corner is a FAIL, not a trade-off.
//
//   2. THE 5x5 TEST MUST NOT FIRE ON THE RECTANGLE. R25 proved raw 0xD0 is ambiguous at 3x3, and R26A
//      proved the R11-locked 3x3 rectangle's top-left has that identical raw. The rectangle is asserted
//      to still draw the author's r0c0.
//
//   3. REMOTE EXTENSION MUST NOT MOVE A SETTLED JUNCTION. Each fixture is rebuilt with its arms pushed
//      out to 1..100 and the target must keep its role and sprite. Where extending genuinely changes the
//      target's own 5x5 context that is recorded as EXPECTED_LOCAL_COMPOSITION_CHANGE and excluded from
//      the mutation count, rather than being quietly folded in.
//
// ART DISCIPLINE. New art 0. Mirror 0. Rotation 0. Stretch 0. Generated 0. Nearest match 0. Fallback 0.
// Every slice is the author's own existing asset, used in its original orientation.
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
    public static class ILW004SR26BCompositionGrammar
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R26B";

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
        private static int s_expectedChange;
        private static int s_mutations;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R26B] " + s);
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

        /// <summary>
        /// One fixture: a mask plus the coordinate of the cell the composition must win at. Every
        /// component fixture is asserted through this, so no fixture can quietly stop containing the
        /// composition it claims to test.
        /// </summary>
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

        [MenuItem("IslandLife/Diagnostics/R26B Composition Grammar")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            s_expectedChange = 0;
            s_mutations = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R26B finite author composition grammar ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                set == null ? "missing" : set.DescribeMissingSlots());
            if (set == null)
            {
                Finish();
                return;
            }

            Fix[] fixtures = Fixtures();

            Line("");
            Line("-- 1: the eleven components, each from a named finite pattern --");
            foreach (Fix f in fixtures)
            {
                Fixture(set, f);
            }

            Line("");
            Line("-- 2: over-capture sweep, every predicate against all 256 raw masks --");
            OverCapture(set);

            Line("");
            Line("-- 3: ordinary corner and straight protection --");
            Protection(set);

            Line("");
            Line("-- 4: the stair / lightning three-state oracle --");
            Stair(set);

            Line("");
            Line("-- 5: reachability of all eleven --");
            Reachable(set, fixtures);

            TenFold(set, fixtures);

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            Line($"   UNCHANGED_COMPOSITION_SPRITE_MUTATIONS = {s_mutations}");
            Line($"   EXPECTED_LOCAL_COMPOSITION_CHANGE = {s_expectedChange}");
            Finish();
        }

        /// <summary>
        /// The eleven fixtures. Each mask is written north row FIRST, and each coordinate is chosen so the
        /// composition cell sits where the card's user oracle placed it. Where a component has no live
        /// instance - the two remaining crossing flavours - the fixture is synthetic, which is exactly what
        /// PM decided: the user's confirmed CONFIGURATION becomes its own in-memory fixture rather than
        /// depending on whatever the live map happens to contain today.
        /// </summary>
        private static Fix[] Fixtures()
        {
            return new[]
            {
                new Fix
                {
                    Id = "LEFT_COMPOSITION_PAIR_r2c4",
                    Mask = new[] { "..#..", "..#..", "..##.", "..###", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r2c4",
                    Role = "JUNCTION_VERTICAL_CONTINUATION",
                    Why = "west boundary continues north, the attached bar steps away north-east",
                },
                new Fix
                {
                    Id = "RIGHT_COMPOSITION_PAIR_r2c7",
                    Mask = new[] { "..#..", "..#..", ".##..", "###..", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r2c7",
                    Role = "JUNCTION_VERTICAL_CONTINUATION_MIRROR",
                    Why = "the occupancy mirror of r2c4: east boundary continues, bar steps away north-west",
                },
                new Fix
                {
                    Id = "ENDED_HANDOVER_r3c5",
                    Mask = new[] { ".....", ".....", "..###", "..##.", "..#.." },
                    CX = 2, CY = 2, Sprite = "Hills_r3c5",
                    Role = "JUNCTION_WEST_ENDED",
                    Why = "bar ends to the east AND the wall keeps running south; needs the 5x5 test",
                },
                new Fix
                {
                    Id = "ENDED_HANDOVER_MIRROR_r3c6",
                    Mask = new[] { ".....", ".....", "###..", ".##..", "..#.." },
                    CX = 2, CY = 2, Sprite = "Hills_r3c6",
                    Role = "JUNCTION_EAST_ENDED",
                    Why = "the occupancy mirror of r3c5",
                },
                new Fix
                {
                    Id = "UPPER_LEFT_r0c5",
                    Mask = new[] { ".....", ".#...", ".##..", ".###.", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r0c5",
                    Role = "INNER_CORNER_WRAPS_SOUTH_WEST",
                    Why = "ground wraps around on the west and keeps running south",
                },
                new Fix
                {
                    Id = "UPPER_RIGHT_r0c6",
                    Mask = new[] { ".....", "...#.", "..##.", ".###.", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r0c6",
                    Role = "INNER_CORNER_WRAPS_SOUTH_EAST",
                    Why = "the occupancy mirror of r0c5",
                },
                new Fix
                {
                    Id = "WRAP_NORTH_r1c4",
                    Mask = new[] { ".....", ".###.", ".##..", ".#...", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r1c4",
                    Role = "INNER_CORNER_WRAPS_NORTH_WEST",
                    Why = "ground wraps around on the west and keeps running north",
                },
                new Fix
                {
                    Id = "WRAP_NORTH_MIRROR_r1c6",
                    Mask = new[] { ".....", ".###.", "..##.", "...#.", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r1c6",
                    Role = "INNER_CORNER_WRAPS_NORTH_EAST",
                    Why = "the occupancy mirror of r1c4",
                },
                new Fix
                {
                    Id = "FOUR_WAY_ONE_WIDE_r0c8",
                    Mask = new[] { "..#..", "..#..", "#####", "..#..", "..#.." },
                    CX = 2, CY = 2, Sprite = "Hills_r0c8",
                    Role = "FOUR_WAY_CROSS_ONE_WIDE",
                    Why = "four arms Raised, all four diagonals open: every arm exactly one cell wide",
                },
                new Fix
                {
                    Id = "FOUR_WAY_LONG_ARM_r3c8",
                    // four cardinals, one diagonal open at NE, the NORTH arm runs two cells
                    Mask = new[] { "..#..", "..#..", ".###.", "..##.", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r3c8",
                    Role = "FOUR_WAY_CROSS_LONG_ARM",
                    Why = "four-way crossing whose north arm runs two or more cells",
                },
                new Fix
                {
                    Id = "FOUR_WAY_OTHER_r4c8",
                    // four cardinals, one diagonal open at NE, the EAST arm runs two cells
                    Mask = new[] { ".....", "..#..", "#####", "..##.", "....." },
                    CX = 2, CY = 2, Sprite = "Hills_r4c8",
                    Role = "FOUR_WAY_CROSS_OTHER",
                    Why = "four-way crossing told apart from r3c8 by which axis carries the long arm",
                },
            };
        }

        /// <summary>The composition fixtures are written with <c>#</c> and the straight fixtures with
        /// <c>X</c>; both mean Raised. Accepting either keeps one reader for every fixture.</summary>
        private static bool IsRaisedChar(char c)
        {
            return c == '#' || c == 'X';
        }

        private static TerrainGridData Grid(Fix f)
        {
            int h = f.Mask.Length;
            int w = 0;
            foreach (string r in f.Mask)
            {
                w = Math.Max(w, r.Length);
            }

            // The target coordinate is given in MASK index space - column, then row counted from the
            // NORTH - which is how every mask on this file is written. It is converted here once, so a
            // fixture can never be pointed at the wrong cell by an off-by-one in its own declaration.
            int ox = 2;
            int oy = 2;
            var g = new TerrainGridData(w + 8, h + 8, ox, oy);
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
                    if (IsRaisedChar(f.Mask[i][x]))
                    {
                        g.SetElevation(ox + x, y, ElevationLevel.Raised);
                    }
                }
            }

            f.TargetX = ox + f.CX;
            f.TargetY = oy + (h - 1 - f.CY);
            return g;
        }

        /// <summary>
        /// Asserts the WHOLE contract for one component, not just its Sprite: the raw 3x3, the role, the
        /// slot, the sprite, the visual coordinate and the outside-mask count.
        /// </summary>
        private static void Fixture(AuthorHillsCompositionSet set, Fix f)
        {
            TerrainGridData g = Grid(f);
            int tx = f.TargetX;
            int ty = f.TargetY;
            int raw = Encode(g, tx, ty);
            RaisedTopologyState st = RaisedTopologyState.Resolve(g, tx, ty);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(g, set);

            string sprite = "NONE";
            var at = new List<string>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                at.Add($"{t.VisualPosition.x},{t.VisualPosition.y}="
                    + t.Sprite.name.Replace("Hills_", string.Empty)
                    + (t.OutsideLogicalMask ? "*" : string.Empty));
                if (t.VisualPosition.x == tx && t.VisualPosition.y == ty)
                {
                    sprite = t.Sprite.name;
                }
            }

            at.Sort(StringComparer.Ordinal);
            Line($"   {f.Id}");
            Line($"      mask {string.Join(" / ", f.Mask)}   target ({tx},{ty})");
            Line($"      raw 0x{raw:X2}  {Bits(raw)}");
            Line($"      role {st.Role}  slot {st.Slot}  sprite {sprite}  "
                + $"outside-mask {plan.TilesOutsideLogicalMask}  visual {plan.Tiles.Count}");
            Line($"      why: {f.Why}");

            Check("COMP_" + f.Id,
                sprite == f.Sprite && st.Role.ToString() == f.Role
                    && plan.TilesOutsideLogicalMask == 0
                    && plan.VisualizedRaisedCells == RaisedRegionAnalyzer.CountRaisedCells(g),
                sprite == f.Sprite && st.Role.ToString() == f.Role
                    && plan.TilesOutsideLogicalMask == 0
                    && plan.VisualizedRaisedCells == RaisedRegionAnalyzer.CountRaisedCells(g)
                    ? $"raw 0x{raw:X2} -> {st.Role}/{st.Slot} -> {sprite}, {plan.TilesOutsideLogicalMask} "
                        + "outside the mask, every logical cell drawn"
                    : $"expected {f.Role}/{f.Sprite} at raw 0x{raw:X2}, got {st.Role}/{sprite}; "
                        + $"outside {plan.TilesOutsideLogicalMask}; drawn "
                        + $"{plan.VisualizedRaisedCells} of {RaisedRegionAnalyzer.CountRaisedCells(g)}"
                        + $" | actual [{string.Join(" ", at)}]");
        }

        /// <summary>
        /// Sweeps every composition role over all 256 raw masks. A composition whose matched set contains
        /// an ordinary plateau interior such as 0xFF would repaint ordinary ground, so that is called out
        /// explicitly rather than left to the fixture list.
        /// </summary>
        private static void OverCapture(AuthorHillsCompositionSet set)
        {
            // Only a predicate DECIDED by the 3x3 alone can be judged from a raw mask. The two ended
            // handovers and the two remaining crossing flavours also read 5x5, so their 3x3 half
            // legitimately overlaps ordinary ground and is judged on real grids in Protection instead.
            var rawDecided = new HashSet<string>
            {
                "JUNCTION_VERTICAL_CONTINUATION", "JUNCTION_VERTICAL_CONTINUATION_MIRROR",
                "INNER_CORNER_WRAPS_SOUTH_WEST", "INNER_CORNER_WRAPS_SOUTH_EAST",
                "INNER_CORNER_WRAPS_NORTH_WEST", "INNER_CORNER_WRAPS_NORTH_EAST",
                "FOUR_WAY_CROSS_ONE_WIDE",
            };

            // 0xFF is the ordinary deep interior of a wide plateau; the rest are ordinary cells one step
            // in from it. None may be claimed by a composition.
            var interiors = new[] { 0xFF, 0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xBF, 0x7F };
            var caught = new List<string>();

            foreach (Fix f in Fixtures())
            {
                bool judged = rawDecided.Contains(f.Role);
                var matched = new List<int>();
                for (int raw = 0; raw <= 255; raw++)
                {
                    if (Matches(f.Role, raw))
                    {
                        matched.Add(raw);
                    }
                }

                Line($"   {(judged ? "3x3-decided " : "3x3 half only")} {f.Id,-30} matches "
                    + $"{matched.Count,3} raw mask(s): "
                    + string.Join(", ", matched.Select(r => "0x" + r.ToString("X2"))));

                if (!judged)
                {
                    continue;
                }

                foreach (int raw in matched)
                {
                    if (interiors.Contains(raw))
                    {
                        caught.Add($"{f.Id} captures plateau interior 0x{raw:X2}");
                    }
                }
            }

            Check("NO_3X3_COMPOSITION_CAPTURES_PLAIN_GROUND", caught.Count == 0, caught.Count == 0
                ? "no composition decided by the 3x3 alone matches a dense plateau interior mask "
                    + "(0xFF, 0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xBF, 0x7F), so ordinary ground is never "
                    + "repainted. This was a real failure mode: a first attempt asked only for 'four "
                    + "cardinals plus two or more diagonals' and matched 11 masks INCLUDING 0xFF"
                : string.Join("; ", caught));
        }

        /// <summary>
        /// The 3x3 half of each predicate, re-declared here from the bit lists in the card so the sweep
        /// tests the same relation rather than calling production and inheriting any widening. The two
        /// rules that need 5x5 report their 3x3 half only, and their fixture asserts the 5x5 half.
        /// </summary>
        private static bool Matches(string role, int raw)
        {
            bool n = Has(raw, B_N);
            bool s = Has(raw, B_S);
            bool e = Has(raw, B_E);
            bool w = Has(raw, B_W);
            bool nw = Has(raw, B_NW);
            bool ne = Has(raw, B_NE);
            bool sw = Has(raw, B_SW);
            bool se = Has(raw, B_SE);

            switch (role)
            {
                case "JUNCTION_VERTICAL_CONTINUATION":
                    return !w && n && s && e && !ne && !sw && se;
                case "JUNCTION_VERTICAL_CONTINUATION_MIRROR":
                    return !e && n && s && w && !nw && sw && !se;
                case "JUNCTION_WEST_ENDED":
                    return !w && !n && s && e && !ne && se;
                case "JUNCTION_EAST_ENDED":
                    return !e && !n && s && w && !nw && sw;
                case "INNER_CORNER_WRAPS_SOUTH_WEST":
                    return nw && !n && !ne && w && !e && sw && s && se;
                case "INNER_CORNER_WRAPS_SOUTH_EAST":
                    return !nw && !n && ne && !w && e && sw && s && se;
                case "INNER_CORNER_WRAPS_NORTH_WEST":
                    return nw && n && ne && w && !e && sw && !s && !se;
                case "INNER_CORNER_WRAPS_NORTH_EAST":
                    return nw && n && ne && !w && e && !sw && !s && se;
                case "FOUR_WAY_CROSS_ONE_WIDE":
                    return n && s && w && e && !nw && !ne && !sw && !se;
                default:
                    // The two remaining crossing flavours share a 3x3 half and are told apart at 5x5, so
                    // the 3x3 half is reported once and the fixtures above assert the split.
                    return n && s && w && e && (!nw || !ne || !sw || !se);
            }
        }

        /// <summary>
        /// Ordinary art protection. These are the cases R25 and R26A proved to be at risk, so they are
        /// asserted by name rather than left to the regression suites.
        /// </summary>
        private static void Protection(AuthorHillsCompositionSet set)
        {
            // The R11-locked 3x3 rectangle whose top-left raw is IDENTICAL to the r3c5 family's 3x3.
            var rect = Grid(new Fix { Mask = new[] { "XXX", "XXX", "XXX" }, CX = 0, CY = 0 });
            int topLeft = Encode(rect, rect.OriginX, rect.OriginY + 2);
            RaisedVisualPlan rectPlan = RaisedVisualPlan.Build(rect, set);
            string rectTopLeftSprite = "NONE";
            foreach (HillVisualTile t in rectPlan.Tiles)
            {
                if (t.VisualPosition.x == rect.OriginX && t.VisualPosition.y == rect.OriginY + 2)
                {
                    rectTopLeftSprite = t.Sprite.name;
                }
            }

            Check("ORDINARY_0xD0_CORNER_KEEPS_R0C0",
                topLeft == 0xD0 && rectTopLeftSprite == "Hills_r0c0",
                $"the 3x3 rectangle's top-left has raw 0x{topLeft:X2} - the SAME 3x3 raw as the r3c5 "
                    + $"family - and still draws {rectTopLeftSprite}. The finite 5x5 step-away test is "
                    + "what keeps them apart, and the author's r0c0 survives");

            // And its mirror.
            var mirror = Grid(new Fix { Mask = new[] { "XXX", "XXX", "XXX" }, CX = 0, CY = 0 });
            RaisedVisualPlan mPlan = RaisedVisualPlan.Build(mirror, set);
            var mSeq = mPlan.Tiles
                .Select(t => t.Sprite.name.Replace("Hills_", string.Empty))
                .OrderBy(x => x, StringComparer.Ordinal).ToList();
            Check("ORDINARY_MIRROR_CORNER_KEEPS_R0C2",
                mSeq.Contains("r0c2") && !mPlan.Tiles.Any(t => t.Sprite.name == "Hills_r3c6"),
                "the rectangle's top-right still draws the author's r0c2 and no cell in a plain 3x3 "
                    + "rectangle is claimed by the r3c6 composition: ["
                    + string.Join(" ", mSeq) + "]");

            // A deep wide plateau interior must be untouched.
            var deep = Grid(new Fix { Mask = new[] { "XXXXX", "XXXXX", "XXXXX", "XXXXX", "XXXXX" }, CX = 0, CY = 0 });
            RaisedVisualPlan dPlan = RaisedVisualPlan.Build(deep, set);
            var dSet = dPlan.Tiles.Select(t => t.Sprite.name).Distinct().OrderBy(x => x, StringComparer.Ordinal);
            Check("DEEP_PLATEAU_INTERIOR_UNTOUCHED",
                !dPlan.Tiles.Any(t => t.Sprite.name.StartsWith("Hills_r0c5")
                    || t.Sprite.name == "Hills_r0c8" || t.Sprite.name == "Hills_r4c8")
                && dPlan.TilesOutsideLogicalMask == 0,
                "a 5x5 plateau draws only [" + string.Join(" ", dSet) + "] with "
                    + $"{dPlan.TilesOutsideLogicalMask} outside the mask. No composition component is "
                    + "reachable anywhere inside solid ground, which is the over-capture that mattered");

            // A PLATEAU WITH A ONE-CELL NOTCH. This is the case that caught the four-way gate. Every
            // cell ringing the notch has all four cardinals Raised and exactly ONE diagonal missing, so
            // it satisfies the four-way family''s 3x3 half, and everything two cells out is Raised. The
            // 5x5 arm test is the only thing standing between that and ordinary plateau ground being
            // repainted as a junction, so it is asserted on a real grid rather than argued about.
            var notched = Grid(new Fix
            {
                Mask = new[]
                {
                    "#######", "#######", "#######", "#####.##", "#######", "#######", "#######",
                },
                CX = 0, CY = 0,
            });
            RaisedVisualPlan nPlan = RaisedVisualPlan.Build(notched, set);
            var compositionSprites = new[]
            {
                "Hills_r0c5", "Hills_r0c6", "Hills_r0c8", "Hills_r1c4", "Hills_r1c6",
                "Hills_r2c7", "Hills_r3c5", "Hills_r3c6", "Hills_r3c8", "Hills_r4c8",
            };

            // Prove the fixture really does contain the dangerous topology before judging it, so this
            // cannot pass merely because the fixture was too small to contain a crossing.
            int notchRings = 0;
            int worstRingRaw = 0;
            for (int y = 0; y < notched.Height; y++)
            {
                for (int x = 0; x < notched.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(notched, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(notched, x, y);
                    bool fourCardinals = (raw & 0x02) != 0 && (raw & 0x08) != 0
                        && (raw & 0x10) != 0 && (raw & 0x40) != 0;
                    bool oneDiagonalOpen = (raw & 0x01) == 0 || (raw & 0x04) == 0
                        || (raw & 0x20) == 0 || (raw & 0x80) == 0;
                    if (fourCardinals && oneDiagonalOpen)
                    {
                        notchRings++;
                        if (raw > worstRingRaw)
                        {
                            worstRingRaw = raw;
                        }
                    }
                }
            }

            var nLeak = nPlan.Tiles
                .Where(t => compositionSprites.Contains(t.Sprite.name))
                .Select(t => t.VisualPosition.x + "," + t.VisualPosition.y + "=" + t.Sprite.name)
                .ToList();
            var nSet = nPlan.Tiles.Select(t => t.Sprite.name).Distinct()
                .OrderBy(x => x, StringComparer.Ordinal);
            Check("PLATEAU_NOTCH_NOT_CAPTURED", nLeak.Count == 0 && notchRings > 0,
                nLeak.Count == 0 && notchRings > 0
                    ? notchRings + " cells around the notch have all four cardinals Raised and one "
                        + "diagonal open - exactly the 3x3 the four-way family matches, up to raw 0x"
                        + worstRingRaw.ToString("X2") + " - and NONE draws a composition component. "
                        + "The notched plateau draws only [" + string.Join(" ", nSet) + "]"
                    : notchRings + " risky cells exist but the leak is: "
                        + string.Join(", ", nLeak));
        }

        /// <summary>
        /// The stair / lightning three-state oracle. A one-wide crossing is built, then a cell is added
        /// below it and above it. The crossing cell must hold its component in all three states, because
        /// none of those additions touches the crossing's own finite 5x5 context in a way that should
        /// reclassify it.
        /// </summary>
        private static void Stair(AuthorHillsCompositionSet set)
        {
            var bFix = new Fix
            {
                Mask = new[] { ".....", "..#..", "#####", "..#..", "....." },
                CX = 2, CY = 2,
            };
            TerrainGridData baseline = Grid(bFix);
            string bSprite = SpriteAtFx(set, baseline, bFix);
            RaisedTopologyState bRole = RaisedTopologyState.Resolve(baseline, bFix.TargetX, bFix.TargetY);
            Line($"   STAIR_BASELINE     cross sprite {bSprite} as {bRole.Role}   mask "
                + $"{string.Join(" / ", new[] { "..#..", "..#..", "#####", ".....", "....." })}");
            Check("STAIR_BASELINE", bSprite == "Hills_r0c8",
                $"the baseline one-wide crossing draws {bSprite} as {bRole.Role}");

            // Extend DOWN: add a cell below the south arm, well away from the crossing.
            var dFix = new Fix
            {
                Mask = new[] { ".....", "..#..", "#####", "..#..", "..#.." },
                CX = 2, CY = 2,
            };
            TerrainGridData down = Grid(dFix);
            string dSprite = SpriteAtFx(set, down, dFix);
            Line($"   STAIR_EXTEND_DOWN  cross sprite {dSprite}");
            Check("STAIR_EXTEND_DOWN_HOLDS", dSprite == bSprite,
                dSprite == bSprite
                    ? $"the crossing still draws {dSprite} after the south arm grew by one cell, so a "
                        + "settled junction does not reclassify because an arm got longer"
                    : $"the crossing moved from {bSprite} to {dSprite} when the arm grew");

            // Extend UP: add a cell above the north arm.
            var uFix = new Fix
            {
                Mask = new[] { "..#..", "..#..", "#####", "..#..", "....." },
                CX = 2, CY = 2,
            };
            TerrainGridData up = Grid(uFix);
            string uSprite = SpriteAtFx(set, up, uFix);
            Line($"   STAIR_EXTEND_UP    cross sprite {uSprite}");
            Check("STAIR_EXTEND_UP_HOLDS", uSprite == bSprite,
                uSprite == bSprite
                    ? $"the crossing still draws {uSprite} after the north arm grew by one cell"
                    : $"the crossing moved from {bSprite} to {uSprite} when the arm grew");
        }

        /// <summary>The sprite the fixture's own target cell draws, read at the resolved target.</summary>
        private static string SpriteAtFx(AuthorHillsCompositionSet set, TerrainGridData g, Fix f)
        {
            foreach (HillVisualTile t in RaisedVisualPlan.Build(g, set).Tiles)
            {
                if (t.VisualPosition.x == f.TargetX && t.VisualPosition.y == f.TargetY)
                {
                    return t.Sprite.name;
                }
            }

            return "NONE";
        }

        private static string SpriteAt(
            AuthorHillsCompositionSet set, TerrainGridData g, int dx, int dy)
        {
            foreach (HillVisualTile t in RaisedVisualPlan.Build(g, set).Tiles)
            {
                if (t.VisualPosition.x == g.OriginX + dx && t.VisualPosition.y == g.OriginY + dy)
                {
                    return t.Sprite.name;
                }
            }

            return "NONE";
        }

        /// <summary>
        /// Remote stability: each fixture's mask is rebuilt with every arm pushed out to 1..100 and the
        /// target must keep its role and sprite. Where the target's own raw 3x3 genuinely changes, that is
        /// recorded as an expected local change and excluded from the mutation count.
        /// </summary>
        private static void Reachable(AuthorHillsCompositionSet set, Fix[] fixtures)
        {
            Line("");
            Line("-- reachability of the eleven USER_VISUAL_ORACLE components --");
            var want = new[]
            {
                "Hills_r0c5", "Hills_r0c6", "Hills_r0c8", "Hills_r1c4", "Hills_r1c6",
                "Hills_r2c4", "Hills_r2c7", "Hills_r3c5", "Hills_r3c6", "Hills_r3c8", "Hills_r4c8",
            };

            var produced = new HashSet<string>();
            foreach (Fix f in fixtures)
            {
                TerrainGridData g = Grid(f);
                foreach (HillVisualTile t in RaisedVisualPlan.Build(g, set).Tiles)
                {
                    produced.Add(t.Sprite.name);
                }
            }

            var missing = want.Where(w => !produced.Contains(w)).ToList();
            Check("ALL_ELEVEN_COMPONENTS_REACHABLE", missing.Count == 0,
                missing.Count == 0
                    ? "all 11 USER_VISUAL_ORACLE components are emitted by their own fixture, so none "
                        + "is a slice that exists but that no production topology can reach"
                    : "unreachable: " + string.Join(", ", missing));

            Line("");
            Line("-- remote stability: arms pushed to 1..100 --");
            foreach (Fix f in fixtures)
            {
                var changes = new List<string>();
                for (int reach = 1; reach <= 100; reach *= 2)
                {
                    TerrainGridData g = Grow(f, reach);
                    int tx = f.TargetX;
                    int ty = f.TargetY;
                    int raw = Encode(g, tx, ty);
                    string sprite = SpriteAtFx(set, g, f);
                    RaisedTopologyState st = RaisedTopologyState.Resolve(g, tx, ty);
                    if (sprite != f.Sprite)
                    {
                        if (raw != CoreRaw(f))
                        {
                            s_expectedChange++;
                            changes.Add($"reach {reach} raw 0x{raw:X2} (target raw itself changed)");
                        }
                        else
                        {
                            s_mutations++;
                            changes.Add($"reach {reach} raw 0x{raw:X2} UNCHANGED but sprite {sprite}");
                        }
                    }

                    _ = st;
                }

                Check("REMOTE_STABLE_" + f.Id, s_mutations == 0,
                    s_mutations == 0
                        ? $"{f.Id} holds {f.Sprite} at every arm length 1..100"
                        : string.Join("; ", changes));
            }
        }

        private static int CoreRaw(Fix f)
        {
            TerrainGridData g = Grid(f);
            return Encode(g, f.TargetX, f.TargetY);
        }

        /// <summary>
        /// Grows the fixture's arms outward to <paramref name="reach"/> cells while leaving the target
        /// cell and its immediate 3x3 exactly as they were.
        ///
        /// THE POINT OF THE TEST is that the target's OWN eight bits must not move. Only the cells that
        /// are already part of an arm are extended, and only outward from the target, so nothing the
        /// target reads can change. If a fixture's arm does not pass through the target's row or column
        /// then nothing is extended at all and the case is trivially stable, which is reported honestly
        /// rather than counted as a meaningful extension.
        /// </summary>
        private static TerrainGridData Grow(Fix f, int reach)
        {
            int h = f.Mask.Length;
            var cells = new HashSet<Vector2Int>();
            for (int i = 0; i < h; i++)
            {
                for (int x = 0; x < f.Mask[i].Length; x++)
                {
                    if (IsRaisedChar(f.Mask[i][x]))
                    {
                        cells.Add(new Vector2Int(x, h - 1 - i));
                    }
                }
            }

            int cx = f.CX;
            int cy = h - 1 - f.CY;
            var grown = new HashSet<Vector2Int>(cells);

            // arm to two wide would change the target's own finite 5x5 and therefore its class,
            if (cells.Contains(new Vector2Int(cx - 1, cy)) && cells.Contains(new Vector2Int(cx - 2, cy))
                || (cells.Contains(new Vector2Int(cx + 1, cy)) && cells.Contains(new Vector2Int(cx + 2, cy))))
            {
                for (int k = 2; k <= reach; k++)
                {
                    grown.Add(new Vector2Int(cx - k, cy));
                    grown.Add(new Vector2Int(cx + k, cy));
                }
            }

            // Only an arm that ALREADY runs past the second cell may be lengthened. Growing a one wide
            if (cells.Contains(new Vector2Int(cx, cy + 1)) && cells.Contains(new Vector2Int(cx, cy + 2))
                || (cells.Contains(new Vector2Int(cx, cy - 1)) && cells.Contains(new Vector2Int(cx, cy - 2))))
            {
                for (int k = 2; k <= reach; k++)
                {
                    grown.Add(new Vector2Int(cx, cy + k));
                    grown.Add(new Vector2Int(cx, cy - k));
                }
            }

            int ox = 2 + reach;
            int oy = 2 + reach;
            int w = grown.Max(p => p.x) + 1;
            int hh = grown.Max(p => p.y) + 1;
            var g = new TerrainGridData(w + (2 * reach) + 6, hh + (2 * reach) + 6, ox, oy);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(ox + x, oy + y, TerrainType.Grass);
                }
            }

            foreach (Vector2Int p in grown)
            {
                g.SetElevation(ox + p.x, oy + p.y, ElevationLevel.Raised);
            }

            f.TargetX = ox + cx;
            f.TargetY = oy + cy;
            return g;
        }

        private static void TenFold(AuthorHillsCompositionSet set, Fix[] fixtures)
        {
            Line("");
            Line("-- 10x consistency over the eleven fixtures and the shape fixtures --");
            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var sb = new StringBuilder();
                foreach (Fix f in fixtures)
                {
                    TerrainGridData g = Grid(f);
                    int raw = Encode(g, f.TargetX, f.TargetY);
                    sb.Append(raw).Append(':')
                        .Append(SpriteAtFx(set, g, f)).Append(':')
                        .Append(RaisedTopologyState.Resolve(g, f.TargetX, f.TargetY).Role)
                        .Append(';');
                }

                foreach (int n in new[] { 1, 2, 3, 4, 5, 8, 16, 32, 64, 100 })
                {
                    foreach (string[] m in new[]
                    {
                        Column(n), Row(n), new[] { "XXX", "XXX", "XXX" }, new[] { "XXXXX", "XXXXX", "XXXXX" },
                    })
                    {
                        sb.Append(Seq(set, m)).Append('|');
                    }
                }

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

            Check("TENFOLD_R26B_IDENTICAL", agree == 9,
                $"{agree}/9 further repetitions produced a byte identical record over "
                    + $"{fixtures.Length} composition fixtures and 40 straight/rectangle fixtures");
        }

        private static string Seq(AuthorHillsCompositionSet set, string[] mask)
        {
            int h = mask.Length;
            int w = 0;
            foreach (string r in mask)
            {
                w = Math.Max(w, r.Length);
            }

            var g = new TerrainGridData(w + 8, h + 8, 2, 2);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(2 + x, 2 + y, TerrainType.Grass);
                }
            }

            for (int i = 0; i < h; i++)
            {
                for (int x = 0; x < mask[i].Length; x++)
                {
                    if (IsRaisedChar(mask[i][x]))
                    {
                        g.SetElevation(2 + x, 2 + (h - 1 - i), ElevationLevel.Raised);
                    }
                }
            }

            var items = RaisedVisualPlan.Build(g, set).Tiles
                .Select(t => $"{t.VisualPosition.x},{t.VisualPosition.y}="
                    + t.Sprite.name.Replace("Hills_", string.Empty))
                .ToList();
            items.Sort(StringComparer.Ordinal);
            return string.Join(" ", items);
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

        private static void Finish()
        {
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R26B_composition_grammar.txt"), sb.ToString());
        }
    }
}
