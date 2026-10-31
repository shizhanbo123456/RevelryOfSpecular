using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EntityHeadshotMenu
{
    private const string MenuPath = "Tools/实体/生成角色头像（头部照）";
    private const int Size = 256;
    private const float FrameRatio = 0.25f;   // 取景边长 = 包围盒高 × 此值，中心取 yRange.y
    private const float CameraGap = 6f;       // 相机距离 = 取景边长 × 此值

    // 临时实例挪到远处，避免场景物件入镜
    private static readonly Vector3 Buffer = new Vector3(10000f, 10000f, 10000f);

    private static readonly string[] PrefabDirs =
    {
        "Assets/Files/Prefabs/Entity/Attacker",
        "Assets/Files/Prefabs/Entity/Defenser",
    };

    private static readonly string[] IconDirs =
    {
        "Assets/Files/UI/Icons/Attacker",
        "Assets/Files/UI/Icons/Defenser",
    };

    [MenuItem(MenuPath, false, 110)]
    private static void CaptureAll()
    {
        int total = 0;
        foreach (string dir in PrefabDirs) total += FindPrefabs(dir).Length;
        if (total == 0)
        {
            EditorUtility.DisplayDialog("角色头像", "两个预制体目录下都没有预制体。", "确定");
            return;
        }

        var disabledLights = DisableSceneLights();
        var cam = CreateCamera();
        var lights = CreateLights();
        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var written = new List<string>();
        var skipped = new List<string>();

        try
        {
            int index = 0;
            bool cancelled = false;
            for (int g = 0; g < PrefabDirs.Length && !cancelled; g++)
            {
                string iconDir = IconDirs[g];
                if (!AssetDatabase.IsValidFolder(iconDir))
                {
                    Debug.LogError($"[角色头像] 输出目录不存在：{iconDir}");
                    skipped.Add(iconDir);
                    continue;
                }

                foreach (string prefabPath in FindPrefabs(PrefabDirs[g]))
                {
                    index++;
                    if (EditorUtility.DisplayCancelableProgressBar("角色头像",
                            $"{index}/{total}  {Path.GetFileName(prefabPath)}", (float)(index - 1) / total))
                    {
                        cancelled = true;
                        break;
                    }

                    string iconPath = $"{iconDir}/{Path.GetFileNameWithoutExtension(prefabPath)}.png";
                    if (Capture(prefabPath, iconPath, cam, rt, tex)) written.Add(iconPath);
                    else skipped.Add(prefabPath);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            cam.targetTexture = null;
            Object.DestroyImmediate(cam.gameObject);
            foreach (var light in lights) Object.DestroyImmediate(light.gameObject);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            RestoreSceneLights(disabledLights);
        }

        if (written.Count > 0)
        {
            AssetDatabase.Refresh();
            foreach (string path in written) ApplySpriteImport(path);
        }

        Debug.Log($"[角色头像] 成功 {written.Count}，跳过 {skipped.Count}\n" +
                  string.Join("\n", written) +
                  (skipped.Count > 0 ? "\n跳过：\n" + string.Join("\n", skipped) : ""));

        EditorUtility.DisplayDialog("角色头像",
            $"生成 {written.Count} 张，跳过 {skipped.Count}。\n" +
            (skipped.Count > 0 ? "跳过原因见 Console。" : "全部成功。") +
            "\n\n输出：Icons/Attacker、Icons/Defenser。",
            "确定");
    }

    private static bool Capture(string prefabPath, string iconPath, Camera cam, RenderTexture rt, Texture2D tex)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[角色头像] 读不到预制体：{prefabPath}");
            return false;
        }

        var instance = (GameObject)Object.Instantiate(prefab);
        try
        {
            instance.transform.position = Buffer;
            var info = instance.GetComponentInChildren<EntityModelInfo>(true);
            if (info == null)
            {
                Debug.LogError($"[角色头像] {prefab.name} 没挂 EntityModelInfo：取不到头部高度");
                return false;
            }

            float height = info.yRange.y - info.yRange.x;
            if (height <= 0f)
            {
                Debug.LogError($"[角色头像] {prefab.name} 的 yRange 未录入或已退化（{height}）：先跑 Tools/实体/预处理模型Bounds");
                return false;
            }

            float side = height * FrameRatio;
            // 水平方向取实体根位置（本地 x/z = 0），包围盒偏心会让角色偏出画面中心
            Vector3 center = info.transform.TransformPoint(new Vector3(0f, info.yRange.y, 0f));

            // 角色面朝 -X：相机从 -X 侧平视取景中心
            cam.transform.SetPositionAndRotation(center - Vector3.right * (side * CameraGap),
                Quaternion.LookRotation(Vector3.right, Vector3.up));
            cam.orthographicSize = side * 0.5f;
            cam.nearClipPlane = side * CameraGap * 0.5f;
            cam.farClipPlane = side * CameraGap * 1.5f;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            File.WriteAllBytes(AbsolutePath(iconPath), tex.EncodeToPNG());
            return true;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Camera CreateCamera()
    {
        var cam = new GameObject("HeadshotCamera").AddComponent<Camera>();
        cam.enabled = false;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = ~0;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        return cam;
    }

    private static Light[] CreateLights()
    {
        return new[]
        {
            NewLight("Key", new Vector3(1f, -0.85f, 0.5f), 1.0f),
            NewLight("Fill", new Vector3(1f, -0.25f, -0.9f), 0.45f),
            NewLight("Rim", new Vector3(-0.8f, -0.2f, 0.6f), 0.35f),
        };
    }

    private static Light NewLight(string name, Vector3 direction, float intensity)
    {
        var light = new GameObject($"HeadshotLight{name}").AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.None;
        light.intensity = intensity;
        light.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return light;
    }

    private static List<Light> DisableSceneLights()
    {
        var disabled = new List<Light>();
        foreach (var light in Object.FindObjectsOfType<Light>())
        {
            if (!light.enabled) continue;
            light.enabled = false; // 场景平行光会叠加到临时打光上，出图亮度会随昼夜漂移
            disabled.Add(light);
        }
        return disabled;
    }

    private static void RestoreSceneLights(List<Light> lights)
    {
        foreach (var light in lights)
            if (light != null) light.enabled = true;
    }

    private static void ApplySpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[角色头像] 取不到导入器：{path}（图片已写入，导入设置需手动调）");
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = Size;
        importer.SaveAndReimport();
    }

    private static string[] FindPrefabs(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return Array.Empty<string>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        var paths = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++) paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
        Array.Sort(paths);
        return paths;
    }

    private static string AbsolutePath(string assetPath)
    {
        return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
    }
}
