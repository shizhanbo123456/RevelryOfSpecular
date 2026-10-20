using System.Collections.Generic;
using System.Text;
using Ros.Info;
using UnityEditor;
using UnityEngine;

// 批量生成实体属性 Info（占位资产，数值后续在 Inspector 调；已存在的跳过，可重复执行）
// 数量口径见策划案 10.3 清单；菜单位置：Tools/配置生成/
public static class BatchGenerateEntityInfosMenu
{
    private const string AttrRoot = "Assets/Files/Info/Attribute";

    [MenuItem("Tools/配置/生成实体Info", false, 0)]
    private static void Generate()
    {
        if (!EditorUtility.DisplayDialog(
                "生成实体属性 Info",
                "将在 Assets/Files/Info/Attribute 下批量生成占位 Info：\n" +
                "进攻角色 18 / 防守角色 6 / 僵尸 1 / 精英僵尸 14 / 守护点 2 / 水晶 1 / 防御塔 4 / 瘟疫树 1\n\n" +
                "已存在的同名资产会跳过不覆盖。是否继续？",
                "生成", "取消"))
            return;

        // 缺失的分类文件夹
        EnsureFolder("Beacon");
        EnsureFolder("Tower");
        EnsureFolder("PlagueTree");

        // GiantInferences 空文件夹清理（其中 7 个预制体实为 瘟疫树1/防御塔4/信标2，归属 Entity 侧）
        TryDeleteEmptyFolder("GiantInfections");

        int created = 0, skipped = 0;
        var log = new StringBuilder();

        GenerateRange<PlayerCharacterInfo>("Attacker", "attacker", 18, log, ref created, ref skipped);
        GenerateRange<PlayerCharacterInfo>("Defenser", "defenser", 6, log, ref created, ref skipped);
        GenerateRange<EntityAttributeInfo>("Zombie", "zombie", 1, log, ref created, ref skipped); // 21 种外观共用，强弱走刷新等级
        GenerateRange<EntityAttributeInfo>("EliteZombie", "elite_zombie", 14, log, ref created, ref skipped);

        Create<EntityAttributeInfo>("Beacon", "beacon_outer", log, ref created, ref skipped);  // 外围守护点（BeaconInfo）
        Create<EntityAttributeInfo>("Beacon", "beacon_core", log, ref created, ref skipped);   // 中心守护点（CoreBeaconInfo）
        Create<EntityAttributeInfo>("Crystal", "crystal", log, ref created, ref skipped);
        GenerateRange<EntityAttributeInfo>("Tower", "tower", 4, log, ref created, ref skipped); // 防御塔 4 种，按 type.value 索引
        Create<EntityAttributeInfo>("PlagueTree", "plague_tree", log, ref created, ref skipped);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.Insert(0, $"<b>生成实体属性 Info</b> 新建 {created} 个，跳过已存在 {skipped} 个\n");
        Debug.Log(log.ToString());
    }

    private static void GenerateRange<T>(string folder, string prefix, int count,
        StringBuilder log, ref int created, ref int skipped) where T : EntityAttributeInfo, new()
    {
        for (int i = 0; i < count; i++)
            Create<T>(folder, $"{prefix}_{i:D2}", log, ref created, ref skipped);
    }

    private static void Create<T>(string folder, string name,
        StringBuilder log, ref int created, ref int skipped) where T : EntityAttributeInfo, new()
    {
        string path = $"{AttrRoot}/{folder}/{name}.asset";

        if (AssetDatabase.LoadAssetAtPath<EntityAttributeInfo>(path) != null)
        {
            skipped++;
            log.AppendLine($"[跳过] {path}");
            return;
        }

        var info = ScriptableObject.CreateInstance<T>();
        info.Name = name; // 记录展示名，便于在 InfoManager 列表中核对顺序
        AssetDatabase.CreateAsset(info, path);
        created++;
        log.AppendLine($"[新建] {path}");
    }

    private static void EnsureFolder(string folderName)
    {
        string path = $"{AttrRoot}/{folderName}";
        if (AssetDatabase.IsValidFolder(path)) return;

        if (!AssetDatabase.IsValidFolder(AttrRoot))
        {
            // 逐级确保 Attribute 根存在
            if (!AssetDatabase.IsValidFolder("Assets/Files")) AssetDatabase.CreateFolder("Assets", "Files");
            if (!AssetDatabase.IsValidFolder("Assets/Files/Info")) AssetDatabase.CreateFolder("Assets/Files", "Info");
            AssetDatabase.CreateFolder("Assets/Files/Info", "Attribute");
        }
        AssetDatabase.CreateFolder(AttrRoot, folderName);
    }

    private static void TryDeleteEmptyFolder(string folderName)
    {
        string path = $"{AttrRoot}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path)) return;
        if (AssetDatabase.FindAssets("", new[] { path }).Length > 0) return;

        AssetDatabase.DeleteAsset(path);
        Debug.Log($"[清理] 空文件夹 {path} 已删除（其中预制体归属 Entity 侧的 瘟疫树/防御塔/信标）");
    }
}
