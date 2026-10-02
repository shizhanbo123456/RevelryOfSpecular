using System.Collections.Generic;
using UnityEngine;

namespace Ros.Info
{
    public enum LevelUpType
    {
        Health25,
        Strength25,
        Magic25,
        CritRate10,
        CritDamage20,
    }

    [CreateAssetMenu(menuName = "Ros/EntityAttributeInfo", fileName = "EntityAttributeInfo")]
    public class EntityAttributeInfo : ScriptableObject
    {
        public string Name;

        [Header("基础属性")]
        public EntityAttribute baseAttribute = new();

        [Header("升级路线")]
        public LevelUpType upgradeA = LevelUpType.Health25;
        public LevelUpType upgradeB = LevelUpType.Strength25;
        public LevelUpType upgradeC = LevelUpType.Magic25;

        public EntityAttribute GetAttribute(int level = 1)
        {
            var attr = baseAttribute.Clone();
            level = Mathf.Max(1, level);
            for (int lv = 2; lv <= level; lv++)
            {
                var type = GetUpgradeType(lv);
                ApplyUpgrade(attr, type);
            }
            // 返回的是"基础属性"：此处 health 是生命值上限。生成实体时克隆给 floating，同名字段即承载当前生命值（故初始即满血）
            return attr;
        }

        public string GetNextLevelGainDescription(int level)
        {
            if (level >= Config.max_entity_level) return "已达等级上限";
            return FieldName(GetUpgradeType(level + 1));
        }

        #region//Local
        private LevelUpType GetUpgradeType(int lv)
        {
            return ((lv - 2) % 3) switch
            {
                0 => upgradeA,
                1 => upgradeB,
                _ => upgradeC,
            };
        }

        private static void ApplyUpgrade(EntityAttribute attr, LevelUpType type)
        {
            switch (type)
            {
                case LevelUpType.Health25: attr.health += attr.health * 0.25f; break;
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
                LevelUpType.Health25 => "生命值上限 +25%",
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
