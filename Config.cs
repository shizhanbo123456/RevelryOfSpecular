using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

public static class Config
{
    #region 匹配与人数
    public const float battle_duration = 600f;
    #endregion

    #region 角色
    public const int attack_character_count = 18;
    public const int defense_character_count = 6;
    public const int max_entity_level = 10;
    public const int player_max_level = 50;
    public static readonly int[] level_up_exp = { 50000, 60000, 90000, 120000, 160000, 200000, 250000, 300000, 360000 };

    public static int GetPlayerLevelUpExp(int currentLevel) => 700 + 300 * Mathf.Max(1, currentLevel);
    #endregion

    #region 守护点（瘟疫信标）
    public const int outer_beacon_count = 3;
    public const int core_beacon_count = 1;
    #endregion

    #region 资源与中立单位
    public const int crystal_type_count = 4;
    public const int crystal_variant_count = 3;
    public const int crystal_graphics_count = crystal_type_count * crystal_variant_count;
    public const int tower_count = 4;
    public const float tower_attack_range = 20f;
    public const float crystal_spawn_checks_per_second = 10f;
    public const float crystal_spawn_min_dist = 40f;
    public const float crystal_spawn_max_dist = 80f;
    public const float crystal_skill_drop_chance = 0.15f;

    #region 瘟疫树（中立争抢单位）
    public const float plague_tree_first_spawn_delay = 30f;
    public const float plague_tree_attack_range = 8f;
    public const float plague_tree_respawn_delay = 60f;
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
    public const int zombie_spawn_level_boosted = 3;

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
    public const float minimap_view_radius = 100f;
    // 客户端小地图点位超时：超过此时间未收到该实体点位包则隐藏（覆盖「离屏残留」与「夜间/致盲停传」两类情况）
    public const float minimap_entity_timeout = 1.5f;
    #endregion

    #region 复活与愈战愈勇
    public const float revive_day_progress_per_second = 1f / 8f;
    public const float revive_night_progress_per_second = 1f / 60f;
    public static readonly float[] revive_progress_multiplier_by_death = { 1f, 0.8f, 0.6f, 0.4f, 0.3f, 0.2f, 0.2f };
    public const float defense_revive_duration = 25f;
    public static readonly int[] yz_stack_by_life = { 0, 0, 1, 1, 2, 2, 3, 4, 5, 5, 5 };
    #endregion

    #region 玩家操作（双手键盘无鼠标：W/S 前后 / A/D 左右 / 前后+左右同按渐转 / J 空手攻击 / K 跳跃 / 左 Shift 滑铲 / U I O L H 技能槽）
    public const float move_turn_rate = 120f;
    public static readonly KeyCode[] skill_slot_keys = { KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.L, KeyCode.H, KeyCode.Y };
    public static readonly PlayerKey[] skill_slot_player_keys = { PlayerKey.U, PlayerKey.I, PlayerKey.O, PlayerKey.L, PlayerKey.H, PlayerKey.Y };
    public const KeyCode melee_key = KeyCode.J;
    public const KeyCode jump_key = KeyCode.K;
    public const KeyCode slide_key = KeyCode.LeftShift;
    #endregion

    #region 武器与技能
    public const float skill_exp_damage_bonus = 0.1f;

    #region 武器技能 id 区间（见策划案 21 章；水晶掉武器按水晶类型从对应区间随机）
    public const int weapon_id_melee_min = 0;   // 近战刀 0~10（11 把）
    public const int weapon_id_melee_max = 10;
    public const int weapon_id_spear_min = 11;  // 长枪 11~18（8 把）
    public const int weapon_id_spear_max = 18;
    public const int weapon_id_gun_min = 19;    // 枪械 19~33（15 把）
    public const int weapon_id_gun_max = 33;
    public const int weapon_id_magic_min = 34;  // 魔法球 34~49（16 个）
    public const int weapon_id_magic_max = 49;

    public static int GetRandomWeaponId(int crystalValue)
    {
        int k = crystalValue % crystal_type_count;
        if (k < 0) k += crystal_type_count;
        return k switch
        {
            0 => Random.Range(weapon_id_melee_min, weapon_id_melee_max + 1),
            1 => Random.Range(weapon_id_spear_min, weapon_id_spear_max + 1),
            2 => Random.Range(weapon_id_gun_min, weapon_id_gun_max + 1),
            _ => Random.Range(weapon_id_magic_min, weapon_id_magic_max + 1),
        };
    }
    #endregion
    #endregion

    #region 结算与得分
    public const float kill_score_factor = 0.1f;
    #endregion

    #region 同步
    public const float entity_sync_interval_fast = 0.02f;
    public const float entity_sync_interval_details = 0.2f;
    #endregion

    #region 通用
    public const int entity_id_max = 30000;
    public const float melee_hit_radius = 0.7f;
    public const float unarmed_skill_cd = 0.1f;
    public const int unarmed_punch_left = 180;
    public const int unarmed_punch_right = 181;
    public const int unarmed_attack_smash = 182;
    public const float anim_move_speed_up = 1.3f;
    public const float anim_move_speed_down = 0.6f;
    public const float anim_move_speed_mire = 0.5f;
    public static readonly Dictionary<EntityType, int[]> initial_skills = new()
    {
        // ===== 进攻方角色（18 人，每角色 1 个天生攻击技能）=====
        // 策划案 4.1「不做定位区分」；暂时按 4 类武器的基础技能循环分配，待角色池定稿后调整
        { EntityType.Attack(0), new[] { 0 } },    // 疾风斩（刀）
        { EntityType.Attack(1), new[] { 11 } },   // 投枪（长枪）
        { EntityType.Attack(2), new[] { 19 } },   // 快速射击（枪械）
        { EntityType.Attack(3), new[] { 34 } },   // 魔弹（魔法球）
        { EntityType.Attack(4), new[] { 0 } },
        { EntityType.Attack(5), new[] { 11 } },
        { EntityType.Attack(6), new[] { 19 } },
        { EntityType.Attack(7), new[] { 34 } },
        { EntityType.Attack(8), new[] { 0 } },
        { EntityType.Attack(9), new[] { 11 } },
        { EntityType.Attack(10), new[] { 19 } },
        { EntityType.Attack(11), new[] { 34 } },
        { EntityType.Attack(12), new[] { 0 } },
        { EntityType.Attack(13), new[] { 11 } },
        { EntityType.Attack(14), new[] { 19 } },
        { EntityType.Attack(15), new[] { 34 } },
        { EntityType.Attack(16), new[] { 0 } },
        { EntityType.Attack(17), new[] { 11 } },

        // ===== 防守方角色（6 人 × 主动1 / 主动2 / 大招，id 段见策划案 21.5）=====
        { EntityType.Defense(0), new[] { 50, 51, 52 } },  // PC104 鹿铠怪人：岩石护盾 / 蘑菇感染 / 灵火
        { EntityType.Defense(1), new[] { 54, 55, 56 } },  // NP114 白眼伯爵：白眼标记 / 无限视野 / 立即入夜
        { EntityType.Defense(2), new[] { 58, 59, 60 } },  // PC106 死灵漫步者：召唤僵尸 / 死灵漫步 / 召唤精英
        { EntityType.Defense(3), new[] { 62, 63, 64 } },  // NP134 蒙面教皇：沉默 / 泥沼 / 反伤
        { EntityType.Defense(4), new[] { 66, 67, 68 } },  // PC102 瘟疫使者：瘟疫标记 / 吸收矿石 / 引爆瘟疫
        { EntityType.Defense(5), new[] { 70, 71, 72 } },  // PC103 苍白舞者：苍白之光 / 苍白之暗 / 迷雾

        // ===== 非玩家单位（只登记 value=0，同类别其它 value 走回退；store 均为 -1 无限制）=====
        { EntityType.Zombie(0), new[] { 101, 102, 103 } },      // 普通僵尸：爪击左 / 爪击右 / 嘶吼
        { EntityType.EliteZombie(0), new[] { 120, 121, 122 } }, // 精英僵尸
        { EntityType.Tower(0), new[] { 140 } },                 // 防御塔：孢子喷射
        { EntityType.PlagueTree(0), new[] { 160 } },            // 瘟疫树：孢子喷发
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
    public const float buff_dot_damage = 8f;
    public const float buff_shield_value = 120f;
    public const float buff_attr_value = 15f;
    #endregion

    #region 防守方技能参数（占位初值，待策划定稿）
    public const float mushroom_infect_duration = 60f;
    public const float infinite_view_distance = 99999f;
    public const float death_stroll_radius = 4f;
    public const float death_stroll_speed_up = 1.3f;
    public const int summon_zombie_count = 5;
    public const int summon_zombie_level = 1;
    public const int summon_elite_count = 3;
    public const int summon_elite_level = 3;
    public const float plague_detonate_damage = 20f;
    public const float defense_ally_cast_radius = 15f;
    public const float defense_nearby_radius = 8f;
    #endregion

    #region 技能参数补充（占位初值，待策划定稿）
    public const float chain_jump_radius = 6f;
    public const float chain_jump_duration = 0.6f;
    public const float airstrike_radius = 3f;
    public const float absorb_crystal_radius = 6f;
    public const float absorb_crystal_damage = 999999f;
    #endregion

    public static readonly Dictionary<EffectType, (SkillVfxKind kind, int index)> buff_vfx = new()
    {
        { EffectType.BeaconReduce, (SkillVfxKind.Shield, 0) },      // S1 红（守护点减伤叠层）
        { EffectType.TowerShield, (SkillVfxKind.Shield, 1) },       // S2 橙构筑（塔身）
        { EffectType.PopeGuard, (SkillVfxKind.Shield, 2) },         // S3 白厚实（守护点）
        { EffectType.Shield, (SkillVfxKind.Shield, 5) },            // S6 黄（光盾）
        { EffectType.Stun, (SkillVfxKind.Buff, 20) },               // BF21 麻痹黄
        { EffectType.TowerBlaze, (SkillVfxKind.Buff, 11) },         // BF12 火焰（塔身）
        { EffectType.Burning, (SkillVfxKind.Buff, 11) },            // BF12 火焰（燃烧 DoT）
        { EffectType.EyeMark, (SkillVfxKind.Buff, 15) },            // BF16 黄色周身泛光
        { EffectType.PaleLight, (SkillVfxKind.Buff, 15) },          // BF16（光标记复用）
        { EffectType.PaleDark, (SkillVfxKind.Buff, 0) },            // BF1 紫雾（暗标记）
        { EffectType.DeathStroll, (SkillVfxKind.Buff, 3) },         // BF4 血色缠绕
        { EffectType.Silence, (SkillVfxKind.Buff, 4) },             // BF5 黑色缠绕
        { EffectType.Mire, (SkillVfxKind.Buff, 25) },               // BF26 水花
        { EffectType.Reflect, (SkillVfxKind.MagicCircle, 8) },      // MC9 红色防御增益
        { EffectType.Root, (SkillVfxKind.MagicCircle, 2) },         // MC3 深红禁锢圆环
        { EffectType.PlagueMark, (SkillVfxKind.Buff, 18) },         // BF19 自然（瘟疫标记）
        { EffectType.Poison, (SkillVfxKind.Buff, 18) },             // BF19 自然（中毒复用）
        { EffectType.Freeze, (SkillVfxKind.Buff, 12) },             // BF13 冻结
        { EffectType.AnimSlowDown, (SkillVfxKind.Buff, 26) },       // BF27 雪（减速）
        { EffectType.AnimSpeedUp, (SkillVfxKind.Buff, 27) },        // BF28 环绕风（加速）
        { EffectType.PlagueBless, (SkillVfxKind.Buff, 28) },        // BF29 绿色祝福（攻占瘟疫树）
    };

    public static readonly Vector3[] weapon_float_offsets =
    {
        new Vector3( 0.55f, 1.15f,  0.35f),  // 槽 1 右前
        new Vector3(-0.55f, 1.15f,  0.35f),  // 槽 2 左前
        new Vector3( 0.70f, 1.45f,  0.00f),  // 槽 3 右中
        new Vector3(-0.70f, 1.45f,  0.00f),  // 槽 4 左中
        new Vector3( 0.70f, 1.15f, -0.40f),  // 槽 5 右后
        new Vector3(-0.70f, 1.15f, -0.40f),  // 槽 6 左后
        new Vector3( 0.45f, 0.75f,  0.20f),  // 槽 7 右下
        new Vector3(-0.45f, 0.75f,  0.20f),  // 槽 8 左下
    };

    public static Vector3 GetWeaponFloatOffset(int slotIndex)
    {
        if (weapon_float_offsets.Length == 0) return Vector3.zero;
        return weapon_float_offsets[Mathf.Clamp(slotIndex, 0, weapon_float_offsets.Length - 1)];
    }
    #endregion

    #region 刚体（可移动单位的权威速度载体：位移效果只产出速度，位置由物理积分）
    public const float rb_drag = 0f;

    public const float move_ground_friction = 2f;

    public const float rb_angular_drag = 1f;
    #endregion

    #region 防守方角色下标（type.value，顺序同策划案第五章与 initial_skills）
    public const int defense_index_deer_knight = 0;    // PC104 鹿铠怪人
    public const int defense_index_count_eye = 1;      // NP114 白眼伯爵
    public const int defense_index_death_stroller = 2; // PC106 死灵漫步者
    public const int defense_index_masked_pope = 3;    // NP134 蒙面教皇
    public const int defense_index_plague_bringer = 4; // PC102 瘟疫使者
    public const int defense_index_pale_dancer = 5;    // PC103 苍白舞者
    #endregion

    #region 防守方被动数值（占位初值，待策划定稿；被动不占技能 id，见策划案 21.5）
    public const float crit_paralysis_duration = 1.5f;
    public const float night_extend_factor = 1.5f;
    public const float pope_guard_reduce_rate = 0.3f;
    public const float attack_revive_slow_factor = 0.6f;
    public const int pale_full_stacks = 10;
    public const int pale_mixed_stacks = 8;
    #endregion

    #region 灵火（TowerBlaze）：塔攻击附加爆炸
    public const float tower_blaze_radius = 2.5f;
    public const float tower_blaze_rate = 0.5f;
    #endregion
}
