using UnityEngine;
using UnityEngine.Tilemaps;

namespace IslandLife.World.Terrain
{
    [CreateAssetMenu(
        fileName = "TerrainRenderAssets",
        menuName = "IslandLife/World/Terrain Render Assets")]
    public class TerrainRenderAssets : ScriptableObject
    {
        [SerializeField]
        private TerrainSpriteSet grass;

        [SerializeField]
        private TerrainSpriteSet sand;

        [SerializeField]
        private TileBase water;

        public TerrainSpriteSet Grass => grass;

        public TerrainSpriteSet Sand => sand;

        public TileBase Water => water;
    }
}
