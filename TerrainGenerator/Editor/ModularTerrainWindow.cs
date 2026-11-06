using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TerrainGen.EditorTools
{
    public class ModularTerrainWindow : EditorWindow
    {
        const string ConfigFolder = "Assets/TerrainGenerator/Config";

        BuildingSettings settings;
        BuildingKitSet kit;
        BuildingField field;
        bool dirty;
        bool showSettings = true;
        Texture2D preview;
        Vector2 scroll;
        string summary;
        bool summaryIsError;
        readonly List<string> report = new List<string>();

        [MenuItem("Tools/模块化地形/建筑生成器")]
        public static void Open()
        {
            var window = GetWindow<ModularTerrainWindow>("建筑生成器");
            window.minSize = new Vector2(420f, 400f);
            if (window.position.width < 460f || window.position.height < 520f)
                window.position = new Rect(window.position.x, window.position.y, 480f, 680f);
        }

        void OnEnable()
        {
            settings = AssetDatabase.LoadAssetAtPath<BuildingSettings>(ConfigFolder + "/BuildingSettings.asset");
            kit = AssetDatabase.LoadAssetAtPath<BuildingKitSet>(ConfigFolder + "/BuildingKitSet.asset");
        }

        void OnDisable() => DestroyImmediate(preview);

        void OnGUI()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                settings = (BuildingSettings)EditorGUILayout.ObjectField("生成参数", settings, typeof(BuildingSettings), false);
                if (GUILayout.Button("创建配置", GUILayout.Width(70))) CreateAssets();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                kit = (BuildingKitSet)EditorGUILayout.ObjectField("素材表", kit, typeof(BuildingKitSet), false);
                if (GUILayout.Button("新建素材表", GUILayout.Width(80))) CreateKit();
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("点“创建配置”新建一份建筑生成参数，或把已有资产拖进来。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("预览布局", GUILayout.Height(24))) Rebuild();
                if (GUILayout.Button("生成到场景", GUILayout.Height(24))) GenerateIntoScene();
                if (GUILayout.Button("清空场景", GUILayout.Height(24))) ClearScene();
                if (GUILayout.Button("随机种子", GUILayout.Height(24), GUILayout.Width(72))) SetRandomSeed();
            }

            if (!string.IsNullOrEmpty(summary))
                EditorGUILayout.HelpBox(summary, summaryIsError ? MessageType.Error : MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));

            showSettings = EditorGUILayout.Foldout(showSettings, "生成参数", true);
            if (showSettings)
            {
                var so = new SerializedObject(settings);
                so.Update();
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.name == "m_Script") continue;
                    EditorGUILayout.PropertyField(it, true);
                }
                if (so.ApplyModifiedProperties()) dirty = true;
            }

            if (preview != null)
            {
                EditorGUILayout.Space(4);
                Rect rect = GUILayoutUtility.GetRect(260f, 260f, GUILayout.ExpandWidth(false));
                EditorGUI.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color(0.35f, 0.35f, 0.35f));
                EditorGUI.DrawPreviewTexture(rect, preview, null, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("输出", EditorStyles.boldLabel);
            for (int i = 0; i < report.Count; i++)
                EditorGUILayout.LabelField(report[i], EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.EndScrollView();
        }

        void Log(string msg)
        {
            if (report.Count >= 300) report.RemoveRange(0, 150);
            report.Add(msg);
        }

        void SetRandomSeed()
        {
            var so = new SerializedObject(settings);
            so.Update();
            so.FindProperty("seed").intValue = Random.Range(1, int.MaxValue);
            so.ApplyModifiedProperties();
            dirty = true;
        }

        void Rebuild()
        {
            report.Clear();
            field = new BuildingField();
            field.Generate(settings);
            dirty = false;
            BuildPreviewTexture();

            int sumW = 0;
            int sumH = 0;
            foreach (RectInt room in field.Rooms)
            {
                sumW += room.width;
                sumH += room.height;
            }
            float avgW = field.Rooms.Count > 0 ? sumW * settings.cellSize / field.Rooms.Count : 0f;
            float avgH = field.Rooms.Count > 0 ? sumH * settings.cellSize / field.Rooms.Count : 0f;

            int guards = 0;
            int extras = 0;
            int towers = 0;
            foreach (MapPoint p in field.Points)
            {
                if (p.Kind == PointKind.Extra) extras++;
                else if (p.Kind == PointKind.Tower) towers++;
                else guards++;
            }

            summary = $"布局已生成：{field.Rooms.Count} 个房间（平均 {avgW:0}×{avgH:0} 单位），地板 {field.FloorCellCount()} 格，"
                      + $"点位 {field.Points.Count} 个（守护 {guards} / 普通 {extras} / 防御塔 {towers}）。";
            summaryIsError = field.FloorCellCount() == 0;
            Log(summary);
        }

        void BuildPreviewTexture()
        {
            DestroyImmediate(preview);
            preview = new Texture2D(field.SizeX, field.SizeZ, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            var roomColors = new Color[field.Rooms.Count];
            var rng = new System.Random(settings.seed);
            for (int i = 0; i < roomColors.Length; i++)
                roomColors[i] = Color.HSVToRGB((float)rng.NextDouble(), 0.45f, 0.88f);

            for (int x = 0; x < field.SizeX; x++)
            {
                for (int z = 0; z < field.SizeZ; z++)
                {
                    int region = field.Region[x, z];
                    Color c;
                    if (field.Layer[x, z] < 0) c = new Color(0.80f, 0.80f, 0.82f);
                    else if (region < 0) c = new Color(0.55f, 0.53f, 0.50f);
                    else c = roomColors[region % roomColors.Length];
                    preview.SetPixel(x, z, c);
                }
            }
            foreach (MapPoint p in field.Points)
            {
                Color c;
                switch (p.Kind)
                {
                    case PointKind.MainGuard: c = new Color(1f, 0.2f, 0.1f); break;
                    case PointKind.SubGuard: c = new Color(1f, 0.62f, 0.05f); break;
                    case PointKind.Tower: c = new Color(0.25f, 0.85f, 0.3f); break;
                    default: c = new Color(0.1f, 0.7f, 1f); break;
                }
                for (int dx = 0; dx < 2; dx++)
                    for (int dz = 0; dz < 2; dz++)
                    {
                        int px = p.Cell.x + dx;
                        int pz = p.Cell.y + dz;
                        if (px < 0 || pz < 0 || px >= field.SizeX || pz >= field.SizeZ) continue;
                        preview.SetPixel(px, pz, c);
                    }
            }

            preview.Apply();
        }

        void GenerateIntoScene()
        {
            report.Clear();
            if (field == null || dirty || field.SizeX != settings.mapCellsX || field.SizeZ != settings.mapCellsZ) Rebuild();

            GameObject existing = GameObject.Find(TerrainAssembler.RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            TerrainBuildResult result = TerrainAssembler.Build(field, settings, kit, null);
            if (!result.Success)
            {
                summary = "生成失败：" + result.error;
                summaryIsError = true;
                Log(result.error);
                return;
            }

            Undo.RegisterCreatedObjectUndo(result.root, "生成建筑");
            Selection.activeGameObject = result.root;
            summary = $"生成完成：{field.Rooms.Count} 个房间，{result.decor} 个预制体，{result.points} 个点位。";
            summaryIsError = false;
            Log(summary);
        }

        void ClearScene()
        {
            report.Clear();
            GameObject root = GameObject.Find(TerrainAssembler.RootName);
            if (root == null)
            {
                summary = "场景里没有 TerrainRoot。";
                summaryIsError = false;
                return;
            }
            Undo.DestroyObjectImmediate(root);
            summary = "已清空 TerrainRoot。";
            summaryIsError = false;
        }

        void CreateKit()
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/TerrainGenerator")) AssetDatabase.CreateFolder("Assets", "TerrainGenerator");
                AssetDatabase.CreateFolder("Assets/TerrainGenerator", "Config");
            }

            string path = ConfigFolder + "/BuildingKitSet.asset";
            kit = AssetDatabase.LoadAssetAtPath<BuildingKitSet>(path);
            if (kit != null)
            {
                summary = "素材表已存在，未覆盖。";
                summaryIsError = false;
                return;
            }

            kit = CreateInstance<BuildingKitSet>();
            AssetDatabase.CreateAsset(kit, path);
            AssetDatabase.SaveAssets();
            summary = "已新建空素材表，请把地面 / 围墙 / 石桩预制体拖进三个列表。";
            summaryIsError = false;
        }

        void CreateAssets()
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/TerrainGenerator")) AssetDatabase.CreateFolder("Assets", "TerrainGenerator");
                AssetDatabase.CreateFolder("Assets/TerrainGenerator", "Config");
            }
            settings = AssetDatabase.LoadAssetAtPath<BuildingSettings>(ConfigFolder + "/BuildingSettings.asset");
            if (settings == null)
            {
                settings = CreateInstance<BuildingSettings>();
                AssetDatabase.CreateAsset(settings, ConfigFolder + "/BuildingSettings.asset");
                AssetDatabase.SaveAssets();
            }
            summary = "生成参数已就绪：" + ConfigFolder + "/BuildingSettings.asset";
            summaryIsError = false;
        }
    }
}
