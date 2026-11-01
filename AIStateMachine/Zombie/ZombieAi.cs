using UnityEngine;

// 僵尸：无目标在出生点附近游荡；有目标按距离分档——超追距放弃、远处嘶吼、贴身停下爪击
public class ZombieAi : AiStateMachine
{
    private const int SlotClawRight = 0;
    private const int SlotClawLeft = 1;
    private const int SlotRoar = 2;

    public readonly ZombieEntityData Owner;

    private readonly WanderState wanderState;
    private readonly ChaseState chaseState;
    private readonly AttackState attackState;

    private readonly Vector3 homePosition;
    private Vector3 wanderPoint;
    private bool hasWanderPoint;
    private float wanderResumeTime;

    private ushort targetId;
    private EntityData currentTarget;
    private bool clawLeftNext;

    public ZombieAi(ZombieEntityData owner)
    {
        Owner = owner;
        homePosition = owner.transform.position;
        ThinkInterval = Config.zombie_decide_interval;

        wanderState = new WanderState(this);
        chaseState = new ChaseState(this);
        attackState = new AttackState(this);
        Initialize(wanderState, owner.AIStaggerPhase);
    }

    public override void Tick()
    {
        if (!Owner.Alive) return;
        base.Tick();
    }

    #region//Local
    private ushort ignoredTargetId;
    private float ignoreTargetUntil;

    // 每 tick 先刷新目标，三个状态共用同一份结论
    protected override void OnThink()
    {
        // 目标连续算不出路：忽略一段时间，避免放弃后下一拍又锁回同一个走不到的目标
        if (Owner.NavUnreachable && currentTarget != null)
        {
            ignoredTargetId = currentTarget.id;
            ignoreTargetUntil = Time.time + Config.ai_ignore_unreachable_time;
            ClearTarget();
        }

        currentTarget = AcquireTarget();
    }

    private bool IsIgnored(EntityData entity)
        => entity != null && entity.id == ignoredTargetId && Time.time < ignoreTargetUntil;

    private EntityData AcquireTarget()
    {
        float range = AcquireRange;
        if (targetId != 0 && BattleManager.EntityContainer.Entities.TryGetObject(targetId, out var locked))
        {
            if (locked != null && locked.Alive && !IsIgnored(locked) && FlatDistance(locked.transform.position) <= range) return locked;
        }

        var nearest = BattleManager.EntityContainer.GetNearestEnemy(Owner, range);
        if (IsIgnored(nearest)) nearest = null;
        targetId = nearest != null ? nearest.id : (ushort)0;
        return nearest;
    }

    // 视野夹到「最大追击距离 - 余量」内：等于上限时刚索到就会超限放弃，追击/游荡每拍互踢
    private float AcquireRange
    {
        get
        {
            float view = Owner.floatingAttribute.viewDistance > 0f
                ? Owner.floatingAttribute.viewDistance
                : Config.zombie_acquire_range;
            return Mathf.Min(view, Config.zombie_max_chase_distance - Config.zombie_acquire_margin);
        }
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 d = point - Owner.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private void ClearTarget()
    {
        targetId = 0;
        currentTarget = null;
    }

    // 左右爪交替：主手不放再试另一只，避免同一只手连击
    private void TryClaw()
    {
        clawLeftNext = !clawLeftNext;
        int first = clawLeftNext ? SlotClawLeft : SlotClawRight;
        if (TryUseSlot(first)) return;
        TryUseSlot(clawLeftNext ? SlotClawRight : SlotClawLeft);
    }

    private void TryRoar() => TryUseSlot(SlotRoar);

    private bool TryUseSlot(int slot)
    {
        if (Owner.InputBlocked(InputBlockOp.Attack)) return false; // 攻击屏蔽：爪击/嘶吼均不放
        var sc = Owner.skillController;
        if (sc == null) return false;
        return sc.TryUseSkill(sc.GetSkillIdAt(slot)) == EntitySkillController.UseResult.Success;
    }

    private sealed class WanderState : AiState
    {
        private readonly ZombieAi ai;

        public WanderState(ZombieAi ai) { this.ai = ai; }

        public override void Enter() { ai.Owner.SetFaceTarget(0); } // 已无目标，别再朝旧目标转

        public override AiState Tick(float deltaTime)
        {
            if (ai.currentTarget != null) return ai.chaseState;

            if (ai.hasWanderPoint)
            {
                if (ai.FlatDistance(ai.wanderPoint) > ZombieEntityData.ArriveRadius) return null; // 还在路上
                ai.hasWanderPoint = false;
                ai.Owner.Stop();
                ai.wanderResumeTime = Time.time + Config.zombie_wander_pause;
                return null;
            }

            if (Time.time < ai.wanderResumeTime) return null; // 到点后的停顿

            Vector2 offset = Random.insideUnitCircle * Config.zombie_wander_radius;
            ai.wanderPoint = ai.homePosition + new Vector3(offset.x, 0f, offset.y);
            ai.hasWanderPoint = true;
            ai.Owner.MoveTo(ai.wanderPoint);
            return null;
        }
    }

    private sealed class ChaseState : AiState
    {
        private readonly ZombieAi ai;

        public ChaseState(ZombieAi ai) { this.ai = ai; }

        public override void Enter() { ai.Owner.SetFaceTarget(0); } // 追击时朝向跟着行进方向

        public override AiState Tick(float deltaTime)
        {
            var target = ai.currentTarget;
            if (target == null) return ai.wanderState;

            float dist = ai.FlatDistance(target.transform.position);
            if (dist > Config.zombie_max_chase_distance)
            {
                ai.ClearTarget(); // 拉开太远：放弃，回出生点游荡
                return ai.wanderState;
            }
            if (dist <= Config.zombie_attack_range) return ai.attackState;

            ai.Owner.MoveTo(target.transform.position);
            if (dist > Config.zombie_roar_range) ai.TryRoar(); // 近了直接近战更优，只在远处嘶吼
            return null;
        }
    }

    private sealed class AttackState : AiState
    {
        private readonly ZombieAi ai;

        public AttackState(ZombieAi ai) { this.ai = ai; }

        // 进入即贴身：停步、锁朝向、当帧出爪
        public override void Enter()
        {
            ai.Owner.Stop();
            var target = ai.currentTarget;
            if (target != null) ai.Owner.SetFaceTarget(target.id);
            ai.TryClaw();
        }

        public override AiState Tick(float deltaTime)
        {
            var target = ai.currentTarget;
            if (target == null) return ai.wanderState; // 目标丢了：直接回游荡，不必经追击态多等一个 tick

            float dist = ai.FlatDistance(target.transform.position);

            // 退出距离大于进入距离：目标在攻击距离边界晃动时不会在追击/攻击之间反复切换
            if (dist > Config.zombie_attack_range * Config.zombie_attack_exit_scale) return ai.chaseState;

            if (dist > Config.zombie_attack_range)
            {
                // 退出缓冲区内继续贴近，保持攻击态，不切状态也不出爪
                ai.Owner.MoveTo(target.transform.position);
                ai.Owner.SetFaceTarget(target.id);
                return null;
            }

            ai.Owner.Stop(); // 停下后仍要能转到目标（朝向由 OnTickMove 的无目的地分支负责）
            ai.Owner.SetFaceTarget(target.id);
            ai.TryClaw();
            return null;
        }
    }
    #endregion
}
