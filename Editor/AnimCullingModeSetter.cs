using UnityEditor;
using UnityEngine;

public static class AnimCullingModeSetter
{
    private const string MenuPath = "Tools/动画/选中物体 Animator 统一配置（不剔除 + 关根运动）";

    [MenuItem(MenuPath, false, 100)]
    private static void ConfigureAnimators()
    {
        var targets = Selection.gameObjects;
        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("[AnimatorSetup] 请先在 Hierarchy / Project 中选中挂 EntityAnim 的物体");
            return;
        }

        int totalAnimators = 0, changedAnimators = 0, emptyObjects = 0;
        bool hasAssetChanges = false;

        foreach (var go in targets)
        {
            if (go == null) continue;
            int before = totalAnimators;
            CollectAndApply(go, ref totalAnimators, ref changedAnimators, ref hasAssetChanges);
            if (totalAnimators == before)
            {
                emptyObjects++;
                Debug.LogWarning($"[AnimatorSetup] {go.name} 上未找到任何 Animator（收集范围：本体 + 直接子物体）");
            }
        }

        if (hasAssetChanges) AssetDatabase.SaveAssets();

        Debug.Log($"[AnimatorSetup] 处理完成：选中 {targets.Length} 个物体，" +
                  $"共 {totalAnimators} 个 Animator，其中 {changedAnimators} 个被修改" +
                  (emptyObjects > 0 ? $"；{emptyObjects} 个物体上没有 Animator" : ""));
    }

    private static void CollectAndApply(GameObject go, ref int total, ref int changed, ref bool hasAssetChanges)
    {
        bool isAsset = EditorUtility.IsPersistent(go);

        if (go.TryGetComponent<Animator>(out var animator)) Apply(animator, isAsset, ref total, ref changed, ref hasAssetChanges);
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i);
            if (child.TryGetComponent<Animator>(out var childAnimator)) Apply(childAnimator, isAsset, ref total, ref changed, ref hasAssetChanges);
        }
    }

    private static void Apply(Animator animator, bool isAsset, ref int total, ref int changed, ref bool hasAssetChanges)
    {
        total++;
        if (animator.cullingMode == AnimatorCullingMode.AlwaysAnimate && !animator.applyRootMotion) return;

        if (isAsset)
        {
            hasAssetChanges = true; // 预制体资产：标脏，最后统一 SaveAssets
        }
        else
        {
            Undo.RecordObject(animator, "Setup Entity Animator"); // 场景物体：走 Undo
        }

        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.applyRootMotion = false;

        // 嵌套预制体（模型来自 FBX 实例）上的修改必须显式登记为实例覆盖，否则不会被保存进预制体
        if (PrefabUtility.IsPartOfPrefabInstance(animator))
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

        EditorUtility.SetDirty(animator);
        changed++;
    }
}
