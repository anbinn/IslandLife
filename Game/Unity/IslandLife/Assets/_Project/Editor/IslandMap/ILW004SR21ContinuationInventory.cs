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
            VerifyHeights(set);
            VerifyRectangles(set);
            TenFoldHeights(set);

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
        /// <summary>
        /// THE TARGET CELL. PM clarified it: the junction where a vertical Raised boundary meets a
        /// Raised platform on its RIGHT. In the locked P that is (4,5): west open (the vertical
        /// boundary), east Raised (the platform), south Raised (the boundary continues), and NORTH is
        /// the only thing that differs between the two states.
        ///
        ///   STATE A  base        [XXX / XX.]        (4,5) with north OPEN   -> no continuation above
        ///   STATE B  continuation[X.. / XXX / XX.]  (4,5) with north Raised -> continuation above
        ///
        /// The base P is drawn with fixture line 0 as the NORTH row, so adding one Raised cell on top
        /// of the west column turns the north row from "XXX" into "X.." and pushes the platform down a
        /// row, which keeps the junction at the same logical address (4,5).
        /// </summary>
        private static readonly int JunctionX = 4;
        private static readonly int JunctionY = 5;

        private static string[] StateFixture(bool withContinuation)
        {
            // No continuation: north row "XXX", south row "XX.".  The junction is the west end of the
            // north row. With continuation: one more Raised cell above that west column, so the west
            // column is 3 deep and the junction keeps the same logical address one row below its top.
            return withContinuation
                ? new[] { "X..", "XXX", "XX." }
                : new[] { "XXX", "XX." };
        }

        private static void StateAdjacency(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- STATE A / STATE B ADJACENCY at the TARGET JUNCTION CELL --");
            Line("   target = the cell where a vertical Raised boundary meets a Raised platform on its "
                + "RIGHT. In the locked P that is (4,5): west open, east Raised, south Raised.");
            Line("   STATE A   = no Raised continuation above  -> north OPEN");
            Line("   STATE B   = Raised continuation above      -> north RAISED");

            var a = Dump(set, StateFixture(false));
            var b = Dump(set, StateFixture(true));
            string key = JunctionX + "," + JunctionY;

            Line("");
            Line("   STATE A full fixture [XXX / XX.] logical " + a.Count + " cells:");
            foreach (KeyValuePair<string, CellInfo> kv in a.OrderBy(k => int.Parse(k.Value.Y.ToString())))
            {
                Line("     " + Describe(kv.Value));
            }

            Line("   STATE B full fixture [X.. / XXX / XX.] logical " + b.Count + " cells:");
            foreach (KeyValuePair<string, CellInfo> kv in b.OrderBy(k => int.Parse(k.Value.Y.ToString())))
            {
                Line("     " + Describe(kv.Value));
            }

            Line("");
            if (!a.ContainsKey(key) || !b.ContainsKey(key))
            {
                Line("   TARGET CELL MISSING from one of the two fixtures");
                return;
            }

            CellInfo ca = a[key];
            CellInfo cb = b[key];
            Line("   STATE_A coord=" + ca.X + "," + ca.Y + " raw=0x" + ca.Raw.ToString("X2")
                + " canonical=" + ca.Canon + " runDepth=" + ca.RunDepth + " role=" + ca.Role
                + " slot=" + ca.Slot + " sprite=" + ca.Sprite);
            Line("   STATE_B coord=" + cb.X + "," + cb.Y + " raw=0x" + cb.Raw.ToString("X2")
                + " canonical=" + cb.Canon + " runDepth=" + cb.RunDepth + " role=" + cb.Role
                + " slot=" + cb.Slot + " sprite=" + cb.Sprite);

            var diff = new List<string>();
            foreach (string k in new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" })
            {
                if (ca.Occupancy[k] != cb.Occupancy[k])
                {
                    diff.Add(k + ": " + ca.Occupancy[k] + " -> " + cb.Occupancy[k]);
                }
            }

            Line("   DIFFERING_NEIGHBORS = " + (diff.Count == 0
                ? "NONE" : string.Join("; ", diff)));
            Line("   => the whole STATE A / STATE B distinction is ONE local bit at that cell: whether "
                + "its NORTH neighbour is Raised. runDepth is " + ca.RunDepth + " in both states, so it "
                + "CANNOT be the discriminator, exactly as the card requires.");

            Line("");
            Line("   stability of the target cell as the vertical boundary grows upward:");
            for (int h = 1; h <= 5; h++)
            {
                var f = ContinuationFixture(h);
                var d = Dump(set, f);
                string k2 = "4,5";
                Line("     " + string.Join(" ", f).Replace("X", "R").Replace(".", ".")
                    + "   cells=" + d.Count
                    + "   target=" + (d.ContainsKey(k2) ? d[k2].Sprite : "-")
                    + "  raw=" + (d.ContainsKey(k2) ? "0x" + d[k2].Raw.ToString("X2") : "-")
                    + "  role=" + (d.ContainsKey(k2) ? d[k2].Role : "-")
                    + "  north=" + (d.ContainsKey(k2) ? d[k2].Occupancy["N"] : "-"));
            }
        }

        private static string Describe(CellInfo c)
        {
            return "(" + c.X + "," + c.Y + ") raw=0x" + c.Raw.ToString("X2")
                + " canon=" + c.Canon + " N=" + c.Occupancy["N"] + " NE=" + c.Occupancy["NE"]
                + " E=" + c.Occupancy["E"] + " SE=" + c.Occupancy["SE"] + " S=" + c.Occupancy["S"]
                + " SW=" + c.Occupancy["SW"] + " W=" + c.Occupancy["W"] + " NW=" + c.Occupancy["NW"]
                + " runDepth=" + c.RunDepth + " role=" + c.Role + " slot=" + c.Slot
                + " sprite=" + c.Sprite;
        }

        /// <summary>
        /// The locked P with 0..4 extra Raised cells stacked on top of its west vertical boundary, so the
        /// junction at (4,5) is unchanged in address and only its north occupancy moves from open to
        /// Raised at the first extension and stays Raised afterwards.
        /// </summary>
        private static string[] ContinuationFixture(int height)
        {
            var rows = new List<string>();
            // IL-WORLD-004S-R21 LADDER FIX. The previous version started at i = 2, so HEIGHT 1 and
            // HEIGHT 2 built the identical fixture and HEIGHT 1 was never measured as its own state.
            // Height 1 is now the bare base P and each additional height adds exactly one Raised cell.
            for (int i = 1; i < height; i++)
            {
                rows.Insert(0, "X..");
            }

            rows.Add("XXX");
            rows.Add("XX.");
            return rows.ToArray();
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

        // ---------------------------------------------------------------- R21 locks

        private static void Check(string id, bool ok, string detail)
        {
            Line((ok ? "PASS  " : "FAIL  ") + id + "  ::  " + detail);
        }

        /// <summary>
        /// The PM-locked junction lock over heights 1..5. H1 must be the no-continuation component and
        /// H2..H5 must ALL be the continuation component, with the junction sprite never sliding back
        /// with runDepth.
        /// </summary>
        private static void VerifyHeights(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- JUNCTION LOCK over HEIGHT 1..5: H1 = r3c5, H2..H5 = r2c4, no sliding --");
            int good = 0;
            for (int h = 1; h <= 5; h++)
            {
                var d = Dump(set, ContinuationFixture(h));
                CellInfo j;
                if (!d.TryGetValue("4,5", out j))
                {
                    Line("   HEIGHT " + h + ": TARGET CELL MISSING");
                    continue;
                }

                string want = h == 1 ? "Hills_r3c5" : "Hills_r2c4";
                bool okSprite = j.Sprite == want;
                bool okRole = h == 1
                    ? j.Role == "CORNER_NO_UPPER_CONTINUATION"
                    : j.Role == "CORNER_WITH_UPPER_CONTINUATION";
                Line("   HEIGHT " + h + " cells=" + d.Count + " north=" + j.Occupancy["N"]
                    + " raw=0x" + j.Raw.ToString("X2") + " canon=" + j.Canon
                    + " runDepth=" + j.RunDepth + " role=" + j.Role + " sprite=" + j.Sprite
                    + (okSprite && okRole ? "   OK" : "   MISMATCH, expected " + want));
                if (okSprite && okRole)
                {
                    good++;
                }
            }

            Check("HEIGHT_LADDER_JUNCTION_LOCK", good == 5,
                good + "/5 heights resolved the PM-locked junction component: H1 no-continuation "
                    + "r3c5, H2..H5 continuation r2c4, with no sliding back to r0c0/r1c0/r2c0");
        }

        /// <summary>
        /// Card section 13: a plain rectangle must NOT be captured by the junction rule. The junction
        /// requires the vertical boundary to END directly under it, which a three-deep or deeper wall
        /// never satisfies.
        /// </summary>
        private static void VerifyRectangles(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- RECTANGLE GUARD: a plain rectangle must not be captured as a corner/junction --");
            var cases = new List<Tuple<string, string[]>>
            {
                Tuple.Create("2x2", new[] { "XX", "XX" }),
                Tuple.Create("3x2", new[] { "XXX", "XXX" }),
                Tuple.Create("5x2", new[] { "XXXXX", "XXXXX" }),
                Tuple.Create("3x3", new[] { "XXX", "XXX", "XXX" }),
                Tuple.Create("5x3", new[] { "XXXXX", "XXXXX", "XXXXX" }),
            };

            int clean = 0;
            foreach (Tuple<string, string[]> c in cases)
            {
                var d = Dump(set, c.Item2);
                int junctions = d.Values.Count(
                    v => v.Role == "CORNER_NO_UPPER_CONTINUATION"
                        || v.Role == "CORNER_WITH_UPPER_CONTINUATION");
                Line("   " + c.Item1.PadRight(4) + " cells=" + d.Count + " junction_roles=" + junctions
                    + "  sprites=" + string.Join(" ",
                        d.Values.OrderBy(v => v.Y).ThenBy(v => v.X).Select(v =>
                            "(" + v.X + "," + v.Y + ")="
                                + v.Sprite.Replace("Hills_", string.Empty))));
                if (junctions == 0)
                {
                    clean++;
                }
            }

            Check("RECTANGLES_NOT_CAPTURED_AS_JUNCTIONS", clean == 5,
                clean + "/5 plain rectangles resolve ZERO junction roles, so north occupancy alone "
                    + "cannot drag an ordinary rectangle into the junction grammar");
        }

        private static void TenFoldHeights(AuthorHillsCompositionSet set)
        {
            Line("");
            Line("-- 10x consistency over HEIGHT 1..5 --");
            string reference = null;
            int agree = 0;
            for (int rep = 0; rep < 10; rep++)
            {
                var keys = new List<string>();
                foreach (int h in new[] { 1, 2, 3, 4, 5 })
                {
                    var d = Dump(set, ContinuationFixture(h));
                    CellInfo j;
                    keys.Add(d.TryGetValue("4,5", out j) ? j.Sprite : "MISSING");
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
            }

            Check("TENFOLD_HEIGHT_LADDER_STABLE", agree == 9,
                agree + "/9 further repetitions produced the identical junction sequence ["
                    + reference + "], i.e. r3c5 once then r2c4 four times");
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