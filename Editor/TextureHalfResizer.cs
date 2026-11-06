using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 贴图减半工具：把 Project 窗口选中的目录下所有 png/jpg 文件重采样为边长一半并覆盖原文件
public static class TextureHalfResizer
{
    [MenuItem("Tools/贴图尺寸减半")]
    private static void Run()
    {
        List<string> dirs = GetSelectedDirs();
        if (dirs.Count == 0)
        {
            Debug.LogError("[贴图减半] 请先在 Project 窗口选中至少一个目录");
            return;
        }

        List<string> files = new List<string>();
        foreach (string dir in dirs)
            files.AddRange(Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories));
        files.RemoveAll(p =>
        {
            string ext = Path.GetExtension(p).ToLower();
            return ext != ".png" && ext != ".jpg" && ext != ".jpeg";
        });
        if (files.Count == 0)
        {
            Debug.Log("[贴图减半] 选中目录下没有 png/jpg 文件");
            return;
        }

        string dirNames = string.Join(", ", dirs.ConvertAll(Path.GetFileName));
        if (!EditorUtility.DisplayDialog("贴图尺寸减半",
                $"目录 [{dirNames}] 下共 {files.Count} 个 png/jpg，将重采样为宽高各一半并覆盖原文件（不可恢复）。", "执行", "取消")) return;

        int done = 0, failed = 0;
        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                EditorUtility.DisplayProgressBar("贴图尺寸减半", Path.GetFileName(path), (float)i / files.Count);

                if (TryHalve(path)) done++;
                else failed++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[贴图减半] 完成：成功 {done} 个，失败 {failed} 个");
    }

    private static List<string> GetSelectedDirs()
    {
        List<string> dirs = new List<string>();
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path))
                dirs.Add(path);
        }
        return dirs;
    }

    private static bool TryHalve(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!src.LoadImage(bytes))
        {
            Object.DestroyImmediate(src);
            Debug.LogError($"[贴图减半] {path} 解码失败，已跳过");
            return false;
        }

        int nw = src.width / 2, nh = src.height / 2;
        if (nw < 1 || nh < 1)
        {
            Object.DestroyImmediate(src);
            Debug.LogError($"[贴图减半] {path} 尺寸 {src.width}x{src.height} 过小无法减半，已跳过");
            return false;
        }

        Color32[] sp = src.GetPixels32();
        Color32[] dp = new Color32[nw * nh];
        for (int y = 0; y < nh; y++)
        {
            int y0 = y * 2 * src.width, y1 = y0 + src.width;
            for (int x = 0; x < nw; x++)
            {
                int x0 = x * 2, x1 = x0 + 1;
                dp[y * nw + x] = Avg(sp[y0 + x0], sp[y0 + x1], sp[y1 + x0], sp[y1 + x1]);
            }
        }
        Object.DestroyImmediate(src);

        Texture2D dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        dst.SetPixels32(dp);
        dst.Apply();

        string ext = Path.GetExtension(path).ToLower();
        byte[] outBytes = (ext == ".png") ? dst.EncodeToPNG() : dst.EncodeToJPG(90);
        Object.DestroyImmediate(dst);

        File.WriteAllBytes(path, outBytes);
        return true;
    }

    private static Color32 Avg(Color32 a, Color32 b, Color32 c, Color32 d)
    {
        return new Color32(
            (byte)((a.r + b.r + c.r + d.r) >> 2),
            (byte)((a.g + b.g + c.g + d.g) >> 2),
            (byte)((a.b + b.b + c.b + d.b) >> 2),
            (byte)((a.a + b.a + c.a + d.a) >> 2));
    }
}
