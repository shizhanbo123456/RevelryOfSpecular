using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 实体头顶血条：阵营/敌我/血量/名牌/buff，以及自身的屏幕投影与距离缩放
    public partial class UI_EntityBar
    {
        // buff 图标数据：type 即 effect.csv 主键，level = 显示层数
        public readonly struct Buff
        {
            public readonly int type;
            public readonly int level;

            public Buff(int type, int level)
            {
                this.type = type;
                this.level = level;
            }
        }

        private readonly List<Buff> buffs = new();

        // 一次摘要刷新；buffData 为 null = 这条包没带 buff 字段，列表保持原样
        public void Bind(int health, int maxHealth, EntityCamp camp, EntityCamp myCamp, string entityName,
            IReadOnlyList<Buff> buffData, Action<int, GObject> onBuffHoverEnter, Action onBuffHoverOut)
        {
            m_campIcon.m_player_camp.selectedIndex = (int)ToPlayerCamp(camp); // 档位即 PlayerCampIcon 页序
            m_is_enemy.selectedIndex = IsEnemy(camp, myCamp) ? 1 : 0;
            if (m_entity_name != null) m_entity_name.text = entityName ?? "";

            // 友/敌两条血条由 m_is_enemy 互斥显示，两条都写同一个比例
            float ratio = maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;
            m_bar_fill_health_partner.fillAmount = ratio;
            m_bar_fill_health_enemy.fillAmount = ratio;
            if (m_label_health != null) m_label_health.text = $"{health}/{maxHealth}";

            if (buffData != null && m_buff_list != null && m_buff_list.m_list != null)
            {
                buffs.Clear();
                for (int i = 0; i < buffData.Count; i++) buffs.Add(buffData[i]);
                var list = m_buff_list.m_list;
                // 必须先设渲染器再设 numItems
                list.itemRenderer = (i, obj) => ((UI_buffItem)obj).Fill(buffs[i].type, buffs[i].level, onBuffHoverEnter, onBuffHoverOut);
                list.numItems = buffs.Count;
            }
            visible = true;
        }

        // 头顶投影 + 距离缩放：拿不到锚点或锚点在相机背后就隐藏
        public void Place(bool hasAnchor, Vector3 headPos, float defaultSizeDistance)
        {
            var cam = WorldLayer.ProjectionCamera;
            if (!hasAnchor || cam == null)
            {
                visible = false;
                return;
            }
            Vector2 panel = WorldLayer.WorldToPanel(headPos, cam, out bool behind);
            if (behind)
            {
                visible = false;
                return;
            }

            // 血条画在屏幕面板上，不随距离自然缩小：按相机到头顶锚点的距离倒比缩放，默认距离处为 (1,1)
            float distance = Vector3.Distance(cam.transform.position, headPos);
            float scale = defaultSizeDistance / Mathf.Max(distance, 0.01f); // 相机贴到锚点时按 1 厘米算，避免除零
            SetScale(scale, scale);
            visible = true;
            xy = WorldLayer.ToLayer(this, panel) - new Vector2(width * 0.5f * scale, 0f); // 缩放后正中仍落在头顶正上方
        }

        // 实体阵营 → 玩家阵营档位（PlayerCampIcon.m_player_camp 页序：0 进攻 / 1 防守 / 2 观战 / 3 中立）
        private static PlayerCampType ToPlayerCamp(EntityCamp camp)
        {
            EntityCamp display = EntityCampUtil.DisplayCamp(camp);
            if (display == EntityCamp.Attack) return PlayerCampType.Attack;
            if (display == EntityCamp.Defense) return PlayerCampType.Defense;
            return PlayerCampType.Neutral; // 中立雕像、珊瑚与其它场景物件
        }

        // 敌对：只有攻/守两方算阵营；自己阵营未知或对方是中立/场景物件时都不算敌对
        private static bool IsEnemy(EntityCamp camp, EntityCamp myCamp)
        {
            if (myCamp != EntityCamp.Attack && myCamp != EntityCamp.Defense) return false;
            EntityCamp display = EntityCampUtil.DisplayCamp(camp); // 僵尸/防御塔算防守方：进攻方看到是敌方，防守方看到是友方
            if (display != EntityCamp.Attack && display != EntityCamp.Defense) return false;
            return display != myCamp;
        }
    }
}
