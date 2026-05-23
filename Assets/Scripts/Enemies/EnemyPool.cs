using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [System.Serializable]
    public class PoolEntry
    {
        public EnemyData data;
        public int preWarm = 10;
    }

    [Header("Pool Entries (one per EnemyData)")]
    public List<PoolEntry> entries;

    private Dictionary<EnemyData, IObjectPool<Enemy>> _pools = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        foreach (var e in entries)
        {
            if (e.data == null || e.data.prefab == null) continue;
            CreatePool(e.data, e.preWarm);
        }
    }

    void CreatePool(EnemyData data, int prewarm)
    {
        var pool = new ObjectPool<Enemy>(
            createFunc: () =>
            {
                var go = Instantiate(data.prefab);
                go.SetActive(false);
                var en = go.GetComponent<Enemy>();
                if (en == null) en = go.AddComponent<Enemy>();
                return en;
            },
            actionOnGet:     e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            actionOnDestroy: e => Destroy(e.gameObject),
            defaultCapacity: prewarm,
            maxSize: 60
        );
        _pools[data] = pool;

        // Pre-warm
        var tmp = new List<Enemy>();
        for (int i = 0; i < prewarm; i++) tmp.Add(pool.Get());
        foreach (var en in tmp) pool.Release(en);
    }

    public Enemy GetEnemy(EnemyData data, Vector3 spawnPos, Vector3[] waypoints)
    {
        if (!_pools.ContainsKey(data))
        {
            Debug.LogWarning($"No pool for {data.name}, creating on-the-fly.");
            CreatePool(data, 5);
        }
        var enemy = _pools[data].Get();
        enemy.transform.position = spawnPos;
        enemy.Initialize(data, waypoints);
        return enemy;
    }

    public void ReturnEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        if (enemy.Data != null && _pools.ContainsKey(enemy.Data))
            _pools[enemy.Data].Release(enemy);
        else
            enemy.gameObject.SetActive(false);
    }

    /// <summary>Return every active enemy to the pool (used on GameOver).</summary>
    public void ReturnAll()
    {
        // Unity 6: потрібен параметр FindObjectsInactive
        var all = FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var e in all)
            if (e.gameObject.activeSelf) ReturnEnemy(e);
    }
}
