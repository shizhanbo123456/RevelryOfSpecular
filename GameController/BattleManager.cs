using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 战斗管理器（服务器权威，客户端仅接收信息摘要做表现）。
/// 核心移动/战斗内容都在服务器完成计算（架构说明总体原则）。
/// 本类为框架：实体容器/玩家进出/输入接收/技能接收已就绪；具体战斗规则（移动、子弹、守护点减伤、分数、昼夜、复活）标 TODO。
/// </summary>
public partial class BattleManager : EnsBehaviour
{
    private void Awake()
    {
        Tool.BattleManager = this;
    }

    /// <summary>是否服务器（房主）。</summary>
    public static bool AtServer => EnsInstance.HasAuthority;

    /// <summary>本局进行中。</summary>
    public bool BattleStarted { get; private set; }

    /// <summary>本局剩余时间（秒）。</summary>
    public float BattleRemainTime { get; private set; }

    #region 玩家信息（clientId → 信息）
    /// <summary>客户端进场选角信息。</summary>
    public readonly Dictionary<short, CSPlayerInfo> PlayerInfoList = new();
    /// <summary>客户端分配阵营。</summary>
    public readonly Dictionary<short, EntityCamp> PlayerCamp = new();
    /// <summary>客户端 → 玩家实体 id。</summary>
    public readonly Dictionary<short, ushort> PlayerEntityId = new();
    /// <summary>实体 id → 所属客户端（-1 = 非玩家实体）。</summary>
    public readonly Dictionary<ushort, short> EntityOwnerClient = new();
    #endregion

    #region 实体容器（按分类，ChunkSearcher 区块加速）
    /// <summary>实体容器（按类别分桶）。</summary>
    public static class EntityContainer
    {
        /// <summary>全部实体。</summary>
        public static readonly ChunkSearcher<EntityData> Entities = new(d => d.transform.position);
        /// <summary>角色（玩家）。</summary>
        public static readonly ChunkSearcher<EntityData> Characters = new(d => d.transform.position);
        /// <summary>守护点。</summary>
        public static readonly ChunkSearcher<EntityData> Beacons = new(d => d.transform.position);
        /// <summary>可采集水晶。</summary>
        public static readonly ChunkSearcher<EntityData> Crystals = new(d => d.transform.position);
        /// <summary>防御塔。</summary>
        public static readonly ChunkSearcher<EntityData> Towers = new(d => d.transform.position);
        /// <summary>僵尸。</summary>
        public static readonly ChunkSearcher<EntityData> Zombies = new(d => d.transform.position);
        /// <summary>中立/其它。</summary>
        public static readonly ChunkSearcher<EntityData> Others = new(d => d.transform.position);

        public static void Clear()
        {
            Entities.Clear();
            Characters.Clear();
            Beacons.Clear();
            Crystals.Clear();
            Towers.Clear();
            Zombies.Clear();
            Others.Clear();
        }

        private static readonly HashSet<int> s_buffer = new();

        /// <summary>范围内最近敌方实体（敌方=与 entity 不同阵营）。</summary>
        public static EntityData GetNearestEnemy(EntityData entity, float radius)
        {
            if (entity == null) return null;
            return GetNearestInCamp(entity.transform.position, radius, OppositeCamp(entity.camp), entity.id);
        }

        /// <summary>范围内最近指定阵营实体（排除 excludeId）。</summary>
        public static EntityData GetNearestInCamp(Vector3 pos, float radius, EntityCamp camp, ushort excludeId = 0)
        {
            Entities.GetIdsInRange(pos, radius, s_buffer);
            EntityData target = null;
            float sqr = float.MaxValue;
            foreach (var id in s_buffer)
            {
                if (!Entities.TryGetObject(id, out var e)) continue;
                if (e.id == excludeId || e.camp != camp || !e.Alive) continue;
                float d = (e.transform.position - pos).sqrMagnitude;
                if (d < sqr)
                {
                    sqr = d;
                    target = e;
                }
            }
            s_buffer.Clear();
            return target;
        }

        /// <summary>范围内所有指定阵营实体（写外部集合）。</summary>
        public static void GetAllInCamp(Vector3 pos, float radius, EntityCamp camp, List<EntityData> outList)
        {
            outList.Clear();
            Entities.GetIdsInRange(pos, radius, s_buffer);
            foreach (var id in s_buffer)
            {
                if (!Entities.TryGetObject(id, out var e)) continue;
                if (e.camp == camp && e.Alive) outList.Add(e);
            }
            s_buffer.Clear();
        }

        private static EntityCamp OppositeCamp(EntityCamp camp)
        {
            if (camp == EntityCamp.Attack) return EntityCamp.Defense;
            if (camp == EntityCamp.Defense) return EntityCamp.Attack;
            return EntityCamp.Neutral;
        }
    }
    #endregion

    #region 实体生命周期
    private ushort nextEntityId = 1;

    /// <summary>生成实体（服务器），返回实体 id。</summary>
    public ushort SpawnEntity(EntityType type, int level, Vector3 pos, EntityCamp camp)
    {
        if (!Tool.InfoManager.TryGetTemplate(type, out var template) || template == null)
        {
            Debug.LogWarning($"缺少服务器模板：{type}，请先在 InfoManager 配置");
            return 0;
        }
        ushort id = AllocEntityId();
        var go = Instantiate(template, pos, Quaternion.identity);
        var data = go.GetComponent<EntityData>();
        if (data == null)
        {
            Debug.LogError($"模板 {template.name} 缺少 EntityData 组件");
            Destroy(go);
            return 0;
        }
        data.OnCreate(id, type, level, camp);
        AddToContainer(data);
        return id;
    }

    private ushort AllocEntityId()
    {
        // 服务器实体 id 自增，0 保留；回绕后跳过已占用（TODO 简单实现，上限后从 100 重新分配）
        do
        {
            nextEntityId++;
            if (nextEntityId == 0) nextEntityId = Config.entity_id_rollback_start;
        } while (EntityContainer.Entities.Contains(nextEntityId));
        return nextEntityId;
    }

    /// <summary>销毁实体（服务器）。</summary>
    public bool DestroyEntity(ushort id)
    {
        if (!EntityContainer.Entities.TryGetObject(id, out var data)) return false;
        RemoveFromContainer(data);
        data.OnDestroyed();
        Destroy(data.gameObject);
        return true;
    }

    private void AddToContainer(EntityData data)
    {
        EntityContainer.Entities.Add(data.id, data);
        switch (data.type.category)
        {
            case EntityCategory.Character_Attack:
            case EntityCategory.Character_Defense:
                EntityContainer.Characters.Add(data.id, data);
                break;
            case EntityCategory.Beacon:
                EntityContainer.Beacons.Add(data.id, data);
                break;
            case EntityCategory.Crystal:
                EntityContainer.Crystals.Add(data.id, data);
                break;
            case EntityCategory.Tower:
                EntityContainer.Towers.Add(data.id, data);
                break;
            case EntityCategory.Zombie:
            case EntityCategory.EliteZombie:
                EntityContainer.Zombies.Add(data.id, data);
                break;
            default:
                EntityContainer.Others.Add(data.id, data);
                break;
        }
    }

    private void RemoveFromContainer(EntityData data)
    {
        EntityContainer.Entities.Remove(data.id);
        EntityContainer.Characters.Remove(data.id);
        EntityContainer.Beacons.Remove(data.id);
        EntityContainer.Crystals.Remove(data.id);
        EntityContainer.Towers.Remove(data.id);
        EntityContainer.Zombies.Remove(data.id);
        EntityContainer.Others.Remove(data.id);
    }
    #endregion

    #region 玩家进出
    /// <summary>玩家进场（服务器，由 NetworkManager RPC 回调）。</summary>
    public void AddPlayer(short clientId, CSPlayerInfo info)
    {
        if (PlayerInfoList.ContainsKey(clientId)) return;
        PlayerInfoList[clientId] = info;

        // 阵营分配：按意向；防守方最多 1 人（TODO：匹配大厅已完成分配时直接采用）
        EntityCamp camp = ResolveCamp(clientId, info.campIntention);
        PlayerCamp[clientId] = camp;

        // 创建玩家实体（客户端具体位置/出生点 TODO）
        EntityType characterType = camp == EntityCamp.Attack
            ? new EntityType(EntityCategory.Character_Attack, info.type.value)
            : new EntityType(EntityCategory.Character_Defense, info.type.value);
        Vector3 spawnPos = camp == EntityCamp.Attack
            ? GetAttackSpawnPos(clientId)
            : Tool.InfoManager.DefenseSpawnPosition;
        ushort entityId = SpawnEntity(characterType, info.level, spawnPos, camp);
        PlayerEntityId[clientId] = entityId;
        EntityOwnerClient[entityId] = clientId;

        // 发送开局信息 + 技能运行时（TODO：初始技能列表=初始武器，待技能包与配置完善）
        var battleInfo = new SCBattleInfo()
        {
            playerEntityId = entityId,
            camp = camp,
            characterType = characterType,
            characterLevel = info.level,
            weaponSlotCount = Tool.InfoManager.GetAttributeInfo(characterType)?.baseAttribute.weaponSlotCount ?? Config.default_weapon_slot_count,
            dayNightPhase = EnvironmentManager.CurrentPhase,
            phaseTime = EnvironmentManager.PhaseTime,
        };
        Tool.NetworkManager.SendBattleInfo(clientId, battleInfo);
        Tool.NetworkManager.SendSkillRuntime(clientId, new SCSkillRuntimeInfo() { selectedIndex = -1 });
        Debug.Log($"玩家 {clientId} 进场：{characterType} camp={camp}");
    }

    /// <summary>玩家退场（服务器）。</summary>
    public void RemovePlayer(short clientId)
    {
        if (PlayerEntityId.TryGetValue(clientId, out var entityId))
        {
            DestroyEntity(entityId);
            PlayerEntityId.Remove(clientId);
        }
        PlayerInfoList.Remove(clientId);
        PlayerCamp.Remove(clientId);
        Debug.Log($"玩家 {clientId} 退场");
    }

    private EntityCamp ResolveCamp(short clientId, int intention)
    {
        if (intention == 1) return EntityCamp.Defense;
        if (intention == 0) return EntityCamp.Attack;
        // 意向任意：检查防守方是否已满
        foreach (var pair in PlayerCamp)
        {
            if (pair.Value == EntityCamp.Defense) return EntityCamp.Attack;
        }
        return EntityCamp.Attack; // TODO: 防守方自愿分配规则（近期担任次数）待匹配系统实现
    }

    private Vector3 GetAttackSpawnPos(short clientId)
    {
        var list = Tool.InfoManager.AttackSpawnPositions;
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        return list[Mathf.Abs(clientId) % list.Count];
    }
    #endregion

    #region 输入与技能接收（服务器）
    /// <summary>接收客户端输入命令（服务器，由 NetworkManager RPC 回调）。</summary>
    public void ReceiveInputCommand(short clientId, CSInputCommand command)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;

        // TODO: 服务器权威移动/近战/滑铲/滚轮选技能/朝向在此计算
        entity.transform.rotation = Quaternion.Euler(0f, command.yaw, 0f);
        if (command.skillScrollDelta != 0 && entity.skillController != null)
        {
            entity.skillController.ScrollSelect(command.skillScrollDelta);
            Tool.NetworkManager.SendSkillRuntime(clientId, entity.skillController.GetRuntimeInfo());
        }
    }

    /// <summary>接收客户端技能释放请求（服务器，由 NetworkManager RPC 回调）。</summary>
    public void ReceiveUseSkill(short clientId, CSUseSkillRequest request)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;
        if (entity.skillController == null) return;

        // 右键仅对远程/施法类技能有效；选中非远程时阻断（服务器校验）
        if (!SkillManager.IsRanged(request.skillId))
        {
            Tool.NetworkManager.SendBattleEvent(clientId, new SCBattleEvent()
            {
                type = SCBattleEvent.Type.ShowText,
                sourceId = entityId,
                value = 12,
            });
            return;
        }
        entity.skillController.TryUseSkill(request.skillId, request.dest);
        Tool.NetworkManager.SendSkillRuntime(clientId, entity.skillController.GetRuntimeInfo());
    }
    #endregion

    #region 子弹（统一攻击实体）
    /// <summary>
    /// 发射子弹（服务器伤害侧；客户端经表现侧同步）。
    /// TODO：BulletContainer 完整实现（位置推进/命中检测/伤害结算）待后续完善。
    /// </summary>
    public void ShootBullet(EntityData shooter, float rate, BezierCurve curve, float radius, float lifeTime,
        Damageable.IDamageable damageable, System.Action<System.Action<EntityEffectController.EffectType, int, float>> addEffectEvent)
    {
        // TODO: 生成 Bullet 记录入 BulletContainer，ManagedUpdate 中推进与命中检测
    }
    #endregion

    #region 战斗规则（TODO：完整实现）
    /// <summary>开局（服务器）。</summary>
    public void StartBattle()
    {
        if (BattleStarted) return;
        BattleStarted = true;
        BattleRemainTime = Config.battle_duration;
        if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.ResetDayNight();
        // TODO: 生成守护点×4 / 水晶 / 防御塔 / 瘟疫树 / 僵尸刷新生效 / 全局参数被动一次性计算
        Debug.Log("战斗开始");
    }

    /// <summary>结束对局（服务器，gameState 见 SCScoreInfo）。</summary>
    public void EndBattle(int gameState)
    {
        if (!BattleStarted) return;
        BattleStarted = false;
        Debug.Log($"对局结束：{gameState}");
        foreach (var clientId in PlayerInfoList.Keys)
        {
            Tool.NetworkManager.SendScoreInfo(clientId, new SCScoreInfo() { gameState = gameState });
        }
    }
    #endregion

    #region 帧循环（服务器权威推进）
    public override void ManagedUpdate()
    {
        if (!AtServer || !BattleStarted) return;

        // 计时
        if (BattleRemainTime > 0f)
        {
            BattleRemainTime -= UnityEngine.Time.deltaTime;
            if (BattleRemainTime <= 0f)
            {
                // TODO: 时间耗尽按分数结算（分数制）
                EndBattle(1);
                return;
            }
        }

        // 昼夜推进（服务器权威，见策划案 13 章）
        if (Tool.EnvironmentManager != null)
        {
            Tool.EnvironmentManager.TickPhase(UnityEngine.Time.deltaTime);
            if (EnvironmentManager.PhaseTime >= EnvironmentManager.GetPhaseDuration(EnvironmentManager.CurrentPhase))
            {
                int next = (EnvironmentManager.CurrentPhase + 1) % EnvironmentManager.PhaseCount;
                Tool.EnvironmentManager.SetPhase(next);
                Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.DayNight, 0, (byte)next);
            }
        }

        // 实体更新
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity != null) entity.OnUpdate();
        }

        // 处理本帧死亡实体
        if (EntityData.KilledList.Count > 0)
        {
            var killed = new List<EntityData>(EntityData.KilledList);
            foreach (var entity in killed)
            {
                entity.OnKilled();
                // TODO: 击杀事件/分数/掉落/复活进度开始
                Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.Kill, entity.id);
                if (EntityOwnerClient.TryGetValue(entity.id, out var owner))
                {
                    Tool.NetworkManager.SendReviveInfo(owner, new SCReviveInfo() { entityId = entity.id, deadCount = 1 });
                }
            }
            EntityData.ClearKilled();
        }

        // 同步实体表现给客户端（TODO: 按可见距离过滤、节流）
        SyncEntitiesToClients();
    }

    private void SyncEntitiesToClients()
    {
        foreach (var clientId in PlayerInfoList.Keys)
        {
            foreach (var entity in EntityContainer.Entities)
            {
                if (entity == null) continue;
                // TODO: 可见距离/阵营视野过滤（12 章视野系统）
                Tool.NetworkManager.SendEntityDisplay(clientId, entity.GetDisplayInfo());
            }
        }
    }
    #endregion
}
