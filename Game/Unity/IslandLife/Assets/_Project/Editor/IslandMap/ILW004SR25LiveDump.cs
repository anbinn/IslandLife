// IL-WORLD-004S-R25 stage 1 - READ ONLY dump of every live FirstIsland Raised cell.
//
// WHY THIS FILE EXISTS ALONE. The card locks four USER_VISUAL_ORACLE components (r2c4, r2c7, r3c5,
// r3c6) but it deliberately does NOT name the semantic role, and it forbids guessing a role name from a
// sprite's sheet row or column. So the first job is FACTS: every live Raised cell, its raw 3x3 read in
// PRODUCTION bit order, its canonical mask, its current role, slot and sprite. The four junction cells
// are then identified by their topology, and only then named.
//
// BIT ORDER (production, from RaisedNeighborResolver.NeighbourOffsets):
//   NW 0x01  N 0x02  NE 0x04  W 0x08  E 0x10  SW 0x20  S 0x40  SE 0x80
// Every mask printed here is DECODED with this table and every fixture is read back OUT of the grid, so
// the R23B harness's N-first slip cannot recur.
//
// READ ONLY. No paint, no undo, no scene open, no AssetDatabase write of any kind. This file only
// creates an in-memory grid from the asset and resolves a projection against it.
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
    public static class ILW004SR25LiveDump
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

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R25DUMP] " + s);
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

        [MenuItem("IslandLife/Diagnostics/R25 Live Cell Dump")]
        public static void Run()
        {
            Lines.Clear();
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R25 live FirstIsland cell dump (READ ONLY) ===");
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

            Line($"   grid {grid.Width}x{grid.Height} from ({grid.OriginX},{grid.OriginY}); "
                + $"Raised cells {RaisedRegionAnalyzer.CountRaisedCells(grid)}; "
                + $"visual tiles {plan.Tiles.Count}; outside {plan.TilesOutsideLogicalMask}; "
                + $"diagnostics {plan.VisualDiagnostics.Count}");

            Line("");
            Line("-- the map itself, '#' Raised, one line per row, north row FIRST --");
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                }
            }

            Line($"   Raised bounding box x {minX}..{maxX}, y {minY}..{maxY}");
            Line($"" + new string(' ', 10) + "  x " + minX + " .. " + maxX);
            for (int y = maxY; y >= minY; y--)
            {
                var sb = new StringBuilder();
                sb.Append("   y=").Append(y.ToString().PadLeft(4)).Append(' ');
                for (int x = minX; x <= maxX; x++)
                {
                    sb.Append(RaisedNeighborResolver.IsRaised(grid, x, y) ? '#' : '.');
                }

                Line(sb.ToString());
            }

            Line("");
            Line("-- EVERY live Raised cell: raw 3x3, canonical, role, slot, sprite --");
            Line("   (x,y)      raw   canon  role                     slot            sprite");

            var rows = new List<string>();
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

                    rows.Add($"   ({x},{y})".PadRight(11)
                        + $" 0x{raw:X2}  0x{(byte)st.CanonicalMask:X2}  "
                        + $"{st.Role.ToString(),-22} {st.Slot.ToString(),-14} {sprite,-12} {Bits(raw)}");
                }
            }

            foreach (string r in rows)
            {
                Line(r);
            }

            // Group by raw so the four junction families are visible as groups, not as one long list.
            Line("");
            Line("-- the same cells GROUPED BY RAW MASK, with how many live cells share each --");
            var grouped = new SortedDictionary<int, List<string>>(Comparer<int>.Default);
            foreach (string r in rows)
            {
                var parts = r.Split(new[] { " 0x" }, StringSplitOptions.None);
                _ = parts;
            }

            var byRaw = new SortedDictionary<int, List<string>>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        continue;
                    }

                    int raw = Encode(grid, x, y);
                    if (!byRaw.TryGetValue(raw, out List<string> l))
                    {
                        l = new List<string>();
                        byRaw[raw] = l;
                    }

                    l.Add($"({x},{y})");
                }
            }

            foreach (KeyValuePair<int, List<string>> kv in byRaw)
            {
                Line($"   raw 0x{kv.Key:X2} ({kv.Key,3}) x{kv.Value.Count,-3} "
                    + $"{string.Join(" ", kv.Value)}   {Bits(kv.Key)}");
            }

            Finish();
        }

        private static void Finish()
        {
            Line("");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R25_live_cell_dump.txt"), sb.ToString());
        }
    }
}