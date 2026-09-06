using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnim : MonoBehaviour
{
    private const string key_characterType = "CharacterType";
    private const string key_moveSpeed = "MoveSpeed";
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

    /// <summary>
    /// 当前状态大类（纯推送制：由各状态的 AnimEvent.OnStateEnter 回调驱动，
    /// Do* 方法只负责写状态机参数/触发器，不直接改状态）。
    /// </summary>
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
    public Action<AnimState, AnimState> OnStateChange;

    /// <summary>当前状态的编号（由 AnimEvent 推送，供 SCEntityDisplayInfo.animId 同步；-1 = 尚未进入任何已配置状态）。</summary>
    private int currentAnimId = -1;

    /// <summary>animId → AnimEvent 映射（Init 时从全部状态机行为构建，供客户端按编号定位状态脚本）。</summary>
    private readonly Dictionary<int, AnimEvent> animEventMap = new Dictionary<int, AnimEvent>();

    /// <summary>
    /// AnimEvent 进入状态时回调（推送制状态追踪）。
    /// 注意：OnStateEnter 在进入过渡的第一帧触发，即过渡开始即切换，不等混合完成。
    /// </summary>
    public void NotifyStateEnter(int animId, AnimState state)
    {
        currentAnimId = animId;
        CurrentState = state;
    }

    /// <summary>按编号取状态机事件脚本（客户端收到同步的 animId 后定位用）。</summary>
    public bool TryGetAnimEvent(int animId, out AnimEvent animEvent)
    {
        return animEventMap.TryGetValue(animId, out animEvent);
    }

    public void GetDisplayAnim(out AnimState state, out int animId, out float normalizedTime)
    {
        state = currentState;
        animId = currentAnimId;
        normalizedTime = 0f;
        if (animator != null && animator.layerCount > 0)
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
        animEventMap.Clear();
        // animId 自动分配：按控制器资产内的状态顺序编号（0 起）。
        // 服务器与客户端使用同一 Controller 资产，遍历顺序一致 → 编号一致，可安全跨端同步。
        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].Init(this, data, i);
            animEventMap.Add(i, behaviours[i]);
        }
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

    /// <summary>
    /// 设置动画移动状态的播放速度倍率（加速/减速/泥沼的载体，见策划案 11.3）。
    /// 移动状态的 Speed Parameter 绑定 MoveSpeed 参数（状态机资产侧配置）。
    /// </summary>
    public void SetMoveSpeedScale(float scale)
    {
        animator?.SetFloat(key_moveSpeed, scale);
    }

    public void DoSpawn()
    {
        animator.SetTrigger(key_spawn);
    }
    public void InAir(bool inAir)
    {
        animator.SetBool(key_inAir, inAir);
    }
    public void Move(bool moving)
    {
        animator.SetBool(key_moving, moving);
    }
    public void DoSlide(float last = 3f)
    {
        animator.SetBool(key_slide, true);
    }
    public void EndSlide()
    {
        animator.SetBool(key_slide,false);
        animator.SetTrigger(key_slideEnd);
    }
    public void Roll()
    {
        animator.SetTrigger(key_roll);
    }
    public void DoAttack(AttackType attack)
    {
        animator.SetInteger(key_attack, (int)attack);
        animator.SetTrigger(key_doAttack);
    }
    public void DoHit()
    {
        animator.SetTrigger(key_hit);
    }
    public void DoDie()
    {
        animator.SetTrigger(key_die);
    }
}
