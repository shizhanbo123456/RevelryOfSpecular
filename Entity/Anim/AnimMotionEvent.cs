using UnityEngine;

public class AnimMotionEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Motion;
}
