using UnityEngine;

public class AnimEvent : StateMachineBehaviour
{
    protected EntityAnim anim;
    protected EntityData data;
    protected bool initialized = false;
    public void Init(EntityAnim anim,EntityData data)
    {
        this.anim=anim;
        this.data= data;
        initialized= true;
    }
}