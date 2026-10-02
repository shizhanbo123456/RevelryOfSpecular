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
    public EntityAttributeInfo BeaconInfo;
    public EntityAttributeInfo CoreBeaconInfo;
    public EntityAttributeInfo CrystalInfo;
    public List<EntityAttributeInfo> TowerInfoList = new();
    public EntityAttributeInfo PlagueTreeInfo;
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
    public List<GameObject> BeaconTemplates = new();
    public List<GameObject> CrystalTemplates = new();
    public List<GameObject> TowerTemplates = new();
    public GameObject PlagueTreeTemplate;

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
            case EntityCategory.Beacon:
                if (BeaconTemplates.Count > 0) template = BeaconTemplates[type.value < Config.outer_beacon_count ? 0 : (BeaconTemplates.Count > 1 ? 1 : 0)];
                break;
            case EntityCategory.Crystal:
                // 下标 = 水晶外观下标（0~11），取模容错；与 AssetsManager.TryGetGraphic 一致
                if (CrystalTemplates.Count > 0) template = CrystalTemplates[Mathf.Max(0, type.value) % CrystalTemplates.Count];
                break;
            case EntityCategory.Tower:
                if (TowerTemplates.Count > 0) template = TowerTemplates[type.value % TowerTemplates.Count];
                break;
            case EntityCategory.PlagueTree: template = PlagueTreeTemplate; break;
        }
        return template != null;
    }
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
            case EntityCategory.Beacon:
                return type.value < Config.outer_beacon_count ? BeaconInfo : CoreBeaconInfo;
            case EntityCategory.Crystal: return CrystalInfo;
            case EntityCategory.Tower:
                return type.value >= 0 && type.value < TowerInfoList.Count ? TowerInfoList[type.value] : null;
            case EntityCategory.PlagueTree: return PlagueTreeInfo;
        }
        return null;
    }
    #endregion
}
