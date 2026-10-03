namespace IslandLife.World.Terrain
{
    public static class SproutLandsTerrainMaskMap
    {
        public static bool TryGetCoordinate(
            TerrainNeighborMask canonicalMask,
            out TerrainSpriteCoordinate coordinate)
        {
            switch (canonicalMask)
            {
                case (TerrainNeighborMask)22:
                    coordinate = new TerrainSpriteCoordinate(0, 0);
                    return true;
                case (TerrainNeighborMask)31:
                    coordinate = new TerrainSpriteCoordinate(0, 1);
                    return true;
                case (TerrainNeighborMask)11:
                    coordinate = new TerrainSpriteCoordinate(0, 2);
                    return true;
                case (TerrainNeighborMask)2:
                    coordinate = new TerrainSpriteCoordinate(0, 3);
                    return true;
                case (TerrainNeighborMask)18:
                    coordinate = new TerrainSpriteCoordinate(0, 4);
                    return true;
                case (TerrainNeighborMask)27:
                    coordinate = new TerrainSpriteCoordinate(0, 5);
                    return true;
                case (TerrainNeighborMask)30:
                    coordinate = new TerrainSpriteCoordinate(0, 6);
                    return true;
                case (TerrainNeighborMask)10:
                    coordinate = new TerrainSpriteCoordinate(0, 7);
                    return true;
                case (TerrainNeighborMask)26:
                    coordinate = new TerrainSpriteCoordinate(0, 8);
                    return true;
                case (TerrainNeighborMask)126:
                    coordinate = new TerrainSpriteCoordinate(0, 9);
                    return true;
                case (TerrainNeighborMask)214:
                    coordinate = new TerrainSpriteCoordinate(1, 0);
                    return true;
                case TerrainNeighborMask.All:
                    coordinate = new TerrainSpriteCoordinate(1, 1);
                    return true;
                case (TerrainNeighborMask)107:
                    coordinate = new TerrainSpriteCoordinate(1, 2);
                    return true;
                case (TerrainNeighborMask)66:
                    coordinate = new TerrainSpriteCoordinate(1, 3);
                    return true;
                case (TerrainNeighborMask)210:
                    coordinate = new TerrainSpriteCoordinate(1, 4);
                    return true;
                case (TerrainNeighborMask)251:
                    coordinate = new TerrainSpriteCoordinate(1, 5);
                    return true;
                case (TerrainNeighborMask)254:
                    coordinate = new TerrainSpriteCoordinate(1, 6);
                    return true;
                case (TerrainNeighborMask)106:
                    coordinate = new TerrainSpriteCoordinate(1, 7);
                    return true;
                case (TerrainNeighborMask)250:
                    coordinate = new TerrainSpriteCoordinate(1, 8);
                    return true;
                case (TerrainNeighborMask)219:
                    coordinate = new TerrainSpriteCoordinate(1, 9);
                    return true;
                case (TerrainNeighborMask)208:
                    coordinate = new TerrainSpriteCoordinate(2, 0);
                    return true;
                case (TerrainNeighborMask)248:
                    coordinate = new TerrainSpriteCoordinate(2, 1);
                    return true;
                case (TerrainNeighborMask)104:
                    coordinate = new TerrainSpriteCoordinate(2, 2);
                    return true;
                case (TerrainNeighborMask)64:
                    coordinate = new TerrainSpriteCoordinate(2, 3);
                    return true;
                case (TerrainNeighborMask)86:
                    coordinate = new TerrainSpriteCoordinate(2, 4);
                    return true;
                case (TerrainNeighborMask)127:
                    coordinate = new TerrainSpriteCoordinate(2, 5);
                    return true;
                case (TerrainNeighborMask)223:
                    coordinate = new TerrainSpriteCoordinate(2, 6);
                    return true;
                case (TerrainNeighborMask)75:
                    coordinate = new TerrainSpriteCoordinate(2, 7);
                    return true;
                case (TerrainNeighborMask)95:
                    coordinate = new TerrainSpriteCoordinate(2, 8);
                    return true;
                case (TerrainNeighborMask)94:
                    coordinate = new TerrainSpriteCoordinate(2, 9);
                    return true;
                case (TerrainNeighborMask)91:
                    coordinate = new TerrainSpriteCoordinate(2, 10);
                    return true;
                case (TerrainNeighborMask)16:
                    coordinate = new TerrainSpriteCoordinate(3, 0);
                    return true;
                case (TerrainNeighborMask)24:
                    coordinate = new TerrainSpriteCoordinate(3, 1);
                    return true;
                case (TerrainNeighborMask)8:
                    coordinate = new TerrainSpriteCoordinate(3, 2);
                    return true;
                case TerrainNeighborMask.None:
                    coordinate = new TerrainSpriteCoordinate(3, 3);
                    return true;
                case (TerrainNeighborMask)80:
                    coordinate = new TerrainSpriteCoordinate(3, 4);
                    return true;
                case (TerrainNeighborMask)120:
                    coordinate = new TerrainSpriteCoordinate(3, 5);
                    return true;
                case (TerrainNeighborMask)216:
                    coordinate = new TerrainSpriteCoordinate(3, 6);
                    return true;
                case (TerrainNeighborMask)72:
                    coordinate = new TerrainSpriteCoordinate(3, 7);
                    return true;
                case (TerrainNeighborMask)88:
                    coordinate = new TerrainSpriteCoordinate(3, 8);
                    return true;
                case (TerrainNeighborMask)218:
                    coordinate = new TerrainSpriteCoordinate(3, 9);
                    return true;
                case (TerrainNeighborMask)122:
                    coordinate = new TerrainSpriteCoordinate(3, 10);
                    return true;
                case (TerrainNeighborMask)82:
                    coordinate = new TerrainSpriteCoordinate(4, 4);
                    return true;
                case (TerrainNeighborMask)123:
                    coordinate = new TerrainSpriteCoordinate(4, 5);
                    return true;
                case (TerrainNeighborMask)222:
                    coordinate = new TerrainSpriteCoordinate(4, 6);
                    return true;
                case (TerrainNeighborMask)74:
                    coordinate = new TerrainSpriteCoordinate(4, 7);
                    return true;
                case (TerrainNeighborMask)90:
                    coordinate = new TerrainSpriteCoordinate(4, 8);
                    return true;
                default:
                    coordinate = default;
                    return false;
            }
        }
    }
}
