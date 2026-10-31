using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 技能悬浮提示：名称/描述/冷却/库存 + 六项评分
    public partial class UI_SkillTip
    {
        private static readonly string[] ScoreNames = { "输出", "冷却恢复", "控制", "支援", "攻击范围", "攻击速度" };

        public void Fill(int skillId)
        {
            var cfg = ConfigManager.SkillConfig.Get(skillId);
            m_SkillName.text = cfg.name;
            m_skillDes.text = cfg.description;
            m_label_cd.text = $"{cfg.cd:0.#}s";
            m_label_store.text = cfg.storeLimit < 0 ? "∞" : cfg.storeLimit.ToString();

            var scores = cfg.scores;
            m_score_list.itemRenderer = (i, obj) =>
            {
                if (obj is not UI_TipLevel row) return;
                row.m_title.text = i < ScoreNames.Length ? ScoreNames[i] : "";
                row.m_level.selectedIndex = Mathf.Clamp(scores[i], 0, 4); // 0~4 = D/C/B/A/S
            };
            m_score_list.numItems = scores.Count;
        }

        public void ShowAt(GObject anchor) => TipLayout.PlaceAbove(this, anchor);

        public void Hide() => visible = false;
    }
}
