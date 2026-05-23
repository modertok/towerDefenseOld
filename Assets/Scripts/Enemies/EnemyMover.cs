using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyMover : MonoBehaviour
{
    private Vector3[] _waypoints;
    private int       _wpIndex;
    private float     _baseSpeed;
    private float     _currentSpeed;
    private float     _slowTimer;
    private Enemy     _enemy;

    // Progress — скільки waypoint-відрізків пройдено (більше = ближче до бази)
    public float Progress   { get; private set; }
    public bool  ReachedEnd { get; private set; }

    void Awake() => _enemy = GetComponent<Enemy>();

    public void Initialize(Vector3[] waypoints, float speed)
    {
        _waypoints    = waypoints;
        _baseSpeed    = speed;
        _currentSpeed = speed;
        _wpIndex      = 0;
        Progress      = 0f;
        ReachedEnd    = false;
        _slowTimer    = 0f;

        if (waypoints != null && waypoints.Length > 0)
            transform.position = waypoints[0];
    }

    void Update()
    {
        if (ReachedEnd || _waypoints == null || _wpIndex >= _waypoints.Length) return;

        // Slow timer
        if (_slowTimer > 0f)
        {
            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0f) _currentSpeed = _baseSpeed;
        }

        Vector3 target = _waypoints[_wpIndex];
        transform.position = Vector3.MoveTowards(
            transform.position, target, _currentSpeed * Time.deltaTime);

        // Відзеркалення спрайту
        float dx = target.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.02f)
            transform.localScale = new Vector3(dx > 0 ? 1f : -1f, 1f, 1f);

        // Досягли поточного waypoint?
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            _wpIndex++;
            if (_wpIndex >= _waypoints.Length)
            {
                ReachedEnd = true;
                // Повідомляємо WaveManager; він забере HP бази і поверне ворога у пул
                WaveManager.Instance?.OnEnemyReachedBase(_enemy);
                return;
            }
        }

        // Оновлюємо Progress
        if (_wpIndex > 0 && _wpIndex < _waypoints.Length)
        {
            float seg  = Vector3.Distance(_waypoints[_wpIndex - 1], _waypoints[_wpIndex]);
            float dist = Vector3.Distance(transform.position, _waypoints[_wpIndex]);
            float frac = seg > 0f ? 1f - dist / seg : 1f;
            Progress = (_wpIndex - 1) + frac;
        }
        else
        {
            Progress = _wpIndex;
        }
    }

    public void ApplySlow(float slowFraction, float duration)
    {
        _currentSpeed = _baseSpeed * (1f - Mathf.Clamp01(slowFraction));
        _slowTimer    = duration;
    }
}
