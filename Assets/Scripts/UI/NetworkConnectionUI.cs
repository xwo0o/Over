using Mirror;
using UnityEngine;

public class NetworkConnectionUI : MonoBehaviour
{
    public void StartHost()
    {
        NetworkManager.singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.singleton.StartClient();
    }

    public void StopHostOrClient()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
        }
    }
}
