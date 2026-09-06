using System;
using UnityEngine;

public class EntityAnim : MonoBehaviour
{
    private const string key_characterType = "CharacterType";
    private const string key_spawn = "DoSpawn";
    private const string key_moving = "Moving";
    private const string key_inAir = "InAir";
    private const string key_slide = "Slide";
    private const string key_slideEnd = "SlideEnd";
    private const string key_roll = "Roll";
    private const string key_doAttack = "DoAttack";
    private const string key_attack = "Attack";
    private const string key_hit = "Hit";
    private const string key_die = "Death";
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
        None=0,
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
    public Action<AttackType> onAttack;//动画中的攻击事件回调
    private Animator animator;

    /// <summary>当前动画状态（由各触发方法维护，供服务器同步表现摘要 SCEntityDisplayInfo.animState）。</summary>
    public AnimState currentState = AnimState.Motion;

    /// <summary>当前具体动画 id（Attack=AttackType / Motion=MotionType，其余 0；供 SCEntityDisplayInfo.animId）。</summary>
    public int currentAnimId = (int)MotionType.Idle;

    /// <summary>取当前动画状态、具体动画 id 与播放进度（归一化 0~1，供表现摘要同步）。</summary>
    public void GetDisplayAnim(out AnimState state, out int animId, out float normalizedTime)
    {
        state = currentState;
        animId = currentAnimId;
        normalizedTime = 0f;
        if (animator != null)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            normalizedTime = st.length > 0f ? Mathf.Repeat(st.normalizedTime, 1f) : 0f;
        }
    }

    public void Init(EntityData data,Action<AttackType>onAttack)
    {
        this.onAttack = onAttack;

        animator = GetComponent<Animator>();
        if (animator == null) 
        { 
            Debug.LogError($"{gameObject.name}未挂载动画状态机");
            return;
        }
        var behaviours=animator.GetBehaviours<AnimEvent>();
        foreach (var be in behaviours) be.Init(this,data);
    }
    public void SetType(CharcterAnimType type)
    {
        animator.SetInteger(key_characterType, (int)type);
    }

    /// <summary>暂停/恢复动画播放（强控施加 = 速度置 0，当前攻击动画事件随之停止触发）。</summary>
    public void SetPaused(bool paused)
    {
        if (animator != null) animator.speed = paused ? 0f : 1f;
    }

    public void DoSpawn()
    {
        currentState = AnimState.Spawn;
        animator.SetTrigger(key_spawn);
    }
    public void InAir(bool inAir)
    {
        if (!inAir) currentState = AnimState.Motion;
        if (inAir) currentAnimId = (int)MotionType.Jump;
        animator.SetBool(key_inAir, inAir);
    }
    public void Move(bool moving)
    {
        currentState = AnimState.Motion;
        currentAnimId = (int)(moving ? MotionType.Run : MotionType.Idle);
        animator.SetBool(key_moving, moving);
    }
    public void DoSlide(float last = 3f)
    {
        currentState = AnimState.Motion;
        currentAnimId = (int)MotionType.Slide;
        animator.SetBool(key_slide, true);
    }
    public void EndSlide()
    {
        currentState = AnimState.Motion;
        currentAnimId = (int)MotionType.Idle;
        animator.SetBool(key_slide,false);
        animator.SetTrigger(key_slideEnd);
    }
    public void Roll()
    {
        currentState = AnimState.Motion;
        currentAnimId = (int)MotionType.Roll;
        animator.SetTrigger(key_roll);
    }
    public void DoAttack(AttackType attack)
    {
        currentState = AnimState.Attack;
        currentAnimId = (int)attack;
        animator.SetInteger(key_attack, (int)attack);
        animator.SetTrigger(key_doAttack);
    }
    public void DoHit()
    {
        currentState = AnimState.Hit;
        currentAnimId = 0;
        animator.SetTrigger(key_hit);
    }
    public void DoDie()
    {
        currentState = AnimState.Die;
        currentAnimId = 0;
        animator.SetTrigger(key_die);
    }
}