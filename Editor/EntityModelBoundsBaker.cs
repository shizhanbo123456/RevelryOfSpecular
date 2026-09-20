using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 为实体预制体（Assets/Files/Prefabs/Entity 下全部）计算模型 bounds 并写入其 EntityModelInfo。
/// 这些预制体基本都是 Prefab Variant，所以走「实例化 → 改值 → ApplyPrefabInstance 写回变体」；
/// 不用 LoadPrefabContents/SaveAsPrefabAsset，后者会把 Variant 压成普通预制体。
/// 记录的是预制体根节点本地空间的轴对齐包围盒，取网格自身的 bind-pose 盒（不用渲染器实时 bounds，避免蒙皮姿态干扰）。
/// </summary>
public static class EntityModelBoundsBaker
{
    private const string PrefabFolder = "Assets/Files/Prefabs/Entity";
    private const string MenuPathBake = "Tools/实体/计算并录入模型 Bounds";
    private const string MenuPathPreview = "Tools/实体/预览模型 Bounds（不写入）";

    private enum BakeResult
    {
        Baked,
        MissingInfo,
        NoMesh,
    }

    [MenuItem(MenuPathBake, false, 100)]
    private static void BakeAll()
    {
        Run(write: true);
    }

    [MenuItem(MenuPathPreview, false, 101)]
    private static void PreviewAll()
    {
        Run(write: false);
    }

    private static void Run(bool write)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            EditorUtility.DisplayDialog("模型 Bounds", $"找不到目录：{PrefabFolder}", "确定");
            return;
        }

        string[] paths = FindPrefabPaths();
        if (paths.Length == 0)
        {
            EditorUtility.DisplayDialog("模型 Bounds", $"{PrefabFolder} 下没有预制体。", "确定");
            return;
        }

        var report = new StringBuilder();
        var inactiveList = new List<string>();
        int baked = 0, missing = 0, noMesh = 0;

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            EditorUtility.DisplayProgressBar(write ? "计算并录入模型 Bounds" : "预览模型 Bounds",
                path, (float)i / paths.Length);

            switch (Process(path, write, out string line, out bool hasInactiveRenderer))
            {
                case BakeResult.Baked:
                    baked++;
                    report.AppendLine(line);
                    if (hasInactiveRenderer) inactiveList.Add(line);
                    break;
                case BakeResult.MissingInfo:
                    missing++;
                    report.AppendLine($"[跳过·无 EntityModelInfo] {path}");
                    break;
                case BakeResult.NoMesh:
                    noMesh++;
                    report.AppendLine($"[跳过·找不到网格] {path}");
                    break;
            }
        }

        EditorUtility.ClearProgressBar();
        if (write) AssetDatabase.SaveAssets();

        Debug.Log($"[EntityModelBoundsBaker] {(write ? "录入" : "预览（未写入）")} {baked}，" +
                  $"缺少组件 {missing}，无网格 {noMesh}\n{report}");

        if (inactiveList.Count > 0)
        {
            Debug.LogWarning($"[EntityModelBoundsBaker] 以下预制体含未激活的 Renderer，包围盒只统计了激活部分：\n" +
                             string.Join("\n", inactiveList));
        }

        EditorUtility.DisplayDialog("模型 Bounds",
            $"{(write ? "已录入" : "预览")} {baked} 个预制体。\n" +
            $"缺少 EntityModelInfo：{missing}\n没有网格：{noMesh}\n" +
            (inactiveList.Count > 0 ? $"含未激活 Renderer：{inactiveList.Count}（见 Console 警告）\n" : "") +
            "\n逐个数值见 Console。" +
            (write ? "\n\n执行过程会实例化再销毁临时物体，当前场景会被标记为已修改，无需保存。" : ""),
            "确定");
    }

    private static BakeResult Process(string path, bool write, out string line, out bool hasInactiveRenderer)
    {
        line = null;
        hasInactiveRenderer = false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return BakeResult.NoMesh;
        if (prefab.GetComponent<EntityModelInfo>() == null) return BakeResult.MissingInfo;

        // 必须实例化才能安全写回 Variant：改资产本体经 ApplyPrefabInstance 落成变体覆写
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null) return BakeResult.NoMesh;

        try
        {
            if (!TryGetLocalBounds(instance, out Bounds bounds, out hasInactiveRenderer))
                return BakeResult.NoMesh;

            line = $"{Path.GetFileNameWithoutExtension(path),-22} " +
                   $"X[{bounds.min.x,7:F2},{bounds.max.x,7:F2}] " +
                   $"Y[{bounds.min.y,7:F2},{bounds.max.y,7:F2}] " +
                   $"Z[{bounds.min.z,7:F2},{bounds.max.z,7:F2}]";

            if (!write) return BakeResult.Baked;

            var info = instance.GetComponent<EntityModelInfo>();
            info.xRange = new Vector2(bounds.min.x, bounds.max.x);
            info.yRange = new Vector2(bounds.min.y, bounds.max.y);
            info.zRange = new Vector2(bounds.min.z, bounds.max.z);
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            return BakeResult.Baked;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    #region//Local
    private static string[] FindPrefabPaths()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        var paths = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++) paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
        Array.Sort(paths); // 顺序稳定，便于两次结果比对
        return paths;
    }

    /// <summary>汇总全部启用 Renderer 的网格盒到根节点本地空间；禁用的只记标志、不参与计算。</summary>
    private static bool TryGetLocalBounds(GameObject root, out Bounds bounds, out bool hasInactiveRenderer)
    {
        bounds = default;
        hasInactiveRenderer = false;
        bool any = false;
        Matrix4x4 toRoot = root.transform.worldToLocalMatrix;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!IsActiveUnderRoot(renderer.transform, root.transform))
            {
                hasInactiveRenderer = true;
                continue;
            }

            Mesh mesh = GetMesh(renderer);
            if (mesh == null) continue;

            Bounds meshBounds = mesh.bounds;
            Matrix4x4 meshToRoot = toRoot * renderer.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = meshToRoot.MultiplyPoint3x4(GetCorner(meshBounds, i));
                if (any) bounds.Encapsulate(point);
                else
                {
                    bounds = new Bounds(point, Vector3.zero);
                    any = true;
                }
            }
        }

        return any;
    }

    /// <summary>逐级检查到根节点为止的启用状态，但不看根节点自身（预制体根被禁用时仍应算出包围盒）。</summary>
    private static bool IsActiveUnderRoot(Transform target, Transform root)
    {
        for (Transform t = target; t != null && t != root; t = t.parent)
        {
            if (!t.gameObject.activeSelf) return false;
        }
        return true;
    }

    private static Mesh GetMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
        if (renderer is MeshRenderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }
        return null;
    }

    private static Vector3 GetCorner(Bounds bounds, int index)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        return new Vector3(
            (index & 1) == 0 ? min.x : max.x,
            (index & 2) == 0 ? min.y : max.y,
            (index & 4) == 0 ? min.z : max.z);
    }
    #endregion
}
