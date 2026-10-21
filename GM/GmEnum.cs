namespace Ros.Gm
{
    public enum GmEnum
    {
        [GmLabel("对局-时间-增加")]
        [GmStatus(GmStatusType.Battle)]
        Battle_RemainTime_Add,
        [GmLabel("对局-时间-减少")]
        [GmStatus(GmStatusType.Battle)]
        Battle_RemainTime_Reduce,
        [GmLabel("对局-时间-设置")]
        [GmStatus(GmStatusType.Battle)]
        Battle_RemainTime_Set,
        [GmLabel("对局-立即结算")]
        [GmStatus(GmStatusType.Battle)]
        Battle_Settle,
        [GmLabel("对局-昼夜-设置时间点")]
        [GmStatus(GmStatusType.Battle)]
        Battle_DayNight_SetTimePoint,
        [GmLabel("对局-昼夜-设置白天时长")]
        [GmStatus(GmStatusType.Battle)]
        Battle_DayNight_SetDayDuration,
        [GmLabel("对局-昼夜-设置晚上时长")]
        [GmStatus(GmStatusType.Battle)]
        Battle_DayNight_SetNightDuration,

        [GmLabel("实体-刷新")]
        [GmStatus(GmStatusType.Battle)]
        Entity_Spawn,
        [GmLabel("实体-击杀")]
        [GmStatus(GmStatusType.Battle)]
        Entity_Kill,
        [GmLabel("实体-击飞")]
        [GmStatus(GmStatusType.Battle)]
        Entity_Knockback,

        [GmLabel("玩家-传送-坐标")]
        [GmStatus(GmStatusType.Battle)]
        Player_Teleport_ToCoordinate,
        [GmLabel("玩家-传送-雕像")]
        [GmStatus(GmStatusType.Battle)]
        Player_Teleport_ToGuardPoint,
        [GmLabel("玩家-立即复活")]
        [GmStatus(GmStatusType.Battle)]
        Player_Revive,
        [GmLabel("玩家-技能-获得技能")]
        [GmStatus(GmStatusType.Battle)]
        Player_Skill_GetSkill,
        [GmLabel("玩家-技能-使用技能")]
        [GmStatus(GmStatusType.Battle)]
        Player_Skill_DirectUseSkill,
        [GmLabel("玩家-技能-清空技能")]
        [GmStatus(GmStatusType.Battle)]
        Player_Skill_ClearSkill,
        [GmLabel("玩家-技能-被动技能启用")]
        [GmStatus(GmStatusType.Lobby)]
        Player_Skill_PassiveEnable,
        [GmLabel("玩家-buff-添加")]
        [GmStatus(GmStatusType.Battle)]
        Player_Buff_AddBuff,
        [GmLabel("玩家-buff-移除指定")]
        [GmStatus(GmStatusType.Battle)]
        Player_Buff_RemoveBuff,
        [GmLabel("玩家-buff-移除全部")]
        [GmStatus(GmStatusType.Battle)]
        Player_Buff_RemoveAllBuff,
        [GmLabel("玩家-外挂-飞行")]
        [GmStatus(GmStatusType.Battle)]
        Player_Cheat_Flight,
        [GmLabel("玩家-外挂-无敌")]
        [GmStatus(GmStatusType.Battle)]
        Player_Cheat_Invincible,
        [GmLabel("玩家-外挂-极速")]
        [GmStatus(GmStatusType.Battle)]
        Player_Cheat_Speed,

        [GmLabel("画面-地形-显示半径")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Screen_Terrain_ShowRadius,
        [GmLabel("画面-地形-显隐地形")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Screen_Terrain_ToggleTerrain,
        [GmLabel("画面-雾气")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Screen_Fog,

        [GmLabel("存档-玩家等级-增加经验")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Save_PlayerLevel_AddExp,
        [GmLabel("存档-玩家等级-减少经验")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Save_PlayerLevel_ReduceExp,
        [GmLabel("存档-玩家等级-增加等级")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Save_PlayerLevel_AddLevel,
        [GmLabel("存档-玩家等级-减少等级")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Save_PlayerLevel_ReduceLevel,
        [GmLabel("存档-角色等级")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Save_CharacterLevel,

        [GmLabel("其它-结算界面")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Other_Settlement,
        [GmLabel("其它-调试")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Other_Debug,
        [GmLabel("其它-性能-模型网格面数")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Other_Collect_MeshFaces,
        [GmLabel("其它-性能-服务器帧率")]
        [GmStatus(GmStatusType.Lobby | GmStatusType.Battle)]
        Other_Collect_ServerFps,
        [GmLabel("其它-性能-服务器时间")]
        [GmStatus(GmStatusType.Lobby | GmStatusType.Battle)]
        Other_Collect_ServerTime,
        [GmLabel("其它-战斗数据-僵尸数量")]
        [GmStatus(GmStatusType.Battle)]
        Other_CombatData_ZombieCount,
        [GmLabel("其它-战斗数据-僵尸刷新等级")]
        [GmStatus(GmStatusType.Battle)]
        Other_CombatData_ZombieSpawnLevel,
        [GmLabel("其它-战斗数据-白天时长")]
        [GmStatus(GmStatusType.Battle)]
        Other_CombatData_DayDuration,
        [GmLabel("其它-战斗数据-夜晚时长")]
        [GmStatus(GmStatusType.Battle)]
        Other_CombatData_NightDuration,
        [GmLabel("其它-敌方对话")]
        [GmStatus(GmStatusType.Lobby | GmStatusType.Battle)]
        Other_EnemyDialog,
        [GmLabel("其它-本地延迟")]
        [GmStatus(GmStatusType.Offline | GmStatusType.Lobby | GmStatusType.Battle)]
        Other_LocalDelay,
    }
}
