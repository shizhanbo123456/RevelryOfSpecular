using System.Collections.Generic;
using System.Linq;
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

    private float syncTimer;
    private float detailsTimer;
    private float aiTimer;

    private float syncTimer;
    private float detailsTimer;
    private float aiTimer;

    #region 玩家进出与组队大厅
    /// <summary>客户端进场选角信息。</summary>
    public readonly Dictionary<short, CSPlayerInfo> PlayerInfoList = new();
    /// <summary>客户端分配阵营（组队大厅中选择）。</summary>
    public readonly Dictionary<short, EntityCamp> PlayerCamp = new();
    /// <summary>客户端 → 玩家实体 id（对局开始后才有值）。</summary>
    public readonly Dictionary<short, ushort> PlayerEntityId = new();
    /// <summary>实体 id → 所属客户端（-1 = 非玩家实体）。</summary>
    public readonly Dictionary<ushort, short> EntityOwnerClient = new();

    /// <summary>进攻方 AI 玩家数量（组队大厅中任意玩家可编辑，房间共享）。</summary>
    public int AttackAICount { get; private set; }
    /// <summary>防守方 AI 玩家数量。</summary>
    public int DefenseAICount { get; private set; }
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
        bool wasCoreBeacon = data.type.category == EntityCategory.Beacon && data.type == EntityType.CoreBeacon;
        Destroy(data.gameObject);
        if (wasCoreBeacon && BattleStarted)
        {
            EndBattle(1); // 中心守护点被摧毁 → 进攻方必然获胜（策划案 17.2）
        }
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
    /// <summary>玩家进场（服务器，由 NetworkManager RPC 回调）：组队阶段只登记信息，不创建实体。</summary>
    public void AddPlayer(short clientId, CSPlayerInfo info)
    {
        if (PlayerInfoList.ContainsKey(clientId)) return;
        PlayerInfoList[clientId] = info;
        BroadcastRoomInfo();
        Debug.Log($"玩家 {clientId} 进入组队大厅");
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
        if (!BattleStarted) BroadcastRoomInfo();
        Debug.Log($"玩家 {clientId} 退场");
    }

    /// <summary>接收组队大厅状态更新（服务器，由 NetworkManager RPC 回调）。</summary>
    public void ReceiveRoomUpdate(short clientId, CSRoomUpdate update)
    {
        if (update == null || BattleStarted || !PlayerInfoList.ContainsKey(clientId)) return;

        // 选队（0 进攻 / 1 防守；-1 = 未选择/取消）
        if (update.camp == 0) PlayerCamp[clientId] = EntityCamp.Attack;
        else if (update.camp == 1) PlayerCamp[clientId] = EntityCamp.Defense;
        else PlayerCamp.Remove(clientId);

        // AI 数量：房间共享，任意玩家可编辑，数量不限制
        AttackAICount = Mathf.Max(0, update.attackAICount);
        DefenseAICount = Mathf.Max(0, update.defenseAICount);

        BroadcastRoomInfo();
    }

    /// <summary>接收开始对局请求（服务器，由 NetworkManager RPC 回调）。</summary>
    public void ReceiveStartRequest(short clientId, CSStartRequest request)
    {
        if (request == null || BattleStarted) return;

        // 校验：所有玩家已选队伍，且双方人数（人类 + AI）均 > 0
        foreach (var pair in PlayerInfoList)
        {
            if (!PlayerCamp.ContainsKey(pair.Key))
            {
                Tool.NetworkManager.SendBattleEvent(clientId, new SCBattleEvent()
                {
                    type = SCBattleEvent.Type.ShowText,
                    sourceId = pair.Key,
                    value = 18, // 尚有玩家未选择队伍
                });
                return;
            }
        }
        if (PlayerCamp.Values.Count(c => c == EntityCamp.Attack) + AttackAICount <= 0 ||
            PlayerCamp.Values.Count(c => c == EntityCamp.Defense) + DefenseAICount <= 0)
        {
            Tool.NetworkManager.SendBattleEvent(clientId, new SCBattleEvent()
            {
                type = SCBattleEvent.Type.ShowText,
                sourceId = 0,
                value = 17, // 双方人数均需 > 0
            });
            return;
        }

        StartBattle();
    }

    /// <summary>组装并广播当前房间状态。</summary>
    private void BroadcastRoomInfo()
    {
        var info = new SCRoomInfo()
        {
            attackAICount = AttackAICount,
            defenseAICount = DefenseAICount,
            battleStarted = BattleStarted,
        };
        foreach (var pair in PlayerInfoList)
        {
            info.members.Add(new SCRoomInfo.RoomMemberInfo()
            {
                clientId = pair.Key,
                camp = PlayerCamp.TryGetValue(pair.Key, out var camp) ? (camp == EntityCamp.Attack ? 0 : 1) : -1,
            });
        }
        Tool.NetworkManager.SendRoomInfo(info);
    }

    private Vector3 GetAttackSpawnPos(short clientId)
    {
        var list = Tool.InfoManager.AttackSpawnPositions;
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        return list[Mathf.Abs(clientId) % list.Count];
    }

    /// <summary>AI 玩家行为：有可用技能就攻击最近的敌方单位，否则站立（被攻击逃跑 TODO）。</summary>
    private void UpdateAI()
    {
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null || !entity.Alive) continue;
            if (EntityOwnerClient.ContainsKey(entity.id)) continue; // 跳过真人玩家
            if (entity.type.category != EntityCategory.Character_Attack &&
                entity.type.category != EntityCategory.Character_Defense) continue;
            if (entity.skillController == null) continue;

            var target = EntityContainer.GetNearestEnemy(entity);
            if (target == null) continue; // 无目标：站立

            foreach (var skillId in entity.skillController.GetSkillIds())
            {
                if (entity.skillController.GetCdRemain(skillId) > 0f) continue;
                if (entity.skillController.GetStore(skillId) == 0) continue;
                entity.skillController.TryUseSkill(skillId, target.transform.position);
                break;
            }
        }
    }

    /// <summary>守护点受到伤害（服务器，由 EntityData.OnDamaged 调用）：进攻方得分 = 对守护点造成的总伤害。</summary>
    public void AddBeaconDamage(float damage)
    {
        AttackScore += damage;
    }

    /// <summary>防守方得分 = 守护点剩余血量 × (1 + 0.1 × 击杀数)（策划案 17.2）。</summary>
    public float DefenseScore()
    {
        float remaining = 0f;
        foreach (var beacon in EntityContainer.Beacons)
        {
            if (beacon != null && beacon.floatingAttribute != null) remaining += beacon.floatingAttribute.health;
        }
        return remaining * (1f + Config.kill_score_factor * DefenseKills);
    }

    /// <summary>AI 玩家行为：有可用技能就攻击最近的敌方单位，否则站立（被攻击逃跑 TODO）。</summary>
    private void UpdateAI()
    {
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null || !entity.Alive) continue;
            if (EntityOwnerClient.ContainsKey(entity.id)) continue; // 跳过真人玩家
            if (entity.type.category != EntityCategory.Character_Attack &&
                entity.type.category != EntityCategory.Character_Defense) continue;
            if (entity.skillController == null) continue;

            var target = EntityContainer.GetNearestEnemy(entity);
            if (target == null) continue; // 无目标：站立

            foreach (var skillId in entity.skillController.GetSkillIds())
            {
                if (entity.skillController.GetCdRemain(skillId) > 0f) continue;
                if (entity.skillController.GetStore(skillId) == 0) continue;
                entity.skillController.TryUseSkill(skillId, target.transform.position);
                break;
            }
        }
    }

    /// <summary>守护点受到伤害（服务器，由 EntityData.OnDamaged 调用）：进攻方得分 = 对守护点造成的总伤害。</summary>
    public void AddBeaconDamage(float damage)
    {
        AttackScore += damage;
    }

    /// <summary>防守方得分 = 守护点剩余血量 × (1 + 0.1 × 击杀数)（策划案 17.2）。</summary>
    public float DefenseScore()
    {
        float remaining = 0f;
        foreach (var beacon in EntityContainer.Beacons)
        {
            if (beacon != null && beacon.floatingAttribute != null) remaining += beacon.floatingAttribute.health;
        }
        return remaining * (1f + Config.kill_score_factor * DefenseKills);
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
        // TODO: 服务器权威移动/近战/滑铲/跳跃在此计算（位移由动画状态机根运动驱动）
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
        // CD/库存变化随实体表现摘要（skills 列表）同步，无需单独通道
    }
    #endregion

    #region 子弹（统一攻击实体）
    /// <summary>
    /// 发射子弹（服务器伤害侧；客户端经表现侧同步）。
    /// attack 为攻击数据（近战与子弹共用，含破霸体等，见策划案 12.1）；
    /// trajectory 为弹道轨迹（BulletTrajectory 基类，各实现见 Bullet 文件夹）。
    /// TODO：BulletContainer 完整实现（位置推进/命中检测/伤害结算）待后续完善。
    /// </summary>
    public void ShootBullet(EntityData shooter, AttackData attack, BulletTrajectory trajectory, float lifeTime)
    {
        // TODO: 生成 Bullet{ attack, trajectory, lifeTime, spawnTime = Time.time } 记录入 BulletContainer，ManagedUpdate 中推进与命中检测
    }
    #endregion

    #region 战斗规则（TODO：完整实现）
    /// <summary>开局（服务器）：生成全部玩家/AI 实体，下发开局信息。</summary>
    public void StartBattle()
    {
        if (BattleStarted) return;
        BattleStarted = true;
        BattleRemainTime = Config.battle_duration;
        if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.ResetDayNight();

        // 玩家实体：按组队大厅中选择的阵营取 CSPlayerInfo 对应一侧角色
        foreach (var pair in PlayerInfoList)
        {
            short clientId = pair.Key;
            if (!PlayerCamp.TryGetValue(clientId, out var camp)) continue;
            bool isAttack = camp == EntityCamp.Attack;
            EntityType characterType = isAttack ? pair.Value.attackCharacter : pair.Value.defenseCharacter;
            int level = isAttack ? pair.Value.attackLevel : pair.Value.defenseLevel;
            Vector3 spawnPos = isAttack ? GetAttackSpawnPos(clientId) : Tool.InfoManager.DefenseSpawnPosition;

            ushort entityId = SpawnEntity(characterType, level, spawnPos, camp);
            PlayerEntityId[clientId] = entityId;
            EntityOwnerClient[entityId] = clientId;

            Tool.NetworkManager.SendBattleInfo(clientId, new SCBattleInfo()
            {
                playerEntityId = entityId,
                camp = camp,
                characterType = characterType,
                characterLevel = level,
                dayNightPhase = EnvironmentManager.CurrentPhase,
                phaseTime = EnvironmentManager.PhaseTime,
            });
            Debug.Log($"玩家 {clientId} 出战：{characterType} camp={camp}");
        }

        // AI 玩家实体：与真人判定完全一致（策划案 17.1），暂用各队 0 号角色（TODO：AI 角色配置）
        for (int i = 0; i < AttackAICount; i++)
        {
            SpawnEntity(EntityType.Attack(0), 1, GetAttackSpawnPos((short)(-100 - i)), EntityCamp.Attack);
        }
        for (int i = 0; i < DefenseAICount; i++)
        {
            SpawnEntity(EntityType.Defense(0), 1, Tool.InfoManager.DefenseSpawnPosition, EntityCamp.Defense);
        }

        BroadcastRoomInfo();
        // TODO: 生成守护点×4 / 水晶 / 防御塔 / 瘟疫树 / 僵尸刷新生效 / 全局参数被动一次性计算
        Debug.Log($"战斗开始：人类 {PlayerInfoList.Count}，AI {AttackAICount + DefenseAICount}");
    }

    /// <summary>结束对局（服务器，gameState 见 SCScoreInfo）。</summary>
    public void EndBattle(int gameState)
    {
        if (!BattleStarted) return;
        BattleStarted = false;
        Debug.Log($"对局结束：{gameState}");
        foreach (var clientId in PlayerInfoList.Keys)
        {
            Tool.NetworkManager.SendScoreInfo(clientId, new SCScoreInfo()
            {
                gameState = gameState,
                attackScore = AttackScore,
                defenseScore = DefenseScore(),
                killScore = DefenseKills,
                remainTime = Mathf.Max(0f, BattleRemainTime),
            });
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
                // 时间耗尽按分数结算（分数制，不单独处理平局）
                EndBattle(AttackScore >= DefenseScore() ? 1 : 2);
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
                if (entity.camp == EntityCamp.Attack) DefenseKills++; // 防守方击杀数（得分公式用）
                // TODO: 掉落/复活进度开始（死亡即摧毁单位，复活时重建并回满）
                Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.Kill, entity.id);
                if (EntityOwnerClient.TryGetValue(entity.id, out var owner))
                {
                    Tool.NetworkManager.SendReviveInfo(owner, new SCReviveInfo() { entityId = entity.id, deadCount = 1 });
                }
            }
            EntityData.ClearKilled();
        }

        // AI 玩家行为（有可用技能攻击最近单位，否则站立；被攻击逃跑 TODO）
        aiTimer -= UnityEngine.Time.deltaTime;
        if (aiTimer <= 0f)
        {
            aiTimer = 0.5f;
            UpdateAI();
        }

        // 同步实体表现给客户端（0.02s 节流；详细数据 0.2s；可见距离过滤 TODO）
        syncTimer -= UnityEngine.Time.deltaTime;
        detailsTimer -= UnityEngine.Time.deltaTime;
        if (syncTimer <= 0f)
        {
            syncTimer = Config.entity_sync_interval_fast;
            bool details = detailsTimer <= 0f;
            if (details) detailsTimer = Config.entity_sync_interval_details;
            SyncEntitiesToClients(details);
        }
    }

    private void SyncEntitiesToClients(bool includeRuntime)
    {
        foreach (var clientId in PlayerInfoList.Keys)
        {
            foreach (var entity in EntityContainer.Entities)
            {
                if (entity == null) continue;
                // TODO: 可见距离/阵营视野过滤（15 章视野系统）
                var info = entity.GetDisplayInfo();
                info.includeRuntime = includeRuntime;
                Tool.NetworkManager.SendEntityDisplay(clientId, info);
            }
        }
    }
    #endregion
}
