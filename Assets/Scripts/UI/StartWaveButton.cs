using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Компонент для кнопки "ПОЧАТИ ХВИЛЮ".
/// Потрібен окремий MonoBehaviour бо анонімні лямбди
/// не серіалізуються в Unity після збереження сцени.
/// </summary>
[RequireComponent(typeof(Button))]
public class StartWaveButton : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);

        // Слідкуємо за станом — ховаємо кнопку під час бою
        GameStateManager.Instance.OnStateChanged += OnStateChanged;
        // Форс-оновлення одразу
        OnStateChanged(GameStateManager.Instance.CurrentState);
    }

    void OnClick()
    {
        if (GameStateManager.Instance.CurrentState == GameState.Preparation)
        {
            Debug.Log("[StartWaveButton] Починаємо хвилю!");
            GameStateManager.Instance.SetState(GameState.Battle);
        }
    }

    void OnStateChanged(GameState state)
    {
        // Видима тільки під час підготовки
        gameObject.SetActive(state == GameState.Preparation);
    }
}
