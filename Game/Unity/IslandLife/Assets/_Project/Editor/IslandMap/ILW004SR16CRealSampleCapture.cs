// IL-WORLD-004S-R16C - zoomed capture of the REAL FirstIsland samples.
//
// The three failing samples the user saved live in the real asset, so they are read from it and
// rendered from it. Nothing here rebuilds an approximate T, L or O, and nothing writes to the map.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IslandLife.World.Terrain;

namespace IslandLife.EditorTools.IslandMap
{
    public static class ILW004SR16CRealSampleCapture
    {
        private const string RealDataPath =
            "Assets/_Project/World/Terrain/FirstIsland_TerrainData.asset";
        private const string CompositionSetPath =
            "Assets/Art/Environment/SproutLands/TerrainRender/AuthorHillsCompositionSet.asset";
        private const string OutDir = @"F:\IslandLife\TempAudit\IL-WORLD-004S-R16C";

        private static readonly List<string> Lines = new List<string>();

        private static void Line(string s)
        {
            Lines.Add(s);
            Debug.Log("[R16Cc] " + s);
        }

        [MenuItem("IslandLife/Diagnostics/R16C Real Sample Capture")]
        public static void Run()
        {
            Lines.Clear();
            Directory.CreateDirectory(OutDir);

            TerrainMapData real = AssetDatabase.LoadAssetAtPath<TerrainMapData>(RealDataPath);
            TerrainGridData grid = real.CreateGridData();
            AuthorHillsCompositionSet set =
                AssetDatabase.LoadAssetAtPath<AuthorHillsCompositionSet>(CompositionSetPath);
            RaisedVisualPlan plan = RaisedVisualPlan.Build(grid, set);

            Line("=== REAL samples, zoomed, read straight from the live asset ===");
            Line($"   {plan.Tiles.Count} hill tiles");

            var comp = new Dictionary<Vector2Int, int>();
            var members = new List<List<Vector2Int>>();
            for (int y = grid.OriginY; y < grid.OriginY + grid.Height; y++)
            {
                for (int x = grid.OriginX; x < grid.OriginX + grid.Width; x++)
                {
                    if (!RaisedNeighborResolver.IsRaised(grid, x, y)
                        || comp.ContainsKey(new Vector2Int(x, y)))
                    {
                        continue;
                    }

                    int id = members.Count;
                    var list = new List<Vector2Int>();
                    members.Add(list);
                    var q = new Queue<Vector2Int>();
                    q.Enqueue(new Vector2Int(x, y));
                    comp[new Vector2Int(x, y)] = id;
                    while (q.Count > 0)
                    {
                        Vector2Int cur = q.Dequeue();
                        list.Add(cur);
                        foreach (Vector2Int s in new[]
                        {
                            new Vector2Int(0, 1), new Vector2Int(0, -1),
                            new Vector2Int(1, 0), new Vector2Int(-1, 0),
                        })
                        {
                            var n = new Vector2Int(cur.x + s.x, cur.y + s.y);
                            if (RaisedNeighborResolver.IsRaised(grid, n.x, n.y)
                                && !comp.ContainsKey(n))
                            {
                                comp[n] = id;
                                q.Enqueue(n);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < members.Count; i++)
            {
                int minX = int.MaxValue, maxX = int.MinValue;
                int minY = int.MaxValue, maxY = int.MinValue;
                foreach (Vector2Int m in members[i])
                {
                    minX = Math.Min(minX, m.x);
                    maxX = Math.Max(maxX, m.x);
                    minY = Math.Min(minY, m.y);
                    maxY = Math.Max(maxY, m.y);
                }

                Dump(grid, members[i], minX, maxX, minY, maxY);
                Render(grid, plan, minX, maxX, minY, maxY, $"REAL_SAMPLE_{i + 1:000}");
            }

            var sb = new StringBuilder();
            foreach (string l in Lines)
            {
                sb.AppendLine(l);
            }

            File.WriteAllText(Path.Combine(OutDir, "R16C_real_samples.txt"), sb.ToString());
            Line("=== done ===");
        }

        private static void Dump(
            TerrainGridData grid, List<Vector2Int> cells,
            int minX, int maxX, int minY, int maxY)
        {
            Line("");
            Line($"--- component {cells.Count} cells, x {minX}..{maxX}, y {minY}..{maxY} ---");
            for (int y = maxY; y >= minY - 2; y--)
            {
                var row = new StringBuilder($"   y={y,4} ");
                for (int x = minX - 1; x <= maxX + 1; x++)
                {
                    if (RaisedNeighborResolver.IsRaised(grid, x, y))
                    {
                        row.Append('#');
                    }
                    else
                    {
                        var claimed = new List<string>();
                        foreach (var t in cells)
                        {
                        }

                        row.Append('.');
                    }
                }

                Line(row.ToString());
            }
        }

        private static void Render(
            TerrainGridData grid, RaisedVisualPlan plan,
            int minX, int maxX, int minY, int maxY, string id)
        {
            const int px = 150;
            int cols = maxX - minX + 5;
            int rows = maxY - minY + 5;

            var go = new GameObject("R16CSample") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Grid g = go.AddComponent<UnityEngine.Grid>();
            g.cellSize = new Vector3(1f, 1f, 0f);
            var mapGo = new GameObject("Raised") { hideFlags = HideFlags.HideAndDontSave };
            mapGo.transform.SetParent(go.transform, false);
            UnityEngine.Tilemaps.Tilemap map = mapGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            mapGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();

            var cache = new Dictionary<Sprite, UnityEngine.Tilemaps.Tile>();
            foreach (HillVisualTile t in plan.Tiles)
            {
                if (t.Sprite == null
                    || t.VisualPosition.x < minX - 2 || t.VisualPosition.x > maxX + 2
                    || t.VisualPosition.y < minY - 3 || t.VisualPosition.y > maxY + 2)
                {
                    continue;
                }

                if (!cache.TryGetValue(t.Sprite, out UnityEngine.Tilemaps.Tile tile))
                {
                    tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                    tile.sprite = t.Sprite;
                    cache[t.Sprite] = tile;
                }

                map.SetTile(t.VisualPosition, tile);
            }

            map.RefreshAllTiles();

            int w = cols * px;
            int h = rows * px;
            var camGo = new GameObject("R16CCam") { hideFlags = HideFlags.HideAndDontSave };
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = rows * 0.5f;
            cam.transform.position = new Vector3(
                minX - 1 + cols * 0.5f, minY - 2 + rows * 0.5f, -100f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(1f, 0f, 1f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.targetTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.Render();
            cam.Render();
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = cam.targetTexture;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, $"{id}.png"), tex.EncodeToPNG());
            Line($"   wrote {id}.png ({w}x{h})");

            RenderTexture.active = prev;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(cam.targetTexture);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}