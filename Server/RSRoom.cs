using Ens.ProtoFormat;

public class RSRoom : EnsRoom
{
    public RSRoom(int id) : base(id)
    {

    }
    public override void RecvEvent(int type, string content)
    {

    }
    public override void Update()
    {

    }
    public override void Join(EnsConnection conn)
    {
        ClientConnections.Add(conn.ClientId, conn);
        conn.room = this;
        Broadcast(conn.ClientId, Delivery.Reliable, new MessageBody_E_SC { eventId = 1, clientId = conn.ClientId }, Pkt.scE);
        if (CurrentAuthorityAt == -1)
        {
            CurrentAuthorityAt = conn.ClientId;
            ConnectionSend(conn, Delivery.Reliable, new MessageBody_A_SC { authorityActive = true }, Pkt.scA);
        }
        else
        {
            ConnectionSend(conn, Delivery.Reliable, new MessageBody_A_SC { authorityActive = false }, Pkt.scA);
        }
    }
    public override void Exit(EnsConnection conn)
    {
        ClientConnections.Remove(conn.ClientId);
        conn.room = null;

        // 先解映射再通知：只清「令牌 -> 连接」，令牌对应的对局数据由 BattleManager 决定保留还是移除
        bool bound = RSPlayerRegistry.TryGetToken(conn.ClientId, out int token);
        RSPlayerRegistry.Unbind(conn.ClientId);
        if (bound && Tool.BattleManager != null) Tool.BattleManager.NotifyPlayerOffline(token);

        // 房主离开只通知同伴，不销毁房间
        Broadcast(conn.ClientId, Delivery.Reliable, new MessageBody_E_SC { eventId = 2, clientId = conn.ClientId }, Pkt.scE);
    }
    // 顶号：只发一条通知给被顶掉的旧连接，由旧客户端自己走断开流程（服务器不直接踢连接）
    public void KickConnection(short physId)
    {
        if (!ClientConnections.ContainsKey(physId)) return;
        PTP(physId, Delivery.Reliable, new MessageBody_M_SC { type = 2, content = "kicked" }, Pkt.scM);
    }
    public override void SetAuthority(short clientId)
    {
        Utils.Debug.Log("cannot set authority");
    }
}
