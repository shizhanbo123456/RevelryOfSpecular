using UnityEngine;

/// <summary>
/// 动画测试实体：继承 EntityData 复用「输入 → 动画声明速度 → TickVelocity 结算」链路，
/// 不走基类 OnCreate——无属性/效果/技能，不进 BattleManager 容器。由 AnimationTest 驱动。
/// 机制与注意事项见《代码架构说明》Test 文件夹节。
/// </summary>
public class TestEntityData : EntityData
{
    private float turnInput; // -1 左 / +1 右

    /// <summary>是否在移动（驱动器读它切换 Run/Idle）。</summary>
    public bool TestMoving => moveInput.sqrMagnitude > 0.0001f;

    /// <summary>写本地输入（坦克式，与 PlayerEntityData 一致：W 前进、A/D 转向、S 弃用）。</summary>
    public void SetTestInput(bool forward, float turn)
    {
        turnInput = turn;
        SetMoveInput(forward ? Vector3.forward : Vector3.zero);
    }

    /// <summary>装配：对齐基类 OnCreate 的动画/刚体/碰撞部分，跳过属性（GetAttribute）/效果/技能。</summary>
    public void SetupTest()
    {
        anim = GetComponentInChildren<EntityAnim>();
        var animData = GetComponent<EntityAnimData>();
        if (animData == null) animData = GetComponentInChildren<EntityAnimData>();

        // SetType 必须在 Init 之后：EntityAnim 的 animators 列表在 Init 里才收集
        if (anim != null) anim.Init(this, OnAnimAttack);
        if (anim != null)
        {
            if (animData != null) anim.SetType(animData.type);
            anim.DoSpawn();
        }

        // 测试场景常无相机对准角色：默认剔除模式会停更状态机、断掉速度声明
        foreach (var animator in GetComponentsInChildren<Animator>(true))
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        SetupTestBody();
        var modelInfo = GetComponentInChildren<EntityModelInfo>();
        if (modelInfo != null) modelInfo.BuildCapsuleCollider();
    }

    /// <summary>朝向推进（对齐 PlayerEntityData 真人分支：A/D 按住即转向，yaw 正 = 右转）。</summary>
    public override void OnTickMove(float deltaTime, bool canInput)
    {
        float yaw = transform.eulerAngles.y;
        if (canInput && MotionCanMove && Mathf.Abs(turnInput) > 0.01f)
            yaw += Config.move_turn_rate * Mathf.Sign(turnInput) * deltaTime;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    #region//Local
    /// <summary>刚体参数对齐基类 SetupBody（private 故独立实现）；rb 必须写基类字段，否则速度结算入口判空即丢。</summary>
    private void SetupTestBody()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.drag = Config.rb_drag;
        rb.angularDrag = Config.rb_angular_drag;
    }

    /// <summary>诊断视图：速度链路内部状态，供 AnimationTest 的 HUD 显示。</summary>
    public string DescribeState()
    {
        Vector3 v = rb != null ? rb.velocity : Vector3.zero;
        return $"state={(anim != null ? anim.CurrentState.ToString() : "无anim")} grounded={grounded} " +
               $"moveInput={moveInput} v={v:F2} src={LastVelocitySource}";
    }
    #endregion
}
