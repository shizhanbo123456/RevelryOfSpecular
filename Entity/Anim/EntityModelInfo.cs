using UnityEngine;

/// <summary>
/// 实体模型的本地包围盒（由 Tools/实体/计算并录入模型 Bounds 自动录入，不要手工填）。
/// xRange / yRange / zRange 各存一个轴上的 (min, max)，值域为预制体根节点本地空间。
/// </summary>
public class EntityModelInfo:MonoBehaviour
{
    public Vector2 xRange;
    public Vector2 yRange;
    public Vector2 zRange;

    /// <summary>
    /// 按录入的本地包围盒构造一个胶囊碰撞体：
    /// - 高度（上下范围）取 Y 轴范围 yRange.y - yRange.x；
    /// - 半径取 X 轴与 Z 轴范围的平均值的一半（宽深平均，避免侧向胖/前后瘦的模型单轴失真）；
    /// - 沿 Y 轴（direction = 1），中心落在各轴中点。
    /// 已存在 CapsuleCollider 则复用并覆盖参数，不重复创建其它碰撞体。
    /// 值域为根节点本地空间，因此直接配到本组件所在 GameObject 的本地 transform 上即可。
    /// </summary>
    public CapsuleCollider BuildCapsuleCollider()
    {
        float height = yRange.y - yRange.x;
        float radius = ((xRange.y - xRange.x) + (zRange.y - zRange.x)) * 0.25f;
        Vector3 center = new Vector3(
            (xRange.x + xRange.y) * 0.5f,
            (yRange.x + yRange.y) * 0.5f,
            (zRange.x + zRange.y) * 0.5f);

        var col = GetComponent<CapsuleCollider>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
        col.direction = 1; // 沿 Y 轴
        col.height = height;
        col.radius = radius;
        col.center = center;
        return col;
    }

    /// <summary>
    /// 按模型根节点本地空间的上下边界调整胶囊（人形实体的动态受击体积，见 EntityData.TickDynamicCapsule）：
    /// 只改高度与 Y 中心，X/Z 中心与半径不变；高度会夹到不小于直径（Unity 要求 height >= 2 × radius）。
    /// </summary>
    public void SetCapsuleVerticalBounds(float localBottom, float localTop)
    {
        var col = GetComponent<CapsuleCollider>();
        if (col == null) return;
        float height = Mathf.Max(localTop - localBottom, col.radius * 2f);
        float centerY = (localBottom + localTop) * 0.5f;
        if (Mathf.Approximately(height, col.height) && Mathf.Approximately(centerY, col.center.y)) return; // 值没变不惊动物理
        col.height = height;
        col.center = new Vector3(col.center.x, centerY, col.center.z);
    }

    private void OnDrawGizmos()
    {
        Vector3 size = new Vector3(xRange.y - xRange.x, yRange.y - yRange.x, zRange.y - zRange.x);
        if (size == Vector3.zero) return; // 尚未录入，没什么可画

        // 按本地空间画：录入值就是根节点本地系，交给矩阵一次性处理旋转与缩放
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 0.85f, 0.95f, 0.6f);
        Gizmos.DrawWireCube(new Vector3(
            (xRange.x + xRange.y) * 0.5f,
            (yRange.x + yRange.y) * 0.5f,
            (zRange.x + zRange.y) * 0.5f), size);
        Gizmos.matrix = previous;
    }
}
