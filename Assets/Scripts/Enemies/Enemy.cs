using UnityEngine;

[RequireComponent(typeof(EnemyMover))]
[RequireComponent(typeof(EnemyHealth))]
public class Enemy : MonoBehaviour
{
    public EnemyData Data { get; private set; }

    private EnemyMover  _mover;
    private EnemyHealth _health;

    // Захист від подвійного повернення у пул
    private bool _consumed;

    public float Progress => _mover != null ? _mover.Progress : 0f;
    public bool  IsDead   => _health != null && _health.IsDead;

    void Awake()
    {
        _mover  = GetComponent<EnemyMover>();
        _health = GetComponent<EnemyHealth>();
    }

    public void Initialize(EnemyData data, Vector3[] waypoints)
    {
        Data      = data;
        _consumed = false;
        _mover.Initialize(waypoints, data.moveSpeed);
        _health.Initialize(data.maxHP);
    }

    public void TakeDamage(float amount)         => _health?.TakeDamage(amount);
    public void ApplySlow(float frac, float dur)  => _mover?.ApplySlow(frac, dur);

    /// <summary>
    /// Єдина точка повернення у пул.
    /// Гарантує що ворог повертається лише один раз.
    /// </summary>
    public void Consume()
    {
        if (_consumed) return;
        _consumed = true;
        EnemyPool.Instance?.ReturnEnemy(this);
    }
}
