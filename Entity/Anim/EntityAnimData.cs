using UnityEngine;

/// <summary>
/// 实体动画/判定共用参数组件（挂载在实体预制体根物体：客户端模型与服务端模板参数一致）。
/// 可移动实体（角色/僵尸）根物体挂 EntityAnim + EntityAnimData；
/// 不可移动实体（水晶/防御塔/瘟疫树/守护点）不需要 EntityAnim。
/// EntityData.OnCreate 时读取本组件：判定柱参数覆盖 + 移动速度按腿高换算。
/// </summary>
public class EntityAnimData : MonoBehaviour
{
    [Header("模型参数（客户端模型与服务端模板一致）")]
    public float height;                    // 身高
    public float legHeight;                 // 腿高（决定跑步速度，见 LegHeightToStandartRunSpeed）
    public EntityAnim.CharcterAnimType type;// 动作集类型

    [Header("判定柱参数（bottom~top + 半径，服务器判定与客户端模型共用）")]
    [SerializeField] private float radius = 0.4f;
    public float Bottom => 0f;
    public float Top => height;
    public float Radius => radius;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(0.5f, height, 0.5f));
        Gizmos.color = new Color(0.9f, 0.7f, 0.3f, 0.5f);
        Gizmos.DrawCube(transform.position + Vector3.up * legHeight * 0.5f, new Vector3(0.3f, legHeight, 0.3f));
    }

    /// <summary>标准跑步速度 = 腿高 × 2.6（身高直接决定移动表现与服务器权威移动速度）。</summary>
    public static float LegHeightToStandartRunSpeed(float legHeight)
    {
        return legHeight * 2.6f;
    }
}
