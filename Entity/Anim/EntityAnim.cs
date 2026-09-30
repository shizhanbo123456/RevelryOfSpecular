using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnim : MonoBehaviour
{
    private const string key_characterType = "CharacterType";
    private const string key_spawn = "Spawn";
    private const string key_jump = "Jump";
    private const string key_moving = "Moving";
    private const string key_inAir = "InAir";
    private const string key_slide = "Slide";
    private const string key_doAttack = "Attack";
    private const string key_attackId = "AttackId";
    private const string key_hit = "Hit";
    private const string key_die = "Died";
    public enum CharcterAnimType
    {
        Female=0,
        Male=1,
        Zombie=2
    }
    public enum AnimState
    {
        Spawn,
        Motion,
        Attack,
        Hit,
        Die,
    }
    public enum MotionType
    {
        Idle,
        Run,
        Jump,
        Slide
    }
    public enum AttackType
    {
        Attack_Hand_L=1,
        Attack_Hand_R=2,
        Jump_Hit=11,
        Jump_Mega=12,
        Attack_Weapon_R=21,
        Attack_Weapon_L=22,
        Attack_Weapon_R_And_L=23,
        Mega_Short=31,
        Mega_Middle=32,
        Mega_Long=33,
        Zombie_Hand_Attack_R=41,
        Zombie_Hand_Attack_L=42,
        Zombie_Scream=43,
        Kick=51,
    }
    private AnimState currentState = AnimState.Motion;
    public AnimState CurrentState
    {
        get=>currentState;
        set
        {
            if (currentState == value) return;
            OnStateChange?.Invoke(currentState, value);
            currentState = value;
        }
    }
    private EntityData data;
    public EntityAnimData animData;


    private int currentAnimId = -1;

    #region//事件
    public Action<AttackType> onAttack;//动画中的攻击事件回调
    public Action<AnimState, AnimState> OnStateChange;
    public Action OnDeathEventEnd;
    #endregion

    private List<Animator> animators;
    private Animator mainAnimator;

    /// <summary>主 Animator（人形骨骼查询用，如脚/头/手；Init 后有效，未找到 Animator 时为 null）。</summary>
    public Animator MainAnimator => mainAnimator;


    public void Init(EntityData data, Action<AttackType> onAttack)
    {
        this.data = data;
        this.onAttack = onAttack;

        // 参数包复位：重复 Init（如视图重建）时清掉上一轮残留的持久参数
        paramPack = AnimParamPack.Default;

        animData = GetComponent<EntityAnimData>();
        if (animData == null) Debug.LogError($"{gameObject.name}未挂载animData");

        animators = new();
        if(TryGetComponent<Animator>(out var anim))
        {
            animators.Add(anim);
        }
        for(int i = 0; i < transform.childCount;i++)
        {
            if(transform.GetChild(i).TryGetComponent<Animator>(out anim))
            {
                animators.Add(anim);
            }
        }
        if (animators.Count == 0)
        {
            Debug.LogError($"{gameObject.name} 中未找到任何animator");
            return;
        }
        mainAnimator = animators[0];
        for (int i = 0; i < animators.Count; i++)
        {
            Animator animator = animators[i];
            var behaviours = animator.GetBehaviours<AnimEvent>();
            foreach (var behaviour in behaviours) behaviour.Init(this, data,i==0);
        }
        // 把复位后的参数推给状态机：保证"paramPack 与 Animator 参数一致"这个前提（值比较的脏标记依赖它）
        PushParamsToAnimators();
    }

    /// <summary>把当前参数包整体推给所有 Animator（Init 后调用，不置脏）。</summary>
    private void PushParamsToAnimators()
    {
        foreach (var animator in animators)
        {
            animator.SetInteger(key_characterType, paramPack.characterType);
            animator.SetInteger(key_attackId, paramPack.attackId);
            animator.SetBool(key_inAir, paramPack.inAir);
            animator.SetBool(key_moving, paramPack.moving);
            animator.SetBool(key_slide, paramPack.slide);
        }
    }
    public void SetType(CharcterAnimType type)
    {
        SetIntParam(ref paramPack.characterType, (int)type, key_characterType);
    }

    public void NotifyStateEnter(int animId, AnimState state)
    {
        currentAnimId = animId;
        CurrentState = state;
        MarkAnimSyncDirty(); // 状态变化 = 动画事件的唯一触发源（trigger 不参与同步）
    }

    public void GetDisplayAnim(out int animId, out float normalizedTime)
    {
        animId = currentAnimId;
        normalizedTime = 0f;
        if (mainAnimator != null && mainAnimator.layerCount > 0)
        {
            // 过渡期间 GetCurrentAnimatorStateInfo 仍是旧状态（OnStateEnter 在过渡开始时就把 currentAnimId
            // 更新成了新状态，hash 与进度会错位——进度会变成旧循环片段的任意值）。
            // 此时若 next 就是 currentAnimId 指向的新状态，进度必须从 next 读，保证 hash 与进度配对一致；
            // next 不是它（状态又被打断切换）或已过渡完毕，则照旧读 current。
            var st = mainAnimator.GetCurrentAnimatorStateInfo(0);
            if (mainAnimator.IsInTransition(0) &&
                mainAnimator.GetNextAnimatorStateInfo(0).fullPathHash == currentAnimId)
            {
                st = mainAnimator.GetNextAnimatorStateInfo(0);
            }
            normalizedTime = st.length > 0f ? Mathf.Repeat(st.normalizedTime, 1f) : 0f;
        }
    }

    #region//设置速度
    private float PlaybackSpeed => paused ? 0f : speed;
    public void SetVelocityForward(float speed)
    {
        if (data == null) return;
        data.SetVelocityForward(speed * PlaybackSpeed, VelocitySource.Animation);
    }
    public void SetVelocityHorizontal(Vector2 speed)
    {
        if (data == null) return;
        data.SetVelocityHorizontal(speed * PlaybackSpeed, VelocitySource.Animation);
    }
    public void SetVelocityVertical(float speed)
    {
        if (data == null) return;
        data.SetVelocityVertical(speed, VelocitySource.Animation);
    }
    #endregion
    #region//速度控制
    private float speed=1;
    private bool paused=false;

    /// <summary>当前动画播放倍率（移速类 Buff 的载体；客户端由其镜像 animator.speed）。</summary>
    public float MoveSpeedScale => speed;
    /// <summary>是否被强控暂停（强控期间动画速度置 0，见 EntityData.SetAnimPaused）。</summary>
    public bool Paused => paused;

    public void SetPaused(bool paused)
    {
        if (this.paused == paused) return;
        this.paused = paused;
        UpdateSpeed();
        MarkAnimSyncDirty(); // 播放速度变化也要同步（否则被强控时客户端动画照播）
    }

    public void SetMoveSpeedScale(float scale)
    {
        if (Mathf.Approximately(speed, scale)) return;
        speed = scale;
        UpdateSpeed();
        MarkAnimSyncDirty();
    }
    private void UpdateSpeed()
    {
        float target = PlaybackSpeed; // 与声明速度用的同一份"动画播放速度"，两处不会走散
        foreach (var animator in animators)
            animator.speed = target;
    }
    #endregion

    #region 手持物体（客户端表现：武器/道具模型挂到手部）
    private readonly GameObject[] heldObjects = new GameObject[2];
    public void SetHeldObject(GameObject prefab, bool leftHand = false)
    {
        int slot = leftHand ? 1 : 0;
        if (heldObjects[slot] != null) Destroy(heldObjects[slot]);
        heldObjects[slot] = null;
        if (prefab == null) return;

        Transform mount = GetHandMount(leftHand);
        if (mount == null) return;
        heldObjects[slot] = Instantiate(prefab, mount);
        heldObjects[slot].transform.localPosition = Vector3.zero;
        heldObjects[slot].transform.localRotation = Quaternion.identity;
    }
    public Transform GetHandMount(bool leftHand)
    {
        return mainAnimator != null
            ? mainAnimator.GetBoneTransform(leftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand)
            : null;
    }
    #endregion

    #region//参数打包（持久参数随动画事件同步；trigger 不同步）
    private AnimParamPack paramPack = AnimParamPack.Default; // 持久参数（int/bool），所有设置必须经过这里

    /// <summary>
    /// 动画同步脏标记（服务器）：状态进入、播放倍率或暂停变化时置位，
    /// 由 BattleManager 在同步 pass 中对可见该实体的客户端补发一条动画事件后清除。客户端不使用本标记。
    /// </summary>
    public bool AnimSyncDirty { get; private set; }
    public void MarkAnimSyncDirty() => AnimSyncDirty = true;
    public void ClearAnimSyncDirty() => AnimSyncDirty = false;

    /// <summary>取当前持久参数包（trigger 不参与同步，故只含持久参数）。</summary>
    public AnimParamPack GetParamPack()
    {
        return paramPack;
    }

    /// <summary>
    /// 应用参数包（客户端）：持久参数逐个写入，由客户端 Controller 按条件自行转换；
    /// 状态本身由动画事件 / 姿态包里的状态 hash 对齐（trigger 不再传输）。
    /// </summary>
    public void ApplyParamPack(in AnimParamPack pack)
    {
        if (animators == null || animators.Count == 0) return;
        paramPack = pack; // 记录持久值（客户端：GetParamPack 也取得到）；不置脏——客户端不做同步
        PushParamsToAnimators();
    }

    /// <summary>
    /// 持久参数的**唯一写入入口（int）**：与 paramPack 里的当前值比较，值真的变化才写状态机并置动画脏标记。
    /// 脏标记即"参数变化也要同步"的来源——参数陈旧会让客户端状态机按错的参数自行转换（见《代码架构说明》动画同步节）。
    /// </summary>
    private void SetIntParam(ref int field, int value, string key)
    {
        if (field == value) return;
        field = value;
        SetIntAll(key, value);
        MarkAnimSyncDirty();
    }

    /// <summary>持久参数的**唯一写入入口（bool）**；语义同 <see cref="SetIntParam"/>（InAir 每帧都会被写，靠值比较避免刷屏）。</summary>
    private void SetBoolParam(ref bool field, bool value, string key)
    {
        if (field == value) return;
        field = value;
        SetBoolAll(key, value);
        MarkAnimSyncDirty();
    }

    private void SetIntAll(string key, int value)
    {
        foreach (var animator in animators)
            animator.SetInteger(key, value);
    }

    private void SetBoolAll(string key, bool value)
    {
        foreach (var animator in animators)
            animator.SetBool(key, value);
    }

    /// <summary>推 trigger 给所有 Animator（本地状态机用；trigger 不写任何同步数据）。</summary>
    private void SetTriggerAll(string key)
    {
        foreach (var animator in animators)
            animator.SetTrigger(key);
    }
    #endregion

    #region//动画控制
    public void DoSpawn()
    {
        SetTriggerAll(key_spawn);
    }
    public void DoJump()
    {
        SetTriggerAll(key_jump);
    }
    public void InAir(bool inAir)
    {
        SetBoolParam(ref paramPack.inAir, inAir, key_inAir); // 每帧都会被调用，值未变不置脏
    }
    public void Move(bool moving)
    {
        SetBoolParam(ref paramPack.moving, moving, key_moving);
    }
    public void DoSlide()
    {
        SetBoolParam(ref paramPack.slide, true, key_slide);
    }
    /// <summary>结束滑铲：Slide 置回 false，退出滑铲状态由控制器按该参数判断。</summary>
    public void EndSlide()
    {
        SetBoolParam(ref paramPack.slide, false, key_slide);
    }
    public void DoAttack(AttackType attack)
    {
        SetIntParam(ref paramPack.attackId, (int)attack, key_attackId);
        SetTriggerAll(key_doAttack);
    }
    public void DoHit()
    {
        SetTriggerAll(key_hit);
    }
    public void DoDie()
    {
        SetTriggerAll(key_die);
    }
    #endregion
}
