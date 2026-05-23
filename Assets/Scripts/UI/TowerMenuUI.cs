using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerMenuUI : MonoBehaviour
{
    public static TowerMenuUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("Tower Data")]
    public TowerData archerData;
    public TowerData mageData;
    public TowerData freezerData;
    public TowerData cannonData;

    [Header("Buttons")]
    public Button archerBtn;
    public Button mageBtn;
    public Button freezerBtn;
    public Button cannonBtn;

    private Vector2Int _targetCell;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        archerBtn?.onClick.AddListener(()  => Buy(archerData));
        mageBtn?.onClick.AddListener(()    => Buy(mageData));
        freezerBtn?.onClick.AddListener(() => Buy(freezerData));
        cannonBtn?.onClick.AddListener(()  => Buy(cannonData));
        Hide();
    }

    public void Show(Vector2Int cell)
    {
        _targetCell = cell;
        panel?.SetActive(true);

        // Позиціонуємо панель над кліком у screen-space
        if (Camera.main != null && panel != null)
        {
            Vector3 worldPos  = GridManager.Instance.GridToWorld(cell);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Переводимо з screen у canvas local
            var canvasRt = panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, null, out Vector2 localPt);

            var rt = panel.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Зміщуємо вгору щоб не перекривати клітинку
                Vector2 offset = new Vector2(0, rt.sizeDelta.y * 0.6f + 20f);
                rt.anchoredPosition = localPt + offset;
                ClampToScreen(rt, canvasRt);
            }
        }

        RefreshButtons();
    }

    public void Hide() => panel?.SetActive(false);

    void Buy(TowerData data)
    {
        if (data == null) return;
        TowerPlacer.Instance.PlaceTower(data, _targetCell);
    }

    void RefreshButtons()
    {
        int gold = EconomyManager.Instance.Gold;
        SetInteractable(archerBtn,  archerData,  gold);
        SetInteractable(mageBtn,    mageData,    gold);
        SetInteractable(freezerBtn, freezerData, gold);
        SetInteractable(cannonBtn,  cannonData,  gold);
    }

    static void SetInteractable(Button btn, TowerData data, int gold)
    {
        if (btn == null || data == null) return;
        btn.interactable = gold >= data.cost;
        var img = btn.GetComponent<Image>();
        if (img) img.color = gold >= data.cost
            ? img.color
            : new Color(img.color.r * 0.5f, img.color.g * 0.5f, img.color.b * 0.5f, 0.7f);
    }

    // Не дати панелі вийти за межі екрану
    static void ClampToScreen(RectTransform rt, RectTransform canvas)
    {
        Vector2 pos  = rt.anchoredPosition;
        Vector2 half = rt.sizeDelta * 0.5f;
        Vector2 cHalf = canvas.sizeDelta * 0.5f;
        pos.x = Mathf.Clamp(pos.x, -cHalf.x + half.x, cHalf.x - half.x);
        pos.y = Mathf.Clamp(pos.y, -cHalf.y + half.y, cHalf.y - half.y);
        rt.anchoredPosition = pos;
    }
}
