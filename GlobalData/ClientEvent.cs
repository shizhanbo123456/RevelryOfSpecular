public static class ClientEvent
{
    #region 连接与流程（10200 段）
    public const int OnConnect = 10200;
    public const int OnEnterWorld = 10201;
    public const int OnRestartGame = 10202;
    public const int OnExitWorld = 10203;
    public const int OnMatchQueueUpdate = 10204;
    #endregion

    #region 战斗流程（10210 段）
    public const int OnBattleStart = 10210;
    public const int OnBattleEnd = 10211;
    public const int OnDayNightChange = 10212;
    public const int OnScoreUpdate = 10213;
    public const int OnSettlementResult = 10214;
    // 10217~10219 未占用：守护点血量、技能槽位与武器等信息统一随实体表现摘要下发
    // （param=SCEntityDisplayInfo，见 OnEntityDisplayUpdate），不单独发事件
    public const int OnReviveProgressUpdate = 10215;
    public const int OnBattleEvent = 10216;
    public const int OnLocalPlayerAliveChange = 10221;
    public const int OnRoomInfoUpdate = 10222;
    #endregion

    #region 实体表现（10230 段）
    public const int OnEntityDisplayUpdate = 10230;
    public const int OnEntityDisplayRemove = 10231;
    public const int OnEntityDisplayCreate = 10232;
    public const int OnMinimapUpdate = 10233;
    public const int OnMinimapRadiusUpdate = 10234; // param=float 切换后的雷达显示半径
    #endregion

    #region UI 通用（10300 段）
    public const int ShowNotice = 10300;
    public const int ShowLoading = 10301;
    public const int ShowConfirm = 10302;
    public const int ShowFloatingText = 10303;
    #endregion
}
