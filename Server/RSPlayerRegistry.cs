using System.Collections.Generic;

// 令牌 ↔ 连接 id 映射。游戏层一律用令牌标识玩家（协议字段同名字段即令牌），
// 只有发包寻址需要换算成"当前连接 id"——连接 id 每次断线重连都会变，令牌不变。
public static class RSPlayerRegistry
{
    private static readonly Dictionary<int, short> physByToken = new();
    private static readonly Dictionary<short, int> tokenByPhys = new();

    // 绑定连接；同一令牌已挂在别的连接上时返回旧连接 id（顶号由调用方负责发 kick 并清理）
    public static short Bind(int token, short physId)
    {
        short replaced = -1;
        if (physByToken.TryGetValue(token, out var oldPhysId))
        {
            if (oldPhysId == physId) return -1;
            tokenByPhys.Remove(oldPhysId);
            replaced = oldPhysId;
        }
        if (tokenByPhys.TryGetValue(physId, out var oldToken) && oldToken != token) physByToken.Remove(oldToken);
        physByToken[token] = physId;
        tokenByPhys[physId] = token;
        return replaced;
    }

    // 连接断开：只解开映射，令牌对应的游戏数据由会话层保留（见 RSRoom/PlayerSession）
    public static void Unbind(short physId)
    {
        if (!tokenByPhys.TryGetValue(physId, out var token)) return;
        tokenByPhys.Remove(physId);
        if (physByToken.TryGetValue(token, out var bound) && bound == physId) physByToken.Remove(token);
    }

    public static bool TryGetPhys(int token, out short physId) => physByToken.TryGetValue(token, out physId);

    public static bool TryGetToken(short physId, out int token) => tokenByPhys.TryGetValue(physId, out token);

    public static void Clear()
    {
        physByToken.Clear();
        tokenByPhys.Clear();
    }
}
