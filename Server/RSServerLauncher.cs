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
        yield return new WaitForSeconds(2);
        EnsInstance.Corr.StartHost();
        EnsInstance.Corr.SetServerListening(true);
        yield return new WaitForSeconds(1);
        JoinRoom.SendRequest(EnsRoomManager.roomIdStart);
        Debug.LogWarning("Server Started");
    }
}