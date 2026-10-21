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
    public List<GameObject> GuardStatueGraphics = new();
    public List<GameObject> CoralSmallGraphics = new();
    public List<GameObject> CoralLargeGraphics = new();
    public List<GameObject> CrystalStatueGraphics = new();
    public List<GameObject> NeutralStatueGraphics = new();
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
    public List<Texture2D> AttackCharacterIcons = new();
    public List<Texture2D> DefenseCharacterIcons = new();
    public Texture MapTexture;
    public Texture MapArea;
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
            case EntityCategory.GuardStatue:
                if (GuardStatueGraphics.Count > 0) graphic = GuardStatueGraphics[type.value < Config.sage_statue_count ? 0 : (GuardStatueGraphics.Count > 1 ? 1 : 0)];
                break;
            case EntityCategory.Coral:
                // 小/大珊瑚各用自己列表，列表内随机取一个变体（每个实体创建时只取一次）
                graphic = RandomOf(type.value <= 0 ? CoralSmallGraphics : CoralLargeGraphics);
                break;
            case EntityCategory.CrystalStatue:
                // 4 种外观按实例编号选用（Config.crystal_statue_count = 4）；配置不足时取模循环
                if (CrystalStatueGraphics.Count > 0) graphic = CrystalStatueGraphics[type.value % CrystalStatueGraphics.Count];
                break;
            case EntityCategory.NeutralStatue:
                // 0 天使雕像 / 1 恶魔雕像，按 value 取对应外观
                if (NeutralStatueGraphics.Count > 0) graphic = NeutralStatueGraphics[type.value % NeutralStatueGraphics.Count];
                break;
        }
        return graphic != null;
    }

    private static GameObject RandomOf(List<GameObject> list) => list.Count > 0 ? list[Random.Range(0, list.Count)] : null;

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
