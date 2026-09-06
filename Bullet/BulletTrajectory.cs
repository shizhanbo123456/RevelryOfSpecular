using UnityEngine;

/// <summary>
/// 弹道轨迹基类（abstract）。
/// 子弹系统只依赖本基类，不关心具体轨迹实现；新增轨迹类型（直线/追踪/环绕/自定义等）
/// 继承并实现 Lerp 即可（现有实现示例：BezierTrajectory）。
/// 约定：factor 为归一化进度 0~1（0=起点，1=终点）；
/// 服务器与客户端需按相同参数各自构建轨迹（服务器用于逻辑判定，客户端用于表现）。
/// 轨迹实现允许携带状态，可实现随时间变化的动态轨迹（如跟随目标、受场景空间影响）。
/// </summary>
public abstract class BulletTrajectory
{
    /// <summary>取归一化进度 factor（0~1）处的位置。</summary>
    public abstract Vector3 Lerp(float factor);

    /// <summary>轨迹起点。</summary>
    public virtual Vector3 Start => Lerp(0f);

    /// <summary>轨迹终点。</summary>
    public virtual Vector3 End => Lerp(1f);
}
