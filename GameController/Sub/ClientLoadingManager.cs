using System.Collections.Generic;
using Ros.Transport;

// 客户端加载管理器：切加载界面 → 逐步请求开战数据 → 回执加载完成 → 收到「开始战斗」后切页。
// 战斗期的玩家名字与玩家↔实体关系全部来自这里，不再读组队期缓存。
public class ClientLoadingManager : ClientSubManager
{
    private enum Step
    {
        Idle,           // 未开始或已结束
        AttackPlayers,
        DefensePlayers,
        AiUnits,
    }

    private readonly List<LoadingUnitEntry> units = new(); // 三步汇总：真人玩家 + AI 单位
    private readonly Dictionary<int, string> names = new(); // 玩家令牌 → 名字
    private readonly Dictionary<ushort, int> entityOwners = new(); // 实体 id → 玩家令牌（名牌反查）
    private readonly Dictionary<int, int> playerEntities = new(); // 玩家令牌 → 实体 id（-1 = 无实体）

    private Step step = Step.Idle;

    public static ClientLoadingManager Current =>
        Tool.ClientLogicManager != null ? Tool.ClientLogicManager.Loading : null;

    public bool HasSession { get; private set; }
    public EntityCamp Camp { get; private set; }
    public ushort OwnEntityId { get; private set; }
    public EntityType CharacterType { get; private set; } // 本局使用的角色（结算算角色经验用）
    public bool IsSpectator => HasSession && Camp == EntityCamp.None;

    protected override void BindEvents()
    {
        EventManager.AddEvent(ClientEvent.OnExitWorld, Clear);
        EventManager.AddEvent(ClientEvent.OnRestartGame, Clear);
    }

    protected override void UnbindEvents()
    {
        EventManager.RemoveEvent(ClientEvent.OnExitWorld, Clear);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, Clear);
    }

    #region//加载流程
    // 服务器宣告进入加载阶段：切加载界面（此时还没有实体 id，等 Prepared）
    public void OnLoadingStart(SCLoadingStart info)
    {
        ClearData();
        HasSession = true;
        Camp = info.camp;
        OwnEntityId = 0;
        step = Step.Idle;
        if (Tool.UIManager != null) Tool.UIManager.ShowLoading();
    }

    // 服务器预处理完成（实体 id 已预分配）：开始逐步请求数据
    public void OnLoadingPrepared(SCLoadingPrepared info)
    {
        OwnEntityId = info.ownEntityId;
        CharacterType = info.characterType;
        NetworkManager.SyncServerTime(info.serverTime); // 服务器时间基准，顺带校准本地时钟
        step = Step.AttackPlayers;
        if (Tool.UIManager != null) Tool.UIManager.SetLoadingProgress(0f, "正在加载…");
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendLoadingAttackPlayerRequest();
    }

    public void OnAttackPlayerResponse(SCLoadingAttackPlayerResponse info)
    {
        if (step != Step.AttackPlayers) return;
        Merge(info.list);
        step = Step.DefensePlayers;
        if (Tool.UIManager != null) Tool.UIManager.SetLoadingProgress(1f / 3f, "正在加载…");
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendLoadingDefensePlayerRequest();
    }

    public void OnDefensePlayerResponse(SCLoadingDefensePlayerResponse info)
    {
        if (step != Step.DefensePlayers) return;
        Merge(info.list);
        step = Step.AiUnits;
        if (Tool.UIManager != null) Tool.UIManager.SetLoadingProgress(2f / 3f, "正在加载…");
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendLoadingAiUnitRequest();
    }

    public void OnAiUnitResponse(SCLoadingAiUnitResponse info)
    {
        if (step != Step.AiUnits) return;
        Merge(info.list);
        step = Step.Idle;
        if (Tool.UIManager != null) Tool.UIManager.SetLoadingProgress(1f, "加载完成");
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendLoadingComplete();
    }

    // 服务器允许进入战斗：收起加载界面，切页交给 UIManager（监听 OnBattleStart）
    public void OnBattleStart()
    {
        if (Tool.UIManager != null) Tool.UIManager.HideLoading();
        EventManager.TrigEvent(ClientEvent.OnBattleStart, (int)Camp);
    }
    #endregion

    #region//数据查询
    // 战斗期玩家名字（AI 也在这套表里，直接带名字下发）
    public string GetName(int clientId)
    {
        if (names.TryGetValue(clientId, out var name) && !string.IsNullOrEmpty(name)) return name;
        return $"玩家{clientId}";
    }

    public string GetNameByEntity(ushort entityId)
    {
        return entityOwners.TryGetValue(entityId, out var clientId) ? GetName(clientId) : "玩家?";
    }

    // clientId → 当前实体 id；-1 = 无实体（死亡 / 观战 / 离场）
    public int GetEntityId(int clientId)
    {
        return playerEntities.TryGetValue(clientId, out var entityId) ? entityId : -1;
    }

    // 所有参战单位（真人 + AI），供死亡观战/纯观战的目标列表使用
    public List<LoadingUnitEntry> GetCombatants() => units;
    #endregion

    #region//Local
    private void Merge(List<LoadingUnitEntry> list)
    {
        if (list == null) return;
        foreach (var entry in list)
        {
            if (entry == null) continue;
            units.Add(entry);
            names[entry.clientId] = entry.name;
            entityOwners[entry.entityId] = entry.clientId;
            playerEntities[entry.clientId] = entry.entityId;
        }
    }

    // 玩家 → 实体关系由网络层直连转入：开战前的批量下发与界面创建存在时差，走事件会整批丢包
    public void OnPlayerEntity(SCPlayerEntity info)
    {
        if (!HasSession || info == null) return;

        // 复活换新实体 id：清掉该玩家的旧关系，避免旧 id 仍能反查到人
        if (playerEntities.TryGetValue(info.clientId, out var old) && old > 0
            && entityOwners.TryGetValue((ushort)old, out var oldOwner) && oldOwner == info.clientId)
            entityOwners.Remove((ushort)old);

        playerEntities[info.clientId] = info.entityId;
        if (info.entityId > 0) entityOwners[(ushort)info.entityId] = info.clientId;

        if (info.clientId == NetworkManager.LocalToken)
            OwnEntityId = info.entityId > 0 ? (ushort)info.entityId : (ushort)0;
    }

    private void Clear()
    {
        ClearData();
        HasSession = false;
        Camp = EntityCamp.None;
        OwnEntityId = 0;
        if (Tool.UIManager != null) Tool.UIManager.HideLoading();
    }

    private void ClearData()
    {
        units.Clear();
        names.Clear();
        entityOwners.Clear();
        playerEntities.Clear();
        step = Step.Idle;
    }
    #endregion
}
