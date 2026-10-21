public static class NoticeMessageMap
{
    // 配表缺条目时仍给可读占位，避免 UI 因提示文案缺失中断
    public static string Get(int id)
    {
        return ConfigManager.NoticeConfig.content.TryGetValue(id, out var config) && !string.IsNullOrEmpty(config.text)
            ? config.text
            : $"未知消息({id})";
    }
}
