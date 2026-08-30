using System;
using UnityEngine;

public class EntityAnim : MonoBehaviour
{
    private const string key_spawn = "DoSpawn";
    private const string key_moving = "Moving";
    private const string key_inAir = "InAir";
    private const string key_slide = "Slide";
    private const string key_slideEnd = "SlideEnd";
    private const string key_doAttack = "DoAttack";
    private const string key_attack = "Attack";
    private const string key_die = "Death";
    public enum AttackType
    {

    }
    public Action<AttackType> onAttack;
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