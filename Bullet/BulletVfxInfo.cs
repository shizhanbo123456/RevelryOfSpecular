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
    public float lifeTime = -1f;

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