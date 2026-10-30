public static class ClientEvent
{
    #region 连接与流程（10200 段）
    public const int OnConnect = 10200;
    public const int OnRestartGame = 10202;
    public const int OnExitWorld = 10203;
    #endregion

    #region 战斗流程（10210 段）
    public const int OnBattleStart = 10210;
    public const int OnBattleEnd = 10211;
    public const int OnDayNightChange = 10212;
    public const int OnScoreUpdate = 10213;
    public const int OnSettlementResult = 10214;
    // 10218~10219 未占用：技能槽位与武器等信息随实体表现摘要下发（见 OnEntityDisplayUpdate）
    public const int OnReviveProgressUpdate = 10215;
    public const int OnBattleEvent = 10216;
    public const int OnDefensePointProgress = 10217; // param=SCDefensePointProgress 四个守护点的开采进度（0~100）
    public const int OnSkillUsed = 10220; // param=int 技能 id（SCSkillUsed，只发给施法者本人，收到才扣一次可用次数）
    public const int OnLobbyInfoUpdate = 10222; // param=SCLobbyInfo 组队期房间成员/阵营/准备/AI 数量
    #endregion

    #region 实体表现（10230 段）
    public const int OnEntityDisplayUpdate = 10230;
    public const int OnEntityDisplayRemove = 10231;
    public const int OnMinimapUpdate = 10233;
    public const int OnMinimapRadiusUpdate = 10234; // param=float 切换后的雷达显示半径
    public const int OnCombatLabelDisplay = 10235;  // param=SCCombatLabel 战斗飘字（伤害/治疗）
    #endregion

    #region UI 通用（10300 段）
    public const int OnShowPrompt = 10305; // param=int notice.csv 消息 id（SCPrompt）
    public const int OnChatMessage = 10310; // param=SCChat 聊天消息
    public const int OnGmResult = 10311;    // param=SCGm GM 指令回包（服务器帧率/时间）
    public const int OnCampAliveCount = 10312; // param=SCCampAliveCount 双方总人数、存活数与观战人数（值变更时下发）
    #endregion
}
