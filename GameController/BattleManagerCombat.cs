using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 战斗核心（partial BattleManager）：服务器权威移动、子弹容器、近战、死亡复活与愈战愈勇。
/// 时间戳原则：CD/重生/子弹等尽量由时间戳外推；
/// 移动为角色相对移动 + 渐转（路径为曲线），按帧增量积分；仅动态项（渐转/位移速度、DoT tick）做必要的推进。
/// </summary>
public partial class BattleManager
{
    #region 服务器权威移动（双手键盘：角色相对移动 + 渐转，朝向服务器权威）
    /// <summary>
    /// 移动推进（Rigidbody 承载速度，服务器权威；全项目唯一的"设置速度"位置）：
    /// 水平速度由实体自己算（EntityData.ResolveMoveVelocity）—— 动画模块通过 EntityAnim.SetVelocity*
    /// 声明速度，未声明时在地面按 2 m/s² 衰减、空中保持水平速度；Y 完全不写（重力/被击飞/下落照常）。
    /// 这里只负责叠加 MotionBase 的 motionVelocity —— 位移效果**不吃速度参数**：冲刺/击退不该被减速 Buff 缩水。
    /// 朝向由实体自己推进（OnTickMove：玩家角色按输入渐转后直接赋 rotation，刚体旋转只锁 X/Z）。
    /// </summary>
    private void TickMovement()
    {
        float dt = Time.deltaTime;
        foreach (var e in EntityContainer.Entities)
        {
            if (e == null || !e.Alive) continue;
            if (e.anim == null) continue; // 无动画单位不移动（与 EntityData.SetupBody 同一判据）

            // 强控（麻痹/冰冻/定身）期间输入不生效；"位移锁输入"由 MotionCanMove 表达
            bool canInput = e.effectController == null || e.effectController.CanMove();

            e.OnTickMove(dt, canInput); // 朝向与输入推进交给实体自己（玩家角色在 PlayerEntityData）

            // 速度由实体自己决定（动画声明的速度 / 未声明时地面摩擦与空中保持，见 EntityData.ResolveMoveVelocity）。
            // **加速/减速/泥沼不在这里乘**：它们只作用于动画播放速度（EntityEffectController.ApplyAnimSpeedScale →
            // EntityAnim.SetMoveSpeedScale），对位移速度的影响由动画模块在声明速度时自行接入。
            e.SetMoveVelocity(e.ResolveMoveVelocity(dt, canInput) + e.motionVelocity);

            // 区块索引跟着走：范围的索敌查询（GetNearestEnemy 等）只查区块桶，不更新就会一直按出生区块找人。
            // 只有可移动单位会换区块，无动画单位出生后不再移动，无需同步。
            EntityContainer.Entities.UpdateObjectPosition(e.id);
        }
    }
    #endregion

    #region 子弹容器（时间戳推进：位置 = 轨迹 Lerp(经过时长/生命)，不做增量移动）
    private readonly List<Bullet> activeBullets = new();
    private static readonly EntityData[] s_bulletBuffer = new EntityData[16];

    /// <summary>登记子弹（ShootBullet 调用）。</summary>
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

    /// <summary>子弹推进：按时间戳算位置 → 命中检测 → 伤害结算；命中不消失，只在生命到期时移除。</summary>
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

    /// <summary>命中检测：以「上一帧位置 → 当前位置」的胶囊覆盖整段路径（防高速穿模），路径上的敌方各结算一次后继续飞。</summary>
    private void TryHitBullet(Bullet b, Vector3 pos)
    {
        var attack = b.attack;
        int count = EntityPhysics.OverlapCapsule(b.LastPosition, pos, attack.radius, s_bulletBuffer);
        for (int i = 0; i < count; i++)
        {
            var e = s_bulletBuffer[i];
            if (!e.Alive) continue;
            if (e.id == attack.shooter || !EntityCampUtil.IsHostile(attack.shooterCamp, e.camp)) continue;
            if (!b.hitIds.Add(e.id)) continue; // 同一发子弹对同一目标只结算一次

            float damage = attack.GetDamage(out bool isCrit);
            // 击飞方向取子弹上一帧位置：高速弹一帧穿过目标时，用当前位置算方向会反转
            e.ProcessHit(attack, damage, isCrit, b.LastPosition);
            if (attack.addEffectEvent != null && e.effectController != null)
            {
                attack.addEffectEvent.Invoke(e.effectController);
            }
            attack.onHit?.Invoke(e);
        }
    }
    #endregion

    #region 死亡 / 复活 / 愈战愈勇
    /// <summary>玩家复活状态（死亡即摧毁单位，复活时重建并回满；Buff 随摧毁消失不回添）。</summary>
    private class ReviveState
    {
        public int deathCount;
        public float progress;
        public float lastSentProgress = -1f;
    }
    private readonly Dictionary<short, ReviveState> reviveStates = new();

    /// <summary>
    /// 各玩家击杀数（按客户端 id）。只统计击杀**敌对阵营的玩家角色**（含 AI 玩家）—— 僵尸/防御塔/水晶/守护点不计。
    /// 每人死亡数见 <see cref="ReviveState.deathCount"/>（复活叠层用）。暂不下发客户端。
    /// </summary>
    public readonly Dictionary<short, int> KillCountByClient = new();

    /// <summary>死亡统一处理：进入复活流程并下发进度；水晶排重生/掉武器并广播采集事件（被「蘑菇感染」的水晶被进攻方摧毁时无产出，走 CrystalBroken）；守护点重算分层减伤；销毁时机交给 BeginDying（等死亡动画播完）。</summary>
    private void HandleDeath(EntityData entity)
    {
        // 水晶的产出与重生排程在其子类 OnKilled 里处理（该方法先于本方法调用）
        Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.Kill);

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

        // 先播死亡动画、再物理销毁：有动画的实体（角色/僵尸）延后到 AnimDieEvent 或超时；
        // 无动画实体（水晶/守护点/防御塔等）立即销毁。见 TickDying
        BeginDying(entity);

        // 守护点的分层减伤重算在其子类 OnKilled 里处理（该方法先于本方法调用）
        if (entity.type.category == EntityCategory.Character_Attack)
        {
            CheckAttackWiped(); // 进攻方全灭 → 立即按分数结算（策划案 17.2，不直接判负）
        }
    }

    /// <summary>正在等死亡动画播完的实体（尚未物理销毁；见 BeginDying）。</summary>
    private readonly List<EntityData> dyingEntities = new();

    /// <summary>
    /// 死亡销毁入口：有动画 → 等死亡动画播完再销毁；无动画（水晶/守护点/防御塔等）→ 立即销毁。
    /// 延后销毁期间实体仍在容器里，但 Alive 已为 false，索敌/受击/移动都会跳过它（见各处 Alive 过滤）。
    /// </summary>
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

    /// <summary>死亡动画推进：AnimDieEvent 播完（deathAnimDone）后真正销毁实体。</summary>
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

    /// <summary>进攻方角色全部阵亡 → 立即按当前分数结算（拆除量过半则可能判胜）。</summary>
    private void CheckAttackWiped()
    {
        foreach (var e in EntityContainer.Entities)
        {
            if (e != null && e.Alive && e.type.category == EntityCategory.Character_Attack) return;
        }
        EndBattle(AttackScore >= DefenseScore() ? 1 : 2);
    }

    /// <summary>
    /// 水晶掉武器（策划案第七章：摧毁水晶 15% 概率获得该类型中的一把）。
    /// 已持有 → 该武器经验 +1；未持有且槽未满 → 入槽；未持有但槽满 → 转经验随机分配（策划案 5.2）。
    /// 仅真人玩家（有归属客户端的攻击者）可拾取。
    /// </summary>
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

    /// <summary>给指定玩家发文字提示（飘字，走 NoticeMessageMap）。</summary>
    private void NotifyPlayer(short clientId, int messageId)
    {
        Tool.NetworkManager.SendBattleEvent(clientId, new SCBattleEvent()
        {
            type = SCBattleEvent.Type.ShowText,
            value = messageId,
        });
    }

    /// <summary>
    /// 复活推进：进攻方进度 = 昼夜速率 × 死亡倍率（1/0.8/0.6/0.4/0.3/0.2...，最低 0.2）；
    /// 防守方固定 25s（昼夜一样）。攒满即复活（速率制，无黎明等待）。
    /// </summary>
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

    /// <summary>复活玩家：重建实体（属性自然回满）、叠愈战愈勇、重新绑定 id 并下发开局信息。</summary>
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

        // 愈战愈勇：第 n 条命层数（序列见 Config，永久 Buff）
        var stacks = Config.yz_stack_by_life;
        int yz = stacks[Mathf.Min(rs.deathCount, stacks.Length - 1)];
        if (yz > 0 && data != null)
        {
            data.effectController?.AddEffect(EffectType.YzCy, yz, float.MaxValue);
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

    /// <summary>开战清空战斗运行状态（id 源、子弹、移动、复活、世界重生、经验统计）。</summary>
    private void ClearBattleState()
    {
        activeBullets.Clear();
        reviveStates.Clear();
        crystalRespawns.Clear();
        plagueTreeRespawnTime = -1f; // 由 SpawnBattleWorld 重新排首次刷新
        HarvestByClient.Clear();
        KillCountByClient.Clear();
    }
    #endregion
}
