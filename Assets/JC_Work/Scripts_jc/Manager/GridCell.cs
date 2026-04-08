/// <summary>
/// 그리드 셀 하나의 데이터. GameObject가 아닌 순수 데이터 클래스.
/// </summary>
[System.Serializable]
public class GridCell
{
    public enum VisibilityState { Unexplored, Fogged, Visible }

    public int x;
    public int y;
    public TerrainType terrainType = TerrainType.Plain;
    public float moveCost = 1f;
    public bool isWalkable = true;
    public VisibilityState visibility = VisibilityState.Unexplored;

    /// <summary>
    /// 이 셀을 점유 중인 MapObject의 ID. -1이면 비어있음.
    /// </summary>
    public int occupantId = -1;

    /// <summary>
    /// 이 셀에 연결된 이벤트 ID. -1이면 없음.
    /// </summary>
    public int eventId = -1;

    public GridCell(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool IsOccupied => occupantId >= 0;
}
