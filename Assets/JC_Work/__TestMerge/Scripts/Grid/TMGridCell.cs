namespace Orora.TestMerge
{
    [System.Serializable]
    public struct TMGridCell
    {
        public TMTerrainType terrainType;
        public TMFogVisibilityState visibility;
        public bool isWalkable;

        public int occupantId;
        public int eventId;

        public int lastRevealedDay;

        public bool IsOccupied => occupantId >= 0;
        public bool IsExplored => visibility != TMFogVisibilityState.Unexplored;
        public bool IsVisible => visibility == TMFogVisibilityState.Visible;

        public static TMGridCell CreateDefault()
        {
            return new TMGridCell
            {
                terrainType = TMTerrainType.Plain,
                visibility = TMFogVisibilityState.Unexplored,
                isWalkable = true,
                occupantId = -1,
                eventId = -1,
                lastRevealedDay = 0,
            };
        }
    }
}
