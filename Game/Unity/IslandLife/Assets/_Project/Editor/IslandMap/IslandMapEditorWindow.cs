using System;
using IslandLife.World.Terrain;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace IslandLife.EditorTools.IslandMap
{
    /// <summary>
    /// The single, permanent map-making entry point (IL-WORLD-004R).
    /// Tools / IslandLife / Map Editor  (Ctrl+Shift+M)
    ///
    /// Terrain is edited here; scenery objects are placed with Unity's normal Scene workflow
    /// (drag prefabs, move, duplicate, delete, undo, save). No second object database is created.
    /// </summary>
    public sealed class IslandMapEditorWindow : EditorWindow
    {
        private Vector2 m_Scroll;

        [MenuItem("Tools/IslandLife/Map Editor %#m", false, 100)]
        public static void Open()
        {
            IslandMapEditorWindow window = GetWindow<IslandMapEditorWindow>(false, "Island Map Editor", true);
            window.minSize = new Vector2(320f, 330f);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Island Map Editor", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Paint terrain here. Place scenery with normal Scene tools.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8f);

            bool active = IslandMapAuthoring.IsActive;

            using (new EditorGUI.DisabledScope(active))
            {
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
                if (GUILayout.Button(active ? "Authoring Active" : "Start Authoring", GUILayout.Height(30f)))
                {
                    IslandMapAuthoring.Start();
                }

                GUI.backgroundColor = previous;
            }

            using (new EditorGUI.DisabledScope(!active))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Terrain Palette", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Surface", EditorStyles.miniBoldLabel);
                DrawPaletteButton(TerrainType.Grass);
                DrawPaletteButton(TerrainType.Water);
                DrawPaletteButton(TerrainType.Sand);
                DrawPaletteButton(TerrainType.Empty);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Elevation (land only)", EditorStyles.miniBoldLabel);
                DrawElevationButton(ElevationLevel.Normal);
                DrawElevationButton(ElevationLevel.Raised);

                // IL-WORLD-004S-R13. Raised erase is a MODE of the existing High Ground brush, not a
                // second brush system, so the current brush and mode are always both visible and the
                // user can never wonder which of the two a click will do.
                using (new EditorGUI.DisabledScope(!IslandMapAuthoring.IsElevationBrush))
                {
                    EditorGUILayout.Space(4f);

                    bool erase = GUILayout.Toggle(
                        IslandMapAuthoring.IsElevationErase,
                        "Raised mode:  ERASE",
                        "Button",
                        GUILayout.Height(22f));
                    if (erase != IslandMapAuthoring.IsElevationErase)
                    {
                        IslandMapAuthoring.SetElevationErase(erase);
                    }

                    EditorGUILayout.LabelField(
                        "Brush:  Raised ground        Mode:  "
                        + IslandMapAuthoring.ElevationModeLabel,
                        EditorStyles.miniLabel);
                }

                // Low Ground is shown but permanently disabled: the Sprout Lands author art has
                // no pit or depression tiles, so there is nothing truthful to draw with it.
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField("Low Ground (-1)  -  unavailable, no author art");
                }

                EditorGUILayout.LabelField("Brush Size", "1 (fixed)");

                EditorGUILayout.Space(8f);
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.78f, 0.65f);
                if (GUILayout.Button("Stop Authoring", GUILayout.Height(26f)))
                {
                    IslandMapAuthoring.Stop();
                }

                GUI.backgroundColor = previous;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(IslandMapAuthoring.Status, MessageType.None);

            TerrainMapData data = IslandMapAuthoring.Data;
            if (data != null)
            {
                EditorGUILayout.LabelField("Authoritative source", data.name);
                EditorGUILayout.LabelField("Origin", data.OriginX + ", " + data.OriginY);
                EditorGUILayout.LabelField("Size", data.Width + " x " + data.Height);
                EditorGUILayout.LabelField("Edits this session", IslandMapAuthoring.Edits.ToString());
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Scenery: drag prefabs from Assets/Art/Prefabs/SproutLands straight into the Scene view. "
                + "Move, duplicate, delete and Undo work as normal. Save the scene with Ctrl+S.",
                MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Elevation buttons. Normal is the "flat ground" reset; High Ground paints only the
        /// vertical layer of a land cell and leaves its base terrain untouched. Low Ground is not
        /// offered - see the disabled label above.
        /// </summary>
        private static void DrawElevationButton(ElevationLevel level)
        {
            string label = level == ElevationLevel.Normal
                ? "Flat Ground (reset)"
                : "High Ground (+1)";

            bool selected = IslandMapAuthoring.IsActive
                && IslandMapAuthoring.ElevationBrush == level;

            using (new EditorGUI.DisabledScope(!IslandMapAuthoring.IsActive))
            {
                Color previous = GUI.backgroundColor;
                if (selected)
                {
                    GUI.backgroundColor = level == ElevationLevel.Raised
                        ? new Color(0.95f, 0.85f, 0.55f)
                        : new Color(0.8f, 0.8f, 0.8f);
                }

                if (GUILayout.Button(label, GUILayout.Height(24f)))
                {
                    IslandMapAuthoring.SetElevationBrush(level, out _);
                }

                GUI.backgroundColor = previous;
            }

            if (level != ElevationLevel.Normal)
            {
                EditorGUILayout.LabelField(
                    "    " + label + ": land cells only, edges auto-join",
                    EditorStyles.miniLabel);
            }
        }

        private static void DrawPaletteButton(TerrainType type)
        {
            bool paintable = IslandMapAuthoring.IsPaintable(type, out string reason);
            bool selected = IslandMapAuthoring.IsActive
                && !IslandMapAuthoring.IsElevationBrush
                && IslandMapAuthoring.Brush == type;

            using (new EditorGUI.DisabledScope(!paintable || !IslandMapAuthoring.IsActive))
            {
                Color previous = GUI.backgroundColor;
                if (selected)
                {
                    GUI.backgroundColor = type == TerrainType.Grass
                        ? new Color(0.65f, 0.92f, 0.65f)
                        : new Color(0.65f, 0.82f, 1f);
                }

                if (GUILayout.Button(type.ToString(), GUILayout.Height(24f)))
                {
                    IslandMapAuthoring.SetBrush(type);
                }

                GUI.backgroundColor = previous;
            }

            if (!paintable)
            {
                EditorGUILayout.LabelField("    " + type + ": not available", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(reason, MessageType.None);
            }
        }

        // ------------------------------------------------------------------ scene input

        private static void OnSceneGUI(SceneView view)
        {
            if (!IslandMapAuthoring.IsActive)
            {
                return; // never consume normal Scene View input while inactive
            }

            Event e = Event.current;

            if (e.type == EventType.Repaint)
            {
                IslandMapAuthoring.DrawSceneOverlay();
                return;
            }

            if (IslandMapAuthoring.HandleSceneEvent(e))
            {
                view.Repaint();
            }
        }
    }
}