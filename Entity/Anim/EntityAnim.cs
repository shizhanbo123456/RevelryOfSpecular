using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnim : MonoBehaviour
{
    // 状态机参数名/类型必须与 Assets/Files/Anim/CharacterAnim.controller 的参数表逐字一致。
    // 参数名或类型写错时 Unity 只打警告、不报错，动画会静默不播 —— 改动这里务必对资产核对。
    private const string key_characterType = "CharacterType"; // Int：0 女 / 1 男 / 2 僵尸
    private const string key_spawn = "Spawn";                 // Trigger：AnyState → Spawn 子状态机
    private const string key_moving = "Moving";               // Bool
    private const string key_inAir = "InAir";                 // Bool
    private const string key_slide = "Slide";                 // Bool
    private const string key_slideEnd = "SlideEnd";           // Trigger
    private const string key_roll = "Roll";                   // Trigger
    private const string key_attack = "Attack";               // Trigger：AnyState → Attack 子状态机
    private const string key_attackId = "AttackId";           // Int：Attack 子状态机内按它选具体招式
    private const string key_hit = "Hit";                     // Trigger：AnyState → Dam_Stand_Stumble
    private const string key_die = "Died";                    // Trigger：AnyState → Death 子状态机
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
        Slide,
        Roll
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

    private int currentAnimId = -1;

    #region//事件
    public Action<AttackType> onAttack;//动画中的攻击事件回调
    public Action<AnimState, AnimState> OnStateChange;
    public Action OnDeathEventEnd;
    #endregion

    /// <summary>该实体身上的全部动画状态机（自身 + 一级子物体）。[0] 为主状态机，其余是同一套动画的附加模型。</summary>
    private readonly List<Animator> animators = new();
    private Animator mainAnimator;


    public void Init(EntityData data,Action<AttackType>onAttack)
    {
        this.onAttack = onAttack;

        animators.Clear();
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
        foreach (var animator in animators)
            animator.SetInteger(key_characterType, (int)type);
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

    #region//速度控制
    private float speed=1;
    private bool paused=false;
    public void SetPaused(bool paused)
    {
        this.paused = paused; // 形参与字段同名，漏掉 this 会写到参数上：字段永远为 false，暂停/恢复全部失效
        UpdateSpeed();
    }

    public void SetMoveSpeedScale(float scale)
    {
        speed = scale;
        UpdateSpeed();
    }
    private void UpdateSpeed()
    {
        float target = paused ? 0f : speed;
        foreach (var animator in animators)
        {
            if (animator != null) animator.speed = target;
        }
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

    #region//动画控制
    public void DoSpawn()
    {
        foreach(var animator in animators)
            animator.SetTrigger(key_spawn);
    }
    public void InAir(bool inAir)
    {
        foreach (var animator in animators)
            animator.SetBool(key_inAir, inAir);
    }
    public void Move(bool moving)
    {
        foreach (var animator in animators)
            animator.SetBool(key_moving, moving);
    }
    public void DoSlide(float last = 3f)
    {
        foreach (var animator in animators)
            animator.SetBool(key_slide, true);
    }
    public void EndSlide()
    {
        foreach (var animator in animators)
        {
            animator.SetBool(key_slide, false);
            animator.SetTrigger(key_slideEnd);
        }
    }
    public void Roll()
    {
        foreach (var animator in animators)
            animator.SetTrigger(key_roll);
    }
    public void DoAttack(AttackType attack)
    {
        foreach (var animator in animators)
        {
            animator.SetInteger(key_attackId, (int)attack); // 先选招式（Attack 子状态机按 AttackId 选状态）
            animator.SetTrigger(key_attack);                // 再进 Attack 子状态机
        }
    }
    public void DoHit()
    {
        foreach (var animator in animators)
            animator.SetTrigger(key_hit);
    }
    public void DoDie()
    {
        foreach (var animator in animators)
            animator.SetTrigger(key_die);
    }
    #endregion
}
