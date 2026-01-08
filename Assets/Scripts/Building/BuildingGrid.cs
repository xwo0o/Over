using UnityEngine;
using System.Collections.Generic;

public class BuildingGrid : MonoBehaviour
{
    public static BuildingGrid Instance { get; private set; }

    public float cellSize = 1f;

    readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Vector3 SnapToGrid(Vector3 worldPosition)
    {
        Vector2Int cell = WorldToCell(worldPosition);
        return CellToWorld(cell);
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / cellSize);
        int y = Mathf.RoundToInt(worldPosition.z / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }

    public bool IsAreaFree(Vector3 worldPosition, int width, int height)
    {
        Vector2Int baseCell = WorldToCell(worldPosition);
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                Vector2Int cell = new Vector2Int(baseCell.x + dx, baseCell.y + dy);
                if (occupiedCells.Contains(cell))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void OccupyArea(Vector3 worldPosition, int width, int height)
    {
        Vector2Int baseCell = WorldToCell(worldPosition);
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                Vector2Int cell = new Vector2Int(baseCell.x + dx, baseCell.y + dy);
                occupiedCells.Add(cell);
            }
        }
    }

    public bool IsOnCamp(Vector3 worldPosition)
    {
        Ray ray = new Ray(worldPosition + Vector3.up * 10f, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 20f))
        {
            return hit.collider.CompareTag("yingdi");
        }
        return false;
    }

    // 释放占用的网格区域
    public void FreeArea(Vector3 worldPosition, int width, int height)
    {
        Vector2Int baseCell = WorldToCell(worldPosition);
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                Vector2Int cell = new Vector2Int(baseCell.x + dx, baseCell.y + dy);
                occupiedCells.Remove(cell);
            }
        }
        
    }

    // 获取占用的网格数量（用于调试）
    public int GetOccupiedCellCount()
    {
        return occupiedCells.Count;
    }

    // 清空所有占用的网格（用于重置场景）
    public void ClearAllOccupiedCells()
    {
        occupiedCells.Clear();
    }
}
