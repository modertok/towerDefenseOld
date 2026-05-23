using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Spawn Settings")]
    public float spawnInterval = 1f;
    public float spawnVariance = 0.2f;
    public int   maxPerWave    = 50;

    private int       _alive;
    private bool      _waveActive;
    private Coroutine _spawnCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void StartWave(List<WaveEntry> wave)
    {
        StopWave();

        int total = 0;
        foreach (var e in wave) total += e.count;
        _alive      = Mathf.Min(total, maxPerWave);
        _waveActive = _alive > 0;

        if (_waveActive)
            _spawnCoroutine = StartCoroutine(SpawnRoutine(wave));
        else
            GameStateManager.Instance.SetState(GameState.RoundEnd); // порожня хвиля
    }

    public void StopWave()
    {
        if (_spawnCoroutine != null) { StopCoroutine(_spawnCoroutine); _spawnCoroutine = null; }
        _waveActive = false;
        EnemyPool.Instance?.ReturnAll();
    }

    // EnemyHealth.Die() → цей метод
    public void OnEnemyDefeated() => CheckWaveEnd();

    // EnemyMover → ворог дістався бази
    public void OnEnemyReachedBase(Enemy enemy)
    {
        Base.Instance?.TakeDamage(enemy.Data.damageToBase);
        enemy.Consume();   // повертаємо у пул через Enemy.Consume()
        CheckWaveEnd();
    }

    // ── Internal ───────────────────────────────────────────────────────────

    void CheckWaveEnd()
    {
        if (!_waveActive) return;
        _alive--;
        if (_alive <= 0)
        {
            _waveActive = false;
            // Маленька пауза перед RoundEnd — щоб все встигло «осістись»
            StartCoroutine(EndWaveDelay());
        }
    }

    IEnumerator EndWaveDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (GameStateManager.Instance.CurrentState == GameState.Battle)
            GameStateManager.Instance.SetState(GameState.RoundEnd);
    }

    IEnumerator SpawnRoutine(List<WaveEntry> wave)
    {
        int spawned = 0;
        Vector3[] wps      = GridManager.Instance.GetWaypointPositions();
        Vector3   spawnPos = wps.Length > 0 ? wps[0] : Vector3.zero;

        foreach (var entry in wave)
        {
            if (entry.enemyData == null) continue;
            for (int i = 0; i < entry.count; i++)
            {
                if (spawned >= maxPerWave) yield break;
                EnemyPool.Instance.GetEnemy(entry.enemyData, spawnPos, wps);
                spawned++;
                float delay = spawnInterval + Random.Range(-spawnVariance, spawnVariance);
                yield return new WaitForSeconds(Mathf.Max(0.2f, delay));
            }
        }
    }
}
