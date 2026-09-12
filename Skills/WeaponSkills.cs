using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 技能占位基类：只定 id / CD / 库存（数值来自策划案第二十一章技能池），效果留空待实现。
    /// 实现时继承占位类替换空方法即可（弹道/近战通用基类见下方 ProjectileSkill / MeleeWeaponSkill）。
    /// </summary>
    public abstract class SkillStub : SkillBase
    {
        private readonly int id;
        private readonly float cd;
        private readonly int store;
        private readonly WeaponRef weapon;

        /// <summary>weapon 省略时为 <see cref="WeaponRef.None"/>（防守方主动技能/大招/被动均无悬浮武器）。</summary>
        protected SkillStub(int id, float cd, int store, WeaponRef weapon = default)
        {
            this.id = id;
            this.cd = cd;
            this.store = store;
            this.weapon = weapon;
        }

        public override int Id => id;
        public override float CD => cd;
        public override int Store => store;
        public override WeaponRef Weapon => weapon;
        /// <summary>
        /// 释放效果：先把实际施法挂到动画攻击帧回调，再播放施法动作；动画播到攻击帧时执行 Execute。
        /// 无动画的单位（防御塔等非人形）与无施法动作的技能（CastAnim = None）直接执行。
        /// </summary>
        public override void DoDamageActs(EntityData entity, Vector3 dest)
        {
            var anim = entity.GetComponentInChildren<EntityAnim>();
            if (anim == null || CastAnim == EntityAnim.AttackType.None)
            {
                Execute(entity, dest);
                return;
            }
            if (WeaponInHand) SetHeldWeapon(entity, Weapon); // 近战类才把武器拿到手上（攻击动作结束由 AnimAttackEvent 清空）
            anim.onAttack = _ => Execute(entity, dest);
            anim.DoAttack(CastAnim);
        }

        /// <summary>技能的实际效果（动画攻击帧触发；非人形单位或无施法动作时立即执行）。</summary>
        protected virtual void Execute(EntityData entity, Vector3 dest) { } // TODO: 各技能逐个实现

        public override void PlayVFX(TrajectoryContext context) { } // TODO: 各技能逐个实现
    }

    #region 武器技能通用基类（数值为占位初值，待策划统一调整）

    /// <summary>特效类别（Bullet~Buff 对应 AssetsManager 的五个特效列表；Weapon = 直接以武器模型作弹体）。</summary>
    public enum SkillVfxKind { None, Bullet, RangeMagic, MagicCircle, Shield, Buff, Weapon }

    /// <summary>弹道形态（对应策划案 21 章的轨迹缩写）。</summary>
    public enum ProjectilePattern { Line, Fan, Circle, SkyFall, Point, Self }

    /// <summary>武器技能基类：注入武器 / 施法动作 / 伤害与特效参数。</summary>
    public abstract class WeaponSkill : SkillStub
    {
        protected readonly SkillVfxKind vfxKind;
        protected readonly int[] vfx;
        protected readonly SkillVfxKind landingVfxKind;
        protected readonly int[] landingVfx;
        protected readonly float rate;
        protected readonly float radius;
        protected readonly float life;
        private readonly EntityAnim.AttackType castAnim;

        protected WeaponSkill(int id, float cd, int store, WeaponCategory category, int weaponIndex,
            EntityAnim.AttackType castAnim, SkillVfxKind vfxKind, int[] vfx,
            float rate, float radius, float life,
            SkillVfxKind landingVfxKind = SkillVfxKind.None, int[] landingVfx = null)
            : base(id, cd, store, new WeaponRef(category, weaponIndex))
        {
            this.castAnim = castAnim;
            this.vfxKind = vfxKind;
            this.vfx = vfx;
            this.rate = rate;
            this.radius = radius;
            this.life = life;
            this.landingVfxKind = landingVfxKind;
            this.landingVfx = landingVfx;
        }

        /// <summary>释放动作（AnimAttackEvent 按此播动画并在攻击帧回调）。</summary>
        public override EntityAnim.AttackType CastAnim => castAnim;

        /// <summary>本技能的伤害数据（武器经验按释放时的经验加成，见策划案 14 章）。</summary>
        protected AttackData BuildAttack(EntityData entity, bool breakEndure = false)
        {
            return AttackData.Create(entity, rate: rate, radius: radius, breakEndure: breakEndure,
                weaponExp: entity.skillController != null ? entity.skillController.GetWeaponExp(Id) : 0);
        }

        /// <summary>远程发射点：有武器时从「本技能所在槽位」的悬浮武器处发射（与客户端显示同一挂点）；无武器用通用发射点。</summary>
        protected Vector3 GetShootPos(EntityData entity)
        {
            if (!Weapon.IsValid) return entity.BulletShootPos();
            int slot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            return entity.GetWeaponFloatPos(slot < 0 ? 0 : slot);
        }

        /// <summary>按角色持有的武器下标取特效（发数多于特效数时复用最后一个）。</summary>
        private static int Pick(int[] list, int index)
        {
            if (list == null || list.Length == 0) return -1;
            return list[Mathf.Min(index, list.Length - 1)];
        }

        /// <summary>播放特效：沿轨迹播放（子弹/护盾/Buff）或在该轨迹终点播放（范围魔法/魔法阵）。</summary>
        protected void PlayVfx(SkillVfxKind kind, int[] list, BulletTrajectory trajectory, int index, float lifeTime)
        {
            if (Tool.VfxManager == null || trajectory == null || kind == SkillVfxKind.None) return;
            if (kind == SkillVfxKind.Weapon) // 模板由技能自身的武器引用决定，不需要特效下标
            {
                Tool.VfxManager.PlayWeaponVFX(Weapon, trajectory, lifeTime);
                return;
            }
            int v = Pick(list, index);
            if (v < 0) return;
            switch (kind)
            {
                case SkillVfxKind.Bullet: Tool.VfxManager.PlayBulletVFX(v, trajectory, lifeTime); break;
                case SkillVfxKind.Shield: Tool.VfxManager.PlayShieldVFX(v, trajectory, lifeTime); break;
                case SkillVfxKind.Buff: Tool.VfxManager.PlayBuffVFX(v, trajectory, lifeTime); break;
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(v, trajectory.Lerp(1f), Quaternion.identity, lifeTime); break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(v, trajectory.Lerp(1f), lifeTime); break;
            }
        }
    }

    /// <summary>
    /// 弹道武器技能基类。构造参数顺序（数值由策划案 21 章读出，占位待调）：
    /// (id, CD, store, 武器类别, 段内下标, 施法动作, 弹道形态, 发数, 扇形展开角, 环形半径, 天降高度,
    ///  伤害倍率, 判定半径, 弹道时长, 特效类别, 特效下标, 落点特效类别, 落点特效下标)
    /// </summary>
    public abstract class ProjectileSkill : WeaponSkill
    {
        private readonly ProjectilePattern pattern;
        private readonly int shots;
        private readonly float spreadDeg;
        private readonly float circleRadius;
        private readonly float skyHeight;
        private readonly bool breakEndure;

        protected ProjectileSkill(int id, float cd, int store, WeaponCategory category, int weaponIndex,
            EntityAnim.AttackType castAnim, ProjectilePattern pattern, int shots, float spreadDeg,
            float rate, float radius, float life,
            SkillVfxKind vfxKind = SkillVfxKind.None, int[] vfx = null,
            float circleRadius = 3f, float skyHeight = 30f, bool breakEndure = false,
            SkillVfxKind landingVfxKind = SkillVfxKind.None, int[] landingVfx = null)
            : base(id, cd, store, category, weaponIndex, castAnim, vfxKind, vfx, rate, radius, life,
                  landingVfxKind, landingVfx)
        {
            this.pattern = pattern;
            this.shots = shots;
            this.spreadDeg = spreadDeg;
            this.circleRadius = circleRadius;
            this.skyHeight = skyHeight;
            this.breakEndure = breakEndure;
        }

        /// <summary>服务器：生成各发终点（扇形 / 环形 / 单点）。</summary>
        private Vector3[] BuildDests(EntityData entity, Vector3 origin, Vector3 dest)
        {
            switch (pattern)
            {
                case ProjectilePattern.Fan: return FanDests(origin, dest, shots, spreadDeg);
                case ProjectilePattern.Circle: return CircleDests(entity.transform.position, circleRadius, shots);
                default: return new[] { dest };
            }
        }

        /// <summary>服务器与客户端共用：按上下文重建第 index 发弹道。</summary>
        private BulletTrajectory BuildTrajectory(TrajectoryContext context, int index)
        {
            var shape = (ProjectilePattern)context.ints[1];
            Vector3 origin = context.vectors[index * 2];
            Vector3 target = context.vectors[index * 2 + 1];
            switch (shape)
            {
                case ProjectilePattern.SkyFall: return new SkyFallTrajectory(origin, target, skyHeight);
                case ProjectilePattern.Point: return new PointTrajectory(target);
                case ProjectilePattern.Self: return new FollowTrajectory((ushort)context.ints[2]);
                default: return new LineTrajectory(origin, target);
            }
        }

        protected override void Execute(EntityData entity, Vector3 dest)
        {
            var context = new TrajectoryContext();
            Vector3 origin = GetShootPos(entity);
            Vector3[] dests = BuildDests(entity, origin, dest);
            context.AddInts(dests.Length, (int)pattern, entity.id);
            for (int i = 0; i < dests.Length; i++) context.AddVectors(origin, dests[i]);

            if (pattern == ProjectilePattern.Point || pattern == ProjectilePattern.Self)
            {
                // 定点/自身类技能无弹道判定；效果（Buff / 召唤 / 位移 / 屏障）待实现
                // TODO: 依赖 Buff 系统与位移系统，先只做表现
            }
            else
            {
                var attack = BuildAttack(entity, breakEndure);
                for (int i = 0; i < dests.Length; i++)
                {
                    Tool.BattleManager?.ShootBullet(entity, attack, BuildTrajectory(context, i), life);
                }
            }
            BroadcastSkillCast(Id, context);
        }

        public override void PlayVFX(TrajectoryContext context)
        {
            if (context == null) return;
            int count = context.ints[0];
            for (int i = 0; i < count; i++)
            {
                BulletTrajectory trajectory = BuildTrajectory(context, i);
                PlayVfx(vfxKind, vfx, trajectory, i, life);
                PlayVfx(landingVfxKind, landingVfx, trajectory, i, life);
            }
        }
    }

    /// <summary>近战武器技能基类（无子弹）：动画攻击帧以手部骨骼为圆心做球判定；多段由动画 Hit1/Hit2 驱动。</summary>
    public abstract class MeleeWeaponSkill : WeaponSkill
    {
        private static readonly EntityData[] s_hitBuffer = new EntityData[16];

        private readonly bool leftHand;

        /// <summary>近战类：释放动作期间武器从悬浮位置到手部，动作结束清空。</summary>
        public override bool WeaponInHand => true;

        protected MeleeWeaponSkill(int id, float cd, int store, WeaponCategory category, int weaponIndex,
            EntityAnim.AttackType castAnim, bool leftHand, float rate, float radius,
            SkillVfxKind selfVfxKind = SkillVfxKind.None, int[] selfVfx = null)
            : base(id, cd, store, category, weaponIndex, castAnim, selfVfxKind, selfVfx, rate, radius, 0.5f)
        {
            this.leftHand = leftHand;
        }

        protected override void Execute(EntityData entity, Vector3 dest)
        {
            Vector3 center = entity.GetComponentInChildren<EntityAnim>().GetHandMount(leftHand).position;
            int count = EntityPhysics.OverlapSphere(center, radius, s_hitBuffer);
            var attack = BuildAttack(entity);
            for (int i = 0; i < count; i++)
            {
                var target = s_hitBuffer[i];
                if (!target.Alive) continue;
                if (target.id == entity.id || target.camp == entity.camp) continue;
                target.ProcessHit(attack, attack.GetDamage());
            }

            // 广播自身表现（客户端按同一上下文重建跟随轨迹播特效）
            var context = new TrajectoryContext();
            context.AddInts(1, (int)ProjectilePattern.Self, entity.id);
            context.AddVectors(entity.transform.position, entity.transform.position);
            BroadcastSkillCast(Id, context);
        }

        public override void PlayVFX(TrajectoryContext context)
        {
            if (context == null) return;
            // 近战特效跟随施放者自身播放
            PlayVfx(vfxKind, vfx, new FollowTrajectory((ushort)context.ints[2]), 0, life);
        }
    }
    #endregion

    /// <summary>武器技能池注册（50 件，id 见策划案第二十一章；实现见本文件下半部）。</summary>
    public static class SkillPoolWeapons
    {
        public static void RegisterAll()
        {
            // 近战刀 0~10
            SkillManager.Register(new SkillGaleSlash());
            SkillManager.Register(new SkillFlyingKnife());
            SkillManager.Register(new SkillCrossSlash());
            SkillManager.Register(new SkillChargeSlash());
            SkillManager.Register(new SkillWhirlSlash());
            SkillManager.Register(new SkillBreakEndureSlash());
            SkillManager.Register(new SkillFanSwordQi());
            SkillManager.Register(new SkillCaptureThrow());
            SkillManager.Register(new SkillShadowStrike());
            SkillManager.Register(new SkillMountainCrash());
            SkillManager.Register(new SkillBladeDance());
            // 长枪 11~17
            SkillManager.Register(new SkillJavelinThrow());
            SkillManager.Register(new SkillPierceSpear());
            SkillManager.Register(new SkillThunderSpear());
            SkillManager.Register(new SkillBindNail());
            SkillManager.Register(new SkillThrustDash());
            SkillManager.Register(new SkillSweepPole());
            SkillManager.Register(new SkillSpearWall());
            // 枪械 18~32
            SkillManager.Register(new SkillQuickShot());
            SkillManager.Register(new SkillFanShotgun());
            SkillManager.Register(new SkillArmorPiercer());
            SkillManager.Register(new SkillGrenade());
            SkillManager.Register(new SkillIncendiary());
            SkillManager.Register(new SkillFreezeRound());
            SkillManager.Register(new SkillParalysisRound());
            SkillManager.Register(new SkillToxicRound());
            SkillManager.Register(new SkillHeavyBreaker());
            SkillManager.Register(new SkillInspireRound());
            SkillManager.Register(new SkillImpactRound());
            SkillManager.Register(new SkillBlinkDash());
            SkillManager.Register(new SkillSnipe());
            SkillManager.Register(new SkillBulletSpray());
            SkillManager.Register(new SkillAirstrikeMark());
            // 魔法球 33~48
            SkillManager.Register(new SkillMagicBolt());
            SkillManager.Register(new SkillFireball());
            SkillManager.Register(new SkillIceShard());
            SkillManager.Register(new SkillWindBlade());
            SkillManager.Register(new SkillPoisonOrb());
            SkillManager.Register(new SkillRockfall());
            SkillManager.Register(new SkillThunderOrb());
            SkillManager.Register(new SkillLightShield());
            SkillManager.Register(new SkillPurgeWave());
            SkillManager.Register(new SkillInspireCircle());
            SkillManager.Register(new SkillFrostNova());
            SkillManager.Register(new SkillThunderFall());
            SkillManager.Register(new SkillBlackHoleBomb());
            SkillManager.Register(new SkillStarfall());
            SkillManager.Register(new SkillVoidGrasp());
            SkillManager.Register(new SkillArcaneBarrier());
        }
    }

    // ---- 近战刀 0~10（刀可飞出：无子弹的走 MeleeWeaponSkill，其余走 ProjectileSkill）----
    /// <summary>疾风斩（id 0·Knife）：单发直线剑气。</summary>
    public class SkillGaleSlash : ProjectileSkill
    {
        public SkillGaleSlash() : base(0, 1f, 15, WeaponCategory.Knife, 0, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Line, 1, 0f, 1.0f, 0.45f, 0.7f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Bullet, vfx: new[] { 39 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>飞刀投掷（id 1·Knife）：飞掷短刀，刀模型本身作弹体沿轨迹飞行。</summary>
    public class SkillFlyingKnife : ProjectileSkill
    {
        public SkillFlyingKnife() : base(1, 2f, 12, WeaponCategory.Knife, 1, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Line, 1, 0f, 1.2f, 0.45f, 0.9f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Weapon, vfx: null, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>十字斩（id 2·Knife）：两道交叉剑气（蓝 + 紫）。</summary>
    public class SkillCrossSlash : ProjectileSkill
    {
        public SkillCrossSlash() : base(2, 5f, 10, WeaponCategory.Knife, 2, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Fan, 2, 30f, 1.6f, 0.5f, 0.8f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Bullet, vfx: new[] { 39, 41 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>冲锋斩（id 3·Knife）：冲锋位移（位移系统待实现，先播环绕风表现）。</summary>
    public class SkillChargeSlash : ProjectileSkill
    {
        public SkillChargeSlash() : base(3, 5f, 10, WeaponCategory.Knife, 3, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Self, 1, 0f, 0f, 0f, 0.6f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Buff, vfx: new[] { 28 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>旋风斩（id 4·Knife）：环形八道剑气（旋风斩）。</summary>
    public class SkillWhirlSlash : ProjectileSkill
    {
        public SkillWhirlSlash() : base(4, 8f, 5, WeaponCategory.Knife, 4, EntityAnim.AttackType.Attack_Weapon_R_And_L,
            ProjectilePattern.Circle, 8, 0f, 1.8f, 0.5f, 0.5f,
            circleRadius: 3f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Bullet, vfx: new[] { 40 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>破霸重斩（id 5·Knife）：破霸重斩（命中可破霸体）。</summary>
    public class SkillBreakEndureSlash : ProjectileSkill
    {
        public SkillBreakEndureSlash() : base(5, 10f, 3, WeaponCategory.Knife, 5, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Line, 1, 0f, 2.2f, 0.5f, 0.8f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: true,
            vfxKind: SkillVfxKind.Bullet, vfx: new[] { 41 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>剑气纵横（id 6·Knife）：三道扇形剑气（蓝/橙/紫各一道）。</summary>
    public class SkillFanSwordQi : ProjectileSkill
    {
        public SkillFanSwordQi() : base(6, 3f, 12, WeaponCategory.Knife, 6, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Fan, 3, 30f, 1.3f, 0.45f, 0.8f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Bullet, vfx: new[] { 39, 40, 41 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>捕获投掷（id 7·Knife）：捕获投掷：禁锢圆环（钉身逻辑待实现）。</summary>
    public class SkillCaptureThrow : ProjectileSkill
    {
        public SkillCaptureThrow() : base(7, 12f, 4, WeaponCategory.Knife, 7, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Point, 1, 0f, 0f, 0f, 1.5f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.MagicCircle, vfx: new[] { 3 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>影袭（id 8·Knife）：影袭：起止法阵（位移系统待实现）。</summary>
    public class SkillShadowStrike : ProjectileSkill
    {
        public SkillShadowStrike() : base(8, 10f, 4, WeaponCategory.Knife, 8, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Point, 1, 0f, 0f, 0f, 1.2f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.MagicCircle, vfx: new[] { 7 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>崩山击（id 9·Knife）：崩山击：跃起砸地，落点小型爆炸。</summary>
    public class SkillMountainCrash : ProjectileSkill
    {
        public SkillMountainCrash() : base(9, 6f, 10, WeaponCategory.Knife, 9, EntityAnim.AttackType.Jump_Mega,
            ProjectilePattern.Point, 1, 0f, 2.0f, 1.2f, 1.2f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.RangeMagic, vfx: new[] { 5 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>刃舞连斩（id 10·Knife）：刃舞连斩：近战球判定（末端剑气待做）。</summary>
    public class SkillBladeDance : MeleeWeaponSkill
    {
        public SkillBladeDance() : base(10, 4f, 12, WeaponCategory.Knife, 10, EntityAnim.AttackType.Attack_Weapon_R,
            leftHand: false, rate: 1.4f, radius: 0.6f, selfVfxKind: SkillVfxKind.Bullet, selfVfx: new[] { 40 }) { }
    }


    // ---- 长枪 11~18 ----
    /// <summary>投枪（id 11·Spear）：投掷长枪，枪模型本身作弹体沿轨迹飞行。</summary>
    public class SkillJavelinThrow : ProjectileSkill
    {
        public SkillJavelinThrow() : base(11, 1.5f, 15, WeaponCategory.Spear, 0, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Line, 1, 0f, 1.2f, 0.45f, 1.0f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Weapon, vfx: null, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>贯穿之枪（id 12·Spear）：贯穿之枪：细长直刺（子弹本身穿透）。</summary>
    public class SkillPierceSpear : ProjectileSkill
    {
        public SkillPierceSpear() : base(12, 4f, 10, WeaponCategory.Spear, 1, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Line, 1, 0f, 1.6f, 0.5f, 1.2f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Bullet, vfx: new[] { 51 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>落雷枪（id 13·Spear）：枪模型升空后从天砸下，落点小型爆炸。</summary>
    public class SkillThunderSpear : ProjectileSkill
    {
        public SkillThunderSpear() : base(13, 10f, 3, WeaponCategory.Spear, 2, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.SkyFall, 1, 0f, 1.9f, 0.8f, 1.2f,
            circleRadius: 0f, skyHeight: 30f, breakEndure: false,
            vfxKind: SkillVfxKind.Weapon, vfx: null, landingVfxKind: SkillVfxKind.RangeMagic, landingVfx: new[] { 5 }) { }
    }

    /// <summary>束缚钉（id 14·Spear）：束缚钉：落点禁锢圆环（束缚逻辑待实现）。</summary>
    public class SkillBindNail : ProjectileSkill
    {
        public SkillBindNail() : base(14, 12f, 4, WeaponCategory.Spear, 3, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Point, 1, 0f, 0f, 0f, 1.5f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.MagicCircle, vfx: new[] { 3 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>突进刺（id 15·Spear）：突进刺（位移系统待实现，先播环绕风表现）。</summary>
    public class SkillThrustDash : ProjectileSkill
    {
        public SkillThrustDash() : base(15, 5f, 10, WeaponCategory.Spear, 4, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Self, 1, 0f, 0f, 0f, 0.6f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.Buff, vfx: new[] { 28 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>横扫千军（id 16·Spear）：横扫千军：近战大范围球判定（纯物理击退，击退逻辑待做）。</summary>
    public class SkillSweepPole : MeleeWeaponSkill
    {
        public SkillSweepPole() : base(16, 6f, 10, WeaponCategory.Spear, 5, EntityAnim.AttackType.Attack_Weapon_R,
            leftHand: false, rate: 1.5f, radius: 0.9f, selfVfxKind: SkillVfxKind.None, selfVfx: null) { }
    }

    /// <summary>枪阵屏障（id 17·Spear）：枪阵屏障：落点力场（屏障逻辑待实现）。</summary>
    public class SkillSpearWall : ProjectileSkill
    {
        public SkillSpearWall() : base(17, 20f, 2, WeaponCategory.Spear, 6, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.Point, 1, 0f, 0f, 0f, 2f,
            circleRadius: 0f, skyHeight: 0f, breakEndure: false,
            vfxKind: SkillVfxKind.MagicCircle, vfx: new[] { 1 }, landingVfxKind: SkillVfxKind.None, landingVfx: null) { }
    }

    /// <summary>枪雨（id 18·Spear）：枪雨：多发升空天降 + 落点小型爆炸。</summary>
    public class SkillSpearRain : ProjectileSkill
    {
        public SkillSpearRain() : base(18, 15f, 3, WeaponCategory.Spear, 7, EntityAnim.AttackType.Attack_Weapon_R,
            ProjectilePattern.SkyFall, 5, 0f, 2.4f, 0.7f, 1.5f,
            circleRadius: 0f, skyHeight: 30f, breakEndure: false,
            vfxKind: SkillVfxKind.Bullet, vfx: new[] { 47 }, landingVfxKind: SkillVfxKind.RangeMagic, landingVfx: new[] { 5 }) { }
    }


    // ---- 枪械 19~33 / 魔法球 34~49：效果待实现（下一批补，轨迹与特效绑定见策划案 21.3/21.4）----
    /// <summary>快速射击（id 19）：占位，效果待实现。</summary>
    public class SkillQuickShot : SkillStub { public SkillQuickShot() : base(19, 0.5f, 20, new WeaponRef(WeaponCategory.Gun, 0)) { } }
    /// <summary>扇形散射（id 20）：占位，效果待实现。</summary>
    public class SkillFanShotgun : SkillStub { public SkillFanShotgun() : base(20, 2f, 15, new WeaponRef(WeaponCategory.Gun, 1)) { } }
    /// <summary>穿甲弹（id 21）：占位，效果待实现。</summary>
    public class SkillArmorPiercer : SkillStub { public SkillArmorPiercer() : base(21, 2f, 12, new WeaponRef(WeaponCategory.Gun, 2)) { } }
    /// <summary>榴弹（id 22）：占位，效果待实现。</summary>
    public class SkillGrenade : SkillStub { public SkillGrenade() : base(22, 3.5f, 10, new WeaponRef(WeaponCategory.Gun, 3)) { } }
    /// <summary>燃烧弹（id 23）：占位，效果待实现。</summary>
    public class SkillIncendiary : SkillStub { public SkillIncendiary() : base(23, 4f, 12, new WeaponRef(WeaponCategory.Gun, 4)) { } }
    /// <summary>冻结弹（id 24）：占位，效果待实现。</summary>
    public class SkillFreezeRound : SkillStub { public SkillFreezeRound() : base(24, 5f, 10, new WeaponRef(WeaponCategory.Gun, 5)) { } }
    /// <summary>麻痹弹（id 25）：占位，效果待实现。</summary>
    public class SkillParalysisRound : SkillStub { public SkillParalysisRound() : base(25, 5f, 10, new WeaponRef(WeaponCategory.Gun, 6)) { } }
    /// <summary>毒气弹（id 26）：占位，效果待实现。</summary>
    public class SkillToxicRound : SkillStub { public SkillToxicRound() : base(26, 6f, 10, new WeaponRef(WeaponCategory.Gun, 7)) { } }
    /// <summary>破甲重弹（id 27）：占位，效果待实现。</summary>
    public class SkillHeavyBreaker : SkillStub { public SkillHeavyBreaker() : base(27, 4f, 12, new WeaponRef(WeaponCategory.Gun, 8)) { } }
    /// <summary>激励弹（id 28）：占位，效果待实现。</summary>
    public class SkillInspireRound : SkillStub { public SkillInspireRound() : base(28, 8f, 10, new WeaponRef(WeaponCategory.Gun, 9)) { } }
    /// <summary>冲击弹（id 29）：占位，效果待实现。</summary>
    public class SkillImpactRound : SkillStub { public SkillImpactRound() : base(29, 8f, 10, new WeaponRef(WeaponCategory.Gun, 10)) { } }
    /// <summary>闪现突进（位移）（id 30）：占位，效果待实现。</summary>
    public class SkillBlinkDash : SkillStub { public SkillBlinkDash() : base(30, 12f, 4, new WeaponRef(WeaponCategory.Gun, 11)) { } }
    /// <summary>狙击（id 31）：占位，效果待实现。</summary>
    public class SkillSnipe : SkillStub { public SkillSnipe() : base(31, 10f, 5, new WeaponRef(WeaponCategory.Gun, 12)) { } }
    /// <summary>弹幕扫射（id 32）：占位，效果待实现。</summary>
    public class SkillBulletSpray : SkillStub { public SkillBulletSpray() : base(32, 15f, 3, new WeaponRef(WeaponCategory.Gun, 13)) { } }
    /// <summary>空袭标记（id 33）：占位，效果待实现。</summary>
    public class SkillAirstrikeMark : SkillStub { public SkillAirstrikeMark() : base(33, 20f, 1, new WeaponRef(WeaponCategory.Gun, 14)) { } }
    /// <summary>魔弹（id 34）：占位，效果待实现。</summary>
    public class SkillMagicBolt : SkillStub { public SkillMagicBolt() : base(34, 0.6f, 20, new WeaponRef(WeaponCategory.MagicOrb, 0)) { } }
    /// <summary>火球（id 35）：占位，效果待实现。</summary>
    public class SkillFireball : SkillStub { public SkillFireball() : base(35, 3f, 12, new WeaponRef(WeaponCategory.MagicOrb, 1)) { } }
    /// <summary>冰锥术（id 36）：占位，效果待实现。</summary>
    public class SkillIceShard : SkillStub { public SkillIceShard() : base(36, 3f, 12, new WeaponRef(WeaponCategory.MagicOrb, 2)) { } }
    /// <summary>风刃（id 37）：占位，效果待实现。</summary>
    public class SkillWindBlade : SkillStub { public SkillWindBlade() : base(37, 2.5f, 12, new WeaponRef(WeaponCategory.MagicOrb, 3)) { } }
    /// <summary>毒珠（id 38）：占位，效果待实现。</summary>
    public class SkillPoisonOrb : SkillStub { public SkillPoisonOrb() : base(38, 4f, 10, new WeaponRef(WeaponCategory.MagicOrb, 4)) { } }
    /// <summary>岩崩（id 39）：占位，效果待实现。</summary>
    public class SkillRockfall : SkillStub { public SkillRockfall() : base(39, 5f, 10, new WeaponRef(WeaponCategory.MagicOrb, 5)) { } }
    /// <summary>雷球（id 40）：占位，效果待实现。</summary>
    public class SkillThunderOrb : SkillStub { public SkillThunderOrb() : base(40, 5f, 10, new WeaponRef(WeaponCategory.MagicOrb, 6)) { } }
    /// <summary>光盾（id 41）：占位，效果待实现。</summary>
    public class SkillLightShield : SkillStub { public SkillLightShield() : base(41, 10f, 8, new WeaponRef(WeaponCategory.MagicOrb, 7)) { } }
    /// <summary>净化波动（id 42）：占位，效果待实现。</summary>
    public class SkillPurgeWave : SkillStub { public SkillPurgeWave() : base(42, 10f, 8, new WeaponRef(WeaponCategory.MagicOrb, 8)) { } }
    /// <summary>激励法阵（id 43）：占位，效果待实现。</summary>
    public class SkillInspireCircle : SkillStub { public SkillInspireCircle() : base(43, 10f, 8, new WeaponRef(WeaponCategory.MagicOrb, 9)) { } }
    /// <summary>冰霜新星（id 44）：占位，效果待实现。</summary>
    public class SkillFrostNova : SkillStub { public SkillFrostNova() : base(44, 10f, 6, new WeaponRef(WeaponCategory.MagicOrb, 10)) { } }
    /// <summary>落雷术（id 45）：占位，效果待实现。</summary>
    public class SkillThunderFall : SkillStub { public SkillThunderFall() : base(45, 10f, 5, new WeaponRef(WeaponCategory.MagicOrb, 11)) { } }
    /// <summary>黑洞炸弹（id 46）：占位，效果待实现。</summary>
    public class SkillBlackHoleBomb : SkillStub { public SkillBlackHoleBomb() : base(46, 5f, 8, new WeaponRef(WeaponCategory.MagicOrb, 12)) { } }
    /// <summary>星辰坠落（id 47）：占位，效果待实现。</summary>
    public class SkillStarfall : SkillStub { public SkillStarfall() : base(47, 20f, 3, new WeaponRef(WeaponCategory.MagicOrb, 13)) { } }
    /// <summary>虚空之握（id 48）：占位，效果待实现。</summary>
    public class SkillVoidGrasp : SkillStub { public SkillVoidGrasp() : base(48, 15f, 4, new WeaponRef(WeaponCategory.MagicOrb, 14)) { } }
    /// <summary>奥术屏障（id 49）：占位，效果待实现。</summary>
    public class SkillArcaneBarrier : SkillStub { public SkillArcaneBarrier() : base(49, 20f, 2, new WeaponRef(WeaponCategory.MagicOrb, 15)) { } }
}
