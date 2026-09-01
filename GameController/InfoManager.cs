using System.Collections.Generic;
using Ros.Info;
using UnityEngine;

/// <summary>
/// 简易配置信息管理器。
/// 持有各类 ScriptableObject 配置（Info）与服务器实体模板引用；简易数据可直接在 Inspector 配置。
/// 服务器模板 = 只含组件没有图形的预制体（架构说明）；客户端图形另见 AssetsManager。
/// </summary>
public class InfoManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InfoManager = this;
    }

    #region 角色属性配置（Info）
    /// <summary>进攻方角色属性（18 人）。</summary>
    public List<EntityAttributeInfo> AttackCharacterInfoList = new();
    /// <summary>防守方角色属性（6 人）。</summary>
    public List<EntityAttributeInfo> DefenseCharacterInfoList = new();
    /// <summary>普通僵尸属性（21 种暂用）。</summary>
    public List<EntityAttributeInfo> ZombieInfoList = new();
    /// <summary>精英僵尸属性（14 种）。</summary>
    public List<EntityAttributeInfo> EliteZombieInfoList = new();
    /// <summary>守护点属性（外围信标）。</summary>
    public EntityAttributeInfo BeaconInfo;
    /// <summary>中心守护点属性。</summary>
    public EntityAttributeInfo CoreBeaconInfo;
    /// <summary>可采集水晶属性。</summary>
    public EntityAttributeInfo CrystalInfo;
    /// <summary>防御塔（瘟疫孢子）属性。</summary>
    public EntityAttributeInfo TowerInfo;
    /// <summary>瘟疫树属性。</summary>
    public EntityAttributeInfo PlagueTreeInfo;
    /// <summary>感染蘑菇属性。</summary>
    public EntityAttributeInfo MushroomInfo;
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

    #region 服务器实体模板（只含组件，无图形）
    public List<GameObject> AttackCharacterTemplates = new();
    public List<GameObject> DefenseCharacterTemplates = new();
    public List<GameObject> ZombieTemplates = new();
    public List<GameObject> EliteZombieTemplates = new();
    public List<GameObject> BeaconTemplates = new();
    public GameObject CrystalTemplate;
    public GameObject TowerTemplate;
    public GameObject PlagueTreeTemplate;
    public GameObject MushroomTemplate;

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
            case EntityCategory.Crystal: template = CrystalTemplate; break;
            case EntityCategory.Tower: template = TowerTemplate; break;
            case EntityCategory.PlagueTree: template = PlagueTreeTemplate; break;
            case EntityCategory.Mushroom: template = MushroomTemplate; break;
        }
        return template != null;
    }
    #endregion

    #region 地图配置
    /// <summary>守护点出生点（前 3 个外围 + 最后 1 个中心）。</summary>
    public List<Vector3> BeaconSpawnPositions = new();
    /// <summary>水晶出生点。</summary>
    public List<Vector3> CrystalSpawnPositions = new();
    /// <summary>防御塔出生点。</summary>
    public List<Vector3> TowerSpawnPositions = new();
    /// <summary>瘟疫树出生点。</summary>
    public Vector3 PlagueTreeSpawnPosition;
    /// <summary>僵尸出生点（道路/墓地/守护点外围）。</summary>
    public List<Vector3> ZombieSpawnPositions = new();
    /// <summary>进攻方出生点。</summary>
    public List<Vector3> AttackSpawnPositions = new();
    /// <summary>防守方出生点。</summary>
    public Vector3 DefenseSpawnPosition;
    #endregion

    #region 血条与层级
    /// <summary>实体类别 → 血条 Y 偏移。</summary>
    public Dictionary<EntityCategory, float> EntityBarYOffsetMap = new();
    /// <summary>实体层。</summary>
    public LayerMask EntityLayer = 1;
    /// <summary>技能目标层。</summary>
    public LayerMask SkillTargetLayerMask = ~0;

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
            case EntityCategory.Zombie:
                if (ZombieInfoList.Count > 0) return ZombieInfoList[type.value % ZombieInfoList.Count];
                break;
            case EntityCategory.EliteZombie:
                if (EliteZombieInfoList.Count > 0) return EliteZombieInfoList[type.value % EliteZombieInfoList.Count];
                break;
            case EntityCategory.Beacon:
                return type.value < Config.outer_beacon_count ? BeaconInfo : CoreBeaconInfo;
            case EntityCategory.Crystal: return CrystalInfo;
            case EntityCategory.Tower: return TowerInfo;
            case EntityCategory.PlagueTree: return PlagueTreeInfo;
            case EntityCategory.Mushroom: return MushroomInfo;
        }
        return null;
    }
    #endregion
}
