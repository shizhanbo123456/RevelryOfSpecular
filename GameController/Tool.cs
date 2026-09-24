using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 全局工具/管理器引用（架构说明：通过 static 字段持有 GameController 中每一个 Mono 管理器的引用）。
/// 所有管理器在 Awake 中注册到对应 static 字段。
/// </summary>
[ExecuteInEditMode]
public class Tool : MonoBehaviour
{
    [SerializeField][Range(0,100)] private int delay = 20;

    /// <summary>场景角色：勾选 = 服务器场景（不校验仅客户端存在的管理器与资产）。</summary>
    [Header("启动自检")]
    public bool isServer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        Thread.Sleep(delay);
        Timer.Update();
    }

    /// <summary>启动 1 秒后自检（等各管理器在 Awake 里注册完）。</summary>
    private void Start()
    {
        if (!Application.isPlaying) return; // 本类带 ExecuteInEditMode，编辑模式也会跑 Start
        Timer.AddTimer(this, _ => RunStartupCheck(), startupCheckDelay, 1, false);
    }

    public static Tool Instance { get; private set; }

    #region 管理器引用（由各管理器 Awake 注册）
    public static NetworkManager NetworkManager;
    public static BattleManager BattleManager;
    public static InfoManager InfoManager;
    public static AssetsManager AssetsManager;
    public static InputManager InputManager;
    public static SaveManager SaveManager;
    public static EnvironmentManager EnvironmentManager;
    public static CameraController CameraController;
    public static ClientLogicManager ClientLogicManager;
    public static AssetsObjectPool AssetsObjectPool;
    public static VfxManager VfxManager;
    public static TransitionManager TransitionManager;

    /// <summary>
    /// 地形生成锚点（全项目唯一地图点位来源）。
    /// 读取前提：地形预制体（挂 LandscapeSpawns 组件）已随场景加载——服务器场景同样必须加载。
    /// 未注册时取用即报错，调用方不做事后兜底。
    /// </summary>
    public static LandscapeSpawns LandscapeSpawns
    {
        get
        {
            if (s_landscapeSpawns == null)
            {
                Debug.LogError("[Tool] LandscapeSpawns 未注册：地形预制体（挂 LandscapeSpawns 组件）必须先随场景加载。");
            }
            return s_landscapeSpawns;
        }
        set => s_landscapeSpawns = value;
    }
    private static LandscapeSpawns s_landscapeSpawns;
    #endregion

    #region 通用工具
    private static Transform t_calTransform;
    public static Transform GetTemporaryTransform()
    {
        if (t_calTransform == null)
        {
            t_calTransform = new GameObject("calTransform").transform;
            DontDestroyOnLoad(t_calTransform.gameObject);
        }
        return t_calTransform;
    }
    public static void ActiveFor<T>(List<T> list, int count) where T : Component
    {
        while (list.Count < count)
        {
            list.Add(Instantiate(list[0].gameObject, list[0].transform.parent).GetComponent<T>());
        }
        for (int i = 0; i < list.Count; i++)
        {
            list[i].gameObject.SetActive(i < count);
        }
    }
    public static void ActiveFor(List<GameObject> list, int count)
    {
        while (list.Count < count)
        {
            list.Add(Instantiate(list[0], list[0].transform.parent));
        }
        for (int i = 0; i < list.Count; i++)
        {
            list[i].gameObject.SetActive(i < count);
        }
    }
    public static void ExpandListTo<T>(List<T> list, int count, T value = default)
    {
        while (list.Count < count)
        {
            list.Add(value);
        }
    }
    #endregion

    #region 启动自检
    /// <summary>自检：管理器是否齐全、配置数量是否与 Config 一致、资产是否被删（空项）。</summary>
    private void RunStartupCheck()
    {
        int errors = 0;

        errors += CheckObject("Tool 自身", Instance);
        errors += CheckObject(nameof(NetworkManager), NetworkManager);
        errors += CheckObject(nameof(BattleManager), BattleManager);
        errors += CheckObject(nameof(InfoManager), InfoManager);
        errors += CheckObject(nameof(EnvironmentManager), EnvironmentManager);
        errors += CheckObject("地形 LandscapeSpawns（唯一点位来源）", s_landscapeSpawns);
        if (!isServer)
        {
            errors += CheckObject(nameof(AssetsManager), AssetsManager);
            errors += CheckObject(nameof(InputManager), InputManager);
            errors += CheckObject(nameof(SaveManager), SaveManager);
            errors += CheckObject(nameof(CameraController), CameraController);
            errors += CheckObject(nameof(ClientLogicManager), ClientLogicManager);
            errors += CheckObject(nameof(VfxManager), VfxManager);
        }

        errors += CheckInfoManager();
        if (!isServer) errors += CheckAssetsManager();

        string mode = isServer ? "服务器" : "客户端";
        if (errors == 0) Debug.Log($"[启动自检] 通过（{mode}）");
        else Debug.LogError($"[启动自检] {mode}：共 {errors} 处问题，详见上方日志");
    }

    /// <summary>InfoManager：属性配置与服务器实体模板（两端都需要）。</summary>
    private static int CheckInfoManager()
    {
        var m = InfoManager;
        if (m == null) return 0; // 缺失已在管理器检查里报过

        int e = 0;
        if (m.entity_layer == 0)
        {
            Debug.LogError("[启动自检] InfoManager.entity_layer 未配置（当前 0）：物理查询会与 Default 层混在一起");
            e++;
        }
        if (m.ground_layer == 0)
        {
            Debug.LogError("[启动自检] InfoManager.ground_layer 未配置（当前 0）：落地检测会退化为 Default 层");
            e++;
        }

        e += CheckList("InfoManager.AttackCharacterInfoList", m.AttackCharacterInfoList, Config.attack_character_count);
        e += CheckList("InfoManager.DefenseCharacterInfoList", m.DefenseCharacterInfoList, Config.defense_character_count);
        e += CheckObject("InfoManager.ZombieInfo", m.ZombieInfo);
        e += CheckList("InfoManager.EliteZombieInfoList", m.EliteZombieInfoList, Config.elite_zombie_variant_count);
        e += CheckObject("InfoManager.BeaconInfo", m.BeaconInfo);
        e += CheckObject("InfoManager.CoreBeaconInfo", m.CoreBeaconInfo);
        e += CheckObject("InfoManager.CrystalInfo", m.CrystalInfo);
        e += CheckList("InfoManager.TowerInfoList", m.TowerInfoList, Config.tower_count);
        e += CheckObject("InfoManager.PlagueTreeInfo", m.PlagueTreeInfo);

        e += CheckList("InfoManager.AttackCharacterTemplates", m.AttackCharacterTemplates, Config.attack_character_count);
        e += CheckList("InfoManager.DefenseCharacterTemplates", m.DefenseCharacterTemplates, Config.defense_character_count);
        e += CheckList("InfoManager.ZombieTemplates", m.ZombieTemplates, Config.zombie_variant_count);
        e += CheckList("InfoManager.EliteZombieTemplates", m.EliteZombieTemplates, Config.elite_zombie_variant_count);
        e += CheckList("InfoManager.BeaconTemplates", m.BeaconTemplates, beacon_type_count);
        e += CheckList("InfoManager.CrystalTemplates", m.CrystalTemplates, Config.crystal_graphics_count);
        e += CheckList("InfoManager.TowerTemplates", m.TowerTemplates, Config.tower_count);
        e += CheckObject("InfoManager.PlagueTreeTemplate", m.PlagueTreeTemplate);

        e += CheckList("InfoManager.SkillInfoList", m.SkillInfoList, SkillManager.GetSkillCount());
        if (m.EntityBarYOffsetMap == null || m.EntityBarYOffsetMap.Count == 0)
        {
            Debug.LogWarning("[启动自检] InfoManager.EntityBarYOffsetMap 为空：血条偏移全部走默认值");
        }
        return e;
    }

    /// <summary>AssetsManager：客户端图形、特效、武器与图标（仅客户端）。</summary>
    private static int CheckAssetsManager()
    {
        var m = AssetsManager;
        if (m == null) return 0;

        int e = 0;
        e += CheckList("AssetsManager.AttackCharacterGraphics", m.AttackCharacterGraphics, Config.attack_character_count);
        e += CheckList("AssetsManager.DefenseCharacterGraphics", m.DefenseCharacterGraphics, Config.defense_character_count);
        e += CheckList("AssetsManager.ZombieGraphics", m.ZombieGraphics, Config.zombie_variant_count);
        e += CheckList("AssetsManager.EliteZombieGraphics", m.EliteZombieGraphics, Config.elite_zombie_variant_count);
        e += CheckList("AssetsManager.BeaconGraphics", m.BeaconGraphics, beacon_type_count);
        e += CheckList("AssetsManager.CrystalGraphics", m.CrystalGraphics, Config.crystal_graphics_count);
        e += CheckList("AssetsManager.TowerGraphics", m.TowerGraphics, Config.tower_count);
        e += CheckObject("AssetsManager.PlagueTreeGraphic", m.PlagueTreeGraphic);

        e += CheckList("AssetsManager.MushroomGraphics", m.MushroomGraphics, 0); // 变体数未定稿，只查空项

        e += CheckList("AssetsManager.BulletVFX", m.BulletVFX, vfx_bullet_count);
        e += CheckList("AssetsManager.ShieldVFX", m.ShieldVFX, vfx_shield_count);
        e += CheckList("AssetsManager.RangeMagicVFX", m.RangeMagicVFX, vfx_range_magic_count);
        e += CheckList("AssetsManager.MagicCircleVFX", m.MagicCircleVFX, vfx_magic_circle_count);
        e += CheckList("AssetsManager.BuffVFX", m.BuffVFX, vfx_buff_count);

        e += CheckList("AssetsManager.MeleeWeaponPrefabs", m.MeleeWeaponPrefabs, Config.weapon_id_melee_max - Config.weapon_id_melee_min + 1);
        e += CheckList("AssetsManager.SpearWeaponPrefabs", m.SpearWeaponPrefabs, Config.weapon_id_spear_max - Config.weapon_id_spear_min + 1);
        e += CheckList("AssetsManager.GunWeaponPrefabs", m.GunWeaponPrefabs, Config.weapon_id_gun_max - Config.weapon_id_gun_min + 1);
        e += CheckList("AssetsManager.MagicOrbPrefabs", m.MagicOrbPrefabs, Config.weapon_id_magic_max - Config.weapon_id_magic_min + 1);

        e += CheckList("AssetsManager.CharacterIcons", m.CharacterIcons, 0); // 数量未定稿，只查空项
        e += CheckList("AssetsManager.WeaponIcons", m.WeaponIcons, 0);
        if (m.CharacterIcons == null || m.CharacterIcons.Count == 0)
        {
            Debug.LogWarning("[启动自检] AssetsManager.CharacterIcons 为空：角色图标未导入");
        }
        if (m.WeaponIcons == null || m.WeaponIcons.Count == 0)
        {
            Debug.LogWarning("[启动自检] AssetsManager.WeaponIcons 为空：武器图标未导入");
        }
        return e;
    }
    #endregion

    #region//Local
    private const float startupCheckDelay = 1f;

    // 以下数量在 Config 里没有对应常量，来源见注释
    private const int beacon_type_count = 2;      // 守护点外形：外围矮信标 + 中心高信标（TryGetTemplate/TryGetGraphic 按此二分）
    private const int vfx_bullet_count = 60;      // 《特效清单与分配表》第二节：20 类 × 3 颜色
    private const int vfx_shield_count = 13;
    private const int vfx_range_magic_count = 10;
    private const int vfx_magic_circle_count = 10;
    private const int vfx_buff_count = 31;

    /// <summary>单个引用是否为空（管理器未注册 / 资产被删都走这里）。</summary>
    private static int CheckObject(string label, UnityEngine.Object obj)
    {
        if (obj == null)
        {
            Debug.LogError($"[启动自检] {label} 为空（缺失或未配置）");
            return 1;
        }
        return 0;
    }

    /// <summary>列表检查：空项 = 错误；数量少于期望 = 错误，多于期望 = 警告。expected = 0 表示数量未定稿，只查空项。</summary>
    private static int CheckList<T>(string label, List<T> list, int expected) where T : UnityEngine.Object
    {
        if (list == null)
        {
            Debug.LogError($"[启动自检] {label} 为 null（未初始化）");
            return 1;
        }

        int e = 0;
        if (expected > 0 && list.Count != expected)
        {
            if (list.Count < expected) { Debug.LogError($"[启动自检] {label} 数量不足：{list.Count} / 期望 {expected}"); e++; }
            else Debug.LogWarning($"[启动自检] {label} 数量偏多：{list.Count} / 期望 {expected}");
        }
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                Debug.LogError($"[启动自检] {label}[{i}] 为空（资产可能已被删除）");
                e++;
            }
        }
        return e;
    }
    #endregion
}

