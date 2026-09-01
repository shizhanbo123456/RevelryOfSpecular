using System;

public class RSRoomManager:EnsRoomManager
{
    public RSRoomManager():base()
    {
        rooms.Add(roomIdStart, EnsServer.RoomFactory.Invoke(roomIdStart));
    }
    public override void RecvEvent(EnsConnection conn, int type, string content)
    {
        if (type == 0)
        {
            // 客户端握手：返回服务器可达确认（版本号占位）
            var cid = short.Parse(content);
            TrigClientEvent(conn, Delivery.Reliable, 0, "ok");
        }
    }
    public override void Update()
    {

    }
    public override bool CreateRoom(EnsConnection conn, out int code)
    {
        Utils.Debug.Log("client cannot create room");
        code = 0;
        return false;
    }
    public override bool JoinRoom(EnsConnection conn, int id, out int code)
    {
        if (conn.room != null)
        {
            code = 1;
            return false;
        }
        var room = rooms[roomIdStart];

        room.Join(conn);
        code = room.RoomId;
        return true;
    }
    public override bool ExitRoom(EnsConnection conn, out int id)
    {
        if (conn.room == null)
        {
            id = 0;
            return false;
        }
        conn.room.Exit(conn);
        id = 0;
        return true;
    }
}