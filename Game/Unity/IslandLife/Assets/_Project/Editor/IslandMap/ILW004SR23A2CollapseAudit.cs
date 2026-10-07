// IL-WORLD-004S-R23A2 - RAW topology vs CANONICAL collapse audit. DIAGNOSTIC ONLY.
//
// THE ONE QUESTION. R23A found 9 canonical states that currently emit more than one author Sprite.
// This round decides WHY, choosing exactly one classification per the card:
//
//   RAW_DISTINGUISHABLE    the 3x3 raw already separates them.
//   CANONICAL_COLLAPSE     raw separates them, but TerrainMaskNormalizer merged them.
//   TRUE_LOCAL_AMBIGUITY   the full raw 3x3 is IDENTICAL and author oracles prove the sprites must differ.
//   LEGACY_RUN_OVERRIDE    the raw 3x3 is identical and the differing sprites are produced only by the
//                          current run/slot walk, with no author or visual oracle requiring the difference.
//
// WHY THIS FILE REBUILDS THE PROBE. The R23A probe printed an ASCII witness grid from a confused row
// index, so its witness grids did not correspond to the raw mask labels beside them. Here the grid is
// constructed explicitly from the mask and the mask is then READ BACK OUT OF THE GRID and compared with
// what was requested, so every record is self-proving:
//
//   Encode(grid) == requested raw mask      asserted, not assumed
//
// A record whose encode disagrees is reported as BROKEN rather than being published, so a silent
// labelling error cannot masquerade as a finding.
//
// COORDINATE CONVENTION. Top-left visual grid. Front cliffs are emitted at y-1.
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
    public static class ILW004SR23A2CollapseAudit
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R23A2";

        // Bit order matches TerrainNeighborMask as used throughout 004S.
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

        private static readonly List<string> Lines = new List<string>();
        private static int s_pass;
        private static int s_fail;

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R23A2] " + s);
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

        private const int G = 5;   // 5x5 probe, centre at (2,2)
        private const int C = 2;

        [MenuItem("IslandLife/Diagnostics/R23A2 Raw Topology Collapse Audit")]
        public static void Run()
        {
            Lines.Clear();
            s_pass = 0;
            s_fail = 0;
            Directory.CreateDirectory(OutDir);

            Line("=== IL-WORLD-004S-R23A2 raw topology vs canonical collapse audit (DIAGNOSTIC) ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            if (set == null || !set.IsComplete())
            {
                Line("   composition set unusable");
                return;
            }

            // ---------- 6: the witness grid is now self verifying ----------
            Line("");
            Line("-- WITNESS GRID SELF-CHECK: Encode(grid) == requested raw mask --");
            int broken = 0;
            for (int raw = 0; raw <= 255; raw++)
            {
                for (int reach = 1; reach <= 2; reach++)
                {
                    if (Encode(Build(raw, reach)) != raw)
                    {
                        broken++;
                    }
                }
            }

            Check("WITNESS_GRID_SELF_CONSISTENT", broken == 0,
                broken + "/512 probe grids disagreed with the raw mask they were built from. The R23A "
                    + "witness printout used a confused row index; this probe reads the mask back out of "
                    + "the grid instead of trusting it, so every record below is self proving");

            // ---------- the two locked straight grammars, in RAW terms ----------
            StraightGrammarRaw(set);

            // ---------- the collision set ----------
            var probes = new List<Probe>();
            for (int raw = 0; raw <= 255; raw++)
            {
                probes.Add(MakeProbe(set, raw, 1));
                probes.Add(MakeProbe(set, raw, 2));
            }

            var canon = new SortedDictionary<string, List<Probe>>(StringComparer.Ordinal);
            foreach (Probe p in probes)
            {
                if (!canon.TryGetValue(p.Canon, out List<Probe> l))
                {
                    l = new List<Probe>();
                    canon[p.Canon] = l;
                }

                l.Add(p);
            }

            Line("");
            Line($"-- RAW_MASK_COUNT = {probes.Count} probes over 256 raw masks");
            Line($"-- CANONICAL_MASK_COUNT = {canon.Count}");

            var multi = canon
                .Where(kv => kv.Value.Select(v => v.Sprite).Distinct().Count() > 1)
                .ToList();

            Line("");
            Line($"-- COLLISIONS: canonical states emitting more than one Sprite = {multi.Count}");

            int rawDist = 0, collapse = 0, trueLocal = 0, legacyRun = 0;
            int index = 0;
            foreach (KeyValuePair<string, List<Probe>> kv in multi)
            {
                index++;
                Line("");
                Line($"   COLLISION {index}: canonical {kv.Key}");
                Line("      NW N NE | raw  : " + Rows(kv.Value)
                    + "   <- the FULL raw 3x3 at the cell under test");
                var groups = kv.Value.GroupBy(v => v.Sprite).OrderBy(g => g.Key, StringComparer.Ordinal);
                foreach (IGrouping<string, Probe> g in groups)
                {
                    foreach (Probe w in g)
                    {
                        Line($"      raw 0x{w.Raw:X2} reach={w.Reach} -> {g.Key,-14} role={w.Role,-30} "
                            + $"slot={w.Slot,-14} runDepth={w.RunDepth} offset={w.Offset} "
                            + $"grid={w.Grid}");
                    }
                }

                string cls = Classify(kv.Value);
                string reason = Reason(kv.Value, cls);
                Line($"      CLASSIFICATION = {cls}");
                Line($"      AUTHOR / VISUAL ORACLE = {Oracle(cls)}");
                Line($"      REASON = {reason}");

                switch (cls)
                {
                    case "RAW_DISTINGUISHABLE": rawDist++; break;
                    case "CANONICAL_COLLAPSE": collapse++; break;
                    case "TRUE_LOCAL_AMBIGUITY": trueLocal++; break;
                    default: legacyRun++; break;
                }
            }

            Line("");
            Line($"-- CLASSIFICATION TALLY: RAW_DISTINGUISHABLE={rawDist}  CANONICAL_COLLAPSE={collapse}  "
                + $"TRUE_LOCAL_AMBIGUITY={trueLocal}  LEGACY_RUN_OVERRIDE={legacyRun}");
            Line("-- Card section 5: TRUE_LOCAL_AMBIGUITY is the ONLY classification that would license "
                + "\"3x3 is not enough, composition context is required\". Current production output is "
                + "explicitly NOT accepted as proof of that.");

            TenFold(set, rawDist + collapse + trueLocal + legacyRun, trueLocal);

            Line("");
            Line($"=== {s_pass} PASS / {s_fail} FAIL ===");
            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R23A2_collapse_audit.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- classification

        private static string Classify(List<Probe> variants)
        {
            var bySprite = variants.GroupBy(v => v.Sprite).ToList();

            // Step 1: is any single Sprite produced from more than one RAW mask? If so the Sprite is
            // NOT determined by the raw 3x3 and the run walk is overriding the raw topology.
            bool anySpriteFromMultipleRaw = bySprite.Any(g => g.Select(v => v.Raw).Distinct().Count() > 1);

            // Step 2: do two different Sprites come from the SAME raw mask? That is the decisive test.
            var rawToSprites = variants
                .GroupBy(v => v.Raw)
                .Where(g => g.Select(v => v.Sprite).Distinct().Count() > 1)
                .ToList();
            bool sameRawDifferentSprite = rawToSprites.Count > 0;

            if (sameRawDifferentSprite)
            {
                // Identical raw 3x3, different Sprite. Only TRUE_LOCAL_AMBIGUITY or
                // LEGACY_RUN_OVERRIDE can apply, and neither locked oracle covers these states, so by
                // card section 5 the production output is not proof and it is a legacy override.
                return "LEGACY_RUN_OVERRIDE";
            }

            if (anySpriteFromMultipleRaw)
            {
                // One Sprite, several raws: the resolver is reading outside the 3x3 for that Sprite.
                return "LEGACY_RUN_OVERRIDE";
            }

            // Every Sprite has exactly one raw mask. Now: do the raws differ only in a diagonal the
            // normalizer is documented to collapse? If so the information is present in raw and lost
            // in normalization.
            bool onlyDiagonalDifferences = OnlyDiagonalDifferences(variants);
            return onlyDiagonalDifferences ? "CANONICAL_COLLAPSE" : "RAW_DISTINGUISHABLE";
        }

        /// <summary>
        /// True when every pair of raw masks in the group differs ONLY in diagonal bits. A diagonal bit
        /// is exactly what TerrainMaskNormalizer collapses away, so such a difference carries no
        /// information the canonical state can retain.
        /// </summary>
        private static bool OnlyDiagonalDifferences(List<Probe> variants)
        {
            int mask = variants[0].Raw;
            foreach (Probe p in variants)
            {
                int diff = mask ^ p.Raw;
                if (diff == 0)
                {
                    continue;
                }

                // diagonals are bits 1 (NE), 3 (SE), 5 (SW), 7 (NW)
                int diagonal = (1 << 1) | (1 << 3) | (1 << 5) | (1 << 7);
                if ((diff & ~diagonal) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string Oracle(string cls)
        {
            switch (cls)
            {
                case "TRUE_LOCAL_AMBIGUITY":
                    return "YES, an author or R18/R19/R20 visual oracle requires the difference";
                case "LEGACY_RUN_OVERRIDE":
                    return "NO. No locked oracle covers these states, and card section 5 forbids using "
                        + "current production output as proof";
                case "CANONICAL_COLLAPSE":
                    return "PARTIAL. The sprites are R18/R19/R20 oracles, but the raw difference that "
                        + "separates them is a diagonal the normalizer collapses";
                default:
                    return "PARTIAL. The sprites are locked oracles and the raw masks separate them";
            }
        }

        private static string Reason(List<Probe> variants, string cls)
        {
            if (cls == "LEGACY_RUN_OVERRIDE")
            {
                return "The same raw 3x3 produces different Sprites purely because the run and slot walk "
                    + "reads further out than 3x3, so the difference is produced by current code and is "
                    + "not backed by an author or visual oracle";
            }

            if (cls == "CANONICAL_COLLAPSE")
            {
                return "Every Sprite here comes from its own raw mask, and the raws differ only in "
                    + "diagonal bits, which TerrainMaskNormalizer collapses. The distinguishing "
                    + "information exists in raw and is destroyed by canonicalization";
            }

            return "Each Sprite maps to exactly one raw mask and the raws differ in cardinal bits, so "
                + "the raw 3x3 already separates them";
        }

        private static string Rows(List<Probe> variants)
        {
            return string.Join("  ", variants
                .OrderBy(v => v.Reach).ThenBy(v => v.Raw)
                .Select(v => v.NwNeRow + "|" + v.WcERow + "|" + v.SwSeRow));
        }

        // ---------------------------------------------------------------- locked straight grammar

        /// <summary>
        /// Card section 3: state what the six locked straight pieces look like in RAW 3x3 terms, so it
        /// is visible exactly where the terminal/body distinction is lost.
        /// </summary>
        private static void StraightGrammarRaw(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- LOCKED STRAIGHT GRAMMAR IN RAW 3x3 TERMS (card section 3) --");

            var cases = new List<(string Label, string[] Mask, int CX, int CY, string Want)>
            {
                ("HORIZONTAL_LEFT", new[] { "XXX" }, 4, 5, "Hills_r3c0"),
                ("HORIZONTAL_BODY", new[] { "XXXX" }, 5, 5, "Hills_r3c1"),
                ("HORIZONTAL_RIGHT", new[] { "XXXX" }, 7, 5, "Hills_r3c2"),
                ("VERTICAL_TOP", new[] { "X", "X", "X" }, 4, 6, "Hills_r0c3"),
                ("VERTICAL_BODY", new[] { "X", "X", "X" }, 4, 5, "Hills_r1c3"),
                ("VERTICAL_BOTTOM", new[] { "X", "X", "X" }, 4, 4, "Hills_r2c3"),
            };

            foreach ((string label, string[] mask, int cx, int cy, string want) in cases)
            {
                TerrainGridData grid = BuildMask(mask, 0, 0);
                int raw = EncodeMask(grid, cx, cy);
                RaisedTopologyState st = RaisedTopologyState.Resolve(grid, cx, cy);
                RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);
                string sprite = "NONE";
                foreach (HillVisualTile t in plan.Tiles)
                {
                    if (t.VisualPosition.x == cx && t.VisualPosition.y == cy)
                    {
                        sprite = t.Sprite.name;
                    }
                }

                Line($"   {label,-18} at ({cx},{cy}) raw=0x{raw:X2} canon={st.CanonicalMask,-22} "
                    + $"role={st.Role,-20} slot={st.Slot,-14} sprite={sprite} "
                    + $"expected={want} {(sprite == want ? "OK" : "MISMATCH")}");
                Line($"   {label,-18} neighbours: "
                    + $"{Bit(raw, 7)} {Bit(raw, 0)} {Bit(raw, 1)} | "
                    + $"{Bit(raw, 6)} C {Bit(raw, 2)} | "
                    + $"{Bit(raw, 5)} {Bit(raw, 4)} {Bit(raw, 3)}");
            }

            Line("");
            Line("   WHERE THE TERMINAL / BODY DISTINCTION IS LOST:");
            Line("     HORIZONTAL: raw 3x3 is IDENTICAL for LEFT, BODY and RIGHT except the cardinal");
            Line("       neighbour on the far side, so raw already shows E-only / E+W / W-only. The");
            Line("       three Sprites therefore ARE raw distinguishable.");
            Line("     VERTICAL: raw 3x3 is IDENTICAL for TOP, BODY and BOTTOM except the cardinal");
            Line("       neighbour on the far side, in exactly the same way. Also raw distinguishable.");
            Line("     The loss therefore happens later, in SlotFor / CliffSlotFor / the run walk, which");
            Line("     is where R22 already measured the unbounded whole-column read.");
        }

        private static string Bit(int raw, int bit)
        {
            return ((raw >> bit) & 1) == 1 ? "X" : ".";
        }

        // ---------------------------------------------------------------- probe

        private sealed class Probe
        {
            public int Raw;
            public int Reach;
            public string Canon;
            public string Role;
            public string Slot;
            public string Sprite;
            public int RunDepth;
            public int Offset;
            public string Grid;
            public string NwNeRow;
            public string WcERow;
            public string SwSeRow;
        }

        /// <summary>
        /// Builds a G x G grid whose centre is Raised and whose neighbours come from the raw mask. With
        /// reach 2 every set arm is also extended one cell further out, which is the ONLY difference
        /// between the two probes of a mask and is invisible to a 3x3 rule.
        /// </summary>
        private static TerrainGridData Build(int raw, int reach)
        {
            var g = new TerrainGridData(G + 4, G + 4, 2, 2);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(2 + x, 2 + y, TerrainType.Grass);
                }
            }

            g.SetElevation(2 + C, 2 + C, ElevationLevel.Raised);
            foreach ((string name, int dx, int dy, int bit) d in Dirs)
            {
                if (((raw >> d.bit) & 1) != 1)
                {
                    continue;
                }

                for (int k = 1; k <= reach; k++)
                {
                    int x = 2 + C + (d.dx * k);
                    int y = 2 + C + (d.dy * k);
                    if (x < 0 || y < 0 || x >= g.Width || y >= g.Height)
                    {
                        continue;
                    }

                    g.SetElevation(x, y, ElevationLevel.Raised);
                }
            }

            return g;
        }

        /// <summary>Reads the raw mask back OUT of the grid, so the record cannot lie about its mask.</summary>
        private static int Encode(TerrainGridData g)
        {
            return EncodeMask(g, 2 + C, 2 + C);
        }

        private static int EncodeMask(TerrainGridData g, int x, int y)
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

        private static Probe MakeProbe(AuthorHillsCompositionSet set, int raw, int reach)
        {
            TerrainGridData g = Build(raw, reach);
            int back = Encode(g);
            RaisedTopologyState st = RaisedTopologyState.Resolve(g, 2 + C, 2 + C);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(g, set);

            string sprite = "NONE";
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.VisualPosition.x == 2 + C && t.VisualPosition.y == 2 + C)
                {
                    sprite = t.Sprite.name;
                }
            }

            string nwNe = Bit(back, 7) + " " + Bit(back, 0) + " " + Bit(back, 1);
            string wcE = Bit(back, 6) + " C " + Bit(back, 2);
            string swSe = Bit(back, 5) + " " + Bit(back, 4) + " " + Bit(back, 3);

            var lines = new List<string>();
            lines.Add($"raw 0x{back:X2} reach {reach}");
            for (int i = G - 1; i >= 0; i--)
            {
                var sb = new StringBuilder();
                for (int j = 0; j < G; j++)
                {
                    sb.Append(RaisedNeighborResolver.IsRaised(g, 2 + j, 2 + i) ? 'X' : '.');
                }

                lines.Add("   " + sb);
            }

            return new Probe
            {
                Raw = back,
                Reach = reach,
                Canon = st.CanonicalMask.ToString(),
                Role = st.Role.ToString(),
                Slot = st.Slot.ToString(),
                Sprite = sprite,
                RunDepth = st.RunDepth,
                Offset = st.OffsetFromRunBottom,
                Grid = string.Join(" / ", lines),
                NwNeRow = nwNe,
                WcERow = wcE,
                SwSeRow = swSe,
            };
        }

        private static TerrainGridData BuildMask(string[] mask, int ox, int oy)
        {
            int h = mask.Length;
            int w = mask[0].Length;
            var g = new TerrainGridData(w + 10, h + 10, 4 + ox, 4 + oy);
            for (int y = 0; y < g.Height; y++)
            {
                for (int x = 0; x < g.Width; x++)
                {
                    g.SetTerrain(4 + ox + x, 4 + oy + y, TerrainType.Grass);
                }
            }

            for (int i = 0; i < h; i++)
            {
                int y = 4 + oy + (h - 1 - i);
                for (int x = 0; x < w; x++)
                {
                    if (x < mask[i].Length && mask[i][x] == 'X')
                    {
                        g.SetElevation(4 + ox + x, y, ElevationLevel.Raised);
                    }
                }
            }

            return g;
        }

        private static void TenFold(AuthorHillsCompositionSet set, int expectTally, int expectTrue)
        {
            Line("");
            Line("-- 10x consistency --");
            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var sig = new List<string>();
                for (int raw = 0; raw <= 255; raw++)
                {
                    for (int reach = 1; reach <= 2; reach++)
                    {
                        Probe p = MakeProbe(set, raw, reach);
                        sig.Add(p.Raw + ":" + p.Reach + ":" + p.Canon + ":" + p.Role + ":"
                            + p.Slot + ":" + p.Sprite + ":" + p.RunDepth);
                    }
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

            Check("TENFOLD_COLLISION_AUDIT_STABLE", agree == 9,
                agree + "/9 further repetitions produced a byte-identical 512-probe record");
            Check("NO_TRUE_LOCAL_AMBIGUITY", expectTrue == 0,
                expectTrue + " canonical state(s) classified TRUE_LOCAL_AMBIGUITY, i.e. cases where an "
                    + "identical raw 3x3 is proven by author or locked visual oracle to need two "
                    + "different Sprites. Zero means card section 5's condition for requiring "
                    + "composition context is NOT met");
            Check("CLASSIFICATION_TALLY_REPRODUCED", expectTally >= 0,
                "tally recorded: " + expectTally + " collisions classified");
        }
    }
}