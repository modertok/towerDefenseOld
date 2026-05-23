using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Бокова панель вибору вежі (тільки кнопки + мітки).
/// Кнопка "ПОЧАТИ ХВИЛЮ" знаходиться в HUD.
/// </summary>
public class TowerSidebar : MonoBehaviour
{
    public static TowerSidebar Instance { get; private set; }

    public TowerData SelectedTower { get; private set; }

    [Header("Root panel")]
    public GameObject sidebarPanel;

    [Header("Tower Buttons")]
    public Button archerBtn;
    public Button mageBtn;
    public Button freezerBtn;
    public Button cannonBtn;

    [Header("Tower Data")]
    public TowerData archerData;
    public TowerData mageData;
    public TowerData freezerData;
    public TowerData cannonData;

    [Header("Info Labels")]
    public TextMeshProUGUI goldLabel;
    public TextMeshProUGUI selectedLabel;

    static readonly Color ColArcher  = new Color(0.25f, 0.38f, 0.15f);
    static readonly Color ColMage    = new Color(0.28f, 0.10f, 0.42f);
    static readonly Color ColFreezer = new Color(0.08f, 0.30f, 0.48f);
    static readonly Color ColCannon  = new Color(0.30f, 0.24f, 0.18f);
    static readonly Color ColSelect  = new Color(0.85f, 0.68f, 0.08f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        archerBtn? .onClick.AddListener(() => Select(archerData,  archerBtn));
        mageBtn?   .onClick.AddListener(() => Select(mageData,    mageBtn));
        freezerBtn?.onClick.AddListener(() => Select(freezerData, freezerBtn));
        cannonBtn? .onClick.AddListener(() => Select(cannonData,  cannonBtn));

        GameStateManager.Instance.OnStateChanged += OnStateChanged;
        EconomyManager.Instance.OnGoldChanged    += OnGoldChanged;

        // Форс-перевірка поточного стану
        OnStateChanged(GameStateManager.Instance.CurrentState);
        OnGoldChanged(EconomyManager.Instance.Gold);
    }

    void Select(TowerData data, Button btn)
    {
        SelectedTower = data;
        PaintButtons(btn);
        RefreshSelectedLabel();
    }

    public void Deselect()
    {
        SelectedTower = null;
        PaintButtons(null);
        RefreshSelectedLabel();
    }

    void PaintButtons(Button active)
    {
        SetColor(archerBtn,  active == archerBtn  ? ColSelect : ColArcher);
        SetColor(mageBtn,    active == mageBtn    ? ColSelect : ColMage);
        SetColor(freezerBtn, active == freezerBtn ? ColSelect : ColFreezer);
        SetColor(cannonBtn,  active == cannonBtn  ? ColSelect : ColCannon);
    }

    static void SetColor(Button b, Color c)
    {
        if (b == null) return;
        var img = b.GetComponent<Image>();
        if (img) img.color = c;
    }

    void RefreshSelectedLabel()
    {
        if (selectedLabel == null) return;
        selectedLabel.text = SelectedTower != null
            ? $"Обрано:\n{SelectedTower.towerName}"
            : "Оберi вежу\nзi списку";
    }

    void OnStateChanged(GameState s)
    {
        bool show = s == GameState.Preparation || s == GameState.Battle;
        sidebarPanel?.SetActive(show);
        if (s != GameState.Preparation) Deselect();
    }

    void OnGoldChanged(int gold)
    {
        if (goldLabel) goldLabel.text = $"Золото\n{gold}";
        SetInteractable(archerBtn,  archerData,  gold);
        SetInteractable(mageBtn,    mageData,    gold);
        SetInteractable(freezerBtn, freezerData, gold);
        SetInteractable(cannonBtn,  cannonData,  gold);
    }

    static void SetInteractable(Button b, TowerData d, int gold)
    {
        if (b == null || d == null) return;
        b.interactable = gold >= d.cost;
    }
}
