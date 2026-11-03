using UnityEngine;

// 中立雕像：定时索敌，范围内有敌人才从技能表随机放一个；生命值低于阈值转愤怒态，出招更快
public class NeutralStatueStateMachine : StateMachine
{
    public readonly NeutralStatueEntityData Owner;

    private readonly NormalState normalState;
    private readonly AngryState angryState;

    // 施法进度 0~1：两个状态共用，换档只改变推进速度，已积累的进度保留
    private float castProgress;

    public NeutralStatueStateMachine(NeutralStatueEntityData owner)
    {
        Owner = owner;
        ThinkInterval = 0f; // 每帧推进进度，进到 1 才索敌
        // 首帧错峰：原口径把首次施法定在 interval×phase 之后，换算成进度起点即 1 - phase
        castProgress = 1f - owner.AutoStaggerPhase;

        normalState = new NormalState(this);
        angryState = new AngryState(this);
        Initialize(IsAngry ? angryState : normalState, 0f);
    }

    public override void Tick()
    {
        if (!Owner.Alive) return; // 死亡后不再施法，实体销毁由战斗管理器负责
        base.Tick();
    }

    #region//Local
    private float NormalInterval => ConfigManager.GlobalVariableConfig.status_auto_cast_interval_neutral;

    private float AngryInterval => NormalInterval * Config.neutral_statue_angry_interval_ratio;

    // 血线只读实时值，不缓存：受击当帧就能换档
    private bool IsAngry =>
        Owner.floatingAttribute.health < Owner.baseAttribute.health * Config.neutral_statue_angry_health_ratio;

    // 满一轮即归零：本轮无敌人同样按下轮重新计（与原实现"到点先重置、再索敌"一致）
    private void AdvanceCast(float interval)
    {
        castProgress += Time.deltaTime / interval;
        if (castProgress < 1f) return;
        castProgress = 0f;
        if (!HasEnemyInRange()) return;
        CastRandomSkill();
    }

    private bool HasEnemyInRange() =>
        BattleManager.EntityContainer.GetNearestEnemy(Owner, Config.neutral_statue_attack_range) != null;

    private void CastRandomSkill()
    {
        var sc = Owner.skillController;
        if (sc == null || sc.SkillCount == 0) return;
        sc.TryUseSkill(sc.GetSkillIdAt(Random.Range(0, sc.SkillCount)));
    }

    private sealed class NormalState : State
    {
        private readonly NeutralStatueStateMachine robot;

        public NormalState(NeutralStatueStateMachine robot) { this.robot = robot; }

        public override void Enter() { }

        public override State Tick(float deltaTime)
        {
            if (robot.IsAngry) return robot.angryState; // 换档优先于出手
            robot.AdvanceCast(robot.NormalInterval);
            return null;
        }
    }

    private sealed class AngryState : State
    {
        private readonly NeutralStatueStateMachine robot;

        public AngryState(NeutralStatueStateMachine robot) { this.robot = robot; }

        public override void Enter() { } // 入场钩子：愤怒表现/播报后续挂这里

        public override State Tick(float deltaTime)
        {
            if (!robot.IsAngry) return robot.normalState; // 血量回到阈值以上（如后续加入回复）
            robot.AdvanceCast(robot.AngryInterval);
            return null;
        }
    }
    #endregion
}
