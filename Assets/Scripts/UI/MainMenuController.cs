using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button rulesButton;
    public Button closeRulesButton;

    [Header("Panels")]
    public GameObject rulesPanel;

    void Start()
    {
        rulesPanel?.SetActive(false);
        startButton?.onClick.AddListener(StartGame);
        rulesButton?.onClick.AddListener(() => rulesPanel?.SetActive(true));
        closeRulesButton?.onClick.AddListener(() => rulesPanel?.SetActive(false));
    }

    void StartGame()
    {
        SceneManager.LoadScene("Game");
    }
}
