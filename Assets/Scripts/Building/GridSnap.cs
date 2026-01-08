using UnityEngine;

public class GridSnap : MonoBehaviour
{
    [Header("网格配置")]
    [SerializeField]
    private int gridSizeX = 25;

    [SerializeField]
    private int gridSizeZ = 25;

    [SerializeField]
    private float planeScale = 5f;

    private float cellSize;
    private float halfWidth;
    private float halfHeight;

    void Awake()
    {
        CalculateGridParameters();
    }

    void CalculateGridParameters()
    {
        cellSize = (10f * planeScale) / Mathf.Max(gridSizeX, gridSizeZ);
        halfWidth = (gridSizeX * cellSize) / 2f;
        halfHeight = (gridSizeZ * cellSize) / 2f;
    }

    public Vector3 SnapToGrid(Vector3 worldPosition)
    {
        Vector2Int cell = WorldToCell(worldPosition);
        return CellToWorld(cell);
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / cellSize);
        int z = Mathf.RoundToInt(worldPosition.z / cellSize);
        return new Vector2Int(x, z);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, transform.position.y, cell.y * cellSize);
    }

    public bool IsCellValid(Vector2Int cell)
    {
        return cell.x >= -gridSizeX / 2 && cell.x < gridSizeX / 2 &&
               cell.y >= -gridSizeZ / 2 && cell.y < gridSizeZ / 2;
    }

    public bool IsPositionInGrid(Vector3 worldPosition)
    {
        Vector2Int cell = WorldToCell(worldPosition);
        return IsCellValid(cell);
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    public int GetGridSizeX()
    {
        return gridSizeX;
    }

    public int GetGridSizeZ()
    {
        return gridSizeZ;
    }

    void OnDrawGizmos()
    {
        CalculateGridParameters();
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        for (int x = -gridSizeX / 2; x < gridSizeX / 2; x++)
        {
            for (int z = -gridSizeZ / 2; z < gridSizeZ / 2; z++)
            {
                Vector3 cellCenter = CellToWorld(new Vector2Int(x, z));
                Vector3 cellSize3 = new Vector3(cellSize * 0.9f, 0.01f, cellSize * 0.9f);
                Gizmos.DrawWireCube(cellCenter, cellSize3);
            }
        }
    }
}
