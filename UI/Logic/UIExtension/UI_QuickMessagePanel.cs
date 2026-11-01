namespace Ros.UI.Main
{
    // 快捷操作面板：8 行文案取 quick_message.csv，行号即消息 id（1~8 号键）
    public partial class UI_QuickMessagePanel
    {
        public void Build()
        {
            var labels = new[] { m_label1, m_label2, m_label3, m_label4, m_label5, m_label6, m_label7, m_label8 };
            for (int i = 0; i < labels.Length; i++)
            {
                var cfg = ConfigManager.QuickMessageConfig.Get(i);
                labels[i].singleLine = false; // 名称与描述分两行
                labels[i].text = $"{cfg.name}\n{cfg.text}";
            }
        }
    }
}
