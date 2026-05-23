using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("UI")]
    public GameObject        panel;
    public TextMeshProUGUI   resultText;
    public TextMeshProUGUI   subtitleText;
    public Button            restartButton;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        panel?.SetActive(false);
    }

    void Start()
    {
        restartButton?.onClick.AddListener(() => GameManager.Instance.RestartGame());
    }

    public void Show(bool defenderWon)
    {
        panel?.SetActive(true);

        if (resultText != null)
            resultText.text = defenderWon
                ? "ПЕРЕМОГА!"
                : "ВИ ПРОГРАЛИ!";

        if (subtitleText != null)
            subtitleText.text = defenderWon
                ? "База вистояла всi 10 раундiв!"
                : "База була знищена...";

        if (defenderWon) AudioBus.PlayVictory();
        else             AudioBus.PlayDefeat();
    }
}
