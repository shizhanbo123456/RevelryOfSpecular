using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 批量旋转子物体编辑器工具。
/// 对所有选中的物体（场景实例或 Project 窗口中的 .prefab 资产）的每个"直接子物体"，
/// 绕其自身局部 X/Y/Z 轴旋转 +90° 或 -90°（共 6 个菜单项）。
/// 菜单位置：Tools/批量旋转子物体/
/// </summary>
public static class BatchRotateChildrenMenu
{
    private const string MenuRoot = "Tools/批量旋转子物体";

    // ---------------- 6 个菜单项 ----------------

    [MenuItem(MenuRoot + "/绕 X 轴 +90°", false, 0)]
    private static void RotateXPlus90() => Apply(Quaternion.Euler(90, 0, 0));

    [MenuItem(MenuRoot + "/绕 X 轴 -90°", false, 1)]
    private static void RotateXMinus90() => Apply(Quaternion.Euler(-90, 0, 0));

    [MenuItem(MenuRoot + "/绕 Y 轴 +90°", false, 2)]
    private static void RotateYPlus90() => Apply(Quaternion.Euler(0, 90, 0));

    [MenuItem(MenuRoot + "/绕 Y 轴 -90°", false, 3)]
    private static void RotateYMinus90() => Apply(Quaternion.Euler(0, -90, 0));

    [MenuItem(MenuRoot + "/绕 Z 轴 +90°", false, 4)]
    private static void RotateZPlus90() => Apply(Quaternion.Euler(0, 0, 90));

    [MenuItem(MenuRoot + "/绕 Z 轴 -90°", false, 5)]
    private static void RotateZMinus90() => Apply(Quaternion.Euler(0, 0, -90));

    // ---------------- 菜单可用性校验 ----------------

    [MenuItem(MenuRoot + "/绕 X 轴 +90°", true), MenuItem(MenuRoot + "/绕 X 轴 -90°", true)]
    [MenuItem(MenuRoot + "/绕 Y 轴 +90°", true), MenuItem(MenuRoot + "/绕 Y 轴 -90°", true)]
    [MenuItem(MenuRoot + "/绕 Z 轴 +90°", true), MenuItem(MenuRoot + "/绕 Z 轴 -90°", true)]
    private static bool Validate() => HasValidSelection();

    // ---------------- 核心逻辑 ----------------

    private static void Apply(Quaternion rot)
    {
        int changedCount = 0;
        var log = new StringBuilder();

        // 1) 场景中选中的实例（带 Undo，可 Ctrl+Z 撤销）
        var sceneParents = new List<GameObject>();
        foreach (var go in Selection.gameObjects)
        {
            if (go == null || EditorUtility.IsPersistent(go)) continue; // 持久化对象走资流程
            sceneParents.Add(go);
        }

        if (sceneParents.Count > 0)
        {
            foreach (var parent in sceneParents)
            {
                var children = CollectDirectChildren(parent.transform);
                if (children.Count == 0) continue;

                Undo.RecordObjects(children.ToArray(), "Batch Rotate Children");

                foreach (var t in children)
                {
                    t.localRotation = rot * t.localRotation; // 绕自身局部轴：左乘
                    changedCount++;
                }

                log.AppendLine($"[场景] {parent.name}: 旋转了 {children.Count} 个直接子物体");
            }

            EditorSceneManager.MarkAllScenesDirty();
        }

        // 2) Project 窗口中选中的 .prefab 资产（直接修改资产文件，不可 Undo）
        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var children = CollectDirectChildren(root.transform);
                foreach (var t in children)
                    t.localRotation = rot * t.localRotation;

                if (children.Count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedCount += children.Count;
                    log.AppendLine($"[资产] {System.IO.Path.GetFileName(path)}: 旋转了 {children.Count} 个直接子物体");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (changedCount > 0)
        {
            Debug.Log($"<b>批量旋转子物体</b> 共旋转 {changedCount} 个子物体\n{log}", GetFirstContext());
        }
        else
        {
            Debug.LogWarning("批量旋转子物体：选中的物体没有直接子物体，或未选中有效目标。");
        }
    }

    private static List<Transform> CollectDirectChildren(Transform parent)
    {
        var result = new List<Transform>();
        foreach (Transform child in parent)
            result.Add(child);
        return result;
    }

    private static bool HasValidSelection()
    {
        foreach (var go in Selection.gameObjects)
        {
            if (go != null && go.transform.childCount > 0)
                return true;
        }

        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null && asset.transform.childCount > 0)
                return true;
        }

        return false;
    }

    private static Object GetFirstContext()
    {
        foreach (var go in Selection.gameObjects)
            if (go != null) return go;
        return null;
    }
}
