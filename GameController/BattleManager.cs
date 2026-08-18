using Ros.Info;
using Ros.Transport;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static EntityEffectController;

public partial class BattleManager : EnsBehaviour
{
    private void Awake()
    {
        EnsInstance.OnClientExit += RemovePlayerInfo;
        Tool.BattleManager = this;
    }
    public static bool AtServer => EnsInstance.HasAuthority;

    public Dictionary<short,CSPlayerInfo>PlayerInfoList=new();
    public Dictionary<short,SCPlayerInfo>RoomInfoList=new();

    public partial LevelInfo GetLevelInfo();
    public SCPlayerInfo AddPlayerInfo(CSPlayerInfo info,short id)
    {
        if (RoomInfoList.ContainsKey(id))
        {
            RemovePlayerInfo(id);
        }
        PlayerInfoList[id] = info;
        int spawn=UnityEngine.Random.Range(0, Landscape.instance.SpawnPosList.Count);
        var eid=EntityContainer.SpawnEntity(info.type, info.level, Landscape.instance.SpawnPosList[spawn].position);
        var roomInfo= new SCPlayerInfo() { playerEntityId = eid,spawnPosId=spawn };
        RoomInfoList[id] = roomInfo;
        return roomInfo;
    }
    public void RemovePlayerInfo(short rid)
    {
        if (RoomInfoList.TryGetValue(rid, out var info))
        {
            var eid = info.playerEntityId;
            EntityContainer.DestroyEntity(eid);
            PlayerInfoList.Remove(rid);
            RoomInfoList.Remove(rid);
        }
    }
    public void ReceiveCommand(short id,CSInputCommand command)
    {
        if (RoomInfoList.TryGetValue(id, out var roomInfo))
        {
            if (EntityContainer.Players.TryGetObject(roomInfo.playerEntityId, out var entity))
            {
                if (entity is PlayerData player)
                {
                    player.MoveTo(command);
                }
                else Debug.LogError($"玩家实体{id}类型错误");
            }
            else Debug.LogError($"未找到玩家实体{id}");
        }
        else Debug.LogError($"未找到{id}对应的roomInfo");
    }
    public void ReceiveUseSkill(short id, int skillId)
    {
        ReceiveUseSkillInternal(id, skillId, null);
    }
    public void ReceiveUseSkill(short id, int skillId, Vector3 dest)
    {
        ReceiveUseSkillInternal(id, skillId, dest);
    }
    private void ReceiveUseSkillInternal(short id, int skillId, Vector3? dest)
    {
        if (!RoomInfoList.TryGetValue(id, out var roomInfo))
        {
            Debug.LogError($"未找到{id}对应的roomInfo");
            return;
        }
        if (!PlayerInfoList.TryGetValue(id, out var playerInfo))
        {
            Debug.LogError($"未找到{id}对应的playerInfo");
            return;
        }
        if (!EntityContainer.Players.TryGetObject(roomInfo.playerEntityId, out var entity))
        {
            Debug.LogError($"未找到玩家实体{id}");
            return;
        }
        var player = entity as PlayerData;
        if (player == null)
        {
            Debug.LogError($"玩家实体{id}类型错误");
            return;
        }
        if (skillId < 0 || playerInfo.skills == null)
        {
            Debug.LogWarning($"玩家{id}使用了无效技能：{skillId}");
            return;
        }

        DecreasePlayerRuneCount(playerInfo, skillId);

        var output = dest.HasValue
            ? SkillManager.DoDamageActs(skillId, player, dest.Value)
            : SkillManager.DoDamageActs(skillId, player);
        UseSkillBuffer(skillId, output.Item1, output.Item2);
    }

    private static void DecreasePlayerRuneCount(CSPlayerInfo playerInfo, int skillId)
    {
        if (playerInfo.skills.TryGetValue(skillId, out var count) && count > 0)
        {
            playerInfo.skills[skillId] = count - 1;
        }
    }

    public ushort SpawnEntity(EntityType type, int level, Vector3 pos)
        =>EntityContainer.SpawnEntity(type,level,pos);
    public bool DestroyEntity(ushort id) => EntityContainer.DestroyEntity(id);
    public void ShootBullet(EntityData shooter, float rate, BezierCurve curve, float radius, float lifeTime, Damageable.IDamageable damageable,Action<Action<EffectType,int,float>>addEffectEvent)
        =>BulletContainer.ShootBullet(shooter,rate,curve,radius,lifeTime,damageable,addEffectEvent);

    public void UseSkillBuffer(int skillId, Vector3 pos,Vector3 dest) 
        => OutputBuffer.OnUseSkill(skillId, pos, dest);
    public void ShowTextValue(ushort value, TextColor color,Vector3 pos)
        =>OutputBuffer.OnShowTextValue(value,color,pos);
    public void ShowTextLabel(string label,TextColor color,Vector3 pos)
        =>OutputBuffer.OnShowTextLabel(label,color,pos);

    #region//Loop
    private float lastRecordTime;
    private int frameCount;
    public override void ManagedUpdate()
    {
        if (Tool.AssetsManager != null) return;//仅仅服务器上生效
        frameCount++;
        if (Time.time - lastRecordTime > 1)
        {
            lastRecordTime=Time.time;
            Debug.Log("frame count:" + frameCount);
            frameCount = 0;
        }

        BulletContainer.RemoveExpiredBullets();
        UpdateEntities();
        ReallocateChuck();
        GameLogic();
        BulletHitCheck();
        HandleKilledEntities();
        FigureClientSyncArea();
        SyncNetwork();
        Clear();
    }
    private void UpdateEntities()
    {
        foreach(var entity in EntityContainer.Entities)
        {
            entity.OnUpdate();
        }
    }
    private void ReallocateChuck()
    {
        EntityContainer.Entities.RefreshAll();
        EntityContainer.Players.RefreshAll();
        EntityContainer.Npcs.RefreshAll();
        BulletContainer.Bullets.RefreshAll();
    }
    private HashSet<int> m_bulletBuffer=new HashSet<int>();
    private void BulletHitCheck()
    {
        foreach (var e in EntityContainer.Entities)
        {
            BulletContainer.Bullets.GetIdsInRelativeBlocks(e.transform.position, e.colliderInfo.radius, m_bulletBuffer);
            foreach(var bulletId in m_bulletBuffer)
            {
                var bullet = BulletContainer.Bullets[bulletId];
                if (!bullet.InDamageWindow) continue;
                if (bullet.shooter == e.id) continue;
                if (BulletHit(bullet, e)&&bullet.damageable.CanDamage(e.id))
                {
                    bool wasAlive = e.Alive;
                    ushort damage=FigureDamage(bullet, e,out var strike);
                    EntityContainer.Entities.TryGetObject(bullet.shooter, out var shooter);
                    e.OnDamaged(damage, shooter);
                    if (wasAlive && !e.Alive)
                    {
                        RegisterKillEvent(bullet.shooter, e);
                    }
                    if (bullet.addEffectEvent != null && e.effectController != null)
                        bullet.addEffectEvent.Invoke(e.effectController.AddEffect);
                    var color = strike ? TextColor.Red : TextColor.Orange;
                    ShowTextValue(damage, color, bullet.Position);
                }
            }
            m_bulletBuffer.Clear();
        }
    }
    private void RegisterKillEvent(ushort shooterEntityId, EntityData victim)
    {
        bool TryGetClientIdByEntityId(ushort entityId, out short clientId)
        {
            foreach (var pair in RoomInfoList)
            {
                if (pair.Value.playerEntityId != entityId) continue;
                clientId = pair.Key;
                return true;
            }
            clientId = -1;
            return false;
        }

        if (!TryGetClientIdByEntityId(shooterEntityId, out var killerClientId)) return;

        byte eventType;
        if (victim.type.IsCharacter()) eventType = NetworkEvent.KillPlayer;
        else if (victim.type.IsNpc()) eventType = NetworkEvent.KillZombie;
        else if (victim.type.IsPlant()) eventType = NetworkEvent.KillPlant;
        else if (victim.type.IsInfectedPlant()) eventType = NetworkEvent.KillInfectedPlant;
        else if (victim.type.IsOre()) eventType = NetworkEvent.KillOre;
        else if (victim.type.IsInfectedOre()) eventType = NetworkEvent.KillInfectedOre;
        else if (victim.type.IsInfection()) eventType = NetworkEvent.KillInfection;
        else return;

        OutputBuffer.AddEvent(killerClientId, eventType, 1);

        if (!victim.type.IsCharacter()) return;
        if (!TryGetClientIdByEntityId(victim.id, out var victimClientId)) return;
        if (!PlayerInfoList.TryGetValue(victimClientId, out var victimInfo)) return;

        var result = new Dictionary<int, int>();
        foreach (var rune in victimInfo.skills)
        {
            if (rune.Value <= 0) continue;
            result[rune.Key] = rune.Value;
        }

        Tool.NetworkManager.PvpKillRewardRpc(killerClientId, new SCPvpKillRewardInfo
        {
            victimType = victimInfo.type,
            runes = result
        });
    }

    private void HandleKilledEntities()
    {
        foreach(var i in EntityData.KilledEntities)
        {
            i.OnKilled();
            EntityContainer.DestroyEntity(i.id);
        }
        EntityData.KilledEntities.Clear();
    }
    private Dictionary<short, HashSet<Vector2Int>> m_chucks = new Dictionary<short, HashSet<Vector2Int>>(); 
    private ObjectPool<HashSet<Vector2Int>> ChunkIndexPool = new ObjectPool<HashSet<Vector2Int>>(() => new HashSet<Vector2Int>(), set => set.Clear());
    private void FigureClientSyncArea()
    {
        foreach(var i in RoomInfoList)
        {
            var eid = i.Value.playerEntityId;
            if (!EntityContainer.Entities.TryGetObject(eid, out var entity)) continue;
            var chunkSet = ChunkIndexPool.Get();
            ChunkSearcher<EntityData>.GetAllRelativeBlocks(entity.transform.position, Landscape.chunkSize, chunkSet);
            m_chucks.Add(i.Key, chunkSet);
        }
    }
    private HashSet<TextValueInfo> m_textValues = new();
    private HashSet<TextLabelInfo> m_textLabels = new();
    private HashSet<UseSkillInfo> m_useSkills = new();
    private HashSet<EntityData> m_entityDisplayInfos = new();
    private void SyncNetwork()
    {
        foreach(var i in m_chucks.Keys)
        {
            foreach (var c in m_chucks[i])
            {
                EntityContainer.Entities.GetObjectsInChunk(c, m_entityDisplayInfos, true);
                OutputBuffer.ReadChunkInfo(c, m_textValues, m_textLabels, m_useSkills);
            }
            foreach (var entity in m_entityDisplayInfos)
            {
                if (entity == null || !entity.Alive) continue;
                Tool.NetworkManager.UpdateInfoRpc(i, entity.GetDisplayInfo());
            }
            foreach(var info in m_textValues)
            {
                Tool.NetworkManager.ShowTextRpc(i,info);
            }
            foreach (var info in m_textLabels)
            {
                Tool.NetworkManager.ShowTextRpc(i, info);
            }
            foreach (var info in m_useSkills)
            {
                Tool.NetworkManager.UseSkillRpc(i, info);
            }
            m_entityDisplayInfos.Clear();
            m_textValues.Clear();
            m_textLabels.Clear();
            m_useSkills.Clear();
        }
        foreach(var i in m_chucks.Values)
            ChunkIndexPool.Release(i);
        m_chucks.Clear();

        foreach(var kvp in OutputBuffer.NetworkEvents)
        {
            Tool.NetworkManager.SyncNetworkEventRpc(kvp.Key, kvp.Value);
        }
    }
    private void Clear()
    {
        OutputBuffer.FrameEndClear();
    }
    #endregion

    /// <summary>
    /// 运行时在 Scene 视图绘制子弹调试信息的开关（默认开启）。
    /// OnDrawGizmos 仅在编辑器中渲染，发布版本零开销。
    /// </summary>
    public static bool DrawBulletDebug = true;
    /// <summary>参考目标碰撞半径：命中范围 = 子弹半径 + 该值（0.4=玩家，可改为 0.3 NPC / 0.45 植物 / 1.22 矿石 / 4.82 感染体）</summary>
    private const float RefTargetRadius = 0.4f;
    /// <summary>弹道曲线采样段数</summary>
    private const int TrajectorySegments = 16;

    private void OnDrawGizmos()
    {
        if (!DrawBulletDebug) return;
        foreach (var b in BulletContainer.Bullets)
        {
            DrawBulletGizmos(b);
        }
    }

    /// <summary>
    /// 绘制单个子弹的调试信息：
    /// 1) 黄色/灰色折线 = 贝塞尔弹道曲线（黄=伤害窗口内，灰=窗口外）
    /// 2) 红色/灰色线框球 = 子弹判定半径（BulletHit 实际使用的 b.radius）
    /// 3) 橙色/灰色线框球 = 实际命中范围（子弹半径 + 参考目标碰撞半径）
    /// </summary>
    private static void DrawBulletGizmos(Bullet b)
    {
        bool inWindow = b.InDamageWindow;

        // 1) 弹道曲线
        Color curveColor = inWindow ? new Color(1f, 0.9f, 0.2f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.35f);
        Vector3 prev = b.curve.Lerp(0f);
        Gizmos.color = curveColor;
        for (int i = 1; i <= TrajectorySegments; i++)
        {
            Vector3 next = b.curve.Lerp(i / (float)TrajectorySegments);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // 2) 子弹判定半径
        Gizmos.color = inWindow ? new Color(1f, 0.25f, 0.25f, 0.6f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(b.Position, b.radius);

        // 3) 实际命中范围 = 子弹半径 + 参考目标半径
        Gizmos.color = inWindow ? new Color(1f, 0.55f, 0.1f, 0.6f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(b.Position, b.radius + RefTargetRadius);
    }

    public static class BulletContainer
    {
        public static ChunkSearcher<Bullet> Bullets = new(d => d.Position);
        private static int bulletId;
        public static void ShootBullet(EntityData shooter, float rate, BezierCurve curve, float radius, float lifeTime, Damageable.IDamageable damageable,Action<Action<EffectType, int, float>>addEffectEvent)
        {
            var b = new Bullet()
            {
                shooter = shooter.id,
                rate = rate,
                curve = curve,
                radius = radius,
                lifeTime = lifeTime,
                damageable = damageable,
                addEffectEvent = addEffectEvent,
                attribute = shooter.floatingAttribute,
                spawnTime = Time.time
            };
            Bullets.Add(bulletId++, b);
            if (bulletId > 2000000000) bulletId = 0;
        }
        private static List<int> toRemove = new List<int>();
        public static void RemoveExpiredBullets()
        {
            foreach (var id in Bullets.Keys)
            {
                var b = Bullets[id];
                if (Time.time - b.spawnTime > b.lifeTime)
                {
                    toRemove.Add(id);
                }
            }
            foreach (var id in toRemove)
            {
                Bullets.Remove(id);
            }
            toRemove.Clear();
        }
    }
    public static class EntityContainer
    {
        private static ushort entityId;

        public static ChunkSearcher<EntityData> Entities = new(d => d.transform.position);
        public static ChunkSearcher<EntityData> Players = new(d => d.transform.position);
        public static ChunkSearcher<EntityData> Npcs = new(d => d.transform.position);
        public static ChunkSearcher<EntityData> OresAndPlants = new(d => d.transform.position);
        public static ChunkSearcher<EntityData> Infections = new(d => d.transform.position);
        public static ushort SpawnEntity(EntityType type, int level, Vector3 pos)
        {
            var data = EntityPool.GetTemplate(type);
            var id = entityId;
            data.OnCreate(id, type, level);
            data.transform.position = pos;
            do
            {
                entityId++;
                if (entityId > 30000) entityId = 0;
            } while (Entities.Contains(entityId));

            Entities.Add(id, data);
            if (type.IsCharacter()) Players.Add(id, data);
            else if (type.IsNpc()) Npcs.Add(id, data);
            else if (type.AnyOre() || type.AnyPlant()) OresAndPlants.Add(id, data);
            else if (type.IsInfection()) Infections.Add(id, data);

            return id;
        }
        public static bool DestroyEntity(ushort id)
        {
            if (Entities.TryGetObject(id, out var data))
            {
                var type = data.type;
                Entities.Remove(id);
                if (type.IsCharacter()) Players.Remove(id);
                else if (type.IsNpc()) Npcs.Remove(id);
                else if (type.AnyOre() || type.AnyPlant()) OresAndPlants.Remove(id);
                else if (type.IsInfection()) Infections.Remove(id);
                EntityPool.ReturnTemplate(data);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    private static class OutputBuffer
    {
        public const float availableRadius = 15f;

        public static Dictionary<short, NetworkEvent> NetworkEvents = new Dictionary<short, NetworkEvent>();
        private static ChunkSearcher<TextValueInfo> TextValueBuffer = new(info => info.pos);
        private static ChunkSearcher<TextLabelInfo> TextLabelBuffer = new(info => info.pos);
        private static ChunkSearcher<UseSkillInfo> UseSkillBuffer = new(info => info.pos);

        private static int id;
        private static int NextId
        {
            get
            {
                id++;
                if (id == 2000000000) id = 0;
                return id;
            }
        }
        
        public static void AddEvent(short id,byte eventType,byte value)
        {
            if(!NetworkEvents.TryGetValue(id, out var info))
            {
                info=new NetworkEvent();
                NetworkEvents.Add(id, info);
            }
            if(info.type.TryGetValue(eventType,out var v))
            {
                int r = v + value;
                if (r > byte.MaxValue)
                {
                    Debug.LogWarning($"玩家{id}的事件{eventType}超过最大值");
                    r=byte.MaxValue;
                }
                info.type[eventType] = (byte)r;
            }
            else
            {
                info.type[eventType] = value;
            }
        }
        public static void OnUseSkill(int id,Vector3 pos,Vector3 dest)
        {
            UseSkillBuffer.Add(NextId,new UseSkillInfo() { id=id, pos=pos, dest=dest });
        }
        public static void OnShowTextValue(ushort value, TextColor color, Vector3 pos)
        {
            TextValueBuffer.Add(NextId, new TextValueInfo() { value = value, color = color, pos = pos });
        }
        public static void OnShowTextLabel(string label, TextColor color, Vector3 pos)
        {
            TextLabelBuffer.Add(NextId, new TextLabelInfo() { label=label, color = color, pos = pos });
        }
        public static void ReadChunkInfo(Vector2Int chunk,HashSet<TextValueInfo> textValues,HashSet<TextLabelInfo>textLabels,HashSet<UseSkillInfo>useSkills)
        {
            // Merge the lists returned by the chunk searcher into the provided sets
            TextValueBuffer.GetObjectsInChunk(chunk, textValues, true);
            TextLabelBuffer.GetObjectsInChunk(chunk, textLabels, true);
            UseSkillBuffer.GetObjectsInChunk(chunk, useSkills, true);
        }
        public static void FrameEndClear()
        {
            NetworkEvents.Clear();
            TextValueBuffer.Clear();
            TextLabelBuffer.Clear();
            UseSkillBuffer.Clear();
        }
    }
}
