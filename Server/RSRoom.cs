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

        // 房主离开只通知同伴，不销毁房间
        Broadcast(conn.ClientId, Delivery.Reliable, new MessageBody_E_SC { eventId = 2, clientId = conn.ClientId }, Pkt.scE);
    }
    public override void SetAuthority(short clientId)
    {
        Utils.Debug.Log("cannot set authority");
    }
}
