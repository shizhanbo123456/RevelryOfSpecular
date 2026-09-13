using System;
using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>轨迹形态（上下文 ints[1]，对应策划案 21 章的 L/P/B/S 缩写）。</summary>
    public enum ProjectilePattern { Line, Point, Bezier, SkyFall }

    /// <summary>特效类别（Bullet~Buff 对应 AssetsManager 的五个特效列表；Weapon = 直接用武器模型作弹体）。</summary>
    public enum SkillVfxKind { None, Bullet, RangeMagic, MagicCircle, Shield, Buff, Weapon }

    public abstract class SkillBase
    {
        public abstract int Id { get; }
        public abstract float CD { get; }
        public abstract int Store { get; }

        public virtual WeaponRef HoldWeapon => WeaponRef.None;
        public virtual WeaponRef FlyWeapon => WeaponRef.None;

        //服务器使用技能主入口，内部生成上下文，并根据上下文生成轨迹发射子弹，并传出Context给客户端
        public abstract SkillContext SkillLogic(EntityData entity);
        //服务器客户端共用，确保轨迹一致
        public virtual BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            return null;
        }
        protected abstract void OnCast(SkillContext context);
        //客户端主入口，用于表现特效
        public abstract void PlayVFX(SkillContext context);

        /// <summary>
        /// 服务器：广播"使用技能"RPC（技能 id + 轨迹上下文），客户端据此重建轨迹播放表现。
        /// </summary>
        protected static void BroadcastSkillCast(int skillId, SkillContext context)
        {
            Tool.NetworkManager?.SendSkillCast(skillId, context);
        }

        #region 通用工具（服务端/客户端共用，保证伤害与特效一致）
        /// <summary>服务器：为手部赋值武器（表现随实体摘要同步给客户端）。</summary>
        protected static void SetHeldWeapon(EntityData entity, WeaponRef weapon) => entity.heldWeapon = weapon;

        /// <summary>以 pos→dest 为基准方向生成扇形终点（水平展开 spreadDeg）。</summary>
        protected static Vector3[] FanDests(Vector3 pos, Vector3 dest, int count, float spreadDeg)
        {
            Vector3 forward = dest - pos;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float dist = Vector3.Distance(pos, dest);
            var dests = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : i / (float)(count - 1) - 0.5f;
                Vector3 dir = (forward + right * Mathf.Tan(t * spreadDeg * Mathf.Deg2Rad)).normalized;
                dests[i] = pos + dir * dist;
            }
            return dests;
        }

        /// <summary>围绕 center 生成圆周终点（XZ 平面）。</summary>
        protected static Vector3[] CircleDests(Vector3 center, float radius, int count, float startAngleDeg = 0f)
        {
            var dests = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (startAngleDeg + i * 360f / count) * Mathf.Deg2Rad;
                dests[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            return dests;
        }

        /// <summary>以 pos→dest 为基准方向水平旋转 angleDeg（右正左负）得到新目标点。</summary>
        protected static Vector3 RotateDest(Vector3 pos, Vector3 dest, float angleDeg)
        {
            Vector3 forward = dest - pos;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * forward;
            return pos + dir * Vector3.Distance(pos, dest);
        }

        /// <summary>默认目标点：面前 distance 米。</summary>
        protected static Vector3 DefaultDest(EntityData entity, float distance)
        {
            return entity.transform.position + entity.transform.forward * distance;
        }

        /// <summary>最近敌人（范围内）。</summary>
        protected static EntityData GetNearestEnemy(EntityData entity, float radius = 10f)
        {
            return BattleManager.EntityContainer.GetNearestEnemy(entity, radius);
        }

        /// <summary>最近友方（范围内，不含自己）。</summary>
        protected static EntityData GetNearestAlly(EntityData entity, float radius = Config.default_skill_auto_target_radius)
        {
            if (entity == null) return null;
            return BattleManager.EntityContainer.GetNearestInCamp(
                entity.transform.position, radius, entity.camp, entity.id);
        }

        /// <summary>全场指定阵营的存活实体（不限距离，写 TargetBuffer）。</summary>
        protected static void AllInCamp(EntityCamp camp)
        {
            TargetBuffer.Clear();
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e != null && e.Alive && e.camp == camp) TargetBuffer.Add(e);
            }
        }

        /// <summary>全场指定类别的存活实体（如所有防御塔 / 守护点，写 TargetBuffer）。</summary>
        protected static void AllOfCategory(EntityCategory category)
        {
            TargetBuffer.Clear();
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e != null && e.Alive && e.type.category == category) TargetBuffer.Add(e);
            }
        }

        /// <summary>与给定阵营敌对的阵营（中立视为无敌人）。</summary>
        protected static EntityCamp Opposing(EntityCamp camp) =>
            camp == EntityCamp.Attack ? EntityCamp.Defense :
            camp == EntityCamp.Defense ? EntityCamp.Attack : EntityCamp.Neutral;

        /// <summary>把实体分桶（Towers / Crystals / Beacons 等）里的存活实体全部写入 TargetBuffer。</summary>
        protected static void AllIn(IEnumerable<EntityData> bucket)
        {
            TargetBuffer.Clear();
            foreach (var e in bucket)
            {
                if (e != null && e.Alive) TargetBuffer.Add(e);
            }
        }

        /// <summary>把实体分桶里范围内的存活实体写入 TargetBuffer（不限阵营）。</summary>
        protected static void InRange(IEnumerable<EntityData> bucket, Vector3 pos, float radius)
        {
            TargetBuffer.Clear();
            float sqr = radius * radius;
            foreach (var e in bucket)
            {
                if (e != null && e.Alive && (e.transform.position - pos).sqrMagnitude <= sqr) TargetBuffer.Add(e);
            }
        }

        /// <summary>从实体分桶里取范围内最近的一个（排除 excludeId）。</summary>
        protected static EntityData NearestIn(IEnumerable<EntityData> bucket, Vector3 pos, float radius,
            ushort excludeId = 0)
        {
            EntityData best = null;
            float bestSqr = radius * radius;
            foreach (var e in bucket)
            {
                if (e == null || !e.Alive || e.id == excludeId) continue;
                float sqr = (e.transform.position - pos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }
            return best;
        }

        /// <summary>范围内所有敌方实体写入 TargetBuffer（不含同阵营）。</summary>
        protected static void EnemiesIn(Vector3 center, float radius, EntityCamp ownCamp)
        {
            TargetBuffer.Clear();
            float sqr = radius * radius;
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e == null || !e.Alive || e.camp == ownCamp) continue;
                if ((e.transform.position - center).sqrMagnitude <= sqr) TargetBuffer.Add(e);
            }
        }

        /// <summary>召唤位置偏移（环形散开，避免重叠落点）。</summary>
        protected static Vector3 SummonOffset(int index, float radius = 2f)
        {
            float angle = index * 72f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        /// <summary>
        /// 默认瞄准点：攻击者视野内最近的敌人，没有则正前方 Config.default_forward_aim_distance 米。
        /// 索敌半径 = 实体可见距离（未配置则用全局索敌半径）。要别的索敌逻辑的技能自行覆写。
        /// </summary>
        protected virtual Vector3 AimPos(EntityData entity)
        {
            float view = entity.floatingAttribute != null && entity.floatingAttribute.viewDistance > 0f
                ? entity.floatingAttribute.viewDistance
                : Config.default_skill_auto_target_radius;
            var target = GetNearestEnemy(entity, view);
            return target != null
                ? target.transform.position
                : DefaultDest(entity, Config.default_forward_aim_distance);
        }
        #endregion

        #region 框架工具（弹道上下文约定 / 攻击结算 / 特效播放，服务端与客户端共用）
        // 上下文约定：ints[0] = 施放者 id；ints[1] = 轨迹形态；vectors 成对存每发的 (起点, 终点)，发数 = vectors.Count / 2。
        // 起点由服务器算好写进上下文（悬浮武器发射点依赖位置+朝向+槽位，客户端无法复原，故不放客户端算）。
        private static readonly EntityData[] s_hitBuffer = new EntityData[16];

        /// <summary>多目标技能的目标缓存（服务器单线程顺序执行，用完即弃，勿跨帧持有）。</summary>
        protected static readonly List<EntityData> TargetBuffer = new();

        /// <summary>施放者 id（上下文 ints[0]）。</summary>
        protected static ushort CasterId(SkillContext context) =>
            context != null && context.ints.Count > 0 ? (ushort)context.ints[0] : (ushort)0;

        /// <summary>轨迹形态（上下文 ints[1]）。</summary>
        protected static ProjectilePattern Pattern(SkillContext context) =>
            context != null && context.ints.Count > 1 ? (ProjectilePattern)context.ints[1] : ProjectilePattern.Line;

        /// <summary>第 index 发的起点 / 终点。</summary>
        protected static Vector3 Origin(SkillContext context, int index) => context.vectors[index * 2];
        protected static Vector3 Dest(SkillContext context, int index) => context.vectors[index * 2 + 1];

        /// <summary>发数（上下文 vectors 的成对数）。</summary>
        protected static int ShotCount(SkillContext context) =>
            context != null && context.vectors != null ? context.vectors.Count / 2 : 0;

        // 多目标技能约定：ints[0] = 施放者 id，ints[1..] = 目标 id 列表（与弹道技能的 vectors 各管各的）。
        /// <summary>服务器：把目标 id 依次追加进上下文。</summary>
        protected static void AddTargets(SkillContext context, List<EntityData> targets)
        {
            if (context == null || targets == null) return;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null) context.AddInts(targets[i].id);
            }
        }

        /// <summary>上下文里的目标数量。</summary>
        protected static int TargetCount(SkillContext context) =>
            context != null && context.ints != null && context.ints.Count > 1 ? context.ints.Count - 1 : 0;

        /// <summary>第 index 个目标 id。</summary>
        protected static ushort TargetId(SkillContext context, int index) => (ushort)context.ints[index + 1];

        /// <summary>服务器发射点：有飞行武器用本技能槽位的悬浮武器位置，否则用通用发射点。</summary>
        protected Vector3 ShootPos(EntityData entity)
        {
            if (!FlyWeapon.IsValid) return entity.BulletShootPos();
            int slot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            return entity.GetWeaponFloatPos(slot < 0 ? 0 : slot);
        }

        /// <summary>按约定建标准上下文（施放者 id + 形态 + 各发 [起点, 终点]）。</summary>
        protected SkillContext BuildShotContext(EntityData entity, ProjectilePattern pattern, params Vector3[] dests)
        {
            var context = new SkillContext();
            context.AddInts(entity.id, (int)pattern);
            Vector3 origin = ShootPos(entity);
            foreach (var dest in dests) context.AddVectors(origin, dest);
            return context;
        }

        /// <summary>把实际效果挂到动画攻击帧；返回 true 表示已在等待，调用方不要再立即执行。</summary>
        protected static bool WaitAttackFrame(EntityData entity, EntityAnim.AttackType castAnim, System.Action onFrame)
        {
            if (entity.anim == null) return false;
            entity.anim.onAttack = _ => onFrame();
            entity.anim.DoAttack(castAnim);
            return true;
        }

        /// <summary>本技能的伤害数据（武器经验按释放时的经验加成）。addEffect = 命中附加 Buff，onHit = 命中额外逻辑。</summary>
        protected AttackData BuildAttack(EntityData entity, float rate, float radius,
            bool breakEndure = false, bool useMagic = false, Action<EntityEffectController> addEffect = null,
            Action<EntityData> onHit = null)
        {
            int exp = entity != null && entity.skillController != null ? entity.skillController.GetWeaponExp(Id) : 0;
            return AttackData.Create(entity, rate, radius, breakEndure, useMagic,
                addEffectEvent: addEffect, onHit: onHit, weaponExp: exp);
        }

        /// <summary>服务器：按上下文里的 id 反查实体（客户端没有实体，只走 PlayVFX）。</summary>
        protected static EntityData Caster(SkillContext context, int idIndex = 0)
        {
            if (context == null || idIndex < 0 || idIndex >= context.ints.Count) return null;
            return BattleManager.GetEntity((ushort)context.ints[idIndex]);
        }

        /// <summary>
        /// 构造"命中附加一个 Buff"的回调：DoT 传 damage、护盾传 shieldValue、属性类传 value。
        /// negative = true 为负面（可被净化），控制与 DoT 类必须传。
        /// </summary>
        protected static Action<EntityEffectController> ApplyEffect(EffectType type, int level, float duration,
            bool negative = false, float value = 0f, float damage = 0f, float shieldValue = 0f, ushort sourceId = 0)
        {
            return effect => effect.AddEffect(type, level, duration, negative,
                new EntityEffectController.EffectPayload
                {
                    value = value,
                    damage = damage,
                    shieldValue = shieldValue,
                    sourceId = sourceId,
                });
        }

        /// <summary>服务器：直接给一个实体挂 Buff（无需命中判定，如护盾 / 增伤 / 净化）。</summary>
        protected static void GiveEffect(EntityData target, EffectType type, int level, float duration,
            bool negative = false, float value = 0f, float damage = 0f, float shieldValue = 0f, ushort sourceId = 0)
        {
            if (target == null || target.effectController == null) return;
            target.effectController.AddEffect(type, level, duration, negative,
                new EntityEffectController.EffectPayload
                {
                    value = value,
                    damage = damage,
                    shieldValue = shieldValue,
                    sourceId = sourceId,
                });
        }

        /// <summary>构造"命中击退"回调：把目标沿远离 from 的方向推开（走 MotionBase 速度积分，不瞬移）。</summary>
        protected static Action<EntityData> Knockback(Vector3 from, float speed = 14f, float duration = 0.25f)
        {
            return target => target?.SetMotion(new MotionPush(from, speed, duration));
        }

        /// <summary>构造"命中拉拽"回调：把目标拉向 to（走 MotionToPoint 速度积分）。</summary>
        protected static Action<EntityData> PullTo(Vector3 to, float speed = 16f)
        {
            return target => target?.SetMotion(new MotionToPoint(to, speed));
        }

        /// <summary>双端按实体 id 取位置 / 取完整变换（位置 + 朝向）。</summary>
        protected static bool TryGetPos(ushort entityId, out Vector3 pos) =>
            BulletTrajectory.TryGetEntityPosition(entityId, out pos);

        protected static bool TryGetTransform(ushort entityId, out Vector3 pos, out Quaternion rot) =>
            BulletTrajectory.TryGetEntityTransform(entityId, out pos, out rot);

        /// <summary>手部骨骼位置；无手部骨骼（非人形）退化为实体位置。</summary>
        protected static Vector3 HandPos(EntityData entity, bool leftHand = false)
        {
            var mount = entity != null && entity.anim != null ? entity.anim.GetHandMount(leftHand) : null;
            return mount != null ? mount.position : entity.transform.position;
        }

        /// <summary>服务器：按发数逐发建轨迹交给子弹容器（穿透与同目标去重由容器负责）。</summary>
        protected void ShootAll(EntityData entity, SkillContext context, AttackData attack)
        {
            for (int i = 0; i < ShotCount(context); i++)
            {
                Tool.BattleManager?.ShootBullet(entity, attack, CreateTrajectory(context, i));
            }
        }

        /// <summary>球判定结算：对球内敌方各结算一次（跳过自己与同阵营），命中附加效果与命中回调同样生效。</summary>
        protected static void StrikeSphere(EntityData entity, Vector3 center, float radius, AttackData attack)
        {
            int count = EntityPhysics.OverlapSphere(center, radius, s_hitBuffer);
            for (int i = 0; i < count; i++)
            {
                var target = s_hitBuffer[i];
                if (target == null || !target.Alive) continue;
                if (target.id == entity.id || target.camp == entity.camp) continue;
                float damage = attack.GetDamage(out bool isCrit);
                target.ProcessHit(attack, damage, isCrit);
                if (attack.addEffectEvent != null && target.effectController != null)
                {
                    attack.addEffectEvent.Invoke(target.effectController);
                }
                attack.onHit?.Invoke(target);
            }
        }

        #region 常用轨迹构建（CreateTrajectory 的默认实现，技能按需调用）
        protected static BulletTrajectory Line(SkillContext context, int index, float duration)
        {
            var t = new LineTrajectory(Origin(context, index), Dest(context, index));
            t.Duration = duration;
            return t;
        }

        protected static BulletTrajectory Point(SkillContext context, int index, float duration)
        {
            var t = new PointTrajectory(Dest(context, index));
            t.Duration = duration;
            return t;
        }

        protected static BulletTrajectory SkyFall(SkillContext context, int index, float duration, float skyHeight = 30f)
        {
            var t = new SkyFallTrajectory(Origin(context, index), Dest(context, index), skyHeight);
            t.Duration = duration;
            return t;
        }

        /// <summary>抛物线（曲射类，如榴弹）：控制点按弧高抬升。</summary>
        protected static BulletTrajectory Arc(SkillContext context, int index, float duration, float height = 8f)
        {
            Vector3 from = Origin(context, index), to = Dest(context, index);
            var t = new BezierTrajectory(from, from + Vector3.up * height, to + Vector3.up * height, to);
            t.Duration = duration;
            return t;
        }
        #endregion

        #region 客户端特效播放（技能 PlayVFX 的默认实现，技能按需调用）
        /// <summary>按发数逐个重建轨迹播特效（范围魔法/魔法阵播在轨迹终点）。</summary>
        protected void PlayAlong(SkillContext context, SkillVfxKind kind, int[] vfx)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            for (int i = 0; i < ShotCount(context); i++)
            {
                var trajectory = CreateTrajectory(context, i);
                if (trajectory != null) PlayOne(kind, Pick(vfx, i), trajectory);
            }
        }

        /// <summary>跟随某个实体播一次（自身光环 / 护盾 / Buff）；lifeTime &gt; 0 时覆盖轨迹时长。</summary>
        protected void PlayFollow(SkillVfxKind kind, int index, ushort entityId, float lifeTime = 0f)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            var trajectory = new FollowTrajectory(entityId);
            if (lifeTime > 0f) trajectory.Duration = lifeTime;
            PlayOne(kind, index, trajectory);
        }

        /// <summary>给上下文里的每个目标各播一次跟随特效（多目标技能表现）。</summary>
        protected void PlayFollowAll(SkillContext context, SkillVfxKind kind, int index, float lifeTime = 0f)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            for (int i = 0; i < TargetCount(context); i++) PlayFollow(kind, index, TargetId(context, i), lifeTime);
        }

        /// <summary>定点播一次（范围魔法 / 魔法阵）。</summary>
        protected void PlayAt(SkillVfxKind kind, int index, Vector3 pos, float duration)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None || index < 0) return;
            switch (kind)
            {
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(index, pos, Quaternion.identity, duration);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, pos, duration);
                    break;
            }
        }

        /// <summary>把特效类别映射到 VfxManager 对应接口；时长统一取轨迹自带的 Duration。</summary>
        private void PlayOne(SkillVfxKind kind, int index, BulletTrajectory trajectory)
        {
            float life = trajectory.Duration;
            switch (kind)
            {
                case SkillVfxKind.Weapon:
                    Tool.VfxManager.PlayWeaponVFX(FlyWeapon, trajectory);
                    break;
                case SkillVfxKind.Bullet when index >= 0:
                    Tool.VfxManager.PlayBulletVFX(index, trajectory);
                    break;
                case SkillVfxKind.Shield when index >= 0:
                    Tool.VfxManager.PlayShieldVFX(index, trajectory);
                    break;
                case SkillVfxKind.Buff when index >= 0:
                    Tool.VfxManager.PlayBuffVFX(index, trajectory);
                    break;
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(index, trajectory.Lerp(1f), Quaternion.identity, life);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, trajectory.Lerp(1f), life);
                    break;
            }
        }

        /// <summary>发数多于特效数时复用最后一个。</summary>
        private static int Pick(int[] list, int index)
        {
            if (list == null || list.Length == 0) return -1;
            return list[Mathf.Min(index, list.Length - 1)];
        }
        #endregion
        #endregion
    }
    public class SkillTemplate:SkillBase
    {
        public override int Id => 6;
        public override float CD => 2.0f;
        public override int Store => 2;
        public override WeaponRef HoldWeapon => new WeaponRef(WeaponCategory.Knife,0);
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 0);
        private SkillContext BuildContext(EntityData entity)
        {
            var c = new SkillContext();
            c.vectors.Add(entity.transform.position + entity.transform.forward * 20);
            c.ints.Add(entity.id);
            return c;
        }
        public override SkillContext SkillLogic(EntityData entity)
        {
            const EntityAnim.AttackType CastAnim = EntityAnim.AttackType.Attack_Hand_R;
            var context =BuildContext(entity);

            if (entity.anim == null)
            {
                OnCast(context);
            }
            else
            {
                entity.anim.onAttack = _ => OnCast(context);
                entity.anim.DoAttack(CastAnim);
            }
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            if (index == 0)
            {
                int attackerId = context.ints[0];
                Vector3 dest = context.vectors[0];
                //return new LineTrajectory();//根据发射者位置和目标位置生成轨迹
            }
            return null;
        }
        protected override void OnCast(SkillContext context)
        {
            
        }
        public override void PlayVFX(SkillContext context)
        {
            var bulletObj=Tool.VfxManager.GetBulletVfx(0);
            var traj = CreateTrajectory(context, 0);
            Tool.VfxManager.Play(bulletObj, traj, 5f);
        }
    }
}
