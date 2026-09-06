using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单资产引用管理器（客户端专用，服务器场景不含本组件）。
/// 持有各类大体积资产引用（模型/特效/图标），非特殊内容全部通过它实现。
/// 高复用内容通过 GameController/Utils/AssetsObjectPool 复用。
/// 各列表下标与策划案 V0.9 /《特效清单与分配表》一致（下标 0 起）。
/// </summary>
public class AssetsManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.AssetsManager = this;
    }

    #region 实体图形（客户端表现用；服务器不加载本组件）
    /// <summary>进攻方角色（18 人，下标 = 角色编号）。</summary>
    public List<GameObject> AttackCharacterGraphics = new();
    /// <summary>防守方角色（6 人，下标 = 角色编号）。</summary>
    public List<GameObject> DefenseCharacterGraphics = new();
    /// <summary>普通僵尸（丰富特征 21 种；场上多样性由服务器生成时随机赋 type.value，按 value 取模选用）。</summary>
    public List<GameObject> ZombieGraphics = new();
    /// <summary>精英僵尸（14 种，同上按 value 取模选用）。</summary>
    public List<GameObject> EliteZombieGraphics = new();
    /// <summary>守护点（瘟疫信标）：[0] = 外围矮信标，[1] = 中心高信标。</summary>
    public List<GameObject> BeaconGraphics = new();
    /// <summary>水晶图形：下标 = 水晶类型（策划案第七章，4 种类型对应 4 类武器，见 Config.crystal_type_count）。</summary>
    public List<GameObject> CrystalGraphics = new();
    /// <summary>防御塔（瘟疫孢子）图形：4 种外观，按塔实例编号 value 选用（4 座塔各配一种）。</summary>
    public List<GameObject> TowerGraphics = new();
    public GameObject PlagueTreeGraphic;
    /// <summary>蘑菇图形（水晶被「蘑菇感染」后的形态，与水晶本质相同；多种外观，表现时随机选用一种）。</summary>
    public List<GameObject> MushroomGraphics = new();
    #endregion

    #region 特效（编号与《特效清单与分配表.md》对应）
    /// <summary>子弹特效（60 个：20 类 × 3 颜色变体，下标 0~59 与特效清单一致）。</summary>
    public List<GameObject> BulletVFX = new();
    /// <summary>护盾特效（13 个）。</summary>
    public List<GameObject> ShieldVFX = new();
    /// <summary>范围魔法（10 个）。</summary>
    public List<GameObject> RangeMagicVFX = new();
    /// <summary>魔法阵（10 个）。</summary>
    public List<GameObject> MagicCircleVFX = new();
    /// <summary>Buff 特效（31 个）。</summary>
    public List<GameObject> BuffVFX = new();
    #endregion

    #region 武器（悬浮武器模型，SelectedWeaponPrefabCreator 生成到 Assets/Files/Prefabs/Weapons）
    /// <summary>近战武器（刀，11 把）。</summary>
    public List<GameObject> MeleeWeaponPrefabs = new();
    /// <summary>长枪（7 把）。</summary>
    public List<GameObject> SpearWeaponPrefabs = new();
    /// <summary>枪械（15 把）。</summary>
    public List<GameObject> GunWeaponPrefabs = new();
    /// <summary>魔法球（16 个）。</summary>
    public List<GameObject> MagicOrbPrefabs = new();
    #endregion

    #region UI 图标
    public List<Sprite> CharacterIcons = new();
    public List<Sprite> WeaponIcons = new();
    #endregion

    /// <summary>按实体类型取客户端图形（TODO：各分类图形配置后生效）。</summary>
    public bool TryGetGraphic(EntityType type, out GameObject graphic)
    {
        graphic = null;
        switch (type.category)
        {
            case EntityCategory.Character_Attack:
                if (type.value >= 0 && type.value < AttackCharacterGraphics.Count) graphic = AttackCharacterGraphics[type.value];
                break;
            case EntityCategory.Character_Defense:
                if (type.value >= 0 && type.value < DefenseCharacterGraphics.Count) graphic = DefenseCharacterGraphics[type.value];
                break;
            case EntityCategory.Zombie:
                if (ZombieGraphics.Count > 0) graphic = ZombieGraphics[type.value % ZombieGraphics.Count];
                break;
            case EntityCategory.EliteZombie:
                if (EliteZombieGraphics.Count > 0) graphic = EliteZombieGraphics[type.value % EliteZombieGraphics.Count];
                break;
            case EntityCategory.Beacon:
                if (BeaconGraphics.Count > 0) graphic = BeaconGraphics[type.value < Config.outer_beacon_count ? 0 : (BeaconGraphics.Count > 1 ? 1 : 0)];
                break;
            case EntityCategory.Crystal:
                // 下标 = 水晶类型（0~3 对应刀/长枪/枪械/魔法球）；越界时取末位兜底
                if (CrystalGraphics.Count > 0) graphic = CrystalGraphics[Mathf.Clamp(type.value, 0, CrystalGraphics.Count - 1)];
                break;
            case EntityCategory.Tower:
                // 4 种外观按实例编号选用（Config.tower_count = 4）；配置不足时取模循环
                if (TowerGraphics.Count > 0) graphic = TowerGraphics[type.value % TowerGraphics.Count];
                break;
            case EntityCategory.PlagueTree: graphic = PlagueTreeGraphic; break;
            case EntityCategory.Mushroom:
                // 蘑菇与水晶本质相同（type.value = 被感染水晶的类型），外观多种、表现随机选用
                if (MushroomGraphics.Count > 0) graphic = MushroomGraphics[Random.Range(0, MushroomGraphics.Count)];
                break;
        }
        return graphic != null;
    }
}
