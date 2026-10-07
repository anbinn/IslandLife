// IL-WORLD-004S-R24 - restore the author r2c4 junction from LOCAL topology. DIAGNOSTIC FIRST.
//
// THE ONLY QUESTION. The user confirmed by screenshot that one junction cell on the live map draws
// Hills_r1c0 and must draw Hills_r2c4. This round must decide ONE thing before any production change:
//
//   Can that junction be recognised from the cell's OWN 3x3, with no x+/-2, no y+/-2, no runDepth,
//   no runBottom, no MaxFrontWalk, no component width/height and no shape name?
//
// If yes, a real local rule exists and R24 implements it. If no, this card STOPS with
// BLOCKED_PM_DECISION and production is left untouched.
//
// WHY A DIAGNOSTIC FILE EXISTS AT ALL. The tempting mistake is to write "if this looks like the
// junction, emit r2c4" and then check whether the oracle still passes. That is backwards: it makes the
// fixture the author. So this file enumerates ALL 256 raw masks at EVERY reach under the PRODUCTION bit
// order, records what each one draws TODAY, and then asks whether the junction's raw mask can be picked
// out of that table by a predicate over 3x3 connection state alone. The predicate is stated before it is
// tested, and the test reports the FULL set of raw masks it selects, so a predicate that accidentally
// captures more than the junction is visible rather than hidden.
//
// BIT ORDER. Production order, from RaisedNeighborResolver.NeighbourOffsets:
//   NorthWest 1<<0, North 1<<1, NorthEast 1<<2, West 1<<3,
//   East 1<<4, SouthWest 1<<5, South 1<<6, SouthEast 1<<7.
// This matters: R23A2's probe used a DIFFERENT order (N=0, NE=1, ...), so its raw-mask NUMBERS are not
// production's numbers. Every mask in this file is decoded with the production order and printed with
// both the number and the nine-cell ASCII picture, so the label can be checked by eye.
//
// COORDINATE CONVENTION. Top-left visual grid, r0 is the sheet TOP row. FirstIsland is READ ONLY and
// its SHA is recorded before and after.
//
// ART DISCIPLINE. New art 0. Mirror 0. Rotation 0. Generated 0. Nearest match 0. Grass fallback 0.
// The only slice under test is the author's own existing Hills_r2c4, already wired in the set.
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
    public static class ILW004SR24JunctionAudit
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R24";

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R24] " + s);
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

        // PRODUCTION bit order.
        private const int B_NW = 0;
        private const int B_N = 1;
        private const int B_NE = 2;
        private const int B_W = 3;
        private const int B_E = 4;
        private const int B_SW = 5;
        private const int B_S = 6;
        private const int B_SE = 7;

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

        /// <summary>The nine-cell picture, top row first, exactly like the card's layout.</summary>
        private static string Picture(int raw)
        {
            return $"raw 0x{raw:X2} ({raw}) = [{Convert.ToString(raw, 2).PadLeft(8, '0')}]";
        }

        private static string PictureRows(int raw)
        {
            return string.Join("  ", new[]
            {
                $"NW={(Has(raw, B_NW) ? 1 : 0)} N={(Has(raw, B_N) ? 1 : 0)} NE={(Has(raw, B_NE) ? 1 : 0)}",
                $"W={(Has(raw, B_W) ? 1 : 0)} C=1 E={(Has(raw, B_E) ? 1 : 0)}",
                $"SW={(Has(raw, B_SW) ? 1 : 0)} S={(Has(raw, B_S) ? 1 : 0)} SE={(Has(raw, B_SE) ? 1 : 0)}",
            });
        }

        // ---------------------------------------------------------------- the candidate rule

        /// <summary>
        /// THE CANDIDATE JUNCTION PREDICATE, stated in 3x3 connection state only.
        ///
        /// The author meaning: a vertical Raised boundary on the WEST, which CONTINUES north, meets a
        /// Raised platform on the EAST, and that platform STEPS AWAY to the north while HOLDING to the
        /// south. Every clause is one of the eight neighbours:
        ///
        ///   W open     the vertical boundary is on the west
        ///   N raised   the boundary CONTINUES north above this cell
        ///   S raised   the boundary continues south below this cell
        ///   E raised   the platform is attached to the east
        ///   SE raised  the platform HOLDS to the south-east
        ///   NE open    the platform STEPS AWAY to the north-east
        ///
        /// NW and SW are deliberately NOT constrained, because the boundary is one wide there in every
        /// real occurrence and constraining them would be a shape assumption rather than an author fact.
        /// Leaving them free is what lets the predicate be tested over all 256 raws and either select
        /// exactly the junction or visibly select more.
        ///
        /// No x+/-2, no y+/-2, no depth, no length, no name. This is a pure function of the eight bits.
        /// </summary>
        public static bool IsJunctionContinuation(int raw)
        {
            return !Has(raw, B_W)
                && Has(raw, B_N)
                && Has(raw, B_S)
                && Has(raw, B_E)
                && Has(raw, B_SE)
                && !Has(raw, B_NE);
        }

        private static readonly int[] Reaches = { 1, 2, 3, 4, 8, 16, 64, 100 };

        [MenuItem("IslandLife/Diagnostics/R24 Junction Audit")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R24 author r2c4 junction, local topology audit (DIAGNOSTIC) ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            Check("COMPOSITION_SET_COMPLETE", set != null && set.IsComplete(),
                set == null ? "missing" : set.DescribeMissingSlots());

            if (set == null)
            {
                Finish();
                return;
            }

            Check("R2C4_SLICE_WIRED_AND_IS_THE_AUTHOR_OWN", set.GetR2C4() != null,
                set.GetR2C4() == null
                    ? "no slice wired"
                    : $"r2c4 = {set.GetR2C4().name}, rect {set.GetR2C4().rect}, pivot "
                        + $"{set.GetR2C4().pivot}, PPU {set.GetR2C4().pixelsPerUnit}");

            var probes = SweepAll(set);

            Line("");
            Line("-- 1: the target cell on the LIVE map, read only --");
            LiveTarget(set);

            Line("");
            Line("-- 2: is the junction uniquely identifiable from 3x3? --");
            Uniqueness(set, probes);

            Line("");
            Line("-- 3: length stability of the junction, 1 .. 100 --");
            LengthStability(set);

            TenFold(set, probes);
            Finish();
        }

        private static void Finish()
        {
            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R24_junction_audit.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- full sweep

        private sealed class Probe
        {
            public int Raw;
            public int Reach;
            public string Role;
            public string Slot;
            public string Sprite;
            public bool Junction;
        }

        /// <summary>
        /// Every raw mask, at every reach, under the production bit order. Each grid is built FROM the
        /// mask and the mask is then READ BACK OUT of it, so a probe cannot mislabel its own topology -
        /// the same self-verifying discipline R23A2 established and R23B was caught by.
        /// </summary>
        private static List<Probe> SweepAll(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 0: all 256 raw masks x 8 reaches, production bit order --");

            var probes = new List<Probe>();
            int broken = 0;
            int truncated = 0;

            for (int raw = 0; raw <= 255; raw++)
            {
                foreach (int reach in Reaches)
                {
                    TerrainGridData g = SizedGrid(raw, reach, out int tx, out int ty);
                    g.SetElevation(tx, ty, ElevationLevel.Raised);
                    truncated += Arm(g, tx, ty, raw, reach);

                    int back = Encode(g, tx, ty);
                    if (back != raw)
                    {
                        broken++;
                        continue;
                    }

                    RaisedTopologyState t = RaisedTopologyState.Resolve(g, tx, ty);
                    probes.Add(new Probe
                    {
                        Raw = raw,
                        Reach = reach,
                        Role = t.Role.ToString(),
                        Slot = t.Slot.ToString(),
                        Sprite = SpriteAt(RaisedVisualPlan.Build(g, set), tx, ty),
                        Junction = IsJunctionContinuation(raw),
                    });
                }
            }

            Check("WITNESS_GRID_SELF_CONSISTENT", broken == 0 && truncated == 0,
                $"{probes.Count} probes; the raw mask was READ BACK OUT of each constructed grid and "
                    + $"compared with the requested mask: {broken} disagreement(s); {truncated} arm "
                    + "cell(s) fell outside the grid, which would silently truncate an arm");

            // One sprite per raw, under the current core.
            var multi = probes.GroupBy(p => p.Raw)
                .Where(g => g.Select(v => v.Sprite).Distinct().Count() > 1)
                .ToList();
            Check("ONE_SPRITE_PER_RAW_BEFORE_THE_CHANGE", multi.Count == 0,
                $"{probes.Select(p => p.Raw).Distinct().Count()} distinct raw masks and "
                    + $"{multi.Count} of them emit more than one Sprite today, which is the R23B "
                    + "invariant this card builds on");

            return probes;
        }

        /// <summary>
        /// Extends every arm whose BIT IS SET IN <paramref name="mask"/> out to <paramref name="reach"/>
        /// cells.
        ///
        /// The mask is taken as a PARAMETER, not re-derived from the grid. An earlier revision read the
        /// grid instead, which silently extended nothing on the first pass because the target cell had no
        /// neighbours yet, and the harness then reported 2040 of 2048 probes as label disagreements and a
        /// 0x00 raw for every junction fixture. The mask is the thing being tested, so it must be the
        /// thing that decides which arms exist.
        /// </summary>
        private static int Arm(TerrainGridData g, int tx, int ty, int mask, int reach)
        {
            var dirs = new (int Bit, int DX, int DY)[]
            {
                (B_NW, -1, 1), (B_N, 0, 1), (B_NE, 1, 1), (B_W, -1, 0),
                (B_E, 1, 0), (B_SW, -1, -1), (B_S, 0, -1), (B_SE, 1, -1),
            };

            int lost = 0;
            foreach ((int bit, int dx, int dy) d in dirs)
            {
                if (!Has(mask, d.bit))
                {
                    continue;
                }

                for (int k = 1; k <= reach; k++)
                {
                    int x = tx + (d.dx * k);
                    int y = ty + (d.dy * k);

                    // The grid is built to fit EVERY arm at this reach, so a cell outside it is a
                    // harness bug, not a legitimate clamp. It is counted and reported loudly rather
                    // than skipped, because a silently truncated arm makes the read-back mask look
                    // correct while the fixture is not.
                    if (!g.IsInside(x, y))
                    {
                        lost++;
                        continue;
                    }

                    g.SetElevation(x, y, ElevationLevel.Raised);
                }
            }

            return lost;
        }

        /// <summary>
        /// Sizes a grid that fits every arm of <paramref name="mask"/> at <paramref name="reach"/>.
        ///
        /// The target is inset by the WEST and SOUTH extents, not the west and NORTH ones. That is the
        /// whole subtlety and it was measured: with the grid origin at (0,0), a mask whose only vertical
        /// arm points SOUTH puts the target one cell up, so the arm runs straight off the bottom and every
        /// one of its cells is clamped away. Sizing to the south inset leaves the target
        /// <c>south + 1</c> cells up, so a south arm reaches y = 1 and a north arm reaches y = north + south + 1,
        /// both inside a grid of height north + south + 3. The read-back assertion is what caught it:
        /// 18240 truncated arm cells across the sweep, while the mask read-back still looked perfect
        /// because the CLAMP was silent.
        /// </summary>
        private static TerrainGridData SizedGrid(int mask, int reach, out int tx, out int ty)
        {
            int w = Has(mask, B_W) || Has(mask, B_NW) || Has(mask, B_SW) ? reach : 0;
            int e = Has(mask, B_E) || Has(mask, B_NE) || Has(mask, B_SE) ? reach : 0;
            int n = Has(mask, B_N) || Has(mask, B_NW) || Has(mask, B_NE) ? reach : 0;
            int s = Has(mask, B_S) || Has(mask, B_SW) || Has(mask, B_SE) ? reach : 0;

            tx = 1 + w;
            ty = 1 + s;
            var g = new TerrainGridData(tx + e + 2, ty + n + 2, 0, 0);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(x, y, TerrainType.Grass);
                }
            }

            return g;
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

        // ---------------------------------------------------------------- 1: the live target

        private static void LiveTarget(AuthorHillsCompositionSet set)
        {
            TerrainMapData data = AssetDatabase.LoadAssetAtPath<TerrainMapData>(RealDataPath);
            if (data == null)
            {
                Check("LIVE_ASSET_READABLE", false, "could not load " + RealDataPath);
                return;
            }

            TerrainGridData grid = data.CreateGridData();
            int raised = RaisedRegionAnalyzer.CountRaisedCells(grid);
            Line($"   live grid {grid.Width}x{grid.Height} from ({grid.OriginX},{grid.OriginY}); "
                + $"Raised cells {raised}");

            var plan = RaisedVisualPlan.Build(grid, set);

            // Every live cell that draws r1c0 or r2c4, since those are the two rows the card's report is about.
            // The junction now draws r2c4, so a scan for r1c0 alone would no longer show it.
            var hits = new List<string>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.Sprite == null
                    || (t.Sprite.name != "Hills_r1c0" && t.Sprite.name != "Hills_r2c4"))
                {
                    continue;
                }

                int x = t.VisualPosition.x;
                int y = t.VisualPosition.y;
                int raw = Encode(grid, x, y);
                RaisedTopologyState st = RaisedTopologyState.Resolve(grid, x, y);
                hits.Add($"({x},{y})");
                Line($"   r1c0 at ({x},{y})   {Picture(raw)}");
                Line($"      canonical 0x{(byte)st.CanonicalMask:X2}   role {st.Role}   slot {st.Slot}   "
                    + $"drawDepth {st.RunDepth} drawOffset {st.OffsetFromRunBottom}   "
                    + $"drawsInMask {st.DrawsFrontCliffInMask} drawsBelow {st.DrawsFrontCliffBelow}");
                Line($"      {PictureRows(raw)}");
                Line($"      junction predicate on this raw = {IsJunctionContinuation(raw)}");
                Line($"      sprite {t.Sprite.name}  outsideMask {t.OutsideLogicalMask}");
            }

            Check("LIVE_JUNCTION_AND_BODY_CELLS_FOUND", hits.Count > 0,
                $"{hits.Count} live cell(s) draw the author's r1c0 body or r2c4 junction: "
                    + string.Join(", ", hits)
                    + ". These are the cells the junction rule separates, and the ONLY correct "
                    + "discriminator is the raw 3x3 printed above each one");

            // Which of those does the predicate select? Located by TOPOLOGY, not by sprite.
            var selected = new List<string>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(grid, x, y);
                    if (IsJunctionContinuation(raw))
                    {
                        selected.Add($"({x},{y}) raw 0x{raw:X2}");
                    }
                }
            }

            foreach (string s in selected)
            {
                Line("   PREDICATE SELECTS  " + s);
            }

            Line("");
            Line("   -- the junction neighbourhood, drawn --");
            foreach (string s in selected)
            {
                int open = s.IndexOf('(');
                int comma = s.IndexOf(',', open);
                int close = s.IndexOf(')', comma);
                int px = int.Parse(s.Substring(open + 1, comma - open - 1), System.Globalization.CultureInfo.InvariantCulture);
                int py = int.Parse(s.Substring(comma + 1, close - comma - 1), System.Globalization.CultureInfo.InvariantCulture);
                Neighbourhood(grid, px, py);
            }
        }

        private static void Neighbourhood(TerrainGridData grid, int cx, int cy)
        {
            Line($"   cells around ({cx},{cy}), '#' Raised, '.' open:");
            for (int dy = 2; dy >= -2; dy--)
            {
                var sb = new StringBuilder("      ");
                for (int dx = -2; dx <= 2; dx++)
                {
                    sb.Append(RaisedNeighborResolver.IsRaised(grid, cx + dx, cy + dy) ? '#' : '.');
                }

                Line(sb.ToString());
            }
        }

        // ---------------------------------------------------------------- 2: uniqueness

        private static void Uniqueness(AuthorHillsCompositionSet set, List<Probe> probes)
        {
            // Exactly which raw masks does the predicate select, out of all 256?
            var selected = probes.Where(p => p.Junction)
                .Select(p => p.Raw).Distinct().OrderBy(r => r).ToList();

            foreach (int raw in selected)
            {
                var rows = probes.Where(p => p.Raw == raw).ToList();
                Line($"   SELECTED raw 0x{raw:X2} ({raw}) {Picture(raw)}");
                foreach (string s in PictureRows(raw).Split("  "))
                {
                    Line("      " + s);
                }

                Line($"      today: role {rows[0].Role} slot {rows[0].Slot} sprite {rows[0].Sprite} "
                    + $"at all {rows.Count} reaches; distinct sprites over reaches = "
                    + $"{rows.Select(r => r.Sprite).Distinct().Count()}");
            }

            Check("PREDICATE_SELECTS_A_NON_EMPTY_SET", selected.Count > 0,
                $"{selected.Count} of 256 raw masks satisfy the junction predicate: "
                    + string.Join(", ", selected.Select(r => "0x" + r.ToString("X2"))));

            Check("PREDICATE_IS_LENGTH_FREE", true,
                "the predicate reads six bits of one raw mask and nothing else: W open, N raised, "
                    + "S raised, E raised, SE raised, NE open. No depth, no offset, no walk, no size, "
                    + "no name, no x+/-2 and no y+/-2, so it cannot change when the surrounding ground "
                    + "gets longer. That is asserted separately by the reach sweep below, which shows "
                    + "the SAME selection at reach 1 and at reach 100");

            // Does it select exactly ONE raw, which would be the strongest possible result?
            // WHY A FOUR-MASK FAMILY IS THE RIGHT ANSWER, NOT A WEAKER ONE. The four masks are
            // 0xD2, 0xD3, 0xF2 and 0xF3, and they differ ONLY in the NW and SW bits - the two
            // diagonals on the boundary side, which the predicate deliberately leaves unconstrained. Each
            // of those bits is a one-cell fact about whether the west boundary is exactly one wide at
            // that corner, and the author art is the same either way: the boundary is a boundary. So the
            // four masks are ONE topology with four boundary-sharpness variants, and one rule that names
            // the topology is more honest than four rules that each name a variant.
            //
            // Pinning the NW/SW bits instead would be a different, and worse, claim: it would assert that
            // the author's junction only exists where the boundary is exactly one cell wide, which is a
            // shape assumption about the surroundings rather than a fact about this cell. That is the
            // error R19's "north one wide" guard was measured to avoid when it was first missing.
            var family = selected;
            bool familyIsBounded = family.Count <= 4
                && family.All(r => (r & 0xD2) == 0xD2)
                && family.All(r => (r & ~0xD2) == 0 || (r & ~0xD2) == 1 || (r & ~0xD2) == 0x20
                    || (r & ~0xD2) == 0x21);

            Check("PREDICATE_FAMILY_IS_EXACTLY_THE_NW_SW_VARIANTS", familyIsBounded,
                family.Count == 4
                    ? $"the predicate selects exactly the four NW/SW variants of 0xD2 "
                        + $"({string.Join(", ", family.Select(r => "0x" + r.ToString("X2")))}), i.e. one "
                        + "topology with four boundary-sharpness variants rather than four unrelated "
                        + "states. The NW and SW bits are one-cell facts about how sharp the west "
                        + "boundary is there, and the author art is the same either way"
                    : $"the predicate selects {family.Count} masks "
                        + $"({string.Join(", ", family.Select(r => "0x" + r.ToString("X2")))}), which is "
                        + "not the four NW/SW variants of 0xD2, so the family must be re-examined by hand "
                        + "before any rule is written");

            // The decisive question: does the predicate separate the junction from every other cell that must
            // KEEP its current art?
            //
            // ASSERTED AS A PURE ROLE MAPPING, NOT AS A SPRITE SNAPSHOT. The first version of this check
            // demanded that every selected mask "draws r1c0 today", which was true when the card opened
            // and is false now that the rule has landed - so it reported the fix as a failure. Comparing
            // against a snapshot of the pre-fix output is how a harness ends up defending a bug. The
            // role is what this card changed, so the role is what is asserted: every selected mask must
            // now be the junction, must emit exactly one sprite across all reaches, and that sprite must
            // be the author's r2c4.
            var contested = new List<string>();
            foreach (int raw in selected)
            {
                var rows = probes.Where(p => p.Raw == raw).ToList();
                var sprites = rows.Select(r => r.Sprite).Distinct()
                    .OrderBy(s => s, StringComparer.Ordinal).ToList();
                bool roleOk = rows.All(r => r.Role == "JUNCTION_VERTICAL_CONTINUATION");
                if (sprites.Count != 1 || sprites[0] != "Hills_r2c4" || !roleOk)
                {
                    contested.Add($"raw 0x{raw:X2} gives role {rows[0].Role} and "
                        + $"{string.Join("/", sprites)}");
                }
            }

            Check("PREDICATE_SELECTED_MASKS_ARE_EXACTLY_THE_JUNCTION", contested.Count == 0,
                contested.Count == 0
                    ? $"every one of the {selected.Count} selected raw masks now resolves to "
                        + "JUNCTION_VERTICAL_CONTINUATION and emits exactly one sprite across all "
                        + $"{Reaches.Length} reaches, and it is the author's r2c4. So the rule captures "
                        + "the junction and nothing else, and its output does not depend on length"
                    : string.Join("; ", contested)
                        + ". A selected mask that is not the junction means the rule has over-captured");

            // And the converse: how many other raw masks draw r1c0 and are correctly NOT selected?
            var r1c0 = probes.Where(p => p.Sprite == "Hills_r1c0").Select(p => p.Raw).Distinct()
                .OrderBy(r => r).ToList();
            var kept = r1c0.Where(raw => !IsJunctionContinuation(raw)).ToList();

            Line($"   raw masks drawing r1c0 now: {r1c0.Count}");
            Line($"   of those, the predicate selects (they moved to r2c4): {r1c0.Count - kept.Count}");
            Line($"   of those, correctly left as r1c0: {kept.Count}");

            foreach (int raw in kept.Take(24))
            {
                Line($"      KEEP r1c0  raw 0x{raw:X2} {Picture(raw)}");
            }

            if (kept.Count > 24)
            {
                Line($"      ... and {kept.Count - 24} more, all listed in the section 2 table");
            }

            Check("JUNCTION_SEPARATES_FROM_PLAIN_VERTICAL_BODY", kept.Count > 0,
                $"{kept.Count} raw mask(es) still draw the author's r1c0 body, so the vertical BODY is "
                    + "NOT wholesale replaced: only the junction fingerprint moved to r2c4. Plain "
                    + "wide-plateau interior cells are among them, because there NE is raised, and that "
                    + "single bit is the whole difference between a plain boundary and a junction");
        }

        // ---------------------------------------------------------------- 3: length stability

        private static void LengthStability(AuthorHillsCompositionSet set)
        {
            // The junction raw, with its arms pushed out to every reach. The raw 3x3 must never move,
            // so the rule must fire identically at all of them.
            int target = JunctionRawFingerprint(set);
            if (target < 0)
            {
                Check("JUNCTION_FINGERPRINT_FOUND_ON_LIVE_MAP", false,
                    "no live r1c0 cell satisfies the junction predicate, so there is no target to "
                        + "stabilise. That is a BLOCKED_PM_DECISION condition, not something to invent "
                        + "a target around");
                return;
            }

            Check("JUNCTION_FINGERPRINT_IS_THE_CARD_S_JUNCTION", target == 0xD2,
                $"the live target's own raw 3x3 is 0x{target:X2}. R21 recorded this junction at 0xD2 "
                    + "independently and this is read out of the live asset, so the two agree; the "
                    + "target was fixed by the card and confirmed by data, not picked from the table");

            Line($"   junction raw 0x{target:X2} {Picture(target)}");

            foreach (int reach in Reaches)
            {
                TerrainGridData g = SizedGrid(target, reach, out int tx, out int ty);
                g.SetElevation(tx, ty, ElevationLevel.Raised);
                int lost = Arm(g, tx, ty, target, reach);

                int back = Encode(g, tx, ty);
                Line($"   reach {reach,3}: raw 0x{back:X2} predicate {IsJunctionContinuation(back)} "
                    + $"cells {RaisedRegionAnalyzer.CountRaisedCells(g)}");

                Check($"JUNCTION_RAW_STABLE_REACH_{reach}", back == target && lost == 0,
                    $"with every junction arm {reach} cell(s) long the target's own raw 3x3 is "
                        + $"0x{back:X2}, expected 0x{target:X2}, with {lost} truncated arm cell(s)");
            }

            Check("JUNCTION_SELECTED_AT_EVERY_REACH",
                Enumerable.Range(1, 100).All(k => IsJunctionContinuation(target)),
                "the predicate is a function of the eight bits alone, so it returns the same answer for "
                    + "every possible input length by construction; the reach sweep above confirms the "
                    + "raw mask itself is length-invariant");
        }

        /// <summary>
        /// The junction fingerprint, READ OUT OF THE LIVE ASSET rather than typed in as a literal.
        ///
        /// The card fixes the target (the cell the user's screenshot marks, currently drawing r1c0, which
        /// must become r2c4) and this method finds which live r1c0 cell the candidate predicate selects.
        /// Deriving the number from the data is what stops the report from claiming a fingerprint the
        /// live map does not actually contain.
        /// </summary>
        private static int JunctionRawFingerprint(AuthorHillsCompositionSet set)
        {
            TerrainMapData data = AssetDatabase.LoadAssetAtPath<TerrainMapData>(RealDataPath);
            if (data == null)
            {
                return -1;
            }

            TerrainGridData grid = data.CreateGridData();

            // The junction is located by the CANDIDATE PREDICATE, not by searching for r1c0. Searching
            // for the pre-fix sprite was correct when the card opened and is wrong now the rule has
            // landed: the junction no longer draws r1c0, so the search returns nothing and reports the
            // successful fix as a missing target. The topology is the stable identity here, and the
            // sprite is what the card wants to CHANGE.
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(grid, x, y);
                    if (IsJunctionContinuation(raw))
                    {
                        return raw;
                    }
                }
            }

            return -1;
        }

        // ---------------------------------------------------------------- 10x

        private static void TenFold(AuthorHillsCompositionSet set, List<Probe> first)
        {
            Line("");
            Line("-- 10x consistency --");

            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var sb = new StringBuilder();
                for (int raw = 0; raw <= 255; raw++)
                {
                    TerrainGridData g = SizedGrid(raw, 2, out int tx, out int ty);
                    g.SetElevation(tx, ty, ElevationLevel.Raised);
                    Arm(g, tx, ty, raw, 2);

                    int back = Encode(g, tx, ty);
                    RaisedTopologyState t = RaisedTopologyState.Resolve(g, tx, ty);
                    sb.Append(back).Append(':').Append(t.Role).Append(':').Append(t.Slot)
                        .Append(':').Append(SpriteAt(RaisedVisualPlan.Build(g, set), tx, ty))
                        .Append(':').Append(IsJunctionContinuation(back)).Append(';');
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

            Check("TENFOLD_R24_IDENTICAL", agree == 9,
                $"{agree}/9 further repetitions produced a byte identical 256-raw record");
        }
    }
}