using System.Collections.Generic;

/// <summary>
/// 消息 ID → 文案映射表。
/// 消息 ID 不硬编码在逻辑中，新增消息在此追加。
/// </summary>
public static class NoticeMessageMap
{
    private static readonly Dictionary<int, string> Map = new()
    {
        { 0, "连接服务器失败" },
        { 1, "服务器信息获取失败" },
        { 2, "输入 IP 地址错误" },
        { 3, "进场失败：房间已满" },
        { 4, "进场失败：正在尝试进入" },
        { 5, "战斗开始" },
        { 6, "进攻方胜利" },
        { 7, "防守方胜利" },
        { 8, "平局" },
        { 9, "时间耗尽，按分数结算" },
        { 10, "进攻方全灭，提前结算" },
        { 11, "中心守护点被摧毁，进攻方获胜" },
        { 12, "当前选中技能无法远程触发" },
        { 13, "武器槽位已满，新武器将替换旧武器" },
        { 14, "获得新武器" },
        { 15, "武器升级" },
        { 16, "守护点正在被攻击" },
    };

    public static string Get(int id)
    {
        return Map.TryGetValue(id, out var text) ? text : $"未知消息({id})";
    }
}
