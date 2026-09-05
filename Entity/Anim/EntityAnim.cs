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
        Die
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
    public void DoDie()
    {
        animator.SetTrigger(key_die);
    }
}