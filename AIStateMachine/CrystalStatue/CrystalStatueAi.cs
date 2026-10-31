using UnityEngine;

// 水晶守护塔：定时索敌，范围内有敌人才从技能表随机放一个；不移动、不转向、不复活
public class CrystalStatueAi : AiStateMachine
{
    public readonly CrystalStatueEntityData Owner;

    private readonly WaitState waitState;

    // 施法进度 0~1：满一轮出手并归零
    private float castProgress;

    public CrystalStatueAi(CrystalStatueEntityData owner)
    {
        Owner = owner;
        ThinkInterval = 0f; // 每帧推进进度，进到 1 才索敌
        // 首帧错峰：原口径把首次施法定在 interval×phase 之后，换算成进度起点即 1 - phase
        castProgress = 1f - owner.AIStaggerPhase;

        waitState = new WaitState(this);
        Initialize(waitState, 0f);
    }

    public override void Tick()
    {
        if (!Owner.Alive) return; // 被摧毁后不再开火，实体销毁由战斗管理器负责
        base.Tick();
    }

    #region//Local
    private float CastInterval => ConfigManager.GlobalVariableConfig.status_auto_cast_interval_crystal;

    // 满一轮即归零：本轮无敌人同样按下轮重新计（与原实现"到点先重置、再索敌"一致）
    private void AdvanceCast()
    {
        castProgress += Time.deltaTime / CastInterval;
        if (castProgress < 1f) return;
        castProgress = 0f;
        if (!HasEnemyInRange()) return;
        CastRandomSkill();
    }

    private bool HasEnemyInRange() =>
        BattleManager.EntityContainer.GetNearestEnemy(Owner, Config.crystal_statue_attack_range) != null;

    private void CastRandomSkill()
    {
        var sc = Owner.skillController;
        if (sc == null || sc.SkillCount == 0) return;
        sc.TryUseSkill(sc.GetSkillIdAt(Random.Range(0, sc.SkillCount)));
    }

    // 当前只有一种行为；将来要加档（如核心被袭提速）就新增状态并在这里返回它
    private sealed class WaitState : AiState
    {
        private readonly CrystalStatueAi ai;

        public WaitState(CrystalStatueAi ai) { this.ai = ai; }

        public override void Enter() { }

        public override AiState Tick(float deltaTime)
        {
            ai.AdvanceCast();
            return null;
        }
    }
    #endregion
}
