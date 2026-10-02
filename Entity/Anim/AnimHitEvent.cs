using UnityEngine;

public class AnimHitEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Hit;
    protected override InputBlockOp blockOp => InputBlockOp.Forward | InputBlockOp.Rotation | InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
}
