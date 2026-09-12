using System.Collections.Generic;
using Ros.Info;
using UnityEngine;

/// <summary>
/// 简易配置信息管理器。
/// 持有各类 ScriptableObject 配置（Info）与服务器实体模板引用；简易数据可直接在 Inspector 配置。
/// 服务器模板 = 只含组件没有图形的预制体（架构说明）；客户端图形另见 AssetsManager。
/// 注：**地图点位不在此配置**，统一由地形组件 LandscapeSpawns 承载（全项目唯一点位来源）。
/// </summary>
public class InfoManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InfoManager = this;
    }

    #region 角色属性配置（Info）
    /// <summary>进攻方角色（18 人，玩家角色信息含解锁等级）。</summary>
    public List<PlayerCharacterInfo> AttackCharacterInfoList = new();
    /// <summary>防守方角色（6 人，玩家角色信息含解锁等级）。</summary>
    public List<PlayerCharacterInfo> DefenseCharacterInfoList = new();
    /// <summary>普通僵尸属性（全场共用 1 份；21 种只是外观变体，强弱由「僵尸刷新等级」驱动，见策划案 10.3/二十章）。</summary>
    public EntityAttributeInfo ZombieInfo;
    /// <summary>精英僵尸属性（14 份，对应 14 个素材模型，按 type.value 索引，由技能召唤产生）。</summary>
    public List<EntityAttributeInfo> EliteZombieInfoList = new();
    /// <summary>守护点属性（外围信标）。</summary>
    public EntityAttributeInfo BeaconInfo;
    /// <summary>中心守护点属性。</summary>
    public EntityAttributeInfo CoreBeaconInfo;
    /// <summary>可采集水晶属性。</summary>
    public EntityAttributeInfo CrystalInfo;
    /// <summary>防御塔（瘟疫孢子）属性（4 种，按 type.value 索引）。</summary>
    public List<EntityAttributeInfo> TowerInfoList = new();
    /// <summary>瘟疫树属性。</summary>
    public EntityAttributeInfo PlagueTreeInfo;
    #endregion

    #region 技能配置
    /// <summary>技能配置列表（下标=技能 id 的辅助映射，具体以 SkillManager 注册表为准）。</summary>
    public List<SkillInfo> SkillInfoList = new();

    /// <summary>按技能 id 查找配置。</summary>
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
    /// <summary>水晶模板：按类别配 4 项即可（对应 4 类武器，见 Config.crystal_type_count）；取用时按 下标 % Count 归到类别（与 AssetsManager.CrystalGraphics 一致）。</summary>
    public List<GameObject> CrystalTemplates = new();
    /// <summary>防御塔模板（瘟疫孢子）：4 种外观，按塔实例编号取模选用（与 AssetsManager.TowerGraphics 一致）。</summary>
    public List<GameObject> TowerTemplates = new();
    public GameObject PlagueTreeTemplate;

    /// <summary>按实体类型取服务器模板（TODO：各分类模板配置后生效）。</summary>
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
                // 下标 = 水晶外观下标；配 4 项时取模即按类别，配 12 项时即按外观（与 AssetsManager.TryGetGraphic 一致）
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

    #region 血条与层级
    /// <summary>实体类别 → 血条 Y 偏移。</summary>
    public Dictionary<EntityCategory, float> EntityBarYOffsetMap = new();

    /// <summary>实体血条 Y 偏移（默认头顶 0.9m）。</summary>
    public float GetEntityBarYOffset(EntityType type)
    {
        if (EntityBarYOffsetMap != null && EntityBarYOffsetMap.TryGetValue(type.category, out var offset))
        {
            return offset;
        }
        return 0.9f;
    }
    #endregion

    #region 属性获取
    /// <summary>按实体类型与等级取属性配置（运行时属性见 EntityData.OnCreate）。</summary>
    public EntityAttribute GetAttribute(EntityType type, int level)
    {
        var info = GetAttributeInfo(type);
        return info != null ? info.GetAttribute(level) : new EntityAttribute();
    }

    /// <summary>按全局角色索引取玩家角色信息（进攻 0~17 / 防守 18~23；无配置返回 null）。</summary>
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

    /// <summary>按实体类型取属性配置资产。</summary>
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
