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

    private static readonly EntityCamp[] s_camps = { EntityCamp.Attack, EntityCamp.Defense };
    private static readonly List<SCMinimapEntity.MinimapEntity> s_minimapEntries = new();
    private static readonly HashSet<ushort> s_visibleScratch = new();
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

    private static bool CanSeeModel(EntityData viewer, EntityData target)
    {
        if (viewer == null || target == null) return false;
        if (target == viewer || target.camp == viewer.camp) return true;
        return IsInRadius(viewer.transform.position, target.transform.position, VisionRadius(viewer));
    }

    public void SendBattleEventToViewers(EntityData target, SCBattleEvent e)
    {
        foreach (var clientId in PlayerInfoList.Keys)
        {
            var viewer = GetEntityOfClient(clientId);
            if (viewer == null || !CanSeeModel(viewer, target)) continue;
            Tool.NetworkManager.SendBattleEvent(clientId, e);
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

    private static void MarkClientVisible(ushort entityId) => s_visibleScratch.Add(entityId);

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
        if (entity == null || entity.floatingAttribute == null) return 0f;
        return Mathf.Max(0f, entity.floatingAttribute.viewDistance);
    }

    private static bool IsMarkedOnMinimap(EntityData entity)
    {
        return entity != null && entity.effectController != null && entity.effectController.HasEffect(EffectType.EyeMark);
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
                if (!PlayerCamp.TryGetValue(pair.Key, out var memberCamp) || memberCamp != camp) continue;
                if (lost)
                {
                    // 阵营小地图失效：仅发一个清空标记（entity 为 null）
                    Tool.NetworkManager.SendMinimapEntity(pair.Key, new SCMinimapEntity { minimapLost = true });
                    continue;
                }
                // 每 tick 起始标记：客户端清空上一 tick 点位后逐个累积；随后每个实体独立成包（无片段号、不拼回）
                Tool.NetworkManager.SendMinimapEntity(pair.Key, new SCMinimapEntity { clear = true });
                foreach (var entry in s_minimapEntries)
                {
                    Tool.NetworkManager.SendMinimapEntity(pair.Key, new SCMinimapEntity { entity = entry });
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
            if (IsMarkedOnMinimap(entity) || IsInCachedCampVision(entity.transform.position)) AppendMinimapEntry(entity);
        }
        Profiler.EndSample();
    }

    private static void CollectCampVisionSources(EntityCamp camp)
    {
        s_campVisionSources.Clear();
        foreach (var member in EntityContainer.Entities)
        {
            if (member == null || member.camp != camp) continue;
            float r = VisionRadius(member);
            var p = member.transform.position;
            s_campVisionSources.Add(new CampVisionSource { x = p.x, z = p.z, radiusSq = r * r });
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
        s_minimapEntries.Add(new SCMinimapEntity.MinimapEntity()
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
        foreach (var clientId in s_clientScratch) visibleByClient.Remove(clientId);
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
            effect.AddEffect(EffectType.MinimapLost, 1, Config.minimap_lost_duration, negative: true);
        }
    }
    #endregion

    #region//Local
    private static bool IsInRadius(Vector3 from, Vector3 to, float radius)
    {
        float dx = from.x - to.x;
        float dz = from.z - to.z;
        return dx * dx + dz * dz <= radius * radius;
    }
    #endregion
}
