using UnityEngine;

public class RosPage:MonoBehaviour
{
    public virtual void Construct()
    {

    }
    public virtual void Init()
    {

    }
    public virtual void Enter(ShowParam param)
    {

    }
    public virtual void Exit()
    {

    }

    /// <summary>每帧推进（仅当前页，由 UIManager.Update 驱动）：倒计时/飘字过期等逐帧表现。</summary>
    public virtual void Tick(float deltaTime)
    {

    }
}
public class ShowParam
{

}