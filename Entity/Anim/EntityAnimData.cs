using UnityEngine;

public class EntityAnimData:MonoBehaviour
{
    public float height;
    public float legHeight;
    public EntityAnim.CharcterAnimType type;
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