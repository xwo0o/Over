using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class CharacterSelectionManager : NetworkBehaviour
{
    readonly Dictionary<NetworkConnectionToClient, string> selections = new Dictionary<NetworkConnectionToClient, string>();

    public static CharacterSelectionManager Instance { get; private set; }

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

    [Command(requiresAuthority = false)]
    public void CmdSelectCharacter(NetworkConnectionToClient conn, string characterId)
    {
        selections[conn] = characterId;
        NetworkPlayer player = conn.identity != null ? conn.identity.GetComponent<NetworkPlayer>() : null;
        if (player != null)
        {
            player.selectedCharacterId = characterId;
        }
    }

    public string GetSelection(NetworkConnectionToClient conn)
    {
        string id;
        selections.TryGetValue(conn, out id);
        return id;
    }
}
