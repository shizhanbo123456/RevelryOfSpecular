using UnityEngine;

public class TestEntityData : EntityData
{
    private float turnInput; // -1 左 / +1 右

    public bool TestMoving => moveInput.sqrMagnitude > 0.0001f;

    public void SetTestInput(bool forward, float turn)
    {
        turnInput = turn;
        SetMoveInput(forward ? Vector3.forward : Vector3.zero);
    }

    public void SetupTest()
    {
        anim = GetComponentInChildren<EntityAnim>();
        var animData = GetComponent<EntityAnimData>();
        if (animData == null) animData = GetComponentInChildren<EntityAnimData>();

        // SetType 必须在 Init 之后：EntityAnim 的 animators 列表在 Init 里才收集
        if (anim != null) anim.Init(this);
        if (anim != null)
        {
            if (animData != null) anim.SetType(animData.type);
            anim.DoSpawn();
        }

        // 测试场景常无相机对准角色：默认剔除模式会停更状态机、断掉速度声明
        foreach (var animator in GetComponentsInChildren<Animator>(true))
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        SetupTestBody();
        GetComponentInChildren<EntityModelInfo>().BuildCapsuleCollider();
    }

    public override void OnTickMove(float deltaTime, bool canInput)
    {
        float yaw = transform.eulerAngles.y;
        if (canInput && MotionCanMove && Mathf.Abs(turnInput) > 0.01f)
            yaw += Config.move_turn_rate * Mathf.Sign(turnInput) * deltaTime;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    #region//Local
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

    public string DescribeState()
    {
        Vector3 v = rb != null ? rb.velocity : Vector3.zero;
        return $"state={(anim != null ? anim.CurrentState.ToString() : "无anim")} grounded={grounded} " +
               $"moveInput={moveInput} v={v:F2}";
    }
    #endregion
}
