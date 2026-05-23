using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Size")]
    public int columns = 12;
    public int rows = 8;
    public float cellSize = 1f;
    public Vector3 origin = new Vector3(-5.5f, -3.5f, 0f); // center grid

    [Header("Waypoints (Enemy Path)")]
    public Transform[] waypoints;

    [Header("Visual Colors (no sprites needed)")]
    public Color grassColor = new Color(0.3f, 0.65f, 0.2f);
    public Color pathColor  = new Color(0.75f, 0.6f, 0.35f);
    public Color gridLineColor = new Color(0f, 0f, 0f, 0.15f);

    private GridCell[,] _grid;

    // Cache two shared sprites
    private Sprite _grassSprite;
    private Sprite _pathSprite;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        _grassSprite = CreateGrassTile(grassColor);
        _pathSprite  = CreateStoneTile(pathColor);
        BuildGrid();
    }

    // ── Build ──────────────────────────────────────────────────────────────

    void BuildGrid()
    {
        _grid = new GridCell[columns, rows];
        HashSet<Vector2Int> pathSet = ComputePathCells();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 worldPos = CellToWorld(x, y);
                bool isPath = pathSet.Contains(new Vector2Int(x, y));
                _grid[x, y] = new GridCell(new Vector2Int(x, y), worldPos, isPath);
                SpawnCellVisual(worldPos, isPath);
            }
        }
    }

    void SpawnCellVisual(Vector3 pos, bool isPath)
    {
        var go = new GameObject(isPath ? "PathCell" : "GrassCell");
        go.transform.parent = transform;
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = isPath ? _pathSprite : _grassSprite;
        sr.transform.localScale = Vector3.one * (cellSize * 0.97f); // tiny gap = grid lines
        sr.sortingOrder = -10;
    }

    // ── Path computation ───────────────────────────────────────────────────

    HashSet<Vector2Int> ComputePathCells()
    {
        var set = new HashSet<Vector2Int>();
        if (waypoints == null || waypoints.Length < 2) return set;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            var from = WorldToGrid(waypoints[i].position);
            var to   = WorldToGrid(waypoints[i + 1].position);
            foreach (var c in BresenhamLine(from, to))
                set.Add(c);
        }
        return set;
    }

    static List<Vector2Int> BresenhamLine(Vector2Int a, Vector2Int b)
    {
        var cells = new List<Vector2Int>();
        int dx = Mathf.Abs(b.x - a.x), sx = a.x < b.x ? 1 : -1;
        int dy = Mathf.Abs(b.y - a.y), sy = a.y < b.y ? 1 : -1;
        int err = dx - dy;
        int x = a.x, y = a.y;

        while (true)
        {
            cells.Add(new Vector2Int(x, y));
            if (x == b.x && y == b.y) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 <  dx) { err += dx; y += sy; }
        }
        return cells;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public Vector2Int WorldToGrid(Vector3 world)
    {
        int x = Mathf.RoundToInt((world.x - origin.x) / cellSize);
        int y = Mathf.RoundToInt((world.y - origin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(Vector2Int pos) => CellToWorld(pos.x, pos.y);

    Vector3 CellToWorld(int x, int y) =>
        origin + new Vector3(x * cellSize, y * cellSize, 0f);

    public GridCell GetCell(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= columns || pos.y < 0 || pos.y >= rows) return null;
        return _grid[pos.x, pos.y];
    }

    public bool CanPlaceTower(Vector2Int pos) => GetCell(pos)?.CanBuild == true;

    public void SetOccupied(Vector2Int pos, bool occupied)
    {
        var cell = GetCell(pos);
        if (cell != null) cell.IsOccupied = occupied;
    }

    public Vector3[] GetWaypointPositions()
    {
        if (waypoints == null) return new Vector3[0];
        var pos = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
            pos[i] = waypoints[i].position;
        return pos;
    }

    // ── Sprite helpers ─────────────────────────────────────────────────────

    /// Трав'яний тайл 8×8 з легким шумом
    static Sprite CreateGrassTile(Color baseColor)
    {
        const int S = 8;
        var tex = new Texture2D(S, S) { filterMode = FilterMode.Point };
        var px  = new Color[S * S];
        Color dark  = baseColor * 0.80f;
        Color light = Color.Lerp(baseColor, Color.white, 0.12f);
        // Шаховий патерн + трохи рандому
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            bool alt = (x + y) % 2 == 0;
            // Мікро-трава
            if ((x == 1 && y == 6) || (x == 5 && y == 2) || (x == 3 && y == 4))
                px[y*S+x] = dark;
            else
                px[y*S+x] = alt ? baseColor : light;
        }
        // Темна рамка (1px)
        for (int i = 0; i < S; i++) {
            px[0*S+i] = dark; px[(S-1)*S+i] = dark;
            px[i*S+0] = dark; px[i*S+S-1]   = dark;
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,S,S), new Vector2(0.5f,0.5f), S);
    }

    /// Кам'яний тайл 8×8 (шлях)
    static Sprite CreateStoneTile(Color baseColor)
    {
        const int S = 8;
        var tex = new Texture2D(S, S) { filterMode = FilterMode.Point };
        var px  = new Color[S * S];
        Color dark  = baseColor * 0.72f;
        Color light = Color.Lerp(baseColor, Color.white, 0.18f);
        // Просте кладіння каменю
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
            px[y*S+x] = baseColor;
        // Горизонтальні шви
        for (int x = 0; x < S; x++) { px[3*S+x] = dark; px[4*S+x] = dark; }
        // Вертикальні шви (зсунуті)
        px[3*S+1] = dark; px[3*S+2] = dark;
        px[0*S+5] = dark; px[1*S+5] = dark; px[2*S+5] = dark;
        px[5*S+2] = dark; px[6*S+2] = dark; px[7*S+2] = dark;
        // Бліки
        px[1*S+1] = light; px[1*S+6] = light;
        px[6*S+1] = light; px[6*S+6] = light;
        // Рамка
        for (int i = 0; i < S; i++) {
            px[0*S+i] = dark; px[(S-1)*S+i] = dark;
            px[i*S+0] = dark; px[i*S+S-1]   = dark;
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,S,S), new Vector2(0.5f,0.5f), S);
    }
}
