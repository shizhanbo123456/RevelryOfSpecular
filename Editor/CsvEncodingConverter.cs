using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// 配表编码工具：检查当前编码 / 统一转成 UTF-8（带 BOM）；ConfigManager 优先按 UTF-8 读
public static class CsvEncodingConverter
{
    private const string ConfigDir = "Assets/StreamingAssets/ConfigData";
    private const string Utf8BomLabel = "UTF-8 (BOM)";

    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Encoding StrictUtf16Le = new UnicodeEncoding(false, false, true);
    private static readonly Encoding StrictUtf16Be = new UnicodeEncoding(true, false, true);
    private static readonly Encoding Utf8Bom = new UTF8Encoding(true);

    [MenuItem("Tools/配表编码/检查当前编码")]
    private static void CheckEncoding()
    {
        string[] files = GetCsvFiles();
        if (files == null) return;

        int unknown = 0;
        foreach (string path in files)
        {
            if (TryDecode(File.ReadAllBytes(path), out _, out string label))
                Debug.Log($"[配表编码] {Path.GetFileName(path)}：{label}");
            else
            {
                Debug.LogError($"[配表编码] {Path.GetFileName(path)}：无法识别（既不是 UTF-8，也不是 GBK/GB18030）");
                unknown++;
            }
        }
        Debug.Log($"[配表编码] 检查完成：共 {files.Length} 个，无法识别 {unknown} 个");
    }

    [MenuItem("Tools/配表编码/全部转 UTF-8（BOM）")]
    private static void ConvertToUtf8()
    {
        string[] files = GetCsvFiles();
        if (files == null) return;

        if (!EditorUtility.DisplayDialog("配表编码转换",
                $"{files.Length} 个 csv 统一转为 UTF-8（带 BOM），原文件会被覆盖。", "转换", "取消")) return;

        int converted = 0, skipped = 0, failed = 0;
        try
        {
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                EditorUtility.DisplayProgressBar("配表编码转换", Path.GetFileName(path), (float)i / files.Length);

                if (!TryDecode(File.ReadAllBytes(path), out string text, out string label))
                {
                    Debug.LogError($"[配表编码] {path} 无法识别：既不是 UTF-8，也不是 GBK/GB18030");
                    failed++;
                    continue;
                }
                if (label == Utf8BomLabel)
                {
                    skipped++;
                    continue;
                }

                File.WriteAllText(path, text, Utf8Bom);
                Debug.Log($"[配表编码] {Path.GetFileName(path)}：{label} → {Utf8BomLabel}");
                converted++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[配表编码] 完成：转换 {converted}，已是 UTF-8（BOM）{skipped}，失败 {failed}");
        EditorUtility.DisplayDialog("配表编码转换",
            $"转换 {converted} 个\n已是 UTF-8（BOM）{skipped} 个\n失败 {failed} 个\n\n详见 Console。", "确定");
    }

    private static string[] GetCsvFiles()
    {
        if (!Directory.Exists(ConfigDir))
        {
            Debug.LogError($"[配表编码] 目录不存在：{ConfigDir}");
            return null;
        }

        string[] files = Directory.GetFiles(ConfigDir, "*.csv", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            Debug.LogWarning($"[配表编码] {ConfigDir} 下没有 csv 文件");
            return null;
        }
        return files;
    }

    // 依次尝试 BOM → 严格 UTF-8 → GB18030/GBK；全部失败返回 false，不做替换字符兜底
    private static bool TryDecode(byte[] bytes, out string text, out string label)
    {
        if (HasBom(bytes, 0xEF, 0xBB, 0xBF) && TryDecode(StrictUtf8, bytes, 3, out text))
        {
            label = Utf8BomLabel;
            return true;
        }
        if (HasBom(bytes, 0xFF, 0xFE) && TryDecode(StrictUtf16Le, bytes, 2, out text))
        {
            label = "UTF-16 LE";
            return true;
        }
        if (HasBom(bytes, 0xFE, 0xFF) && TryDecode(StrictUtf16Be, bytes, 2, out text))
        {
            label = "UTF-16 BE";
            return true;
        }
        if (TryDecode(StrictUtf8, bytes, 0, out text))
        {
            label = "UTF-8";
            return true;
        }

        // 54936 = GB18030（GBK 超集），936 = GBK
        int[] codePages = { 54936, 936 };
        foreach (int codePage in codePages)
        {
            Encoding encoding = GetStrict(codePage);
            if (encoding == null)
            {
                Debug.LogWarning($"[配表编码] 当前运行时不支持代码页 {codePage}，跳过");
                continue;
            }
            if (TryDecode(encoding, bytes, 0, out text))
            {
                label = $"CP{codePage}";
                return true;
            }
        }

        text = null;
        label = null;
        return false;
    }

    private static bool TryDecode(Encoding encoding, byte[] bytes, int offset, out string text)
    {
        try
        {
            text = encoding.GetString(bytes, offset, bytes.Length - offset);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = null;
            return false;
        }
    }

    private static Encoding GetStrict(int codePage)
    {
        try
        {
            return Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }
        catch (Exception e) when (e is NotSupportedException || e is ArgumentException)
        {
            return null;
        }
    }

    private static bool HasBom(byte[] bytes, params byte[] bom)
    {
        if (bytes.Length < bom.Length) return false;
        for (int i = 0; i < bom.Length; i++)
        {
            if (bytes[i] != bom[i]) return false;
        }
        return true;
    }
}
