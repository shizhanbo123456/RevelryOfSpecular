using UnityEngine;

public class AnimSpawnEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Spawn;
    protected override InputBlockOp blockOp => InputBlockOp.Forward | InputBlockOp.Rotation | InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
}
