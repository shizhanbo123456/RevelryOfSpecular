using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 菜单：Tools/材质/批量替换为卡通材质。
// 为每个角色预制体在 Mat/<角色名>/ 下生成卡通材质，并把预制体的材质引用改成新材质（嵌套模型预制体只产生 overrides，不影响共享的模型资产）。
public static class ToonMaterialReplacer
{
    private const string MenuPath = "Tools/材质/批量替换为卡通材质";
    private const string ToonShaderPath = "Assets/Files/Shaders/ToonCharacter.shader";
    private const string MatRoot = "Assets/Files/Shaders/Mat";

    private static readonly string[] PrefabFolders =
    {
        "Assets/Files/Prefabs/Entity/Attacker",
        "Assets/Files/Prefabs/Entity/Defenser",
    };

    private class Stats
    {
        public int prefabs;
        public int created;
        public int reused;
        public int slots;
        public int failedPrefabs;
        public readonly Dictionary<string, int> sourceShaders = new Dictionary<string, int>();
    }

    [MenuItem(MenuPath, false, 3)]
    private static void Replace()
    {
        Shader toonShader = AssetDatabase.LoadAssetAtPath<Shader>(ToonShaderPath);
        if (toonShader == null)
        {
            Debug.LogError($"[卡通材质替换] 找不到 shader：{ToonShaderPath}（路径或文件名是否被改动？）");
            return;
        }

        if (!AssetDatabase.IsValidFolder(MatRoot))
        {
            Debug.LogError($"[卡通材质替换] 材质输出目录不存在：{MatRoot}");
            return;
        }

        List<string> prefabPaths = CollectPrefabs();
        if (prefabPaths.Count == 0)
        {
            Debug.LogError("[卡通材质替换] Attacker / Defenser 目录下没有找到预制体");
            return;
        }

        if (!EditorUtility.DisplayDialog("批量替换为卡通材质",
                $"将处理 {prefabPaths.Count} 个角色预制体：\n" +
                $"在 {MatRoot}/<角色名>/ 下生成卡通材质，并改写预制体里的材质引用。\n\n" +
                "会直接修改预制体资产，不可 Undo，建议先提交 Git。",
                "开始替换", "取消"))
            return;

        var stats = new Stats();
        var processed = new List<string>();
        try
        {
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                string path = prefabPaths[i];
                if (EditorUtility.DisplayCancelableProgressBar("替换角色材质",
                        $"{Path.GetFileNameWithoutExtension(path)} ({i + 1}/{prefabPaths.Count})",
                        (float)i / prefabPaths.Count))
                {
                    Debug.LogWarning($"[卡通材质替换] 用户取消，已处理 {i}/{prefabPaths.Count} 个预制体");
                    break;
                }
                ProcessPrefab(path, toonShader, stats);
                processed.Add(path);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        stats.failedPrefabs = Verify(processed, toonShader);
        Debug.Log(BuildSummary(stats));
    }

    private static List<string> CollectPrefabs()
    {
        var paths = new List<string>();
        foreach (string folder in PrefabFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"[卡通材质替换] 预制体目录不存在：{folder}");
                continue;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }
        paths.Sort(string.CompareOrdinal);
        return paths;
    }

    private static void ProcessPrefab(string prefabPath, Shader toonShader, Stats stats)
    {
        string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
        string matFolder = EnsureMaterialFolder(prefabName);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        int createdBefore = stats.created;
        int replacedSlots = 0;
        try
        {
            var map = new Dictionary<Material, Material>();   // 同一预制体内复用同一份新材质
            var usedPaths = new HashSet<string>();

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials.Length == 0) continue;

                bool changed = false;
                var replaced = new Material[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null)
                    {
                        Debug.LogWarning($"[卡通材质替换] {prefabName} / {GetPath(root.transform, renderer.transform)} 的材质槽 [{i}] 为空，跳过");
                        replaced[i] = null;
                        continue;
                    }
                    if (source.shader == toonShader)
                    {
                        replaced[i] = source;   // 已替换过，保证重复执行不产生副本
                        string sourcePath = AssetDatabase.GetAssetPath(source);
                        if (sourcePath.StartsWith(MatRoot) && !sourcePath.StartsWith(matFolder + "/"))
                            Debug.LogWarning($"[卡通材质替换] {prefabName} / {GetPath(root.transform, renderer.transform)} 槽 [{i}] " +
                                             $"引用的是其它角色的材质 {sourcePath}（可能是变体继承），未复制成本角色专属材质");
                        continue;
                    }
                    replaced[i] = GetOrCreateMaterial(source, toonShader, matFolder, map, usedPaths, stats);
                    changed = true;
                    replacedSlots++;
                }
                if (changed) renderer.sharedMaterials = replaced;
            }
        }
        finally
        {
            if (replacedSlots > 0)
            {
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (saved == null) Debug.LogError($"[卡通材质替换] 保存预制体失败：{prefabPath}");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        stats.prefabs++;
        stats.slots += replacedSlots;
        Debug.Log($"[卡通材质替换] {prefabName}：替换 {replacedSlots} 个槽位，新建 {stats.created - createdBefore} 个材质");
    }

    private static Material GetOrCreateMaterial(Material source, Shader toonShader, string folder,
        Dictionary<Material, Material> map, HashSet<string> usedPaths, Stats stats)
    {
        Material cached;
        if (map.TryGetValue(source, out cached)) return cached;

        string sourceShaderName = source.shader != null ? source.shader.name : "(missing shader)";
        int shaderCount;
        stats.sourceShaders.TryGetValue(sourceShaderName, out shaderCount);
        stats.sourceShaders[sourceShaderName] = shaderCount + 1;

        string path = folder + "/" + SanitizeFileName(source.name) + ".mat";
        if (usedPaths.Contains(path)) path = AssetDatabase.GenerateUniqueAssetPath(path);   // 同一预制体内重名
        usedPaths.Add(path);

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (existing.shader == toonShader)
            {
                map.Add(source, existing);   // 上次运行留下的素材，直接复用，避免覆盖美术调参
                stats.reused++;
                return existing;
            }
            string occupied = path;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            usedPaths.Add(path);
            Debug.LogWarning($"[卡通材质替换] {occupied} 已存在但不是卡通材质，改用新路径 {path}");
        }

        Material created = new Material(toonShader);
        CopyMaterialProperties(source, created);
        ForceOpaqueIfUnsupported(source, created);
        AssetDatabase.CreateAsset(created, path);
        map.Add(source, created);
        stats.created++;
        return created;
    }

    // 只复制目标 shader 声明过的属性，且源材质必须有同名属性
    private static void CopyMaterialProperties(Material source, Material target)
    {
        Shader shader = target.shader;
        int count = shader.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            string propertyName = shader.GetPropertyName(i);
            if (!source.HasProperty(propertyName)) continue;

            switch (shader.GetPropertyType(i))
            {
                case ShaderPropertyType.Texture:
                    Texture texture = source.GetTexture(propertyName);
                    target.SetTexture(propertyName, texture);
                    if (texture != null)
                    {
                        target.SetTextureScale(propertyName, source.GetTextureScale(propertyName));
                        target.SetTextureOffset(propertyName, source.GetTextureOffset(propertyName));
                    }
                    break;
                case ShaderPropertyType.Color:
                    target.SetColor(propertyName, source.GetColor(propertyName));
                    break;
                case ShaderPropertyType.Vector:
                    target.SetVector(propertyName, source.GetVector(propertyName));
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                case ShaderPropertyType.Int:
                    target.SetFloat(propertyName, source.GetFloat(propertyName));
                    break;
            }
        }
    }

    // 卡通 shader 只支持 Opaque/Cutout，Fade/Transparent 会带上透明混合参数但留在不透明队列，必须显式退回
    private static void ForceOpaqueIfUnsupported(Material source, Material target)
    {
        if (!target.HasProperty("_Mode")) return;
        float mode = source.HasProperty("_Mode") ? source.GetFloat("_Mode") : 0f;
        if (mode < 1.5f) return;

        target.SetFloat("_Mode", 0f);
        if (target.HasProperty("_SrcBlend")) target.SetFloat("_SrcBlend", (float)BlendMode.One);
        if (target.HasProperty("_DstBlend")) target.SetFloat("_DstBlend", (float)BlendMode.Zero);
        if (target.HasProperty("_ZWrite")) target.SetFloat("_ZWrite", 1f);
        Debug.LogWarning($"[卡通材质替换] {source.name} 原为 Fade/Transparent，卡通 shader 不支持，已按 Opaque 生成");
    }

    private static string EnsureMaterialFolder(string prefabName)
    {
        string folderName = SanitizeFileName(prefabName);
        string folder = MatRoot + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(MatRoot, folderName);
        return folder;
    }

    // 回读预制体资产校验：仍有非卡通材质的槽位说明 overrides 没写进去
    private static int Verify(List<string> prefabPaths, Shader toonShader)
    {
        var failures = new List<string>();
        foreach (string prefabPath in prefabPaths)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null)
            {
                failures.Add($"{Path.GetFileNameWithoutExtension(prefabPath)}（预制体加载失败）");
                continue;
            }

            int badSlots = 0;
            foreach (Renderer renderer in asset.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && materials[i].shader != toonShader) badSlots++;
            }
            if (badSlots > 0) failures.Add($"{Path.GetFileNameWithoutExtension(prefabPath)}（仍有 {badSlots} 个槽位不是卡通材质）");
        }

        if (failures.Count > 0)
        {
            Debug.LogError($"[卡通材质替换] 校验失败 {failures.Count} 个预制体：\n  " +
                           string.Join("\n  ", failures) +
                           "\n请检查这些预制体是否为不可编辑的 Model Prefab 实例。");
        }
        return failures.Count;
    }

    private static string BuildSummary(Stats stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[卡通材质替换] 完成：预制体 {stats.prefabs} 个，替换槽位 {stats.slots} 个，" +
                      $"新建材质 {stats.created} 个，复用 {stats.reused} 个");
        if (stats.failedPrefabs > 0) sb.AppendLine($"  校验未通过：{stats.failedPrefabs} 个预制体（见上方 LogError）");

        if (stats.sourceShaders.Count > 0)
        {
            sb.AppendLine("  源材质 shader 分布：");
            foreach (KeyValuePair<string, int> pair in stats.sourceShaders)
                sb.AppendLine($"    {pair.Key}：{pair.Value} 个");
        }
        return sb.ToString();
    }

    #region//Local
    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Material";
        char[] invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            bool bad = false;
            for (int j = 0; j < invalid.Length; j++)
                if (c == invalid[j]) { bad = true; break; }
            sb.Append(bad ? '_' : c);
        }
        string result = sb.ToString().Trim();
        return result.Length == 0 ? "Material" : result;
    }

    private static string GetPath(Transform root, Transform target)
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
