using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 动画测试驱动器：挂场景任意物体，Play 即生效（不依赖网络与 BattleManager）。
/// 随机生成人物模型 + 本地键位驱动 + 每帧 TickVelocity。键位：W 前进 / A D 转向 / Space 或 K 跳 /
/// 左 Shift 滑铲 / R 重新随机生成 / 按住 T 绕过动画声明直写 3m/s（二分定位移动断点）。
/// 机制见《代码架构说明》Test 文件夹节。
/// </summary>
public class AnimationTest : MonoBehaviour
{
    /// <summary>人物模型预制体扫描目录（攻守双方角色）。</summary>
    private static readonly string[] CharacterPrefabFolders =
    {
        "Assets/Files/Prefabs/Entity/Attacker",
        "Assets/Files/Prefabs/Entity/Defenser",
    };

    private TestEntityData current;

    private void Start()
    {
        EnsureInfoManager();
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

    /// <summary>诊断 HUD（测试工具常开，不影响正式逻辑）。</summary>
    private void OnGUI()
    {
        if (current == null) return;
        GUI.Label(new Rect(10f, 10f, 1200f, 30f),
            "[AnimationTest] " + current.DescribeState() + "   按住T=直写3m/s W前进 A/D转向 Space跳 Shift滑铲 R换人");
    }

    /// <summary>销毁当前模型并随机生成一个新的（出生点 = 本组件所在物体的位置与朝向）。</summary>
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
        Debug.Log($"[AnimationTest] 已生成 {prefab.name} @ {transform.position}");
    }

    /// <summary>随机取人物模型预制体：编辑器扫目录，拿不到（打包/目录空）退回 Inspector 手动列表。</summary>
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

    /// <summary>
    /// 落地检测经 EntityPhysics.GroundMask 读 Tool.InfoManager.ground_layer；
    /// 场景没有 InfoManager 时补最小实例：ground_layer 优先取 Ground 层（无则 Default）。
    /// </summary>
    private static void EnsureInfoManager()
    {
        if (Tool.InfoManager != null) return;

        var go = new GameObject("[AnimationTest] InfoManager");
        var manager = go.AddComponent<InfoManager>();
        int ground = LayerMask.NameToLayer("Ground");
        manager.ground_layer = ground >= 0 ? ground : 0;
        Debug.Log($"[AnimationTest] 场景无 InfoManager，已补最小实例（ground_layer={manager.ground_layer}）");
    }

    /// <summary>打包运行（AssetDatabase 不可用）或扫描目录为空时的兜底列表。</summary>
    [Tooltip("可选：手动指定人物模型预制体（打包运行时的唯一来源；编辑器下扫描目录为空才生效）")]
    [SerializeField] private List<GameObject> manualPrefabs = new();
}
