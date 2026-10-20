using UnityEngine;

public class AnimFallEvent : AnimMotionEvent
{
    protected override InputBlockOp blockOp => InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
}
