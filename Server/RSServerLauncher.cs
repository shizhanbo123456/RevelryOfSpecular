using Ens.Request.Client;
using System.Collections;
using UnityEngine;

public class RSServerLauncher : MonoBehaviour
{
    private void Start()
    {
        EnsServer.RoomManagerFactory = () => new RSRoomManager();
        EnsServer.RoomFactory = i => new RSRoom(i);
        StartCoroutine(StartHost());
    }
    public IEnumerator StartHost()
    {
        yield return new WaitForSeconds(0.3f);
        Debug.LogWarning("Starting Host");
        EnsInstance.Corr.StartHost();
        yield return new WaitForSeconds(0.3f);
        Debug.LogWarning("Start listening");
        EnsInstance.Corr.SetServerListening(true);
        yield return new WaitForSeconds(0.3f);
        Debug.LogWarning("Initializing virtual client");
        JoinRoom.SendRequest(EnsRoomManager.roomIdStart);
        Debug.LogWarning("Server Started");
    }
}