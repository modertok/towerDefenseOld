using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrepPhaseUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public Button     startWaveButton;
    public TextMeshProUGUI budgetText;
    public TextMeshProUGUI towerHintText;

    void Start()
    {
        startWaveButton?.onClick.AddListener(OnStartWave);

        GameStateManager.Instance.OnStateChanged  += OnStateChanged;
        EconomyManager.Instance.OnBudgetChanged   += SetBudget;

        // ВАЖЛИВО: перевіряємо поточний стан одразу
        // (GameManager.Start() може спрацювати РАНІШЕ ніж ми підписалися)
        OnStateChanged(GameStateManager.Instance.CurrentState);
        SetBudget(EconomyManager.Instance.AttackBudget);
    }

    void OnStateChanged(GameState state)
    {
        panel?.SetActive(state == GameState.Preparation);
        if (state == GameState.Preparation)
            SetHint("Клікай на зелену клітинку щоб поставити вежу");
    }

    void OnStartWave()
    {
        GameStateManager.Instance.SetState(GameState.Battle);
    }

    void SetBudget(int budget)
    {
        if (budgetText != null)
            budgetText.text = $"AI бюджет: {budget}";
    }

    void SetHint(string hint)
    {
        if (towerHintText != null)
            towerHintText.text = hint;
    }
}
