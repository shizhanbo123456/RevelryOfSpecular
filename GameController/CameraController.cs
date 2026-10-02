using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform lookTarget;

    public Vector2 zRange = new Vector2(3f, 8f);
    public Vector2 yRange = new Vector2(1f, 3f);

    public float slowSmooth = 2f;
    public float fastSmooth = 10f;

    private float yaw;

    private void Awake()
    {
        Tool.CameraController = this;
    }

    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
        if (target != null) yaw = target.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (lookTarget == null) return;

        float targetYaw = lookTarget.eulerAngles.y;
        yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-fastSmooth * Time.deltaTime));

        Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
        float zCenter = (zRange.x + zRange.y) * 0.5f;
        float yCenter = (yRange.x + yRange.y) * 0.5f;

        Vector3 toCam = transform.position - lookTarget.position;
        float yCur = toCam.y;
        float zCur = Vector3.Dot(toCam, back);

        // y/z 各自按是否越界独立选慢速/快速平滑，目标均为区间中心
        float yRate = (yCur >= yRange.x && yCur <= yRange.y) ? slowSmooth : fastSmooth;
        float zRate = (zCur >= zRange.x && zCur <= zRange.y) ? slowSmooth : fastSmooth;

        float yNew = Mathf.Lerp(yCur, yCenter, 1f - Mathf.Exp(-yRate * Time.deltaTime));
        float zNew = Mathf.Lerp(zCur, zCenter, 1f - Mathf.Exp(-zRate * Time.deltaTime));

        transform.position = lookTarget.position + Vector3.up * yNew + back * zNew;
        transform.LookAt(lookTarget.position);
    }
}
