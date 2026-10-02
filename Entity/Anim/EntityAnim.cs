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
            if (OnStateChange != null) OnStateChange.Invoke(currentState, value);
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
            // 过渡期间 GetCurrentAnimatorStateInfo 仍是旧状态；若 next 正是 currentAnimId 新状态，进度须从 next 读以保证 hash 与进度配对一致
            var st = mainAnimator.GetCurrentAnimatorStateInfo(0);
            if (mainAnimator.IsInTransition(0) &&
                mainAnimator.GetNextAnimatorStateInfo(0).fullPathHash == currentAnimId)
            {
                st = mainAnimator.GetNextAnimatorStateInfo(0);
            }
            normalizedTime = st.length > 0f ? Mathf.Repeat(st.normalizedTime, 1f) : 0f;
        }
    }

    public WeaponRef GetDisplayHeldWeapon(WeaponRef serverHeld)
    {
        bool weaponAttack = CurrentState == AnimState.Attack
            && paramPack.attackId is (int)AttackType.Attack_Weapon_R
                or (int)AttackType.Attack_Weapon_L
                or (int)AttackType.Attack_Weapon_R_And_L;
        return weaponAttack && serverHeld.IsValid ? serverHeld : WeaponRef.None;
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

    public float MoveSpeedScale => speed;
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

    public bool AnimSyncDirty { get; private set; }
    public void MarkAnimSyncDirty() => AnimSyncDirty = true;
    public void ClearAnimSyncDirty() => AnimSyncDirty = false;

    public AnimParamPack GetParamPack()
    {
        return paramPack;
    }

    public void ApplyParamPack(in AnimParamPack pack)
    {
        if (animators == null || animators.Count == 0) return;
        paramPack = pack; // 记录持久值（客户端：GetParamPack 也取得到）；不置脏——客户端不做同步
        PushParamsToAnimators();
    }

    private void SetIntParam(ref int field, int value, string key)
    {
        if (field == value) return;
        field = value;
        SetIntAll(key, value);
        MarkAnimSyncDirty();
    }

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
