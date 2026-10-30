using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public partial class BattleManager
{
    #region 服务器权威移动（双手键盘：角色相对移动 + 渐转，朝向服务器权威）
    private void TickMovement()
    {
        float dt = Time.deltaTime;
        foreach (var e in EntityContainer.Entities)
        {
            if (e == null || !e.Alive) continue;
            if (e.anim == null) continue; // 无动画单位不移动（与 EntityData.SetupBody 同一判据）
            e.TickVelocity(dt);
        }
    }
    #endregion

    #region 子弹容器（时间戳推进：位置 = 轨迹 Lerp(经过时长/生命)，不做增量移动）
    private const float AllTimeWindow = 999f; // hit.end = 0 表示全程都在伤害窗口内
    private readonly List<Bullet> activeBullets = new();
    private static readonly EntityData[] s_bulletBuffer = new EntityData[16];
    private static readonly HashSet<string> s_noHitReported = new(); // 缺命中球只报一次，避免每发刷屏

    private void AddBullet(AttackData attack, BulletTrajectory trajectory, float lifeTime, BulletHitSpec spec)
    {
        if (attack == null || trajectory == null) return;
        float now = Time.time;
        if (spec.Hits.Length == 0 && s_noHitReported.Add(spec.label ?? "未知特效"))
            Debug.LogError($"[子弹] {spec.label} 没有命中球（BulletVfxInfo.hits 为空）：这发子弹不会造成任何伤害，请补齐特效资产");

        activeBullets.Add(new Bullet()
        {
            attack = attack,
            trajectory = trajectory,
            lifeTime = lifeTime > 0f ? lifeTime : trajectory.Duration, // 未传时长则用轨迹自带的
            spawnTime = now,
            lastTickTime = now,
            spec = spec,
            lastHitTime = new Dictionary<ushort, float>(),
        });
    }

    private void TickBullets()
    {
        float now = Time.time;
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            var b = activeBullets[i];
            float age = b.Age(now);
            if (age >= b.lifeTime)
            {
                activeBullets.RemoveAt(i);
                continue;
            }

            // 只用到「时刻 → 轨迹位置」，不累计 deltaTime
            float prevAge = b.Age(b.lastTickTime);
            Vector3 pos = b.At(age);
            Vector3 prevPos = b.At(prevAge);
            Quaternion rot = FacingAt(b.trajectory, age, b.lifeTime);
            Quaternion prevRot = FacingAt(b.trajectory, prevAge, b.lifeTime);

            b.lastTickTime = now;
            activeBullets[i] = b; // 结构体写回采样时刻

            TryHitBullet(b, now, age, pos, prevPos, rot, prevRot);
        }
    }

    // 与客户端 BulletPlayer.Tangent 同一算法：取轨迹上稍后的一点定朝向
    private static Quaternion FacingAt(BulletTrajectory trajectory, float age, float lifeTime)
    {
        float inv = lifeTime > 0f ? 1f / lifeTime : 0f;
        Vector3 cur = trajectory.Lerp(Mathf.Clamp01(age * inv));
        Vector3 dir = trajectory.Lerp(Mathf.Clamp01((age + 0.02f) * inv)) - cur;
        return dir.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(dir) : Quaternion.identity;
    }

    private void TryHitBullet(Bullet b, float now, float age, Vector3 pos, Vector3 prevPos, Quaternion rot, Quaternion prevRot)
    {
        var attack = b.attack;
        var spec = b.spec;
        var hits = spec.Hits;
        for (int h = 0; h < hits.Length; h++)
        {
            var hit = hits[h];
            float end = hit.end <= 0f ? AllTimeWindow : hit.end;
            if (age < hit.start || age > end) continue; // 时间戳直接比较：窗口外不判定

            Vector3 offset = hit.center * spec.scale;
            Vector3 center = pos + rot * offset;
            Vector3 prevCenter = prevPos + prevRot * offset;
            float radius = hit.radius * spec.scale;
#if UNITY_EDITOR
            DamageRangeDebugHost.Capsule(prevCenter, center, radius); // 调试：可视化本段命中球扫掠范围
#endif
            int count = EntityPhysics.OverlapCapsule(prevCenter, center, radius, s_bulletBuffer);
            for (int i = 0; i < count; i++)
            {
                var e = s_bulletBuffer[i];
                if (e == null || !e.Alive) continue;
                if (e.id == attack.shooter || !EntityCampUtil.IsHostile(attack.shooterCamp, e.camp)) continue;
                // 同一目标按伤害间隔冷却重复结算；间隔 <=0 时一目标只结算一次
                if (b.lastHitTime.TryGetValue(e.id, out float lastHit) &&
                    (spec.interval <= 0f || now - lastHit < spec.interval)) continue;
                b.lastHitTime[e.id] = now;

                int damage = attack.GetDamage(out CombatLabelType type, spec.rates != null ? spec.rates[h] : 1f);
                // 击飞方向取上一帧的命中球心：高速弹一帧穿过目标时，用当前位置算方向会反转
                Vector3 floatPos = new Vector3(e.transform.position.x, prevCenter.y, e.transform.position.z);
                e.ProcessHit(attack, damage, type, prevCenter, floatPos);
                if (attack.addEffectEvent != null && e.effectController != null)
                {
                    attack.addEffectEvent.Invoke(e.effectController);
                }
                if (attack.onHit != null) attack.onHit.Invoke(e);
            }
        }
    }
    #endregion

    #region 死亡 / 复活 / 愈战愈勇
    private class ReviveState
    {
        public int deathCount;
        public float progress;
        public float lastSentProgress = -1f;
        public EntitySkillController.Loadout? loadout; // 死亡瞬间的技能快照，复活重建实体后归还
    }
    private readonly Dictionary<int, ReviveState> reviveStates = new();

    public readonly Dictionary<int, int> KillCountByClient = new();

    private void HandleDeath(EntityData entity)
    {
        // 击杀归属：击杀者由 lastAttacker 反查（归属已随死亡移除时算不出，例如同归于尽 → 不计）
        var killer = entity.lastAttacker;
        bool victimIsPlayer = entity.type.category == EntityCategory.Character_Attack ||
                              entity.type.category == EntityCategory.Character_Defense;
        if (victimIsPlayer && killer != null && EntityCampUtil.IsHostile(killer.camp, entity.camp) &&
            EntityOwnerClient.TryGetValue(killer.id, out var killerClient))
        {
            KillCountByClient.TryGetValue(killerClient, out int killCount);
            KillCountByClient[killerClient] = killCount + 1;
        }

        // 玩家死亡才进事件列表：有归属击杀 = 玩家间击败，无归属 = 无源死亡
        if (victimIsPlayer && EntityOwnerClient.TryGetValue(entity.id, out var victimOwner))
        {
            var e = killer != null && EntityOwnerClient.TryGetValue(killer.id, out var killerOwner)
                ? new SCBattleEvent()
                {
                    type = SCBattleEvent.Type.PlayerKill,
                    textId = SCBattleEvent.PlayerNameBase + killerOwner,
                    textId2 = SCBattleEvent.PlayerNameBase + victimOwner,
                }
                : new SCBattleEvent()
                {
                    type = SCBattleEvent.Type.DeathUnattributed,
                    textId = SCBattleEvent.PlayerNameBase + victimOwner,
                };
            Tool.NetworkManager.SendBattleEvent(e);
        }

        if (EntityOwnerClient.TryGetValue(entity.id, out var owner))
        {
            EntityOwnerClient.Remove(entity.id);
            PlayerEntityId.Remove(owner); // 复活时重建并重新绑定

            if (!reviveStates.TryGetValue(owner, out var rs))
            {
                rs = new ReviveState();
                reviveStates[owner] = rs;
            }
            rs.deathCount++;
            rs.progress = 0f;
            rs.lastSentProgress = -1f;
            if (entity.skillController != null) rs.loadout = entity.skillController.CaptureLoadout();
            SendReviveProgress(owner, rs, entity.id, ready: false);
        }

        // 先播死亡动画、再物理销毁：有动画的实体（角色/僵尸）延后到 AnimDieEvent；
        // 无动画实体（珊瑚/雕像/水晶雕像等）立即销毁。见 TickDying
        BeginDying(entity);

        // 雕像的分层减伤重算在其子类 OnKilled 里处理（该方法先于本方法调用）
        if (entity.type.category == EntityCategory.Character_Attack)
        {
            CheckAttackWiped(); // 进攻方全灭 → 立即按分数结算（策划案 17.2，不直接判负）
        }
    }

    private readonly List<EntityData> dyingEntities = new();

    private void BeginDying(EntityData entity)
    {
        if (entity == null) return;
        if (entity.anim == null)
        {
            DestroyEntity(entity.id);
            return;
        }
        if (!dyingEntities.Contains(entity)) dyingEntities.Add(entity);
    }

    private void TickDying()
    {
        for (int i = dyingEntities.Count - 1; i >= 0; i--)
        {
            var entity = dyingEntities[i];
            if (entity == null) // 已被其它路径销毁（对局结束清理等）
            {
                dyingEntities.RemoveAt(i);
                continue;
            }
            if (!entity.deathAnimDone) continue;
            dyingEntities.RemoveAt(i);
            DestroyEntity(entity.id);
        }
    }

    private void CheckAttackWiped()
    {
        foreach (var e in EntityContainer.Entities)
        {
            if (e != null && e.Alive && e.type.category == EntityCategory.Character_Attack) return;
        }
        EndBattle(AttackScore >= DefenseScore() ? 1 : 2);
    }

    // 珊瑚被摧毁：先按尺寸给击杀者回血（与是否掉落、掉落结果无关），再走掉落
    public void OnCoralDestroyed(EntityData coral)
    {
        HealCoralKiller(coral);
        TryDropCoralWeapon(coral);
    }

    // 击败珊瑚的玩家角色实体（含 AI 代打）；击杀者未归属任何客户端时返回 null
    private EntityData GetCoralKillerPlayer(EntityData coral, out int clientId)
    {
        clientId = -1;
        var killer = coral.lastAttacker;
        if (killer == null) return null;
        if (!EntityOwnerClient.TryGetValue(killer.id, out clientId)) return null;
        if (!PlayerEntityId.TryGetValue(clientId, out var playerId)) return null;
        return EntityContainer.Entities.TryGetObject(playerId, out var player) ? player : null;
    }

    // 小/大珊瑚各配一个固定恢复量（global_variables），夹到生命上限
    private void HealCoralKiller(EntityData coral)
    {
        var player = GetCoralKillerPlayer(coral, out _);
        if (player == null || !player.Alive) return;
        float heal = coral.type.value <= 0
            ? ConfigManager.GlobalVariableConfig.coral_small_regeneration
            : ConfigManager.GlobalVariableConfig.coral_large_regeneration;
        if (heal <= 0f) return;
        player.floatingAttribute.health = Mathf.Min(player.baseAttribute.health, player.floatingAttribute.health + heal);
        player.healthDirty = true;
    }

    // 掉武器：服务器只记持有，已持有就不重复加；能装进技能栏几个由客户端决定
    public void TryDropCoralWeapon(EntityData coral)
    {
        var player = GetCoralKillerPlayer(coral, out int clientId);
        if (player == null || player.skillController == null) return;

        float chance = coral.type.value <= 0
            ? ConfigManager.GlobalVariableConfig.coral_small_drop_chance
            : ConfigManager.GlobalVariableConfig.coral_large_drop_chance;
        if (UnityEngine.Random.value > chance) return;

        int weaponId = Config.GetRandomWeaponId(); // 全部武器均匀随机
        var ids = player.skillController.GetSkillIds();
        if (ids.Contains(weaponId)) return;
        player.skillController.AddSkill(weaponId);
        NotifyPlayer(clientId, 14); // 获得新武器
    }

    // 战斗事件列表的纯文字提示（文本走 notice.csv）
    private void NotifyPlayer(int clientId, int messageId)
    {
        Tool.NetworkManager.SendBattleEvent(clientId, new SCBattleEvent()
        {
            type = SCBattleEvent.Type.Notice,
            textId = messageId,
        });
    }

    private void TickRevive()
    {
        float dt = UnityEngine.Time.deltaTime;
        foreach (var pair in reviveStates)
        {
            int clientId = pair.Key;
            if (PlayerEntityId.ContainsKey(clientId)) continue; // 已复活

            var rs = pair.Value;
            if (!PlayerCamps.TryGetValue(clientId, out var camp)) continue;

            // 复活时长固定看配表（第几次复活 → 秒数），不再区分昼夜
            var revive = ConfigManager.ReviveTimeConfig.GetClamped(Mathf.Max(1, rs.deathCount));
            if (camp == PlayerCampType.Attack)
            {
                rs.progress += dt / revive.attackReviveSeconds;
            }
            else
            {
                rs.progress += dt / revive.defenseReviveSeconds;
            }

            if (rs.progress >= 1f)
            {
                RespawnPlayer(clientId, rs);
            }
            else if (rs.progress - rs.lastSentProgress >= 0.05f) // 进度节流下发（5% 一档）
            {
                SendReviveProgress(clientId, rs, 0, ready: false);
            }
        }
    }

    private void RespawnPlayer(int clientId, ReviveState rs)
    {
        if (!PlayerCamps.TryGetValue(clientId, out var camp)) return;
        if (!PlayerInfoList.TryGetValue(clientId, out var info)) return;
        bool isAttack = camp == PlayerCampType.Attack;
        EntityType characterType = isAttack ? info.attackCharacter : info.defenseCharacter;
        int level = isAttack ? info.attackLevel : info.defenseLevel;

        var spawns = Tool.LandscapeSpawns;
        // 复活点：与开局出生共用同一个「出生/复活位置列表」，随机取点（不去重）
        Vector3 pos = isAttack
            ? LandscapeSpawns.RandomOf(spawns.attackPositions)
            : LandscapeSpawns.RandomOf(spawns.defensePositions);

        EntityCamp entityCamp = isAttack ? EntityCamp.Attack : EntityCamp.Defense;
        ushort entityId = SpawnEntity(characterType, level, pos, entityCamp);
        if (entityId == 0)
        {
            rs.progress = 1f; // 模板缺失：保持攒满状态稍后重试
            return;
        }
        PlayerEntityId[clientId] = entityId;
        EntityOwnerClient[entityId] = clientId;
        Tool.NetworkManager.SendPlayerEntity(clientId, entityId);
        ExitSpectate(clientId); // 复活即结束观战，视野回到自己实体

        var data = GetEntity(entityId);
        if (data != null && data.skillController != null && rs.loadout.HasValue)
        {
            data.skillController.ApplyLoadout(rs.loadout.Value); // 死亡无损失：技能带走，库存按上限回满
        }
        if (data != null) data.aiControlled = AIClients.Contains(clientId);

        // 愈战愈勇：第 n 条命层数（配表按死亡次数给出，永久 Buff）
        int fury = ConfigManager.BattleFuryLayerConfig.GetClamped(rs.deathCount).buffLayerCount;
        if (fury > 0 && data != null)
        {
            if (data.effectController != null) data.effectController.AddEffect(EffectType.BattleFury, fury, float.MaxValue);
        }

        rs.progress = 0f; // deathCount 保留，供下次倍率与叠层
        rs.lastSentProgress = -1f;
        // 复活换了新实体 id：客户端由 SCPlayerEntity 更新自己的实体 id 与名牌归属，不再单独下发会话信息
        SendReviveProgress(clientId, rs, entityId, ready: true);

        // 复活播报（事件列表，icon=玩家复活，文本=该玩家名）
        Tool.NetworkManager.SendBattleEvent(new SCBattleEvent()
        {
            type = SCBattleEvent.Type.PlayerRespawn,
            textId = SCBattleEvent.PlayerNameBase + clientId,
        });
    }

    private void SendReviveProgress(int clientId, ReviveState rs, ushort entityId, bool ready)
    {
        rs.lastSentProgress = rs.progress;
        Tool.NetworkManager.SendReviveInfo(clientId, new SCReviveInfo()
        {
            entityId = entityId,
            progress = Mathf.Clamp01(rs.progress),
            ready = ready,
            furyStack = ConfigManager.BattleFuryLayerConfig.GetClamped(rs.deathCount).buffLayerCount,
        });
    }

    private void ClearBattleState()
    {
        activeBullets.Clear();
        reviveStates.Clear();
        neutralStatueRespawnTime = -1f; // 由 SpawnBattleWorld 重新排首次刷新
        HarvestByClient.Clear();
        KillCountByClient.Clear();
        minimapRadiusByClient.Clear(); // 每局雷达显示半径回到默认档
        visionStates.Clear(); // 每局视野锚点与观战状态重置
        skillFailPromptTime.Clear();
        lastAttackerTotal = lastDefenseTotal = lastAttackerAlive = lastDefenseAlive = -1; // 每局重置，保证开局首帧必发
        System.Array.Clear(defensePointProgress, 0, defensePointProgress.Length);
        for (int i = 0; i < sentDefensePointProgress.Length; i++) sentDefensePointProgress[i] = -1; // 同上：开局必发一次
        plagueGuardReadyTime.Clear();
    }

    // 重连补发：把「已发送快照」作废，下一帧的变更检测会自然重播一次（值不变，对其它客户端无害）
    private void InvalidateBroadcastSnapshots()
    {
        lastAttackerTotal = lastDefenseTotal = lastAttackerAlive = lastDefenseAlive = lastSpectatorCount = -1;
        for (int i = 0; i < sentDefensePointProgress.Length; i++) sentDefensePointProgress[i] = -1;
    }

    #region 守护点开采进度（0~100，与视野解耦，变更即广播）
    private readonly int[] defensePointProgress = new int[4];       // 0 主点 / 1~3 副点
    private readonly int[] sentDefensePointProgress = { -1, -1, -1, -1 };

    private void TickDefensePointProgress()
    {
        foreach (var statue in EntityContainer.GuardStatues)
        {
            if (statue == null) continue;
            int index = DefensePointIndexOf(statue);
            if (index < 0) continue;
            defensePointProgress[index] = ProgressOf(statue);
        }

        bool changed = false;
        for (int i = 0; i < defensePointProgress.Length; i++)
        {
            if (defensePointProgress[i] == sentDefensePointProgress[i]) continue;
            sentDefensePointProgress[i] = defensePointProgress[i];
            changed = true;
        }
        if (!changed) return;

        Tool.NetworkManager.SendDefensePointProgress(new SCDefensePointProgress()
        {
            main = defensePointProgress[0],
            sub1 = defensePointProgress[1],
            sub2 = defensePointProgress[2],
            sub3 = defensePointProgress[3],
        });
    }

    private void MarkDefensePointDestroyed(EntityData statue)
    {
        int index = DefensePointIndexOf(statue);
        if (index >= 0) defensePointProgress[index] = 100;
    }

    private static int DefensePointIndexOf(EntityData statue)
    {
        if (statue.type == EntityType.KingStatue) return 0;
        if (statue.type.category != EntityCategory.GuardStatue) return -1;
        return statue.type.value >= 0 && statue.type.value < Config.sage_statue_count ? statue.type.value + 1 : -1;
    }

    // 满血 0、打空 100：血量归零单独判，避免浮点误差把最后一格停在 99
    private static int ProgressOf(EntityData statue)
    {
        float max = statue.baseAttribute.health;
        if (max <= 0f) return 0;
        float health = statue.floatingAttribute.health;
        if (health <= 0f) return 100;
        return Mathf.Clamp((int)(100f * (1f - health / max)), 0, 100);
    }
    #endregion

    #region 技能释放回执
    private const float SkillFailPromptInterval = 1f; // 同一客户端提示间隔，连点不刷屏
    private readonly Dictionary<int, float> skillFailPromptTime = new();

    // 技能失败回包：只有玩家看得懂的原因才提示（沉默/强控），AI 与配置错误不提示
    public void NotifySkillUseFailed(EntityData entity, EntitySkillController.UseResult result)
    {
        if (entity == null) return;
        int messageId;
        if (result == EntitySkillController.UseResult.Silenced) messageId = 30;
        else if (result == EntitySkillController.UseResult.Controlled) messageId = 31;
        else return;

        if (!EntityOwnerClient.TryGetValue(entity.id, out var clientId)) return; // AI / 非玩家没有客户端
        if (skillFailPromptTime.TryGetValue(clientId, out var last) && Time.time - last < SkillFailPromptInterval) return;
        skillFailPromptTime[clientId] = Time.time;
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendPrompt(clientId, messageId);
    }

    // 技能受理成功回执：客户端只凭这条包扣可用次数（AI / 无主实体没有客户端，直接跳过）
    public void NotifySkillUsed(EntityData entity, int skillId)
    {
        if (entity == null || skillId < 0) return;
        if (!EntityOwnerClient.TryGetValue(entity.id, out var clientId)) return;
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendSkillUsed(clientId, skillId);
    }
    #endregion

    private int lastAttackerTotal = -1, lastDefenseTotal = -1, lastAttackerAlive = -1, lastDefenseAlive = -1, lastSpectatorCount = -1;

    // 双方玩家（真人+AI）总人数、存活数与观战人数：ManagedUpdate 每帧比对，变更才下发（阵亡/复活/进出房/GM 击杀统一在此收口）
    private void TickCampAliveCount()
    {
        int aTotal = 0, dTotal = 0, aAlive = 0, dAlive = 0, sCount = 0;
        foreach (var pair in PlayerCamps)
        {
            if (pair.Value == PlayerCampType.Spectator)
            {
                sCount++; // 观战方不计入双方人数，单独统计
                continue;
            }
            bool attack = pair.Value == PlayerCampType.Attack;
            if (attack) aTotal++; else dTotal++;
            var data = PlayerEntityId.TryGetValue(pair.Key, out var eid) ? GetEntity(eid) : null;
            if (data != null && data.Alive)
            {
                if (attack) aAlive++; else dAlive++;
            }
        }
        if (aTotal == lastAttackerTotal && dTotal == lastDefenseTotal
            && aAlive == lastAttackerAlive && dAlive == lastDefenseAlive
            && sCount == lastSpectatorCount) return;
        lastAttackerTotal = aTotal;
        lastDefenseTotal = dTotal;
        lastAttackerAlive = aAlive;
        lastDefenseAlive = dAlive;
        lastSpectatorCount = sCount;
        Tool.NetworkManager.SendCampAliveCount(new SCCampAliveCount()
        {
            attackerTotal = aTotal,
            defenseTotal = dTotal,
            attackerAlive = aAlive,
            defenseAlive = dAlive,
            spectatorCount = sCount,
        });
    }
    #endregion
}
