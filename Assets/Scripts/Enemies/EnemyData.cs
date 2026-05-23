using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "TowerDefense/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string enemyName = "Enemy";
    public Sprite icon;

    [Header("Stats")]
    public int   maxHP        = 40;
    public float moveSpeed    = 3f;
    public int   damageToBase = 1;   // HP base втрачає при досягненні
    public int   goldReward   = 5;   // золото Захисника за вбивство

    [Header("Wave Cost")]
    public int cost = 10;            // очки атаки для Атакуючого

    [Header("Special")]
    public bool immuneToSlow = false; // Ghost ігнорує Freezer

    [Header("Prefab")]
    public GameObject prefab;
}
