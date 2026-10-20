using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapSnapshotMenu
{
    private const string OutputAssetPath = "Assets/Files/Map/Map.png";
    private const float RegionSize = 1000f; // 截图范围：世界 x/z 各 1000 米，从 (0,0) 起
    private const int PixelSize = 4096;
    private const float CaptureHeight = 1000f;

    [MenuItem("Tools/截图/生成地图截图")]
    public static void Capture()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Files/Map"));

        var go = new GameObject("MapSnapshotCamera");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = RegionSize * 0.5f;
        cam.aspect = 1f;
        cam.transform.SetPositionAndRotation(
            new Vector3(RegionSize * 0.5f, CaptureHeight, RegionSize * 0.5f),
            Quaternion.Euler(90f, 0f, 0f)); // 朝向正下方，画面上方 = 世界 +Z
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = CaptureHeight * 2f;
        cam.cullingMask = ~0;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        var rt = new RenderTexture(PixelSize, PixelSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var tex = new Texture2D(PixelSize, PixelSize, TextureFormat.RGBA32, false);
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, PixelSize, PixelSize), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "Files/Map/Map.png"), tex.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[MapSnapshot] 已输出 {OutputAssetPath}（{PixelSize}×{PixelSize}，区域 {RegionSize}×{RegionSize} 米）");
    }
}
