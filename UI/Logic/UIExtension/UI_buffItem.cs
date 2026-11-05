using System;
using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // buff 图标条目：正负档、图标、层数，悬浮回调交给调用方
    public partial class UI_buffItem
    {
        public void Fill(int buffType, int level, Action<int, GObject> onHoverEnter, Action onHoverOut)
        {
            var cfg = ConfigManager.EffectConfig.Get(buffType);
            m_negative.selectedIndex = cfg.positive == true ? 0 : 1; // 0 正面 / 1 负面
            Texture icon = null;
            var map = Tool.AssetsManager != null ? Tool.AssetsManager.BuffIconMap : null;
            if (map != null && map.TryGetValue(cfg.iconId, out var tex)) icon = tex;
            m_mask.m_loader.texture = icon != null ? new NTexture(icon) : null; // 没配图标 → 不显示
            // level 即显示层数：逐层计时类 = 各层等级之和，单层类 = 该条等级（服务端已聚合）
            m_layer_count.text = level.ToString();
            // GList 复用条目，先清后加以免叠加
            onRollOver.Set(() => onHoverEnter?.Invoke(buffType, this));
            onRollOut.Set(() => onHoverOut?.Invoke());
        }
    }
}
