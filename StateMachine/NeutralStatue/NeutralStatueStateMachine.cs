using UnityEngine;

// 中立雕像：技能冷却好了且范围内有敌人就随机放一个；不移动、不转向
public class NeutralStatueStateMachine : DecisionMachine, IBehaviorHost
{
    public readonly NeutralStatueEntityData Owner;

    private readonly BehaviorArbiter arbiter;
    private readonly CastState castState;

    public NeutralStatueStateMachine(NeutralStatueEntityData owner)
    {
        Owner = owner;
        ThinkInterval = 0f; // 每帧检查，冷却与索敌查询都在状态里节流

        castState = new CastState(this);
        arbiter = new BehaviorArbiter(this);
        Initialize(castState, 0f);
    }

    protected override EntityData MachineOwner => Owner;

    public override void Tick()
    {
        if (!Owner.Alive) return; // 死亡后不再施法，实体销毁由战斗管理器负责
        TickSkillRecharge();
        base.Tick();
        arbiter.Tick(Time.deltaTime);
    }

    #region//Local
    private bool HasEnemyInRange() =>
        BattleManager.EntityContainer.GetNearestEnemy(Owner, Config.neutral_statue_attack_range) != null;

    private void RequestIdle() => arbiter.RequestIdle();

    private void RequestAttack() => arbiter.RequestAttack(null);

    // 随机挑一个冷却好了的技能；全没好返回 false
    private bool TryCastRandom()
    {
        var sc = Owner.skillController;
        if (sc == null || sc.SkillCount == 0) return false;
        int start = Random.Range(0, sc.SkillCount);
        for (int i = 0; i < sc.SkillCount; i++)
        {
            if (TryUseSkill(sc.GetSkillIdAt((start + i) % sc.SkillCount))) return true;
        }
        return false;
    }

    private bool AnySkillReady()
    {
        var sc = Owner.skillController;
        if (sc == null || sc.SkillCount == 0) return false;
        for (int i = 0; i < sc.SkillCount; i++)
        {
            if (IsSkillReady(sc.GetSkillIdAt(i))) return true;
        }
        return false;
    }
    #endregion

    #region//IBehaviorHost
    EntityData IBehaviorHost.Owner => Owner;

    public void Stop() => Owner.StopMoving();

    public void FaceTarget(EntityData target) { } // 雕像不转向

    public bool TryAttack(EntityData target) => TryCastRandom();

    public bool IsActing => Owner.anim != null && Owner.anim.CurrentState == EntityAnim.AnimState.Attack;

    public void ClearFace() { }

    public void NotifyStuck() { } // 不移动，不存在卡住
    #endregion

    #region//DecisionStates
    private sealed class CastState : DecisionState
    {
        // 索敌是范围查询：冷却没好时不查，就绪后按此间隔扫描
        private const float ScanInterval = 0.2f;

        private readonly NeutralStatueStateMachine machine;
        private float nextScanTime;

        public CastState(NeutralStatueStateMachine machine) { this.machine = machine; }

        public override DecisionState Decide()
        {
            if (machine.IsActing)
            {
                machine.RequestAttack(); // 施法动作未播完：压住
                return null;
            }

            if (!machine.AnySkillReady())
            {
                machine.RequestIdle();
                return null;
            }

            if (Time.time < nextScanTime)
            {
                machine.RequestIdle();
                return null;
            }
            nextScanTime = Time.time + ScanInterval;

            if (!machine.HasEnemyInRange())
            {
                machine.RequestIdle();
                return null;
            }

            machine.RequestAttack();
            return null;
        }
    }
    #endregion
}
