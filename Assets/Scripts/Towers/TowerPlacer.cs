using UnityEngine;
using UnityEngine.InputSystem;

// ─────────────────────────────────────────────────────────────────────────────
// TowerRangeDisplay — в тому ж файлі, щоб гарантовано компілювалось
// ─────────────────────────────────────────────────────────────────────────────
public class TowerRangeDisplay : MonoBehaviour
{
    public static TowerRangeDisplay Instance { get; private set; }

    private LineRenderer _lr;
    private const int SEG = 60;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.loop          = true;
        _lr.useWorldSpace = true;
        _lr.positionCount = SEG;
        _lr.startWidth    = 0.07f;
        _lr.endWidth      = 0.07f;
        _lr.sortingOrder  = 15;

        Shader sh = Shader.Find("Sprites/Default")
                 ?? Shader.Find("Hidden/Internal-Colored");
        if (sh != null)
            _lr.material = new Material(sh);

        var grad = new Gradient();
        var ck = new GradientColorKey[]
        {
            new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0f),
            new GradientColorKey(new Color(1f, 0.85f, 0.1f), 1f)
        };
        var ak = new GradientAlphaKey[]
        {
            new GradientAlphaKey(0.85f, 0f),
            new GradientAlphaKey(0.85f, 1f)
        };
        grad.SetKeys(ck, ak);
        _lr.colorGradient = grad;

        gameObject.SetActive(false);
    }

    public void ShowAt(Vector3 center, float radius)
    {
        gameObject.SetActive(true);
        for (int i = 0; i < SEG; i++)
        {
            float a = 2f * Mathf.PI * i / SEG;
            _lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(a) * radius,
                center.y + Mathf.Sin(a) * radius,
                -0.05f));
        }
    }

    public void Hide() => gameObject.SetActive(false);
}

// ─────────────────────────────────────────────────────────────────────────────
// TowerPlacer
// ─────────────────────────────────────────────────────────────────────────────
public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        // Гарантуємо що OverlapCircle бачить тригери (на випадок якщо ProjectSettings вимикає)
        Physics2D.queriesHitTriggers = true;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            TowerSidebar.Instance?.Deselect();
            TowerRangeDisplay.Instance?.Hide();
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame) return;

        Vector2 sp    = mouse.position.ReadValue();
        float   zDist = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 wp    = Camera.main.ScreenToWorldPoint(new Vector3(sp.x, sp.y, zDist));
        wp.z = 0f;

        // 1) КЛІК ПО ВЕЖІ — показати дальність. Перевіряється ПЕРШИМ і
        //    НЕ враховує IsPointerOverUI (раптом фрейм фолс-позитивно блокує).
        Tower clicked = TowerAt(wp);
        if (clicked != null)
        {
            TowerRangeDisplay.Instance?.ShowAt(clicked.transform.position, clicked.Data.range);
            return;
        }

        // 2) Клік не по вежі — приховати коло
        TowerRangeDisplay.Instance?.Hide();

        // 3) Розміщення нової вежі — лише якщо не на UI
        if (IsPointerOverUI()) return;

        var state = GameStateManager.Instance.CurrentState;
        if (state != GameState.Preparation && state != GameState.Battle) return;

        Vector2Int cell = GridManager.Instance.WorldToGrid(wp);
        GridCell   gc   = GridManager.Instance.GetCell(cell);
        if (gc == null || !gc.CanBuild) return;

        TowerData data = TowerSidebar.Instance?.SelectedTower;
        if (data == null) return;

        PlaceTower(data, cell);
    }

    public void PlaceTower(TowerData data, Vector2Int cell)
    {
        if (data == null) { Debug.LogError("[TowerPlacer] data==null"); return; }
        if (!GridManager.Instance.CanPlaceTower(cell)) { Debug.Log("[TowerPlacer] зайнято"); return; }
        if (!EconomyManager.Instance.SpendGold(data.cost)) { Debug.Log($"[TowerPlacer] мало золота!"); return; }
        if (data.towerPrefab == null)
        {
            Debug.LogError($"[TowerPlacer] towerPrefab null для {data.towerName}");
            EconomyManager.Instance.AddGold(data.cost);
            return;
        }

        Vector3 pos = GridManager.Instance.GridToWorld(cell);
        var go = Instantiate(data.towerPrefab, pos, Quaternion.identity);
        go.SetActive(true);

        if (go.GetComponent<Collider2D>() == null)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 0.8f;
            col.isTrigger = true;
        }

        var tower = go.GetComponent<Tower>() ?? go.AddComponent<Tower>();
        tower.Initialize(data, cell);
        GridManager.Instance.SetOccupied(cell, true);

        Debug.Log($"[TowerPlacer] {data.towerName} на {cell} | {EconomyManager.Instance.Gold}g");
    }

    /// Знаходить найближчу вежу до точки. Працює БЕЗ Physics2D —
    /// сканує всі Tower-компоненти в сцені (надійно для пулу/тригерів).
    static Tower TowerAt(Vector3 pos)
    {
        Tower best = null;
        float bestDist = 0.7f;  // радіус "хіта" в світових одиницях
        foreach (var t in Object.FindObjectsByType<Tower>(FindObjectsSortMode.None))
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            float d = Vector2.Distance(t.transform.position, pos);
            if (d < bestDist) { best = t; bestDist = d; }
        }
        return best;
    }

    /// Перевірка чи курсор над UI — сумісна з новим InputSystem.
    static bool IsPointerOverUI()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;
        // Новий InputSystem: без параметра — перевіряє ВСІ pointer-и
        return es.IsPointerOverGameObject();
    }
}
