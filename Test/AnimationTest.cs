using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationTest : MonoBehaviour
{
    private static readonly string[] CharacterPrefabFolders =
    {
        "Assets/Files/Prefabs/Entity/Attacker",
        "Assets/Files/Prefabs/Entity/Defenser",
    };

    private TestEntityData current;
    private Vector2 attackScroll;
    private GUIStyle bigStyle;
    private static readonly EntityAnim.AttackType[] AttackTypes =
        (EntityAnim.AttackType[])System.Enum.GetValues(typeof(EntityAnim.AttackType));

    private void Start()
    {
        EnsureInfoManager();
        EnsureCamera();
        SpawnRandom();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SpawnRandom();
            return;
        }
        if (current == null) return;

        // 移动输入（坦克式）：W = 前进；A/D = 转向（W 按住 = 边走边转，未按 = 原地转）；S 弃用
        bool forward = Input.GetKey(KeyCode.W);
        float turn = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        current.SetTestInput(forward, turn);

        // Run/Idle 切换（正式版在输入边沿写，轮询等效）
        if (current.anim != null) current.anim.Move(current.TestMoving);

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.K)) && current.anim != null) current.anim.DoJump();
        if (Input.GetKeyDown(KeyCode.LeftShift) && current.anim != null) current.anim.DoSlide();

        // 诊断：绕过动画声明直写速度，用于区分"声明侧断了"还是"刚体/物理侧断了"
        if (Input.GetKey(KeyCode.T)) current.SetVelocityForward(3f, VelocitySource.Animation);

        // 与服务器循环同一条速度结算链路
        current.TickVelocity(Time.deltaTime);
    }

    private void OnGUI()
    {
        if (current == null) return;
        GUI.Label(new Rect(10f, 10f, 1200f, 30f),
            "[AnimationTest] " + current.DescribeState() + "   按住T=直写3m/s W前进 A/D转向 Space跳 Shift滑铲 R换人");

        // 当前速度大字显示（刚体实际速度 = 速度结算链路的最终输出）
        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold };
            bigStyle.normal.textColor = Color.white;
        }
        Vector3 v = current.rb != null ? current.rb.velocity : Vector3.zero;
        GUI.Label(new Rect(10f, 48f, 900f, 60f),
            $"速度 = ({v.x:F2}, {v.y:F2}, {v.z:F2})  |v| = {v.magnitude:F2} m/s", bigStyle);

        // 攻击动画按钮：逐个 AttackType 触发（与正式版同一入口 anim.DoAttack）
        GUILayout.BeginArea(new Rect(Screen.width - 230f, 40f, 220f, Screen.height - 50f));
        GUILayout.Label("攻击动画");
        attackScroll = GUILayout.BeginScrollView(attackScroll);
        if (current.anim != null)
        {
            foreach (var type in AttackTypes)
            {
                if (GUILayout.Button(type.ToString())) current.anim.DoAttack(type);
            }
        }
        else
        {
            GUILayout.Label("模型未挂 EntityAnim");
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void SpawnRandom()
    {
        if (current != null) Destroy(current.gameObject);

        GameObject prefab = PickRandomPrefab();
        if (prefab == null)
        {
            Debug.LogError("[AnimationTest] 找不到人物模型预制体：确认 Files/Prefabs/Entity 的 Attacker/Defenser 目录，或在 Inspector 配 manualPrefabs");
            return;
        }

        GameObject go = Instantiate(prefab, transform.position, transform.rotation);
        go.name = $"AnimationTest_{prefab.name}";
        current = go.AddComponent<TestEntityData>();
        current.SetupTest();
        // 相机跟随目标换成新实体（CameraController 对远距离目标有瞬移阈值，不会飞过去）
        if (Tool.CameraController != null) Tool.CameraController.SetLookTarget(current.transform);
        Debug.Log($"[AnimationTest] 已生成 {prefab.name} @ {transform.position}");
    }

    private GameObject PickRandomPrefab()
    {
        var candidates = new List<GameObject>();
#if UNITY_EDITOR
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", CharacterPrefabFolders))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null) candidates.Add(prefab);
        }
#endif
        if (candidates.Count == 0 && manualPrefabs != null)
            foreach (var prefab in manualPrefabs)
                if (prefab != null) candidates.Add(prefab);

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private static void EnsureInfoManager()
    {
        if (Tool.InfoManager != null) return;

        var go = new GameObject("[AnimationTest] InfoManager");
        var manager = go.AddComponent<InfoManager>();
        int ground = LayerMask.NameToLayer("Ground");
        manager.ground_layer = ground >= 0 ? ground : 0;
        Debug.Log($"[AnimationTest] 场景无 InfoManager，已补最小实例（ground_layer={manager.ground_layer}）");
    }

    // 测试场景无相机时补一个；已有多余 CameraController 的场景直接复用
    private static void EnsureCamera()
    {
        if (Tool.CameraController != null) return;
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("[AnimationTest] Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
        cam.gameObject.AddComponent<CameraController>();
    }

    [Tooltip("可选：手动指定人物模型预制体（打包运行时的唯一来源；编辑器下扫描目录为空才生效）")]
    [SerializeField] private List<GameObject> manualPrefabs = new();
}
