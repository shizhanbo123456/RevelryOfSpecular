using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneMeshStatsMenu
{
    private const string MenuPath = "Tools/场景/统计激活模型面数";
    private const int DetailLimit = 100;

    private struct Entry
    {
        public Renderer Renderer;
        public string Path;
        public int Triangles;
        public bool Skinned;
    }

    [MenuItem(MenuPath, false, 210)]
    private static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("模型面数", "当前没有有效场景。", "确定");
            return;
        }

        var entries = new List<Entry>();
        int total = 0, skinnedTotal = 0, staticTotal = 0, skippedNoMesh = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(false))
            {
                bool skinned = renderer is SkinnedMeshRenderer;
                if (!skinned && renderer is not MeshRenderer) continue;
                if (!renderer.enabled) continue;

                Mesh mesh = GetMesh(renderer, skinned);
                if (mesh == null)
                {
                    skippedNoMesh++;
                    continue;
                }

                int triangles = CountTriangles(mesh);
                if (triangles <= 0) continue;

                entries.Add(new Entry
                {
                    Renderer = renderer,
                    Path = GetPath(renderer.transform),
                    Triangles = triangles,
                    Skinned = skinned,
                });
                total += triangles;
                if (skinned) skinnedTotal += triangles;
                else staticTotal += triangles;
            }
        }

        if (entries.Count == 0)
        {
            EditorUtility.DisplayDialog("模型面数", $"当前场景「{scene.name}」没有激活的模型。", "确定");
            return;
        }

        entries.Sort((a, b) => b.Triangles.CompareTo(a.Triangles));

        var summary = new StringBuilder();
        summary.AppendLine($"[模型面数] 场景：{scene.name}");
        summary.AppendLine($"激活模型：{entries.Count} 个，三角面合计：{total:N0}（静态 {staticTotal:N0} / 蒙皮 {skinnedTotal:N0}）");
        if (skippedNoMesh > 0) summary.AppendLine($"跳过无 Mesh 的 Renderer：{skippedNoMesh} 个");
        summary.AppendLine($"按面数降序明细（共 {entries.Count} 项，列出前 {Mathf.Min(entries.Count, DetailLimit)} 项）");
        Debug.Log(summary.ToString());

        int limit = Mathf.Min(entries.Count, DetailLimit);
        for (int i = 0; i < limit; i++)
        {
            Entry entry = entries[i];
            string tag = entry.Skinned ? "蒙皮" : "静态";
            Debug.Log($"{i + 1}. [{tag}] {entry.Triangles:N0} 面\t{entry.Path}", entry.Renderer);
        }
        if (entries.Count > limit)
            Debug.Log($"[模型面数] 其余 {entries.Count - limit} 项已省略。");

        EditorUtility.DisplayDialog("模型面数",
            $"场景：{scene.name}\n激活模型：{entries.Count} 个\n三角面合计：{total:N0}\n" +
            $"静态网格：{staticTotal:N0}\n蒙皮网格：{skinnedTotal:N0}\n\n" +
            $"明细已输出到 Console（前 {limit} 项，按面数降序）。", "确定");
    }

    private static Mesh GetMesh(Renderer renderer, bool skinned)
    {
        if (skinned) return ((SkinnedMeshRenderer)renderer).sharedMesh;
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        return filter != null ? filter.sharedMesh : null;
    }

    private static int CountTriangles(Mesh mesh)
    {
        int triangles = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            if (mesh.GetTopology(i) != MeshTopology.Triangles) continue;
            triangles += (int)(mesh.GetIndexCount(i) / 3);
        }
        return triangles;
    }

    private static string GetPath(Transform transform)
    {
        var names = new List<string>();
        for (Transform t = transform; t != null; t = t.parent) names.Add(t.name);
        names.Reverse();
        return string.Join("/", names);
    }
}
