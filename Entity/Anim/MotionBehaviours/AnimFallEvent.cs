using UnityEngine;

public class AnimFallEvent : AnimMotionEvent
{
    protected override InputBlockOp blockOp => InputBlockOp.Forward | InputBlockOp.Rotation | InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
}
