using System;
using UnityEngine;

namespace IslandLife.World.Terrain
{
    [Serializable]
    public struct TerrainCell
    {
        [SerializeField]
        private TerrainType terrainType;

        public TerrainType TerrainType => terrainType;

        public TerrainCell(TerrainType terrainType)
        {
            this.terrainType = terrainType;
        }
    }
}
