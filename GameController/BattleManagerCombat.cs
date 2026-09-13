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
    /// <summary>玩家移动状态。</summary>
    private class MoveState
    {
        public PlayerKey held;    // 当前按住的移动键（按下/抬起边沿维护）
        public bool moving;
        public Vector2 dir;       // 原始按键输入（x = 左右横移，y = 前后）
        public float yaw;         // 角色朝向（度，服务器权威渐转推进）
        public float yawSpeed;    // 绕 Y 角速度（度/秒，客户端推演用）
        public bool blocked;
    }
    private readonly Dictionary<ushort, MoveState> moveStates = new();

    /// <summary>记录移动输入（按下/抬起边沿 → 按住掩码；朝向仍由服务器渐转权威推进）。</summary>
    private void RecordMoveInput(EntityData entity, CSMoveInput move)
    {
        if (!moveStates.TryGetValue(entity.id, out var st))
        {
            st = new MoveState { yaw = entity.transform.eulerAngles.y }; // 初始朝向 = 生成时的朝向
            moveStates[entity.id] = st;
        }
        st.held = (st.held | move.pressed) & ~move.released;

        float x = ((st.held & PlayerKey.D) != 0 ? 1f : 0f) - ((st.held & PlayerKey.A) != 0 ? 1f : 0f);
        float z = ((st.held & PlayerKey.W) != 0 ? 1f : 0f) - ((st.held & PlayerKey.S) != 0 ? 1f : 0f);
        st.dir = new Vector2(x, z).normalized;
        st.moving = x != 0f || z != 0f;

        // Run/Idle 随表现摘要同步给客户端
        entity.GetComponentInChildren<EntityAnim>()?.Move(st.moving);
    }

    /// <summary>记录动作输入（攻击/跳跃/滑铲/技能槽，均为按下边沿）。</summary>
    private void RecordActionInput(EntityData entity, CSActionInput action)
    {
        var anim = entity.GetComponentInChildren<EntityAnim>();
        bool moving = moveStates.TryGetValue(entity.id, out var st) && st.moving;

        // 跳跃：InAir 一段时间后落回（时间戳延时）
        if ((action.pressed & PlayerKey.K) != 0)
        {
            anim?.InAir(true);
            var weak = entity;
            GenericTimer.AddTimer(0, Config.jump_duration, _ =>
            {
                if (weak != null) weak.GetComponentInChildren<EntityAnim>()?.InAir(false);
            });
        }
        // 滑铲：进入滑铲状态，持续时间后结束
        if ((action.pressed & PlayerKey.LShift) != 0)
        {
            anim?.DoSlide();
            var weak = entity;
            GenericTimer.AddTimer(0, Config.slide_duration, _ =>
            {
                if (weak != null) weak.GetComponentInChildren<EntityAnim>()?.EndSlide();
            });
        }
        // 空手攻击走技能释放链路（策划案 12 章）：静止 = 原地砸击，移动 = 随机左右拳
        if ((action.pressed & PlayerKey.J) != 0)
        {
            int meleeSkill = moving
                ? (Random.Range(0, 2) == 0 ? Config.unarmed_punch_left : Config.unarmed_punch_right)
                : Config.unarmed_attack_smash;
            entity.skillController.TryUseSkill(meleeSkill);
        }

        // 技能槽：键位 → 槽位下标（技能 id 由服务器权威决定）
        for (int i = 0; i < Config.skill_slot_player_keys.Length; i++)
        {
            if ((action.pressed & Config.skill_slot_player_keys[i]) == 0) continue;
            UseSkillSlot(entity, i);
            break;
        }
    }

    /// <summary>技能槽直触：槽位下标 → 服务器权威技能 id（CD/库存/强控校验在 TryUseSkill 内）。</summary>
    private void UseSkillSlot(EntityData entity, int slot)
    {
        if (entity.skillController == null) return;
        var ids = entity.skillController.GetSkillIds();
        if (slot < 0 || slot >= ids.Count || ids[slot] < 0) return;

        entity.skillController.SelectIndex(slot); // 选中下标供 UI 高亮
        entity.skillController.TryUseSkill(ids[slot]);
    }

    /// <summary>
    /// 移动推进（双手键盘：角色相对移动 + 渐转）：
    /// 朝向服务器权威——前后 + 左右同按时按 Config.move_turn_rate 渐转（纯前后/纯左右不转向）；
    /// 移动方向 = 当前朝向 × 原始输入（渐转路径为曲线，故按帧增量积分而非时间戳外推）；
    /// 强控/位移锁输入时冻结；MotionBase 位移速度独立积分。
    /// </summary>
    private void TickMovement()
    {
        float dt = Time.deltaTime;
        foreach (var pair in moveStates)
        {
            if (!EntityContainer.Entities.TryGetObject(pair.Key, out var e) || e == null || !e.Alive) continue;
            var st = pair.Value;
            st.blocked = e.effectController != null && !e.effectController.CanMove();

            if (!st.blocked && st.moving && e.MotionCanMove)
            {
                // 渐转：前后 + 左右同按时朝向逐渐偏向横移侧（yaw 正 = 右转，负 = 左转）
                float yawDelta = 0f;
                if (Mathf.Abs(st.dir.x) > 0.01f && Mathf.Abs(st.dir.y) > 0.01f)
                {
                    yawDelta = Config.move_turn_rate * Mathf.Sign(st.dir.x) * dt;
                    st.yaw += yawDelta;
                }
                st.yawSpeed = dt > 0f ? yawDelta / dt : 0f;
                e.transform.rotation = Quaternion.Euler(0f, st.yaw, 0f);

                // 角色相对移动：前 = 角色前方，左右 = 角色侧方，方向随朝向变化
                Vector3 dir = Quaternion.Euler(0f, st.yaw, 0f) * new Vector3(st.dir.x, 0f, st.dir.y).normalized;
                e.transform.position += dir * e.moveSpeed * dt;
            }
            else
            {
                st.yawSpeed = 0f;
            }

            // 位移效果速度（MotionBase）独立积分
            if (e.motionVelocity.sqrMagnitude > 0f)
            {
                e.transform.position += e.motionVelocity * dt;
            }
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
            if (e.id == attack.shooter || e.camp == attack.shooterCamp) continue;
            if (!b.hitIds.Add(e.id)) continue; // 同一发子弹对同一目标只结算一次

            float damage = attack.GetDamage(out bool isCrit);
            e.ProcessHit(attack, damage, isCrit);
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

    /// <summary>死亡统一处理：摧毁单位；水晶排重生/掉武器并广播采集事件（被「蘑菇感染」的水晶被进攻方摧毁时无产出，走 CrystalBroken）；守护点重算分层减伤；玩家进入复活流程并下发进度。</summary>
    private void HandleDeath(EntityData entity)
    {
        if (entity.type.category == EntityCategory.Crystal)
        {
            // 蘑菇感染判定走 Buff 查询（服务器不存在蘑菇实体，见策划案 11.3 蘑菇感染）：
            // 被感染水晶被进攻方摧毁 → 无产出（不掉武器，广播 CrystalBroken）；防守方摧毁或未感染 → 正常产出
            bool infectedAndBrokenByAttack = entity.effectController != null &&
                                             entity.effectController.HasEffect(EffectType.MushroomInfect) &&
                                             entity.lastAttacker != null &&
                                             entity.lastAttacker.camp == EntityCamp.Attack;
            if (infectedAndBrokenByAttack)
            {
                Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.CrystalBroken);
            }
            else
            {
                Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.CrystalCollected);
                TryDropCrystalWeapon(entity);
            }
            ScheduleCrystalRespawn(entity); // 蘑菇状态下的水晶被摧毁后就相当于水晶被摧毁（重生排程照常，见策划案第七章）
        }
        Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.Kill);

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

        // 守护点阵亡后重算中心守护点的分层减伤（走 effectController 的 BeaconReduce 通道）
        if (entity.type.category == EntityCategory.Beacon)
        {
            UpdateCoreBeaconReduce();
        }
        else if (entity.type.category == EntityCategory.Character_Attack)
        {
            CheckAttackWiped(); // 进攻方全灭 → 立即按分数结算（策划案 17.2，不直接判负）
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
    private void TryDropCrystalWeapon(EntityData crystal)
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
        data?.skillController?.SetSkillList(Config.GetInitialSkills(characterType)); // 初始技能表与角色绑定

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

    /// <summary>开战清空战斗运行状态（id 源、子弹、移动、复活、水晶重生、经验统计）。</summary>
    private void ClearBattleState()
    {
        activeBullets.Clear();
        moveStates.Clear();
        reviveStates.Clear();
        crystalRespawns.Clear();
        HarvestByClient.Clear();
    }
    #endregion
}
