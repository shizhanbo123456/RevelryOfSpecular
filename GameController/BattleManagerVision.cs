using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;
using UnityEngine.Profiling;

public partial class BattleManager
{
    // 小地图同步的分段性能采样标签（仅用于 Profiler 定位，不影响任何逻辑）
    private const string kMinimapSyncTag = "BM.Minimap.Sync";
    private const string kMinimapPruneTag = "BM.Minimap.Prune";
    private const string kMinimapCampLoopTag = "BM.Minimap.CampLoop";
    private const string kMinimapCampLostTag = "BM.Minimap.IsCampLost";
    private const string kMinimapBuildCampTag = "BM.Minimap.BuildCamp";
    private const string kMinimapCampScanTag = "BM.Minimap.Scan";
    private const string kMinimapCollectTag = "BM.Minimap.CollectVision";
    private const string kMinimapAppendTag = "BM.Minimap.Append";
    private const string kMinimapMessageTag = "BM.Minimap.BuildMessage";
    private const string kMinimapBuildSelfTag = "BM.Minimap.BuildSelf";
    private const string kMinimapSendTag = "BM.Minimap.Send";

    private float minimapTimer;

    private readonly Dictionary<short, HashSet<ushort>> visibleByClient = new();
    private readonly Dictionary<short, float> minimapRadiusByClient = new(); // 各客户端雷达显示半径（F 键切换，缺省 = minimap_view_radius）

    #region 视野锚点与观战
    // 每个玩家的视野锚点：位置/半径随操控实体逐帧刷新；实体销毁（死亡）后冻结在死亡位置；
    // 观战时跟随目标实体（相机跟随者），目标死亡清空其观战列表后自然冻结
    public class VisionState
    {
        public ushort entityId;                           // 当前操控实体 id（死亡后保留最后值）
        public Vector3 position;                          // 视野判定圆心
        public float viewRadius;                          // 视野判定半径
        public bool spectating;                           // 是否正在观战
        public short spectateTarget = -1;                 // 观战目标 id（-1 = 无；AI 为虚拟负数 id）
        public readonly List<short> spectators = new();   // 观战我的玩家列表
    }

    private readonly Dictionary<short, VisionState> visionStates = new();

    // 每帧刷新：存活且未观战 = 自己实体；观战中 = 目标实体；无实体可跟随（死亡/目标死亡）时保持冻结
    private void TickVisionStates()
    {
        foreach (var pair in PlayerInfoList)
        {
            if (!visionStates.TryGetValue(pair.Key, out var state))
            {
                state = new VisionState();
                visionStates[pair.Key] = state;
            }
            // 目标实体已销毁：立即清空其观战列表，观战者回到各自的冻结视野
            if (state.spectators.Count > 0 && GetEntityOfClient(pair.Key) == null) ClearSpectators(pair.Key);

            var follow = state.spectating && visionStates.TryGetValue(state.spectateTarget, out _)
                ? GetEntityOfClient(state.spectateTarget)
                : GetEntityOfClient(pair.Key);
            if (follow == null) continue; // 冻结：不再更新位置与半径
            state.entityId = follow.id;
            state.position = follow.transform.position;
            state.viewRadius = VisionRadius(follow);
        }
    }

    // 观战请求：目标须为参战单位（真人或 AI）、存活且不可是自己；非法 / 死亡 / 异阵营目标 = 退出观战
    public void SpectateRequest(short clientId, int targetClientId)
    {
        if (!AtServer || !BattleStarted) return;
        ExitSpectate(clientId);
        if (!visionStates.TryGetValue(clientId, out var state)) return;
        if (targetClientId == -1 || targetClientId == clientId) return; // -1 = 无目标
        if (targetClientId < short.MinValue || targetClientId > short.MaxValue) return; // 超出 clientId 值域
        short target = (short)targetClientId;
        if (!PlayerInfoList.ContainsKey(target) || !visionStates.TryGetValue(target, out var targetState)) return;
        if (!PlayerCamps.TryGetValue(clientId, out var myCamp) || !PlayerCamps.TryGetValue(target, out var targetCamp)) return;
        if (targetCamp == PlayerCampType.Spectator) return; // 观战方玩家无实体，无可跟随视角
        // 观战方玩家可观战任意存活单位；攻/守玩家只能观战同阵营
        if (myCamp != PlayerCampType.Spectator && myCamp != targetCamp) return;
        if (GetEntityOfClient(target) == null) return; // 目标死亡
        state.spectating = true;
        state.spectateTarget = target;
        targetState.spectators.Add(clientId);
    }

    // 退出观战：复活 / 换目标 / 退场时调用
    public void ExitSpectate(short clientId)
    {
        if (!visionStates.TryGetValue(clientId, out var state) || !state.spectating) return;
        state.spectating = false;
        if (state.spectateTarget != -1 && visionStates.TryGetValue(state.spectateTarget, out var target))
            target.spectators.Remove(clientId);
        state.spectateTarget = -1;
    }

    // 清空某玩家的观战列表（其操控实体死亡时）
    private void ClearSpectators(short clientId)
    {
        if (!visionStates.TryGetValue(clientId, out var state)) return;
        foreach (var spectator in state.spectators)
        {
            if (visionStates.TryGetValue(spectator, out var s) && s.spectateTarget == clientId)
            {
                s.spectating = false;
                s.spectateTarget = -1;
            }
        }
        state.spectators.Clear();
    }

    // 以玩家视野锚点（而非其实体）判定可见性：同阵营恒可见，否则 XZ 距离 ≤ 锚点半径
    private bool CanSeeFromState(short clientId, EntityData target)
    {
        if (target == null) return false;
        if (!visionStates.TryGetValue(clientId, out var state)) return false;
        if (PlayerCamps.TryGetValue(clientId, out var camp))
        {
            // 同阵营恒可见（观战方玩家无实体阵营，不走此捷径）
            if (camp == PlayerCampType.Attack && target.camp == EntityCamp.Attack) return true;
            if (camp == PlayerCampType.Defense && target.camp == EntityCamp.Defense) return true;
        }
        return IsInRadius(state.position, target.transform.position, state.viewRadius);
    }
    #endregion

    private static readonly EntityCamp[] s_camps = { EntityCamp.Attack, EntityCamp.Defense };
    private static readonly List<SCMinimapEntity> s_minimapEntries = new();
    private static readonly HashSet<ushort> s_visibleScratch = new();
    private static readonly HashSet<ushort> s_dirtySweep = new(); // 本轮至少被一个客户端可见的实体（用于清 health/buff 脏标记）
    private static readonly HashSet<ushort> s_removedScratch = new();
    private static readonly HashSet<short> s_clientScratch = new();

    private struct CampVisionSource
    {
        public float x;
        public float z;
        public float radiusSq;
    }
    private static readonly List<CampVisionSource> s_campVisionSources = new();

    #region 世界可见（模型同步过滤）
    private EntityData GetEntityOfClient(short clientId)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return null;
        return EntityContainer.Entities.TryGetObject(entityId, out var entity) ? entity : null;
    }

    public void SendCombatLabelToViewers(EntityData target, SCCombatLabel label)
    {
        foreach (var clientId in PlayerInfoList.Keys)
        {
            if (!CanSeeFromState(clientId, target)) continue;
            Tool.NetworkManager.SendCombatLabel(clientId, label);
        }
    }

    private void BeginClientVisibility(short clientId)
    {
        if (!visibleByClient.TryGetValue(clientId, out var seen))
        {
            seen = new HashSet<ushort>();
            visibleByClient[clientId] = seen;
        }
        s_visibleScratch.Clear();
    }

    private static void MarkClientVisible(ushort entityId)
    {
        s_visibleScratch.Add(entityId);
        s_dirtySweep.Add(entityId);
    }

    private void EndClientVisibility(short clientId)
    {
        if (!visibleByClient.TryGetValue(clientId, out var seen)) return;
        if (seen.Count > 0)
        {
            s_removedScratch.Clear();
            foreach (var id in seen)
            {
                if (!s_visibleScratch.Contains(id)) s_removedScratch.Add(id);
            }
            foreach (var id in s_removedScratch)
            {
                Tool.NetworkManager.SendRemoveEntity(clientId, id);
            }
        }
        seen.Clear();
        foreach (var id in s_visibleScratch) seen.Add(id);
    }
    #endregion

    #region 小地图（阵营共享视野）
    private static float VisionRadius(EntityData entity)
    {
        if (entity == null) return 0f;
        return Mathf.Max(0f, entity.floatingAttribute.viewDistance);
    }

    private static bool IsMarkedOnMinimap(EntityData entity)
    {
        return entity != null && entity.effectController != null && entity.effectController.HasEffect(EffectType.EyeMark);
    }

    // 黑夜标记：敌对小地图上完全不下发（本阵营仍可见）
    private static bool IsHiddenFromEnemyMinimap(EntityData entity)
    {
        return entity != null && entity.effectController != null && entity.effectController.HasEffect(EffectType.NightMark);
    }

    private static bool IsCampMinimapLost(EntityCamp camp)
    {
        foreach (var member in EntityContainer.Entities)
        {
            if (member == null || member.camp != camp) continue;
            if (member.effectController != null && member.effectController.HasEffect(EffectType.MinimapLost)) return true;
        }
        return false;
    }

    private void SyncMinimapToClients()
    {
        // 采样名带上实体总数 E：在 Profiler 里可直接读出参与遍历的规模（判定 O(E²) 是否被实体数放大）
        Profiler.BeginSample(kMinimapSyncTag + " E=" + EntityContainer.Entities.Count);

        Profiler.BeginSample(kMinimapPruneTag);
        PruneVisibilityCache();
        Profiler.EndSample();

        Profiler.BeginSample(kMinimapCampLoopTag);
        foreach (var camp in s_camps)
        {
            Profiler.BeginSample(kMinimapCampLostTag);
            bool lost = IsCampMinimapLost(camp);
            Profiler.EndSample();

            Profiler.BeginSample(kMinimapBuildCampTag);
            if (!lost) BuildCampMinimapEntries(camp);
            Profiler.EndSample();

            Profiler.BeginSample(kMinimapSendTag);
            foreach (var pair in PlayerInfoList)
            {
                if (!visionStates.TryGetValue(pair.Key, out var state)) continue;
                if (!PlayerCamps.TryGetValue(pair.Key, out var memberCamp)) continue;
                if (memberCamp == PlayerCampType.Spectator)
                {
                    // 观战方玩家无阵营：跟随观战目标所在阵营的共享视野
                    if (!state.spectating || !PlayerCamps.TryGetValue(state.spectateTarget, out var targetCamp)
                        || !IsMemberOfCamp(targetCamp, camp)) continue;
                }
                else if (!IsMemberOfCamp(memberCamp, camp)) continue;
                // 阵营小地图失效（夜间/致盲）：本 tick 不传输任何点位包，客户端超时后自动隐藏
                if (lost) continue;
                if (!loadedClients.Contains(pair.Key)) continue; // 未完成加载的客户端先不给小地图数据
                float radiusSq = GetMinimapRadius(pair.Key);
                radiusSq *= radiusSq;
                var viewPos = state.position; // 视野锚点：存活/观战/死亡冻结统一由 TickVisionStates 维护
                // 每个实体独立成包（无片段号、不拼回）；客户端分帧累积，并对超时未更新的点位做隐藏。
                // 非本阵营单位按该客户端的雷达显示半径在服务器侧过滤，圈外不下发
                foreach (var entry in s_minimapEntries)
                {
                    if (entry.camp != camp)
                    {
                        float dx = entry.posX - viewPos.x, dz = entry.posZ - viewPos.z;
                        if (dx * dx + dz * dz > radiusSq) continue;
                    }
                    Tool.NetworkManager.SendMinimapEntity(pair.Key, entry);
                }
            }
            Profiler.EndSample();
        }
        Profiler.EndSample();

        Profiler.EndSample();
    }

    private void BuildCampMinimapEntries(EntityCamp camp)
    {
        s_minimapEntries.Clear();

        // 先收集本阵营成员的视野源（只扫一遍，O(n)）；之后每个实体只需与"成员数 k"比较，
        // 而不是与"实体总数 n"比较 —— 消除原本 IsInCampVision 对每个敌方实体再全量扫 n 的 O(n²)。
        Profiler.BeginSample(kMinimapCollectTag);
        CollectCampVisionSources(camp);
        Profiler.EndSample();

        Profiler.BeginSample(kMinimapCampScanTag);
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null) continue;
            if (entity.camp == camp)
            {
                AppendMinimapEntry(entity);
                continue;
            }
            if (IsHiddenFromEnemyMinimap(entity)) continue; // 黑夜标记：敌方小地图不显示
            if (IsMarkedOnMinimap(entity) || IsInCachedCampVision(entity.transform.position)) AppendMinimapEntry(entity);
        }
        Profiler.EndSample();
    }

    private void CollectCampVisionSources(EntityCamp camp)
    {
        s_campVisionSources.Clear();
        // 视野源 = 该阵营所有成员的视野锚点（含死亡冻结的：阵亡玩家仍为阵营提供死前位置的视野）
        foreach (var pair in PlayerCamps)
        {
            if (!IsMemberOfCamp(pair.Value, camp)) continue; // 观战方玩家不贡献阵营视野源
            if (!visionStates.TryGetValue(pair.Key, out var state)) continue;
            s_campVisionSources.Add(new CampVisionSource
            {
                x = state.position.x,
                z = state.position.z,
                radiusSq = state.viewRadius * state.viewRadius,
            });
        }
    }

    private static bool IsInCachedCampVision(Vector3 target)
    {
        for (int i = 0; i < s_campVisionSources.Count; i++)
        {
            var s = s_campVisionSources[i];
            float dx = s.x - target.x;
            float dz = s.z - target.z;
            if (dx * dx + dz * dz <= s.radiusSq) return true;
        }
        return false;
    }

    private static void AppendMinimapEntry(EntityData entity)
    {
        Profiler.BeginSample(kMinimapAppendTag);
        var pos = entity.transform.position;
        s_minimapEntries.Add(new SCMinimapEntity
        {
            entityId = entity.id,
            type = entity.type,
            camp = entity.camp,
            posX = pos.x,
            posZ = pos.z,
            marked = IsMarkedOnMinimap(entity),
        });
        Profiler.EndSample();
    }

    private void PruneVisibilityCache()
    {
        s_clientScratch.Clear();
        foreach (var pair in visibleByClient)
        {
            if (!PlayerInfoList.ContainsKey(pair.Key)) s_clientScratch.Add(pair.Key);
        }
        foreach (var clientId in s_clientScratch)
        {
            visibleByClient.Remove(clientId);
            minimapRadiusByClient.Remove(clientId);
            ExitSpectate(clientId); // 兜底：从目标的观战列表摘除
            visionStates.Remove(clientId);
        }
    }

    //F 键切换雷达显示半径：在档位间循环并回应新值
    public void SwitchMinimapRadius(short clientId)
    {
        var radii = ConfigManager.GlobalVariableConfig.minimap_view_radius;
        if (radii == null || radii.Count == 0)
        {
            Debug.LogError("[配表] global_variables.csv 的 minimap_view_radius 至少要配一档半径");
            return;
        }
        float current = GetMinimapRadius(clientId);
        int index = -1;
        for (int i = 0; i < radii.Count; i++)
        {
            if (Mathf.Approximately(radii[i], current)) { index = i; break; }
        }
        float next = radii[(index + 1) % radii.Count]; //未记录时 index=-1，落到第一档
        minimapRadiusByClient[clientId] = next;
        Tool.NetworkManager.SendMinimapRadius(clientId, next);
    }

    private float GetMinimapRadius(short clientId)
    {
        if (minimapRadiusByClient.TryGetValue(clientId, out var r)) return r;

        var radii = ConfigManager.GlobalVariableConfig.minimap_view_radius;
        if (radii == null || radii.Count == 0)
        {
            Debug.LogError("[配表] global_variables.csv 的 minimap_view_radius 至少要配一档半径");
            return 0f;
        }
        return radii[0];
    }
    #endregion

    #region 小地图失联（昼夜事件驱动）
    private void InitVision()
    {
        EnvironmentManager.DayNightFlipped -= OnDayNightFlippedVision;
        EnvironmentManager.DayNightFlipped += OnDayNightFlippedVision;
    }

    private void OnDayNightFlippedVision(bool isDay)
    {
        if (!AtServer || !BattleStarted) return;

        // 昼夜翻转播报（事件列表 icon=天黑/天亮）
        Tool.NetworkManager.SendBattleEvent(new SCBattleEvent()
        {
            type = isDay ? SCBattleEvent.Type.Daybreak : SCBattleEvent.Type.Nightfall,
            textId = isDay ? 22 : 21,
        });

        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null || entity.camp != EntityCamp.Attack) continue;
            SetMinimapLost(entity, !isDay);
        }
    }

    private void ApplyMinimapLostOnSpawn(EntityData entity)
    {
        if (!BattleStarted || EnvironmentManager.IsDay) return;
        if (entity == null || entity.camp != EntityCamp.Attack) return;
        SetMinimapLost(entity, true);
    }

    private static void SetMinimapLost(EntityData entity, bool lost)
    {
        if (entity == null) return;
        var effect = entity.effectController;
        if (effect == null) return;
        if (!lost)
        {
            effect.RemoveEffect(EffectType.MinimapLost);
            return;
        }
        if (!effect.HasEffect(EffectType.MinimapLost))
        {
            effect.AddEffect(EffectType.MinimapLost, 1, Config.minimap_lost_duration);
        }
    }
    #endregion

    #region//Local
    // 玩家阵营（三分类）与实体阵营（二分类）匹配：观战方不匹配任何实体阵营
    private static bool IsMemberOfCamp(PlayerCampType campType, EntityCamp entityCamp)
    {
        return campType == PlayerCampType.Attack && entityCamp == EntityCamp.Attack
            || campType == PlayerCampType.Defense && entityCamp == EntityCamp.Defense;
    }

    private static bool IsInRadius(Vector3 from, Vector3 to, float radius)
    {
        float dx = from.x - to.x;
        float dz = from.z - to.z;
        return dx * dx + dz * dz <= radius * radius;
    }
    #endregion
}
