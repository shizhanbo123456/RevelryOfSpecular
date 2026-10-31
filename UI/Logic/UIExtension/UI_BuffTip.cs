using FairyGUI;

namespace Ros.UI.Main
{
    // buff 悬浮提示：名称与描述取自 effect.csv
    public partial class UI_BuffTip
    {
        public void Fill(int buffType)
        {
            var cfg = ConfigManager.EffectConfig.Get(buffType);
            m_buff_name.text = cfg.name;
            m_buff_des.text = cfg.description;
        }

        public void ShowAt(GObject anchor) => TipLayout.PlaceAbove(this, anchor);

        public void Hide() => visible = false;
    }
}
