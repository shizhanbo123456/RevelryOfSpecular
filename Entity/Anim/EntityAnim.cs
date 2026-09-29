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
    private const string key_slideEnd = "SlideEnd";
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


    public void Init(EntityData data, Action<AttackType> onAttack)
    {
        this.data = data;
        this.onAttack = onAttack;

        // 参数包复位：重复 Init（如视图重建）时清掉上一轮残留的持久参数与触发戳
        paramPack = AnimParamPack.Default;
        System.Array.Clear(triggerSetFrames, 0, triggerSetFrames.Length);

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
    }
    public void SetType(CharcterAnimType type)
    {
        paramPack.characterType = (int)type;
        SetIntAll(key_characterType, (int)type);
    }

    public void NotifyStateEnter(int animId, AnimState state)
    {
        currentAnimId = animId;
        CurrentState = state;
    }

    public void GetDisplayAnim(out int animId, out float normalizedTime)
    {
        animId = currentAnimId;
        normalizedTime = 0f;
        if (mainAnimator != null && mainAnimator.layerCount > 0)
        {
            var st = mainAnimator.GetCurrentAnimatorStateInfo(0);
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
    public void SetPaused(bool paused)
    {
        this.paused = paused;
        UpdateSpeed();
    }

    public void SetMoveSpeedScale(float scale)
    {
        speed = scale;
        UpdateSpeed();
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

    #region//参数打包（方案 B：参数随表现摘要同步）
    private AnimParamPack paramPack = AnimParamPack.Default; // 持久参数（int/bool），所有设置必须经过这里
    private enum TrigIndex { Spawn = 0, Jump, SlideEnd, Attack, Hit, Die, Count }
    private static readonly string[] triggerKeys = { key_spawn, key_jump, key_slideEnd, key_doAttack, key_hit, key_die };
    private readonly int[] triggerSetFrames = new int[(int)TrigIndex.Count]; // 各 trigger 最后被设置的帧，用于生成"本帧触发"标志

    /// <summary>
    /// 取当前参数包：持久参数 + 本帧内被设置的 trigger。
    /// trigger 按 Time.frameCount 打戳，无需手动清除——下一帧自动失效；
    /// 客户端若错过该帧包，由后续同步的状态 hash（animId）兜底对齐。
    /// </summary>
    public AnimParamPack GetParamPack()
    {
        var pack = paramPack;
        int frame = Time.frameCount;
        pack.trigSpawn = triggerSetFrames[(int)TrigIndex.Spawn] == frame;
        pack.trigJump = triggerSetFrames[(int)TrigIndex.Jump] == frame;
        pack.trigSlideEnd = triggerSetFrames[(int)TrigIndex.SlideEnd] == frame;
        pack.trigAttack = triggerSetFrames[(int)TrigIndex.Attack] == frame;
        pack.trigHit = triggerSetFrames[(int)TrigIndex.Hit] == frame;
        pack.trigDie = triggerSetFrames[(int)TrigIndex.Die] == frame;
        return pack;
    }

    /// <summary>
    /// 应用参数包（客户端）：持久参数逐个写入，trigger 原样 SetTrigger，
    /// 由客户端 Controller 按条件自动转换到下一状态；之后的 Play(hash) 仅作服务器权威对齐。
    /// </summary>
    public void ApplyParamPack(in AnimParamPack pack)
    {
        if (animators == null || animators.Count == 0) return;
        // 记录持久值，保证 GetParamPack 在客户端也能取到当前参数
        paramPack = new AnimParamPack()
        {
            characterType = pack.characterType,
            attackId = pack.attackId,
            inAir = pack.inAir,
            moving = pack.moving,
            slide = pack.slide,
        };
        foreach (var animator in animators)
        {
            animator.SetInteger(key_characterType, pack.characterType);
            animator.SetInteger(key_attackId, pack.attackId);
            animator.SetBool(key_inAir, pack.inAir);
            animator.SetBool(key_moving, pack.moving);
            animator.SetBool(key_slide, pack.slide);
            if (pack.trigSpawn) animator.SetTrigger(key_spawn);
            if (pack.trigJump) animator.SetTrigger(key_jump);
            if (pack.trigSlideEnd) animator.SetTrigger(key_slideEnd);
            if (pack.trigAttack) animator.SetTrigger(key_doAttack);
            if (pack.trigHit) animator.SetTrigger(key_hit);
            if (pack.trigDie) animator.SetTrigger(key_die);
        }
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

    /// <summary>设置 trigger：写本帧触发戳（供 GetParamPack 采集）+ 推给所有 Animator。</summary>
    private void FireTriggerAll(TrigIndex index)
    {
        triggerSetFrames[(int)index] = Time.frameCount;
        foreach (var animator in animators)
            animator.SetTrigger(triggerKeys[(int)index]);
    }
    #endregion

    #region//动画控制
    public void DoSpawn()
    {
        FireTriggerAll(TrigIndex.Spawn);
    }
    public void DoJump()
    {
        FireTriggerAll(TrigIndex.Jump);
    }
    public void InAir(bool inAir)
    {
        paramPack.inAir = inAir;
        SetBoolAll(key_inAir, inAir);
    }
    public void Move(bool moving)
    {
        paramPack.moving = moving;
        SetBoolAll(key_moving, moving);
    }
    public void DoSlide()
    {
        paramPack.slide = true;
        SetBoolAll(key_slide, true);
    }
    public void EndSlide()
    {
        paramPack.slide = false;
        SetBoolAll(key_slide, false);
        FireTriggerAll(TrigIndex.SlideEnd);
    }
    public void DoAttack(AttackType attack)
    {
        paramPack.attackId = (int)attack;
        SetIntAll(key_attackId, (int)attack);
        FireTriggerAll(TrigIndex.Attack);
    }
    public void DoHit()
    {
        FireTriggerAll(TrigIndex.Hit);
    }
    public void DoDie()
    {
        FireTriggerAll(TrigIndex.Die);
    }
    #endregion
}
