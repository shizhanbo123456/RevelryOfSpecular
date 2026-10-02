using UnityEngine;

public class EntityModelInfo:MonoBehaviour
{
    public Vector2 xRange;
    public Vector2 yRange;
    public Vector2 zRange;

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
