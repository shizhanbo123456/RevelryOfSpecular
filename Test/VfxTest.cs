using System.Collections.Generic;
using UnityEngine;

public class VfxTest : MonoBehaviour
{
    private struct VfxEntry
    {
        public string label;
        public GameObject prefab;
    }

    private static readonly string[] CategoryNames = { "Bullet", "Shield", "RangeMagic", "MagicCircle", "Buff" };

    private readonly List<VfxEntry> entries = new();
    private readonly List<int> categoryStarts = new();
    private Vector2 scroll;
    private int selected = -1;
    private GameObject currentVfx;
    private float angle;
    private Rect windowRect = new(20f, 20f, 300f, 600f);

    [Tooltip("绕圈半径（米）")]
    [SerializeField] private float radius = 10f;
    [Tooltip("绕圈高度（米）")]
    [SerializeField] private float height = 1.5f;
    [Tooltip("角速度（度/秒）")]
    [SerializeField] private float angularSpeed = 90f;

    private void Start()
    {
        BuildEntries();
    }

    private void Update()
    {
        if (currentVfx == null) return;
        angle = Mathf.Repeat(angle + angularSpeed * Time.deltaTime, 360f);
        OrbitPose(angle, out Vector3 pos, out Quaternion rot);
        currentVfx.transform.SetPositionAndRotation(pos, rot);
    }

    // 绕世界原点转圈：位置在半径/高度圆周上，朝向沿运动切线
    private void OrbitPose(float deg, out Vector3 pos, out Quaternion rot)
    {
        float rad = deg * Mathf.Deg2Rad;
        pos = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius + Vector3.up * height;
        rot = Quaternion.LookRotation(new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)), Vector3.up);
    }

    private void OnGUI()
    {
        if (entries.Count == 0 && Tool.AssetsManager != null) BuildEntries();
        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "VfxTest 特效试放");
    }

    private void BuildEntries()
    {
        entries.Clear();
        categoryStarts.Clear();
        var mgr = Tool.AssetsManager;
        if (mgr == null) return;
        AddCategory("Bullet", mgr.BulletVFX);
        AddCategory("Shield", mgr.ShieldVFX);
        AddCategory("RangeMagic", mgr.RangeMagicVFX);
        AddCategory("MagicCircle", mgr.MagicCircleVFX);
        AddCategory("Buff", mgr.BuffVFX);
    }

    private void AddCategory(string name, List<GameObject> list)
    {
        categoryStarts.Add(entries.Count);
        int count = list != null ? list.Count : 0;
        for (int i = 0; i < count; i++)
            entries.Add(new VfxEntry { label = $"{name} {i}", prefab = list[i] });
    }

    private void DrawWindow(int id)
    {
        if (Tool.AssetsManager == null)
        {
            GUILayout.Label("场景中未找到 AssetsManager");
            return;
        }

        GUILayout.Label($"半径 {radius:F1} m");
        radius = GUILayout.HorizontalSlider(radius, 1f, 50f);
        GUILayout.Label($"高度 {height:F1} m");
        height = GUILayout.HorizontalSlider(height, 0f, 20f);
        GUILayout.Label($"角速度 {angularSpeed:F0} °/s");
        angularSpeed = GUILayout.HorizontalSlider(angularSpeed, 10f, 360f);

        string stopText = selected >= 0 && selected < entries.Count
            ? $"停止（当前：{entries[selected].label}）"
            : "停止";
        if (GUILayout.Button(stopText)) Select(-1);

        scroll = GUILayout.BeginScrollView(scroll);
        for (int c = 0; c < categoryStarts.Count && c < CategoryNames.Length; c++)
        {
            int next = c + 1 < categoryStarts.Count ? categoryStarts[c + 1] : entries.Count;
            GUILayout.Label($"── {CategoryNames[c]} ──");
            for (int i = categoryStarts[c]; i < next; i++)
            {
                var e = entries[i];
                GUI.enabled = e.prefab != null;
                string text = (i == selected ? "▶ " : "") + e.label + (e.prefab == null ? "（空）" : "");
                if (GUILayout.Button(text)) Select(i);
            }
        }
        GUI.enabled = true;
        GUILayout.EndScrollView();
    }

    private void Select(int index)
    {
        if (currentVfx != null) Destroy(currentVfx); // 切换/停止时销毁上一个
        currentVfx = null;
        selected = index;
        if (index < 0 || index >= entries.Count || entries[index].prefab == null) return;
        OrbitPose(angle, out Vector3 pos, out Quaternion rot);
        currentVfx = Instantiate(entries[index].prefab, pos, rot);
        currentVfx.name = $"VfxTest_{entries[index].prefab.name}";
    }
}
