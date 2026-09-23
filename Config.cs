using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 全局静态配置常量（V0.9 非对称攻防）。
/// 数值来源：策划案第二十章（战斗全局参数一览）与第二十一章（技能池）；后续可在玩法中调整。
/// </summary>
public static class Config
{
    #region 匹配与人数
    /// <summary>单局时长（秒）= 10 分钟。</summary>
    public const float battle_duration = 600f;
    #endregion

    #region 角色
    /// <summary>进攻方角色池数量（18 人，具体角色池待定）。</summary>
    public const int attack_character_count = 18;
    /// <summary>防守方角色池数量（6 人，基于实际模型资源）。</summary>
    public const int defense_character_count = 6;
    /// <summary>角色等级上限（1~10）。</summary>
    public const int max_entity_level = 10;
    /// <summary>玩家等级达到该值解锁全部角色。</summary>
    public const int player_max_level = 50;
    /// <summary>
    /// 角色升级所需经验（下标 0 = 1→2 级，依次到 9→10 级）。
    /// 获得经验 = 对水晶造成的伤害量。
    /// </summary>
    public static readonly int[] level_up_exp = { 50000, 60000, 90000, 120000, 160000, 200000, 250000, 300000, 360000 };

    /// <summary>
    /// 玩家（账号级）从当前等级升到下一级所需经验：700 + 300 × 当前等级。
    /// 公式集中于此，SaveManager 不持有数值公式。
    /// </summary>
    public static int GetPlayerLevelUpExp(int currentLevel) => 700 + 300 * Mathf.Max(1, currentLevel);
    #endregion

    #region 守护点（瘟疫信标）
    /// <summary>外围守护点数量。</summary>
    public const int outer_beacon_count = 3;
    /// <summary>中心守护点数量。</summary>
    public const int core_beacon_count = 1;
    #endregion

    #region 资源与中立单位
    /// <summary>水晶类型数（策划案第七章：4 种类型对应 4 类武器刀/长枪/枪械/魔法球）。</summary>
    public const int crystal_type_count = 4;
    /// <summary>每类水晶的外观变体数。</summary>
    public const int crystal_variant_count = 3;
    /// <summary>
    /// 水晶外观总数（= 类型数 × 变体数 = 12）：外观列表下标 0~11，
    /// **类别 = 下标 % crystal_type_count**，即 k、k+4、k+8（k=0~3）属同一类。
    /// </summary>
    public const int crystal_graphics_count = crystal_type_count * crystal_variant_count;
    /// <summary>防御塔（瘟疫孢子）数量。</summary>
    public const int tower_count = 4;
    /// <summary>水晶刷新冷却下限/上限（秒，30~60s 随机）。</summary>
    public const float crystal_respawn_min = 30f;
    public const float crystal_respawn_max = 60f;
    /// <summary>摧毁水晶后获得技能的概率（15%，可调）。</summary>
    public const float crystal_skill_drop_chance = 0.15f;

    #region 瘟疫树（中立争抢单位）
    /// <summary>第 1 棵树的刷新延迟（秒）：开战后开始计时。</summary>
    public const float plague_tree_first_spawn_delay = 30f;
    /// <summary>树被打死后的重生倒计时（秒）：倒计时结束在候选点随机刷新一棵。</summary>
    public const float plague_tree_respawn_delay = 60f;
    /// <summary>攻占奖励「瘟疫祝福」：持续时长（秒）。</summary>
    public const float plague_bless_duration = 30f;
    /// <summary>攻占奖励「瘟疫祝福」：出伤乘区增幅（+75%）。</summary>
    public const float plague_bless_damage_up = 0.75f;
    /// <summary>攻占奖励「瘟疫祝福」：受伤乘区减免（−25%）。</summary>
    public const float plague_bless_damage_reduce = 0.25f;
    #endregion
    #endregion

    #region 僵尸
    /// <summary>全场僵尸数量上限（普通 + 精英，达上限停止刷新）。</summary>
    public const int zombie_max = 30;
    /// <summary>夜间刷新 cd 进度增速——僵尸数为 0 时（每秒进度，越少越快）。</summary>
    public const float zombie_refresh_rate_fast = 0.5f;
    /// <summary>夜间刷新 cd 进度增速——僵尸数接近上限时（每秒进度）。</summary>
    public const float zombie_refresh_rate_slow = 0.05f;
    /// <summary>夜间刷新 cd 进度上限（达到即刷新一只并清零；僵尸数量达上限时不刷新且进度清零）。</summary>
    public const float zombie_refresh_progress_max = 1f;
    #endregion

    #region 昼夜
    // 昼夜时长已移至 EnvironmentManager 的 Inspector 字段（dayDuration / nightDuration），
    // 时间改为归一化周期值（[0,2)：0/2 = 午夜，1 = 正午）循环推演，此处不再保留"阶段时长"常量。
    /// <summary>昼夜快照心跳间隔（秒）：服务器按此间隔补发完整快照，兜底两端的长期漂移。</summary>
    public const float daynight_sync_interval = 10f;
    #endregion

    #region 视野与小地图（策划案第十五章）
    // 可见范围只有一套数值：角色属性 EntityAttribute.viewDistance
    // （策划案 15 章：可见距离决定敌方模型可见性与小地图显示）。小地图不另设阈值。
    /// <summary>小地图下发间隔（秒）。</summary>
    public const float minimap_sync_interval = 0.2f;
    /// <summary>「小地图失联」的时长（秒）：取极大值，跨昼夜由进入白天的事件移除，不依赖到时。</summary>
    public const float minimap_lost_duration = 99999f;
    /// <summary>
    /// HUD 小地图的显示半径（米，客户端专用）：圆形小地图只画以自己为中心该半径内的单位。
    /// 纯表现裁剪，不参与服务器可见性判定（实际能收到什么仍由 viewDistance 决定）。
    /// </summary>
    public const float minimap_view_radius = 40f;
    #endregion

    #region 复活与愈战愈勇
    /// <summary>进攻方白天复活进度速率（每秒积累 1/8，8s 攒满；速率制防昼夜状态切换问题）。</summary>
    public const float revive_day_progress_per_second = 1f / 8f;
    /// <summary>进攻方夜晚复活进度速率（每秒积累 1/60，接近不可复活）。</summary>
    public const float revive_night_progress_per_second = 1f / 60f;
    /// <summary>死亡次数 → 进度积累倍率（第 1 次 = 1，最低 0.2；下标 = 已死亡次数，超出取末位）。</summary>
    public static readonly float[] revive_progress_multiplier_by_death = { 1f, 0.8f, 0.6f, 0.4f, 0.3f, 0.2f, 0.2f };
    /// <summary>防守方复活时长（秒，昼夜一样）。</summary>
    public const float defense_revive_duration = 25f;
    /// <summary>愈战愈勇每条命层数序列（第 1 条命起；超出取末位 5，见策划案 11.3）。</summary>
    public static readonly int[] yz_stack_by_life = { 0, 0, 1, 1, 2, 2, 3, 4, 5, 5, 5 };
    #endregion

    #region 玩家操作（双手键盘无鼠标：W/S 前后 / A/D 左右 / 前后+左右同按渐转 / J 空手攻击 / K 跳跃（移动中优先翻滚）/ 左 Shift 滑铲 / U I O L H 技能槽）
    /// <summary>移动渐转速率（度/秒）：前后 + 左右同按时角色按此速率逐渐转向（服务器权威推进）。</summary>
    public const float move_turn_rate = 120f;
    /// <summary>技能槽触发键（按槽位顺序：U I O L H Y）。</summary>
    public static readonly KeyCode[] skill_slot_keys = { KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.L, KeyCode.H, KeyCode.Y };
    /// <summary>技能槽对应的传输键位（顺序必须与 skill_slot_keys 一致）。</summary>
    public static readonly PlayerKey[] skill_slot_player_keys = { PlayerKey.U, PlayerKey.I, PlayerKey.O, PlayerKey.L, PlayerKey.H, PlayerKey.Y };
    /// <summary>空手攻击键（静止 = 跃起砸地，移动 = 出拳）。</summary>
    public const KeyCode melee_key = KeyCode.J;
    /// <summary>跳跃键。移动中按下且翻滚不在冷却 → 翻滚，否则（静止 / 冷却中）普通跳跃。</summary>
    public const KeyCode jump_key = KeyCode.K;
    /// <summary>滑铲键（左 Shift）。</summary>
    public const KeyCode slide_key = KeyCode.LeftShift;
    /// <summary>滑铲持续时间。</summary>
    public const float slide_duration = 0.6f;
    /// <summary>翻滚冷却（秒）：移动中按跳跃键优先翻滚，冷却中或未移动则退化为普通跳跃。</summary>
    public const float roll_cd = 5f;
    #endregion

    #region 武器与技能
    /// <summary>技能经验伤害加成：每点经验 +10%（策划案 14 章：基础 × (1 + 10% × 经验)，未设上限）。</summary>
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

    /// <summary>
    /// 按水晶外观下标随机取一把对应类别的武器技能 id。
    /// 类别 = 下标 % crystal_type_count（k、k+4、k+8 属同一类）：0刀 1长枪 2枪械 3魔法球。
    /// </summary>
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
    /// <summary>防守方得分中每次击杀的加成系数：分数 = 守护点剩余血量 × (1 + 0.1 × 击杀数)。</summary>
    public const float kill_score_factor = 0.1f;
    #endregion

    #region 同步
    /// <summary>高频实体同步间隔（秒）：只同步位置/旋转/动画。</summary>
    public const float entity_sync_interval_fast = 0.02f;
    /// <summary>完整同步间隔（秒）：额外同步血量/Buff/技能槽等运行时数据。</summary>
    public const float entity_sync_interval_details = 0.2f;
    #endregion

    #region 通用
    /// <summary>实体 id 上限（每次开始战斗时 id 源置零，超过上限从 1 重新分配，跳过已占用）。</summary>
    public const int entity_id_max = 30000;
    /// <summary>服务器权威移动速度（米/秒，暂定；客户端移动由动画状态机根运动表现，见策划案 10 章）。</summary>
    public const float base_move_speed = 5f;
    /// <summary>空手攻击判定球半径（米；球心为拳击手部骨骼 / 砸击用角色位置）。</summary>
    public const float melee_hit_radius = 0.7f;
    /// <summary>空手攻击技能 CD（秒；取最小间隔，避免同帧连发与除零）。</summary>
    public const float unarmed_skill_cd = 0.1f;
    /// <summary>空手攻击技能 id：左手拳击（移动中随机触发）。</summary>
    public const int unarmed_punch_left = 180;
    /// <summary>空手攻击技能 id：右手拳击（移动中随机触发）。</summary>
    public const int unarmed_punch_right = 181;
    /// <summary>空手攻击技能 id：原地砸击（静止时触发）。</summary>
    public const int unarmed_attack_smash = 182;
    /// <summary>跳跃初速度（米/秒，向上；占位初值待定）。起跳只给这一次竖直速度，之后由重力接管；
    /// 空中/落地不由计时决定，由 <see cref="EntityData"/> 的落地物理检测写入 InAir；
    /// 若动画状态通过 EntityAnim.SetVelocityVertical 声明了竖直速度，则以声明值为准。</summary>
    public const float jump_speed = 5f;
    /// <summary>加速倍率（策划案 11.3）：作用于**动画**——动画播放速度与动画声明的速度（EntityAnim.PlaybackSpeed：既写 animator.speed，
    /// 也在 SetVelocityForward/Horizontal 里缩放声明值）。其它速度来源（输入退化移速、MotionBase 位移、重力）完全不吃这个倍率。</summary>
    public const float anim_move_speed_up = 1.3f;
    /// <summary>减速倍率（作用范围同加速）。</summary>
    public const float anim_move_speed_down = 0.6f;
    /// <summary>泥沼倍率（教皇主动2 全场敌方减速；作用范围同加速）。</summary>
    public const float anim_move_speed_mire = 0.5f;
    /// <summary>普通僵尸外观变体数（丰富特征 21 种，生成时随机赋 type.value）。</summary>
    public const int zombie_variant_count = 21;
    /// <summary>
    /// 角色初始技能表：实体类型 → 技能 id 列表（顺序 = 键盘槽位 U I O L H Y）。
    /// 进攻方 = 1 个天生攻击技能；防守方 = 主动1/主动2/大招（被动不是技能，见策划案 21.5）。
    /// 非玩家单位登记 value=0 即可，其它 value 走同类别回退（见 GetInitialSkills）。
    /// <b>未登记的角色 = 空表（不持有任何技能）</b>，直接在下表补全即可，代码无需改动。
    /// 技能 id 段（见策划案第二十一章）：武器 0~49 / 防守方 50~73（53/57/61/65/69/73 为被动空位）、
    /// 非玩家与空手 100~199（空手 180~182 / 普通僵尸 101~119 / 精英僵尸 120~139 / 防御塔 140~159 / 瘟疫树 160~179）。
    /// </summary>
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

    /// <summary>取角色初始技能表（精确匹配失败时按同一 EntityCategory 回退；未配置返回空表；返回副本）。</summary>
    public static List<int> GetInitialSkills(EntityType character)
    {
        if (initial_skills.TryGetValue(character, out var ids) && ids != null) return new List<int>(ids);
        foreach (var pair in initial_skills)
        {
            if (pair.Key.category == character.category && pair.Value != null) return new List<int>(pair.Value);
        }
        return new List<int>();
    }

    /// <summary>技能自动索敌半径。</summary>
    public const float default_skill_auto_target_radius = 20f;

    /// <summary>索敌无目标时，瞄准点取正前方该距离（米）。</summary>
    public const float default_forward_aim_distance = 10f;

    /// <summary>持续型 Buff 特效的挂载时长（秒/局内远大于单局时长，实际由 Buff 移除时销毁）。</summary>
    public const float buff_vfx_life_time = 3600f;

    #region 通用 Buff 数值（占位初值，待策划定稿后在此统一调整）
    /// <summary>控制类时长（秒）：麻痹 / 冰冻 / 定身 / 沉默。</summary>
    public const float buff_duration_control = 3f;
    /// <summary>减益类时长（秒）：减速 / 中毒 / 燃烧。</summary>
    public const float buff_duration_debuff = 5f;
    /// <summary>增益类时长（秒）：护盾 / 加速 / 增伤。</summary>
    public const float buff_duration_buff = 8f;
    /// <summary>DoT 每跳伤害（固定数值，1s 一跳）。</summary>
    public const float buff_dot_damage = 8f;
    /// <summary>护盾值。</summary>
    public const float buff_shield_value = 120f;
    /// <summary>属性增益量（激励法阵等）。</summary>
    public const float buff_attr_value = 15f;
    #endregion

    #region 防守方技能参数（占位初值，待策划定稿）
    /// <summary>蘑菇感染在目标水晶上的存续时长（秒）。</summary>
    public const float mushroom_infect_duration = 60f;
    /// <summary>无限视野的可见距离加成（等同全图）。</summary>
    public const float infinite_view_distance = 99999f;
    /// <summary>死灵漫步的光环半径（米）。</summary>
    public const float death_stroll_radius = 4f;
    /// <summary>死灵漫步的移速提升倍率（占位初值，待策划定稿）：载体 = 动画播放速度倍率，与加速/减速/泥沼同一通道。</summary>
    public const float death_stroll_speed_up = 1.3f;
    /// <summary>召唤的一小波僵尸数量 / 等级。</summary>
    public const int summon_zombie_count = 5;
    public const int summon_zombie_level = 1;
    /// <summary>召唤的精英僵尸数量 / 等级。</summary>
    public const int summon_elite_count = 3;
    public const int summon_elite_level = 3;
    /// <summary>引爆瘟疫标记：每层造成的伤害。</summary>
    public const float plague_detonate_damage = 20f;
    /// <summary>索敌半径：岩石护盾 / 蘑菇感染（附近防御塔 / 水晶）。</summary>
    public const float defense_ally_cast_radius = 15f;
    /// <summary>沉默 / 瘟疫标记的作用半径（米）。</summary>
    public const float defense_nearby_radius = 8f;
    #endregion

    #region 技能参数补充（占位初值，待策划定稿）
    /// <summary>雷球链式跳：第二目标索敌半径（米）。</summary>
    public const float chain_jump_radius = 6f;
    /// <summary>雷球链式跳：整段飞行时长（秒）。</summary>
    public const float chain_jump_duration = 0.6f;
    /// <summary>空袭标记：落点轰炸半径（米）。</summary>
    public const float airstrike_radius = 3f;
    /// <summary>吸收水晶：作用半径（米）。</summary>
    public const float absorb_crystal_radius = 6f;
    /// <summary>吸收水晶：造成的伤害量（等同击败水晶，走概率产出流程）。</summary>
    public const float absorb_crystal_damage = 999999f;
    #endregion

    /// <summary>
    /// Buff 持续特效：客户端按 SCEntityDisplayInfo.buffs 增删（分配表见《特效清单与分配表》「三.2 Buff 与状态」）。
    /// 表内下标 = 清单编号 − 1；未列出的 Buff 无持续特效（属性修改类、纯逻辑类）。
    /// 迷雾（Fog）不在此表：其表现已由 EnvironmentManager 的体积雾承担，避免重复叠加。
    /// </summary>
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

    /// <summary>悬浮武器挂点表（本地坐标，相对实体根物体）：槽位 i 用第 i 个，左右交替分布。
    /// 客户端显示与服务器远程发射点共用本表，任何实体通用（不依赖 EntityAnim）。</summary>
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

    /// <summary>取槽位对应的悬浮武器本地偏移（越界取末位）。</summary>
    public static Vector3 GetWeaponFloatOffset(int slotIndex)
    {
        if (weapon_float_offsets.Length == 0) return Vector3.zero;
        return weapon_float_offsets[Mathf.Clamp(slotIndex, 0, weapon_float_offsets.Length - 1)];
    }
    #endregion

    #region 刚体（可移动单位的权威速度载体：位移效果只产出速度，位置由物理积分）
    /// <summary>可移动类别（双方角色 + 普通/精英僵尸）：只有这些类别挂 Rigidbody 并由移动系统驱动。</summary>
    public static bool IsMovable(EntityCategory category) => category switch
    {
        EntityCategory.Character_Attack or EntityCategory.Character_Defense or
        EntityCategory.Zombie or EntityCategory.EliteZombie => true,
        _ => false,
    };

    /// <summary>
    /// 刚体线性阻力：**必须为 0**。速度完全由动画声明的速度与地面摩擦决定（见 EntityData.ResolveMoveVelocity）——
    /// 阻力不为 0 会让"空中保持水平速度"失效，并在地面上叠加出第二条衰减曲线，与地面摩擦打架。
    /// </summary>
    public const float rb_drag = 0f;

    /// <summary>
    /// 地面水平摩擦（米/秒²）：**动画模块没有声明速度**且玩家没有推进输入时，水平速度朝 0 按此值衰减；
    /// 不在地面上则不衰减（保持水平速度 —— 跳跃/被击飞不会在空中掉速）。
    /// </summary>
    public const float move_ground_friction = 2f;

    /// <summary>刚体角阻力（旋转只锁 X/Z、**Y 轴不锁**；朝向由角色控制直接赋 rotation）。</summary>
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
    /// <summary>PC104 被动「暴击麻痹」：暴击命中时施加的麻痹时长（秒）。</summary>
    public const float crit_paralysis_duration = 1.5f;
    /// <summary>NP114 被动「夜间时间延长」：夜晚时长倍率（白天按同量压缩，一个昼夜周期总长不变）。</summary>
    public const float night_extend_factor = 1.5f;
    /// <summary>NP134 被动「教皇守护」：入夜时给守护点的减伤比例。</summary>
    public const float pope_guard_reduce_rate = 0.3f;
    /// <summary>PC102 被动「进攻方复活速度减慢」：进攻方复活进度倍率（仅进攻方，防守方不受影响）。</summary>
    public const float attack_revive_slow_factor = 0.6f;
    /// <summary>PC103 被动「光暗转化」：单一标记叠到此层数即转化为苍白之冰 / 苍白之雷。</summary>
    public const int pale_full_stacks = 10;
    /// <summary>PC103 被动「光暗转化」：光暗均达此层数且都未满时，双标记转化为苍白之火。</summary>
    public const int pale_mixed_stacks = 8;
    #endregion

    #region 僵尸刷新等级与变体数（策划案九/十章、二十章）
    /// <summary>夜间刷新普通僵尸的默认等级。</summary>
    public const int zombie_spawn_level = 1;
    /// <summary>PC106 被动「提升僵尸刷新时的等级」生效后，夜刷普通僵尸的等级。</summary>
    public const int zombie_spawn_level_boosted = 3;
    /// <summary>精英僵尸素材/属性配置数量（14 种，type.value 0~13；技能召唤时按此范围随机种类）。</summary>
    public const int elite_zombie_variant_count = 14;
    #endregion

    #region 灵火（TowerBlaze）：塔攻击附加爆炸
    /// <summary>附加爆炸的判定半径（米）。</summary>
    public const float tower_blaze_radius = 2.5f;
    /// <summary>附加爆炸的伤害倍率（相对塔的魔法伤害）。</summary>
    public const float tower_blaze_rate = 0.5f;
    #endregion
}
