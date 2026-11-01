using UnityEngine;

// 人机玩家的各状态实现（状态图与消息矩阵见 Doc/08-游戏AI逻辑）
public partial class AiPlayerAi
{
    private const float DefaultAttackRange = 2f; // 栏内没有带射程的技能时按空手射程算，不能用大值否则会停在打不到的距离

    private sealed class SearchState : AiState
    {
        private readonly AiPlayerAi ai;
        private bool hasPoint;
        private Vector3 point;
        private float resumeTime;
        private float nextGuardCheck;

        public SearchState(AiPlayerAi ai) { this.ai = ai; }

        public override void Enter()
        {
            ai.Owner.ClearAiFacePoint();
            hasPoint = false;
            resumeTime = 0f;
        }

        public override AiState Tick(float deltaTime)
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
                if (ai.Owner.DistanceToNavDestination > Config.ai_search_arrive) { ai.MoveTowards(point); return null; }
                hasPoint = false;
                ai.Owner.StopMoving();
                resumeTime = Time.time + UnityEngine.Random.Range(Config.ai_search_pause_min, Config.ai_search_pause_max);
                return null;
            }

            if (Time.time < resumeTime) return null;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * Config.ai_search_radius;
            point = ai.Owner.transform.position + new Vector3(offset.x, 0f, offset.y);
            hasPoint = true;
            ai.MoveTowards(point);
            return null;
        }

        public override AiState OnAllyChaseRequest(EntityData target) => ai.chaseState.Bind(target, this, Config.ai_chase_limit);

        // 走不到就丢掉这个点，下一次决策重选
        public override void OnStuck()
        {
            hasPoint = false;
            resumeTime = 0f;
        }
    }

    private sealed class SearchCombatState : AiState
    {
        private readonly AiPlayerAi ai;
        private EntityData coral;

        public SearchCombatState(AiPlayerAi ai) { this.ai = ai; }

        public SearchCombatState Bind(EntityData target) { coral = target; return this; }

        public override void Enter() { }

        public override AiState Tick(float deltaTime)
        {
            // 唯一的"可立即打断"战斗态：真正的敌人一来就让位
            var enemy = ai.NearestEnemy(false);
            if (enemy != null) return ai.chaseState.Bind(enemy, ai.searchState, Config.ai_chase_limit);

            if (coral == null || !coral.Alive) return ai.searchState;

            ai.FaceTarget(coral);
            if (ai.FlatDistanceTo(coral) > ai.AttackRange)
            {
                ai.MoveTowards(coral.transform.position);
                return null;
            }
            ai.Owner.StopMoving();
            ai.TryAttack(coral);
            return null;
        }

        public override AiState OnAllyChaseRequest(EntityData target) => ai.chaseState.Bind(target, ai.searchState, Config.ai_chase_limit);
    }

    private sealed class ChaseState : AiState
    {
        private readonly AiPlayerAi ai;
        private EntityData target;
        private AiState returnState;
        private float limit;

        public ChaseState(AiPlayerAi ai) { this.ai = ai; }

        public ChaseState Bind(EntityData t, AiState ret, float limitRange)
        {
            target = t;
            returnState = ret;
            limit = limitRange;
            return this;
        }

        public override void Enter() { ai.Owner.ClearAiFacePoint(); }

        public override AiState Tick(float deltaTime)
        {
            if (target == null || !target.Alive) return returnState;
            if (ai.FlatDistanceTo(target) > limit) return returnState;
            if (ai.FlatDistanceTo(target) <= ai.AttackRange) return ai.ResolveCombat(returnState).Bind(target, returnState);

            ai.FaceTarget(target);
            ai.MoveTowards(target.transform.position);
            return null;
        }
    }

    // 战斗态：四种战斗的差异全部由构造参数决定（撤离血线 / 活动范围 / 脱战与撤离去向）
    private sealed class CombatState : AiState
    {
        private PlayerEntityData owner;
        private AiPlayerAi ai;
        private float retreatRatio;
        private float activityRadius;
        private AiState retreatState;

        private EntityData target;
        private AiState returnState;

        public void Setup(PlayerEntityData owner, AiPlayerAi ai, float retreatRatio, float activityRadius, AiState retreatState)
        {
            this.owner = owner;
            this.ai = ai;
            this.retreatRatio = retreatRatio;
            this.activityRadius = activityRadius;
            this.retreatState = retreatState;
        }

        public CombatState Bind(EntityData t, AiState ret)
        {
            target = t;
            returnState = ret;
            return this;
        }

        public override bool BlocksTaskMessages => true;

        public override void Enter() { owner.StopMoving(); }

        public override AiState Tick(float deltaTime)
        {
            if (target == null || !target.Alive) return ai.ConsumePendingOr(returnState);

            if (ai.HealthRatio < retreatRatio) return retreatState;

            float dist = ai.FlatDistanceTo(target);
            if (activityRadius > 0f && dist > activityRadius) return ai.ConsumePendingOr(returnState);

            ai.FaceTarget(target);
            // 追与停用不同阈值：接敌距离停下、退出距离才重新前压，避免在边界反复走停
            if (dist > ai.AttackRange * Config.ai_combat_exit_scale)
            {
                ai.MoveTowards(target.transform.position);
                return null;
            }

            owner.StopMoving();
            ai.TryAttack(target); // 全没好就站着，不空放
            return null;
        }
    }

    private sealed class EscortApproachState : AiState
    {
        private readonly AiPlayerAi ai;
        private EntityData escorted;
        private float enterTime;

        public EscortApproachState(AiPlayerAi ai) { this.ai = ai; }

        public EscortApproachState Bind(EntityData target) { escorted = target; return this; }

        public override void Enter()
        {
            enterTime = Time.time;
            ai.Owner.ClearAiFacePoint();
        }

        public override AiState Tick(float deltaTime)
        {
            if (escorted == null || !escorted.Alive) return ai.searchState;
            if (Time.time - enterTime > Config.ai_escort_timeout) return ai.searchState;
            if (ai.FlatDistanceTo(escorted) <= Config.ai_escort_arrive) return ai.escortState.Bind(escorted);

            ai.FaceTarget(escorted);
            ai.MoveTowards(escorted.transform.position);
            return null;
        }
    }

    private sealed class EscortState : AiState
    {
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
            ai.Owner.ClearAiFacePoint();
        }

        public override AiState Tick(float deltaTime)
        {
            if (escorted == null || !escorted.Alive) return ai.searchState;

            var enemy = ai.NearestEnemyAround(escorted.transform.position, Config.ai_escort_combat_radius, false);
            if (enemy != null) return ai.escortCombatState.Bind(enemy, this);

            Vector3 slot = escorted.transform.position + slotOffset;
            float dist = ai.FlatDistance(slot);
            // 追上与停下用不同阈值：锚点随护送对象移动，单阈值会在边界反复走停
            if (dist > Config.ai_escort_follow)
            {
                ai.MoveTowards(slot);
                return null;
            }
            if (dist <= Config.ai_escort_follow * Config.ai_hysteresis_ratio) ai.Owner.StopMoving();

            ai.FaceTarget(escorted);
            return null;
        }
    }

    private sealed class DangerEvadeState : AiState
    {
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

        public override AiState Tick(float deltaTime)
        {
            if (Time.time - enterTime >= Config.ai_evade_duration) return ai.ConsumePendingOr(ai.searchState);
            if (ai.FlatDistance(threat) >= Config.ai_evade_distance) return ai.ConsumePendingOr(ai.searchState);

            ai.MoveTowards(escapeTo);
            return null;
        }
    }

    private sealed class DangerClearState : AiState
    {
        private readonly AiPlayerAi ai;
        private Vector3 point;
        private float enterTime;

        public DangerClearState(AiPlayerAi ai) { this.ai = ai; }

        public DangerClearState Bind(Vector3 value) { point = value; return this; }

        public override void Enter()
        {
            enterTime = Time.time;
            ai.Owner.ClearAiFacePoint();
        }

        public override AiState Tick(float deltaTime)
        {
            if (Time.time - enterTime > Config.ai_danger_clear_timeout) return ai.searchState;

            var enemy = ai.NearestEnemyAround(point, Config.ai_danger_clear_radius, false);
            if (enemy != null) return ai.chaseState.Bind(enemy, this, Config.ai_chase_limit);

            if (ai.FlatDistance(point) > Config.ai_danger_clear_arrive) ai.MoveTowards(point);
            else ai.Owner.StopMoving();
            return null;
        }
    }

    private sealed class RallyApproachState : AiState
    {
        private readonly AiPlayerAi ai;
        private TaskTarget anchor;

        public RallyApproachState(AiPlayerAi ai) { this.ai = ai; }

        public RallyApproachState Bind(TaskTarget target) { anchor = target; return this; }

        public override void Enter() { ai.Owner.ClearAiFacePoint(); }

        public override AiState Tick(float deltaTime)
        {
            if (!ai.TryResolveAnchor(anchor, out var point)) return ai.searchState;
            if (ai.FlatDistance(point) <= Config.ai_rally_arrive) return ai.rallyWaitState.Bind(anchor);

            ai.MoveTowards(point);
            return null;
        }
    }

    private sealed class RallyWaitState : AiState
    {
        private readonly AiPlayerAi ai;
        private TaskTarget anchor;
        private float nextCheck;

        public RallyWaitState(AiPlayerAi ai) { this.ai = ai; }

        public RallyWaitState Bind(TaskTarget target) { anchor = target; return this; }

        public override void Enter() { ai.Owner.StopMoving(); }

        public override AiState Tick(float deltaTime)
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
            if (dist > Config.ai_rally_keep) ai.MoveTowards(point);
            else if (dist <= Config.ai_rally_keep * Config.ai_hysteresis_ratio) ai.Owner.StopMoving();
            return null;
        }
    }

    private sealed class AssaultState : AiState
    {
        private readonly AiPlayerAi ai;
        private TaskTarget anchor;

        public AssaultState(AiPlayerAi ai) { this.ai = ai; }

        public AssaultState Bind(TaskTarget target) { anchor = target; return this; }

        public override void Enter() { ai.Owner.ClearAiFacePoint(); }

        public override AiState Tick(float deltaTime)
        {
            if (!ai.TryResolveAnchor(anchor, out var point)) return ai.searchState;

            var enemy = ai.NearestEnemy(false);
            if (enemy != null) return ai.chaseState.Bind(enemy, this, Config.ai_assault_combat_activity);

            if (!anchor.hasPosition) { ai.MoveTowards(point); return null; } // 动态锚点：一路跟着发送者

            if (ai.FlatDistance(point) > Config.ai_assault_arrive) { ai.MoveTowards(point); return null; }
            return ai.searchState; // 静态锚点到达且无敌：继续推
        }
    }
}
