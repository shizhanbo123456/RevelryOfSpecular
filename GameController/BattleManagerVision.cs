using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 视野系统（partial BattleManager，策划案第十五章）。
/// 两层可见性全部在服务器计算，客户端只做表现，且**共用同一套数值** —— 角色属性
/// EntityAttribute.viewDistance（策划案 15 章：可见距离决定敌方模型可见性与小地图显示）：
///   ① 世界可见（模型）：己方单位恒可见；敌方 / 中立单位仅当距离 ≤ 观察者可见距离时同步给该玩家。
///   ② 小地图可见（**阵营共享视野**）：观察者换成**本阵营全体成员** —— 任一成员看得见就等于全阵营看得见
///      （己方单位恒显示；被「白眼标记」的敌方单位无条件强制显示）。
/// 可见距离是角色属性，「无限视野」等属性类 Buff 提升后两层同时生效（该技能 = +99999，即视野覆盖全图）。
/// 昼夜的第三条全局影响 = 夜间进攻方全体「小地图失联」（策划案 11.3/十六章，也就是 15 章所称的
/// 「夜间小地图显示阈值变短」）：收不到来自队友的视野，**不改变自身可见距离**，模型视野照常。
/// 施加由昼夜翻转事件驱动（见 InitVision），夜间出生 / 复活的新单位在 SpawnEntity 时补齐。
/// 失去视野时主动下发移除，客户端不必等 3s 超时兜底。
/// </summary>
public partial class BattleManager
{
    /// <summary>小地图下发节流计时（秒）。</summary>
    private float minimapTimer;

    /// <summary>各客户端上一次可见的实体集合（用于「刚刚离开视野 → 下发移除」）。</summary>
    private readonly Dictionary<short, HashSet<ushort>> visibleByClient = new();

    private static readonly EntityCamp[] s_camps = { EntityCamp.Attack, EntityCamp.Defense };
    private static readonly List<SCMinimapInfo.MinimapEntity> s_minimapEntries = new();
    private static readonly HashSet<ushort> s_visibleScratch = new();
    private static readonly HashSet<ushort> s_removedScratch = new();
    private static readonly HashSet<short> s_clientScratch = new();

    #region 世界可见（模型同步过滤）
    /// <summary>该客户端的观察者实体（未出战 / 复活等待中返回 null）。</summary>
    private EntityData GetEntityOfClient(short clientId)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return null;
        return EntityContainer.Entities.TryGetObject(entityId, out var entity) ? entity : null;
    }

    /// <summary>该观察者能否看到目标的模型：自己与同阵营恒可见，其余按可见距离判定。</summary>
    private static bool CanSeeModel(EntityData viewer, EntityData target)
    {
        if (viewer == null || target == null) return false;
        if (target == viewer || target.camp == viewer.camp) return true;
        return IsInRadius(viewer.transform.position, target.transform.position, VisionRadius(viewer));
    }

    /// <summary>开始本客户端的可见集合统计（同步循环前调用）。</summary>
    private void BeginClientVisibility(short clientId)
    {
        if (!visibleByClient.TryGetValue(clientId, out var seen))
        {
            seen = new HashSet<ushort>();
            visibleByClient[clientId] = seen;
        }
        s_visibleScratch.Clear();
    }

    /// <summary>标记本帧可见（同步循环内调用）。</summary>
    private static void MarkClientVisible(ushort entityId) => s_visibleScratch.Add(entityId);

    /// <summary>结束本客户端的可见集合统计：上一帧可见、本帧不可见的实体下发移除（同步循环后调用）。</summary>
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
    /// <summary>可见距离（属性值，已含属性类 Buff 的修改量）：模型与小地图共用这一套数值。</summary>
    private static float VisionRadius(EntityData entity) =>
        entity?.floatingAttribute != null ? Mathf.Max(0f, entity.floatingAttribute.viewDistance) : 0f;

    /// <summary>是否被「白眼标记」（小地图强制显示）。</summary>
    private static bool IsMarkedOnMinimap(EntityData entity) =>
        entity?.effectController != null && entity.effectController.HasEffect(EffectType.EyeMark);

    /// <summary>
    /// 该阵营是否处于「小地图失联」（策划案 11.3 的 Buff：无法获得来自队友的小地图视野，
    /// 由昼夜系统在夜间自动添加给进攻方全体、黎明移除，见 OnDayNightFlippedVision）。
    /// </summary>
    private static bool IsCampMinimapLost(EntityCamp camp)
    {
        foreach (var member in EntityContainer.Entities)
        {
            if (member == null || member.camp != camp) continue;
            if (member.effectController != null && member.effectController.HasEffect(EffectType.MinimapLost)) return true;
        }
        return false;
    }

    /// <summary>小地图同步：按阵营构建共享视野并下发（失联阵营改为按客户端下发自身视野）。</summary>
    private void SyncMinimapToClients()
    {
        PruneVisibilityCache();
        foreach (var camp in s_camps)
        {
            bool lost = IsCampMinimapLost(camp);
            SCMinimapInfo shared = lost ? null : BuildCampMinimap(camp);
            foreach (var pair in PlayerInfoList)
            {
                if (!PlayerCamp.TryGetValue(pair.Key, out var memberCamp) || memberCamp != camp) continue;
                var info = lost ? BuildSelfMinimap(pair.Key) : shared;
                if (info != null) Tool.NetworkManager.SendMinimapInfo(pair.Key, info);
            }
        }
    }

    /// <summary>阵营共享视野：己方单位恒显示；敌方 / 中立单位在本阵营任一成员视野内、或被白眼标记时显示。</summary>
    private SCMinimapInfo BuildCampMinimap(EntityCamp camp)
    {
        s_minimapEntries.Clear();
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null) continue;
            if (entity.camp == camp)
            {
                AppendMinimapEntry(entity);
                continue;
            }
            if (IsMarkedOnMinimap(entity) || IsInCampVision(camp, entity)) AppendMinimapEntry(entity);
        }
        return BuildMinimapMessage(false);
    }

    /// <summary>「小地图失联」下的自身视野：只含自己、自身可见距离内的敌方 / 中立、以及被白眼标记的敌方。</summary>
    private SCMinimapInfo BuildSelfMinimap(short clientId)
    {
        var viewer = GetEntityOfClient(clientId);
        if (viewer == null) return null;
        s_minimapEntries.Clear();
        float radius = VisionRadius(viewer);
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null) continue;
            if (entity == viewer)
            {
                AppendMinimapEntry(entity);
                continue;
            }
            if (entity.camp == viewer.camp) continue; // 失联：队友不提供视野
            if (IsMarkedOnMinimap(entity) || IsInRadius(viewer.transform.position, entity.transform.position, radius))
            {
                AppendMinimapEntry(entity);
            }
        }
        return BuildMinimapMessage(true);
    }

    /// <summary>目标是否落在该阵营任一成员的可见距离内（团队共享视野）。</summary>
    private static bool IsInCampVision(EntityCamp camp, EntityData target)
    {
        foreach (var member in EntityContainer.Entities)
        {
            if (member == null || member.camp != camp) continue;
            if (IsInRadius(member.transform.position, target.transform.position, VisionRadius(member))) return true;
        }
        return false;
    }

    private static void AppendMinimapEntry(EntityData entity)
    {
        var pos = entity.transform.position;
        s_minimapEntries.Add(new SCMinimapInfo.MinimapEntity()
        {
            entityId = entity.id,
            type = entity.type,
            camp = entity.camp,
            posX = pos.x,
            posZ = pos.z,
            marked = IsMarkedOnMinimap(entity),
        });
    }

    private static SCMinimapInfo BuildMinimapMessage(bool lost)
    {
        var info = new SCMinimapInfo() { minimapLost = lost };
        info.entities.AddRange(s_minimapEntries);
        return info;
    }

    /// <summary>清理已离开房间的客户端缓存（可见集合随会话存在）。</summary>
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
    /// <summary>
    /// 订阅昼夜翻转（与防守方被动同一套钩子）。对局开始时调用（先退订再订阅，避免重复订阅）。
    /// </summary>
    private void InitVision()
    {
        EnvironmentManager.DayNightFlipped -= OnDayNightFlippedVision;
        EnvironmentManager.DayNightFlipped += OnDayNightFlippedVision;
    }

    /// <summary>
    /// 「小地图失联」的施加（策划案 11.3 / 十六章）：进入夜晚 → 给进攻方全体添加；进入白天 → 全部清除。
    /// 时长取 Config.minimap_lost_duration（极大值）：跨昼夜由这里的白天事件移除，不依赖到时。
    /// 它只影响小地图（不改变自身可见距离），模型视野照常。
    /// </summary>
    private void OnDayNightFlippedVision(bool isDay)
    {
        if (!AtServer || !BattleStarted) return;
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null || entity.camp != EntityCamp.Attack) continue;
            SetMinimapLost(entity, !isDay);
        }
    }

    /// <summary>
    /// 实体生成时按当前昼夜补齐：昼夜事件只在翻转那一刻遍历一次，
    /// 夜间出生 / 复活出的新进攻方单位赶不上本次夜晚的翻转，会在这一轮到天亮前漏掉失联。
    /// </summary>
    private void ApplyMinimapLostOnSpawn(EntityData entity)
    {
        if (!BattleStarted || EnvironmentManager.IsDay) return;
        if (entity == null || entity.camp != EntityCamp.Attack) return;
        SetMinimapLost(entity, true);
    }

    /// <summary>添加 / 移除「小地图失联」。</summary>
    private static void SetMinimapLost(EntityData entity, bool lost)
    {
        var effect = entity?.effectController;
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
    /// <summary>XZ 平面距离（俯视判定，忽略高度）。</summary>
    private static bool IsInRadius(Vector3 from, Vector3 to, float radius)
    {
        float dx = from.x - to.x;
        float dz = from.z - to.z;
        return dx * dx + dz * dz <= radius * radius;
    }
    #endregion
}
