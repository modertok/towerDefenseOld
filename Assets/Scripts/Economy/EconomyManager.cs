using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Starting Values")]
    public int startGold          = 300;
    public int startAttackBudget  = 200;
    public int budgetIncrement    = 50;   // додається кожен раунд

    private int _gold;
    private int _attackBudget;

    public int Gold         => _gold;
    public int AttackBudget => _attackBudget;

    public event System.Action<int> OnGoldChanged;
    public event System.Action<int> OnBudgetChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void Initialize()
    {
        _gold         = startGold;
        _attackBudget = startAttackBudget;
        OnGoldChanged?.Invoke(_gold);
        OnBudgetChanged?.Invoke(_attackBudget);
    }

    // ── Gold ───────────────────────────────────────────────────────────────

    /// <summary>Витратити золото. Повертає false якщо не вистачає.</summary>
    public bool SpendGold(int amount)
    {
        if (_gold < amount) return false;
        _gold -= amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    public void AddGold(int amount)
    {
        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
        AudioBus.PlayGold();
    }

    // ── Attack Budget ──────────────────────────────────────────────────────

    public void IncreaseBudget()
    {
        _attackBudget += budgetIncrement;
        OnBudgetChanged?.Invoke(_attackBudget);
    }
}
