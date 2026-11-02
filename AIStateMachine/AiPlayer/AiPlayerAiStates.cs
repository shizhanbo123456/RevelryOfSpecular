using Ros.Transport;
using UnityEngine;

// 人机玩家的各决策状态实现（状态图与消息矩阵见 Doc/08-游戏AI逻辑），只写期望行为、不碰执行层
public partial class AiPlayerAi
{
    private const float DefaultAttackRange = 2f; // 栏内没有带射程的技能时按空手射程算，不能用大值否则会停在打不到的距离

    private sealed class SearchState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.Search;
        private readonly AiPlayerAi ai;
        private bool hasPoint;
        private Vector3 point;
        private float resumeTime;
        private float nextGuardCheck;

        public SearchState(AiPlayerAi ai) { this.ai = ai; }

        public override void Enter()
        {
            hasPoint = false;
            resumeTime = 0f;
        }

        public override AiDecisionState Decide()
        {
            // 近身敌人优先于守护点告警，避免被远处警报引走、自己在原地挨打
            var enemy = ai.NearestEnemy(false);
            if (enemy != null) return ai.chaseState.Bind(enemy, this, Config.ai_chase_limit);

            if (ai.Owner.camp == EntityCamp.Defense && Time.time >= nextGuardCheck)
            {
                nextGuardCheck = Time.time + Config.ai_guard_check_interval;
                var intruder = ai.FindGuardingEnemy();
                if (intruder != null) return ai.chaseState.Bind(intruder, this, Config.ai_chase_limit);
            }

            // 珊瑚不还手，顺手打（打断规则最宽松的就是搜索战斗）
            var coral = ai.NearestEnemy(true);
            if (coral != null) return ai.searchCombatState.Bind(coral);

            if (hasPoint)
            {
                // 用执行层吸附后的目的地判距，避免「点被 NavMesh 吸附到脚下、状态机仍认为没到」而原地卡死
                if (ai.Owner.DistanceToNavDestination > Config.ai_search_arrive) { ai.RequestMove(point); return null; }
                hasPoint = false;
                resumeTime = Time.time + UnityEngine.Random.Range(Config.ai_search_pause_min, Config.ai_search_pause_max);
                ai.RequestIdle();
                return null;
            }

            if (Time.time < resumeTime) return null; // 停顿中：保持 Idle

            Vector2 offset = UnityEngine.Random.insideUnitCircle * Config.ai_search_radius;
            point = ai.Owner.transform.position + new Vector3(offset.x, 0f, offset.y);
            hasPoint = true;
            ai.RequestMove(point);
            return null;
        }

        public override AiDecisionState OnAllyChaseRequest(EntityData target) => ai.chaseState.Bind(target, this, Config.ai_chase_limit);

        // 走不到就丢掉这个点，下一次决策重选
        public override void OnStuck()
        {
            hasPoint = false;
            resumeTime = 0f;
        }
    }

    private sealed class SearchCombatState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.SearchCombat;
        private readonly AiPlayerAi ai;
        private EntityData coral;

        public SearchCombatState(AiPlayerAi ai) { this.ai = ai; }

        public SearchCombatState Bind(EntityData target) { coral = target; return this; }

        public override AiDecisionState Decide()
        {
            // 唯一的"可立即打断"战斗态：真正的敌人一来就让位
            var enemy = ai.NearestEnemy(false);
            if (enemy != null) return ai.chaseState.Bind(enemy, ai.searchState, Config.ai_chase_limit);

            if (coral == null || !coral.Alive) return ai.searchState;

            if (ai.FlatDistanceTo(coral) > ai.AttackRange) { ai.RequestMove(coral.transform.position); return null; }
            ai.RequestAttack(coral);
            return null;
        }

        public override AiDecisionState OnAllyChaseRequest(EntityData target) => ai.chaseState.Bind(target, ai.searchState, Config.ai_chase_limit);
    }

    private sealed class ChaseState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.Chase;
        private readonly AiPlayerAi ai;
        private EntityData target;
        private AiDecisionState returnState;
        private float limit;

        public ChaseState(AiPlayerAi ai) { this.ai = ai; }

        private bool abandoned;

        // 目标走不到：放弃并暂时忽略它，回到来时的状态
        public override void OnStuck()
        {
            ai.IgnoreTarget(target);
            abandoned = true;
        }

        public ChaseState Bind(EntityData t, AiDecisionState ret, float limitRange)
        {
            target = t;
            returnState = ret;
            limit = limitRange;
            abandoned = false;
            return this;
        }

        public override AiDecisionState Decide()
        {
            if (abandoned) { abandoned = false; return returnState; }
            if (target == null || !target.Alive) return returnState;
            if (ai.FlatDistanceTo(target) > limit) return returnState;
            if (ai.FlatDistanceTo(target) <= ai.AttackRange) return ai.ResolveCombat(returnState).Bind(target, returnState);

            ai.RequestMove(target.transform.position);
            return null;
        }
    }

    // 战斗决策：四种战斗的差异全部由构造参数决定（撤离血线 / 活动范围 / 撤离去向）
    private sealed class CombatDecision : AiDecisionState
    {
        private readonly AiPlayerAi ai;
        private float retreatRatio;
        private float activityRadius;
        private AiDecisionState retreatState;
        private EntityReportState reportState; // 同一个战斗类被 5 处复用，上报类型只能按实例区分

        public override EntityReportState ReportState => reportState;

        private EntityData target;
        private AiDecisionState returnState;
        private bool abandoned;

        // 追不上（目标在走不到的位置）：放弃并暂时忽略
        public override void OnStuck()
        {
            ai.IgnoreTarget(target);
            abandoned = true;
        }

        public CombatDecision(AiPlayerAi ai) { this.ai = ai; }

        public void Setup(float retreatRatio, float activityRadius, AiDecisionState retreatState, EntityReportState reportState)
        {
            this.retreatRatio = retreatRatio;
            this.activityRadius = activityRadius;
            this.retreatState = retreatState;
            this.reportState = reportState;
        }

        public CombatDecision Bind(EntityData t, AiDecisionState ret)
        {
            target = t;
            returnState = ret;
            abandoned = false;
            return this;
        }

        public override bool BlocksTaskMessages => true;

        public override AiDecisionState Decide()
        {
            if (abandoned) { abandoned = false; return ai.ConsumePendingOr(returnState); }
            if (target == null || !target.Alive) return ai.ConsumePendingOr(returnState);

            if (ai.HealthRatio < retreatRatio) return retreatState;

            float dist = ai.FlatDistanceTo(target);
            if (activityRadius > 0f && dist > activityRadius) return ai.ConsumePendingOr(returnState);

            // 追与停用不同阈值：接敌距离停下、退出距离才重新前压，避免在边界反复走停
            if (dist > ai.AttackRange * Config.ai_combat_exit_scale) { ai.RequestMove(target.transform.position); return null; }

            ai.RequestAttack(target);
            return null;
        }
    }

    private sealed class EscortApproachState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.EscortApproach;
        private readonly AiPlayerAi ai;
        private EntityData escorted;
        private float enterTime;

        public EscortApproachState(AiPlayerAi ai) { this.ai = ai; }

        public EscortApproachState Bind(EntityData target) { escorted = target; return this; }

        public override void Enter() { enterTime = Time.time; }

        public override AiDecisionState Decide()
        {
            if (escorted == null || !escorted.Alive) return ai.searchState;
            if (Time.time - enterTime > Config.ai_escort_timeout) return ai.searchState;
            if (ai.FlatDistanceTo(escorted) <= Config.ai_escort_arrive) return ai.escortState.Bind(escorted);

            ai.RequestMove(escorted.transform.position);
            return null;
        }
    }

    private sealed class EscortState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.Escort;
        private readonly AiPlayerAi ai;
        private EntityData escorted;
        private Vector3 slotOffset;

        public EscortState(AiPlayerAi ai) { this.ai = ai; }

        public EscortState Bind(EntityData target) { escorted = target; return this; }

        // 进入时随机定一个相对站位，之后始终去"目标位置 + 偏移"，多个人机不会挤在一起
        public override void Enter()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * Config.ai_escort_slot_radius;
            slotOffset = new Vector3(offset.x, 0f, offset.y);
        }

        public override AiDecisionState Decide()
        {
            if (escorted == null || !escorted.Alive) return ai.searchState;

            var enemy = ai.NearestEnemyAround(escorted.transform.position, Config.ai_escort_combat_radius, false);
            if (enemy != null) return ai.escortCombatState.Bind(enemy, this);

            Vector3 slot = escorted.transform.position + slotOffset;
            float dist = ai.FlatDistance(slot);
            // 追上与停下用不同阈值：锚点随护送对象移动，单阈值会在边界反复走停
            if (dist > Config.ai_escort_follow) { ai.RequestMove(slot); return null; }
            if (dist <= Config.ai_escort_follow * Config.ai_hysteresis_ratio) ai.RequestIdle(escorted);

            return null; // 滞回带：保持当前行为
        }
    }

    private sealed class DangerEvadeState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.DangerEvade;
        private readonly AiPlayerAi ai;
        private Vector3 threat;
        private float enterTime;
        private Vector3 escapeTo;

        public DangerEvadeState(AiPlayerAi ai) { this.ai = ai; }

        public DangerEvadeState Bind(Vector3 point) { threat = point; return this; }

        public override bool BlocksTaskMessages => true;

        public override void Enter()
        {
            enterTime = Time.time;
            Vector3 away = ai.Owner.transform.position - threat;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -ai.Owner.transform.forward;
            escapeTo = ai.Owner.transform.position + away.normalized * Config.ai_evade_distance;
        }

        public override AiDecisionState Decide()
        {
            if (Time.time - enterTime >= Config.ai_evade_duration) return ai.ConsumePendingOr(ai.searchState);
            if (ai.FlatDistance(threat) >= Config.ai_evade_distance) return ai.ConsumePendingOr(ai.searchState);

            ai.RequestMove(escapeTo);
            return null;
        }
    }

    private sealed class DangerClearState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.DangerClear;
        private readonly AiPlayerAi ai;
        private Vector3 point;
        private float enterTime;

        public DangerClearState(AiPlayerAi ai) { this.ai = ai; }

        public DangerClearState Bind(Vector3 value) { point = value; return this; }

        public override void Enter() { enterTime = Time.time; }

        public override AiDecisionState Decide()
        {
            if (Time.time - enterTime > Config.ai_danger_clear_timeout) return ai.searchState;

            var enemy = ai.NearestEnemyAround(point, Config.ai_danger_clear_radius, false);
            if (enemy != null) return ai.chaseState.Bind(enemy, this, Config.ai_chase_limit);

            if (ai.FlatDistance(point) > Config.ai_danger_clear_arrive) ai.RequestMove(point);
            else ai.RequestIdle();
            return null;
        }
    }

    private sealed class RallyApproachState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.RallyApproach;
        private readonly AiPlayerAi ai;
        private TaskTarget anchor;

        public RallyApproachState(AiPlayerAi ai) { this.ai = ai; }

        public RallyApproachState Bind(TaskTarget target) { anchor = target; return this; }

        public override AiDecisionState Decide()
        {
            if (!ai.TryResolveAnchor(anchor, out var point)) return ai.searchState;
            if (ai.FlatDistance(point) <= Config.ai_rally_arrive) return ai.rallyWaitState.Bind(anchor);

            ai.RequestMove(point);
            return null;
        }
    }

    private sealed class RallyWaitState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.RallyWait;
        private readonly AiPlayerAi ai;
        private TaskTarget anchor;
        private float nextCheck;

        public RallyWaitState(AiPlayerAi ai) { this.ai = ai; }

        public RallyWaitState Bind(TaskTarget target) { anchor = target; return this; }

        public override AiDecisionState Decide()
        {
            if (!ai.TryResolveAnchor(anchor, out var point)) return ai.searchState;

            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + Config.ai_guard_check_interval;
                var enemy = ai.NearestEnemyAround(point, Config.ai_rally_keep, false);
                if (enemy != null)
                {
                    ai.GroupCall(enemy, Config.ai_rally_call_radius); // 叫上附近人机一起守
                    return ai.rallyGuardState.Bind(enemy, this);
                }
            }

            float dist = ai.FlatDistance(point);
            // 靠近与停下用不同阈值：锚点可能跟着发送者移动，单阈值会在边界反复走停
            if (dist > Config.ai_rally_keep) { ai.RequestMove(point); return null; }
            if (dist <= Config.ai_rally_keep * Config.ai_hysteresis_ratio) ai.RequestIdle();

            return null; // 滞回带：保持当前行为
        }
    }

    private sealed class AssaultState : AiDecisionState
    {
        public override EntityReportState ReportState => EntityReportState.Assault;
        private readonly AiPlayerAi ai;
        private TaskTarget anchor;

        public AssaultState(AiPlayerAi ai) { this.ai = ai; }

        public AssaultState Bind(TaskTarget target) { anchor = target; return this; }

        public override AiDecisionState Decide()
        {
            if (!ai.TryResolveAnchor(anchor, out var point)) return ai.searchState;

            var enemy = ai.NearestEnemy(false);
            if (enemy != null) return ai.chaseState.Bind(enemy, this, Config.ai_assault_combat_activity);

            if (!anchor.hasPosition) { ai.RequestMove(point); return null; } // 动态锚点：一路跟着发送者

            if (ai.FlatDistance(point) > Config.ai_assault_arrive) { ai.RequestMove(point); return null; }
            return ai.searchState; // 静态锚点到达且无敌：继续推
        }
    }
}
