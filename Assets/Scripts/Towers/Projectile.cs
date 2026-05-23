using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8f;

    private Enemy      _target;
    private TowerData  _data;
    private GameObject _sourcePrefab;
    private bool       _spent;

    public void Initialize(Enemy target, TowerData data, GameObject sourcePrefab)
    {
        _target       = target;
        _data         = data;
        _sourcePrefab = sourcePrefab;
        _spent        = false;
    }

    void Update()
    {
        if (_spent) return;

        // Ціль зникла, вмерла або повернулась у пул → відбій
        if (_target == null || _target.IsDead || !_target.gameObject.activeSelf)
        {
            ReturnToPool();
            return;
        }

        Vector3 dir = (_target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // Повернути снаряд обличчям до цілі
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        if (Vector3.Distance(transform.position, _target.transform.position) < 0.15f)
            OnHit();
    }

    void OnHit()
    {
        if (_spent) return;
        _spent = true;

        switch (_data.attackType)
        {
            case AttackType.Single:
                _target.TakeDamage(_data.damage);
                AudioBus.PlayHit();
                break;

            case AttackType.AoE:
                var hits = Physics2D.OverlapCircleAll(
                    transform.position, _data.aoeRadius, LayerMask.GetMask("Enemy"));
                foreach (var h in hits)
                {
                    var e = h.GetComponent<Enemy>();
                    if (e != null && !e.IsDead && e.gameObject.activeSelf)
                        e.TakeDamage(_data.damage);
                }
                AudioBus.PlayExplosion();
                break;

            case AttackType.Slow:
                if (!_target.Data.immuneToSlow)
                    _target.ApplySlow(_data.slowFraction, _data.slowDuration);
                _target.TakeDamage(_data.damage * 0.5f);
                AudioBus.PlayFreeze();
                break;
        }

        ReturnToPool();
    }

    void ReturnToPool()
    {
        _spent = true;
        ProjectilePool.Instance?.Return(_sourcePrefab, this);
    }
}
