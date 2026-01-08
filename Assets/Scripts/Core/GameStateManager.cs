using Mirror;
using UnityEngine;

public enum GamePhase
{
    Lobby,
    InGame
}

public class GameStateManager : NetworkBehaviour
{
    [SyncVar]
    public GamePhase phase = GamePhase.Lobby;

    public static GameStateManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Server]
    public void SetPhase(GamePhase newPhase)
    {
        phase = newPhase;
    }
}
