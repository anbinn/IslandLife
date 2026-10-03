using UnityEngine;

namespace IslandLife.World.Terrain
{
    [CreateAssetMenu(
        fileName = "TerrainSpriteSet",
        menuName = "IslandLife/World/Terrain Sprite Set")]
    public class TerrainSpriteSet : ScriptableObject
    {
        [SerializeField]
        private TerrainType terrainType;

        [SerializeField]
        private Sprite[] sprites = new Sprite[77];

        public TerrainType TerrainType => terrainType;

        public bool TryGetSprite(TerrainSpriteCoordinate coordinate, out Sprite sprite)
        {
            if (coordinate.Row < 0 || coordinate.Row > 6
                || coordinate.Column < 0 || coordinate.Column > 10)
            {
                sprite = null;
                return false;
            }

            int index = coordinate.Row * 11 + coordinate.Column;
            if (index >= sprites.Length || sprites[index] == null)
            {
                sprite = null;
                return false;
            }

            sprite = sprites[index];
            return true;
        }
    }
}
