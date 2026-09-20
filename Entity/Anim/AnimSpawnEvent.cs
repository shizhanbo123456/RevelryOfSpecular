using UnityEngine;

public class AnimSpawnEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Spawn;
}
