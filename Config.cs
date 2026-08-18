using System.Collections.Generic;
using UnityEngine;

public class Config
{
    #region 玩家带入属性
    public const int skill_slot_count = 6;
    public const int max_selected_imprint_count = 3;
    public const int max_selected_skill_count = 6;
    public const int take_in_imprint_count = max_selected_imprint_count;
    public const int take_in_skill_count = skill_slot_count;
    public const int max_take_in_rune_weight = 100;
    public const int default_c_rune_count = 999;
    public static readonly List<KeyCode> skill_input_key_list = new()
    {
        KeyCode.J,
        KeyCode.K,
        KeyCode.U,
        KeyCode.I,
        KeyCode.O,
        KeyCode.L,
    };
    #endregion

    #region 操作配置
    public const KeyCode move_forward_key = KeyCode.W;
    public const KeyCode move_left_key = KeyCode.A;
    public const KeyCode move_backward_key = KeyCode.S;
    public const KeyCode move_right_key = KeyCode.D;
    public const int camera_drag_mouse_button = 0;
    public const int auto_target_mouse_button = 0;
    public const int target_position_mouse_button = 1;
    public const int scroll_up_skill_slot_direction = -1;
    public const int scroll_down_skill_slot_direction = 1;
    public const float mouse_ray_max_distance = 1000f;
    public const float center_skill_ray_distance = 15f;
    /// <summary>屏幕中心瞄准：命中点距相机的距离比旋转中心距相机短至少该值（m）时，判定该命中点在玩家背后（被背后的墙遮挡），跳过</summary>
    public const float center_skill_occlude_gap = 1f;
    public const float default_skill_auto_target_radius = 10f;
    public const float default_skill_target_distance = 10f;
    public const float stamina_decay_per_second = 0.5f;
    public const float battle_camera_distance = 7f;
    public const float battle_camera_height = 3f;
    public const float battle_camera_rotate_speed = 120f;
    public const float battle_camera_drag_rotate_speed_horizontal = 80f;
    public const float battle_camera_drag_rotate_speed_vertical = -40f;
    public const float battle_camera_pitch_min = -30f;
    public const float battle_camera_pitch_max = 89f;
    public const float battle_camera_default_pitch = 45f;
    public const float battle_camera_look_height = 1.2f;
    #endregion

    #region UI与结算配置
    public const float exit_area_radius = 5f;
    public const float exit_max_read_time = 15f;
    public const float exit_normal_read_time = 10f;
    public const float exit_min_read_time = 5f;
    public const int lost_base_exp = 3;
    public const int min_exit_base_exp = 10;
    public const int normal_exit_base_exp = 18;
    public const int max_exit_base_exp = 40;
    public const int kill_player_exp = 5;
    public const int kill_zombie_exp = 1;
    public const int kill_infection_exp = 30;
    public const float zombie_drop_b_rune_rate = 0.05f;
    public const float zombie_drop_a_rune_rate = 0.02f;
    public const float zombie_drop_s_rune_rate = 0.003f;
    public const float infection_drop_a_rune_rate = 0.65f;
    public const float infection_drop_s_rune_rate = 0.35f;
    public const float plant_fruit_drop_rate = 0.2f;
    public const float plant_note_drop_rate = 0.25f;
    public const float infected_plant_fruit_drop_rate = 0.2f;
    public const float infected_plant_token_drop_rate = 0.03f;
    public const float infected_plant_rune_drop_rate = 1f;
    public const float infected_ore_token_drop_rate = 0.03f;
    public const float infected_ore_rune_drop_rate = 0.25f;
    public const float infection_token_drop_rate_without_level = 0.3f;
    public const float kill_imprint_exp_rate = 1f;
    public const float battle_ui_min_scale = 0.55f;
    public const float battle_ui_max_scale = 1f;
    public const float battle_ui_min_scale_distance = 4f;
    public const float battle_ui_max_scale_distance = 20f;
    #endregion

    #region 通用配置
    public struct UpgradeConfig
    {
        public int level;
        public int exp;//升级到下一级所需的经验
    }
    public const int max_entity_level = 10;
    public const int min_entity_level = 1;//玩家角色未解锁时level=0，获取解锁道具后level设置为1
    public const int max_imprint_level = 10;
    public const int min_imprint_level = 1;
    public const int note_count = 4;
    public const int character_count = 14;
    public const int npc_count = 61;//npc就是野怪，即Zombies
    public const int plant_count = 15;
    public const int ore_count = 8;
    public const int infection_count = 7;
    public const int imprint_count = 30;
    public const int skill_count = 31;
    public const float npc_attack_range = 2f;
    public const float npc_attack_pre_time = 0.4f;
    public const float npc_attack_post_time = 0.5f;

    //角色升级经验
    public static readonly List<UpgradeConfig> character_upgrade_exp = new()
    {
        new(){level=0,exp=500},
        new(){level=1,exp=250},
        new(){level=2,exp=500},
        new(){level=3,exp=750},
        new(){level=4,exp=1000},
        new(){level=5,exp=1250},
        new(){level=6,exp=1500},
        new(){level=7,exp=1750},
        new(){level=8,exp=2000},
        new(){level=9,exp=2250},
        new(){level=10,exp=int.MaxValue},
    };

    //印记升级经验
    public static readonly List<UpgradeConfig> imprint_upgrade_exp = new()
    {
        new(){level=0,exp=1},
        new(){level=1,exp=2},
        new(){level=2,exp=2},
        new(){level=3,exp=5},
        new(){level=4,exp=5},
        new(){level=5,exp=5},
        new(){level=6,exp=5},
        new(){level=7,exp=5},
        new(){level=8,exp=10},
        new(){level=9,exp=10},
        new(){level=10,exp=int.MaxValue},
    };
    #endregion

    #region 关卡信息
    public const float infection_update_interval = 10f;
    public const float infection_converge_target = 100f;
    public const float infection_converge_up_delta = 2f;
    public const float infection_converge_down_delta = 1.5f;
    public const float infection_min_value = 10f;
    public const float infection_max_value = 300f;
    public const float zombie_alive_infection_per_second = 0.1f;
    public const float zombie_spawn_infection_delta = 1f;
    public const float zombie_death_infection_delta = -4f;
    public const float infection_spawn_infection_delta = 20f;
    public const float zombie_migration_min_cd = 20f;
    public const float zombie_migration_max_cd = 40f;
    public const float zombie_spawn_interval = 10f;
    public const float resource_spawn_check_interval = 20f;
    public const float infection_spawn_check_interval = 40f;
    public const float interactable_prop_refresh_interval = 150f;
    public const float resource_respawn_cd = 90f;
    public const float infection_respawn_cd = 90f;
    public const float infected_resource_min_infection = 120f;
    public const float infected_resource_full_infection = 150f;
    public const float infection_entity_spawn_min_infection = 180f;
    public const float zombie_level_infection_rate = 0.03f;
    public const float resource_level_infection_rate = 0.03f;
    public const float ore_level_infection_rate = 0.03f;
    public const float infection_level_infection_rate = 0.03f;

    //撤离点开启区间与概率（线性插值）：appear=必定开启端、disappear=必定不开启端
    //初级 [50,100]：感染值越低概率越高 P=(disappear-v)/(disappear-appear)=(100-v)/50
    //中级 [120,180]：感染值越高概率越高 P=(v-disappear)/(appear-disappear)=(v-120)/60
    //高级 [200,300]：感染值越高概率越高 P=(v-disappear)/(appear-disappear)=(v-200)/100
    public const int exit_min_appear_infection_degree = 50;
    public const int exit_min_disappear_infection_degree = 100;
    public const int exit_normal_appear_infection_degree = 180;
    public const int exit_normal_disappear_infection_degree = 120;
    public const int exit_max_appear_infection_degree = 300;
    public const int exit_max_disappear_infection_degree = 200;
    //撤离带出负重上限（对应迷失0 / 初级60 / 中级120 / 高级200）
    public const int exit_max_carry_weight_limit = 200;
    public const int exit_normal_carry_weight_limit = 120;
    public const int exit_min_carry_weight_limit = 60;
    //角色经验结算：存活每分钟基础经验 + 结算倍率（初级×1 / 中级×1.5 / 高级×2 / 迷失×0.3）
    public const int character_exp_per_survive_minute = 5;
    public const float exit_exp_multiplier_max = 2f;
    public const float exit_exp_multiplier_normal = 1.5f;
    public const float exit_exp_multiplier_min = 1f;
    public const float exit_exp_multiplier_lost = 0.3f;
    //基础印记产出（随机印记类数 × 每类经验，从全部印记池随机）：迷失 1×3 / 初级 2×5 / 中级 3×6 / 高级 5×8
    public const int exit_lost_imprint_type_count = 1;
    public const int exit_lost_imprint_exp_each = 3;
    public const int exit_min_imprint_type_count = 2;
    public const int exit_min_imprint_exp_each = 5;
    public const int exit_normal_imprint_type_count = 3;
    public const int exit_normal_imprint_exp_each = 6;
    public const int exit_max_imprint_type_count = 5;
    public const int exit_max_imprint_exp_each = 8;
    //撤离结算信物奖励：中级触发概率与数量权重(0:50%/1:35%/2:15%)；高级数量权重(2:60%/3:30%/4:10%)
    public const float exit_token_mid_trigger_rate = 0.3f;
    public static readonly float[] exit_token_mid_count_weights = { 0.50f, 0.35f, 0.15f };        // 索引=数量 0~2
    public static readonly float[] exit_token_high_count_weights = { 0f, 0f, 0.60f, 0.30f, 0.10f }; // 索引=数量 0~4（2/3/4）
    #endregion

    #region //其它配置信息
    public struct PlayerCharacterConfig
    {
        public string JobName;
        public string Background;
        public string AbilityName;
        public string AbilityDescFormat(int level)
        {
            // 根据技能名称匹配计算公式
            return AbilityName switch
            {
                "攻守兼备" => $"攻击、防御各提升{5 * level}%",
                "探索经历" => $"增加经验获取{5 * level}%",
                "印记收集" => $"印记掉落量增加{5 * level}%",
                "矿石采集" => $"采矿伤害提升{40 * level}%",
                "火焰适应" => $"受到燃烧伤害降低{(100f / (1 + 0.1f * level)):F1}%",
                "无畏无惧" => $"受到伤害增加{10 * level}，自身造成伤害增加{20 * level}",
                "硬化身躯" => $"受到伤害减少防御的{level*0.1f}倍",
                "笔记收集" => $"笔记掉落量增加{5 * level}%",
                "强效再生" => $"周期性恢复生命值，等效等级 = {level}",
                "强健体魄" => $"普通撤离点最多携带击杀印记数量：{level * 3}",
                "剧毒适应" => $"受到瘟疫伤害降低{(100f/(1+10 * level)):F1}%",
                "魔术戏法" => $"造成伤害增加{20 * level}",
                "武术专精" => $"对其他玩家造成{10 * level}%增伤",
                "瘟疫研究" => $"对瘟疫单位造成{20 * level}%增伤",
                _ => string.Empty
            };
        }
    }
    public static readonly Dictionary<EntityType, PlayerCharacterConfig> CharacterConfigDict = new()
    {
        {
            EntityType.Char0,
            new PlayerCharacterConfig
            {
                JobName = "警察",
                Background = "曾是城区巡逻警，为人恪守规则，常年奔走在犯罪高发街区。病毒爆发当晚，他正独自处理街头骚乱，被失控感染者扑倒咬伤。尸变后依旧保留着执勤的本能，游荡在警局与老街道之间，警服残破、警棍紧握，眼神空洞却会本能追逐 “可疑人员”。",
                AbilityName = "攻守兼备",
            }
        },
        {
            EntityType.Char1,
            new PlayerCharacterConfig
            {
                JobName = "学生",
                Background = "十七岁的普通高中女生，性格内向，事发时正背着书包走在放学路上。突如其来的灾难打碎了平静，她在校园小巷遭到袭击。变成行尸后，破旧的校服沾满污渍，书包还斜挎在肩头，行动略显怯懦，常在废弃教学楼、操场徘徊，反复做出赶路、躲藏的动作。",
                AbilityName = "探索经历",
            }
        },
        {
            EntityType.Char2,
            new PlayerCharacterConfig
            {
                JobName = "主妇",
                Background = "普通社区主妇，操持家务、邻里和睦，灾难来临前正准备出门采购物资。猝不及防的感染让她失去理智，宽松的居家衣物撕裂磨损，体态臃肿，行动缓慢。始终游荡在居民楼片区，偶尔会对着空荡的家门驻足，残留着微弱的居家记忆。",
                AbilityName = "印记收集",
            }
        },
        {
            EntityType.Char3,
            new PlayerCharacterConfig
            {
                JobName = "保洁员",
                Background = "在市政大楼工作多年的保洁员，勤恳寡言，每天准时打扫整栋楼宇。病毒扩散时，他被困在密闭的地下清洁通道，最终不幸异变。身上套着沾满灰尘与污渍的工作服，手里还攥着拖把，机械地在走廊、地下室来回走动，重复着往日清扫的动作。",
                AbilityName = "矿石采集",
            }
        },
        {
            EntityType.Char4,
            new PlayerCharacterConfig
            {
                JobName = "搏击手",
                Background = "前地下搏击选手，常年健身、体格壮硕，依靠蛮力谋生。感染后病毒彻底激化了他体内的野性，身躯肌肉异常膨胀、皮肤龟裂外翻，力量远超普通僵尸。失去所有理智，仅凭破坏欲行动，横冲直撞，是区域内极具威胁的狂暴变异体。",
                AbilityName = "火焰适应",
            }
        },
        {
            EntityType.Char5,
            new PlayerCharacterConfig
            {
                JobName = "囚犯",
                Background = "重刑监狱在押囚犯，入狱前混迹街头。暴动爆发时监狱彻底失控，他在混乱中被狱警击断右臂，又被感染者咬伤。残缺的躯体让他行动失衡，囚服破烂不堪，独臂依旧会疯狂挥击，常年游荡在废弃监狱周边，充满暴戾与怨念。",
                AbilityName = "无畏无惧",
            }
        },
        {
            EntityType.Char6,
            new PlayerCharacterConfig
            {
                JobName = "安保",
                Background = "曾是重型安保人员，因精神异常被强制佩戴封闭式铁制防护面罩关押。病毒席卷后他挣脱束缚，体型远超常人，厚重铁面罩牢牢嵌在头部无法取下，遮挡住整张面孔。行动笨重但冲击力极强，金属面罩撞击墙面会发出沉闷异响，威慑力十足。",
                AbilityName = "硬化身躯",
            }
        },
        {
            EntityType.Char7,
            new PlayerCharacterConfig
            {
                JobName = "文员",
                Background = "普通工薪阶层，老实本分，每日奔波于公司与家之间。灾难爆发时身处闹市，在人群混乱中被感染。西装外套凌乱、皮鞋磨损，保留着成年人沉稳的体态，行动节奏平缓，混迹在各类废墟街区，是最常见的普通行尸。",
                AbilityName = "笔记收集",
            }
        },
        {
            EntityType.Char8,
            new PlayerCharacterConfig
            {
                JobName = "实验员",
                Background = "曾在城郊生物实验厂区工作，长期接触不明实验药剂。病毒与体内药剂产生诡异融合，后背、脖颈滋生出数条黏滑肉质触手，躯体扭曲畸形。不再依靠四肢单纯行走，触手可抓取、缠绕猎物，藏身于阴暗潮湿的仓库、下水道，外形惊悚诡异。",
                AbilityName = "强效再生",
            }
        },
        {
            EntityType.Char9,
            new PlayerCharacterConfig
            {
                JobName = "运动员",
                Background = "职业短跑运动员，爆发力与速度远超常人。感染后体能被病毒无限放大，骨骼与肌肉发生异变，身形矫健、动作迅猛。运动服破损不堪，奔跑时速度极快，擅长追逐猎物，在开阔场地中极具威胁，很难被甩开。",
                AbilityName = "强健体魄",
            }
        },
        {
            EntityType.Char10,
            new PlayerCharacterConfig
            {
                JobName = "上校",
                Background = "退役陆军上校，一生服从军令，性格冷峻威严。灾难初期他试图组织人员撤离、搭建防线，却在阵地沦陷时中弹负伤并被感染。军装勋章残缺，身姿依旧挺直，尸变后仍保留军人列队、巡查的本能，游荡在废弃军事据点，自带压迫感。",
                AbilityName = "剧毒适应",
            }
        },
        {
            EntityType.Char11,
            new PlayerCharacterConfig
            {
                JobName = "小丑",
                Background = "巡回马戏团职业小丑，常年以滑稽笑容取悦观众。病毒降临在演出场馆内，欢乐场地瞬间沦为炼狱。油彩斑驳脱落，夸张的小丑服饰残破扭曲，脸上凝固着诡异假笑。行动飘忽不定，时而蹦跳、时而潜行，笑声混杂嘶吼，氛围感阴森诡异。",
                AbilityName = "魔术戏法",
            }
        },
        {
            EntityType.Char12,
            new PlayerCharacterConfig
            {
                JobName = "武士",
                Background = "美式传统武道馆教习，痴迷古武士文化，日常身着练习服饰、佩戴武士短刀。灾难来袭时，武道馆成为沦陷重灾区，他奋力抵抗后不幸被咬。武道服撕裂，短刀仍握在手中，动作保留着习武的姿态，攻守意识残存，近战杀伤力极强。",
                AbilityName = "武术专精",
            }
        },
        {
            EntityType.Char13,
            new PlayerCharacterConfig
            {
                JobName = "白领",
                Background = "市中心写字楼白领，每日穿梭在格子间，精致干练。病毒爆发于上班早高峰，拥挤的电梯与办公区让她无处可逃。职业套裙凌乱变形，高跟鞋早已丢失，长发散乱，行动机械，反复徘徊在写字楼楼层之间，偶尔做出敲击键盘、翻看文件的无意识动作。",
                AbilityName = "瘟疫研究",
            }
        }
    };

    public static readonly List<string> ImprintName = new()
    {
        "锐锋印记",
        "双刃印记",
        "穿刺印记",
        "撕裂印记",
        "连斩印记",
        "飞刃印记",
        "齐射印记",
        "寒霜印记",
        "烈焰印记",
        "毒火印记",
        "腐蚀印记",
        "雷暴印记",
        "焚风印记",
        "爆燃印记",
        "剧毒印记",
        "破甲印记",
        "反击印记",
        "守护印记",
        "坚壁印记",
        "生长印记",
        "铁壁印记",
        "疾行印记",
        "迂回印记",
        "迅羽印记",
        "麻痹印记",
        "暗影印记",
        "治愈印记",
        "急救印记",
        "亡语印记",
        "炎怒印记"
    };
    public static string ImprintAbilityFormat(int type, int level)
    {
        return type switch
        {
            0 => $"攻击力提升 {20 * level}",
            1 => $"最终伤害提升 {10 * level}",
            2 => $"无视敌方 {20 + 10 * level}% 防御",
            3 => $"攻击附加流血效果，持续3秒，流血等级 = {level * 2}",
            4 => $"暴击率提升 {10 * level}",
            5 => $"暴击伤害提升 {20 * level}",
            6 => $"技能倍率提升 {0.2 * level:F1}",
            7 => $"冰冻持续时间延长 {level * 0.5f:F1} 秒",
            8 => $"燃烧伤害提升 {20 * level}%",
            9 => $"火焰与毒素混合伤害提升 {10 * level}%",
            10 => $"技能倍率提升 {0.2 + 0.02 * level:F2}",
            11 => $"伤害可溅射周围敌人，溅射比例 {5 * level}%",
            12 => $"施加灼烧等级提升 {level}",
            13 => $"技能冷却恢复速度提升 {4 * level}%",
            14 => $"毒素伤害提升 {16 * level}%",
            15 => $"无视敌方 {5 * level}% 防御",
            16 => $"对流血状态敌人额外造成 {12 * level}% 伤害",
            17 => $"防御值提升 {18 * level}",
            18 => $"受到攻击时反弹 {10 + 2 * level}% 伤害",
            19 => $"生命上限提升 {120 * level}",
            20 => $"基于自身防御获得 {5 * level}% 减伤",
            21 => $"体力上限提升 {5 * level}",
            22 => $"移动速度提升 {0.5 + 0.1 * level:F1}",
            23 => $"体力消耗降低 {5 * level}%",
            24 => $"基于自身攻击力提升 {5 * level}% 最终伤害",
            25 => $"拥有 {4 * level}% 概率闪避敌方伤害",
            26 => $"单次承受伤害最高不超过生命上限的 {30 - 5 * level}%",
            27 => $"攻击时有10%概率触发吸血效果",
            28 => $"敌方生命值越低伤害越高，最高增幅 {15 * level}%",
            29 => $"自身生命值越低伤害越高，最高增幅 {20 * level}%",
            _ => "未知印记效果"
        };
}
    public static readonly List<string> SkillNameList = new()
    {
        "风暴剑意",
        "血影突袭",
        "冰芒爆裂",
        "血雾侵蚀",
        "血铠护体",
        "血狱光柱",
        "地狱血啸",
        "狱血怒吼",
        "狱血穿刺弹",
        "齿轮风暴",
        "剧毒瓶",
        "黑球爆冲",
        "地裂冲击",
        "爆弹轰击",
        "风暴领域",
        "震波冲击",
        "裂地冲击",
        "连环地爆",
        "虚空爆破",
        "雷球弹跳",
        "雷霆天降",
        "虚空吞噬",
        "圣辉裁决",
        "鬼魂穿透弹",
        "毒柱喷涌",
        "雷影闪冲",
        "速射雷弹",
        "雷龙卷",
        "风卷聚怪",
        "魂漩风暴",
        "无限雷暴"
    };
    #endregion
}
