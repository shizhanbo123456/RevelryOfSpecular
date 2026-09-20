using UnityEngine;

public class AnimHitEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Hit;
}
