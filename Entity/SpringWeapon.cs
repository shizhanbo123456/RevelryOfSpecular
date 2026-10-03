using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpringWeapon : MonoBehaviour
{
    [SerializeField]private List<Transform> Anchors;
    [SerializeField] private float elasticityCoefficient = 1f;
    [SerializeField] private float drag = 1f;
    private Vector3[] springPos;
    private Vector3[] springSpeed;
    private Quaternion[] springRotation;
    private const int count = 8;
    private bool initialized = false;
    public void Init()//代码中可以立即初始化，防止Start不及时
    {
        Start();
    }
    public void Start()
    {
        if (initialized) return;
        springPos = Anchors.Select(t => t.position).ToArray();
        springSpeed = new Vector3[count];
        springRotation = Anchors.Select(t => t.rotation).ToArray();
        initialized = true;
    }
    private void Update()
    {
        if (!initialized) return;
        for (int i = 0; i < count; i++)
        {
            float d = (Anchors[i].position-springPos[i]).magnitude;
            float ds = d * elasticityCoefficient * Time.deltaTime;
            springSpeed[i] += (Anchors[i].position - springPos[i]).normalized * ds;
            springSpeed[i] *= 1f - drag * Time.deltaTime;
            springPos[i] += springSpeed[i] * Time.deltaTime;
            springRotation[i]=Quaternion.Lerp(springRotation[i], Anchors[i].rotation, Time.deltaTime * 10f);
        }
    }
    private void OnDrawGizmos()
    {
        if (!initialized) return;
        for(int i = 0; i < count; i++)
        {
            Gizmos.DrawSphere(springPos[i], 0.1f);
        }
    }
    public void GetPos(int index,out Vector3 v,out Quaternion q)
    {
        v = springPos[index];
        q = springRotation[index];
    }

    public const int slotCount = 8;

    // 将 SpringWeapon 预制体挂到 host（客户端表现体）根物体下：X/Z 与旋转跟随实体自身，Scale=模型高度/2。
    // 高度（局部 Y）由调用方每帧抬到根骨骼(Hips)高度（无骨骼回退模型中心），见 EntityPlayerManager。
    public static SpringWeapon Attach(GameObject host)
    {
        var prefab = Tool.InfoManager != null ? Tool.InfoManager.SpringWeapon : null;
        if (prefab == null) return null;
        var modelInfo = host.GetComponentInChildren<EntityModelInfo>();
        float modelHeight = modelInfo != null ? (modelInfo.yRange.y - modelInfo.yRange.x) : 2f;
        var sw = Object.Instantiate(prefab, host.transform).GetComponent<SpringWeapon>();
        sw.transform.localPosition = Vector3.zero;
        sw.transform.localRotation = Quaternion.identity;
        sw.transform.localScale = Vector3.one * (modelHeight * 0.5f);
        sw.Init();
        return sw;
    }
}
