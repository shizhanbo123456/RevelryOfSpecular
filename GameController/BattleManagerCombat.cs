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
    private readonly List<Bullet> activeBullets = new();
    private static readonly EntityData[] s_bulletBuffer = new EntityData[16];

    private void AddBullet(AttackData attack, BulletTrajectory trajectory, float lifeTime)
    {
        if (attack == null || trajectory == null) return;
        activeBullets.Add(new Bullet()
        {
            attack = attack,
            trajectory = trajectory,
            lifeTime = lifeTime > 0f ? lifeTime : trajectory.Duration, // 未传时长则用轨迹自带的
            spawnTime = Time.time,
            hitIds = new HashSet<ushort>(),
        });
    }

    private void TickBullets()
    {
        float now = Time.time;
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            var b = activeBullets[i];
            float t = now - b.spawnTime;
            if (t >= b.lifeTime)
            {
                activeBullets.RemoveAt(i);
                continue;
            }
            TryHitBullet(b, b.trajectory.Lerp(t / b.lifeTime));
        }
    }

    private void TryHitBullet(Bullet b, Vector3 pos)
    {
        var attack = b.attack;
#if UNITY_EDITOR
        DamageRangeDebugHost.Capsule(b.LastPosition, pos, attack.radius); // 调试：可视化本次胶囊伤害判定范围（渐隐渲染）
#endif
        int count = EntityPhysics.OverlapCapsule(b.LastPosition, pos, attack.radius, s_bulletBuffer);
        for (int i = 0; i < count; i++)
        {
            var e = s_bulletBuffer[i];
            if (!e.Alive) continue;
            if (e.id == attack.shooter || !EntityCampUtil.IsHostile(attack.shooterCamp, e.camp)) continue;
            if (!b.hitIds.Add(e.id)) continue; // 同一发子弹对同一目标只结算一次

            int damage = attack.GetDamage(out bool isCrit);
            // 击飞方向取子弹上一帧位置：高速弹一帧穿过目标时，用当前位置算方向会反转
            Vector3 floatPos = new Vector3(e.transform.position.x, b.LastPosition.y, e.transform.position.z);
            e.ProcessHit(attack, damage, isCrit, b.LastPosition, floatPos);
            if (attack.addEffectEvent != null && e.effectController != null)
            {
                attack.addEffectEvent.Invoke(e.effectController);
            }
            if (attack.onHit != null) attack.onHit.Invoke(e);
        }
    }
    #endregion

    #region 死亡 / 复活 / 愈战愈勇
    private class ReviveState
    {
        public int deathCount;
        public float progress;
        public float lastSentProgress = -1f;
    }
    private readonly Dictionary<short, ReviveState> reviveStates = new();

    public readonly Dictionary<short, int> KillCountByClient = new();

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
            SendReviveProgress(owner, rs, entity.id, ready: false);
        }

        // 先播死亡动画、再物理销毁：有动画的实体（角色/僵尸）延后到 AnimDieEvent；
        // 无动画实体（水晶/守护点/防御塔等）立即销毁。见 TickDying
        BeginDying(entity);

        // 守护点的分层减伤重算在其子类 OnKilled 里处理（该方法先于本方法调用）
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

    public void TryDropCrystalWeapon(EntityData crystal)
    {
        var killer = crystal.lastAttacker;
        if (killer == null) return;
        if (!EntityOwnerClient.TryGetValue(killer.id, out var clientId)) return;
        if (!PlayerEntityId.TryGetValue(clientId, out var playerId)) return;
        if (!EntityContainer.Entities.TryGetObject(playerId, out var player) || player.skillController == null) return;
        if (UnityEngine.Random.value > Config.crystal_skill_drop_chance) return;

        int weaponId = Config.GetRandomWeaponId(crystal.type.value); // 类别由函数内部按 % crystal_type_count 推出
        int slotMax = player.floatingAttribute.weaponSlotCount;
        var sc = player.skillController;
        if (sc.GetSkillIds().Contains(weaponId))
        {
            sc.AddWeaponExp(weaponId);
            NotifyPlayer(clientId, 15); // 武器升级
        }
        else if (sc.GetSkillIds().Count < slotMax)
        {
            sc.AddSkill(weaponId);
            NotifyPlayer(clientId, 14); // 获得新武器
        }
        else
        {
            sc.AddWeaponExpToRandom();
            NotifyPlayer(clientId, 13); // 槽满转经验
        }
    }

    // 战斗事件列表的纯文字提示（文本走 NoticeMessageMap）
    private void NotifyPlayer(short clientId, int messageId)
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
            short clientId = pair.Key;
            if (PlayerEntityId.ContainsKey(clientId)) continue; // 已复活

            var rs = pair.Value;
            if (!PlayerCamp.TryGetValue(clientId, out var camp)) continue;
            if (camp == EntityCamp.Attack)
            {
                float rate = EnvironmentManager.IsDay
                    ? Config.revive_day_progress_per_second
                    : Config.revive_night_progress_per_second;
                var mults = Config.revive_progress_multiplier_by_death;
                float mult = mults[Mathf.Min(Mathf.Max(rs.deathCount - 1, 0), mults.Length - 1)];
                rs.progress += dt * rate * mult * AttackReviveFactor; // PC102 被动「进攻方复活速度减慢」
            }
            else
            {
                rs.progress += dt / Config.defense_revive_duration;
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

    private void RespawnPlayer(short clientId, ReviveState rs)
    {
        if (!PlayerCamp.TryGetValue(clientId, out var camp)) return;
        if (!PlayerInfoList.TryGetValue(clientId, out var info)) return;
        bool isAttack = camp == EntityCamp.Attack;
        EntityType characterType = isAttack ? info.attackCharacter : info.defenseCharacter;
        int level = isAttack ? info.attackLevel : info.defenseLevel;

        var spawns = Tool.LandscapeSpawns;
        // 复活点：与开局出生共用同一个「出生/复活位置列表」，随机取点（不去重）
        Vector3 pos = isAttack
            ? LandscapeSpawns.RandomOf(spawns.attackPositions)
            : LandscapeSpawns.RandomOf(spawns.defensePositions);

        ushort entityId = SpawnEntity(characterType, level, pos, camp);
        if (entityId == 0)
        {
            rs.progress = 1f; // 模板缺失：保持攒满状态稍后重试
            return;
        }
        PlayerEntityId[clientId] = entityId;
        EntityOwnerClient[entityId] = clientId;

        var data = GetEntity(entityId);
        if (data != null) data.aiControlled = AIClients.Contains(clientId);

        // 愈战愈勇：第 n 条命层数（序列见 Config，永久 Buff）
        var stacks = Config.yz_stack_by_life;
        int yz = stacks[Mathf.Min(rs.deathCount, stacks.Length - 1)];
        if (yz > 0 && data != null)
        {
            if (data.effectController != null) data.effectController.AddEffect(EffectType.YzCy, yz, float.MaxValue);
        }

        rs.progress = 0f; // deathCount 保留，供下次倍率与叠层
        rs.lastSentProgress = -1f;
        Tool.NetworkManager.SendBattleInfo(clientId, new SCBattleInfo()
        {
            playerEntityId = entityId,
            camp = camp,
            characterType = characterType,
        });
        SendReviveProgress(clientId, rs, entityId, ready: true);

        // 复活播报（事件列表，icon=玩家复活，文本=该玩家名）
        Tool.NetworkManager.SendBattleEvent(new SCBattleEvent()
        {
            type = SCBattleEvent.Type.PlayerRespawn,
            textId = SCBattleEvent.PlayerNameBase + clientId,
        });
    }

    private void SendReviveProgress(short clientId, ReviveState rs, ushort entityId, bool ready)
    {
        rs.lastSentProgress = rs.progress;
        Tool.NetworkManager.SendReviveInfo(clientId, new SCReviveInfo()
        {
            entityId = entityId,
            progress = Mathf.Clamp01(rs.progress),
            ready = ready,
            yzStack = Config.yz_stack_by_life[Mathf.Min(rs.deathCount, Config.yz_stack_by_life.Length - 1)],
        });
    }

    private void ClearBattleState()
    {
        activeBullets.Clear();
        reviveStates.Clear();
        plagueTreeRespawnTime = -1f; // 由 SpawnBattleWorld 重新排首次刷新
        HarvestByClient.Clear();
        KillCountByClient.Clear();
        minimapRadiusByClient.Clear(); // 每局雷达显示半径回到默认档
    }
    #endregion
}
