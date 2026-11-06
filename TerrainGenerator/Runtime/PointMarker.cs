using UnityEngine;

namespace TerrainGen
{
    public class PointMarker : MonoBehaviour
    {
        public string label = "Point";
        public Color color = Color.cyan;
        public float radius = 3f;

        void OnDrawGizmos()
        {
            Vector3 p = transform.position;
            Vector3 top = p + Vector3.up * radius * 2f;

            Gizmos.color = color;
            Gizmos.DrawWireSphere(p, radius);
            Gizmos.DrawLine(p, top);
            Gizmos.DrawLine(top + Vector3.left * radius * 0.6f, top + Vector3.right * radius * 0.6f);
            Gizmos.DrawLine(top + Vector3.forward * radius * 0.6f, top + Vector3.back * radius * 0.6f);

            // Gizmos 没有 DrawWireDisc，用线段拼地面圆环
            const int segments = 24;
            Vector3 prev = p + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 cur = p + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(transform.position + Vector3.up * (radius * 2f + 0.5f), label);
        }
#endif
    }
}
