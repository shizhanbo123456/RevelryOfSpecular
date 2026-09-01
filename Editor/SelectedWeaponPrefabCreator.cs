using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 将当前在 Hierarchy 中选中的武器模型包装成便于编辑的预制体。
/// 生成结构：PrefabRoot（仅 Transform） -> WeaponModel（唯一直接子物体）。
/// </summary>
public static class SelectedWeaponPrefabCreator
{
    private const string OutputFolder = "Assets/Files/Prefabs/Weapons";
    private const string MenuPath = "Tools/武器/将选中场景物体创建为武器预制体";

    [MenuItem(MenuPath, false, 100)]
    private static void CreatePrefabs()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        var sceneObjects = new List<GameObject>();

        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject == null || EditorUtility.IsPersistent(selectedObject))
                continue;

            if (!selectedObject.scene.IsValid())
                continue;

            sceneObjects.Add(selectedObject);
        }

        if (sceneObjects.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "创建武器预制体",
                "请先在 Hierarchy 中选择至少一个场景武器物体。\nProject 窗口中的模型资源不会被直接处理。",
                "确定");
            return;
        }

        EnsureOutputFolderExists();

        int createdCount = 0;
        var createdPrefabs = new List<Object>();
        var messages = new List<string>();

        foreach (GameObject source in sceneObjects)
        {
            string prefabName = SanitizeFileName(source.name);
            if (string.IsNullOrWhiteSpace(prefabName))
                prefabName = "Weapon";

            string requestedPath = $"{OutputFolder}/{prefabName}.prefab";
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(requestedPath);
            GameObject root = null;

            try
            {
                root = new GameObject(prefabName);

                // 实例化整个场景物体，保留模型资源引用、组件、材质、子层级及当前缩放。
                GameObject weaponModel = Object.Instantiate(source);
                weaponModel.name = source.name;
                weaponModel.transform.SetParent(root.transform, false);
                weaponModel.transform.localPosition = Vector3.zero;
                weaponModel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                weaponModel.transform.localScale = source.transform.localScale;

                bool centered = CenterVisualBoundsOnRoot(root.transform, weaponModel.transform);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                if (prefab == null)
                    throw new InvalidOperationException($"保存预制体失败：{prefabPath}");

                createdCount++;
                createdPrefabs.Add(prefab);

                string note = centered ? string.Empty : "（未找到 Renderer，未执行模型居中）";
                messages.Add($"{source.name} -> {prefabPath}{note}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[武器预制体工具] 处理 {source.name} 失败：{exception}", source);
                messages.Add($"{source.name} -> 创建失败，详见 Console");
            }
            finally
            {
                if (root != null)
                    Object.DestroyImmediate(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (createdPrefabs.Count > 0)
        {
            Selection.objects = createdPrefabs.ToArray();
            EditorGUIUtility.PingObject(createdPrefabs[0]);
        }

        string summary = $"已创建 {createdCount}/{sceneObjects.Count} 个武器预制体。\n\n" +
                         string.Join("\n", messages);
        Debug.Log("[武器预制体工具]\n" + summary);
        EditorUtility.DisplayDialog("创建武器预制体", summary, "确定");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateCreatePrefabs()
    {
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject != null &&
                !EditorUtility.IsPersistent(selectedObject) &&
                selectedObject.scene.IsValid())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 在应用子物体 90 度旋转后计算所有可视 Renderer 的世界包围盒，
    /// 再移动唯一子物体，使包围盒中心与预制体根节点原点重合。
    /// </summary>
    private static bool CenterVisualBoundsOnRoot(Transform root, Transform weaponModel)
    {
        Renderer[] renderers = weaponModel.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        bool hasBounds = false;
        Bounds combinedBounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return false;

        Vector3 worldOffset = root.position - combinedBounds.center;
        weaponModel.localPosition += root.InverseTransformVector(worldOffset);
        return true;
    }

    private static string SanitizeFileName(string fileName)
    {
        string result = fileName.Trim();
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            result = result.Replace(invalidCharacter, '_');

        return result;
    }

    private static void EnsureOutputFolderExists()
    {
        string[] folders = OutputFolder.Split('/');
        string currentPath = folders[0];

        for (int index = 1; index < folders.Length; index++)
        {
            string nextPath = $"{currentPath}/{folders[index]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, folders[index]);

            currentPath = nextPath;
        }
    }
}
