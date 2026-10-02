using VolumetricFogAndMist;
using UnityEngine;

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
