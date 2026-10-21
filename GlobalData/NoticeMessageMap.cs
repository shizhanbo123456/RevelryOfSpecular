public static class NoticeMessageMap
{
    public static string Get(int id)
    {
        var config = ConfigManager.NoticeConfig.Get(id);
        return string.IsNullOrEmpty(config.text) ? $"未知消息({id})" : config.text;
    }
}
