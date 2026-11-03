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
    // 10218~10219 未占用
    public const int OnReviveProgressUpdate = 10215;
    public const int OnBattleEvent = 10216;
    public const int OnDefensePointProgress = 10217; // param=SCDefensePointProgress 四个守护点的开采进度（0~100）
    public const int OnSkillBarRefresh = 10221; // 技能栏需要重渲染（持有表变化 / 选择面板应用 / 服务器快照到达）
    public const int OnLobbyInfoUpdate = 10222; // param=SCLobbyInfo 组队期房间成员/阵营/准备/AI 数量
    #endregion

    #region 实体表现（10230 段）
    public const int OnEntityDisplayUpdate = 10230;
    public const int OnEntityDisplayRemove = 10231;
    public const int OnMinimapUpdate = 10233;
    public const int OnMinimapRadiusUpdate = 10234; // param=float 切换后的雷达显示半径
    public const int OnCombatLabelDisplay = 10235;  // param=SCCombatLabel 战斗飘字（伤害/治疗）
    public const int OnGodViewEntities = 10236;     // param=SCGodViewEntities 上帝视角实体块（不含珊瑚）
    public const int OnGodViewCorals = 10237;       // param=SCGodViewCorals 上帝视角珊瑚块（仅 id 与位置）
    public const int OnGodViewDetail = 10238;       // param=SCGodViewEntityDetail 上帝视角点选实体的详情快照
    #endregion

    #region UI 通用（10300 段）
    public const int OnShowPrompt = 10305; // param=int notice.csv 消息 id（SCPrompt）
    public const int OnChatMessage = 10310; // param=SCChat 聊天消息
    public const int OnGmResult = 10311;    // param=SCGm GM 指令回包（服务器帧率/时间）
    public const int OnCampAliveCount = 10312; // param=SCCampAliveCount 双方总人数、存活数与观战人数（值变更时下发）
    public const int OnQuickMessage = 10313;   // param=SCQuickMessage 同阵营快捷消息（含发送者与人机上报状况）
    public const int OnStateReport = 10314;       // param=SCStateReport 同阵营状态上报（客户端本地拼聊天文本）
    #endregion
}
