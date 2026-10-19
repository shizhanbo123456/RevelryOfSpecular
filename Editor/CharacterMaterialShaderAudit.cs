using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// 菜单：Tools/材质/统计攻守方角色 Shader。只读工具，不改动任何资产。
public static class CharacterMaterialShaderAudit
{
    private const string MenuPath = "Tools/材质/统计攻守方角色 Shader";

    [MenuItem(MenuPath, false, 1)]
    private static void Audit()
    {
        AssetsManager assets = Tool.AssetsManager;
        if (assets == null) assets = Object.FindObjectOfType<AssetsManager>(); // 编辑模式未进 Play 时 Awake 未执行
        if (assets == null)
        {
            Debug.LogError("[角色材质审计] 未找到 AssetsManager：请进入 Play 模式，或确认场景中的 AssetsManager 处于激活状态。");
            return;
        }

        var detail = new StringBuilder("[角色材质审计] 明细（角色 → Renderer → 材质槽 → Shader）\n");
        var summary = new Dictionary<Shader, ShaderStat>();
        int slotCount = 0;
        int errors = 0;

        errors += AuditCamp("进攻方", assets.AttackCharacterGraphics, detail, summary, ref slotCount);
        errors += AuditCamp("防守方", assets.DefenseCharacterGraphics, detail, summary, ref slotCount);

        Debug.Log(detail.ToString());
        Debug.Log(BuildSummary(summary, slotCount, errors));
    }

    private static int AuditCamp(string camp, List<GameObject> graphics, StringBuilder detail,
        Dictionary<Shader, ShaderStat> summary, ref int slotCount)
    {
        if (graphics == null)
        {
            Debug.LogError($"[角色材质审计] AssetsManager.{camp}Graphics 列表为 null");
            return 1;
        }

        int errors = 0;
        detail.AppendLine($"\n==== {camp}：{graphics.Count} 个角色 ====");
        for (int i = 0; i < graphics.Count; i++)
        {
            GameObject graphic = graphics[i];
            if (graphic == null)
            {
                Debug.LogError($"[角色材质审计] {camp} 第 {i} 项为空（资产可能已被删除）");
                errors++;
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(graphic);
            detail.AppendLine($"[{i}] {graphic.name}{(string.IsNullOrEmpty(assetPath) ? string.Empty : "  " + assetPath)}");

            Renderer[] renderers = graphic.GetComponentsInChildren<Renderer>(true);
            int meshRendererCount = 0;
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer)) continue;
                meshRendererCount++;

                string rendererPath = GetRelativePath(graphic.transform, renderer.transform);
                Material[] materials = renderer.sharedMaterials; // shared：编辑模式下读 instance 材质会污染资产
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null)
                    {
                        Debug.LogError($"[角色材质审计] {camp} {graphic.name} / {rendererPath} 的材质槽 [{m}] 为空");
                        errors++;
                        continue;
                    }

                    Shader shader = material.shader;
                    if (shader == null)
                    {
                        Debug.LogError($"[角色材质审计] {camp} {graphic.name} / {rendererPath} 的材质 {material.name} 未找到 Shader");
                        errors++;
                        continue;
                    }

                    slotCount++;
                    detail.AppendLine($"    {rendererPath}  [{m}]  {material.name}  →  {shader.name}");

                    ShaderStat stat;
                    if (!summary.TryGetValue(shader, out stat))
                    {
                        stat = new ShaderStat();
                        summary.Add(shader, stat);
                    }
                    stat.refCount++;
                    stat.materials.Add(material);
                }
            }

            if (meshRendererCount == 0)
            {
                Debug.LogWarning($"[角色材质审计] {camp} 第 {i} 项 {graphic.name} 下没有 SkinnedMeshRenderer / MeshRenderer");
            }
        }
        return errors;
    }

    private static string BuildSummary(Dictionary<Shader, ShaderStat> summary, int slotCount, int errors)
    {
        var entries = new List<KeyValuePair<Shader, ShaderStat>>(summary);
        entries.Sort(CompareByRefCount);

        int materialCount = 0;
        for (int i = 0; i < entries.Count; i++) materialCount += entries[i].Value.materials.Count;

        var sb = new StringBuilder();
        sb.AppendLine($"[角色材质审计] 汇总：材质槽 {slotCount} 个，去重材质 {materialCount} 种，Shader {entries.Count} 种，问题 {errors} 处");
        for (int i = 0; i < entries.Count; i++)
        {
            Shader shader = entries[i].Key;
            ShaderStat stat = entries[i].Value;
            sb.AppendLine($"  {shader.name}  —  {stat.materials.Count} 种材质 / {stat.refCount} 次引用{ShaderAssetPath(shader)}");
        }
        return sb.ToString();
    }

    #region//Local
    private class ShaderStat
    {
        public readonly HashSet<Material> materials = new HashSet<Material>();
        public int refCount;
    }

    private static int CompareByRefCount(KeyValuePair<Shader, ShaderStat> a, KeyValuePair<Shader, ShaderStat> b)
    {
        if (a.Value.refCount != b.Value.refCount) return b.Value.refCount - a.Value.refCount;
        return string.CompareOrdinal(a.Key.name, b.Key.name);
    }

    private static string ShaderAssetPath(Shader shader)
    {
        string path = AssetDatabase.GetAssetPath(shader);
        return string.IsNullOrEmpty(path) ? string.Empty : "  @" + path;
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        var stack = new Stack<string>();
        for (Transform t = target; t != null && t != root; t = t.parent) stack.Push(t.name);
        if (stack.Count == 0) return root.name;

        var sb = new StringBuilder(root.name);
        while (stack.Count > 0) sb.Append('/').Append(stack.Pop());
        return sb.ToString();
    }
    #endregion
}
