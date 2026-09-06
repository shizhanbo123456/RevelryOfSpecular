using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 战斗核心（partial BattleManager）：服务器权威移动、子弹容器、近战、死亡复活与愈战愈勇。
/// 时间戳原则：位置/CD/重生等尽量由时间戳外推（记录起点 + 时刻，按当前时间算当前表现），
/// 仅动态项（MotionBase 位移速度、DoT tick）做必要的推进。
/// </summary>
public partial class BattleManager
{
    #region 服务器权威移动
    /// <summary>玩家移动状态（方向变化时重锚时间戳；位置 = 起点 + 方向 × 速度 × 时长）。</summary>
    private class MoveState
    {
        public bool moving;
        public Vector2 dir;
        public Vector3 startPos;
        public float startTime;
        public bool blocked;
    }
    private readonly Dictionary<ushort, MoveState> moveStates = new();

    /// <summary>记录客户端输入（权威移动模拟 + 跳跃/滑铲/空手攻击动作触发）。</summary>
    private void RecordInput(EntityData entity, CSInputCommand command)
    {
        var anim = entity.GetComponentInChildren<EntityAnim>();

        // 朝向
        entity.transform.rotation = Quaternion.Euler(0f, command.yaw, 0f);

        // 移动状态（方向/启停变化时重锚时间戳）
        if (!moveStates.TryGetValue(entity.id, out var st))
        {
            st = new MoveState();
            moveStates[entity.id] = st;
        }
        if (st.moving != command.moving || st.dir != command.moveDir)
        {
            st.moving = command.moving;
            st.dir = command.moveDir;
            st.startPos = entity.transform.position;
            st.startTime = Time.time;
        }

        if (anim == null) return;

        // 跳跃：InAir 一段时间后落回（时间戳延时）
        if (command.jumpPressed)
        {
            anim.InAir(true);
            var weak = entity;
            GenericTimer.AddTimer(0, Config.jump_duration, _ =>
            {
                if (weak != null) weak.GetComponentInChildren<EntityAnim>()?.InAir(false);
            });
        }
        // 滑铲：进入滑铲状态，持续时间后结束
        if (command.slidePressed)
        {
            anim.DoSlide();
            var weak = entity;
            GenericTimer.AddTimer(0, Config.slide_duration, _ =>
            {
                if (weak != null) weak.GetComponentInChildren<EntityAnim>()?.EndSlide();
            });
        }
        // 空手/近战攻击：静止 = 跃起砸地，移动 = 出拳（策划案 12 章）
        if (command.meleePressed)
        {
            DoMelee(entity, command.moving);
        }
    }

    /// <summary>空手/近战攻击：播放攻击动作，前摇结束后对面前锥形内最近敌人结算（AttackData 共用链路）。</summary>
    private void DoMelee(EntityData entity, bool moving)
    {
        entity.GetComponentInChildren<EntityAnim>()?.DoAttack(
            moving ? EntityAnim.AttackType.Attack_Hand_R : EntityAnim.AttackType.Jump_Mega);

        var attack = AttackData.Create(entity, rate: 1f, radius: Config.melee_hit_radius,
            breakEndure: false, useMagic: false);
        Vector3 origin = entity.transform.position;
        Vector3 forward = entity.transform.forward;
        GenericTimer.AddTimer((entity, origin, forward), Config.weapon_short_windup, p =>
        {
            MeleeHit(p.Item1, p.Item2, p.Item3, attack);
        });
    }

    /// <summary>近战结算：正面锥形内最近敌方（打不到同阵营；中立单位如水晶可被打）。</summary>
    private void MeleeHit(EntityData attacker, Vector3 origin, Vector3 forward, AttackData attack)
    {
        if (attacker == null || !attacker.Alive) return;
        var target = EntityContainer.GetNearestEnemy(attacker, Config.melee_range + attacker.colliderInfo.radius);
        if (target == null) return;
        Vector3 to = target.transform.position - origin;
        to.y = 0f;
        if (to.magnitude > Config.melee_range + target.colliderInfo.radius) return;
        if (Vector3.Dot(to.normalized, forward) < 0.2f) return; // 需在正面锥形内
        target.ProcessHit(attack, attack.GetDamage());
    }

    /// <summary>
    /// 移动推进：位置由时间戳外推（起点 + 方向 × 速度 × 时长，强控/位移锁输入时冻结）；
    /// MotionBase 位移速度为动态值，做积分并重锚。
    /// </summary>
    private void TickMovement()
    {
        float now = Time.time;
        foreach (var pair in moveStates)
        {
            if (!EntityContainer.Entities.TryGetObject(pair.Key, out var e) || e == null || !e.Alive) continue;
            var st = pair.Value;
            bool blocked = e.effectController != null && !e.effectController.CanMove();
            if (st.blocked != blocked)
            {
                st.blocked = blocked;
                st.startPos = e.transform.position;
                st.startTime = now;
            }
            Vector3 pos = st.startPos;
            if (!blocked && st.moving && e.MotionCanMove)
            {
                pos += new Vector3(st.dir.x, 0f, st.dir.y) * e.moveSpeed * (now - st.startTime);
            }
            if (e.motionVelocity.sqrMagnitude > 0f)
            {
                pos += e.motionVelocity * (now - st.startTime);
                st.startPos = pos;
                st.startTime = now;
            }
            e.transform.position = pos;
        }
    }
    #endregion

    #region 子弹容器（时间戳推进：位置 = 轨迹 Lerp(经过时长/生命)，不做增量移动）
    private readonly List<Bullet> activeBullets = new();
    private static readonly HashSet<int> s_bulletBuffer = new();

    /// <summary>登记子弹（ShootBullet 调用）。</summary>
    private void AddBullet(AttackData attack, BulletTrajectory trajectory, float lifeTime)
    {
        if (attack == null || trajectory == null) return;
        activeBullets.Add(new Bullet()
        {
            attack = attack,
            trajectory = trajectory,
            lifeTime = lifeTime,
            spawnTime = Time.time,
        });
    }

    /// <summary>子弹推进：按时间戳算位置 → 命中检测 → 伤害结算；生命到期自然消失。</summary>
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
            Vector3 pos = b.trajectory.Lerp(t / b.lifeTime);
            if (TryHitBullet(b, pos))
            {
                activeBullets.RemoveAt(i);
            }
        }
    }

    /// <summary>命中检测：与攻击者不同阵营的实体（水晶/瘟疫树等中立单位也可被打）。</summary>
    private bool TryHitBullet(Bullet b, Vector3 pos)
    {
        var attack = b.attack;
        EntityContainer.Entities.GetIdsInRange(pos, attack.radius + 2f, s_bulletBuffer);
        bool hit = false;
        foreach (var id in s_bulletBuffer)
        {
            if (!EntityContainer.Entities.TryGetObject(id, out var e) || e == null || !e.Alive) continue;
            if (e.id == attack.shooter || e.camp == attack.shooterCamp) continue;

            // 判定柱命中：水平距离 ≤ 弹半径 + 目标半径，且高度与判定柱相交
            Vector3 to = e.transform.position - pos;
            to.y = 0f;
            float bottomY = e.transform.position.y + e.colliderInfo.bottom;
            float topY = e.transform.position.y + e.colliderInfo.top;
            if (pos.y < bottomY - 0.5f || pos.y > topY + 0.5f) continue;
            if (to.magnitude > attack.radius + e.colliderInfo.radius) continue;

            float damage = attack.GetDamage();
            e.ProcessHit(attack, damage);
            if (attack.addEffectEvent != null && e.effectController != null)
            {
                attack.addEffectEvent.Invoke((type, level, duration) =>
                    e.effectController.AddEffect(type, level, duration));
            }
            hit = true;
            break; // 单发子弹命中即消失
        }
        s_bulletBuffer.Clear();
        return hit;
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

    /// <summary>死亡统一处理：摧毁单位；水晶排重生；玩家进入复活流程并下发进度。</summary>
    private void HandleDeath(EntityData entity)
    {
        if (entity.type.category == EntityCategory.Crystal) ScheduleCrystalRespawn(entity);
        Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.Kill, entity.id);

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

        DestroyEntity(entity.id);
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
                float rate = EnvironmentManager.CurrentPhase == 0
                    ? Config.revive_day_progress_per_second
                    : Config.revive_night_progress_per_second;
                var mults = Config.revive_progress_multiplier_by_death;
                float mult = mults[Mathf.Min(Mathf.Max(rs.deathCount - 1, 0), mults.Length - 1)];
                rs.progress += dt * rate * mult;
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
        Vector3 pos = isAttack
            ? LandscapeSpawns.RandomOf(spawns != null && spawns.attackRevivePositions.Count > 0
                ? spawns.attackRevivePositions : Tool.InfoManager.AttackSpawnPositions)
            : LandscapeSpawns.RandomOf(spawns != null && spawns.defenseRevivePositions.Count > 0
                ? spawns.defenseRevivePositions : new List<Vector3> { Tool.InfoManager.DefenseSpawnPosition });

        ushort entityId = SpawnEntity(characterType, level, pos, camp);
        if (entityId == 0)
        {
            rs.progress = 1f; // 模板缺失：保持攒满状态稍后重试
            return;
        }
        PlayerEntityId[clientId] = entityId;
        EntityOwnerClient[entityId] = clientId;

        var data = GetEntity(entityId);
        data?.skillController?.SetSkillList(new List<int> { Config.initial_skill_id });

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
            characterLevel = level,
            dayNightPhase = EnvironmentManager.CurrentPhase,
            phaseTime = EnvironmentManager.PhaseTime,
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
            deadCount = rs.deathCount,
            yzStack = Config.yz_stack_by_life[Mathf.Min(rs.deathCount, Config.yz_stack_by_life.Length - 1)],
        });
    }

    /// <summary>开战清空战斗运行状态（id 源、子弹、移动、复活、水晶重生）。</summary>
    private void ClearBattleState()
    {
        activeBullets.Clear();
        moveStates.Clear();
        reviveStates.Clear();
        crystalRespawns.Clear();
    }
    #endregion
}
