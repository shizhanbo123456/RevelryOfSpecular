using System.Collections.Generic;
using UnityEngine;

namespace Ros.Info
{
    /// <summary>
    /// 升级属性枚举：每次升级按枚举提升对应属性（量纲 = 基础值的固定百分比，加法叠加不乘算）。
    /// 暴击率/暴击伤害为概率/倍率型，最终属性 = 基础 + 次数 × 基础 × 百分比（非 基础×(1+次数×百分比)）。
    /// </summary>
    public enum LevelUpType
    {
        /// <summary>最大生命 +25%。</summary>
        Health25,
        /// <summary>力量 +25%。</summary>
        Strength25,
        /// <summary>魔法 +25%。</summary>
        Magic25,
        /// <summary>暴击率 +10%（概率型，加法）。</summary>
        CritRate10,
        /// <summary>暴击伤害 +20%（倍率型，加法）。</summary>
        CritDamage20,
    }

    /// <summary>
    /// 角色属性配置（ScriptableObject，以 Info 结尾）。
    /// 基础属性 + 升级路线：SO 中定义三个升级枚举（A/B/C），升级到 2~10 级时依次为 A、B、C 提升（轮询）。
    /// 例：A=生命25 B=力量25 C=魔法25 → 2 级用 A、3 级用 B、4 级用 C、5 级用 A……
    /// </summary>
    [CreateAssetMenu(menuName = "Ros/EntityAttributeInfo", fileName = "EntityAttributeInfo")]
    public class EntityAttributeInfo : ScriptableObject
    {
        public string Name;

        [Header("基础属性")]
        public EntityAttribute baseAttribute = new();

        [Header("升级路线（2~10 级按 A→B→C 轮询提升，百分比基于基础属性加法叠加）")]
        public LevelUpType upgradeA = LevelUpType.Health25;
        public LevelUpType upgradeB = LevelUpType.Strength25;
        public LevelUpType upgradeC = LevelUpType.Magic25;

        /// <summary>按等级取属性（level 从 1 开始；2~10 级依次轮询 A/B/C）。</summary>
        public EntityAttribute GetAttribute(int level = 1)
        {
            var attr = baseAttribute.Clone();
            attr.level = Mathf.Max(1, level);
            for (int lv = 2; lv <= attr.level; lv++)
            {
                var type = GetUpgradeType(lv);
                ApplyUpgrade(attr, type);
            }
            // 出生满血
            attr.health = attr.maxHealth;
            return attr;
        }

        /// <summary>下一等级将获得的加成描述（供升级界面显示"本级获得的唯一变化"）。</summary>
        public string GetNextLevelGainDescription(int level)
        {
            if (level >= Config.max_entity_level) return "已达等级上限";
            return FieldName(GetUpgradeType(level + 1));
        }

        #region//Local
        /// <summary>取升到 lv 级时应用的升级枚举（2 级 = A，3 级 = B，4 级 = C，轮询）。</summary>
        private LevelUpType GetUpgradeType(int lv)
        {
            return ((lv - 2) % 3) switch
            {
                0 => upgradeA,
                1 => upgradeB,
                _ => upgradeC,
            };
        }

        /// <summary>应用一次升级（百分比基于基础属性，加法叠加）。</summary>
        private static void ApplyUpgrade(EntityAttribute attr, LevelUpType type)
        {
            switch (type)
            {
                case LevelUpType.Health25: attr.maxHealth += attr.maxHealth * 0.25f; break;
                case LevelUpType.Strength25: attr.strength += (int)(attr.strength * 0.25f); break;
                case LevelUpType.Magic25: attr.magic += (int)(attr.magic * 0.25f); break;
                case LevelUpType.CritRate10: attr.critRate += (int)(attr.critRate * 0.1f); break;
                case LevelUpType.CritDamage20: attr.critDamage += attr.critDamage * 0.2f; break;
            }
        }

        private static string FieldName(LevelUpType type)
        {
            return type switch
            {
                LevelUpType.Health25 => "最大生命 +25%",
                LevelUpType.Strength25 => "力量 +25%",
                LevelUpType.Magic25 => "魔法 +25%",
                LevelUpType.CritRate10 => "暴击率 +10%",
                LevelUpType.CritDamage20 => "暴击伤害 +20%",
                _ => type.ToString(),
            };
        }
        #endregion
    }
}
