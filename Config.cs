using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

public static class Config
{
    #region 匹配与人数
    public const float battle_duration = 600f;
    public const float ready_start_delay = 3f; // 全员准备后到开战的倒计时（秒）
    #endregion

    #region 角色
    public const int attack_character_count = 18;
    public const int defense_character_count = 6;
    public const int save_max_level = 10; // 玩家存档等级上限
    public const int battle_max_level = 20; // 战斗期等级上限：生成实体时钳制，最高刷出 20 级
    public const int player_max_level = 50;

    // 升到下一级所需经验；配表最大等级不足时停止升级并报错
    public static int GetPlayerLevelUpExp(int currentLevel)
    {
        int level = currentLevel + 1;
        if (!ConfigManager.ExpPlayerConfig.content.ContainsKey(level))
        {
            Debug.LogError($"[配表] exp_player.csv 缺少 {level} 级（player_max_level={player_max_level}）：玩家等级不再提升，请补齐配表");
            return int.MaxValue;
        }
        return ConfigManager.ExpPlayerConfig.Get(level).exp;
    }
    #endregion

    #region 守护雕像（防守目标）
    public const int sage_statue_count = 3;
    public const int king_statue_count = 1;
    #endregion

    #region 资源与中立单位
    public const int coral_size_count = 2;              // 0 = 小珊瑚，1 = 大珊瑚
    public const int crystal_statue_count = 4;
    public const float crystal_statue_attack_range = 20f;
    public const float coral_spawn_checks_per_second = 10f;
    public const float coral_spawn_min_dist = 40f;
    public const float coral_spawn_max_dist = 80f;
    public const float coral_large_chance = 0.3f;       // 刷新时出大珊瑚的概率
    public const float coral_small_drop_chance = 0.2f;  // 小珊瑚被摧毁时的掉武器概率
    public const float coral_large_drop_chance = 1f;    // 大珊瑚被摧毁时的掉武器概率

    #region 中立雕像（中立争抢单位）
    public const int neutral_statue_variant_count = 2;   // 0 天使雕像 / 1 恶魔雕像
    public const float neutral_statue_first_spawn_delay = 30f;
    public const float neutral_statue_attack_range = 8f;
    public const float neutral_statue_respawn_delay = 60f;
    public const float plague_bless_duration = 30f;
    public const float plague_bless_damage_up = 0.75f;
    public const float plague_bless_damage_reduce = 0.25f;
    #endregion
    #endregion

    #region 僵尸
    public const int zombie_max = 30;
    public const float zombie_refresh_rate_fast = 0.5f;
    public const float zombie_refresh_rate_slow = 0.05f;
    public const float zombie_refresh_progress_max = 1f;
    public const int zombie_variant_count = 21;
    public const int elite_zombie_variant_count = 14;
    public const int zombie_spawn_level = 1;
    // 刷新等级随战斗时间增长：每经过 7.5% 战斗时间 +1；死亡漫步者被动再额外 +4（普通/精英不区分）
    public const float zombie_level_step_ratio = 0.075f;
    public const int zombie_spawn_level_passive_boost = 4;

    // —— AI 行为（策划案第九章只写了「无目标时游荡 / 发现目标后主动追击 / 设最大追击距离」，数值均为占位初值）——
    public const float zombie_decide_interval = 0.15f;
    public const float zombie_acquire_range = 12f;
    public const float zombie_attack_range = 2f;
    public const float zombie_roar_range = 10f;
    public const float zombie_max_chase_distance = 16f;
    public const float zombie_wander_radius = 8f;
    public const float zombie_wander_pause = 2f;
    #endregion

    #region 昼夜
    // 昼夜时长已移至 EnvironmentManager 的 Inspector 字段（dayDuration / nightDuration），
    // 时间改为归一化周期值（[0,2)：0/2 = 午夜，1 = 正午）循环推演，此处不再保留"阶段时长"常量。
    public const float daynight_sync_interval = 10f;
    #endregion

    #region 视野与小地图（策划案第十五章）
    // 可见范围只有一套数值：角色属性 EntityAttribute.viewDistance
    // （策划案 15 章：可见距离决定敌方模型可见性与小地图显示）。小地图不另设阈值。
    public const float minimap_sync_interval = 0.5f;
    public const float minimap_lost_duration = 99999f;

    // 雷达显示半径档位来自配表 global_variables（minimap_view_radius），首档即默认档
    public static float minimap_view_radius
    {
        get
        {
            var radii = ConfigManager.GlobalVariableConfig.Get("minimap_view_radius").floats;
            if (radii == null || radii.Count == 0)
            {
                Debug.LogError("[配表] global_variables.csv 的 minimap_view_radius 至少要配一档半径");
                return 0f;
            }
            return radii[0];
        }
    }

    // 客户端小地图点位超时：超过此时间未收到该实体点位包则隐藏（覆盖「离屏残留」与「夜间/致盲停传」两类情况）
    public const float minimap_entity_timeout = 1.5f;
    // 雷达显示半径切换键
    public const KeyCode minimap_radius_key = KeyCode.F;
    // 地形边长（米）：世界 x/z 范围 0~size；小地图地形底图与世界坐标的换算基准
    public const float map_terrain_size = 1000f;
    #endregion

    #region 玩家操作（双手键盘无鼠标：W/S 前后 / A/D 左右 / 前后+左右同按渐转 / J 空手攻击 / K 跳跃 / 左 Shift 滑铲 / U I O L H 技能槽）
    public const float move_turn_rate = 120f;
    public static readonly KeyCode[] skill_slot_keys = { KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.L, KeyCode.H, KeyCode.Y };
    public static readonly PlayerKey[] skill_slot_player_keys = { PlayerKey.U, PlayerKey.I, PlayerKey.O, PlayerKey.L, PlayerKey.H, PlayerKey.Y };
    public const KeyCode melee_key = KeyCode.J;
    public const KeyCode jump_key = KeyCode.K;
    public const KeyCode slide_key = KeyCode.LeftShift;
    public const float fly_vertical_speed = 5f; // 飞行模式垂直速度（长按 K 升 / 长按 Shift 降）
    public const float gm_speed_multiplier = 5f;  // GM「极速」动画移速倍率（占位初值）
    public const float gm_teleport_height = 200f; // GM 传送的 Y（传送面板只给 x/z）
    #endregion

    #region 武器与技能
    public const float skill_exp_damage_bonus = 0.1f;

    #region 武器技能 id 区间（珊瑚掉武器全段随机；段长 = 武器数量，段长与顺序不可随意调整）
    public const int weapon_id_melee_min = (int)SkillType.SkillFanIceCone;         // 近战刀 11 把
    public const int weapon_id_melee_max = (int)SkillType.SkillSkyfallWeapon;
    public const int weapon_id_spear_min = (int)SkillType.SkillBoomerangWeapon;    // 长枪 8 把
    public const int weapon_id_spear_max = (int)SkillType.SkillIceFanTiny;
    public const int weapon_id_gun_min = (int)SkillType.SkillBackVolley;          // 枪械 15 把
    public const int weapon_id_gun_max = (int)SkillType.SkillShieldFour;
    public const int weapon_id_magic_min = (int)SkillType.SkillSplitShot;         // 魔法球 16 个
    public const int weapon_id_magic_max = (int)SkillType.SkillAllyShieldTwentyFive;

    public static int GetRandomWeaponId()
    {
        // 四段在 SkillType 里连续（刀 11 / 长枪 8 / 枪械 15 / 魔法球 16 = 50），整段随机即全武器均匀随机
        return Random.Range(weapon_id_melee_min, weapon_id_magic_max + 1);
    }
    #endregion
    #endregion

    #region 结算与得分
    public const float kill_score_factor = 0.1f;
    #endregion

    #region 同步
    public const float entity_sync_interval_fast = 0.02f;
    public const float entity_sync_interval_details = 0.2f;
    public const float entity_sync_pos_epsilon = 0.1f;  // 位姿脏判定：与上次下发位置差超过该值（米）才置脏
    public const float entity_sync_yaw_epsilon = 3f;    // 位姿脏判定：与上次下发朝向差超过该值（度）才置脏
    #endregion

    #region 通用
    public const int entity_id_max = 30000;
    public const float melee_hit_radius = 0.7f;
    public const float unarmed_skill_cd = 0.1f;
    public const int unarmed_punch_left = (int)SkillType.SkillPunchLeft;
    public const int unarmed_punch_right = (int)SkillType.SkillPunchRight;
    public const int unarmed_attack_smash = (int)SkillType.SkillPunchSmash;
    public const float anim_move_speed_up = 1.3f;
    public const float anim_move_speed_down = 0.6f;
    public const float anim_move_speed_mire = 0.5f;
    // 初始技能表：进攻方按 4 类武器的基础技能循环，防守方按角色（主动1 / 主动2 / 大招）固定，非玩家按单位类别
    public static readonly Dictionary<EntityType, int[]> initial_skills = new()
    {
        // ===== 进攻方角色（18 人，每角色 1 个天生攻击技能）=====
        // 策划案 4.1「不做定位区分」；暂时按 4 类武器的基础技能循环分配，待角色池定稿后调整
        { EntityType.Attack(0), new[] { (int)SkillType.SkillFanIceCone } },
        { EntityType.Attack(1), new[] { (int)SkillType.SkillBoomerangWeapon } },
        { EntityType.Attack(2), new[] { (int)SkillType.SkillBackVolley } },
        { EntityType.Attack(3), new[] { (int)SkillType.SkillSplitShot } },
        { EntityType.Attack(4), new[] { (int)SkillType.SkillFanIceCone } },
        { EntityType.Attack(5), new[] { (int)SkillType.SkillBoomerangWeapon } },
        { EntityType.Attack(6), new[] { (int)SkillType.SkillBackVolley } },
        { EntityType.Attack(7), new[] { (int)SkillType.SkillSplitShot } },
        { EntityType.Attack(8), new[] { (int)SkillType.SkillFanIceCone } },
        { EntityType.Attack(9), new[] { (int)SkillType.SkillBoomerangWeapon } },
        { EntityType.Attack(10), new[] { (int)SkillType.SkillBackVolley } },
        { EntityType.Attack(11), new[] { (int)SkillType.SkillSplitShot } },
        { EntityType.Attack(12), new[] { (int)SkillType.SkillFanIceCone } },
        { EntityType.Attack(13), new[] { (int)SkillType.SkillBoomerangWeapon } },
        { EntityType.Attack(14), new[] { (int)SkillType.SkillBackVolley } },
        { EntityType.Attack(15), new[] { (int)SkillType.SkillSplitShot } },
        { EntityType.Attack(16), new[] { (int)SkillType.SkillFanIceCone } },
        { EntityType.Attack(17), new[] { (int)SkillType.SkillBoomerangWeapon } },

        // ===== 防守方角色（6 人 × 主动1 / 主动2 / 大招）=====
        { EntityType.Defense(0), new[] { (int)SkillType.SkillRockShield, (int)SkillType.SkillPowerSuppress, (int)SkillType.SkillCrystalStatueBlazeCast } },      // 鹿铠怪人
        { EntityType.Defense(1), new[] { (int)SkillType.SkillEyeMark, (int)SkillType.SkillNightMark, (int)SkillType.SkillForceNight } },                 // 白眼伯爵
        { EntityType.Defense(2), new[] { (int)SkillType.SkillSummonZombies, (int)SkillType.SkillDeathStrollCast, (int)SkillType.SkillSummonElites } },    // 死灵漫步者
        { EntityType.Defense(3), new[] { (int)SkillType.SkillSilenceCast, (int)SkillType.SkillMireCast, (int)SkillType.SkillReflectCast } },             // 蒙面教皇
        { EntityType.Defense(4), new[] { (int)SkillType.SkillPlagueMarkCast, (int)SkillType.SkillPlaguePollute, (int)SkillType.SkillDetonatePlague } },    // 瘟疫使者
        { EntityType.Defense(5), new[] { (int)SkillType.SkillPaleLightCast, (int)SkillType.SkillPaleDarkCast, (int)SkillType.SkillFogCast } },           // 苍白舞者

        // ===== 非玩家单位（只登记 value=0，同类别其它 value 走回退）=====
        { EntityType.Zombie(0), new[] { (int)SkillType.SkillZombieClawR, (int)SkillType.SkillZombieClawL } },                       // 普通僵尸：爪击右 / 爪击左
        { EntityType.EliteZombie(0), new[] { (int)SkillType.SkillEliteZombieClawR, (int)SkillType.SkillEliteZombieClawL, (int)SkillType.SkillEliteZombieScream } }, // 精英僵尸
        { EntityType.CrystalStatue(0), new[] { (int)SkillType.SkillCrystalStatueSporeA, (int)SkillType.SkillCrystalStatueSporeB, (int)SkillType.SkillCrystalStatueSporeC } }, // 水晶雕像：三种孢子
        { EntityType.NeutralStatue(0), new[] { (int)SkillType.SkillNeutralStatueSpore } },     // 中立雕像：孢子喷发
    };

    public static List<int> GetInitialSkills(EntityType character)
    {
        if (initial_skills.TryGetValue(character, out var ids) && ids != null) return new List<int>(ids);
        foreach (var pair in initial_skills)
        {
            if (pair.Key.category == character.category && pair.Value != null) return new List<int>(pair.Value);
        }
        return new List<int>();
    }

    public const float default_skill_auto_target_radius = 20f;

    // 技能自动索敌前方扇形半角（度）；敌人在朝向左右各该角度内才索敌
    public const float default_skill_auto_target_sector_half_angle = 30f;

    public const float default_forward_aim_distance = 10f;

    public const float buff_vfx_life_time = 3600f;

    #region 通用 Buff 数值（占位初值，待策划定稿后在此统一调整）
    public const float buff_duration_control = 3f;
    public const float buff_duration_debuff = 5f;
    public const float buff_duration_buff = 8f;
    public const float buff_attr_ratio = 0.2f;      // 属性 Buff 每级幅度 = 对应属性初始值 × 此系数
    public const float shield_health_ratio = 0.1f;  // 护盾值 = 被施加者生命上限 × 此系数 × 等级
    public const float burning_damage_per_tick = 30f; // 燃烧每跳 = 此值 × 等级
    public const float poison_damage_ratio = 0.5f;  // 中毒每跳 = max(施加者力/魔) × 此系数 × 等级
    public const float reflect_rate = 0.1f;         // 反伤 = 实际受伤 × 此系数 × 等级（向下取整，min=1）
    #endregion

    #region 防守方技能参数（占位初值，待策划定稿）
    public const float death_stroll_radius = 4f;
    public const float death_stroll_dot_ratio = 1.5f; // 死灵漫步光环每跳 = max(施加者力/魔) × 此系数 × 等级
    public const float death_stroll_speed_up = 1.3f;
    public const int summon_zombie_count = 5;
    public const int summon_elite_count = 3;
    public const float plague_detonate_damage = 20f;
    public const float defense_ally_cast_radius = 15f;
    public const float defense_nearby_radius = 8f;
    public const int power_suppress_hits = 3;
    public const float power_suppress_interval = 0.4f;
    public const float power_suppress_radius = 3f;
    public const float power_suppress_rate = 1f;
    public const float power_suppress_slow_duration = 10f;
    public const int power_suppress_vfx_index = 5;   // RangeMagic[5]
    public const float power_suppress_vfx_life = 1f;
    public const float night_mark_duration = 8f;
    public const float plague_mark_duration = 30f;   // 瘟疫标记持续时长（主动1 与 瘟疫污染扩散共用）
    public const float plague_pollute_radius = 8f;   // 珊瑚被摧毁时的瘟疫标记扩散半径
    #endregion

    #region 技能参数补充（占位初值，待策划定稿）
    public const float chain_jump_radius = 6f;
    public const float chain_jump_duration = 0.6f;
    public const float airstrike_radius = 3f;
    #endregion

    public static readonly Dictionary<EffectType, (SkillVfxKind kind, int index)> buff_vfx = new()
    {
        { EffectType.StatueReduce, (SkillVfxKind.Shield, 0) },
        { EffectType.StatueShield, (SkillVfxKind.MagicCircle, 8) },
        { EffectType.PopeGuard, (SkillVfxKind.Shield, 12) },
        { EffectType.Shield, (SkillVfxKind.Shield, 2) },
        { EffectType.Stun, (SkillVfxKind.Buff, 20) },
        { EffectType.StatueBlaze, (SkillVfxKind.MagicCircle, 3) },
        { EffectType.Burning, (SkillVfxKind.Buff, 11) },
        { EffectType.EyeMark, (SkillVfxKind.Buff, 23) },
        { EffectType.PaleLight, (SkillVfxKind.Buff, 15) },
        { EffectType.PaleDark, (SkillVfxKind.Buff, 4) },
        { EffectType.PaleFire, (SkillVfxKind.Buff, 3) },
        { EffectType.PaleIce, (SkillVfxKind.Buff, 14) },
        { EffectType.PaleThunder, (SkillVfxKind.Buff, 20) },
        { EffectType.DeathStroll, (SkillVfxKind.Buff, 29) },
        { EffectType.DeathCurse, (SkillVfxKind.Buff, 0) },
        { EffectType.Silence, (SkillVfxKind.Buff, 2) },
        { EffectType.Mire, (SkillVfxKind.Buff, 17) },
        { EffectType.Reflect, (SkillVfxKind.MagicCircle, 6) },
        { EffectType.Root, (SkillVfxKind.Buff, 19) },
        { EffectType.PlagueMark, (SkillVfxKind.Buff, 30) },
        { EffectType.PlaguePollute, (SkillVfxKind.Buff, 22) },
        { EffectType.NightMark, (SkillVfxKind.Buff, 30) },
        { EffectType.Poison, (SkillVfxKind.Buff, 13) },
        { EffectType.Freeze, (SkillVfxKind.Buff, 12) },
        { EffectType.SlowDown, (SkillVfxKind.Buff, 17) },
        { EffectType.SpeedUp, (SkillVfxKind.Buff, 27) },
        { EffectType.PlagueBless, (SkillVfxKind.MagicCircle, 9) },
        { EffectType.ZombieScreamBoost, (SkillVfxKind.MagicCircle, 9) },

        { EffectType.AttrStrengthBoost, (SkillVfxKind.MagicCircle, 0) },
        { EffectType.AttrMagicBoost, (SkillVfxKind.MagicCircle, 0) },
        { EffectType.AttrCritRateBoost, (SkillVfxKind.MagicCircle, 1) },
        { EffectType.AttrCritDamageBoost, (SkillVfxKind.MagicCircle, 1) },
        { EffectType.AttrStrengthDecrease, (SkillVfxKind.MagicCircle, 5) },
        { EffectType.AttrMagicDecrease, (SkillVfxKind.MagicCircle, 5) },
        { EffectType.AttrCritRateDecrease, (SkillVfxKind.MagicCircle, 5) },
        { EffectType.AttrCritDamageDecrease, (SkillVfxKind.MagicCircle, 5) },
    };

    #endregion

    #region 刚体（可移动单位的权威速度载体：位移效果只产出速度，位置由物理积分）
    public const float rb_drag = 0f;

    public const float move_ground_friction = 2f;

    public const float rb_angular_drag = 1f;
    #endregion

    #region 防守方角色下标（type.value，顺序同策划案第五章与 initial_skills）
    public const int defense_index_deer_knight = 0;    // 鹿铠怪人
    public const int defense_index_count_eye = 1;      // 白眼伯爵
    public const int defense_index_death_stroller = 2; // 死灵漫步者
    public const int defense_index_masked_pope = 3;    // 蒙面教皇
    public const int defense_index_plague_bringer = 4; // 瘟疫使者
    public const int defense_index_pale_dancer = 5;    // 苍白舞者
    #endregion

    #region 防守方被动数值（占位初值，待策划定稿；被动不占技能 id，见策划案 21.5）
    public const float crit_paralysis_duration = 1.5f;
    public const float night_extend_factor = 1.5f;
    public const float pope_guard_reduce_rate = 0.3f;
    public const float attack_revive_slow_factor = 0.6f;
    public const int pale_full_stacks = 10;
    public const int pale_mixed_stacks = 8;
    #endregion

    #region 灵火（StatueBlaze）：水晶雕像攻击附加爆炸
    public const float statue_blaze_radius = 2.5f;
    public const float statue_blaze_rate = 0.5f;
    #endregion
}
