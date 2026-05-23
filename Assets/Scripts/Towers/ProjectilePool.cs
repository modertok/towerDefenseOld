using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    private readonly Dictionary<string, IObjectPool<Projectile>> _pools  = new();
    private readonly Dictionary<string, GameObject>              _prefabs = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public Projectile Get(GameObject prefab)
    {
        string key = prefab.name;
        EnsurePool(key, prefab);
        return _pools[key].Get();
    }

    public void Return(GameObject prefab, Projectile proj)
    {
        if (prefab == null) { proj.gameObject.SetActive(false); return; }
        string key = prefab.name;
        EnsurePool(key, prefab);
        _pools[key].Release(proj);
    }

    void EnsurePool(string key, GameObject prefab)
    {
        if (_pools.ContainsKey(key)) return;
        _prefabs[key] = prefab;
        _pools[key] = new ObjectPool<Projectile>(
            createFunc: () =>
            {
                var go = Instantiate(_prefabs[key]);
                go.SetActive(false);
                return go.GetComponent<Projectile>();
            },
            actionOnGet:     p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject),
            defaultCapacity: 20,
            maxSize: 150
        );
    }
}
