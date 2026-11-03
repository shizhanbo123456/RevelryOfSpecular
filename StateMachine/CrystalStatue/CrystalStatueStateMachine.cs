using Ros.Skill;
using UnityEngine;

// 水晶守护塔：技能冷却好了且范围内有敌人就放一发；不移动、不转向、不复活
public class CrystalStatueStateMachine : DecisionMachine, IBehaviorHost
{
    public readonly CrystalStatueEntityData Owner;

    private readonly BehaviorArbiter arbiter;
    private readonly CastState castState;

    public CrystalStatueStateMachine(CrystalStatueEntityData owner)
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
        if (!Owner.Alive) return; // 被摧毁后不再开火，实体销毁由战斗管理器负责
        base.Tick();
        arbiter.Tick(Time.deltaTime);
    }

    #region//Local
    // 带灵火时改放灵火喷吐（打脚 + 到点爆炸），灵火到期自动回到普通孢子
    private int CurrentSkillId
    {
        get
        {
            bool blaze = Owner.effectController != null && Owner.effectController.HasEffect(EffectType.StatueBlaze);
            return blaze ? (int)SkillType.SkillCrystalStatueSporeBlaze : (int)SkillType.SkillCrystalStatueSporeA;
        }
    }

    private bool SkillReady => IsSkillReady(CurrentSkillId);

    private bool HasEnemyInRange() =>
        BattleManager.EntityContainer.GetNearestEnemy(Owner, Config.crystal_statue_attack_range) != null;

    private void RequestIdle() => arbiter.RequestIdle();

    private void RequestAttack() => arbiter.RequestAttack(null);
    #endregion

    #region//IBehaviorHost
    EntityData IBehaviorHost.Owner => Owner;

    public void Stop() => Owner.StopMoving();

    public void FaceTarget(EntityData target) { } // 雕像不转向

    public bool TryAttack(EntityData target) => TryUseSkill(CurrentSkillId);

    public bool IsActing => Owner.anim != null && Owner.anim.CurrentState == EntityAnim.AnimState.Attack;

    public void ClearFace() { }

    public void NotifyStuck() { } // 不移动，不存在卡住
    #endregion

    #region//DecisionStates
    // 当前只有一种行为；将来要加档（如核心被袭提速）就新增决策态并在这里返回它
    private sealed class CastState : DecisionState
    {
        // 索敌是范围查询：冷却没好时不查，就绪后按此间隔扫描
        private const float ScanInterval = 0.2f;

        private readonly CrystalStatueStateMachine machine;
        private float nextScanTime;

        public CastState(CrystalStatueStateMachine machine) { this.machine = machine; }

        public override void Enter() { }

        public override DecisionState Decide()
        {
            if (machine.IsActing)
            {
                machine.RequestAttack(); // 施法动作未播完：压住
                return null;
            }

            if (!machine.SkillReady)
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
