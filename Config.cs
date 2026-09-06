using UnityEngine;

/// <summary>
/// 全局静态配置常量（V0.9 非对称攻防）。
/// 数值来源：策划案第二十章（战斗全局参数一览）与第二十一章（技能池）；后续可在玩法中调整。
/// </summary>
public static class Config
{
    #region 匹配与人数
    /// <summary>标准对局进攻方人数（4v1，可 AI 填充）。</summary>
    public const int attack_player_max = 4;
    /// <summary>标准对局防守方人数。</summary>
    public const int defense_player_max = 1;
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
    /// <summary>初始免费角色数量。</summary>
    public const int initial_unlocked_character_count = 3;
    /// <summary>
    /// 角色升级所需经验（下标 0 = 1→2 级，依次到 9→10 级）。
    /// 获得经验 = 对水晶造成的伤害量。
    /// </summary>
    public static readonly int[] level_up_exp = { 50000, 60000, 90000, 120000, 160000, 200000, 250000, 300000, 360000 };
    #endregion

    #region 守护点（瘟疫信标）
    /// <summary>外围守护点数量。</summary>
    public const int outer_beacon_count = 3;
    /// <summary>中心守护点数量。</summary>
    public const int core_beacon_count = 1;
    /// <summary>每个存活外围守护点给中心提供的减伤比例。</summary>
    public const float core_damage_reduce_per_outer = 0.25f;
    /// <summary>守护点血量（暂定）。</summary>
    public const int beacon_max_health = 5000;
    /// <summary>中心守护点血量（暂定）。</summary>
    public const int core_beacon_max_health = 8000;
    #endregion

    #region 资源与中立单位
    /// <summary>可采集水晶数量（暂定）。</summary>
    public const int crystal_count = 8;
    /// <summary>防御塔（瘟疫孢子）数量。</summary>
    public const int tower_count = 4;
    /// <summary>瘟疫树数量。</summary>
    public const int plague_tree_count = 1;
    /// <summary>瘟疫树攻占后 CD 恢复速度倍率。</summary>
    public const float plague_tree_cd_multiplier = 0.5f;
    /// <summary>瘟疫树攻占持续时间下限/上限（秒，30~60s 随机）。</summary>
    public const float plague_tree_effect_duration_min = 30f;
    public const float plague_tree_effect_duration_max = 60f;
    /// <summary>水晶刷新冷却下限/上限（秒，30~60s 随机）。</summary>
    public const float crystal_respawn_min = 30f;
    public const float crystal_respawn_max = 60f;
    /// <summary>摧毁水晶后获得技能的概率（15%，可调）。</summary>
    public const float crystal_skill_drop_chance = 0.15f;
    #endregion

    #region 僵尸
    /// <summary>全场僵尸数量上限（普通 + 精英，达上限停止刷新）。</summary>
    public const int zombie_max = 30;
    /// <summary>僵尸最大追击距离。</summary>
    public const float zombie_chase_range = 25f;
    /// <summary>夜间刷新 cd 进度增速——僵尸数为 0 时（每秒进度，越少越快）。</summary>
    public const float zombie_refresh_rate_fast = 0.5f;
    /// <summary>夜间刷新 cd 进度增速——僵尸数接近上限时（每秒进度）。</summary>
    public const float zombie_refresh_rate_slow = 0.05f;
    /// <summary>夜间刷新 cd 进度上限（达到即刷新一只并清零；僵尸数量达上限时不刷新且进度清零）。</summary>
    public const float zombie_refresh_progress_max = 1f;
    #endregion

    #region 昼夜
    /// <summary>白天时长（秒）。</summary>
    public const float day_duration = 80f;
    /// <summary>黄昏预警时长（秒）。</summary>
    public const float dusk_duration = 10f;
    /// <summary>夜晚时长（秒）。</summary>
    public const float night_duration = 80f;
    /// <summary>黎明预警时长（秒）。</summary>
    public const float dawn_duration = 10f;
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
    /// <summary>愈战愈勇每层增伤/减伤比例（10%）。</summary>
    public const float yz_growth_per_stack = 0.1f;
    #endregion

    #region 玩家操作（双手键盘：WASD 移动 / J 空手攻击 / K 跳跃 / U I O L H 技能槽 1~5）
    /// <summary>技能槽触发键（按槽位顺序：U I O L H）。</summary>
    public static readonly KeyCode[] skill_slot_keys = { KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.L, KeyCode.H };
    /// <summary>空手攻击键（静止 = 跃起砸地，移动 = 出拳）。</summary>
    public const KeyCode melee_key = KeyCode.J;
    /// <summary>跳跃键。</summary>
    public const KeyCode jump_key = KeyCode.K;
    /// <summary>滑铲键（左 Shift）。</summary>
    public const KeyCode slide_key = KeyCode.LeftShift;
    /// <summary>滑铲持续时间。</summary>
    public const float slide_duration = 0.6f;
    /// <summary>近战武器剑气射程上限（暂定）。</summary>
    public const float sword_qi_max_range = 8f;
    #endregion

    #region 武器与技能
    /// <summary>默认技能槽位数量（U I O L H 共 5 键；角色属性可覆盖）。</summary>
    public const int default_weapon_slot_count = 5;
    /// <summary>无施法动作弹幕的武器前摇（秒，暂定）。</summary>
    public const float weapon_short_windup = 0.15f;
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
    /// <summary>实体 id 回绕起点（ChunkSearcher 管理物体、EnsNetworkObjectManager 的 id 源同规则）。</summary>
    public const int entity_id_rollback_start = 100;
    /// <summary>技能自动索敌半径。</summary>
    public const float default_skill_auto_target_radius = 20f;
    #endregion
}
