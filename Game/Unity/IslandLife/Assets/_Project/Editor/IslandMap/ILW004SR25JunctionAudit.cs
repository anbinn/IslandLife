// IL-WORLD-004S-R25 - four-component junction grammar audit. DIAGNOSTIC, and it may well STOP.
//
// THE QUESTION THIS FILE ANSWERS, AND NOTHING ELSE. The card locks four USER_VISUAL_ORACLE components
// (r2c4, r2c7, r3c5, r3c6) and names NO semantic role for them. A = r2c4 is already locked by R24 at
// live cell (0,-10), raw 0xD2. The other three are given only as "positions the user pointed at in a
// screenshot", and this file does NOT have those screenshots.
//
// So the first job is to establish what can and cannot be concluded from the live topology alone, and to
// show any collision that would make a guessed rule unsafe. This file therefore:
//   1. enumerates every live raw 3x3 that currently draws a piece a junction grammar might want;
//   2. for each of the three unknown components, works out which live raw families are even CANDIDATES;
//   3. PROVES, cell by cell, whether any candidate family collides with cells that must keep their art;
//   4. audits reachability of every already-locked Hills component.
// It changes NO production file and writes nothing.
//
// BIT ORDER (production): NW 0x01  N 0x02  NE 0x04  W 0x08  E 0x10  SW 0x20  S 0x40  SE 0x80
// Every mask is decoded with this table and read back OUT of the live grid, so the R23B N-first slip
// cannot recur.
//
// READ ONLY: no paint, no undo, no scene, no AssetDatabase write.
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
    public static class ILW004SR25JunctionAudit
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R25";

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
            Debug.Log("[R25] " + s);
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

        private static string On(int raw, int bit)
        {
            return Has(raw, bit) ? "+" : "-";
        }

        [MenuItem("IslandLife/Diagnostics/R25 Junction Audit")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R25 four-component junction audit (DIAGNOSTIC) ===");
            Line("   production bit order: NW 0x01 N 0x02 NE 0x04 W 0x08 E 0x10 SW 0x20 S 0x40 SE 0x80");

            TerrainMapData data = AssetDatabase.LoadAssetAtPath<TerrainMapData>(RealDataPath);
            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            if (data == null || set == null)
            {
                Line("   cannot load the live map or the composition set");
                Finish();
                return;
            }

            TerrainGridData grid = data.CreateGridData();
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            var live = new List<Cell>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(grid, x, y);
                    RaisedTopologyState st = RaisedTopologyState.Resolve(grid, x, y);
                    string sprite = "NONE";
                    foreach (HillVisualTile t in plan.Tiles)
                    {
                        if (t.VisualPosition.x == x && t.VisualPosition.y == y)
                        {
                            sprite = t.Sprite == null ? "NULL" : t.Sprite.name;
                        }
                    }

                    live.Add(new Cell
                    {
                        X = x,
                        Y = y,
                        Raw = raw,
                        Role = st.Role.ToString(),
                        Slot = st.Slot.ToString(),
                        Sprite = sprite,
                    });
                }
            }

            Line($"   live Raised cells {live.Count}; visual tiles {plan.Tiles.Count}; "
                + $"outside the logical mask {plan.TilesOutsideLogicalMask}; "
                + $"diagnostics {plan.VisualDiagnostics.Count}");

            Line("");
            Line("-- A: the component R24 already locked --");
            ReportA(live);

            Line("");
            Line("-- B/C/D: which live raw families are candidates, and do they collide? --");
            Candidates(live);

            Line("");
            Line("-- exhaustive local-differentiation search --");
            ExhaustiveSearch(live, grid);

            Line("");
            Line("-- the 0xD0 collision, proven cell by cell --");
            CollisionD0(live, grid);

            Line("");
            Line("-- what the live map does NOT tell us --");
            Line("   The card names four sprites and four positions the user pointed at, but the");
            Line("   positions are given as screenshot arrows and this file has no screenshot. The live");
            Line("   map proves which topologies EXIST; it cannot prove which cell the user MEANT.");
            Line("   Naming a role before that is answered would be inferring a semantic from a guess.");

            Line("");
            Line("-- reachability of every already-locked Hills component --");
            Reachability(live);

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            Finish();
        }

        private sealed class Cell
        {
            public int X;
            public int Y;
            public int Raw;
            public string Role;
            public string Slot;
            public string Sprite;
        }

        private static void ReportA(List<Cell> live)
        {
            var a = live.Where(c => c.Sprite == "Hills_r2c4").ToList();
            foreach (Cell c in a)
            {
                Line($"   A at ({c.X},{c.Y})  raw 0x{c.Raw:X2}  role {c.Role}  slot {c.Slot}  "
                    + $"sprite {c.Sprite}   {Bits(c.Raw)}");
            }

            Check("A_IS_R2C4_FROM_LOCAL_3X3_ONLY", a.Count == 1,
                a.Count == 1
                    ? $"A is ({a[0].X},{a[0].Y}) raw 0x{a[0].Raw:X2} = N,E,S,SE raised; NW,NE,W,SW "
                        + "open. R24 proved this from the cell's own 3x3 with no length and no "
                        + "lookahead, and it still holds"
                    : $"{a.Count} live cells draw r2c4; expected exactly 1");

            // The family A selects, for the joint comparison.
            var family = Enumerable.Range(0, 256).Where(IsFamilyA).ToList();
            Line($"   A's raw family under the R24 predicate: "
                + $"{string.Join(", ", family.Select(r => "0x" + r.ToString("X2")))}");
        }

        /// <summary>The R24 predicate, re-declared from the six named bits, in production order.</summary>
        private static bool IsFamilyA(int raw)
        {
            return !Has(raw, B_W)
                && Has(raw, B_N)
                && Has(raw, B_S)
                && Has(raw, B_E)
                && Has(raw, B_SE)
                && !Has(raw, B_NE);
        }

        /// <summary>
        /// The same handover read on the OTHER side: the vertical boundary is on the EAST, so W is
        /// raised and E is open, and the mass steps away north-west while holding south-east.
        ///
        /// This is the ONLY mirror candidate derivable from A's own topology: swap the W and E clauses
        /// and the SW and SE clauses. It is a CANDIDATE, not a rule, and it is tested below for
        /// over-capture before anything is believed.
        /// </summary>
        private static bool IsFamilyB(int raw)
        {
            return !Has(raw, B_E)
                && Has(raw, B_N)
                && Has(raw, B_S)
                && Has(raw, B_W)
                && Has(raw, B_SW)
                && !Has(raw, B_NW);
        }

        /// <summary>
        /// A's family with the N bit dropped: the vertical boundary does NOT continue north, so this is
        /// where the boundary ENDS and hands over. That is exactly the pair R21 recorded as
        /// CORNER_NO_UPPER_CONTINUATION (r3c5) against CORNER_WITH_UPPER_CONTINUATION (r2c4), with north
        /// occupancy as the sole discriminator.
        /// </summary>
        private static bool IsFamilyC(int raw)
        {
            return !Has(raw, B_W)
                && !Has(raw, B_N)
                && Has(raw, B_S)
                && Has(raw, B_E)
                && Has(raw, B_SE)
                && !Has(raw, B_NE);
        }

        /// <summary>B's family with the N bit dropped.</summary>
        private static bool IsFamilyD(int raw)
        {
            return !Has(raw, B_E)
                && !Has(raw, B_N)
                && Has(raw, B_S)
                && Has(raw, B_W)
                && Has(raw, B_SW)
                && !Has(raw, B_NW);
        }

        private static void Candidates(List<Cell> live)
        {
            var fams = new (string Label, Func<int, bool> Pred)[]
            {
                ("A r2c4 (locked by R24)", IsFamilyA),
                ("B r2c7 (mirror of A)", IsFamilyB),
                ("C r3c5 (A family, N dropped)", IsFamilyC),
                ("D r3c6 (B family, N dropped)", IsFamilyD),
            };

            foreach ((string label, Func<int, bool> pred) in fams)
            {
                var masks = Enumerable.Range(0, 256).Where(pred).OrderBy(r => r).ToList();
                var hits = live.Where(c => pred(c.Raw)).ToList();

                Line("");
                Line($"   CANDIDATE {label}");
                Line($"      matched raw masks ({masks.Count}): "
                    + string.Join(", ", masks.Select(r => "0x" + r.ToString("X2"))));
                Line($"      live cells matched: {hits.Count}");
                foreach (Cell c in hits)
                {
                    Line($"         ({c.X},{c.Y}) raw 0x{c.Raw:X2} currently {c.Sprite} "
                        + $"as {c.Role}/{c.Slot}");
                }

                Line($"      the six distinguishing bits, A vs this family:");
                var refBits = masks.Count > 0 ? masks[0] : 0;
                Line($"         NW{On(refBits, B_NW)} N{On(refBits, B_N)} NE{On(refBits, B_NE)} | "
                    + $"W{On(refBits, B_W)} E{On(refBits, B_E)} | SW{On(refBits, B_SW)} "
                    + $"S{On(refBits, B_S)} SE{On(refBits, B_SE)}");
            }

            // The joint bit comparison the card asks for, between the four families' core masks.
            int maskA = 0xD2;
            int maskB = FirstOr(0, IsFamilyB);
            int maskC = FirstOr(0, IsFamilyC);
            int maskD = FirstOr(0, IsFamilyD);
            Line("");
            Line("-- JOINT COMPARISON of the four families' core masks --");
            Line($"   A=0x{maskA:X2} {Bits(maskA)}");
            Line($"   B=0x{maskB:X2} {Bits(maskB)}");
            Line($"   C=0x{maskC:X2} {Bits(maskC)}");
            Line($"   D=0x{maskD:X2} {Bits(maskD)}");
            Line($"   A vs B differing bits: {Diff(maskA, maskB)}");
            Line($"   C vs D differing bits: {Diff(maskC, maskD)}");
            Line($"   A vs C differing bits: {Diff(maskA, maskC)}");
            Line($"   B vs D differing bits: {Diff(maskB, maskD)}");
            Line($"   A vs B is a strict mirror: {IsMirror(maskA, maskB)}");
            Line($"   C vs D is a strict mirror: {IsMirror(maskC, maskD)}");
        }

        private static int FirstOr(int fallback, Func<int, bool> pred)
        {
            for (int r = 0; r <= 255; r++)
            {
                if (pred(r))
                {
                    return r;
                }
            }

            return fallback;
        }

        private static string Diff(int a, int b)
        {
            var names = new (int Bit, string Name)[]
            {
                (B_NW, "NW"), (B_N, "N"), (B_NE, "NE"), (B_W, "W"),
                (B_E, "E"), (B_SW, "SW"), (B_S, "S"), (B_SE, "SE"),
            };

            var differing = names
                .Where(n => Has(a, n.Bit) != Has(b, n.Bit))
                .Select(n => $"{n.Name}:{On(a, n.Bit)}->{On(b, n.Bit)}")
                .ToList();

            return differing.Count == 0 ? "IDENTICAL" : string.Join(" ", differing);
        }

        /// <summary>
        /// A strict logical mirror: every west-ish bit maps to its east-ish counterpart and vice versa,
        /// with N, S unchanged. This describes OCCUPANCY ONLY and never implies mirroring any Sprite.
        /// </summary>
        private static bool IsMirror(int left, int right)
        {
            return Has(left, B_W) == Has(right, B_E)
                && Has(left, B_E) == Has(right, B_W)
                && Has(left, B_NW) == Has(right, B_NE)
                && Has(left, B_NE) == Has(right, B_NW)
                && Has(left, B_SW) == Has(right, B_SE)
                && Has(left, B_SE) == Has(right, B_SW)
                && Has(left, B_N) == Has(right, B_N)
                && Has(left, B_S) == Has(right, B_S);
        }

        /// <summary>
        /// The collision that decides this card. Family C is A minus the N bit, i.e. raw 0xD0 in its
        /// simplest form. This shows EVERY live cell with that raw, and whether it is a junction or an
        /// ordinary corner whose art must not move.
        /// </summary>
        /// <summary>
        /// The exhaustive local-differentiation search for the three components the card does not locate.
        ///
        /// The method is to enumerate, for every live cell, its RAW 3x3 together with the piece it
        /// currently draws. That table is the complete set of facts available: if a component's candidate
        /// cells all share one raw with cells that must keep their art, then no 3x3 rule can separate
        /// them, and card section 21 STOP condition 2 is met.
        /// </summary>
        private static void ExhaustiveSearch(List<Cell> live, TerrainGridData grid)
        {
            Line("");
            Line("-- exhaustive: every live raw 3x3, with the piece it currently draws --");
            var table = live.GroupBy(c => c.Raw)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Raw = g.Key,
                    Cells = g.ToList(),
                    Sprites = g.Select(v => v.Sprite).Distinct()
                        .OrderBy(s => s, StringComparer.Ordinal).ToList(),
                })
                .ToList();

            foreach (var row in table)
            {
                Line($"   raw 0x{row.Raw:X2}  x{row.Cells.Count,-2} "
                    + $"{string.Join(" ", row.Cells.Select(v => $"({v.X},{v.Y})")),-28} "
                    + $"-> {string.Join(", ", row.Sprites),-22} {Bits(row.Raw)}");
            }

            Line("");
            Line("-- any live raw that currently draws MORE THAN ONE sprite? --");
            var mixed = table.Where(r => r.Sprites.Count > 1).ToList();
            foreach (var row in mixed)
            {
                Line($"   raw 0x{row.Raw:X2} draws {string.Join(" AND ", row.Sprites)}");
                foreach (var v in row.Cells)
                {
                    Line($"      ({v.X},{v.Y}) draws {v.Sprite}");
                    Neighbourhood(grid, v.X, v.Y);
                }
            }

            Check("NO_LIVE_RAW_SPLITS_TWO_DIFFERENT_SPRITES", mixed.Count == 0,
                mixed.Count == 0
                    ? "no live raw 3x3 currently draws two different Sprites, so the Sprite is still a "
                        + "function of the raw alone on the user's own map"
                    : $"{mixed.Count} live raw mask(es) draw two different Sprites, which is a genuine "
                        + "local ambiguity in the CURRENT core and is listed above cell by cell");

            Line("");
            Line("-- the live families the three UNLOCATED components could plausibly be --");
            Line("   The card gives no coordinate for r2c7, r3c5 or r3c6, only that the user pointed at");
            Line("   them. So the honest question is which live families exist at all:");
            foreach ((string label, Func<int, bool> pred) in new (string, Func<int, bool>)[]
            {
                ("r2c7 mirror of A", IsFamilyB),
                ("r3c5 A minus N", IsFamilyC),
                ("r3c6 B minus N", IsFamilyD),
            })
            {
                var masks = Enumerable.Range(0, 256).Where(pred).OrderBy(r => r).ToList();
                var hits = live.Where(c => pred(c.Raw)).ToList();
                Line($"   {label,-20} masks {string.Join(",", masks.Select(r => "0x" + r.ToString("X2")))}"
                    + $"  live matches {hits.Count} "
                    + $"[{string.Join(" ", hits.Select(h => $"({h.X},{h.Y})={h.Sprite}"))}]");
            }

            Line("");
            Line("-- the other three corners of the live structure that contains A --");
            Line("   (0,-10) is A = r2c4, already locked. Its neighbours in the same ring are:");
            foreach (Cell c in live.Where(c =>
                         (c.X == 8 && c.Y == -10) || (c.X == 8 && c.Y == -5) || (c.X == 0 && c.Y == -6)))
            {
                Line($"   ({c.X},{c.Y}) raw 0x{c.Raw:X2} {c.Sprite} as {c.Role}/{c.Slot}  {Bits(c.Raw)}");
            }
        }

        private static void CollisionD0(List<Cell> live, TerrainGridData grid)
        {
            int d0 = (1 << B_E) | (1 << B_S) | (1 << B_SE);
            var hits = live.Where(c => c.Raw == d0).ToList();

            Line($"   raw 0x{d0:X2} = {Bits(d0)}");
            Line($"   live cells with exactly this raw: {hits.Count}");
            foreach (Cell c in hits)
            {
                Neighbourhood(grid, c.X, c.Y);
                Line($"      ({c.X},{c.Y}) currently {c.Sprite} as {c.Role}/{c.Slot}");
            }

            Check("D0_FAMILY_OVERLAPS_TWO_DIFFERENT_MEANINGS", hits.Count <= 1,
                hits.Count <= 1
                    ? "at most one live cell carries this raw, so the family is unambiguous"
                    : $"{hits.Count} live cells share the IDENTICAL full raw 3x3 0x{d0:X2} "
                        + "({Bits(d0)}). If one of them is the user's r3c5 position and another must keep "
                        + "the author's r0c0 left terminal, then NO 3x3-only rule can separate them, and "
                        + "that is card section 21 STOP condition 2. The per-cell neighbourhoods printed "
                        + "above are the evidence: they are genuinely different shapes, so the difference "
                        + "lies OUTSIDE the 3x3");

            // And the same check for a plain rectangle, which is the R11-locked comparison case.
            Line("");
            Line("-- is the 3x3 rectangle's top-left cell the same raw as family C's core? --");
            var rect = new[] { "XXX", "XXX", "XXX" };
            var rg = MaskGrid(rect);
            int rectTopLeft = Encode(rg, OriginOf(rg, rect, 2, 0), 0);
            Line($"   3x3 rectangle top-left raw = 0x{rectTopLeft:X2}   {Bits(rectTopLeft)}");
            Line($"   family C core raw          = 0x{d0:X2}   {Bits(d0)}");
            Check("RECTANGLE_TOP_LEFT_IS_NOT_IDENTICAL_TO_FAMILY_C",
                rectTopLeft != d0,
                rectTopLeft != d0
                    ? "they differ, so the junction family does not collide with the rectangle corner"
                    : "IDENTICAL: the 3x3 rectangle's top-left cell and the family C core have the same "
                        + "full raw 3x3. The rectangle is R11-locked to the author's r0c0 and the card "
                        + "section 9 protects r0c0 from over-capture, so one 3x3-only rule cannot "
                        + "satisfy both");
        }

        private static void Neighbourhood(TerrainGridData grid, int cx, int cy)
        {
            Line($"      cells around ({cx},{cy}), '#' Raised, '.' open, north row first:");
            for (int dy = 2; dy >= -2; dy--)
            {
                var sb = new StringBuilder("         ");
                for (int dx = -2; dx <= 2; dx++)
                {
                    sb.Append(RaisedNeighborResolver.IsRaised(grid, cx + dx, cy + dy) ? '#' : '.');
                }

                Line(sb.ToString());
            }
        }

        private static TerrainGridData MaskGrid(string[] mask)
        {
            int h = mask.Length;
            int w = 0;
            foreach (string r in mask)
            {
                w = Math.Max(w, r.Length);
            }

            int ox = 2;
            int oy = 2;
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
                    if (mask[i][x] == 'X')
                    {
                        g.SetElevation(ox + x, y, ElevationLevel.Raised);
                    }
                }
            }

            return g;
        }

        private static int OriginOf(TerrainGridData g, string[] mask, int row, int col)
        {
            return g.OriginX + col;
        }

        private static void Reachability(List<Cell> live)
        {
            var locked = new (string Sprite, string Provenance)[]
            {
                ("Hills_r3c3", "R18 1x1"),
                ("Hills_r0c3", "R18 narrow cap"),
                ("Hills_r1c3", "R18 narrow body"),
                ("Hills_r2c3", "R18 narrow front"),
                ("Hills_r3c0", "R18 band left terminal"),
                ("Hills_r3c1", "R18 band body"),
                ("Hills_r3c2", "R18 band right terminal"),
                ("Hills_r0c0", "R11 rectangle top-left"),
                ("Hills_r0c1", "R11 rectangle top body"),
                ("Hills_r0c2", "R11 rectangle top-right"),
                ("Hills_r1c0", "R11 rectangle body-left"),
                ("Hills_r1c1", "R11 rectangle body"),
                ("Hills_r1c2", "R11 rectangle body-right"),
                ("Hills_r2c0", "R11 rectangle front-left"),
                ("Hills_r2c1", "R11 rectangle front body"),
                ("Hills_r2c2", "R11 rectangle front-right"),
                ("Hills_r3c4", "R19 left corner, user confirmed"),
                ("Hills_r3c7", "R19 right corner, user confirmed"),
                ("Hills_r0c4", "R20 left top corner"),
                ("Hills_r0c7", "R20 right top corner"),
                ("Hills_r2c4", "R24 junction, user confirmed"),
            };

            Line("   SPRITE                 LIVE-CELL REACHABLE   PROVENANCE");
            int unreachable = 0;
            var missing = new List<string>();
            foreach ((string sprite, string prov) in locked)
            {
                int n = live.Count(c => c.Sprite == sprite);
                bool reachable = n > 0;
                if (!reachable)
                {
                    unreachable++;
                    missing.Add(sprite);
                }

                Line($"   {sprite,-22} {(reachable ? "YES" : "no "),-20} {prov}   (live cells {n})");
            }

            Check("LOCKED_COMPONENTS_REACHABLE_ON_LIVE_MAP", unreachable == 0,
                unreachable == 0
                    ? $"all {locked.Length} already-locked components are emitted by at least one live "
                        + "cell, so none is locked-but-unreachable"
                    : $"{unreachable} locked component(s) are emitted by NO live cell: "
                        + string.Join(", ", missing)
                        + ". A component that no topology can reach must not be reported as complete");
        }

        private static void Finish()
        {
            Line("");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R25_junction_audit.txt"), sb.ToString());
        }
    }
}
