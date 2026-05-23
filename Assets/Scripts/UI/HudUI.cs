using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HudUI : MonoBehaviour
{
    public static HudUI Instance { get; private set; }

    [Header("Text Elements")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI baseHPText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI stateText;

    [Header("Start Wave Button (ховається під час бою)")]
    public GameObject startWaveBtnGo;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        EconomyManager.Instance.OnGoldChanged    += SetGold;
        Base.Instance.OnHPChanged                += SetBaseHP;
        GameStateManager.Instance.OnStateChanged += SetState;

        SetGold(EconomyManager.Instance.Gold);
        SetBaseHP(Base.Instance.CurrentHP);
        SetState(GameStateManager.Instance.CurrentState);
        UpdateRound(0, FindAnyObjectByType<GameManager>()?.totalRounds ?? 10);
    }

    void SetGold(int gold)     => SetText(goldText,   $"Золото: {gold}");
    void SetBaseHP(int hp)     => SetText(baseHPText, $"База: {hp} HP");

    void SetState(GameState s)
    {
        SetText(stateText, StateLabel(s));

        // Кнопка "ПОЧАТИ ХВИЛЮ" видима ТІЛЬКИ під час підготовки
        bool showBtn = s == GameState.Preparation;
        if (startWaveBtnGo != null)
            startWaveBtnGo.SetActive(showBtn);
    }

    public void UpdateRound(int current, int total) =>
        SetText(roundText, $"Раунд: {current}/{total}");

    static void SetText(TextMeshProUGUI t, string txt) { if (t) t.text = txt; }

    static string StateLabel(GameState s) => s switch
    {
        GameState.Preparation => "[ Пiдготовка ]",
        GameState.Battle      => "[ Битва ]",
        GameState.RoundEnd    => "[ Кiнець раунду ]",
        GameState.GameOver    => "[ Гра закiнчена ]",
        _                     => ""
    };
}
