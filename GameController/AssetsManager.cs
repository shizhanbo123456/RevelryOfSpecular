using System.Collections.Generic;
using UnityEngine;

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
    public List<GameObject> CrystalGraphics = new();
    public List<GameObject> TowerGraphics = new();
    public GameObject PlagueTreeGraphic;
    public List<GameObject> MushroomGraphics = new();
    #endregion

    #region 特效（编号与《特效清单与分配表.md》对应）
    public List<GameObject> BulletVFX = new();
    public List<GameObject> ShieldVFX = new();
    public List<GameObject> RangeMagicVFX = new();
    public List<GameObject> MagicCircleVFX = new();
    public List<GameObject> BuffVFX = new();
    #endregion

    #region 武器（悬浮武器模型，SelectedWeaponPrefabCreator 生成到 Assets/Files/Prefabs/Weapons）
    public List<GameObject> MeleeWeaponPrefabs = new();
    public List<GameObject> SpearWeaponPrefabs = new();
    public List<GameObject> GunWeaponPrefabs = new();
    public List<GameObject> MagicOrbPrefabs = new();
    #endregion

    #region UI 图标
    public List<Sprite> AttackCharacterIcons = new();
    public List<Sprite> DefenseCharacterIcons = new();
    #endregion

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
                // 下标 = 水晶外观下标（0~11）；k/k+4/k+8 为一类，类别 = 下标 % 4；配置不足时取模循环
                if (CrystalGraphics.Count > 0) graphic = CrystalGraphics[Mathf.Max(0, type.value) % CrystalGraphics.Count];
                break;
            case EntityCategory.Tower:
                // 4 种外观按实例编号选用（Config.tower_count = 4）；配置不足时取模循环
                if (TowerGraphics.Count > 0) graphic = TowerGraphics[type.value % TowerGraphics.Count];
                break;
            case EntityCategory.PlagueTree: graphic = PlagueTreeGraphic; break;
        }
        return graphic != null;
    }

    public bool TryGetWeaponPrefab(WeaponRef weapon, out GameObject prefab)
    {
        prefab = null;
        if (!weapon.IsValid) return false;
        switch (weapon.category)
        {
            case WeaponCategory.Knife:    prefab = WeaponAt(MeleeWeaponPrefabs, weapon.index); break;
            case WeaponCategory.Spear:    prefab = WeaponAt(SpearWeaponPrefabs, weapon.index); break;
            case WeaponCategory.Gun:      prefab = WeaponAt(GunWeaponPrefabs, weapon.index); break;
            case WeaponCategory.MagicOrb: prefab = WeaponAt(MagicOrbPrefabs, weapon.index); break;
        }
        return prefab != null;
    }

    private static GameObject WeaponAt(List<GameObject> list, int index)
    {
        return list != null && index >= 0 && index < list.Count ? list[index] : null;
    }
}
