using UnityEngine;

public class AnimEvent : StateMachineBehaviour
{
    protected virtual EntityAnim.AnimState State { get; }
    //用于传递给客户端识别动画片段
    public int AnimId { get; private set; } = -1;

    protected EntityAnim anim;
    protected EntityData data;
    protected bool main;
    protected float MoveSpeed=>anim.animData.RunSpeed;
    protected float JumpSpeed=>anim.animData.JumpSpeed;
    protected bool initialized = false;

    public void Init(EntityAnim anim, EntityData data,bool mainAnim)//仅仅主动画机的片段会触发事件
    {
        this.anim = anim;
        this.data = data;
        main = mainAnim;
        initialized = true;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (data == null) return; // client has no EntityData: this log is server-only
        AnimId = stateInfo.fullPathHash;
        if (!main) return;
        anim.NotifyStateEnter(AnimId, State);

        // Server-side state switch log (English only): target clip + progress. Set logStateSwitch = false before shipping.
        if (logStateSwitch)
        {
            Debug.LogWarning($"[Anim] id={data.id} type={data.type} state={AnimId} progress={stateInfo.normalizedTime:F3} length={stateInfo.length:F2}s cur={ClipNames(animator.GetCurrentAnimatorClipInfo(layerIndex))} next={ClipNames(animator.GetNextAnimatorClipInfo(layerIndex))}");
        }
    }

    #region//Local
    /// <summary>Server-side switch log switch.</summary>
    private static bool logStateSwitch = true;

    /// <summary>Clip names of an animator clip info array; "-" when empty, "+" joins multiple (blending).</summary>
    private static string ClipNames(AnimatorClipInfo[] infos)
    {
        if (infos == null || infos.Length == 0) return "-";
        var sb = new System.Text.StringBuilder();
        foreach (var ci in infos)
        {
            if (ci.clip == null) continue;
            if (sb.Length > 0) sb.Append('+');
            sb.Append(ci.clip.name);
        }
        return sb.Length > 0 ? sb.ToString() : "-";
    }
    #endregion
}
