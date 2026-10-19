using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class VfxSnapshotMenu
{
    private const string OutputDir = "Assets/Scenes/VfxTest/graphics";
    private const string TestFlagKey = "VfxSnapshot.TestRan";
    private const int Size = 512;
    private const float Delay = 0.5f;
    private const float Gap = 1f;
    private const int TestPerCategory = 3;

    private static readonly string[] CategoryNames = { "Bullet", "Shield", "RangeMagic", "MagicCircle", "Buff" };

    private struct Item
    {
        public string fileName;
        public GameObject prefab;
    }

    private static readonly List<Item> items = new();
    private static int cursor;
    private static int failed;
    private static bool running;
    private static bool isTest;
    private static GameObject current;
    private static float nextTime;
    private static bool inGap;
    private static Camera cam;
    private static RenderTexture rt;
    private static Texture2D tex;

    [MenuItem("Tools/截图/特效截图 - 测试（每类前 3 个）", false, 21)]
    public static void CaptureTest()
    {
        Begin(TestPerCategory, true);
    }

    [MenuItem("Tools/截图/特效截图 - 全量（全部特效）", false, 22)]
    public static void CaptureAll()
    {
        Begin(int.MaxValue, false);
    }

    [MenuItem("Tools/截图/特效截图 - 测试（每类前 3 个）", true)]
    private static bool ValidateTest()
    {
        return !running;
    }

    [MenuItem("Tools/截图/特效截图 - 全量（全部特效）", true)]
    private static bool ValidateAll()
    {
        return !running;
    }

    private static void Begin(int perCategory, bool test)
    {
        if (running) return;
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("特效截图",
                "必须在 Play 模式下运行：预制体靠 Play On Awake 自动播放，编辑模式下截到的全是静止初始帧。", "知道了");
            return;
        }

        var mgr = Tool.AssetsManager;
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("特效截图", "场景内 AssetsManager 未注册（Awake 未执行）：请确认当前在 Play 模式且 VfxTest 场景挂有 AssetsManager。", "知道了");
            return;
        }

        BuildQueue(mgr, perCategory);
        if (items.Count == 0)
        {
            EditorUtility.DisplayDialog("特效截图", "特效列表为空，没有可截图的条目。", "知道了");
            return;
        }

        string mode = test ? "测试" : "全量";
        if (!test && !SessionState.GetBool(TestFlagKey, false) &&
            !EditorUtility.DisplayDialog("全量特效截图",
                $"本次会话还没跑过测试模式，建议先跑测试确认生成正常。\n\n将连续截图 {items.Count} 张，约 {items.Count * (Delay + Gap + 0.2f):F0} 秒（进度条可点取消）。继续？",
                "继续", "取消"))
            return;

        Directory.CreateDirectory(OutputPath());
        running = true;
        isTest = test;
        cursor = 0;
        failed = 0;
        SetupCamera();
        EditorApplication.update += Tick;
        Debug.Log($"[VfxSnapshot] {mode}开始：共 {items.Count} 张 → {OutputDir}");
        CreateCurrent();
    }

    private static void BuildQueue(AssetsManager mgr, int perCategory)
    {
        items.Clear();
        var lists = new[] { mgr.BulletVFX, mgr.ShieldVFX, mgr.RangeMagicVFX, mgr.MagicCircleVFX, mgr.BuffVFX };
        var empty = new List<string>();
        for (int c = 0; c < lists.Length; c++)
        {
            if (lists[c] == null)
            {
                empty.Add(CategoryNames[c]);
                continue;
            }
            int count = Mathf.Min(lists[c].Count, perCategory);
            for (int i = 0; i < count; i++)
            {
                if (lists[c][i] == null)
                {
                    empty.Add($"{CategoryNames[c]}[{i}]");
                    continue;
                }
                items.Add(new Item { fileName = $"{CategoryNames[c]}_{i:00}.png", prefab = lists[c][i] });
            }
        }
        if (empty.Count > 0)
            Debug.LogWarning($"[VfxSnapshot] 以下条目为空，已跳过：{string.Join("、", empty)}");
    }

    private static void SetupCamera()
    {
        var pos = new Vector3(5f, 5f, 5f);
        cam = new GameObject("VfxSnapshotCamera").AddComponent<Camera>();
        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-pos, Vector3.up));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f, 1f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.cullingMask = ~0;
        rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
    }

    private static void CreateCurrent()
    {
        Item item = items[cursor];
        current = UnityEngine.Object.Instantiate(item.prefab, Vector3.zero, Quaternion.identity);
        current.name = $"VfxSnapshot_{item.fileName}";
        nextTime = Time.time + Delay;
    }

    private static void Tick()
    {
        if (!Application.isPlaying)
        {
            Abort();
            return;
        }

        string label = inGap ? $"等待下一个（{Gap:F0}s）" : $"{cursor + 1}/{items.Count}  {items[cursor].fileName}";
        if (EditorUtility.DisplayCancelableProgressBar("特效截图", label, (float)cursor / items.Count))
        {
            Abort();
            return;
        }
        if (Time.time < nextTime) return;

        if (inGap)
        {
            inGap = false;
            CreateCurrent();
            return;
        }

        Shoot(items[cursor]);
        cursor++;
        DestroyCurrent(); // 销毁后空出 1s，避免上一个特效的残留粒子串进下一张
        if (cursor >= items.Count)
        {
            Finish();
            return;
        }
        inGap = true;
        nextTime = Time.time + Gap;
    }

    private static void Shoot(Item item)
    {
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(OutputPath(), item.fileName), tex.EncodeToPNG());
        }
        catch (Exception e)
        {
            failed++;
            Debug.LogError($"[VfxSnapshot] {item.fileName} 截图失败：{e}");
        }
    }

    private static void Finish()
    {
        int total = items.Count;
        int ok = total - failed;
        bool test = isTest;
        Teardown();
        AssetDatabase.Refresh();
        if (test) SessionState.SetBool(TestFlagKey, true);
        Debug.Log($"[VfxSnapshot] {(test ? "测试" : "全量")}完成：成功 {ok} / {total}，失败 {failed}，输出 {OutputDir}");
        EditorUtility.DisplayDialog("特效截图",
            $"{(test ? "测试" : "全量")}完成：成功 {ok} / {total}，失败 {failed}。\n\n输出目录：{OutputDir}", "好");
    }

    private static void Abort()
    {
        int done = cursor;
        int total = items.Count;
        Teardown();
        Debug.LogWarning($"[VfxSnapshot] 已中止：完成 {done} / {total}。已生成的文件保留在 {OutputDir}");
    }

    private static void Teardown()
    {
        EditorApplication.update -= Tick;
        DestroyCurrent();
        ReleaseCamera();
        EditorUtility.ClearProgressBar();
        running = false;
    }

    private static void DestroyCurrent()
    {
        if (current != null) UnityEngine.Object.DestroyImmediate(current);
        current = null;
    }

    private static void ReleaseCamera()
    {
        if (cam != null)
        {
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
        }
        cam = null;
        if (rt != null)
        {
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
        rt = null;
        if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
        tex = null;
    }

    private static string OutputPath()
    {
        return Path.Combine(Application.dataPath, "Scenes/VfxTest/graphics");
    }
}
