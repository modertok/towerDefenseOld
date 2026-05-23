using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public int totalRounds = 10;

    [Header("Round End Delay (seconds)")]
    public float roundEndDelay = 2f;

    private int _currentRound = 0;
    public int CurrentRound => _currentRound;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        StartGame();
    }

    void StartGame()
    {
        _currentRound = 0;
        EconomyManager.Instance.Initialize();
        Base.Instance.Initialize();
        Base.Instance.OnBaseDestroyed += HandleBaseDestroyed;
        GameStateManager.Instance.OnStateChanged += HandleStateChanged;
        GameStateManager.Instance.SetState(GameState.Preparation);
    }

    void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Preparation:
                HudUI.Instance?.UpdateRound(_currentRound, totalRounds);
                break;

            case GameState.Battle:
                _currentRound++;
                HudUI.Instance?.UpdateRound(_currentRound, totalRounds);
                var wave = AIAttacker.Instance.GenerateWave(
                    EconomyManager.Instance.AttackBudget, _currentRound);
                WaveManager.Instance.StartWave(wave);
                break;

            case GameState.RoundEnd:
                EconomyManager.Instance.IncreaseBudget();
                if (_currentRound >= totalRounds)
                    StartCoroutine(DelayedState(GameState.GameOver));
                else
                    StartCoroutine(DelayedState(GameState.Preparation));
                break;

            case GameState.GameOver:
                bool defenderWon = Base.Instance.CurrentHP > 0;
                GameOverUI.Instance?.Show(defenderWon);
                break;
        }
    }

    IEnumerator DelayedState(GameState state)
    {
        yield return new WaitForSeconds(roundEndDelay);
        GameStateManager.Instance.SetState(state);
    }

    void HandleBaseDestroyed()
    {
        WaveManager.Instance.StopWave();
        GameStateManager.Instance.SetState(GameState.GameOver);
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager
            .LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
