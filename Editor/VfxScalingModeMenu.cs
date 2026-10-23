using UnityEditor;
using UnityEngine;

public static class VfxScalingModeMenu
{
    private static readonly string[] Folders =
    {
        "Assets/Files/Prefabs/VFX",
        "Assets/Files/Prefabs/Weapons",
    };

    private const string MenuPath = "Tools/特效/粒子缩放模式全部改为 Hierarchy";

    [MenuItem(MenuPath, false, 30)]
    public static void SetHierarchyAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", Folders);
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("粒子缩放模式", "目标目录下没有预制体。", "知道了");
            return;
        }
        if (!EditorUtility.DisplayDialog("粒子缩放模式",
            $"将扫描 {guids.Length} 个预制体，把所有粒子系统的 Scaling Mode 改为 Hierarchy。\n\n会直接修改资产文件，建议先提交一次。继续？",
            "继续", "取消"))
            return;

        int prefabs = 0, systems = 0, skipped = 0;
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("粒子缩放模式", path, (float)i / guids.Length);

                // 变体 / 模型改缩放模式会把预制体拍平成普通预制体，一律跳过（变体可继承基础预制体的设置）
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.Regular)
                {
                    skipped++;
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    if (main.scalingMode == ParticleSystemScalingMode.Hierarchy) continue;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy; // MainModule 是结构体包装，赋值即写入组件
                    dirty = true;
                    systems++;
                }
                if (dirty)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabs++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[粒子缩放模式] 完成：{prefabs} 个预制体、{systems} 个粒子系统改为 Hierarchy；扫描 {guids.Length} 个，跳过变体/模型 {skipped} 个");
    }

    [MenuItem(MenuPath, true)]
    private static bool SetHierarchyAllValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;
}
