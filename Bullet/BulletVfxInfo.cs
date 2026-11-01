using System.Collections.Generic;
using UnityEngine;

public class BulletVfxInfo:MonoBehaviour
{
    [System.Serializable]
    public struct HitInfo
    {
        public float start;
        public float end;
        public Vector3 center;
        public float radius;
    }
    public List<HitInfo> hits=new();
    [Tooltip("特效存活时长（秒）；-1 = 资产自身不主动清除，由代码给时长（简单子弹等）")]
    public float lifeTime = -1f;

    // 资产填写的存活时长；没有 BulletVfxInfo 或填 -1 时返回 -1，表示由代码决定
    public static float LifeOf(GameObject prefab)
    {
        if (prefab == null) return -1f;
        var info = prefab.GetComponent<BulletVfxInfo>();
        return info != null ? info.lifeTime : -1f;
    }

#if UNITY_2017_1_OR_NEWER
    [SerializeField] private float time;
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.7f, 0.1f, 0.7f);
        foreach(var hit in hits)
        {
            if (time > hit.start && time < hit.end)
            {
                Gizmos.DrawSphere(hit.center, hit.radius);
            }
        }
    }
}