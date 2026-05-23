using UnityEngine;

public class TowerShooter : MonoBehaviour
{
    private TowerData _data;
    private float     _cooldown;

    public void Initialize(TowerData data)
    {
        _data     = data;
        _cooldown = 0f;
    }

    void Update()
    {
        if (_data == null) return;
        if (GameStateManager.Instance.CurrentState != GameState.Battle) return;

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        Enemy target = FindBestTarget();
        if (target == null) return;

        Fire(target);
        _cooldown = 1f / _data.fireRate;
    }

    // ── Targeting: вибираємо ворога з найвищим Progress (ближче до бази) ──

    Enemy FindBestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, _data.range, LayerMask.GetMask("Enemy"));

        Enemy best = null;
        float bestProg = -1f;

        foreach (var h in hits)
        {
            var e = h.GetComponent<Enemy>();
            if (e == null || e.IsDead) continue;
            if (e.Progress > bestProg)
            {
                bestProg = e.Progress;
                best = e;
            }
        }
        return best;
    }

    // ── Fire ──────────────────────────────────────────────────────────────

    void Fire(Enemy target)
    {
        if (_data.projectilePrefab == null) return;
        var proj = ProjectilePool.Instance.Get(_data.projectilePrefab);
        if (proj == null) return;

        proj.transform.position = transform.position;
        proj.Initialize(target, _data, _data.projectilePrefab);
        AudioBus.PlayShot();
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (_data == null) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _data.range);
    }
}
