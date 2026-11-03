using UnityEngine;

// 僵尸：无目标在出生点附近游荡；有目标按距离分档——超追距放弃、远处嘶吼、贴身停下爪击
public class ZombieStateMachine : DecisionMachine, IBehaviorHost
{
    private const int SlotClawRight = 0;
    private const int SlotClawLeft = 1;
    private const int SlotRoar = 2;

    public readonly ZombieEntityData Owner;

    private readonly BehaviorArbiter arbiter;
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

    public ZombieStateMachine(ZombieEntityData owner)
    {
        Owner = owner;
        homePosition = owner.transform.position;
        ThinkInterval = Config.zombie_decide_interval;

        wanderState = new WanderState(this);
        chaseState = new ChaseState(this);
        attackState = new AttackState(this);
        arbiter = new BehaviorArbiter(this);
        Initialize(wanderState, owner.AutoStaggerPhase);
    }

    protected override EntityData MachineOwner => Owner;

    public override void Tick()
    {
        if (!Owner.Alive) return;
        base.Tick();
        arbiter.Tick(Time.deltaTime);
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

    private bool CanUseSlot(int slot) => IsSkillReady(SkillIdAt(slot));

    // 左右爪交替：主手不放再试另一只，避免同一只手连击；普通攻击不计冷却
    private bool TryClaw()
    {
        clawLeftNext = !clawLeftNext;
        int first = clawLeftNext ? SlotClawLeft : SlotClawRight;
        if (TryUseSkillNoCd(SkillIdAt(first))) return true;
        return TryUseSkillNoCd(SkillIdAt(clawLeftNext ? SlotClawRight : SlotClawLeft));
    }

    private void RequestIdle() => arbiter.RequestIdle();

    private void RequestMove(Vector3 point) => arbiter.RequestMove(point);

    private void RequestChase(EntityData target) => arbiter.RequestChase(target);

    private void RequestAttack(EntityData target) => arbiter.RequestAttack(target);
    #endregion

    #region//IBehaviorHost
    EntityData IBehaviorHost.Owner => Owner;

    public void Stop() => Owner.Stop();

    public void FaceTarget(EntityData target) => Owner.SetFaceTarget(target != null ? target.id : (ushort)0);

    // 远距离用嘶吼（计冷却），近距离用爪击（不计冷却）
    public bool TryAttack(EntityData target)
    {
        if (target == null) return false;
        if (FlatDistance(target.transform.position) > Config.zombie_roar_range) return TryUseSkill(SkillIdAt(SlotRoar));
        return TryClaw();
    }

    public bool IsActing => Owner.anim != null && Owner.anim.CurrentState == EntityAnim.AnimState.Attack;

    public void ClearFace() => Owner.SetFaceTarget(0);

    // 卡住由 OnThink 统一处理（忽略该目标一段时间），行为层不必额外做
    public void NotifyStuck() { }
    #endregion

    #region//DecisionStates
    private sealed class WanderState : DecisionState
    {
        private readonly ZombieStateMachine machine;

        public WanderState(ZombieStateMachine machine) { this.machine = machine; }

        public override void Enter() => machine.Owner.SetFaceTarget(0); // 已无目标，别再朝旧目标转

        public override DecisionState Decide()
        {
            if (machine.currentTarget != null) return machine.chaseState;

            if (machine.hasWanderPoint)
            {
                if (machine.FlatDistance(machine.wanderPoint) > ZombieEntityData.ArriveRadius)
                {
                    machine.RequestMove(machine.wanderPoint); // 还在路上
                    return null;
                }
                machine.hasWanderPoint = false;
                machine.wanderResumeTime = Time.time + Config.zombie_wander_pause;
                machine.RequestIdle();
                return null;
            }

            if (Time.time < machine.wanderResumeTime)
            {
                machine.RequestIdle(); // 到点后的停顿
                return null;
            }

            Vector2 offset = Random.insideUnitCircle * Config.zombie_wander_radius;
            machine.wanderPoint = machine.homePosition + new Vector3(offset.x, 0f, offset.y);
            machine.hasWanderPoint = true;
            machine.RequestMove(machine.wanderPoint);
            return null;
        }
    }

    private sealed class ChaseState : DecisionState
    {
        private readonly ZombieStateMachine machine;

        public ChaseState(ZombieStateMachine machine) { this.machine = machine; }

        public override void Enter() => machine.Owner.SetFaceTarget(0); // 追击时朝向跟着行进方向

        public override DecisionState Decide()
        {
            var target = machine.currentTarget;
            if (target == null) return machine.wanderState;

            float dist = machine.FlatDistance(target.transform.position);
            if (dist > Config.zombie_max_chase_distance)
            {
                machine.ClearTarget(); // 拉开太远：放弃，回出生点游荡
                return machine.wanderState;
            }
            if (dist <= Config.zombie_attack_range) return machine.attackState;

            // 远处且嘶吼冷却好了：进攻击态放一嗓子，放完由攻击态切回追击
            if (dist > Config.zombie_roar_range && machine.CanUseSlot(SlotRoar)) return machine.attackState;

            machine.RequestChase(target); // 跟随目标物体
            return null;
        }
    }

    private sealed class AttackState : DecisionState
    {
        private readonly ZombieStateMachine machine;

        public AttackState(ZombieStateMachine machine) { this.machine = machine; }

        public override DecisionState Decide()
        {
            var target = machine.currentTarget;
            if (target == null) return machine.wanderState; // 目标丢了：直接回游荡，不必经追击态多等一拍

            // 动作没播完：压住，不出新招也不切状态
            if (machine.IsActing)
            {
                machine.RequestAttack(target);
                return null;
            }

            float dist = machine.FlatDistance(target.transform.position);

            if (dist <= Config.zombie_attack_range)
            {
                machine.RequestAttack(target);
                return null;
            }

            if (dist > Config.zombie_roar_range && machine.CanUseSlot(SlotRoar))
            {
                machine.RequestAttack(target);
                return null;
            }

            // 退出距离大于进入距离：目标在攻击距离边界晃动时不会在追击/攻击之间反复切换
            if (dist > Config.zombie_attack_range * Config.zombie_attack_exit_scale) return machine.chaseState;

            // 退出缓冲区内继续贴近，保持攻击态，不切状态也不出招
            machine.RequestChase(target);
            return null;
        }
    }
    #endregion
}
