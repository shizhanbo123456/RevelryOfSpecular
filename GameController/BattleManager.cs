using System.Collections.Generic;
using System.Linq;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 战斗管理器（服务器权威，客户端仅接收信息摘要做表现）。
/// 核心移动/战斗内容都在服务器完成计算（架构说明总体原则）。
/// 战斗核心已就绪：权威移动/子弹容器/近战/对局实体生成/死亡复活/计分/僵尸刷新；瘟疫树/防御塔/僵尸/AI 的攻击与行动暂留空。
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

    /// <summary>进攻方得分 = 对守护点造成的总伤害（策划案 17.2）。</summary>
    public float AttackScore { get; private set; }
    /// <summary>防守方击杀数（击杀进攻方单位）。</summary>
    public int DefenseKills { get; private set; }

    private float syncTimer;
    private float detailsTimer;
    private float aiTimer;
    /// <summary>夜间僵尸刷新 cd 进度（满 1 刷新一只并清零，见 Config.zombie_refresh_*）。</summary>
    private float zombieRefreshProgress;

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

    /// <summary>对局中各客户端对水晶造成的累计伤害（结算经验用，策划案 17.3）。</summary>
    public readonly Dictionary<short, float> CrystalExpByClient = new();

    /// <summary>累计水晶伤害经验（EntityData.OnDamaged 调用；经验 = 对水晶造成的伤害量）。</summary>
    public void AddCrystalExp(short clientId, float damage)
    {
        if (damage <= 0f) return;
        CrystalExpByClient.TryGetValue(clientId, out float exp);
        CrystalExpByClient[clientId] = exp + damage;
    }
    #endregion

    #region 实体容器（按分类，ChunkSearcher 区块加速）
    /// <summary>实体容器（按类别分桶）。</summary>
    public static class EntityContainer
    {
        /// <summary>全部实体。</summary>
        public static readonly ChunkSearcher<EntityData> Entities = new(d => d.transform.position);
        /// <summary>守护点。</summary>
        public static readonly ChunkSearcher<EntityData> Beacons = new(d => d.transform.position);
        /// <summary>可采集水晶。</summary>
        public static readonly ChunkSearcher<EntityData> Crystals = new(d => d.transform.position);
        /// <summary>防御塔。</summary>
        public static readonly ChunkSearcher<EntityData> Towers = new(d => d.transform.position);
        /// <summary>僵尸。</summary>
        public static readonly ChunkSearcher<EntityData> Zombies = new(d => d.transform.position);

        public static void Clear()
        {
            Entities.Clear();
            Beacons.Clear();
            Crystals.Clear();
            Towers.Clear();
            Zombies.Clear();
        }

        private static readonly HashSet<int> s_buffer = new();

        /// <summary>范围内最近敌方实体（敌方=与 entity 不同阵营）。</summary>
        public static EntityData GetNearestEnemy(EntityData entity, float radius = Config.default_skill_auto_target_radius)
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

    /// <summary>开始战斗时重置 id 源（每次开战从 1 重新分配）。</summary>
    private void ResetEntityIdSource() => nextEntityId = 1;

    private ushort AllocEntityId()
    {
        // 服务器实体 id 自增，0 保留；超过上限 30000 回绕到 1（跳过已占用）
        do
        {
            nextEntityId++;
            if (nextEntityId > Config.entity_id_max) nextEntityId = 1;
        } while (EntityContainer.Entities.Contains(nextEntityId));
        return nextEntityId;
    }

    /// <summary>按 id 取实体（不存在返回 null）。</summary>
    public static EntityData GetEntity(ushort id) =>
        EntityContainer.Entities.TryGetObject(id, out var e) ? e : null;

    /// <summary>销毁实体（服务器）：统一在此通知所有客户端移除表现。</summary>
    public bool DestroyEntity(ushort id)
    {
        if (!EntityContainer.Entities.TryGetObject(id, out var data)) return false;
        RemoveFromContainer(data);

        // 通知所有客户端移除该实体表现（死亡/离场/摧毁统一走此入口）
        foreach (var clientId in PlayerInfoList.Keys)
        {
            Tool.NetworkManager.SendRemoveEntity(clientId, id);
        }
        // 守护点被摧毁事件（UI 飘字/表现用）
        if (BattleStarted && data.type.category == EntityCategory.Beacon)
        {
            Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.BeaconDestroyed, id);
        }

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
        }
    }

    private void RemoveFromContainer(EntityData data)
    {
        EntityContainer.Entities.Remove(data.id);
        EntityContainer.Beacons.Remove(data.id);
        EntityContainer.Crystals.Remove(data.id);
        EntityContainer.Towers.Remove(data.id);
        EntityContainer.Zombies.Remove(data.id);
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
                    sourceId = (ushort)pair.Key,
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

    /// <summary>进攻方开局出生点（地形组件备选位置，按玩家序号轮流分配避免扎堆）。</summary>
    private Vector3 GetAttackSpawnPos(short clientId)
    {
        return LandscapeSpawns.IndexedOf(Tool.LandscapeSpawns.attackSpawnPositions, clientId);
    }

    /// <summary>防守方开局出生点（地形组件备选位置，按玩家序号轮流分配避免扎堆）。</summary>
    private Vector3 GetDefenseSpawnPos(short clientId)
    {
        return LandscapeSpawns.IndexedOf(Tool.LandscapeSpawns.defenseSpawnPositions, clientId);
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

    /// <summary>
    /// 重算中心守护点的分层减伤（策划案 9.2：每个存活外围守护点提供 25% 减伤）。
    /// 统一走 effectController 的 BeaconReduce 通道（Add/Remove 内部重算），外部不做 Buff 遍历。
    /// </summary>
    private void UpdateCoreBeaconReduce()
    {
        int aliveOuter = 0;
        foreach (var beacon in EntityContainer.Beacons)
        {
            if (beacon != null && beacon.Alive && beacon.type != EntityType.CoreBeacon) aliveOuter++;
        }
        foreach (var beacon in EntityContainer.Beacons)
        {
            if (beacon != null && beacon.type == EntityType.CoreBeacon)
            {
                beacon.effectController?.AddEffect(EffectType.BeaconReduce, aliveOuter, float.MaxValue);
            }
        }
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
    /// <summary>接收客户端输入命令（服务器，由 NetworkManager RPC 回调）：权威移动与动作触发见 BattleManagerCombat。</summary>
    public void ReceiveInputCommand(short clientId, CSInputCommand command)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;
        RecordInput(entity, command);
    }

    /// <summary>接收客户端技能释放请求（服务器，由 NetworkManager RPC 回调）：键盘槽位直触。</summary>
    public void ReceiveUseSkill(short clientId, CSUseSkillRequest request)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;
        if (entity.skillController == null) return;

        // 键盘槽位直触：选中下标 = 该技能所在槽位（供 UI 高亮），CD/库存/强控校验在 TryUseSkill 内
        int slot = entity.skillController.GetSkillIds().IndexOf(request.skillId);
        if (slot >= 0) entity.skillController.SelectIndex(slot);

        entity.skillController.TryUseSkill(request.skillId, request.dest);
        // CD/库存变化随实体表现摘要（skills 列表）同步，无需单独通道
    }
    #endregion

    #region 子弹（统一攻击实体）
    /// <summary>
    /// 发射子弹（服务器伤害侧；客户端经表现侧同步）。
    /// attack 为攻击数据（近战与子弹共用，含破霸体等，见策划案 12.1）；
    /// trajectory 为弹道轨迹（BulletTrajectory 基类，各实现见 Bullet 文件夹）。
    /// 实现见 BattleManagerCombat（时间戳推进：位置 = 轨迹 Lerp(经过时长/生命)）。
    /// </summary>
    public void ShootBullet(EntityData shooter, AttackData attack, BulletTrajectory trajectory, float lifeTime)
    {
        AddBullet(attack, trajectory, lifeTime);
    }
    #endregion

    #region 战斗规则（TODO：完整实现）
    /// <summary>开局（服务器）：生成全部玩家/AI 实体，下发开局信息。</summary>
    public void StartBattle()
    {
        if (BattleStarted) return;

        // 清场上局残留实体（结算期间实体保留展示；须在 BattleStarted 置位前清除，避免触发中心守护点结束判定）
        var stale = new List<EntityData>();
        foreach (var e in EntityContainer.Entities)
        {
            if (e != null) stale.Add(e);
        }
        foreach (var e in stale)
        {
            DestroyEntity(e.id);
        }

        BattleStarted = true;
        BattleRemainTime = Config.battle_duration;
        if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.ResetDayNight();

        // 开战重置：id 源置零、计分清零、子弹/移动/复活/重生状态清空
        ResetEntityIdSource();
        AttackScore = 0f;
        DefenseKills = 0;
        ClearBattleState();

        // 玩家实体：按组队大厅中选择的阵营取 CSPlayerInfo 对应一侧角色
        foreach (var pair in PlayerInfoList)
        {
            short clientId = pair.Key;
            if (!PlayerCamp.TryGetValue(clientId, out var camp)) continue;
            bool isAttack = camp == EntityCamp.Attack;
            EntityType characterType = isAttack ? pair.Value.attackCharacter : pair.Value.defenseCharacter;
            int level = isAttack ? pair.Value.attackLevel : pair.Value.defenseLevel;
            Vector3 spawnPos = isAttack ? GetAttackSpawnPos(clientId) : GetDefenseSpawnPos(clientId);

            ushort entityId = SpawnEntity(characterType, level, spawnPos, camp);
            PlayerEntityId[clientId] = entityId;
            EntityOwnerClient[entityId] = clientId;
            GetEntity(entityId)?.skillController?.SetSkillList(new List<int> { Config.initial_skill_id });

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
            ushort aiId = SpawnEntity(EntityType.Attack(0), 1, GetAttackSpawnPos((short)(-100 - i)), EntityCamp.Attack);
            GetEntity(aiId)?.skillController?.SetSkillList(new List<int> { Config.initial_skill_id });
        }
        for (int i = 0; i < DefenseAICount; i++)
        {
            ushort aiId = SpawnEntity(EntityType.Defense(0), 1, GetDefenseSpawnPos((short)(-200 - i)), EntityCamp.Defense);
            GetEntity(aiId)?.skillController?.SetSkillList(new List<int> { Config.initial_skill_id });
        }

        // 对局世界：守护点×4 / 水晶 / 防御塔 / 瘟疫树（位置来自地形组件 LandscapeSpawns）
        SpawnBattleWorld();
        UpdateCoreBeaconReduce(); // 初始分层减伤 = 存活外围数 × 25%

        // 昼夜同步（客户端收到后按配置时长与流速自行推演）
        Tool.NetworkManager.SendDayNightInfo(new SCDayNightInfo()
        {
            phase = EnvironmentManager.CurrentPhase,
            phaseTime = EnvironmentManager.PhaseTime,
            rate = 1f,
        });

        BroadcastRoomInfo();
        Debug.Log($"战斗开始：人类 {PlayerInfoList.Count}，AI {AttackAICount + DefenseAICount}");
    }

    /// <summary>结束对局（服务器，gameState 见 SCScoreInfo）：玩家回到组队状态，房间状态广播以便下一轮准备。</summary>
    public void EndBattle(int gameState)
    {
        if (!BattleStarted) return;
        BattleStarted = false;
        Debug.Log($"对局结束：{gameState}");
        foreach (var clientId in PlayerInfoList.Keys)
        {
            CrystalExpByClient.TryGetValue(clientId, out float crystalExp);
            Tool.NetworkManager.SendScoreInfo(clientId, new SCScoreInfo()
            {
                gameState = gameState,
                attackScore = AttackScore,
                defenseScore = DefenseScore(),
                killScore = DefenseKills,
                remainTime = Mathf.Max(0f, BattleRemainTime),
                expGain = Mathf.RoundToInt(crystalExp), // 经验 = 对水晶造成的伤害量（策划案 17.3）
            });
        }
        BroadcastRoomInfo(); // battleStarted = false：客户端结算页关闭后回组队大厅准备下一轮
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

        // 昼夜推进（服务器权威，见策划案 13 章；阶段切换下发同步包，客户端自行推演）
        if (Tool.EnvironmentManager != null)
        {
            Tool.EnvironmentManager.TickPhase(UnityEngine.Time.deltaTime);
            if (EnvironmentManager.PhaseTime >= EnvironmentManager.GetPhaseDuration(EnvironmentManager.CurrentPhase))
            {
                int next = (EnvironmentManager.CurrentPhase + 1) % EnvironmentManager.PhaseCount;
                Tool.EnvironmentManager.SetPhase(next);
                Tool.NetworkManager.SendDayNightInfo(new SCDayNightInfo()
                {
                    phase = EnvironmentManager.CurrentPhase,
                    phaseTime = EnvironmentManager.PhaseTime,
                    rate = 1f,
                });
            }
        }

        // 实体更新
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity != null) entity.OnUpdate();
        }

        // 战斗核心推进：权威移动（时间戳外推）/ 子弹容器 / 复活与水晶重生
        TickMovement();
        TickBullets();
        TickRevive();
        TickWorldRespawn();

        // 处理本帧死亡实体：摧毁单位（死亡即摧毁，复活时重建并回满，Buff 随之消失）
        if (EntityData.KilledList.Count > 0)
        {
            var killed = new List<EntityData>(EntityData.KilledList);
            foreach (var entity in killed)
            {
                entity.OnKilled();
                if (entity.camp == EntityCamp.Attack) DefenseKills++; // 防守方击杀数（得分公式用）
                HandleDeath(entity);
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

        // 夜间僵尸刷新（策划案第九章）：cd 进度满 1 → 刷新一只并清零；
        // 僵尸数量达上限时不刷新且进度清零；白天/黄昏/黎明进度不增加
        if (EnvironmentManager.CurrentPhase == 2) // 2 = 夜晚
        {
            int zombieCount = EntityContainer.Zombies.Count;
            if (zombieCount >= Config.zombie_max)
            {
                zombieRefreshProgress = 0f;
            }
            else
            {
                // 越少越快：0 只 → 0.5/s，接近上限 → 0.05/s（线性插值）
                float rate = Mathf.Lerp(Config.zombie_refresh_rate_fast, Config.zombie_refresh_rate_slow,
                    (float)zombieCount / Config.zombie_max);
                zombieRefreshProgress += UnityEngine.Time.deltaTime * rate;
                if (zombieRefreshProgress >= Config.zombie_refresh_progress_max)
                {
                    zombieRefreshProgress = 0f;
                    SpawnZombie(); // 出生点分散在道路/墓地/守护点外围（LandscapeSpawns），攻击/行动暂留空
                }
            }
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
                info.ownerClientId = EntityOwnerClient.TryGetValue(entity.id, out var oc) ? oc : (short)-1;
                FillDisplayVelocity(entity, info);
                Tool.NetworkManager.SendEntityDisplay(clientId, info);
            }
        }
    }

    /// <summary>填充表现推演数据：速度（权威移动 + 位移效果，世界系）与绕 Y 角速度（客户端包间推演用）。</summary>
    private void FillDisplayVelocity(EntityData entity, SCEntityDisplayInfo info)
    {
        if (moveStates.TryGetValue(entity.id, out var ms))
        {
            bool movingVisibly = ms.moving && !ms.blocked && entity.MotionCanMove;
            Vector3 worldDir = Quaternion.Euler(0f, ms.yaw, 0f) * new Vector3(ms.dir.x, 0f, ms.dir.y).normalized;
            info.velocity = (movingVisibly ? worldDir * entity.moveSpeed : Vector3.zero)
                            + entity.motionVelocity;
            info.yawSpeed = ms.yawSpeed;
        }
        else
        {
            info.velocity = entity.motionVelocity;
            info.yawSpeed = 0f;
        }
    }
    #endregion
}
