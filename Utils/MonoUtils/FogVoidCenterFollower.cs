using VolumetricFogAndMist;
using UnityEngine;

/// <summary>
/// 雾洞跟随：把 VolumetricFog 的雾洞 Center（fogVoidPosition）每帧同步到本物体所在位置。
/// 只同步 X/Z，Y 保留雾组件 Inspector 中配置的值；fog 字段为空时本脚本不生效。
/// </summary>
public class FogVoidCenterFollower : MonoBehaviour
{
    [SerializeField] private VolumetricFog fog; // Inspector 中拖入雾组件；为空时不生效

    private void LateUpdate()
    {
        if (fog == null) return;
        Vector3 center = fog.fogVoidPosition;
        Vector3 position = transform.position;
        fog.fogVoidPosition = new Vector3(position.x, center.y, position.z);
    }
}
