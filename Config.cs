using UnityEngine;

/// <summary>
/// 全局静态配置常量（V0.8 非对称攻防）。
/// 数值均为当前合理默认值，策划案第 17 章待定项处已标注 TODO，后续可改为配置资产。
/// </summary>
public static class Config
{
    #region 匹配与人数
    /// <summary>标准对局进攻方人数（4v1，可 AI 填充，TODO 待定）。</summary>
    public const int attack_player_max = 4;
    /// <summary>标准对局防守方人数。</summary>
    public const int defense_player_max = 1;
    /// <summary>单局时长（秒），TODO 待定。</summary>
    public const float battle_duration = 900f;
    #endregion

    #region 角色
    /// <summary>进攻方角色池数量（18 人，具体角色池待定）。</summary>
    public const int attack_character_count = 18;
    /// <summary>防守方角色池数量（6 人，基于实际模型资源）。</summary>
    public const int defense_character_count = 6;
    /// <summary>角色等级上限（1~10，TODO 待定）。</summary>
    public const int max_entity_level = 10;
    /// <summary>玩家等级达到该值解锁全部角色（TODO 待定）。</summary>
    public const int player_max_level = 50;
    /// <summary>初始免费角色数量（TODO 待定）。</summary>
    public const int initial_unlocked_character_count = 3;
    #endregion

    #region 守护点（瘟疫信标）
    /// <summary>外围守护点数量。</summary>
    public const int outer_beacon_count = 3;
    /// <summary>中心守护点数量。</summary>
    public const int core_beacon_count = 1;
    /// <summary>每个存活外围守护点给中心提供的减伤比例。</summary>
    public const float core_damage_reduce_per_outer = 0.25f;
    /// <summary>守护点血量（TODO 待定）。</summary>
    public const int beacon_max_health = 5000;
    /// <summary>中心守护点血量（TODO 待定）。</summary>
    public const int core_beacon_max_health = 8000;
    #endregion

    #region 资源与中立单位
    /// <summary>可采集水晶数量（TODO 待定）。</summary>
    public const int crystal_count = 8;
    /// <summary>防御塔（瘟疫孢子）数量。</summary>
    public const int tower_count = 4;
    /// <summary>瘟疫树数量。</summary>
    public const int plague_tree_count = 1;
    /// <summary>瘟疫树攻占后 CD 恢复速度倍率。</summary>
    public const float plague_tree_cd_multiplier = 0.5f;
    /// <summary>瘟疫树攻占持续时间（秒，TODO 待定）。</summary>
    public const float plague_tree_effect_duration = 30f;
    /// <summary>水晶刷新冷却（秒，TODO 待定）。</summary>
    public const float crystal_respawn_time = 60f;
    #endregion

    #region 僵尸
    /// <summary>全场僵尸数量下限。</summary>
    public const int zombie_min = 10;
    /// <summary>全场僵尸数量上限。</summary>
    public const int zombie_max = 30;
    /// <summary>精英僵尸同时存在上限。</summary>
    public const int elite_zombie_max = 3;
    /// <summary>僵尸最大追击距离。</summary>
    public const float zombie_chase_range = 25f;
    /// <summary>普通僵尸死亡后延迟刷新（秒，TODO 待定）。</summary>
    public const float zombie_respawn_delay = 20f;
    #endregion

    #region 昼夜
    /// <summary>白天时长（秒）。</summary>
    public const float day_duration = 180f;
    /// <summary>黄昏预警时长（秒）。</summary>
    public const float dusk_duration = 20f;
    /// <summary>夜晚时长（秒）。</summary>
    public const float night_duration = 120f;
    /// <summary>黎明预警时长（秒）。</summary>
    public const float dawn_duration = 20f;
    #endregion

    #region 复活与愈战愈勇
    /// <summary>白天复活进度攒满时间（秒，TODO 待定）。</summary>
    public const float revive_day_accumulate = 8f;
    /// <summary>夜晚复活进度攒满时间（秒，TODO 待定）。</summary>
    public const float revive_night_accumulate = 60f;
    /// <summary>每次死亡后复活进度积累变慢系数（乘法，TODO 待定）。</summary>
    public const float revive_dead_decay_per_death = 0.5f;
    /// <summary>愈战愈勇最大叠层（3~5，TODO 待定）。</summary>
    public const int yz_max_stack = 5;
    /// <summary>愈战愈勇每层属性提升比例（TODO 待定）。</summary>
    public const float yz_growth_per_stack = 0.1f;
    /// <summary>AI 复活时间倍率（AI 显著短于真人，TODO 待定）。</summary>
    public const float ai_revive_time_multiplier = 0.3f;
    #endregion

    #region 玩家操作
    /// <summary>滑铲持续时间（秒）。</summary>
    public const float slide_duration = 0.6f;
    /// <summary>滑铲移动速度倍率。</summary>
    public const float slide_speed_multiplier = 1.8f;
    /// <summary>右键阻断提示：选中技能为非远程/施法类时的提示文案（TODO 待定）。</summary>
    public const string right_click_blocked_notice = "当前选中技能无法远程触发";
    /// <summary>近战武器剑气射程上限（6~10m，TODO 待定）。</summary>
    public const float sword_qi_max_range = 8f;
    #endregion

    #region 武器与技能
    /// <summary>默认武器槽位数量（角色属性，进攻/防守双方，TODO 各角色具体槽位待配置）。</summary>
    public const int default_weapon_slot_count = 3;
    /// <summary>无施法动作弹幕的武器前摇（秒，0.1~0.2 建议，TODO 未拍板）。</summary>
    public const float weapon_short_windup = 0.15f;
    #endregion

    #region 结算与得分
    /// <summary>击杀分占总分比例（防守方得分=守护点剩余血量大头+击杀小分，TODO 待定）。</summary>
    public const float kill_score_weight = 0.1f;
    /// <summary>击杀基础分（TODO 待定）。</summary>
    public const int kill_score_base = 50;
    /// <summary>平局判定：双方分数差小于该值视为平局（TODO 待定）。</summary>
    public const float draw_score_threshold = 0.01f;
    #endregion

    #region 通用
    /// <summary>实体 id 回绕起点（ChunkSearcher 管理物体、EnsNetworkObjectManager 的 id 源同规则）。</summary>
    public const int entity_id_rollback_start = 100;
    /// <summary>技能自动索敌半径。</summary>
    public const float default_skill_auto_target_radius = 20f;
    #endregion
}
