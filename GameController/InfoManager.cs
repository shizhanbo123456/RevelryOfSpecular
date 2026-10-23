using UnityEngine;

public class InfoManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InfoManager = this;
    }

    public int entity_layer;

    public int ground_layer;

    public GameObject SpringWeapon;//挂载SpringWeapon组件

    #region 属性（数据源：StreamingAssets/ConfigData/entity.csv）
    public EntityAttribute GetAttribute(EntityType type, int level)
    {
        return ConfigManager.EntityConfig.GetByCategory(type).GetAttribute(level);
    }

    public ConfigManager.EntityConfig GetEntityConfig(EntityType type)
    {
        return ConfigManager.EntityConfig.GetByCategory(type);
    }

    // 全局角色下标：0~16 进攻方，17~22 防守方
    public ConfigManager.EntityConfig GetPlayerCharacterInfo(int globalIndex)
    {
        if (globalIndex < Config.attack_character_count)
            return ConfigManager.EntityConfig.Get(EntityType.Attack(globalIndex));

        return ConfigManager.EntityConfig.Get(EntityType.Defense(globalIndex - Config.attack_character_count));
    }
    #endregion
}
