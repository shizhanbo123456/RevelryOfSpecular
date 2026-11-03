using Ros.Transport;

public partial class NetworkManager
{
    #region 上帝视角（客户端反复请求，服务器只回应，不主动下发）
    public void SendGodViewArea(CSGodViewArea area)
    {
        if (area == null || !CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveGodViewAreaLocal, SendTo.RoomOwner, Delivery.Unreliable, area, LocalToken);
    }

    public void SendGodViewEntityDetail(CSGodViewEntityDetail request)
    {
        if (request == null || !CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveGodViewEntityDetailLocal, SendTo.RoomOwner, Delivery.Reliable, request, LocalToken);
    }

    public void SendGodViewEntities(int clientId, SCGodViewEntities chunk)
    {
        if (chunk == null || !TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveGodViewEntitiesLocal, to, Delivery.Unreliable, chunk);
    }

    public void SendGodViewCorals(int clientId, SCGodViewCorals chunk)
    {
        if (chunk == null || !TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveGodViewCoralsLocal, to, Delivery.Unreliable, chunk);
    }

    public void SendGodViewEntityDetail(int clientId, SCGodViewEntityDetail info)
    {
        if (info == null || !TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveGodViewEntityDetailLocal, to, Delivery.Reliable, info);
    }

    [Rpc]
    private void ServerReceiveGodViewAreaLocal(CSGodViewArea request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveGodViewArea(clientId, request);
    }

    [Rpc]
    private void ServerReceiveGodViewEntityDetailLocal(CSGodViewEntityDetail request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveGodViewEntityDetail(clientId, request);
    }

    [Rpc]
    private void ClientReceiveGodViewEntitiesLocal(SCGodViewEntities chunk)
    {
        if (chunk == null) return;
        EventManager.TrigEvent(ClientEvent.OnGodViewEntities, chunk);
    }

    [Rpc]
    private void ClientReceiveGodViewCoralsLocal(SCGodViewCorals chunk)
    {
        if (chunk == null) return;
        EventManager.TrigEvent(ClientEvent.OnGodViewCorals, chunk);
    }

    [Rpc]
    private void ClientReceiveGodViewEntityDetailLocal(SCGodViewEntityDetail info)
    {
        if (info == null) return;
        EventManager.TrigEvent(ClientEvent.OnGodViewDetail, info);
    }
    #endregion
}
