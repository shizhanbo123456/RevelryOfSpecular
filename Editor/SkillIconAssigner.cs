using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SkillIconAssigner
{
    private const string MenuPath = "Tools/技能/按分配表为 SkillInfo 录入图标";
    private const string SkillInfoRoot = "Assets/Files/Info/Skill";
    private const string IconFolder = "Assets/Packages/UI/Magic Skill Icons Vol 2";

    private static readonly Dictionary<int, int> IconMap = new()
    {
        // Common 0~49
        [0] = 5, [1] = 8, [2] = 44, [3] = 37, [4] = 46, [5] = 58, [6] = 88, [7] = 40, [8] = 24, [9] = 39,
        [10] = 87, [11] = 7, [12] = 36, [13] = 50, [14] = 15, [15] = 53, [16] = 60, [17] = 78, [18] = 52,
        [19] = 45, [20] = 57, [21] = 75, [22] = 76, [23] = 82, [24] = 30, [25] = 19, [26] = 35, [27] = 69,
        [28] = 55, [29] = 3, [30] = 79, [31] = 56, [32] = 10, [33] = 65,
        [34] = 29, [35] = 86, [36] = 13, [37] = 43, [38] = 48, [39] = 84, [40] = 4, [41] = 23, [42] = 25,
        [43] = 34, [44] = 26, [45] = 68, [46] = 6, [47] = 77, [48] = 63, [49] = 32,
        // Defenser 50~72
        [50] = 9, [51] = 51, [52] = 83, [54] = 12, [55] = 66, [56] = 80, [58] = 72, [59] = 28, [60] = 85,
        [62] = 42, [63] = 67, [64] = 16, [66] = 74, [67] = 20, [68] = 11, [70] = 14, [71] = 70, [72] = 27,
        // Others 100~182（101/102、120/121、180/181、103/122 共用图标）
        [101] = 38, [102] = 38, [103] = 22,
        [120] = 71, [121] = 71, [122] = 22,
        [140] = 47, [160] = 81,
        [180] = 17, [181] = 17, [182] = 31,
    };

    [MenuItem(MenuPath, false, 100)]
    private static void AssignAll()
    {
        var guids = AssetDatabase.FindAssets("t:SkillInfo", new[] { SkillInfoRoot });
        int assigned = 0, unchanged = 0;
        var unmapped = new StringBuilder();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var info = AssetDatabase.LoadAssetAtPath<SkillInfo>(path);
            if (info == null) continue;

            if (!IconMap.TryGetValue(info.id, out int iconIndex))
            {
                unmapped.Append($"\n  {info.id} {info.name}（{path}）");
                continue;
            }

            var sprite = LoadIconSprite(iconIndex);
            if (sprite == null)
            {
                Debug.LogError($"[SkillIconAssigner] 图标加载失败：{IconPath(iconIndex)}（skill {info.id}）");
                continue;
            }

            if (info.icon == sprite)
            {
                unchanged++;
                continue;
            }

            info.icon = sprite;
            EditorUtility.SetDirty(info);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var report = $"[SkillIconAssigner] 完成：新录入 {assigned}，本已一致 {unchanged}。";
        if (unmapped.Length > 0)
            report += $"\n分配表中没有的技能 id（未处理）：{unmapped}";
        Debug.Log(report);
    }

    private static string IconPath(int index) => $"{IconFolder}/icon_{index}.png";

    private static Sprite LoadIconSprite(int index)
    {
        var path = IconPath(index);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        // 纹理未导入为 Sprite 时自动修正一次导入设置
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.Sprite) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
