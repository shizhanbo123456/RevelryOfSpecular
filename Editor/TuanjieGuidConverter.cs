using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 团结引擎 GUID 转换工具
/// 使用方式：将此文件放入 Assets/Editor/ 目录，用团结引擎打开项目，
/// 点击菜单 Tools -> 转换GUID，等待完成后关闭团结引擎，再用标准 Unity 打开
/// </summary>
public class TuanjieGuidConverter
{
    [MenuItem("Tools/转换GUID")]
    private static void ConvertAllGuids()
    {
        var allAssets = AssetDatabase.GetAllAssetPaths();
        int converted = 0;
        int skipped = 0;
        int errors = 0;

        for (int i = 0; i < allAssets.Length; i++)
        {
            string path = allAssets[i];
            
            // 跳过目录和Packages
            if (AssetDatabase.IsValidFolder(path)) continue;
            if (path.EndsWith(".meta")) continue;

            string metaFilePath = path + ".meta";
            if (!File.Exists(metaFilePath))
            {
                // 可能是Library中的文件，跳过
                continue;
            }

            // 团结引擎的 AssetPathToGUID 返回的是标准 hex GUID
            string realGuid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(realGuid))
            {
                errors++;
                continue;
            }

            // 读取 .meta 文件
            string metaContent = File.ReadAllText(metaFilePath);
            string currentGuid = GetGuidFromMeta(metaContent);
            
            if (string.IsNullOrEmpty(currentGuid))
            {
                errors++;
                continue;
            }

            // 如果已经是标准格式（32位hex），跳过
            if (IsStandardGuid(currentGuid) && currentGuid == realGuid)
            {
                skipped++;
                continue;
            }

            // 替换 GUID
            string newContent = metaContent.Replace(currentGuid, realGuid);
            if (newContent != metaContent)
            {
                File.WriteAllText(metaFilePath, newContent);
                converted++;
                
                if (converted % 50 == 0 || i == allAssets.Length - 1)
                {
                    EditorUtility.DisplayProgressBar("转换GUID", 
                        $"已处理: {i + 1}/{allAssets.Length} (已转换: {converted})",
                        (float)(i + 1) / allAssets.Length);
                }
            }
            else
            {
                skipped++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        Debug.Log($"===== GUID 转换完成 =====");
        Debug.Log($"  已转换: {converted}");
        Debug.Log($"  已跳过: {skipped}");
        Debug.Log($"  错误: {errors}");
        
        EditorUtility.DisplayDialog("转换完成", 
            $"GUID 转换完成！\n\n已转换: {converted}\n已跳过(已是标准格式): {skipped}\n错误: {errors}\n\n请关闭团结引擎，用标准 Unity 重新打开项目。",
            "确定");
    }

    private static string GetGuidFromMeta(string metaContent)
    {
        string[] lines = metaContent.Split('\n');
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("guid:"))
            {
                return line.Replace("guid:", "").Trim();
            }
        }
        return string.Empty;
    }

    private static bool IsStandardGuid(string guid)
    {
        if (guid.Length != 32) return false;
        foreach (char c in guid)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }
}
