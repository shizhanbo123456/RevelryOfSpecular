using System.Collections.Generic;
using UnityEngine;

namespace Ros.Info
{
    /// <summary>
    /// 角色属性配置（ScriptableObject，以 Info 结尾）。
    /// 基础属性 + 独立成长路线（每次升级只提供一个固定、明确的属性加成，见策划案 8.3）。
    /// </summary>
    [CreateAssetMenu(menuName = "Ros/EntityAttributeInfo", fileName = "EntityAttributeInfo")]
    public class EntityAttributeInfo : ScriptableObject
    {
        public string Name;

        [Header("基础属性")]
        public EntityAttribute baseAttribute = new();

        [Header("成长路线：每级固定加成（数组下标 0 = Lv1→Lv2 的加成）")]
        [Tooltip("每个元素代表升到下一级时的唯一属性加成；留空则使用下方线性成长。")]
        public List<EntityAttributeDelta> levelUpGains = new();

        [Header("线性成长（levelUpGains 为空时使用）")]
        public EntityAttributeDelta linearGrowth = new();

        /// <summary>按等级取属性（level 从 1 开始）。</summary>
        public EntityAttribute GetAttribute(int level = 1)
        {
            var attr = baseAttribute.Clone();
            attr.level = Mathf.Max(1, level);
            int lv = attr.level;
            if (levelUpGains != null && levelUpGains.Count > 0)
            {
                // 逐级应用：Lv1→Lv2 用 levelUpGains[0]，依此类推
                for (int i = 0; i < lv - 1 && i < levelUpGains.Count; i++)
                {
                    attr.ApplyDelta(levelUpGains[i]);
                }
            }
            else if (linearGrowth != null && linearGrowth.value != 0f)
            {
                var linear = new EntityAttributeDelta(linearGrowth.field, linearGrowth.value * (lv - 1));
                attr.ApplyDelta(linear);
            }
            // 出生满血
            attr.health = attr.maxHealth;
            return attr;
        }

        /// <summary>下一等级将获得的加成描述（供升级界面显示"本级获得的唯一变化"）。</summary>
        public string GetNextLevelGainDescription(int level)
        {
            if (levelUpGains == null || levelUpGains.Count == 0)
            {
                if (linearGrowth == null || linearGrowth.value == 0f) return "无成长";
                return $"{FieldName(linearGrowth.field)} +{linearGrowth.value}";
            }
            int index = level - 1;
            if (index < 0 || index >= levelUpGains.Count) return "已达等级上限";
            var gain = levelUpGains[index];
            return $"{FieldName(gain.field)} +{gain.value}";
        }

        private static string FieldName(EntityAttributeDelta.Field field)
        {
            switch (field)
            {
                case EntityAttributeDelta.Field.Health: return "最大生命值";
                case EntityAttributeDelta.Field.Strength: return "力量";
                case EntityAttributeDelta.Field.Magic: return "魔法";
                case EntityAttributeDelta.Field.MoveSpeed: return "移动速度";
                case EntityAttributeDelta.Field.CritRate: return "暴击率";
                case EntityAttributeDelta.Field.CritDamage: return "暴击伤害";
                case EntityAttributeDelta.Field.KnockbackResistance: return "击退抗性";
                case EntityAttributeDelta.Field.ViewDistance: return "可见距离";
                case EntityAttributeDelta.Field.WeaponSlotCount: return "武器槽位";
                default: return field.ToString();
            }
        }
    }
}
