using UnityEngine;

public class GridCell
{
    public Vector2Int GridPosition { get; }
    public Vector3 WorldPosition { get; }
    public bool IsPath { get; set; }
    public bool IsOccupied { get; set; }

    public GridCell(Vector2Int gridPos, Vector3 worldPos, bool isPath = false)
    {
        GridPosition = gridPos;
        WorldPosition = worldPos;
        IsPath = isPath;
        IsOccupied = false;
    }

    public bool CanBuild => !IsPath && !IsOccupied;
}
