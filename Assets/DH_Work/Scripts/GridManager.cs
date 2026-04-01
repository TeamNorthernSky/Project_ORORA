using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Transform landTransform;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField, Range(0.1f, 1f)] private float obstacleCheckFill = 0.9f;

    private Vector3 gridOrigin = Vector3.zero;

    public float CellSize => cellSize;

    public Transform LandTransform => landTransform;

    private void Awake()
    {
        if (cellSize <= 0f)
            cellSize = 1f;
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int gx = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int gy = Mathf.RoundToInt((worldPosition.y - gridOrigin.y) / cellSize);
        return new Vector2Int(gx, gy);
    }

    public Vector3 GridToWorldCenter(Vector2Int grid)
    {
        float x = gridOrigin.x + (grid.x * cellSize);
        float z = gridOrigin.z + (grid.y * cellSize);
        return new Vector3(x, 0f, z);
    }

    public bool IsWalkable(Vector2Int grid)
    {
        Vector3 center = GridToWorldCenter(grid);
        center.y = GetLandSurfaceY() + 0.5f;

        Vector3 halfExtents = new Vector3(cellSize * 0.5f * obstacleCheckFill, 0.45f, cellSize * 0.5f * obstacleCheckFill);

        Collider[] cols = Physics.OverlapBox(center, halfExtents, Quaternion.identity, obstacleLayerMask);
        for(int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if(c == null)
                continue;

            if (landTransform != null && (c.transform == landTransform || c.transform.IsChildOf(landTransform)))
                continue;

            if(!string.IsNullOrEmpty(obstacleTag))
            {
                if (c.CompareTag(obstacleTag) || c.transform.root.CompareTag(obstacleTag))
                    return false;
            }
            return false;
        }
        if(!string.IsNullOrEmpty(obstacleTag))
        {
            if (obstacleLayerMask.value == 0)
            {
                Collider[] all = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0);
                for (int i = 0; i < all.Length; i++)
                {
                    var c = all[i];
                    if (c == null)
                        continue;
                    if (landTransform != null && (c.transform == landTransform || c.transform.IsChildOf(landTransform)))
                        continue;
                    if (c.CompareTag(obstacleTag) || c.transform.root.CompareTag(obstacleTag))
                        return false;
                }
            }
        }
        return true;
    }

    public float GetLandSurfaceY()
    {
        if (landTransform == null)
            return 0f;

        if (landTransform.TryGetComponent<Collider>(out var col))
            return col.bounds.max.y;

        return landTransform.position.y;
    }
}
