using System.Collections.Generic;
using System.Linq;
using Ros.Transport;
using UnityEngine;
using UnityEngine.Profiling;

public partial class BattleManager : EnsBehaviour
{
    private void Awake()
    {
        Tool.BattleManager = this;
    }

    public static bool AtServer => EnsInstance.HasAuthority;

    public bool BattleStarted { get; private set; }

    public float BattleRemainTime { get; private set; }

    public float AttackScore { get; private set; }
    public int DefenseKills { get; private set; }

    private float syncTimer;
    private float detailsTimer;
    private float dayNightSyncTimer;
    private float zombieRefreshProgress;
    private int zombieCount;

    #region 玩家进出与组队大厅
    public readonly Dictionary<short, CSPlayerInfo> PlayerInfoList = new();
    public readonly Dictionary<short, EntityCamp> PlayerCamp = new();
    public readonly Dictionary<short, ushort> PlayerEntityId = new();
    public readonly Dictionary<ushort, short> EntityOwnerClient = new();

    private const short AIClientIdStart = -1000;
    private short nextAIClientId = AIClientIdStart;
    public readonly HashSet<short> AIClients = new();

    public int AttackAICount { get; private set; }
    public int DefenseAICount { get; private set; }

    public readonly Dictionary<short, float> HarvestByClient = new();

    public void AddHarvest(short clientId, float damage)
    {
        if (damage <= 0f) return;
        HarvestByClient.TryGetValue(clientId, out float sum);
        HarvestByClient[clientId] = sum + damage;
    }

    public EntityData GetTopHarvester()
    {
        short bestClient = -1;
        float bestValue = 0f;
        foreach (var pair in HarvestByClient)
        {
            if (pair.Value <= bestValue) continue;
            if (!PlayerEntityId.TryGetValue(pair.Key, out var entityId)) continue;
            if (!EntityContainer.Entities.TryGetObject(entityId, out var e) || e == null) continue;
            if (e.camp != EntityCamp.Attack) continue;
            bestValue = pair.Value;
            bestClient = pair.Key;
        }
        return bestClient >= 0 && PlayerEntityId.TryGetValue(bestClient, out var playerId)
            ? GetEntity(playerId)
            : null;
    }
    #endregion

    #region 实体容器（按分类，ChunkSearcher 区块加速）
    public static class EntityContainer
    {
        public static readonly ChunkSearcher<EntityData> Entities = new(d => d.transform.position);
        public static readonly ChunkSearcher<EntityData> Beacons = new(d => d.transform.position);
        public static readonly ChunkSearcher<EntityData> Crystals = new(d => d.transform.position);
        public static readonly ChunkSearcher<EntityData> Towers = new(d => d.transform.position);

        public static void Clear()
        {
            Entities.Clear();
            Beacons.Clear();
            Crystals.Clear();
            Towers.Clear();
        }

        private static readonly HashSet<int> s_buffer = new();

        public static EntityData GetNearestEnemy(EntityData entity, float radius = Config.default_skill_auto_target_radius)
        {
            if (entity == null) return null;
            return GetNearestInCamp(entity.transform.position, radius, EntityCampUtil.HostileOf(entity.camp), entity.id);
        }

        // 仅索敌前方扇形内的敌人（水平夹角 <= halfAngleDeg）：用于技能自动索敌，避免锁到背后目标
        public static EntityData GetNearestEnemyInFront(EntityData entity, float radius, float halfAngleDeg)
        {
            if (entity == null) return null;
            Vector3 pos = entity.transform.position;
            Vector3 fwd = entity.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) return null;
            fwd.Normalize();
            float cosLimit = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            EntityCamp hostile = EntityCampUtil.HostileOf(entity.camp);
            Entities.GetIdsInRange(pos, radius, s_buffer);
            EntityData target = null;
            float bestSqr = float.MaxValue;
            foreach (var id in s_buffer)
            {
                if (!Entities.TryGetObject(id, out var e) || e == null) continue;
                if (e.id == entity.id || (hostile & e.camp) == 0 || !e.Alive) continue;
                Vector3 to = e.transform.position - pos;
                to.y = 0f;
                float dsqr = to.sqrMagnitude;
                if (dsqr > radius * radius || dsqr < 1e-6f) continue;
                if (Vector3.Dot(fwd, to.normalized) < cosLimit) continue;
                if (dsqr < bestSqr)
                {
                    bestSqr = dsqr;
                    target = e;
                }
            }
            s_buffer.Clear();
            return target;
        }

        public static EntityData GetNearestInCamp(Vector3 pos, float radius, EntityCamp camp, ushort excludeId = 0)
        {
            Entities.GetIdsInRange(pos, radius, s_buffer);
            EntityData target = null;
            float sqr = float.MaxValue;
            foreach (var id in s_buffer)
            {
                if (!Entities.TryGetObject(id, out var e)) continue;
                if (e.id == excludeId || (camp & e.camp) == 0 || !e.Alive) continue;
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

        public static void GetAllInCamp(Vector3 pos, float radius, EntityCamp camp, List<EntityData> outList)
        {
            outList.Clear();
            Entities.GetIdsInRange(pos, radius, s_buffer);
            foreach (var id in s_buffer)
            {
                if (!Entities.TryGetObject(id, out var e)) continue;
                if ((camp & e.camp) != 0 && e.Alive) outList.Add(e);
            }
            s_buffer.Clear();
        }
    }
    #endregion

    #region 实体生命周期
    private ushort nextEntityId = 1;

    public ushort SpawnEntity(EntityType type, int level, Vector3 pos, EntityCamp camp)
    {
        if (!Tool.InfoManager.TryGetTemplate(type, out var template) || template == null)
        {
            Debug.LogWarning($"缺少服务器模板：{type}，请先在 InfoManager 配置");
            return 0;
        }
        ushort id = AllocEntityId();
        var go = Instantiate(template, pos, Quaternion.identity);
        // EntityData 不挂预制体（模板与客户端图形是同一批预制体），生成时按类别补上对应子类
        var data = go.GetComponent<EntityData>();
        if (data == null) data = EntityData.AddTo(go, type.category);
        if (data == null)
        {
            Debug.LogError($"实体类别 {type.category} 没有对应的 EntityData 子类，无法生成");
            Destroy(go);
            return 0;
        }
        data.OnCreate(id, type, level, camp);
        // 初始技能表按实体类型统一赋：玩家角色与非玩家单位（僵尸/精英/防御塔/瘟疫树）同一条路径，
        // 未登记的类别得空表（见 Config.initial_skills）
        if (data.skillController != null) data.skillController.SetSkillList(Config.GetInitialSkills(type));
        ApplyMinimapLostOnSpawn(data); // 夜间出生/复活：补上「小地图失联」（昼夜事件只在翻转那一刻遍历）
        AddToContainer(data);
        return id;
    }

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

    public static EntityData GetEntity(ushort id) =>
        EntityContainer.Entities.TryGetObject(id, out var e) ? e : null;

    public bool DestroyEntity(ushort id)
    {
        if (!EntityContainer.Entities.TryGetObject(id, out var data)) return false;
        RemoveFromContainer(data);

        // 通知所有客户端移除该实体表现（死亡/离场/摧毁统一走此入口）
        foreach (var clientId in PlayerInfoList.Keys)
        {
            Tool.NetworkManager.SendRemoveEntity(clientId, id);
        }
        // 守护点被摧毁事件（UI 飘字/表现用）：带上 beacon 标识（-1=中心守护点 main，0~2=外围 Sub1~3）
        if (BattleStarted && data.type.category == EntityCategory.Beacon)
        {
            int beaconId = data.type == EntityType.CoreBeacon ? -1 : data.type.value;
            Tool.NetworkManager.SendBattleEvent(new SCBattleEvent
            {
                type = SCBattleEvent.Type.BeaconDestroyed,
                value = beaconId,
            });
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

    private static bool IsZombieCategory(EntityCategory category) =>
        category == EntityCategory.Zombie || category == EntityCategory.EliteZombie;

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
        }
        if (IsZombieCategory(data.type.category)) zombieCount++;
    }

    private void RemoveFromContainer(EntityData data)
    {
        EntityContainer.Entities.Remove(data.id);
        EntityContainer.Beacons.Remove(data.id);
        EntityContainer.Crystals.Remove(data.id);
        EntityContainer.Towers.Remove(data.id);
        if (IsZombieCategory(data.type.category)) zombieCount--;
    }
    #endregion

    #region 玩家进出
    public void AddPlayer(short clientId, CSPlayerInfo info)
    {
        if (PlayerInfoList.ContainsKey(clientId)) return;
        PlayerInfoList[clientId] = info;
        BroadcastRoomInfo();
        Debug.Log($"玩家 {clientId} 进入组队大厅");
    }

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

    private void RebuildAIPlayers()
    {
        foreach (var aiClientId in AIClients)
        {
            PlayerInfoList.Remove(aiClientId);
            PlayerCamp.Remove(aiClientId);
        }
        AIClients.Clear();

        nextAIClientId = AIClientIdStart;
        for (int i = 0; i < AttackAICount; i++) AddAIPlayer(EntityCamp.Attack);
        for (int i = 0; i < DefenseAICount; i++) AddAIPlayer(EntityCamp.Defense);
    }

    private void AddAIPlayer(EntityCamp camp)
    {
        short aiClientId = nextAIClientId--;
        var info = new CSPlayerInfo()
        {
            attackLevel = 1,
            defenseLevel = 1,
            // 名字由虚拟 clientId 推导：重建时 id 源归位，编号稳定且不与真人（正数 id）冲突
            name = $"AI玩家{AIClientIdStart - aiClientId + 1}",
        };
        if (camp == EntityCamp.Attack)
        {
            info.attackCharacter = EntityType.Attack(UnityEngine.Random.Range(0, Config.attack_character_count));
        }
        else
        {
            info.defenseCharacter = EntityType.Defense(UnityEngine.Random.Range(0, Config.defense_character_count));
        }
        PlayerInfoList[aiClientId] = info;
        PlayerCamp[aiClientId] = camp;
        AIClients.Add(aiClientId);
    }

    public void ReceiveRoomUpdate(short clientId, CSRoomUpdate update)
    {
        if (update == null || BattleStarted || !PlayerInfoList.ContainsKey(clientId)) return;

        // 选队（0 进攻 / 1 防守；-1 = 未选择/取消）
        if (update.camp == 0) PlayerCamp[clientId] = EntityCamp.Attack;
        else if (update.camp == 1) PlayerCamp[clientId] = EntityCamp.Defense;
        else PlayerCamp.Remove(clientId);

        // AI 数量：房间共享，任意玩家可编辑，数量不限制。数量变化时重建 AI 玩家（= AI 进场随机选角）
        int attackAI = Mathf.Max(0, update.attackAICount);
        int defenseAI = Mathf.Max(0, update.defenseAICount);
        if (attackAI != AttackAICount || defenseAI != DefenseAICount)
        {
            AttackAICount = attackAI;
            DefenseAICount = defenseAI;
            RebuildAIPlayers();
        }

        BroadcastRoomInfo();
    }

    public void ReceiveStartRequest(short clientId, CSStartRequest request)
    {
        if (request == null || BattleStarted) return;

        // 校验：所有真人玩家已选队伍，且双方人数（人类 + AI）均 > 0
        foreach (var pair in PlayerInfoList)
        {
            if (AIClients.Contains(pair.Key)) continue; // AI 开战前已定阵营
            if (!PlayerCamp.ContainsKey(pair.Key))
            {
                Tool.NetworkManager.SendPrompt(clientId, 18); // 尚有玩家未选择队伍
                return;
            }
        }
        // AI 已并入 PlayerCamp，此处不再另加 AI 数量（否则重复计数）
        if (PlayerCamp.Values.Count(c => c == EntityCamp.Attack) <= 0 ||
            PlayerCamp.Values.Count(c => c == EntityCamp.Defense) <= 0)
        {
            Tool.NetworkManager.SendPrompt(clientId, 17); // 双方人数均需 > 0
            return;
        }

        StartBattle();
    }

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
            if (AIClients.Contains(pair.Key)) continue; // AI 只以 attackAICount/defenseAICount 展示
            int campValue = PlayerCamp.TryGetValue(pair.Key, out var camp) ? (camp == EntityCamp.Attack ? 0 : 1) : -1;
            //所选角色：按成员最终阵营取 CSPlayerInfo 里对应一侧（首页选择随加入上报）
            int characterIndex = -1;
            if (campValue == 0 && pair.Value.attackCharacter.category == EntityCategory.Character_Attack)
                characterIndex = pair.Value.attackCharacter.value;
            else if (campValue == 1 && pair.Value.defenseCharacter.category == EntityCategory.Character_Defense)
                characterIndex = pair.Value.defenseCharacter.value;
            info.members.Add(new SCRoomInfo.RoomMemberInfo()
            {
                clientId = pair.Key,
                camp = campValue,
                characterIndex = characterIndex,
                name = pair.Value.name,
            });
        }
        Tool.NetworkManager.SendRoomInfo(info);
    }

    private Vector3 GetAttackSpawnPos()
    {
        return LandscapeSpawns.RandomOf(Tool.LandscapeSpawns.attackPositions);
    }

    private Vector3 GetDefenseSpawnPos()
    {
        return LandscapeSpawns.RandomOf(Tool.LandscapeSpawns.defensePositions);
    }

    private void UpdateAI()
    {
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity != null) entity.TickAI();
        }
    }

    public void AddBeaconDamage(float damage)
    {
        AttackScore += damage;
    }

    public void UpdateCoreBeaconReduce()
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
                if (beacon.effectController != null) beacon.effectController.AddEffect(EffectType.BeaconReduce, aliveOuter, float.MaxValue);
            }
        }
    }

    public float BeaconRemainingHealth()
    {
        float remaining = 0f;
        foreach (var beacon in EntityContainer.Beacons)
        {
            if (beacon != null) remaining += beacon.floatingAttribute.health;
        }
        return remaining;
    }

    public float DefenseScore()
    {
        return BeaconRemainingHealth() * (1f + Config.kill_score_factor * DefenseKills);
    }

    #endregion

    #region 输入与技能接收（服务器）
    public void ReceiveInput(short clientId, CSPlayerInput input)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;
        entity.RecordInput(input);
    }
    #endregion

    #region 子弹（统一攻击实体）
    public void ShootBullet(EntityData shooter, AttackData attack, BulletTrajectory trajectory, float lifeTime = 0f)
    {
        AddBullet(attack, trajectory, lifeTime);
    }
    #endregion

    #region 战斗规则
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
        if (Tool.EnvironmentManager != null)
        {
            Tool.EnvironmentManager.ResetDayNight();
            // 先退订再订阅：防御方被动（教皇守护）靠昼夜翻转驱动，重复订阅会导致同一次入夜施加多遍
            EnvironmentManager.DayNightFlipped -= OnDayNightFlipped;
            EnvironmentManager.DayNightFlipped += OnDayNightFlipped;
            InitVision(); // 视野系统：小地图失联同样由昼夜翻转驱动
        }

        // 开战重置：id 源置零、计分清零、子弹/移动/复活/重生状态清空
        ResetEntityIdSource();
        AttackScore = 0f;
        DefenseKills = 0;
        zombieCount = 0; // 上局僵尸已在上面的清场里逐个减过，这里再显式归零（与计分同一口径）
        ClearBattleState();

        // 玩家实体重与 AI 同路径（AI clientId 为负虚拟 id），仅输入来源不同：真人 CSPlayerInput、AI UpdateAI
        foreach (var pair in PlayerInfoList)
        {
            short clientId = pair.Key;
            if (!PlayerCamp.TryGetValue(clientId, out var camp)) continue;
            bool isAttack = camp == EntityCamp.Attack;
            EntityType characterType = isAttack ? pair.Value.attackCharacter : pair.Value.defenseCharacter;
            int level = isAttack ? pair.Value.attackLevel : pair.Value.defenseLevel;
            Vector3 spawnPos = isAttack ? GetAttackSpawnPos() : GetDefenseSpawnPos();

            ushort entityId = SpawnEntity(characterType, level, spawnPos, camp);
            if (entityId == 0) continue; // 服务器模板缺失（SpawnEntity 已告警）
            PlayerEntityId[clientId] = entityId;
            EntityOwnerClient[entityId] = clientId;

            // AI 玩家：置位后 PlayerEntityData 才走 AI 决策（真人不受影响，输入仍来自网络）
            if (AIClients.Contains(clientId))
            {
                var aiEntity = GetEntity(entityId);
                if (aiEntity != null) aiEntity.aiControlled = true;
            }

            // AI 无连接：SendBattleInfo 内部按 HasClient 丢弃负数 id
            Tool.NetworkManager.SendBattleInfo(clientId, new SCBattleInfo()
            {
                playerEntityId = entityId,
                camp = camp,
                characterType = characterType,
            });
            Debug.Log($"{(AIClients.Contains(clientId) ? "AI" : "玩家")} {clientId} 出战：{characterType} camp={camp}");
        }

        // 对局世界：守护点×4 / 水晶 / 防御塔 / 瘟疫树（位置来自地形组件 LandscapeSpawns）
        SpawnBattleWorld();
        UpdateCoreBeaconReduce(); // 初始分层减伤 = 存活外围数 × 25%

        // 防守方被动（僵尸刷新等级 / 夜晚延长 / 进攻方复活减速 / 教皇守护）：须在双方实体生成后扫阵营
        ApplyGlobalPassives();

        // 昼夜快照（周期时间 + 白天时长 + 晚上时长）：客户端据此自行推演，中途改时长会再补发
        if (Tool.EnvironmentManager != null)
        {
            Tool.NetworkManager.SendDayNightInfo(Tool.EnvironmentManager.BuildSnapshot());
        }

        BroadcastRoomInfo();
        Debug.Log($"战斗开始：人类 {PlayerInfoList.Count - AIClients.Count}，AI {AIClients.Count}");
    }

    public void EndBattle(int gameState)
    {
        if (!BattleStarted) return;
        BattleStarted = false;
        Debug.Log($"对局结束：{gameState}");
        foreach (var clientId in PlayerInfoList.Keys)
        {
            HarvestByClient.TryGetValue(clientId, out float harvest);
            Tool.NetworkManager.SendScoreInfo(clientId, new SCScoreInfo()
            {
                gameState = gameState,
                attackScore = AttackScore,
                defenseScore = DefenseScore(),
                killScore = DefenseKills,
                remainTime = Mathf.Max(0f, BattleRemainTime),
                expGain = Mathf.RoundToInt(harvest), // 经验 = 采集量 = 对守护点造成的伤害量（策划案 17.3）
                beaconHealth = BeaconRemainingHealth(),
            });
        }
        BroadcastRoomInfo(); // battleStarted = false：客户端结算页关闭后回组队大厅准备下一轮
    }
    #endregion

    #region 帧循环（服务器权威推进）
    // 性能采样标签：在 Profiler 中把 ManagedUpdate 拆成独立项，按调用顺序逐个看耗时
    private const string kManagedUpdateProfilerTag = "BattleManager.ManagedUpdate";
    private const string kTimerTag = "BattleManager.Timer";
    private const string kDayNightTag = "BattleManager.DayNight";
    private const string kEntityUpdateTag = "BattleManager.EntityUpdate";
    private const string kTickMovementTag = "BattleManager.TickMovement";
    private const string kSyncTransformsTag = "BattleManager.SyncTransforms";
    private const string kTickBulletsTag = "BattleManager.TickBullets";
    private const string kTickReviveTag = "BattleManager.TickRevive";
    private const string kTickWorldRespawnTag = "BattleManager.TickWorldRespawn";
    private const string kProcessKilledTag = "BattleManager.ProcessKilled";
    private const string kTickDyingTag = "BattleManager.TickDying";
    private const string kUpdateAITag = "BattleManager.UpdateAI";
    private const string kZombieRefreshTag = "BattleManager.ZombieRefresh";
    private const string kCrystalSpawnTag = "BattleManager.CrystalSpawn";
    private const string kSyncEntitiesTag = "BattleManager.SyncEntitiesToClients";
    private const string kSyncMinimapTag = "BattleManager.SyncMinimap";

    public override void ManagedUpdate()
    {
        Profiler.BeginSample(kManagedUpdateProfilerTag);
        if (!AtServer || !BattleStarted) { Profiler.EndSample(); return; }

        // 计时
        Profiler.BeginSample(kTimerTag);
        if (BattleRemainTime > 0f)
        {
            BattleRemainTime -= UnityEngine.Time.deltaTime;
            if (BattleRemainTime <= 0f)
            {
                // 时间耗尽按分数结算（分数制，不单独处理平局）
                EndBattle(AttackScore >= DefenseScore() ? 1 : 2);
                Profiler.EndSample(); // kTimerTag
                Profiler.EndSample(); // kManagedUpdateProfilerTag
                return;
            }
        }
        Profiler.EndSample(); // kTimerTag

        // 昼夜推进（服务器权威，见策划案 13 章）：周期值在 [0,2) 循环（0/2 午夜、1 正午），方向由周期值推导。
        // 时长或时间被改动时立即补发快照；此外每 Config.daynight_sync_interval 秒心跳一次，兜底两端漂移
        Profiler.BeginSample(kDayNightTag);
        if (Tool.EnvironmentManager != null)
        {
            Tool.EnvironmentManager.Tick(UnityEngine.Time.deltaTime);
            dayNightSyncTimer -= UnityEngine.Time.deltaTime;
            if (Tool.EnvironmentManager.ConsumeSyncRequest() || dayNightSyncTimer <= 0f)
            {
                dayNightSyncTimer = Config.daynight_sync_interval;
                Tool.NetworkManager.SendDayNightInfo(Tool.EnvironmentManager.BuildSnapshot());
            }
        }
        Profiler.EndSample(); // kDayNightTag

        // 实体更新
        Profiler.BeginSample(kEntityUpdateTag);
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity != null) entity.OnUpdate();
        }
        Profiler.EndSample(); // kEntityUpdateTag

        // 战斗核心推进：权威移动（时间戳外推）/ 子弹容器 / 复活与水晶重生
        Profiler.BeginSample(kTickMovementTag);
        TickMovement();
        Profiler.EndSample();
        Profiler.BeginSample(kSyncTransformsTag);
        Physics.SyncTransforms(); //位移已由刚体积分，此处兜底按 transform 移动的对象（项目关闭了自动同步）
        Profiler.EndSample();
        Profiler.BeginSample(kTickBulletsTag);
        TickBullets();
        Profiler.EndSample();
        Profiler.BeginSample(kTickReviveTag);
        TickRevive();
        Profiler.EndSample();
        Profiler.BeginSample(kTickWorldRespawnTag);
        TickWorldRespawn();
        Profiler.EndSample();

        // 处理本帧死亡实体：入队等死亡动画播完再销毁（死亡动画由 EntityData.MarkAsKilled 立刻播放）
        Profiler.BeginSample(kProcessKilledTag);
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
        Profiler.EndSample(); // kProcessKilledTag

        Profiler.BeginSample(kTickDyingTag);
        TickDying(); // 死亡动画播完后物理销毁（BeginDying 入队）
        Profiler.EndSample();

        // AI 行为（各实体在内部按 id 错峰，见 UpdateAI）
        Profiler.BeginSample(kUpdateAITag);
        UpdateAI();
        Profiler.EndSample();

        // 夜间僵尸刷新（策划案第九章）：cd 进度满 1 → 刷新一只并清零；
        // 僵尸数量达上限时不刷新且进度清零；白天（t ≥ 0.5）进度不增加
        Profiler.BeginSample(kZombieRefreshTag);
        if (!EnvironmentManager.IsDay) // t < 0.5 = 晚上
        {
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
                    SpawnZombie(); // 出生点分散在道路/墓地/守护点外围（LandscapeSpawns）
                }
            }
        }
        Profiler.EndSample(); // kZombieRefreshTag

        // 水晶邻近生成：按 Config.crystal_spawn_checks_per_second 每秒若干次检测（随机玩家 → 40~80m 环带内刷新点）
        Profiler.BeginSample(kCrystalSpawnTag);
        TickCrystalSpawn(UnityEngine.Time.deltaTime);
        Profiler.EndSample();

        // 同步实体表现给客户端（0.02s 节流；详细数据 0.2s；仅视野内的实体，见 SyncEntitiesToClients）
        Profiler.BeginSample(kSyncEntitiesTag);
        syncTimer -= UnityEngine.Time.deltaTime;
        detailsTimer -= UnityEngine.Time.deltaTime;
        if (syncTimer <= 0f)
        {
            syncTimer = Config.entity_sync_interval_fast;
            bool details = detailsTimer <= 0f;
            if (details) detailsTimer = Config.entity_sync_interval_details;
            SyncEntitiesToClients(details);
        }
        Profiler.EndSample(); // kSyncEntitiesTag

        // 小地图同步（阵营共享视野，独立节流，见 Config.minimap_sync_interval）
        Profiler.BeginSample(kSyncMinimapTag);
        minimapTimer -= UnityEngine.Time.deltaTime;
        if (minimapTimer <= 0f)
        {
            minimapTimer = Config.minimap_sync_interval;
            SyncMinimapToClients();
        }
        Profiler.EndSample(); // kSyncMinimapTag
        Profiler.EndSample(); // kManagedUpdateProfilerTag
    }

    private void SyncEntitiesToClients(bool includeRuntime)
    {
        foreach (var clientId in PlayerInfoList.Keys)
        {
            var viewer = GetEntityOfClient(clientId);
            if (viewer == null)
            {
                visibleByClient.Remove(clientId); // 复活等待中无观察者：实体已由 DestroyEntity 通知移除
                continue;
            }
            // 上一轮可见集合：用于判断"刚进入视野"，新实体必须整包下发（客户端要靠持久参数才能摆对状态）
            visibleByClient.TryGetValue(clientId, out var prevSeen);
            BeginClientVisibility(clientId);
            foreach (var entity in EntityContainer.Entities)
            {
                if (entity == null) continue;
                if (!CanSeeModel(viewer, entity)) continue;
                MarkClientVisible(entity.id);
                var info = entity.GetDisplayInfo();
                bool firstSeen = prevSeen == null || !prevSeen.Contains(entity.id);
                info.includeRuntime = includeRuntime || firstSeen;
                info.ownerClientId = EntityOwnerClient.TryGetValue(entity.id, out var oc) ? oc : (short)-1;
                FillDisplayVelocity(entity, info);
                Tool.NetworkManager.SendEntityDisplay(clientId, info);

                if (entity.anim != null && (entity.anim.AnimSyncDirty || firstSeen)) SendEntityAnim(clientId, entity);
            }
            EndClientVisibility(clientId);
        }

        // 脏标记是实体级的（与客户端无关），全部客户端处理完再统一清除
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity != null && entity.anim != null) entity.anim.ClearAnimSyncDirty();
        }
    }

    private void SendEntityAnim(short clientId, EntityData entity)
    {
        entity.anim.GetDisplayAnim(out int animId, out float frame);
        var held = entity.anim.GetDisplayHeldWeapon(entity.heldWeapon); // 按当前动画状态解析手持武器（非武器类攻击 = 空手）
        Tool.NetworkManager.SendEntityAnim(clientId, new SCEntityAnimInfo()
        {
            entityId = entity.id,
            animId = animId,
            animFrame = frame,
            animParams = entity.anim.GetParamPack(),
            moveSpeedScale = entity.anim.MoveSpeedScale,
            paused = entity.anim.Paused,
            heldWeaponCategory = (int)held.category,
            heldWeaponIndex = held.index,
        });
    }

    private void FillDisplayVelocity(EntityData entity, SCEntityDisplayInfo info)
    {
        // 位置由物理积分，必须取刚体实际速度：用"意图速度"外插会与权威位置持续漂移；无刚体（不可移动单位）即无速度
        Vector3 v = entity.rb != null ? entity.rb.velocity : Vector3.zero;
        info.velocity = new Vector3(v.x, 0f, v.z); // 纵向不做客户端推演
        info.yawSpeed = entity.YawSpeed;
    }
    #endregion
}
