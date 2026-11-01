using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

// 人机玩家状态机：15 个状态 + 快捷消息任务系统（设计见 Doc/08-游戏AI逻辑）
public partial class AiPlayerAi : AiStateMachine
{
    // 任务来源消息（锚点由"是否携带位置"决定静态/动态）
    protected struct TaskTarget
    {
        public bool hasPosition;
        public Vector3 position;
        public EntityData sender;
    }

    public readonly PlayerEntityData Owner;

    private readonly AiPlayerSkillBar skillBar;

    private readonly SearchState searchState;
    private readonly SearchCombatState searchCombatState;
    private readonly ChaseState chaseState;
    private readonly CombatState combatState;
    private readonly EscortApproachState escortApproachState;
    private readonly EscortState escortState;
    private readonly CombatState escortCombatState;
    private readonly DangerEvadeState dangerEvadeState;
    private readonly DangerClearState dangerClearState;
    private readonly CombatState dangerCombatState;
    private readonly RallyApproachState rallyApproachState;
    private readonly RallyWaitState rallyWaitState;
    private readonly CombatState rallyGuardState;
    private readonly AssaultState assaultState;
    private readonly CombatState assaultCombatState;

    // 任务槽：单槽，后到覆盖
    private bool hasPending;
    private QuickMessageType pendingType;
    private TaskTarget pendingTarget;

    // 卡住检测：一段时间没有位移就换目标
    private Vector3 stuckSamplePos;
    private float stuckSampleTime;

    public AiPlayerAi(PlayerEntityData owner)
    {
        Owner = owner;
        skillBar = new AiPlayerSkillBar(owner);
        ThinkInterval = Config.ai_think_interval;

        searchState = new SearchState(this);
        searchCombatState = new SearchCombatState(this);
        chaseState = new ChaseState(this);
        combatState = new CombatState();
        escortApproachState = new EscortApproachState(this);
        escortState = new EscortState(this);
        escortCombatState = new CombatState();
        dangerEvadeState = new DangerEvadeState(this);
        dangerClearState = new DangerClearState(this);
        dangerCombatState = new CombatState();
        rallyApproachState = new RallyApproachState(this);
        rallyWaitState = new RallyWaitState(this);
        rallyGuardState = new CombatState();
        assaultState = new AssaultState(this);
        assaultCombatState = new CombatState();

        // 战斗态：撤离血线 / 活动范围 / 撤离去向 按战斗策略表配；脱战回到由 Bind 时传入
        combatState.Setup(Owner, this, Config.ai_combat_retreat_ratio, 0f, dangerEvadeState);
        escortCombatState.Setup(Owner, this, Config.ai_escort_retreat_ratio, Config.ai_escort_combat_radius * 1.5f, dangerEvadeState);
        dangerCombatState.Setup(Owner, this, Config.ai_combat_retreat_ratio, Config.ai_danger_clear_radius * 1.5f, dangerEvadeState);
        rallyGuardState.Setup(Owner, this, Config.ai_rally_retreat_ratio, Config.ai_guard_alert_radius, rallyApproachState);
        assaultCombatState.Setup(Owner, this, Config.ai_combat_retreat_ratio, Config.ai_assault_combat_activity, dangerEvadeState);

        stuckSamplePos = owner.transform.position;
        stuckSampleTime = Time.time;
        Initialize(searchState, owner.AIStaggerPhase);
    }

    public override void Tick()
    {
        if (!Owner.Alive) return;
        skillBar.Tick(Time.deltaTime);
        base.Tick();
    }

    protected override void OnThink()
    {
        skillBar.Rebuild(); // 持有表可能因掉落/复活变化，重排一次即"获得技能重新入栏"
        UpdateStuckSample();
    }

    #region//Local
    protected float HealthRatio
    {
        get
        {
            float cap = Owner.baseAttribute.health;
            return cap > 0f ? Owner.floatingAttribute.health / cap : 0f;
        }
    }

    protected float ViewDistance => Owner.floatingAttribute.viewDistance;

    // 静止一段时间说明目的地走不到
    private void UpdateStuckSample()
    {
        if (Time.time - stuckSampleTime < Config.ai_unreachable_pause) return;
        Vector3 delta = Owner.transform.position - stuckSamplePos;
        delta.y = 0f;
        stuckSampleTime = Time.time;
        stuckSamplePos = Owner.transform.position;
        if (delta.magnitude >= Config.ai_stuck_min_move) return;

        // 丢掉走不到的目的地，交给状态换目标，避免一直对着同一处撞
        Owner.StopMoving();
        Current?.OnStuck();
    }

    protected void MoveTowards(Vector3 point) => Owner.MoveTo(point);

    protected void FaceTarget(EntityData target)
    {
        if (target == null) return;
        Owner.SetAiFacePoint(target.transform.position);
    }

    // 站定攻击：有就绪技能就放，全部没好就原地站着（文档要求的"不空放"）
    protected bool TryAttack(EntityData target)
    {
        if (Owner.InputBlocked(InputBlockOp.Attack)) return false;
        return skillBar.TryCastAny(target);
    }

    protected EntityData NearestEnemy(bool includeCoral)
    {
        // 索敌半径取「视野」与「追击上限 - 余量」的较小者：等于上限时刚索到就会超限放弃，搜索/追击每拍互踢
        float radius = Mathf.Min(ViewDistance, Config.ai_chase_limit - Config.ai_acquire_margin);
        return NearestEnemyAround(Owner.transform.position, radius, includeCoral);
    }

    protected EntityData NearestEnemyAround(Vector3 center, float radius, bool includeCoral)
    {
        var hostile = EntityCampUtil.HostileOf(Owner.type.category);
        s_ids.Clear();
        BattleManager.EntityContainer.Entities.GetIdsInRange(center, radius, s_ids);

        EntityData best = null;
        float bestSqr = float.MaxValue;
        foreach (var id in s_ids)
        {
            if (!BattleManager.EntityContainer.Entities.TryGetObject(id, out var e)) continue;
            if (e == null || !e.Alive || e.id == Owner.id) continue;
            if ((hostile & e.camp) == 0) continue;
            bool isCoral = e.type.category == EntityCategory.Coral;
            if (isCoral != includeCoral) continue;
            float d = (e.transform.position - center).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        s_ids.Clear();
        return best;
    }

    private static readonly HashSet<int> s_ids = new();

    // 防守方：定时扫描己方守护点，取离自己最近的"正在打守护点"的敌人
    protected EntityData FindGuardingEnemy()
    {
        EntityData best = null;
        float bestSqr = float.MaxValue;
        foreach (var statue in BattleManager.EntityContainer.GuardStatues)
        {
            if (statue == null || !statue.Alive || statue.camp != Owner.camp) continue;
            var enemy = NearestEnemyAround(statue.transform.position, Config.ai_guard_alert_radius, false);
            if (enemy == null) continue;
            float d = (enemy.transform.position - Owner.transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = enemy; }
        }
        return best;
    }

    protected float FlatDistance(Vector3 point)
    {
        Vector3 d = point - Owner.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    protected float FlatDistanceTo(EntityData target) => FlatDistance(target.transform.position);

    // 队伍呼叫：同阵营、在半径内的其他 AI 一起追这个敌人
    protected void GroupCall(EntityData target, float radius)
    {
        if (target == null) return;
        float sqr = radius * radius;
        foreach (var e in BattleManager.EntityContainer.Entities)
        {
            if (e == null || e == Owner || !e.Alive || !e.aiControlled) continue;
            if (e.camp != Owner.camp) continue;
            var player = e as PlayerEntityData;
            if (player == null) continue;
            if ((e.transform.position - Owner.transform.position).sqrMagnitude > sqr) continue;
            player.RequestAiChase(target);
        }
    }

    // 中立单位（争夺中立的目标）
    protected EntityData FindNeutralStatue()
    {
        foreach (var e in BattleManager.EntityContainer.Entities)
        {
            if (e == null || !e.Alive) continue;
            if (e.type.category == EntityCategory.NeutralStatue) return e;
        }
        return null;
    }

    protected void ReportStatus()
    {
        string text = $"{Current.Name} {Mathf.RoundToInt(HealthRatio * 100f)}%";
        if (Tool.BattleManager != null) Tool.BattleManager.ReportAiStatus(Owner, text);
    }

    // 攻击距离取技能栏第一个有射程的技能（栏内已按品质降序）
    protected float AttackRange
    {
        get
        {
            var slots = skillBar.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!SkillManager.TryGet(slots[i], out var skill)) continue;
                if (skill.CastRange > 0f) return skill.CastRange;
            }
            return DefaultAttackRange;
        }
    }

    private CombatState ResolveCombat(AiState returnState)
    {
        if (returnState == dangerClearState) return dangerCombatState;
        if (returnState == assaultState) return assaultCombatState;
        return combatState;
    }

    // 静态锚点用快照；动态锚点读发送者当前位置，发送者失效则任务作废
    protected bool TryResolveAnchor(TaskTarget target, out Vector3 point)
    {
        point = default;
        if (target.hasPosition) { point = target.position; return true; }
        if (target.sender == null || !target.sender.Alive) return false;
        point = target.sender.transform.position;
        return true;
    }
    #endregion

    #region//消息处理
    public void OnQuickMessage(SCQuickMessage message)
    {
        if (message == null || !Owner.Alive) return;
        var type = (QuickMessageType)message.messageId;

        if (type == QuickMessageType.ReportStatus) return; // AI 自己发的，不处理
        if (type == QuickMessageType.RequestReport) { ReportStatus(); return; }
        if (type == QuickMessageType.ReleaseSignal) { CancelAll(); return; }
        if (type == QuickMessageType.Danger) { HandleDanger(message); return; }
        if (type == QuickMessageType.Attack && !CanAcceptAttackMessage()) return;

        var target = new TaskTarget
        {
            hasPosition = message.hasTargetPosition,
            position = message.targetPosition,
            sender = ResolveSender(message.senderClientId),
        };

        // 战斗态与危险躲避：只入槽，脱战后执行
        if (Current != null && Current.BlocksTaskMessages) { StorePending(type, target); return; }
        SwitchTo(BuildTaskState(type, target, searchState, false));
    }

    private EntityData ResolveSender(int clientId)
    {
        return Tool.BattleManager != null ? Tool.BattleManager.GetSenderEntity(clientId) : null;
    }

    private void HandleDanger(SCQuickMessage message)
    {
        Vector3 point = message.hasTargetPosition
            ? message.targetPosition
            : (ResolveSender(message.senderClientId) != null ? ResolveSender(message.senderClientId).transform.position : Owner.transform.position);

        bool nearby = FlatDistance(point) <= Config.ai_danger_response_distance;
        if (nearby && HealthRatio > Config.ai_danger_response_ratio)
            SwitchTo(dangerClearState.Bind(point));
        else
            SwitchTo(dangerEvadeState.Bind(point));
    }

    private void CancelAll()
    {
        hasPending = false;
        Owner.StopMoving();
        Owner.ClearAiFacePoint();
        SwitchTo(searchState);
    }

    private void StorePending(QuickMessageType type, TaskTarget target)
    {
        if (type == QuickMessageType.Attack && !CanAcceptAttackMessage()) return;
        if (type == QuickMessageType.ContestNeutral && FindNeutralStatue() == null) return;
        hasPending = true;
        pendingType = type;
        pendingTarget = target;
    }

    // 进攻只在集结系与战斗态生效，其余状态忽略（有目标时会自己打）
    private bool CanAcceptAttackMessage()
    {
        if (Current == searchState || Current == searchCombatState) return false;
        if (Current == chaseState) return false;
        if (Current == escortState || Current == escortApproachState) return false;
        if (Current == assaultState || Current == assaultCombatState) return false;
        if (Current == dangerClearState) return false;
        return true;
    }

    protected AiState ConsumePendingOr(AiState fallback)
    {
        if (!hasPending) return fallback;
        hasPending = false;
        return BuildTaskState(pendingType, pendingTarget, fallback, true);
    }

    // 把任务消息翻译成目标状态；静态锚点在这里落定，动态锚点每帧读 sender 位置
    private AiState BuildTaskState(QuickMessageType type, TaskTarget target, AiState fallback, bool fromPending)
    {
        switch (type)
        {
            case QuickMessageType.RequestEscort:
                if (target.sender == null || !target.sender.Alive) return fallback;
                return escortApproachState.Bind(target.sender);
            case QuickMessageType.Rally:
                if (!target.hasPosition && (target.sender == null || !target.sender.Alive)) return fallback;
                return rallyApproachState.Bind(target);
            case QuickMessageType.Attack:
                if (!target.hasPosition && (target.sender == null || !target.sender.Alive)) return fallback;
                return assaultState.Bind(target);
            case QuickMessageType.ContestNeutral:
            {
                var neutral = FindNeutralStatue();
                if (neutral == null) return fallback; // 执行时再校验一次：中立单位已不存在
                return chaseState.Bind(neutral, searchState, Config.ai_chase_limit);
            }
            default:
                return fallback;
        }
    }
    #endregion
}
