using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    private GameState _currentState = GameState.Menu;
    public GameState CurrentState => _currentState;

    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void SetState(GameState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        Debug.Log($"[State] → {newState}");
        OnStateChanged?.Invoke(newState);
    }
}
