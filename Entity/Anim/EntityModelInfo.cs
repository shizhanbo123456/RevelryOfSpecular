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
