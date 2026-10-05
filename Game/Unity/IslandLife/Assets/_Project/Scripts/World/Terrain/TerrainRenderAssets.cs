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

        [SerializeField]
        private TileBase grassAutoTile;

        [SerializeField]
        private AuthorHillsCompositionSet raisedComposition;

        public TerrainSpriteSet Grass => grass;

        public TerrainSpriteSet Sand => sand;

        public TileBase Water => water;

        public TileBase GrassAutoTile => grassAutoTile;

        /// <summary>
        /// The author's verified Hills.png cells that Raised logical cells are projected onto.
        /// Null is legal and means "no hill visual": every Raised cell then simply renders as
        /// ordinary Grass and no soil is invented.
        /// </summary>
        public AuthorHillsCompositionSet RaisedComposition => raisedComposition;
    }
}