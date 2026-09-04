using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单资产引用管理器（客户端专用，服务器场景不含本组件）。
/// 持有各类大体积资产引用（模型/特效/图标），非特殊内容全部通过它实现。
/// 高复用内容通过 GameController/Utils/AssetsObjectPool 复用。
/// </summary>
public class AssetsManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.AssetsManager = this;
    }

    #region 实体图形（客户端表现用；服务器不加载本组件）
    public List<GameObject> AttackCharacterGraphics = new();
    public List<GameObject> DefenseCharacterGraphics = new();
    public List<GameObject> ZombieGraphics = new();
    public List<GameObject> EliteZombieGraphics = new();
    public List<GameObject> BeaconGraphics = new();
    public GameObject CrystalGraphic;
    public GameObject TowerGraphic;
    public GameObject PlagueTreeGraphic;
    public GameObject MushroomGraphic;
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
            case EntityCategory.Crystal: graphic = CrystalGraphic; break;
            case EntityCategory.Tower: graphic = TowerGraphic; break;
            case EntityCategory.PlagueTree: graphic = PlagueTreeGraphic; break;
            case EntityCategory.Mushroom: graphic = MushroomGraphic; break;
        }
        return graphic != null;
    }
}
