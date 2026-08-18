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
        E_EventMessageWriter.instance.b = 0x01;
        E_EventMessageWriter.instance.connId = conn.ClientId;
        Broadcast(conn.ClientId, Header.E, Delivery.Reliable, E_EventMessageWriter.instance);
        if (CurrentAuthorityAt == -1)
        {
            CurrentAuthorityAt = conn.ClientId;
            BoolWriter.instance.target = true;
            ConnectionSend(conn,Header.A, Delivery.Reliable, BoolWriter.instance);
        }
        else
        {
            BoolWriter.instance.target = false;
            ConnectionSend(conn, Header.A, Delivery.Reliable, BoolWriter.instance);
        }
    }
    public override void Exit(EnsConnection conn)
    {
        ClientConnections.Remove(conn.ClientId);
        conn.room = null;

        E_EventMessageWriter.instance.b = 0x02;
        E_EventMessageWriter.instance.connId = conn.ClientId;
        Broadcast(conn.ClientId, Header.E, Delivery.Reliable, E_EventMessageWriter.instance);
    }
    public override void SetAuthority(short clientId)
    {
        Utils.Debug.Log("cannot set authority");
    }
}