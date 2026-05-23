using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "TowerDefense/TowerData")]
public class TowerData : ScriptableObject
{
    [Header("Info")]
    public string towerName = "Tower";
    public Sprite icon;

    [Header("Economy")]
    public int cost = 100;

    [Header("Combat")]
    public float fireRate  = 1f;    // пострілів/сек
    public float range     = 3f;    // в одиницях сцени
    public float damage    = 20f;

    [Header("Attack Type")]
    public AttackType attackType = AttackType.Single;
    public float aoeRadius   = 1.5f;  // Mage
    public float slowFraction = 0.5f; // Freezer: 0.5 = -50% швидкості
    public float slowDuration = 2f;   // секунди

    [Header("Prefabs")]
    public GameObject towerPrefab;
    public GameObject projectilePrefab;
}
