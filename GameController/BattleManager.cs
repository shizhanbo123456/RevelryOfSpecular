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
    public readonly Dictionary<short, CSLobbyPlayerInfo> PlayerInfoList = new();
    public readonly Dictionary<short, PlayerCampType> PlayerCamps = new(); // 玩家阵营三分类（攻/守/观战），与实体阵营分离
    private readonly Dictionary<short, bool> readyByClient = new(); // 组队界面准备状态（真人专用）
    private Timer.TimerHandle startCountdownHandle; // 非 null 即开战倒计时进行中；取消时须 Cancel，否则残留任务会提前开局
    public readonly Dictionary<short, ushort> PlayerEntityId = new();
    public readonly Dictionary<ushort, short> EntityOwnerClient = new();

    private const short AIClientIdStart = -1000;
    private short nextAIClientId = AIClientIdStart;
    public readonly HashSet<short> AIClients = new();

    // 加载阶段：已完成的客户端（战斗表现包只发给它们；观战未完成时不阻塞开战）
    private bool LoadingStarted;
    private readonly HashSet<short> loadedClients = new();
    // 已预分配的实体 id（尚未 Spawn），分配器跳过它们，避免重复
    private readonly HashSet<ushort> reservedEntityIds = new();

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
        public static readonly ChunkSearcher<EntityData> GuardStatues = new(d => d.transform.position);
        public static readonly ChunkSearcher<EntityData> Corals = new(d => d.transform.position);
        public static readonly ChunkSearcher<EntityData> CrystalStatues = new(d => d.transform.position);

        public static void Clear()
        {
            Entities.Clear();
            GuardStatues.Clear();
            Corals.Clear();
            CrystalStatues.Clear();
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
        return SpawnEntity(AllocEntityId(), type, level, pos, camp);
    }

    // 用加载阶段预分配的 id 生成（id 由 BeginLoading 预留，客户端加载期拿到的就是它）
    public ushort SpawnEntity(ushort reservedId, EntityType type, int level, Vector3 pos, EntityCamp camp)
    {
        if (!Tool.AssetsManager.TryGetGraphic(type, out var prefab) || prefab == null)
        {
            Debug.LogWarning($"缺少实体预制体：{type}，请先在 AssetsManager 配置对应 Graphics");
            return 0;
        }
        ushort id = reservedId;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        // EntityData 不挂预制体（图形预制体自带表现），生成时按类别补上对应子类
        var data = go.GetComponent<EntityData>();
        if (data == null) data = EntityData.AddTo(go, type.category);
        if (data == null)
        {
            Debug.LogError($"实体类别 {type.category} 没有对应的 EntityData 子类，无法生成");
            Destroy(go);
            return 0;
        }
        data.OnCreate(id, type, level, camp);
        // 初始技能表按实体类型统一赋（来源 entity.csv 初始技能列表列）：玩家角色与非玩家单位（僵尸/精英/雕像）同一条路径，
        // 表里没配即为空表
        if (data.skillController != null)
            data.skillController.SetSkillList(ConfigManager.EntityConfig.GetByCategory(type).initialSkills);
        ApplyMinimapLostOnSpawn(data); // 夜间出生/复活：补上「小地图失联」（昼夜事件只在翻转那一刻遍历）
        AddToContainer(data);
        reservedEntityIds.Remove(id); // 已实体化，不再占用预留位
        return id;
    }

    private void ResetEntityIdSource() => nextEntityId = 1;

    private ushort AllocEntityId()
    {
        // 服务器实体 id 自增，0 保留；超过上限 30000 回绕到 1（跳过已占用与已预留）
        do
        {
            nextEntityId++;
            if (nextEntityId > Config.entity_id_max) nextEntityId = 1;
        } while (EntityContainer.Entities.Contains(nextEntityId) || reservedEntityIds.Contains(nextEntityId));
        return nextEntityId;
    }

    public static EntityData GetEntity(ushort id) =>
        EntityContainer.Entities.TryGetObject(id, out var e) ? e : null;

    public bool DestroyEntity(ushort id)
    {
        if (!EntityContainer.Entities.TryGetObject(id, out var data)) return false;
        RemoveFromContainer(data);
        // 玩家实体销毁：清空其"玩家 → 实体"关系（死亡/离场/对局清理统一走此入口）
        if (EntityOwnerClient.TryGetValue(id, out var ownerId))
        {
            EntityOwnerClient.Remove(id);
            Tool.NetworkManager.SendPlayerEntity(ownerId, -1);
        }

        // 通知所有客户端移除该实体表现（死亡/离场/摧毁统一走此入口）
        foreach (var clientId in PlayerInfoList.Keys)
        {
            Tool.NetworkManager.SendRemoveEntity(clientId, id);
        }
        // 雕像被摧毁事件（事件列表 icon=其它）：textId 区分哪一座（19 中心 / 24~26 外围）
        if (BattleStarted && data.type.category == EntityCategory.GuardStatue)
        {
            MarkDefensePointDestroyed(data); // 实体销毁后算不出血量，进度在这里定死 100
            int textId = data.type == EntityType.KingStatue ? 19 : 24 + data.type.value; // GuardStatue(0~2) → 24~26
            Tool.NetworkManager.SendBattleEvent(new SCBattleEvent
            {
                type = SCBattleEvent.Type.StatueDestroyed,
                textId = textId,
            });
        }
        // 水晶雕像被摧毁事件（事件列表 icon=其它）
        else if (BattleStarted && data.type.category == EntityCategory.CrystalStatue)
        {
            Tool.NetworkManager.SendBattleEvent(new SCBattleEvent
            {
                type = SCBattleEvent.Type.CrystalStatueDestroyed,
                textId = 27,
            });
        }

        data.OnDestroyed();
        bool wasKingStatue = data.type.category == EntityCategory.GuardStatue && data.type == EntityType.KingStatue;
        Destroy(data.gameObject);
        if (wasKingStatue && BattleStarted)
        {
            EndBattle(1); // 国王雕像被摧毁 → 进攻方必然获胜（策划案 17.2）
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
            case EntityCategory.GuardStatue:
                EntityContainer.GuardStatues.Add(data.id, data);
                break;
            case EntityCategory.Coral:
                EntityContainer.Corals.Add(data.id, data);
                break;
            case EntityCategory.CrystalStatue:
                EntityContainer.CrystalStatues.Add(data.id, data);
                break;
        }
        if (IsZombieCategory(data.type.category)) zombieCount++;
    }

    private void RemoveFromContainer(EntityData data)
    {
        EntityContainer.Entities.Remove(data.id);
        EntityContainer.GuardStatues.Remove(data.id);
        EntityContainer.Corals.Remove(data.id);
        EntityContainer.CrystalStatues.Remove(data.id);
        if (IsZombieCategory(data.type.category)) zombieCount--;
    }
    #endregion

    #region 玩家进出
    public void AddPlayer(short clientId, CSLobbyPlayerInfo info)
    {
        if (PlayerInfoList.ContainsKey(clientId)) return;
        PlayerInfoList[clientId] = info;
        PlayerCamps[clientId] = PlayerCampType.Spectator; // 进入大厅默认观战方
        SendLobbyPlayerNames(clientId); // 名字单独下发，先把映射铺好再播房间成员列表
        BroadcastLobbyInfo();
        Debug.Log($"玩家 {clientId} 进入组队大厅");
    }

    // 新玩家进房：新玩家名字广播给所有人，再把房间里已有玩家的名字逐条发给新玩家（AI 不下发名字）
    private void SendLobbyPlayerNames(short newClientId)
    {
        if (PlayerInfoList.TryGetValue(newClientId, out var me))
            Tool.NetworkManager.SendLobbyPlayerName(new SCLobbyPlayerName { clientId = newClientId, name = me.name });

        foreach (var pair in PlayerInfoList)
        {
            if (pair.Key < 0) continue; // AI 无名字
            Tool.NetworkManager.SendLobbyPlayerName(newClientId, new SCLobbyPlayerName { clientId = pair.Key, name = pair.Value.name });
        }
    }

    public void RemovePlayer(short clientId)
    {
        if (PlayerEntityId.TryGetValue(clientId, out var entityId))
        {
            DestroyEntity(entityId);
            PlayerEntityId.Remove(clientId);
        }
        ExitSpectate(clientId);  // 从被观战目标的列表摘除自己
        ClearSpectators(clientId); // 观战我的人失去目标（回到各自冻结视野）
        visionStates.Remove(clientId);
        readyByClient.Remove(clientId);
        loadedClients.Remove(clientId);
        PlayerInfoList.Remove(clientId);
        PlayerCamps.Remove(clientId);
        // 开始倒计时中有人退场：剩余真人不再全员 ready 则取消计时并提示（仍满足则继续倒计时）
        if (startCountdownHandle != null && !AllHumansReady())
        {
            CancelStartCountdown();
            SendPromptToAll(29);
        }
        // 加载阶段有人退场：剩下的人若已全部回执，直接开战，不再等退场者
        if (LoadingStarted && !BattleStarted && AllCombatClientsLoaded()) StartBattle();
        if (!BattleStarted) BroadcastLobbyInfo();
        Debug.Log($"玩家 {clientId} 退场");
    }

    private void RebuildAIPlayers()
    {
        foreach (var aiClientId in AIClients)
        {
            PlayerInfoList.Remove(aiClientId);
            PlayerCamps.Remove(aiClientId);
        }
        AIClients.Clear();

        nextAIClientId = AIClientIdStart;
        for (int i = 0; i < AttackAICount; i++) AddAIPlayer(PlayerCampType.Attack);
        for (int i = 0; i < DefenseAICount; i++) AddAIPlayer(PlayerCampType.Defense);
    }

    private void AddAIPlayer(PlayerCampType camp)
    {
        short aiClientId = nextAIClientId--;
        var info = new CSLobbyPlayerInfo()
        {
            attackLevel = 1,
            defenseLevel = 1,
            // 名字由虚拟 clientId 推导：重建时 id 源归位，编号稳定且不与真人（正数 id）冲突
            name = $"AI玩家{AIClientIdStart - aiClientId + 1}",
        };
        if (camp == PlayerCampType.Attack)
        {
            info.attackCharacter = EntityType.Attack(UnityEngine.Random.Range(0, Config.attack_character_count));
        }
        else
        {
            info.defenseCharacter = EntityType.Defense(UnityEngine.Random.Range(0, Config.defense_character_count));
        }
        PlayerInfoList[aiClientId] = info;
        PlayerCamps[aiClientId] = camp;
        AIClients.Add(aiClientId);
    }

    public void ReceiveLobbyUpdate(short clientId, CSLobbyUpdate update)
    {
        // 加载阶段与对局中都不再接受组队大厅的改动
        if (update == null || BattleStarted || LoadingStarted || !PlayerInfoList.ContainsKey(clientId)) return;

        // 选队（0 进攻 / 1 防守 / 2 观战；进入大厅默认观战，不存在无阵营状态）
        if (update.camp == (int)PlayerCampType.Attack) PlayerCamps[clientId] = PlayerCampType.Attack;
        else if (update.camp == (int)PlayerCampType.Defense) PlayerCamps[clientId] = PlayerCampType.Defense;
        else if (update.camp == (int)PlayerCampType.Spectator) PlayerCamps[clientId] = PlayerCampType.Spectator;

        // 准备状态（任意玩家可随时切换，全员准备后进入开战倒计时）
        SetReady(clientId, update.ready);

        // AI 数量：房间共享，任意玩家可编辑，数量不限制。数量变化时重建 AI 玩家（= AI 进场随机选角）
        int attackAI = Mathf.Max(0, update.attackAICount);
        int defenseAI = Mathf.Max(0, update.defenseAICount);
        if (attackAI != AttackAICount || defenseAI != DefenseAICount)
        {
            AttackAICount = attackAI;
            DefenseAICount = defenseAI;
            RebuildAIPlayers();
        }

        BroadcastLobbyInfo();
    }

    #region 准备与开局倒计时
    private bool AllHumansReady()
    {
        foreach (var clientId in PlayerInfoList.Keys)
        {
            if (AIClients.Contains(clientId)) continue;
            if (!readyByClient.TryGetValue(clientId, out var ready) || !ready) return false;
        }
        return true;
    }

    private void SetReady(short clientId, bool ready)
    {
        if (BattleStarted || LoadingStarted || AIClients.Contains(clientId) || !PlayerInfoList.ContainsKey(clientId)) return;
        readyByClient[clientId] = ready;
        if (!ready)
        {
            if (startCountdownHandle != null)
            {
                CancelStartCountdown();
                SendPromptToAll(29); // 有玩家取消了准备
            }
            return;
        }
        if (startCountdownHandle != null || !AllHumansReady()) return;
        // 全员准备：双方（人类 + AI）均需 > 0 才进入开始倒计时
        if (PlayerCamps.Values.Count(c => c == PlayerCampType.Attack) <= 0
            || PlayerCamps.Values.Count(c => c == PlayerCampType.Defense) <= 0)
        {
            SendPromptToAll(17);
            return;
        }
        SendPromptToAll(28); // 全员已准备，战斗即将开始
        startCountdownHandle = Timer.AddTimer(0, _ => TryFireStartBattle(), ConfigManager.GlobalVariableConfig.ready_start_delay, 1, false);
    }

    // 业务取消：置空句柄并让已注册任务作废，重开倒计时才是全新计时
    private void CancelStartCountdown()
    {
        if (startCountdownHandle != null) startCountdownHandle.Cancel();
        startCountdownHandle = null;
    }

    // 倒计时到期：期间可能有人取消/退场，重新校验后进入加载阶段
    private void TryFireStartBattle()
    {
        if (startCountdownHandle == null) return;
        startCountdownHandle = null;
        if (BattleStarted || LoadingStarted) return;
        if (!AllHumansReady()
            || PlayerCamps.Values.Count(c => c == PlayerCampType.Attack) <= 0
            || PlayerCamps.Values.Count(c => c == PlayerCampType.Defense) <= 0)
        {
            SendPromptToAll(17);
            return;
        }
        BeginLoading();
    }

    #region 加载阶段
    // 加载阶段：先让客户端切加载界面，再预分配实体 id，等客户端逐步请求数据并回执完成后才真正开战
    private void BeginLoading()
    {
        if (LoadingStarted || BattleStarted) return;
        LoadingStarted = true;
        loadedClients.Clear();

        // 上局残留（结算期间保留实体展示）在这里统一清掉
        var stale = new List<EntityData>();
        foreach (var e in EntityContainer.Entities)
        {
            if (e != null) stale.Add(e);
        }
        foreach (var e in stale) DestroyEntity(e.id);

        ResetEntityIdSource(); // 本局唯一一次重置 id 源
        reservedEntityIds.Clear();
        PlayerEntityId.Clear();

        // ① 先切加载界面（观战方 camp = None）
        foreach (var pair in PlayerInfoList)
        {
            if (AIClients.Contains(pair.Key)) continue;
            Tool.NetworkManager.SendLoadingStart(pair.Key, new SCLoadingStart { camp = CampOf(pair.Key) });
        }

        // ② 预分配实体 id：只登记不生成（提前生成会让实体动画在加载期就开始播）
        foreach (var pair in PlayerInfoList)
        {
            if (CampOf(pair.Key) == EntityCamp.None) continue; // 观战方无实体
            ushort id = AllocEntityId();
            reservedEntityIds.Add(id);
            PlayerEntityId[pair.Key] = id;
            EntityOwnerClient[id] = pair.Key;
        }

        // ③ 预处理完成：客户端此时才开始逐步请求数据
        foreach (var pair in PlayerInfoList)
        {
            if (AIClients.Contains(pair.Key)) continue;
            PlayerEntityId.TryGetValue(pair.Key, out var ownId);
            bool isDefense = PlayerCamps.TryGetValue(pair.Key, out var camp) && camp == PlayerCampType.Defense;
            Tool.NetworkManager.SendLoadingPrepared(pair.Key, new SCLoadingPrepared
            {
                ownEntityId = ownId,
                // 观战方没有战斗角色，与旧行为一致地沿用其进攻方选择（结算取角色下标用）
                characterType = isDefense ? pair.Value.defenseCharacter : pair.Value.attackCharacter,
                serverTime = Time.time,
            });
        }
        Debug.Log($"[加载] 开始：真人 {PlayerInfoList.Count - AIClients.Count}，预留实体 id {reservedEntityIds.Count} 个");
    }

    private EntityCamp CampOf(short clientId)
    {
        if (!PlayerCamps.TryGetValue(clientId, out var camp)) return EntityCamp.None;
        if (camp == PlayerCampType.Attack) return EntityCamp.Attack;
        if (camp == PlayerCampType.Defense) return EntityCamp.Defense;
        return EntityCamp.None;
    }

    public void OnLoadingRequestAttackPlayers(short clientId) =>
        Tool.NetworkManager.SendLoadingAttackPlayerResponse(clientId,
            new SCLoadingAttackPlayerResponse { list = BuildLoadingUnits(PlayerCampType.Attack) });

    public void OnLoadingRequestDefensePlayers(short clientId) =>
        Tool.NetworkManager.SendLoadingDefensePlayerResponse(clientId,
            new SCLoadingDefensePlayerResponse { list = BuildLoadingUnits(PlayerCampType.Defense) });

    public void OnLoadingRequestAiUnits(short clientId) =>
        Tool.NetworkManager.SendLoadingAiUnitResponse(clientId,
            new SCLoadingAiUnitResponse { list = BuildLoadingUnits(null) });

    // humanCamp = null 表示 AI 单位（虚拟负数 clientId）
    private List<LoadingUnitEntry> BuildLoadingUnits(PlayerCampType? humanCamp)
    {
        var list = new List<LoadingUnitEntry>();
        foreach (var pair in PlayerInfoList)
        {
            bool isAI = AIClients.Contains(pair.Key);
            if (humanCamp.HasValue)
            {
                if (isAI) continue;
                if (!PlayerCamps.TryGetValue(pair.Key, out var camp) || camp != humanCamp.Value) continue;
            }
            else if (!isAI) continue;

            if (!PlayerEntityId.TryGetValue(pair.Key, out var entityId)) continue; // 观战方无实体
            list.Add(new LoadingUnitEntry
            {
                clientId = pair.Key,
                entityId = entityId,
                name = NameOf(pair.Key, pair.Value),
                camp = CampOf(pair.Key),
            });
        }
        return list;
    }

    private static string NameOf(short clientId, CSLobbyPlayerInfo info)
    {
        if (info != null && !string.IsNullOrEmpty(info.name)) return info.name;
        return clientId < 0 ? $"AI玩家{AIClientIdStart - clientId + 1}" : $"玩家{clientId}";
    }

    // 攻守真人全部回执完成后才真正开战；观战方不阻塞
    private bool AllCombatClientsLoaded()
    {
        foreach (var pair in PlayerCamps)
        {
            if (AIClients.Contains(pair.Key)) continue;
            if (pair.Value == PlayerCampType.Spectator) continue;
            if (!loadedClients.Contains(pair.Key)) return false;
        }
        return true;
    }

    public void OnClientLoadingComplete(short clientId)
    {
        if (AIClients.Contains(clientId) || !PlayerInfoList.ContainsKey(clientId)) return;
        if (!LoadingStarted && !BattleStarted) return;
        if (!loadedClients.Add(clientId)) return;

        if (BattleStarted)
        {
            // 开战后才完成的观战方：补发开始战斗，表现数据由首见全量补上
            Tool.NetworkManager.SendBattleStart(clientId);
            return;
        }
        if (AllCombatClientsLoaded()) StartBattle();
    }
    #endregion

    private void SendPromptToAll(int messageId)
    {
        foreach (var clientId in PlayerInfoList.Keys) Tool.NetworkManager.SendPrompt(clientId, messageId);
    }
    #endregion

    private void BroadcastLobbyInfo()
    {
        var info = new SCLobbyInfo()
        {
            attackAICount = AttackAICount,
            defenseAICount = DefenseAICount,
            battleStarted = BattleStarted,
        };
        foreach (var pair in PlayerInfoList)
        {
            if (pair.Key < 0) continue; // AI 不进成员列表，只通过两个数量表达
            int campValue = PlayerCamps.TryGetValue(pair.Key, out var camp) ? (int)camp : (int)PlayerCampType.Spectator;
            //所选角色：按成员最终阵营取 CSLobbyPlayerInfo 里对应一侧（首页选择随加入上报）
            int characterIndex = -1;
            if (campValue == (int)PlayerCampType.Attack && pair.Value.attackCharacter.category == EntityCategory.Character_Attack)
                characterIndex = pair.Value.attackCharacter.value;
            else if (campValue == (int)PlayerCampType.Defense && pair.Value.defenseCharacter.category == EntityCategory.Character_Defense)
                characterIndex = pair.Value.defenseCharacter.value;
            readyByClient.TryGetValue(pair.Key, out var ready);
            info.members.Add(new SCLobbyInfo.LobbyMemberInfo()
            {
                clientId = pair.Key,
                camp = campValue,
                characterIndex = characterIndex,
                ready = ready,
            });
        }
        Tool.NetworkManager.SendLobbyInfo(info);
    }

    //聊天只转发不存历史；发送方名字/阵营/角色由服务器补齐（客户端不知道别人的）
    public void ReceiveChat(short clientId, CSChat chat)
    {
        if (chat == null) return;
        PlayerInfoList.TryGetValue(clientId, out var info);
        int campValue = PlayerCamps.TryGetValue(clientId, out var camp) ? (int)camp : (int)PlayerCampType.Spectator;
        int characterIndex = -1;
        if (info != null)
        {
            if (campValue == 0 && info.attackCharacter.category == EntityCategory.Character_Attack)
                characterIndex = info.attackCharacter.value;
            else if (campValue == 1 && info.defenseCharacter.category == EntityCategory.Character_Defense)
                characterIndex = info.defenseCharacter.value;
        }
        Tool.NetworkManager.SendChat(new SCChat()
        {
            senderClientId = clientId,
            channel = chat.channel,
            camp = campValue,
            characterIndex = characterIndex,
            name = info != null ? info.name : "",
            content = chat.content,
        });
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

    public void AddStatueDamage(float damage)
    {
        AttackScore += damage;
    }

    public void UpdateKingStatueReduce()
    {
        int aliveOuter = 0;
        foreach (var guardStatue in EntityContainer.GuardStatues)
        {
            if (guardStatue != null && guardStatue.Alive && guardStatue.type != EntityType.KingStatue) aliveOuter++;
        }
        foreach (var guardStatue in EntityContainer.GuardStatues)
        {
            if (guardStatue != null && guardStatue.type == EntityType.KingStatue)
            {
                if (guardStatue.effectController == null) continue;
                // 分层减伤是独立计时类，重算前必须先移除，否则每次调用都会压一层
                guardStatue.effectController.RemoveEffect(EffectType.StatueReduce);
                if (aliveOuter > 0) guardStatue.effectController.AddEffect(EffectType.StatueReduce, aliveOuter, float.MaxValue);
            }
        }
    }

    public float StatueRemainingHealth()
    {
        float remaining = 0f;
        foreach (var guardStatue in EntityContainer.GuardStatues)
        {
            if (guardStatue != null) remaining += guardStatue.floatingAttribute.health;
        }
        return remaining;
    }

    public float DefenseScore()
    {
        return StatueRemainingHealth() * (1f + Config.kill_score_factor * DefenseKills);
    }

    #endregion

    #region 输入与技能接收（服务器）
    public void ReceiveInput(short clientId, CSPlayerInput input)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;
        entity.RecordInput(input);
    }

    // 技能键：客户端已经把按键解析成技能 id（技能栏只在客户端），这里只校验持有与状态
    public void ReceiveSkillCast(short clientId, int skillId)
    {
        if (skillId < 0) return;
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;
        if (entity.InputBlocked(InputBlockOp.Attack)) return; // 攻击动画等期间的输入阻断
        var result = entity.skillController.TryUseSkill(skillId);
        if (result != EntitySkillController.UseResult.Success) NotifySkillUseFailed(entity, result);
    }

    // 相机模式决定 WASD 的解释口径（俯视 = 四向），由客户端在切模式时同步
    public void SetCameraMode(short clientId, bool topDown)
    {
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity)) return;
        var player = entity as PlayerEntityData;
        if (player == null) return;
        player.SetTopDown(topDown);
    }
    #endregion

    #region 子弹（统一攻击实体）
    // 命中判定完全由 spec（特效命中球 + 配表倍率/间隔）驱动，不再有按寿命归一化的伤害窗口
    public void ShootBullet(EntityData shooter, AttackData attack, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletHitSpec spec = default)
    {
        AddBullet(attack, trajectory, lifeTime, spec);
    }
    #endregion

    #region 战斗规则
    public void StartBattle()
    {
        if (BattleStarted) return;

        // 清场上局残留实体（结算期间实体保留展示；须在 BattleStarted 置位前清除，避免触发国王雕像结束判定）
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
        BattleRemainTime = ConfigManager.GlobalVariableConfig.battle_duration;
        if (Tool.EnvironmentManager != null)
        {
            Tool.EnvironmentManager.ResetDayNight();
            // 先退订再订阅：防御方被动（教皇守护）靠昼夜翻转驱动，重复订阅会导致同一次入夜施加多遍
            EnvironmentManager.DayNightFlipped -= OnDayNightFlipped;
            EnvironmentManager.DayNightFlipped += OnDayNightFlipped;
            InitVision(); // 视野系统：小地图失联同样由昼夜翻转驱动
        }

        // 开战重置：计分清零、子弹/移动/复活/重生状态清空（id 源已在加载阶段重置并预留，这里不再重置）
        if (reservedEntityIds.Count == 0) ResetEntityIdSource(); // 未经加载阶段直接开战（GM/调试）时兜底
        AttackScore = 0f;
        DefenseKills = 0;
        zombieCount = 0; // 上局僵尸已在上面的清场里逐个减过，这里再显式归零（与计分同一口径）
        ClearBattleState();

        // 玩家实体重与 AI 同路径（AI clientId 为负虚拟 id），仅输入来源不同：真人 CSPlayerInput、AI UpdateAI
        foreach (var pair in PlayerInfoList)
        {
            short clientId = pair.Key;
            var campType = PlayerCamps[clientId];
            if (campType == PlayerCampType.Spectator)
            {
                Tool.NetworkManager.SendPlayerEntity(clientId, -1); // 观战方无战斗实体
                continue;
            }
            bool isAttack = campType == PlayerCampType.Attack;
            EntityType characterType = isAttack ? pair.Value.attackCharacter : pair.Value.defenseCharacter;
            int level = isAttack ? pair.Value.attackLevel : pair.Value.defenseLevel;
            Vector3 spawnPos = isAttack ? GetAttackSpawnPos() : GetDefenseSpawnPos();
            EntityCamp camp = isAttack ? EntityCamp.Attack : EntityCamp.Defense;

            // 用加载阶段预分配的 id 生成，客户端拿到的 id 与加载期请求的表一致
            ushort entityId = PlayerEntityId.TryGetValue(clientId, out var reservedId)
                ? SpawnEntity(reservedId, characterType, level, spawnPos, camp)
                : SpawnEntity(characterType, level, spawnPos, camp);
            if (entityId == 0) continue; // 服务器模板缺失（SpawnEntity 已告警）
            PlayerEntityId[clientId] = entityId;
            EntityOwnerClient[entityId] = clientId;
            Tool.NetworkManager.SendPlayerEntity(clientId, entityId);

            // AI 玩家：置位后 PlayerEntityData 才走 AI 决策（真人不受影响，输入仍来自网络）
            if (AIClients.Contains(clientId))
            {
                var aiEntity = GetEntity(entityId);
                if (aiEntity != null) aiEntity.aiControlled = true;
            }
            Debug.Log($"{(AIClients.Contains(clientId) ? "AI" : "玩家")} {clientId} 出战：{characterType} camp={camp} id={entityId}");
        }

        // 对局世界：雕像×4 / 珊瑚 / 水晶雕像 / 中立雕像（位置来自地形组件 LandscapeSpawns）
        SpawnBattleWorld();
        UpdateKingStatueReduce(); // 初始分层减伤 = 存活外围数 × 25%

        // 防守方开局被动（僵尸刷新等级 / 教皇守护）：须在双方实体生成后扫阵营；受击类被动在伤害结算里即时判定
        ApplyGlobalPassives();

        // 昼夜快照（周期时间 + 白天时长 + 晚上时长）：客户端据此自行推演，中途改时长会再补发
        if (Tool.EnvironmentManager != null)
        {
            Tool.NetworkManager.SendDayNightInfo(Tool.EnvironmentManager.BuildSnapshot());
        }

        BroadcastLobbyInfo();

        // 只告知已加载完成的客户端开战；未完成的观战方等它回执完成时补发
        LoadingStarted = false;
        reservedEntityIds.Clear();
        foreach (var clientId in loadedClients) Tool.NetworkManager.SendBattleStart(clientId);
        Debug.Log($"战斗开始：人类 {PlayerInfoList.Count - AIClients.Count}，AI {AIClients.Count}");
    }

    public void EndBattle(int gameState)
    {
        if (!BattleStarted) return;
        BattleStarted = false; // 先置 false：DestroyEntity 靠它判断要不要发雕像/水晶雕像摧毁事件
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
                expGain = Mathf.RoundToInt(harvest), // 经验 = 采集量 = 对雕像造成的伤害量（策划案 17.3）
                guardStatueHealth = StatueRemainingHealth(),
            });
        }

        // 对局结束立即清理全部实体（结算展示由客户端自己保留/清除，服务器不再留残局实体）
        var stale = new List<EntityData>();
        foreach (var e in EntityContainer.Entities)
        {
            if (e != null) stale.Add(e);
        }
        foreach (var e in stale) DestroyEntity(e.id);
        PlayerEntityId.Clear();
        EntityOwnerClient.Clear();
        zombieCount = 0;

        BroadcastLobbyInfo(); // battleStarted = false：客户端结算页关闭后回组队大厅准备下一轮
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
    private const string kCampAliveCountTag = "BattleManager.CampAliveCount";
    private const string kDefensePointTag = "BattleManager.DefensePointProgress";
    private const string kTickDyingTag = "BattleManager.TickDying";
    private const string kUpdateAITag = "BattleManager.UpdateAI";
    private const string kZombieRefreshTag = "BattleManager.ZombieRefresh";
    private const string kCoralSpawnTag = "BattleManager.CoralSpawn";
    private const string kSyncEntitiesTag = "BattleManager.SyncEntitiesToClients";
    private const string kSyncMinimapTag = "BattleManager.SyncMinimap";
    private const string kVisionStateTag = "BattleManager.VisionState";
    private const string kTransformDirtyTag = "BattleManager.TransformDirty";

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

        // 战斗核心推进：权威移动（时间戳外推）/ 子弹容器 / 复活与珊瑚刷新
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

        Profiler.BeginSample(kCampAliveCountTag);
        TickCampAliveCount();
        Profiler.EndSample();

        Profiler.BeginSample(kDefensePointTag);
        TickDefensePointProgress(); // 必须早于 TickDying：血量归零当帧还能读到雕像
        Profiler.EndSample();

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
            if (zombieCount >= ConfigManager.GlobalVariableConfig.zombie_max)
            {
                zombieRefreshProgress = 0f;
            }
            else
            {
                // 越少越快：0 只 → zombie_refresh_rate_fast，接近上限 → zombie_refresh_rate_slow（线性插值）
                float rate = Mathf.Lerp(ConfigManager.GlobalVariableConfig.zombie_refresh_rate_fast, ConfigManager.GlobalVariableConfig.zombie_refresh_rate_slow,
                    (float)zombieCount / ConfigManager.GlobalVariableConfig.zombie_max);
                zombieRefreshProgress += UnityEngine.Time.deltaTime * rate;
                if (zombieRefreshProgress >= Config.zombie_refresh_progress_max)
                {
                    zombieRefreshProgress = 0f;
                    SpawnZombie(); // 出生点分散在道路/墓地/雕像外围（LandscapeSpawns）
                }
            }
        }
        Profiler.EndSample(); // kZombieRefreshTag

        // 珊瑚邻近生成：按 Config.coral_spawn_checks_per_second 每秒若干次检测（随机玩家 → 40~80m 环带内刷新点）
        Profiler.BeginSample(kCoralSpawnTag);
        TickCoralSpawn(UnityEngine.Time.deltaTime);
        Profiler.EndSample();

        // 视野锚点逐帧刷新（观战跟随 / 死亡冻结），供实体同步与小地图使用
        Profiler.BeginSample(kVisionStateTag);
        TickVisionStates();
        Profiler.EndSample();

        // 位姿脏标记（必须在本帧移动/物理同步之后判定）：与上次下发位姿差超阈值才置脏，无脏的快轮整包不下发
        Profiler.BeginSample(kTransformDirtyTag);
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity != null) entity.CheckTransformDirty();
        }
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
            // 未完成加载的客户端不发战斗表现数据：其可见集合保持为空，回执后第一轮自然按首见全量补上
            if (!loadedClients.Contains(clientId)) continue;
            // 视野锚点（存活跟随自己 / 观战跟随目标 / 死亡冻结在死亡位置），死亡不再跳过：按冻结位置继续下发
            if (!visionStates.TryGetValue(clientId, out _)) continue;
            // 上一轮可见集合：用于判断"刚进入视野"，新实体必须整包下发（客户端要靠持久参数才能摆对状态）
            visibleByClient.TryGetValue(clientId, out var prevSeen);
            BeginClientVisibility(clientId);
            foreach (var entity in EntityContainer.Entities)
            {
                if (entity == null) continue;
                if (!CanSeeFromState(clientId, entity)) continue;
                MarkClientVisible(entity.id); // 后面可能跳过下发，但可见集合仍要标上，否则会被当成离屏而回 remove 包
                bool firstSeen = prevSeen == null || !prevSeen.Contains(entity.id);
                bool full = includeRuntime || firstSeen;
                // 持有技能列表只发给该实体所属客户端（技能栏只在客户端，别人不消费）
                bool ownEntity = EntityOwnerClient.TryGetValue(entity.id, out var ownerClient) && ownerClient == clientId;
                var info = entity.GetDisplayInfo(full, ownEntity);
                info.includeRuntime = full;
                // 位姿/血量/buff/技能全无变化就不发整包；全量轮 full 恒真，兼作静止实体的心跳
                if (info.hasTransform || info.hasHealth || info.hasBuffs || info.hasSkills)
                {
                    FillDisplayVelocity(entity, info);
                    Tool.NetworkManager.SendEntityDisplay(clientId, info);
                }
                // 动画包独立于表现包：表现包被跳过时仍要发，否则本轮末尾清掉 AnimSyncDirty 后这次状态变化就永久丢了
                if (entity.anim != null && (entity.anim.AnimSyncDirty || firstSeen)) SendEntityAnim(clientId, entity);
            }
            EndClientVisibility(clientId);
        }

        // 本轮至少被一个客户端可见的实体：清 health/buff/skill 脏标记（无人可见则保留，等下次可见随完整同步下发）
        foreach (var id in s_dirtySweep)
        {
            var e = GetEntity(id);
            if (e == null) continue;
            e.healthDirty = false;
            e.buffDirty = false;
            e.skillDirty = false;
            // 位姿待发次数每轮只 +1（与客户端数量无关）；基准只在真的发出去后推进，否则慢速移动永远攒不满阈值
            if (includeRuntime || e.transformDirty < 2) e.OnTransformSent();
        }
        s_dirtySweep.Clear();

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
        if (!info.hasTransform) return; // 不带位姿的包也不带速度，客户端不会拿它外插
        // 位置由物理积分，必须取刚体实际速度：用"意图速度"外插会与权威位置持续漂移；无刚体（不可移动单位）即无速度
        Vector3 v = entity.rb != null ? entity.rb.velocity : Vector3.zero;
        info.velocity = new Vector3(v.x, 0f, v.z); // 纵向不做客户端推演
        info.yawSpeed = entity.YawSpeed;
    }
    #endregion
}
