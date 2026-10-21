using System.Collections.Generic;
using UnityEngine;

public class InfoManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InfoManager = this;
    }

    public int entity_layer;

    public int ground_layer;

    #region 服务器实体模板（只含组件，无图形；下标语义与 AssetsManager 的图形列表保持一致）
    public List<GameObject> AttackCharacterTemplates = new();
    public List<GameObject> DefenseCharacterTemplates = new();
    public List<GameObject> ZombieTemplates = new();
    public List<GameObject> EliteZombieTemplates = new();
    public List<GameObject> GuardStatueTemplates = new();
    public List<GameObject> CoralSmallTemplates = new();
    public List<GameObject> CoralLargeTemplates = new();
    public List<GameObject> CrystalStatueTemplates = new();
    public List<GameObject> NeutralStatueTemplates = new();
    [Space]
    public GameObject SpringWeapon;//挂载SpringWeapon组件

    public bool TryGetTemplate(EntityType type, out GameObject template)
    {
        template = null;
        switch (type.category)
        {
            case EntityCategory.Character_Attack:
                if (type.value >= 0 && type.value < AttackCharacterTemplates.Count) template = AttackCharacterTemplates[type.value];
                break;
            case EntityCategory.Character_Defense:
                if (type.value >= 0 && type.value < DefenseCharacterTemplates.Count) template = DefenseCharacterTemplates[type.value];
                break;
            case EntityCategory.Zombie:
                if (ZombieTemplates.Count > 0) template = ZombieTemplates[type.value % ZombieTemplates.Count];
                break;
            case EntityCategory.EliteZombie:
                if (EliteZombieTemplates.Count > 0) template = EliteZombieTemplates[type.value % EliteZombieTemplates.Count];
                break;
            case EntityCategory.GuardStatue:
                if (GuardStatueTemplates.Count > 0) template = GuardStatueTemplates[type.value < Config.sage_statue_count ? 0 : (GuardStatueTemplates.Count > 1 ? 1 : 0)];
                break;
            case EntityCategory.Coral:
                // 服务器模板只承载组件：小/大珊瑚各取自己列表的首个
                template = FirstTemplate(type.value <= 0 ? CoralSmallTemplates : CoralLargeTemplates);
                break;
            case EntityCategory.CrystalStatue:
                if (CrystalStatueTemplates.Count > 0) template = CrystalStatueTemplates[type.value % CrystalStatueTemplates.Count];
                break;
            case EntityCategory.NeutralStatue:
                // 天使 / 恶魔雕像共用一套组件模板；配置两个则按 value 区分外观与模板
                if (NeutralStatueTemplates.Count > 0) template = NeutralStatueTemplates[type.value % NeutralStatueTemplates.Count];
                break;
        }
        return template != null;
    }

    private static GameObject FirstTemplate(List<GameObject> list) => list.Count > 0 ? list[0] : null;
    #endregion

    #region 属性（数据源：StreamingAssets/ConfigData/entity.csv）
    public EntityAttribute GetAttribute(EntityType type, int level)
    {
        return ConfigManager.EntityConfig.GetByCategory(type).GetAttribute(level);
    }

    public ConfigManager.EntityConfig GetEntityConfig(EntityType type)
    {
        return ConfigManager.EntityConfig.GetByCategory(type);
    }

    // 全局角色下标：0~17 进攻方，18~23 防守方
    public ConfigManager.EntityConfig GetPlayerCharacterInfo(int globalIndex)
    {
        if (globalIndex < Config.attack_character_count)
            return ConfigManager.EntityConfig.Get(EntityType.Attack(globalIndex));

        return ConfigManager.EntityConfig.Get(EntityType.Defense(globalIndex - Config.attack_character_count));
    }
    #endregion
}
