using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 僵尸与精英僵尸（共用，差异在属性与技能表）。
/// 刷新产出（夜间进度、数量上限、出生点、外观变体、等级）是对局级调度，仍在 BattleManagerWorld。
/// 本类承载其 **AI 行为**：移动（寻路已实现，见 MoveTo/Stop）与索敌、攻击/技能触发时机（待 AI，`BattleManager.UpdateAI` 仍为空）。
/// </summary>
public class ZombieEntityData : EntityData
{
    /// <summary>到点判定距离（米）。</summary>
    private const float ArriveRadius = 0.4f;
    /// <summary>拐点判定距离（米）：离拐点这么近就换下一个。</summary>
    private const float CornerRadius = 0.3f;
    /// <summary>重算路径的间隔（秒）：目标不动时不必每帧跑一次寻路。</summary>
    private const float RepathInterval = 0.25f;
    /// <summary>NavMesh 吸附半径（米）：目标点取自地面物件、出生点也可能贴边，都可能略有偏差。</summary>
    private const float SampleRadius = 1f;

    private readonly NavMeshPath path = new(); // 复用同一个对象，避免每次重算都新建
    private Vector3[] corners;

    private Vector3 destination;
    private bool hasDestination;
    private float nextRepathTime;

    private float yaw;
    private float yawSpeed;
    private bool yawInitialized;

    /// <summary>绕 Y 角速度（度/秒，随表现摘要下发客户端做包间推演）。</summary>
    public override float YawSpeed => yawSpeed;

    /// <summary>
    /// 前往某处。只记目标，"往哪走"留到 OnTickMove 里算 —— 寻路只借 NavMesh 算方向，
    /// 移动仍走"移动输入 + 动画声明速度"这一条链路（见 ResolveMoveVelocity）。可反复调用来改目标。
    /// </summary>
    public void MoveTo(Vector3 dest)
    {
        // 目标点常取自地面物件（守护点/水晶/角色），可能略偏网格 → 先吸附
        if (NavMesh.SamplePosition(dest, out var hit, SampleRadius, NavMesh.AllAreas)) dest = hit.position;

        // 目标没挪窝就不重算路径：AI 逐帧拿最新坐标调 MoveTo 追人时，不至于每帧跑一次寻路
        Vector3 moved = dest - destination;
        moved.y = 0f;
        if (!hasDestination || moved.sqrMagnitude > CornerRadius * CornerRadius) nextRepathTime = 0f;

        destination = dest;
        hasDestination = true;
        if (!yawInitialized)
        {
            yaw = transform.eulerAngles.y; // 初始朝向 = 生成时的朝向
            yawInitialized = true;
        }
    }

    /// <summary>停止：清目标与移动输入，动画回 Idle。不直接写速度 —— 水平速度按地面摩擦自然衰减（见 ResolveMoveVelocity ②）。</summary>
    public void Stop()
    {
        hasDestination = false;
        corners = null;
        yawSpeed = 0f;
        SetMoveInput(Vector3.zero);
        anim?.Move(false);
    }

    /// <summary>逐帧推进：算方向 → 渐转朝向 → 写移动输入（速度由动画声明，本类不产出速度）。</summary>
    public override void OnTickMove(float deltaTime, bool canInput)
    {
        if (!hasDestination) return;

        Vector3 to = destination - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude <= ArriveRadius * ArriveRadius)
        {
            Stop();
            return;
        }

        // 强控（麻痹/冰冻/定身）与位移锁输入期间不推进，但**保留目标**，解除后继续走
        if (!canInput || !MotionCanMove)
        {
            yawSpeed = 0f;
            anim?.Move(false);
            return;
        }

        Vector3 dir = ResolveDirection();
        if (dir.sqrMagnitude < 0.0001f) // 方向退化（理论上不该发生）：停住，不沿用上一帧的输入
        {
            SetMoveInput(Vector3.zero);
            anim?.Move(false);
            return;
        }

        // 服务器权威渐转（客户端按 YawSpeed 推演）
        float prevYaw = yaw;
        yaw = Mathf.MoveTowardsAngle(yaw, Quaternion.LookRotation(dir).eulerAngles.y, Config.move_turn_rate * deltaTime);
        yawSpeed = deltaTime > 0f ? Mathf.DeltaAngle(prevYaw, yaw) / deltaTime : 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 只喂"前进"：朝向已由上面的渐转负责。喂实际方向会在目标位于背后时触发"后退"语义
        // （前后声明的负号是给玩家输入的，见 ResolveMoveVelocity），表现为僵尸倒着走
        SetMoveInput(Vector3.forward);
        anim?.Move(true);
    }

    /// <summary>本帧的水平前进方向：沿 NavMesh 拐点走；算不出路径（未烘焙/脱离网格）时直线朝目标兜底，等它自己走回网格。</summary>
    private Vector3 ResolveDirection()
    {
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + RepathInterval;
            bool ok = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            corners = ok && path.corners.Length > 1 ? path.corners : null;
        }

        Vector3 target = destination;
        if (corners != null)
        {
            int i = 0; // 起点与走过的拐点都靠这个距离跳过
            while (i < corners.Length && FlatSqrDistance(corners[i]) <= CornerRadius * CornerRadius) i++;
            if (i < corners.Length) target = corners[i];
        }

        Vector3 dir = target - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    /// <summary>水平距离平方（寻路只关心水平面）。</summary>
    private float FlatSqrDistance(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.sqrMagnitude;
    }
}
