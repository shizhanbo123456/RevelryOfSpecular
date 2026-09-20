using UnityEngine;

public class AnimDieEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Die;
}
