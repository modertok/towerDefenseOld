using UnityEngine;

/// <summary>База Захисника. Ставиться в кінці шляху.</summary>
public class Base : MonoBehaviour
{
    public static Base Instance { get; private set; }

    [Header("Base HP")]
    public int maxHP = 20;

    private int _currentHP;
    public  int CurrentHP => _currentHP;

    public event System.Action<int> OnHPChanged;
    public event System.Action      OnBaseDestroyed;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void Initialize()
    {
        _currentHP = maxHP;
        OnHPChanged?.Invoke(_currentHP);
    }

    public void TakeDamage(int damage)
    {
        if (_currentHP <= 0) return;
        _currentHP = Mathf.Max(0, _currentHP - damage);
        OnHPChanged?.Invoke(_currentHP);
        AudioBus.PlayBaseHit();
        if (_currentHP <= 0) OnBaseDestroyed?.Invoke();
    }
}
