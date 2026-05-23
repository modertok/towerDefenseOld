using UnityEngine;

public class Tower : MonoBehaviour
{
    public TowerData Data { get; private set; }
    private Vector2Int _gridPos;

    public void Initialize(TowerData data, Vector2Int gridPos)
    {
        Data     = data;
        _gridPos = gridPos;

        var shooter = GetComponent<TowerShooter>();
        if (shooter != null) shooter.Initialize(data);
    }

    void OnDestroy()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.SetOccupied(_gridPos, false);
    }
}
