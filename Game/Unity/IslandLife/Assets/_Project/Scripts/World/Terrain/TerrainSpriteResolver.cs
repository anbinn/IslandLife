using System;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    public static class TerrainSpriteResolver
    {
        public static bool TryResolve(
            TerrainGridData grid,
            int x,
            int y,
            TerrainSpriteSet spriteSet,
            out Sprite sprite)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (spriteSet == null)
            {
                throw new ArgumentNullException(nameof(spriteSet));
            }

            TerrainType center = grid.GetTerrain(x, y);
            if (center == TerrainType.Empty || center != spriteSet.TerrainType)
            {
                sprite = null;
                return false;
            }

            TerrainNeighborMask rawMask =
                TerrainNeighborResolver.ResolveSameTerrainMask(grid, x, y);
            TerrainNeighborMask canonicalMask =
                TerrainMaskNormalizer.Normalize(rawMask);

            if (!SproutLandsTerrainMaskMap.TryGetCoordinate(
                    canonicalMask,
                    out TerrainSpriteCoordinate coordinate))
            {
                sprite = null;
                return false;
            }

            return spriteSet.TryGetSprite(coordinate, out sprite);
        }
    }
}
