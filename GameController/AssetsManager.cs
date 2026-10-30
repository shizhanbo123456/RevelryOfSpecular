using System;
using System.Collections.Generic;
using UnityEngine;

public class AssetsManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.AssetsManager = this;
        SkillIconMap = BuildIconMap(skillIconTextures, nameof(skillIconTextures));
        BuffIconMap = BuildIconMap(buffIconTextures, nameof(buffIconTextures));
        BuildSpawnMask();
    }

    // 图标 id 取贴图名前 3 位（如 "015_霜棱散" → 15）；名字不合规或 id 重复属资源配置错配，直接抛
    private static Dictionary<int, Texture> BuildIconMap(List<Texture> textures, string fieldName)
    {
        var map = new Dictionary<int, Texture>();
        if (textures == null)
            throw new FormatException($"[图标] AssetsManager.{fieldName} 列表为空引用，请在 Inspector 里重新配置");

        foreach (var texture in textures)
        {
            if (texture == null)
                throw new FormatException($"[图标] AssetsManager.{fieldName} 含空引用，请检查 Inspector 列表");
            string textureName = texture.name;
            if (textureName.Length < 3 || !int.TryParse(textureName.Substring(0, 3), out int id))
                throw new FormatException($"[图标] AssetsManager.{fieldName} 的贴图「{textureName}」前 3 位不是图标 id");
            if (map.ContainsKey(id))
                throw new FormatException($"[图标] AssetsManager.{fieldName} 的贴图「{textureName}」id {id} 与「{map[id].name}」重复");
            map.Add(id, texture);
        }
        return map;
    }

    #region 实体图形（客户端表现用；服务器不加载本组件）
    public List<GameObject> AttackCharacterGraphics = new();
    public List<GameObject> DefenseCharacterGraphics = new();
    public List<GameObject> ZombieGraphics = new();
    public List<GameObject> EliteZombieGraphics = new();
    public List<GameObject> SageStatueGraphics = new();      // 副守护点 3 种，下标即 GuardStatue.value 0~2
    public GameObject KingStatueGraphic;                    // 主守护点
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
    public List<GameObject> HitVFX = new();
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
    [SerializeField] private List<Texture> skillIconTextures = new();   // 贴图名前 3 位 = 技能 id
    [SerializeField] private List<Texture> buffIconTextures = new();    // 贴图名前 3 位 = Buff(EffectType) id
    public Texture MapTexture;
    public Texture MapArea;

    public Dictionary<int, Texture> SkillIconMap { get; private set; } = new();
    public Dictionary<int, Texture> BuffIconMap { get; private set; } = new();
    #endregion

    #region 合法刷新区（MapSpace 掩膜，珊瑚/僵尸采样用）
    [Tooltip("合法刷新位置掩膜：红通道 > 128 的像素视为合法；图左下 = 世界(0,0)，上 = +Z，需开 Read/Write")]
    public Texture MapSpace;

    private bool[] spawnMask;   // [v * width + u]，true = 合法（与贴图像素行序一致，左下为 0）
    private int spawnMaskWidth;
    private int spawnMaskHeight;

    // 加载期把掩膜烘成 bool 数组，运行期采样只查数组
    private void BuildSpawnMask()
    {
        if (MapSpace == null)
            throw new FormatException("[刷新区] AssetsManager.MapSpace 未赋值：珊瑚/僵尸无法判断合法刷新位置");

        if (MapSpace is not Texture2D tex || !tex.isReadable)
            throw new FormatException($"[刷新区] AssetsManager.MapSpace「{MapSpace.name}」不是可读的 Texture2D，请在贴图导入设置里勾 Read/Write");

        spawnMaskWidth = tex.width;
        spawnMaskHeight = tex.height;
        Color32[] pixels = tex.GetPixels32();
        spawnMask = new bool[pixels.Length];

        // Unity 的 GetPixels32 与 GetPixelBilinear 同样以左下角为原点，行序与 uv 的 v 一致
        int legalCount = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r <= 128) continue;
            spawnMask[i] = true;
            legalCount++;
        }

        if (legalCount == 0)
            throw new FormatException($"[刷新区] AssetsManager.MapSpace「{tex.name}」没有任何红通道 > 128 的像素：合法刷新区为空，珊瑚/僵尸将永远无法生成");
    }

    public bool IsLegalSpawnXZ(float x, float z)
    {
        float u = x / Landscape.MapSize;
        float v = z / Landscape.MapSize;
        if (u < 0f || v < 0f || u >= 1f || v >= 1f) return false;
        return spawnMask[(int)(v * spawnMaskHeight) * spawnMaskWidth + (int)(u * spawnMaskWidth)];
    }
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
                // 副守护点 value 0~2 各用自己那一种；主守护点 value >= sage_statue_count 用国王
                if (type.value >= Config.sage_statue_count) graphic = KingStatueGraphic;
                else if (type.value >= 0 && type.value < SageStatueGraphics.Count) graphic = SageStatueGraphics[type.value];
                break;
            case EntityCategory.Coral:
                // 小/大珊瑚各用自己列表，列表内随机取一个变体（每个实体创建时只取一次）
                graphic = RandomOf(type.value <= 0 ? CoralSmallGraphics : CoralLargeGraphics);
                break;
            case EntityCategory.CrystalStatue:
                // 3 种外观按实例编号选用（Config.crystal_statue_count = 3）；配置不足时取模循环
                if (CrystalStatueGraphics.Count > 0) graphic = CrystalStatueGraphics[type.value % CrystalStatueGraphics.Count];
                break;
            case EntityCategory.NeutralStatue:
                // 0 天使雕像 / 1 恶魔雕像，按 value 取对应外观
                if (NeutralStatueGraphics.Count > 0) graphic = NeutralStatueGraphics[type.value % NeutralStatueGraphics.Count];
                break;
        }
        return graphic != null;
    }

    private static GameObject RandomOf(List<GameObject> list) => list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : null;

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
