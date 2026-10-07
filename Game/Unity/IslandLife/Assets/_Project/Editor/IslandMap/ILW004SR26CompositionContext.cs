// IL-WORLD-004S-R26 stage A - composition context audit. DIAGNOSTIC ONLY.
//
// THE QUESTION. R25 proved that raw 0xD0 occurs both at a plain rectangle corner (where R11 locks the
// author's r0c0) and at what the user says is a composition site. The live map has since been edited by
// the user and now contains only ONE 0xD0 cell, which makes the collision look resolved. This file
// checks whether it is actually resolved, or merely no longer visible because the other corners were
// deleted. A deleted counterexample is not a resolved counterexample.
//
// It also counts how many distinct composition SITES the live map actually offers, against the 11
// components the card locks, because a rule cannot be written for a site that does not exist.
//
// BIT ORDER (production): NW 0x01 N 0x02 NE 0x04 W 0x08 E 0x10 SW 0x20 S 0x40 SE 0x80
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
    public static class ILW004SR26CompositionContext
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R26";

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
            Debug.Log("[R26A] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R26A Composition Context Audit")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R26A composition context audit (DIAGNOSTIC) ===");

            TerrainMapData data = AssetDatabase.LoadAssetAtPath<TerrainMapData>(RealDataPath);
            if (data == null)
            {
                Line("   cannot load the live map");
                Finish();
                return;
            }

            TerrainGridData grid = data.CreateGridData();
            Line($"   live Raised cells {RaisedRegionAnalyzer.CountRaisedCells(grid)}");

            Line("");
            Line("-- 1: every live cell whose raw 3x3 is a CROSS or a HANDOVER candidate --");
            Candidates(grid);

            Line("");
            Line("-- 2: is R25's 0xD0 collision really resolved, or only hidden? --");
            D0Resolved(grid);

            Line("");
            Line("-- 3: how many distinct composition SITES does the live map offer? --");
            SiteCount(grid);

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            Finish();
        }

        /// <summary>
        /// Composition-shaped raws, named by their OWN BITS rather than by any sprite or shape name.
        /// A handover is a solid arm on one side with the opposite side open; a cross is all four
        /// cardinals solid with no diagonal solid.
        /// </summary>
        private static bool IsHandover(int raw)
        {
            bool westOpen = !Has(raw, B_W);
            bool eastOpen = !Has(raw, B_E);
            return (westOpen && Has(raw, B_E)) || (eastOpen && Has(raw, B_W));
        }

        private static bool IsCross(int raw)
        {
            return Has(raw, B_N) && Has(raw, B_S) && Has(raw, B_W) && Has(raw, B_E)
                && !Has(raw, B_NW) && !Has(raw, B_NE) && !Has(raw, B_SW) && !Has(raw, B_SE);
        }

        private static void Candidates(TerrainGridData grid)
        {
            var found = new List<(int X, int Y, int Raw, string Kind)>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(grid, x, y);
                    string kind = null;
                    if (IsCross(raw))
                    {
                        kind = "CROSS";
                    }
                    else if (IsHandover(raw))
                    {
                        kind = "HANDOVER";
                    }

                    if (kind != null)
                    {
                        found.Add((x, y, raw, kind));
                    }
                }
            }

            Line($"   live cells that are a CROSS or a HANDOVER: {found.Count}");
            foreach ((int x, int y, int raw, string kind) in found)
            {
                Line($"   ({x},{y}) {kind,-9} raw 0x{raw:X2}   {Bits(raw)}");
                Neighbourhood(grid, x, y);
            }

            Check("LIVE_COMPOSITION_SITE_COUNT", found.Count > 0,
                $"{found.Count} candidate composition site(s) exist on the live map right now");
        }

        /// <summary>
        /// THE CENTRAL QUESTION OF THIS CARD. R25 found raw 0xD0 at five live cells, four of them plain
        /// block corners needing r0c0. The user then edited the map and only one 0xD0 cell remains. This
        /// rebuilds the deleted counterexample from a synthetic R11-locked 3x3 rectangle - which is
        /// deterministic and does not depend on the user's map at all - and compares its 3x3 against the
        /// surviving user site.
        ///
        /// If the two are identical at 3x3, the collision was never resolved; it was only moved out of
        /// view. Deleting the evidence is not the same as proving the rule cannot over-capture.
        /// </summary>
        private static void D0Resolved(TerrainGridData grid)
        {
            // The surviving user site.
            var user = new List<(int X, int Y, int Raw)>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (RaisedNeighborResolver.IsRaised(grid, x, y)
                        && Encode(grid, x, y) == 0xD0)
                    {
                        user.Add((x, y, 0xD0));
                    }
                }
            }

            foreach ((int x, int y, int raw) in user)
            {
                Line($"   live user site with raw 0xD0: ({x},{y})");
                Neighbourhood(grid, x, y);
            }

            // The R11-locked counterexample, rebuilt from the locked fixture itself.
            var rect = MaskGrid(new[] { "XXX", "XXX", "XXX" });
            int topLeftX = rect.OriginX;
            int topLeftY = rect.OriginY + 2;
            int rectRaw = Encode(rect, topLeftX, topLeftY);
            Line($"   R11-locked 3x3 rectangle top-left: ({topLeftX},{topLeftY}) raw 0x{rectRaw:X2}");
            Neighbourhood(rect, topLeftX, topLeftY);

            Check("R25_D0_COLLISION_RESOLVED_OR_MOVED_ASIDE",
                rectRaw != 0xD0 || user.Count == 0,
                rectRaw == 0xD0 && user.Count > 0
                    ? $"STILL A COLLISION, and the map edit only hid it. The user's composition site "
                        + $"({user[0].X},{user[0].Y}) and the R11-locked 3x3 rectangle's top-left "
                        + $"({topLeftX},{topLeftY}) have the IDENTICAL raw 3x3 0x{rectRaw:X2}. The "
                        + "rectangle is R11-locked to the author's r0c0 and card section 11 protects "
                        + "r0c0. Deleting the four other corners removed the visible copies, not the "
                        + "conflict, and a rule for 0xD0 would still break every 3x3 rectangle"
                    : user.Count == 0
                        ? "no live 0xD0 cell remains at all, so the user's composition site is not on "
                            + "the map right now"
                        : $"the rectangle's top-left raw is 0x{rectRaw:X2}, which differs from 0xD0");

            // Can a 5x5 read separate them? Answered honestly, including whether the separator is
            // explainable or arbitrary.
            Line("");
            Line("-- can a finite 5x5 context separate the two? --");
            if (user.Count > 0 && rectRaw == 0xD0)
            {
                var five = new List<string>();
                for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
                {
                    for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                    {
                        if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                        {
                            continue;
                        }

                        if (Encode(grid, x, y) == 0xD0)
                        {
                            five.Add(Stamp5(grid, x, y));
                        }
                    }
                }

                five.Add(Stamp5(rect, topLeftX, topLeftY));

                Line($"   user site 5x5 stamp   : {five[0]}");
                for (int i = 1; i < five.Count; i++)
                {
                    Line($"   rectangle 5x5 stamp   : {five[i]}");
                }

                Line($"   stamps differ: {five[0] != five[1]}");
                Line("   A differing 5x5 stamp means a finite context COULD separate them. It does NOT");
                Line("   mean a rule exists: the separator still has to be stated as a named, finite");
                Line("   predicate, and the card forbids a mask pile or a nearest match.");
            }
        }

        /// <summary>
        /// A 5x5 occupancy stamp, row by row, north row first. Used to compare two contexts without
        /// asserting that either one means anything.
        /// </summary>
        private static string Stamp5(TerrainGridData g, int cx, int cy)
        {
            var sb = new StringBuilder();
            for (int dy = 2; dy >= -2; dy--)
            {
                if (dy != 2)
                {
                    sb.Append(" / ");
                }

                for (int dx = -2; dx <= 2; dx++)
                {
                    sb.Append(RaisedNeighborResolver.IsRaised(g, cx + dx, cy + dy) ? '#' : '.');
                }
            }

            return sb.ToString();
        }

        private static void Neighbourhood(TerrainGridData grid, int cx, int cy)
        {
            Line($"      5x5 around ({cx},{cy}), '#' Raised, north row first:");
            for (int dy = 2; dy >= -2; dy--)
            {
                var sb = new StringBuilder("        ");
                for (int dx = -2; dx <= 2; dx++)
                {
                    sb.Append(RaisedNeighborResolver.IsRaised(grid, cx + dx, cy + dy) ? '#' : '.');
                }

                Line(sb.ToString());
            }
        }

        /// <summary>
        /// Counts DISTINCT composition topologies the live map offers, against the 11 components the card
        /// locks. Distinct TOPOLOGIES is the right unit, not distinct cells: a rule selects a topology,
        /// so N components need N distinct topologies to exist somewhere.
        /// </summary>
        private static void SiteCount(TerrainGridData grid)
        {
            var topo = new SortedDictionary<int, List<string>>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(grid, x, y);
                    if (!IsCross(raw) && !IsHandover(raw))
                    {
                        continue;
                    }

                    if (!topo.TryGetValue(raw, out List<string> l))
                    {
                        l = new List<string>();
                        topo[raw] = l;
                    }

                    l.Add($"({x},{y})");
                }
            }

            Line($"   distinct composition TOPOLOGIES on the live map: {topo.Count}");
            foreach (KeyValuePair<int, List<string>> kv in topo)
            {
                Line($"   raw 0x{kv.Key:X2}  {string.Join(" ", kv.Value),-22} "
                    + $"{(IsCross(kv.Key) ? "CROSS" : "HANDOVER"),-9} {Bits(kv.Key)}");
            }

            // Cells adjacent to a cross, which are where an "upper" composition would sit: a column cell
            // whose two side diagonals are solid, i.e. ground passes by on both sides of it.
            Line("");
            Line("-- cells flanking a cross / handover: candidates for an 'upper' composition --");
            var flanks = new List<string>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(grid, x, y);
                    bool flanksSouth = (Has(raw, B_SW) && Has(raw, B_SE) && !Has(raw, B_S));
                    bool flanksNorth = (Has(raw, B_NW) && Has(raw, B_NE) && !Has(raw, B_N));
                    if (flanksSouth || flanksNorth)
                    {
                        flanks.Add($"({x},{y}) raw 0x{raw:X2} {(flanksNorth ? "flanked N" : "flanked S")}");
                    }
                }
            }

            foreach (string s in flanks)
            {
                Line("   " + s);
            }

            Line($"   flanking candidates: {flanks.Count}");
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

        private static void Finish()
        {
            Line("");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R26A_composition_context.txt"), sb.ToString());
        }
    }
}