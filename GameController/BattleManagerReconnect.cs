using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

// 会话补发：断线重连（战斗期会话保留到本局结束，其余阶段掉线直接移除玩家，见 NotifyPlayerOffline）
// 与「对局已开始时中途进房」共用同一套从零重建流程
public partial class BattleManager
{
    private readonly HashSet<int> offlineClients = new(); // 掉线中的令牌（仅战斗期登记）

    // 连接层断开：战斗期保留会话（本局内随时可以回来），大厅/加载期直接移除
    // —— 后者必须移除，否则「全员准备」与「全员加载回执」会被永久卡住
    public void NotifyPlayerOffline(int clientId)
    {
        if (!PlayerInfoList.ContainsKey(clientId)) return; // 主动退出：调用方已 RemovePlayer
        if (!BattleStarted)
        {
            RemovePlayer(clientId);
            return;
        }
        offlineClients.Add(clientId);
        Debug.Log($"[重连] 令牌 {clientId} 断线，会话保留至本局结束");
    }

    // 同一令牌再次进房：数据都还在，补发一次全量让客户端重建表现
    public void NotifyPlayerOnline(int clientId)
    {
        if (offlineClients.Remove(clientId)) Debug.Log($"[重连] 令牌 {clientId} 已重连");
        BroadcastLobbyInfo(); // 该客户端靠它取得 hasLobbyInfo（否则世界指令发不出去）
        if (BattleStarted) ResyncBattle(clientId);
    }

    // 一局结束：掉线者不再有重连意义，清出成员表（这是掉线会话唯一的清理时机）
    private void RemoveOfflineSessions()
    {
        if (offlineClients.Count == 0) return;
        var tokens = new List<int>(offlineClients);
        offlineClients.Clear();
        foreach (var token in tokens) RemovePlayer(token);
    }

    // 从零重建客户端（断线重连 / 对局中途进入共用）：视图、单位表、技能栏都以这里下发的为准
    private void ResyncBattle(int token)
    {
        if (!PlayerInfoList.ContainsKey(token)) return;

        // 清可见集合：下一轮同步按「首见全量」把视野内全部实体重发一遍
        visibleByClient.Remove(token);
        // 让客户端重新回执加载完成，随后由 OnClientLoadingComplete 补发「开战」把它送回战斗页
        loadedClients.Remove(token);

        Tool.NetworkManager.SendLoadingStart(token, new SCLoadingStart { camp = CampOf(token), resync = true });

        PlayerEntityId.TryGetValue(token, out var ownId);
        var info = PlayerInfoList[token];
        bool isDefense = PlayerCamps.TryGetValue(token, out var camp) && camp == PlayerCampType.Defense;
        Tool.NetworkManager.SendLoadingPrepared(token, new SCLoadingPrepared
        {
            ownEntityId = ownId,
            // 观战方没有战斗角色，与首次加载保持同一口径（沿用其进攻方选择，结算取角色下标用）
            characterType = isDefense ? info.defenseCharacter : info.attackCharacter,
            serverTime = Time.time,
        });

        // 玩家 → 实体关系全量：客户端据此认自己、名牌反查、观战列表
        foreach (var pair in PlayerEntityId) Tool.NetworkManager.SendPlayerEntity(pair.Key, pair.Value);

        // 中途快照：分数与剩余时间（gameState = 0 表示未结束，客户端不结算）
        Tool.NetworkManager.SendScoreInfo(token, new SCScoreInfo
        {
            resync = true,
            attackScore = AttackScore,
            defenseScore = DefenseScore(),
            killScore = DefenseKills,
            remainTime = Mathf.Max(0f, BattleRemainTime),
        });

        // 昼夜快照（心跳 10s 也会补，但重连应当立即正确）
        if (Tool.EnvironmentManager != null) Tool.NetworkManager.SendDayNightInfo(token, Tool.EnvironmentManager.BuildSnapshot());

        // 仅在「已死、等复活」时补复活进度：活着时 reviveStates 里仍有残留记录（进度已归零），
        // 照发会让客户端把复活条显示成停在 0%
        if (!PlayerEntityId.ContainsKey(token) && reviveStates.TryGetValue(token, out var revive))
            SendReviveProgress(token, revive, 0, false);

        // 阵营存活 / 守护点：作废「已发送快照」，下一帧的变更检测会自然重播一次
        InvalidateBroadcastSnapshots();
    }
}
