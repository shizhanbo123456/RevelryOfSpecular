/// <summary>
/// 客户端事件 ID 常量（EventManager 使用）。
/// 事件 ID 统一在此定义，禁止硬编码。新增事件在此追加，注意注释标明参数类型。
/// </summary>
public static class ClientEvent
{
    #region 连接与流程（10200 段）
    /// <summary>连接服务器成功（无参）。</summary>
    public const int OnConnect = 10200;
    /// <summary>进入世界完成（无参）。</summary>
    public const int OnEnterWorld = 10201;
    /// <summary>游戏需要重新开始/返回初始界面（无参）。</summary>
    public const int OnRestartGame = 10202;
    /// <summary>主动退出世界（无参）。</summary>
    public const int OnExitWorld = 10203;
    /// <summary>匹配队列状态变化（param=string 队列描述）。</summary>
    public const int OnMatchQueueUpdate = 10204;
    #endregion

    #region 战斗流程（10210 段）
    /// <summary>战斗开始（param=int 本地玩家分配阵营 0进攻 1防守）。</summary>
    public const int OnBattleStart = 10210;
    /// <summary>战斗结束（param=int 结果 0进攻胜 1防守胜 2平局 3提前结算）。</summary>
    public const int OnBattleEnd = 10211;
    /// <summary>昼夜阶段变化（param=int 阶段 0白天 1黄昏 2夜晚 3黎明）。</summary>
    public const int OnDayNightChange = 10212;
    /// <summary>分数更新（param=SCScoreInfo）。</summary>
    public const int OnScoreUpdate = 10213;
    // 10214~10219 已废弃：守护点血量/技能列表/技能运行时/武器获得
    // 均随实体表现摘要（param=SCEntityDisplayInfo，OnEntityDisplayUpdate）统一同步
    /// <summary>复活进度更新（param=SCReviveInfo）。</summary>
    public const int OnReviveProgressUpdate = 10215;
    /// <summary>击杀/目标事件（param=SCBattleEvent）。</summary>
    public const int OnBattleEvent = 10216;
    /// <summary>实体死亡（param=ushort 实体 id）。</summary>
    public const int OnEntityDead = 10220;
    /// <summary>本地玩家死亡/复活（param=bool true 死亡 false 复活）。</summary>
    public const int OnLocalPlayerAliveChange = 10221;
    /// <summary>组队大厅房间状态更新（param=SCRoomInfo）。</summary>
    public const int OnRoomInfoUpdate = 10222;
    #endregion

    #region 实体表现（10230 段）
    /// <summary>实体表现更新（param=SCEntityDisplayInfo）。</summary>
    public const int OnEntityDisplayUpdate = 10230;
    /// <summary>实体移除（param=int 实体 id）。</summary>
    public const int OnEntityDisplayRemove = 10231;
    /// <summary>实体创建（param=SCEntityDisplayInfo）。</summary>
    public const int OnEntityDisplayCreate = 10232;
    #endregion

    #region UI 通用（10300 段）
    /// <summary>显示提示消息（param=string 文案）。</summary>
    public const int ShowNotice = 10300;
    /// <summary>显示/隐藏加载界面（param=bool）。</summary>
    public const int ShowLoading = 10301;
    /// <summary>显示确认面板（param=string 文案，回调由面板持有）。</summary>
    public const int ShowConfirm = 10302;
    /// <summary>飘字（param=SCBattleEvent 文本信息）。</summary>
    public const int ShowFloatingText = 10303;
    /// <summary>右键阻断提示（param=string 文案；新操作方案下由技能/状态校验触发）。</summary>
    public const int OnRightClickBlocked = 10304;
    #endregion
}
