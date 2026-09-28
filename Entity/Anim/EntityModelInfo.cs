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
    /// - 半径取 Z 轴前后距离的一半 (zRange.y - zRange.x) * 0.5f；
    /// - 沿 Y 轴（direction = 1），中心落在各轴中点。
    /// 已存在 CapsuleCollider 则复用并覆盖参数，不重复创建其它碰撞体。
    /// 值域为根节点本地空间，因此直接配到本组件所在 GameObject 的本地 transform 上即可。
    /// </summary>
    public CapsuleCollider BuildCapsuleCollider()
    {
        float height = yRange.y - yRange.x;
        float radius = (zRange.y - zRange.x) * 0.5f;
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
