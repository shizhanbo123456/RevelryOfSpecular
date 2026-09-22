using UnityEngine;

public class EntityAnimData : MonoBehaviour
{
    [Header("模型参数（客户端模型与服务端模板一致）")]
    public float height;                    // 身高
    public float legHeight;                 // 腿高
    public EntityAnim.CharcterAnimType type;// 动作集类型

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(0.5f, height, 0.5f));
        Gizmos.color = new Color(0.9f, 0.7f, 0.3f, 0.5f);
        Gizmos.DrawCube(transform.position + Vector3.up * legHeight * 0.5f, new Vector3(0.3f, legHeight, 0.3f));
    }

    public static float LegHeightToStandartRunSpeed(float legHeight)
    {
        return legHeight * 2.6f;
    }
}
