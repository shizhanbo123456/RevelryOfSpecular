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

    /// <summary>当前状态 hash（-1 = 尚未进入任何状态）</summary>
    private int currentAnimId = -1;

    /// <summary>状态 hash → 状态脚本（进入状态时登记）</summary>
    private readonly Dictionary<int, AnimEvent> animEventMap = new Dictionary<int, AnimEvent>();

    /// <summary>状态脚本进入状态时回调（OnStateEnter 在过渡第一帧触发）</summary>
    public void NotifyStateEnter(int animId, AnimState state)
    {
        currentAnimId = animId;
        CurrentState = state;
    }

    /// <summary>登记状态脚本（按其状态 hash 索引）</summary>
    public void RegisterAnimEvent(AnimEvent animEvent)
    {
        animEventMap[animEvent.AnimId] = animEvent;
    }

    /// <summary>按状态 hash 取状态脚本</summary>
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
        foreach (var behaviour in behaviours) behaviour.Init(this, data);
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

    #region 手持物体（客户端表现：武器/道具模型挂到手部）
    [Header("手持物体挂点（不配置时按 Humanoid 骨骼自动定位手部）")]
    [SerializeField] private Transform handMountR;
    [SerializeField] private Transform handMountL;

    /// <summary>已挂载的手持物体实例（[0] = 右手，[1] = 左手）。</summary>
    private readonly GameObject[] heldObjects = new GameObject[2];

    /// <summary>
    /// 设置手持物体：把预制体实例挂到手部，替换该手上的旧实例（prefab 传 null = 清除该手）。
    /// 预制体由调用方从 AssetsManager 武器列表取得（客户端表现专用，服务器模板无图形）。
    /// 依赖 animator 字段（Init 时赋值，Init 后续接线调用）。
    /// </summary>
    public void SetHeldObject(GameObject prefab, bool leftHand = false)
    {
        int slot = leftHand ? 1 : 0;
        if (heldObjects[slot] != null) Destroy(heldObjects[slot]);
        heldObjects[slot] = null;
        if (prefab == null) return;

        Transform mount = GetHandMount(leftHand);
        if (mount == null)
        {
            Debug.LogWarning($"{gameObject.name} 未找到{(leftHand ? "左" : "右")}手挂点：模型非 Humanoid 时请在 Inspector 配置 handMount{(leftHand ? "L" : "R")}");
            return;
        }
        heldObjects[slot] = Instantiate(prefab, mount);
        heldObjects[slot].transform.localPosition = Vector3.zero;
        heldObjects[slot].transform.localRotation = Quaternion.identity;
    }

    /// <summary>清除手持物体（leftHand = 清左手，默认清右手）。</summary>
    public void ClearHeldObject(bool leftHand = false) => SetHeldObject(null, leftHand);

    /// <summary>取手部挂点：优先 Inspector 配置的挂点，否则按 Humanoid 骨骼自动定位手部。</summary>
    private Transform GetHandMount(bool leftHand)
    {
        var configured = leftHand ? handMountL : handMountR;
        if (configured != null) return configured;
        return animator != null
            ? animator.GetBoneTransform(leftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand)
            : null;
    }
    #endregion

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
