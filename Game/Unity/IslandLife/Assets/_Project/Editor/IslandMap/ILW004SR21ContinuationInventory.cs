// IL-WORLD-004S-R21 - corner continuation grammar: INVENTORY + ADJACENCY. DIAGNOSTIC ONLY.
//
// WHY THIS FILE EXISTS. Card sections 6, 7, 8 and 18 require three things to be MEASURED before any
// production rule may be written:
//
//   1. A full inventory of the author Hills sheet over rows r0..r8, separating NON_EMPTY cells from
//      TRANSPARENT_EMPTY ones. A transparent cell is an author slot that is simply empty; it is not a
//      missing asset and it is not to be deleted or "fixed".
//   2. The REAL adjacency of the junction cell in STATE A and in STATE B, taken from the production
//      resolver path, printed per direction, so the rule can only ever be written from conditions that
//      genuinely differ between the two states.
//   3. Which existing author slices are even CANDIDATES for each state, so the uniqueness question in
//      section 18 can be answered with evidence instead of taste.
//
// THIS HARNESS ASSERTS NOTHING. It deliberately does not encode a STATE A or STATE B sprite, because
// card section 6 forbids forcing Hills_r2c4 or Hills_r3c5 into a fixture on the strength of their names
// alone, and section 18 requires a STOP when more than one candidate is reasonable. Writing an
// expectation here would pre-empt exactly the decision the card reserves for PM.
//
// COORDINATE CONVENTION. Top-left visual grid. Fixture line 0 is the NORTH row and the last line is the
// SOUTH row, so (4,4) is the south-west cell.
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
    public static class ILW004SR21ContinuationInventory
    {
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string SpriteDir = "Assets/Art/Environment/SproutLands/Sprites";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R21";

        private static readonly List<string> Lines = new List<string>();
        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R21] " + s);
        }

        [MenuItem("IslandLife/Diagnostics/R21 Continuation Inventory")]
        public static void Run()
        {
            Lines.Clear();
            Directory.CreateDirectory(OutDir);
            Line("=== IL-WORLD-004S-R21 corner continuation: INVENTORY + ADJACENCY (diagnostic) ===");

            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);

            Inventory(set);
            StateAdjacency(set);
            Candidates(set);

            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R21_inventory.txt"), sb.ToString());
        }

        // ---------------------------------------------------------------- 7: full sheet inventory

        private static void Inventory(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- HILLS SHEET INVENTORY, rows r0..r8 --");

            var byRow = new SortedDictionary<int, List<string>>();
            var empty = new List<string>();
            var nonEmpty = new List<string>();

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { SpriteDir });
            var slices = new SortedDictionary<string, Sprite>();
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                string n = System.IO.Path.GetFileNameWithoutExtension(p);
                if (!n.StartsWith("Hills_r"))
                {
                    continue;
                }

                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null)
                {
                    slices[n] = s;
                }
            }

            foreach (KeyValuePair<string, Sprite> kv in slices)
            {
                string name = kv.Key;
                int c = int.Parse(name.Substring("Hills_r".Length + 2));
                int r = int.Parse(name.Substring("Hills_r".Length, 1));
                bool filled = HasAnyPixel(kv.Value);
                if (!byRow.TryGetValue(r, out List<string> list))
                {
                    list = new List<string>();
                    byRow[r] = list;
                }

                list.Add(c + (filled ? "" : "=EMPTY"));
                if (filled)
                {
                    nonEmpty.Add(name);
                }
                else
                {
                    empty.Add(name);
                }
            }

            Line($"   slices found = {slices.Count}");
            Line($"   NON_EMPTY = {nonEmpty.Count}");
            Line($"   TRANSPARENT_EMPTY = {empty.Count}");
            Line("   per row (column index, '=EMPTY' marks an author empty slot):");
            foreach (KeyValuePair<int, List<string>> kv in byRow)
            {
                Line($"     r{kv.Key}: {string.Join(" ", kv.Value.OrderBy(x => int.Parse(x.Split('=')[0])))}");
            }

            Line("   NON_EMPTY cells:");
            Line("     " + string.Join(" ", nonEmpty.OrderBy(Group).ThenBy(Col)));
            Line("   TRANSPARENT_EMPTY cells:");
            Line("     " + string.Join(" ", empty.OrderBy(Group).ThenBy(Col)));

            Line("   which of them the production composition set already references:");
            var referenced = new List<string>();
            foreach (System.Reflection.FieldInfo f in typeof(AuthorHillsCompositionSet)
                .GetFields(System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic))
            {
                if (f.FieldType != typeof(Sprite))
                {
                    continue;
                }

                Sprite v = (Sprite)f.GetValue(set);
                if (v != null)
                {
                    referenced.Add($"{f.Name}={v.name}");
                }
            }

            Line("     " + string.Join(" ", referenced.OrderBy(x => x)));

            Line("   author sheet note: a TRANSPARENT_EMPTY cell is an empty slot in the author's own "
                + "layout. It is NOT a missing asset and nothing may delete or synthesise it.");
        }

        private static int Group(string n)
        {
            return int.Parse(n.Substring("Hills_r".Length, 1));
        }

        private static int Col(string n)
        {
            return int.Parse(n.Substring("Hills_r".Length + 2));
        }

        /// <summary>
        /// True when the slice contains at least one pixel above a small alpha threshold. The author
        /// textures are not Read/Write enabled, so the pixels are copied with Graphics.Blit into a
        /// readable RenderTexture and read back, which never modifies the source asset.
        /// </summary>
        private static bool HasAnyPixel(Sprite s)
        {
            Texture2D src = s.texture;
            if (src == null)
            {
                return false;
            }

            var rt = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            try
            {
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                var tmp = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                tmp.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                tmp.Apply();
                Color32[] px = tmp.GetPixels32();
                UnityEngine.Object.DestroyImmediate(tmp);
                for (int i = 0; i < px.Length; i++)
                {
                    if (px[i].a > 8)
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                RenderTexture.active = prev;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        // ---------------------------------------------------------------- 8: real adjacency

        /// <summary>
        /// The locked P fixture with its vertical arm extended upward, one cell at a time, printed per
        /// cell with the full 3x3 occupancy so the only conditions that actually differ between STATE A
        /// and STATE B are visible.
        /// </summary>
        private static void StateAdjacency(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- STATE A / STATE B ADJACENCY, real resolver path, vertical arm +1 .. +4 --");

            // The base P of R20B is [XXX / XX.] and its vertical arm is the WEST column, which is the
            // only arm the extension is applied to. Height H means that column carries H cells.
            var perHeight = new SortedDictionary<int, Dictionary<string, CellInfo>>();
            for (int h = 1; h <= 5; h++)
            {
                perHeight[h] = Dump(set, HeightFixture(h));
                List<string> seq = perHeight[h].Values
                    .Select(c => "(" + c.X + "," + c.Y + ")="
                        + c.Sprite.Replace("Hills_", string.Empty))
                    .OrderBy(x => x, StringComparer.Ordinal).ToList();
                Line("   HEIGHT " + h + ": " + string.Join(" ", seq));
            }

            Line("");
            Line("   per-cell detail, HEIGHT 1 (STATE A) and HEIGHT 2 (STATE B):");
            foreach (KeyValuePair<string, CellInfo> kv in perHeight[1])
            {
                CellInfo a = kv.Value;
                CellInfo b = perHeight[2][kv.Key];
                Line($"     ({a.X},{a.Y})  A: raw=0x{a.Raw:X2} canon={a.Canon} runDepth={a.RunDepth} "
                    + $"role={a.Role} slot={a.Slot} sprite={a.Sprite}");
                Line($"     ({a.X},{a.Y})  B: raw=0x{b.Raw:X2} canon={b.Canon} runDepth={b.RunDepth} "
                    + $"role={b.Role} slot={b.Slot} sprite={b.Sprite}");
                var diff = new List<string>();
                foreach (string k in new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" })
                {
                    if (a.Occupancy[k] != b.Occupancy[k])
                    {
                        diff.Add($"{k}:{a.Occupancy[k]}->{b.Occupancy[k]}");
                    }
                }

                Line($"     ({a.X},{a.Y})  DIFFERING_NEIGHBORS = "
                    + (diff.Count == 0 ? "NONE, this cell is unaffected by the extension" : string.Join(" ", diff)));
            }

            Line("");
            Line("   cells whose SPRITE changed between HEIGHT 1 and HEIGHT 2 (the real STATE A/B pair):");
            bool any = false;
            foreach (KeyValuePair<string, CellInfo> kv in perHeight[1])
            {
                CellInfo a = kv.Value;
                CellInfo b = perHeight[2][kv.Key];
                if (a.Sprite != b.Sprite)
                {
                    any = true;
                    Line($"     ({a.X},{a.Y}): {a.Sprite} -> {b.Sprite}  (role {a.Role} -> {b.Role}, "
                        + $"slot {a.Slot} -> {b.Slot})");
                }
            }

            if (!any)
            {
                Line("     NO CELL CHANGED ITS SPRITE between HEIGHT 1 and HEIGHT 2 on this fixture");
            }

            Line("");
            Line("   stability of the junction sprite as the arm grows (section 10):");
            foreach (KeyValuePair<string, CellInfo> kv in perHeight[1])
            {
                var seq = new List<string>();
                for (int h = 1; h <= 5; h++)
                {
                    seq.Add(perHeight[h][kv.Key].Sprite.Replace("Hills_", string.Empty));
                }

                Line($"     ({kv.Key})  H1..H5 = {string.Join(" ", seq)}");
            }
        }

        private sealed class CellInfo
        {
            public int X;
            public int Y;
            public byte Raw;
            public string Canon;
            public int RunDepth;
            public string Role;
            public string Slot;
            public string Sprite;
            public readonly Dictionary<string, string> Occupancy =
                new Dictionary<string, string>();
        }

        /// <summary>
        /// The locked P with its west vertical arm extended. Height 1 is the base [XXX / XX.], and each
        /// extra height adds one Raised cell to the WEST column above the existing ones.
        /// </summary>
        private static string[] HeightFixture(int height)
        {
            // South row "XX." and north row "XXX", then the west column extended upward.
            var rows = new List<string> { "XXX" };
            rows.Add("XX.");
            for (int extra = 2; extra < height; extra++)
            {
                rows.Insert(0, "X..");
            }

            return rows.ToArray();
        }

        private static Dictionary<string, CellInfo> Dump(
            AuthorHillsCompositionSet set, string[] mask)
        {
            var result = new Dictionary<string, CellInfo>();
            TerrainGridData grid = Build(mask);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            foreach (HillVisualTile t in plan.Tiles)
            {
                RaisedTopologyState st =
                    RaisedTopologyState.Resolve(grid, t.VisualPosition.x, t.VisualPosition.y);
                var c = new CellInfo
                {
                    X = t.VisualPosition.x,
                    Y = t.VisualPosition.y,
                    Raw = (byte)st.RawMask,
                    Canon = st.CanonicalMask.ToString(),
                    RunDepth = st.RunDepth,
                    Role = st.Role.ToString(),
                    Slot = st.Slot.ToString(),
                    Sprite = t.Sprite.name,
                };

                foreach (string d in new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" })
                {
                    c.Occupancy[d] = Has(st.RawMask, d) ? "R" : ".";
                }

                result[c.X + "," + c.Y] = c;
            }

            return result;
        }

        private static bool Has(TerrainNeighborMask m, string dir)
        {
            TerrainNeighborMask bit;
            switch (dir)
            {
                case "N": bit = TerrainNeighborMask.North; break;
                case "NE": bit = TerrainNeighborMask.NorthEast; break;
                case "E": bit = TerrainNeighborMask.East; break;
                case "SE": bit = TerrainNeighborMask.SouthEast; break;
                case "S": bit = TerrainNeighborMask.South; break;
                case "SW": bit = TerrainNeighborMask.SouthWest; break;
                case "W": bit = TerrainNeighborMask.West; break;
                case "NW": bit = TerrainNeighborMask.NorthWest; break;
                default: throw new ArgumentOutOfRangeException(nameof(dir), dir, "bad direction");
            }

            return (m & bit) != 0;
        }

        private static TerrainGridData Build(string[] mask)
        {
            int h = mask.Length;
            int w = mask[0].Length;
            var grid = new TerrainGridData(w + 10, h + 10, 4, 4);
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

        // ---------------------------------------------------------------- 6 / 18: candidates

        private static void Candidates(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- CANDIDATE AUTHOR SLICES for the rows PM named, and what is actually referenced --");

            string[] names = { "Hills_r2c4", "Hills_r3c5", "Hills_r2c5", "Hills_r2c0", "Hills_r2c1",
                "Hills_r2c2", "Hills_r3c0", "Hills_r3c1", "Hills_r3c2" };
            foreach (string n in names)
            {
                Sprite s = Load(n);
                bool filled = s != null && HasAnyPixel(s);
                bool inSet = s != null && Referenced(set, s);
                Line("   " + n.PadRight(11) + " present=" + (s != null).ToString().PadRight(5)
                    + " non_empty=" + filled.ToString().PadRight(5)
                    + " referenced_by_set=" + inSet
                    + (s == null ? string.Empty : $"  rect=({s.rect.x},{s.rect.y}) {s.rect.width}x{s.rect.height}"));
            }

            Line("");
            Line("   UNICARNESS CHECK, for PM. For each state the card requires ONE existing author "
                + "slice to be provable from raw 3x3 adjacency plus canonical adjacency plus the "
                + "already-locked grammar. The facts needed to answer that are printed above; this "
                + "harness deliberately does not pick a winner.");
        }

        private static Sprite Load(string name)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { SpriteDir });
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<Sprite>(p);
                }
            }

            return null;
        }

        private static bool Referenced(AuthorHillsCompositionSet set, Sprite s)
        {
            foreach (System.Reflection.FieldInfo f in typeof(AuthorHillsCompositionSet)
                .GetFields(System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(Sprite) && (Sprite)f.GetValue(set) == s)
                {
                    return true;
                }
            }

            return false;
        }
    }
}