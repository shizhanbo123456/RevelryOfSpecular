using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 批量调整子物体位置：对每个选中的物体（场景实例或 .prefab 资产），
/// 计算其所有子物体 mesh 的合并世界包围盒，然后把所有直接子物体
/// 按相同的位移量整体平移，使包围盒中心对齐到物体自身位置（根节点不动）。
/// 所有子物体共享同一位移，拼合模型不会发生部件错位。
/// 菜单位置：Tools/批量调整子物体位置/
/// </summary>
public static class BatchAlignChildrenPivotMenu
{
    private const string MenuPath = "Tools/批量调整子物体位置/包围盒中心对齐到物体位置";

    [MenuItem(MenuPath, false, 0)]
    private static void AlignBoundsCenterToPivot()
    {
        int changedCount = 0;
        var log = new StringBuilder();

        // 1) 场景中选中的实例（带 Undo，可 Ctrl+Z 撤销）
        foreach (var go in Selection.gameObjects)
        {
            if (go == null || EditorUtility.IsPersistent(go)) continue;
            TryAlign(go, log, ref changedCount, "场景");
        }

        if (changedCount > 0)
            EditorSceneManager.MarkAllScenesDirty();

        // 2) Project 窗口中选中的 .prefab 资产（直接修改资产文件，不可 Undo）
        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                string name = root.name;
                if (TryComputeBoundsDelta(root, out Vector3 delta))
                {
                    var children = CollectDirectChildren(root.transform);
                    foreach (var t in children)
                        t.position += delta;

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedCount += children.Count;
                    log.AppendLine($"[资产] {name}: 位移 {delta:F4}，移动 {children.Count} 个直接子物体");
                }
                else
                {
                    log.AppendLine($"[资产] {name}: 跳过（子物体中没有任何 mesh）");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (changedCount > 0)
            Debug.Log($"<b>包围盒中心对齐物体位置</b> 共调整 {changedCount} 个直接子物体\n{log}");
        else
            Debug.LogWarning("包围盒中心对齐物体位置：没有需要调整的目标（选中物体没有带 mesh 的子物体）。");
    }

    [MenuItem(MenuPath, true)]
    private static bool Validate()
    {
        foreach (var go in Selection.gameObjects)
        {
            if (go != null && go.transform.childCount > 0)
                return true;
        }

        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) &&
                path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>对单个场景物体执行对齐。返回是否实际发生了位移。</summary>
    private static bool TryAlign(GameObject root, StringBuilder log, ref int changedCount, string tag)
    {
        if (!TryComputeBoundsDelta(root, out Vector3 delta))
        {
            log.AppendLine($"[{tag}] {root.name}: 跳过（子物体中没有任何 mesh）");
            return false;
        }

        if (delta.sqrMagnitude < 1e-10f)
        {
            log.AppendLine($"[{tag}] {root.name}: 跳过（包围盒中心已对齐物体位置）");
            return true; // 无需位移
        }

        var children = CollectDirectChildren(root.transform);

        // Undo 记录必须在修改之前
        Undo.RecordObjects(children.ToArray(), "Align Children Bounds Center To Pivot");

        foreach (var t in children)
            t.position += delta;

        changedCount += children.Count;
        log.AppendLine($"[{tag}] {root.name}: 位移 {delta:F4}，移动 {children.Count} 个直接子物体");
        return true;
    }

    /// <summary>
    /// 计算使子物体合并世界包围盒中心对齐到根物体位置所需的位移量。
    /// 仅统计"子物体"上的 mesh（根物体自身挂的 mesh 不计入）。
    /// </summary>
    private static bool TryComputeBoundsDelta(GameObject root, out Vector3 delta)
    {
        delta = Vector3.zero;

        if (!TryGetChildrenWorldBounds(root, out Bounds bounds))
            return false;

        delta = root.transform.position - bounds.center;
        return true;
    }

    /// <summary>合并根物体所有子孙节点上 mesh 的世界包围盒（根自身除外）。</summary>
    private static bool TryGetChildrenWorldBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool has = false;

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.transform == root.transform) continue;
            if (mf.sharedMesh == null) continue;
            EncapsulateMesh(ref bounds, ref has, mf.sharedMesh, mf.transform.localToWorldMatrix);
        }

        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.transform == root.transform) continue;
            if (smr.sharedMesh == null) continue;
            EncapsulateMesh(ref bounds, ref has, smr.sharedMesh, smr.transform.localToWorldMatrix);
        }

        return has;
    }

    /// <summary>把 mesh 的局部 bounds 经矩阵变换到世界空间（8 个角点逐一 encapsulate，支持旋转/缩放）。</summary>
    private static void EncapsulateMesh(ref Bounds acc, ref bool has, Mesh mesh, Matrix4x4 l2w)
    {
        Bounds local = mesh.bounds;
        Vector3 c = local.center;
        Vector3 e = local.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = l2w.MultiplyPoint3x4(c + new Vector3(e.x * x, e.y * y, e.z * z));
                    if (!has)
                    {
                        acc = new Bounds(corner, Vector3.zero);
                        has = true;
                    }
                    else
                    {
                        acc.Encapsulate(corner);
                    }
                }
            }
        }
    }

    private static List<Transform> CollectDirectChildren(Transform parent)
    {
        var result = new List<Transform>();
        foreach (Transform child in parent)
            result.Add(child);
        return result;
    }
}
