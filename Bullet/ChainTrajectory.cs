using UnityEngine;

/// <summary>
/// 链式跳轨迹（雷球等）：依次经过「施放者 → 第一目标 → 第二目标」三个实体 id。
/// 前半段飞向第一目标、后半段跳向第二目标；位置按 id 双端取位（服务器判定与客户端表现共用）。
/// </summary>
public class ChainTrajectory : BulletTrajectory
{
    private readonly ushort casterId;
    private readonly ushort firstId;
    private readonly ushort secondId;
    private Vector3 lastFirst = Vector3.zero;
    private Vector3 lastSecond = Vector3.zero;

    public ChainTrajectory(ushort casterId, ushort firstId, ushort secondId)
    {
        this.casterId = casterId;
        this.firstId = firstId;
        this.secondId = secondId;
    }

    /// <summary>前半段（0~0.5）施放者→第一目标，后半段（0.5~1）第一目标→第二目标。</summary>
    public override Vector3 Lerp(float factor)
    {
        if (TryGetEntityPosition(firstId, out var first)) lastFirst = first;
        if (TryGetEntityPosition(secondId, out var second)) lastSecond = second;
        if (factor < 0.5f)
        {
            Vector3 from = lastFirst;
            if (TryGetEntityPosition(casterId, out var caster)) from = caster;
            return Vector3.Lerp(from, lastFirst, factor * 2f);
        }
        return Vector3.Lerp(lastFirst, lastSecond, (factor - 0.5f) * 2f);
    }
}
