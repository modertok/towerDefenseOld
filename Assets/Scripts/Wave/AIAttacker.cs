using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Автоматично формує хвилю ворогів для режиму PvE.
/// Зі зростанням раунду збільшується частка Орків і Привидів.
/// </summary>
public class AIAttacker : MonoBehaviour
{
    public static AIAttacker Instance { get; private set; }

    [Header("Enemy Types")]
    public EnemyData goblinData;
    public EnemyData orcData;
    public EnemyData ghostData;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// Генерує список ворогів у межах бюджету.
    /// </summary>
    public List<WaveEntry> GenerateWave(int budget, int round)
    {
        // Ваги за типом залежно від раунду
        float wGoblin = Mathf.Clamp(1.0f - round * 0.07f, 0.2f, 1.0f);
        float wOrc    = Mathf.Clamp(round * 0.07f,         0f,   0.6f);
        float wGhost  = round >= 4 ? Mathf.Clamp((round - 3) * 0.08f, 0f, 0.4f) : 0f;
        float wTotal  = wGoblin + wOrc + wGhost;

        var dict = new Dictionary<EnemyData, int>();
        int remaining    = budget;
        int spawnedCount = 0;
        int maxEnemies   = 50;

        while (remaining >= MinCost() && spawnedCount < maxEnemies)
        {
            EnemyData chosen = PickRandom(wGoblin / wTotal, wOrc / wTotal, remaining);
            if (chosen == null) break;

            remaining -= chosen.cost;
            spawnedCount++;

            if (dict.ContainsKey(chosen)) dict[chosen]++;
            else dict[chosen] = 1;
        }

        // Перетворюємо в список WaveEntry
        var result = new List<WaveEntry>();
        // Спочатку Goblin → потім Orc → потім Ghost (для порядку спавну)
        AddIfExists(result, dict, goblinData);
        AddIfExists(result, dict, ghostData);
        AddIfExists(result, dict, orcData);
        return result;
    }

    EnemyData PickRandom(float probGoblin, float probOrc, int remaining)
    {
        float roll = Random.value;
        if (roll < probGoblin && goblinData != null && goblinData.cost <= remaining)
            return goblinData;
        if (roll < probGoblin + probOrc && orcData != null && orcData.cost <= remaining)
            return orcData;
        if (ghostData != null && ghostData.cost <= remaining)
            return ghostData;
        if (goblinData != null && goblinData.cost <= remaining)
            return goblinData;
        return null;
    }

    int MinCost()
    {
        int min = int.MaxValue;
        if (goblinData != null) min = Mathf.Min(min, goblinData.cost);
        if (orcData    != null) min = Mathf.Min(min, orcData.cost);
        if (ghostData  != null) min = Mathf.Min(min, ghostData.cost);
        return min == int.MaxValue ? 999 : min;
    }

    static void AddIfExists(List<WaveEntry> list, Dictionary<EnemyData, int> dict, EnemyData key)
    {
        if (key != null && dict.ContainsKey(key) && dict[key] > 0)
            list.Add(new WaveEntry { enemyData = key, count = dict[key] });
    }
}
