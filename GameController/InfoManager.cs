using System.Collections.Generic;
using Ros.Info;
using UnityEngine;

public class InfoManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InfoManager = this;
    }

    public int entity_layer;

    public int ground_layer;

    #region 角色属性配置（Info）
    public List<PlayerCharacterInfo> AttackCharacterInfoList = new();
    public List<PlayerCharacterInfo> DefenseCharacterInfoList = new();
    public EntityAttributeInfo ZombieInfo;
    public List<EntityAttributeInfo> EliteZombieInfoList = new();
    public EntityAttributeInfo SageStatueInfo;
    public EntityAttributeInfo KingStatueInfo;
    public EntityAttributeInfo CoralSmallInfo;
    public EntityAttributeInfo CoralLargeInfo;
    public List<EntityAttributeInfo> CrystalStatueInfoList = new();
    public EntityAttributeInfo NeutralStatueInfo;
    #endregion

    #region 技能配置
    public List<SkillInfo> SkillInfoList = new();

    public SkillInfo GetSkillInfo(int id)
    {
        if (SkillInfoList == null) return null;
        foreach (var info in SkillInfoList)
        {
            if (info != null && info.id == id) return info;
        }
        return null;
    }
    #endregion

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

    #region 属性获取
    public EntityAttribute GetAttribute(EntityType type, int level)
    {
        var info = GetAttributeInfo(type);
        return info != null ? info.GetAttribute(level) : new EntityAttribute();
    }

    public PlayerCharacterInfo GetPlayerCharacterInfo(int globalIndex)
    {
        if (globalIndex < 0) return null;
        if (globalIndex < Config.attack_character_count)
        {
            return globalIndex < AttackCharacterInfoList.Count ? AttackCharacterInfoList[globalIndex] : null;
        }
        int defIndex = globalIndex - Config.attack_character_count;
        return defIndex < DefenseCharacterInfoList.Count ? DefenseCharacterInfoList[defIndex] : null;
    }

    public EntityAttributeInfo GetAttributeInfo(EntityType type)
    {
        switch (type.category)
        {
            case EntityCategory.Character_Attack:
                if (type.value >= 0 && type.value < AttackCharacterInfoList.Count) return AttackCharacterInfoList[type.value];
                break;
            case EntityCategory.Character_Defense:
                if (type.value >= 0 && type.value < DefenseCharacterInfoList.Count) return DefenseCharacterInfoList[type.value];
                break;
            case EntityCategory.Zombie: return ZombieInfo;
            case EntityCategory.EliteZombie:
                if (EliteZombieInfoList.Count > 0) return EliteZombieInfoList[type.value % EliteZombieInfoList.Count];
                break;
            case EntityCategory.GuardStatue:
                return type.value < Config.sage_statue_count ? SageStatueInfo : KingStatueInfo;
            case EntityCategory.Coral:
                return type.value <= 0 ? CoralSmallInfo : CoralLargeInfo;
            case EntityCategory.CrystalStatue:
                return type.value >= 0 && type.value < CrystalStatueInfoList.Count ? CrystalStatueInfoList[type.value] : null;
            case EntityCategory.NeutralStatue: return NeutralStatueInfo;
        }
        return null;
    }
    #endregion
}
