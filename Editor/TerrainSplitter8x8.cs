using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 地形拆分工具：把选中的 Terrain 均分为 8×8 = 64 个子 Terrain。
/// - 高度：原高度图逐块切片（分辨率 = (原分辨率-1)/8+1），相邻块共享边界采样点，无接缝；
/// - 贴图：alphamap 逐块切片（分辨率 = 原/8），TerrainLayer 资产直接引用原资产不复制；
/// - 树：按世界位置归入对应子 Terrain（位置转为子地形内归一化坐标，原型引用同一预制体）；
/// - 草：detail 逐层切片（分辨率 = 原/8），原型配置逐字段克隆；
/// - 原 Terrain 完全不动：本工具只读它，产出全部是新建的 TerrainData 资产与场景对象。
/// 使用：Hierarchy 选中带 Terrain 的物体 → Tools/Terrain/拆分为 8×8 子地形。
/// 产出：Assets/TerrainChunks/<地形名>/ 下 64 个 TerrainData 资产 + 场景中 <地形名>_8x8 父物体（64 个子物体）。
/// </summary>
public static class TerrainSplitter8x8
{
    private const int Grid = 8;

    [MenuItem("Tools/Terrain/拆分为 8×8 子地形")]
    public static void Split()
    {
        var src = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Terrain>() : null;
        if (src == null)
        {
            EditorUtility.DisplayDialog("地形拆分", "请先在 Hierarchy 中选中一个挂有 Terrain 组件的物体。", "确定");
            return;
        }

        TerrainData srcData = src.terrainData;
        int srcHRes = srcData.heightmapResolution;

        // 高度图必须能整切：(res-1) 是 8 的倍数。标准分辨率 513/1025/2049/257/129 均满足。
        if ((srcHRes - 1) % Grid != 0)
        {
            EditorUtility.DisplayDialog("地形拆分",
                $"高度图分辨率 {srcHRes} 无法均分为 {Grid} 份（要求 (分辨率-1) 是 {Grid} 的倍数）。" +
                "标准分辨率（513/1025/2049 等）都满足，请检查该地形的 Set Resolution 设置。", "确定");
            return;
        }
        // 仅允许水平 Y 轴旋转与等比 1 缩放：子地形按平面网格拼合，带倾斜/缩放时拼回的大地形会错位
        if (src.transform.lossyScale != Vector3.one)
        {
            Debug.LogWarning($"[TerrainSplitter] 地形带非 1 缩放（scale={src.transform.lossyScale}），子地形继承父变换后尺寸会与预期不符，请自行确认。");
        }
        Vector3 euler = src.transform.eulerAngles;
        if (Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) > 0.01f || Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f)) > 0.01f)
        {
            Debug.LogWarning($"[TerrainSplitter] 地形带 X/Z 旋转（euler={euler}），子地形网格拼合会倾斜，请自行确认。");
        }

        int chunkHRes = (srcHRes - 1) / Grid + 1;          // 65/129/257…，含共享边界采样点
        Vector3 srcSize = srcData.size;
        Vector3 chunkSize = new Vector3(srcSize.x / Grid, srcSize.y, srcSize.z / Grid);

        // 一次性读出全部源数据（工具内只读，不改原地形）
        float[,] srcHeights = srcData.GetHeights(0, 0, srcHRes, srcHRes);
        float[,,] srcAlphas = null;
        int srcAlphaRes = srcData.alphamapResolution;
        int chunkAlphaRes = srcAlphaRes / Grid;
        bool hasAlphas = srcData.terrainLayers != null && srcData.terrainLayers.Length > 0 && srcAlphaRes > 0;
        if (hasAlphas && srcAlphaRes % Grid != 0)
        {
            Debug.LogWarning($"[TerrainSplitter] alphamap 分辨率 {srcAlphaRes} 不是 {Grid} 的倍数，贴图按最近邻重采样（可能有细微偏差）。");
        }
        if (hasAlphas) srcAlphas = srcData.GetAlphamaps(0, 0, srcAlphaRes, srcAlphaRes);
        if (chunkAlphaRes < 16) chunkAlphaRes = 16; // Unity 最低 alphamap 分辨率兜底

        int srcDetailRes = srcData.detailResolution;
        int chunkDetailRes = Mathf.Max(8, srcDetailRes / Grid);
        int detailLayerCount = srcDetailRes > 0 ? srcData.detailPrototypes.Length : 0;

        // 自检日志：贴图链路出问题时按这条日志定位断点
        Debug.Log($"[TerrainSplitter] 源地形数据: terrainLayers={srcData.terrainLayers?.Length} " +
                  $"alphaRes={srcAlphaRes} baseMapRes={srcData.baseMapResolution} heightRes={srcHRes} " +
                  $"detailRes={srcDetailRes} detailProtos={srcData.detailPrototypes?.Length} " +
                  $"trees={srcData.treeInstances.Length} treeProtos={srcData.treePrototypes?.Length} " +
                  $"material={(src.materialTemplate ? src.materialTemplate.name : "默认")} drawInstanced={src.drawInstanced} " +
                  $"hasAlphas={hasAlphas}");
        if (!hasAlphas)
        {
            EditorUtility.DisplayDialog("地形拆分",
                "源地形没有可用的贴图层（terrainLayers 为空或 alphamap 分辨率为 0），" +
                "子地形会是纯色。请先确认该地形 Paint Texture 里配了层。", "确定");
        }

        // 克隆原型（树/草的配置是类实例，必须逐字段新副本，不能共享实例）；逐块剪枝时按需对单个原型克隆
        TreeInstance[] srcTrees = srcData.treeInstances;

        // 输出目录
        string dir = $"Assets/TerrainChunks/{srcData.name}";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string parentName = $"{srcData.name}_8x8";
        Transform parent = new GameObject(parentName).transform;
        parent.SetPositionAndRotation(src.transform.position, src.transform.rotation);

        try
        {
            for (int gz = 0; gz < Grid; gz++)
            {
                for (int gx = 0; gx < Grid; gx++)
                {
                    EditorUtility.DisplayProgressBar("地形拆分",
                        $"生成子地形 ({gx + 1}/{Grid}, {gz + 1}/{Grid})", (gz * Grid + gx) / (float)(Grid * Grid));

                    TerrainData td = new TerrainData();
                    td.name = $"{srcData.name}_{gx}_{gz}";
                    // 先把资产落壳，之后所有数据写入完成时统一 SetDirty——
                    // 若先写数据再 CreateAsset，序列化可能捕获到控制贴图的旧状态（第2层权重丢失的根源）
                    string assetPath = $"{dir}/{td.name}.asset";
                    AssetDatabase.CreateAsset(td, assetPath);
                    td.heightmapResolution = chunkHRes;
                    // 分辨率必须在写入数据之前显式设定：动态 new 的 TerrainData 若不先定 alphamap 分辨率，
                    // SetAlphamaps 的权重写不进控制贴图，渲染会退化成整块纯色
                    td.alphamapResolution = chunkAlphaRes;
                    td.baseMapResolution = Mathf.Max(16, srcData.baseMapResolution / Grid);
                    td.size = chunkSize;

                    // 高度：直接切片（含共享边界），相邻块采样点一致 → 无接缝、几何与原地形一致
                    float[,] chunkHeights = new float[chunkHRes, chunkHRes];
                    int y0 = gz * (chunkHRes - 1), x0 = gx * (chunkHRes - 1);
                    for (int y = 0; y < chunkHRes; y++)
                        for (int x = 0; x < chunkHRes; x++)
                            chunkHeights[y, x] = srcHeights[y0 + y, x0 + x];
                    td.SetHeights(0, 0, chunkHeights);

                    // 贴图层：切片后逐块剪枝——本块没用到的 TerrainLayer 不进子地形
                    //（内置管线地形 shader 按层数逐像素采样 splat，背着闲置层就是白白多采样）
                    if (hasAlphas)
                    {
                        int layerCount = srcAlphas.GetLength(2);
                        float[,,] chunkAlphas = new float[chunkAlphaRes, chunkAlphaRes, layerCount];
                        for (int y = 0; y < chunkAlphaRes; y++)
                        {
                            // 子块采样点 → 原图索引（整除时即精确切片）
                            int sy = Mathf.Min(srcAlphaRes - 1, Mathf.FloorToInt((gz * chunkAlphaRes + y) * srcAlphaRes / (float)(Grid * chunkAlphaRes)));
                            for (int x = 0; x < chunkAlphaRes; x++)
                            {
                                int sx = Mathf.Min(srcAlphaRes - 1, Mathf.FloorToInt((gx * chunkAlphaRes + x) * srcAlphaRes / (float)(Grid * chunkAlphaRes)));
                                for (int l = 0; l < layerCount; l++)
                                    chunkAlphas[y, x, l] = srcAlphas[sy, sx, l];
                            }
                        }

                        // 统计每层最大权重：低于阈值视为本块未使用（阈值≈控制图一个编码步，肉眼不可辨）
                        const float unusedThreshold = 0.001f;
                        var usedLayers = new List<int>();
                        for (int l = 0; l < layerCount; l++)
                        {
                            float maxWeight = 0f;
                            for (int y = 0; y < chunkAlphaRes; y++)
                                for (int x = 0; x < chunkAlphaRes; x++)
                                    if (chunkAlphas[y, x, l] > maxWeight) maxWeight = chunkAlphas[y, x, l];
                            if (maxWeight > unusedThreshold) usedLayers.Add(l);
                        }
                        if (usedLayers.Count == 0) usedLayers.Add(0); // 整块无权重时保底保留第一层

                        if (usedLayers.Count == layerCount)
                        {
                            td.terrainLayers = srcData.terrainLayers; // 全部用到：整表引用原资产
                            td.SetAlphamaps(0, 0, chunkAlphas);
                        }
                        else
                        {
                            // 剪掉未使用层；被剪层若有微小残余权重，归一化回剩余层，保证权重和仍为 1
                            var prunedLayers = new TerrainLayer[usedLayers.Count];
                            var prunedAlphas = new float[chunkAlphaRes, chunkAlphaRes, usedLayers.Count];
                            for (int y = 0; y < chunkAlphaRes; y++)
                            {
                                for (int x = 0; x < chunkAlphaRes; x++)
                                {
                                    float sum = 0f;
                                    for (int k = 0; k < usedLayers.Count; k++)
                                    {
                                        float w = chunkAlphas[y, x, usedLayers[k]];
                                        prunedAlphas[y, x, k] = w;
                                        sum += w;
                                    }
                                    if (sum > 0.0001f)
                                        for (int k = 0; k < usedLayers.Count; k++) prunedAlphas[y, x, k] /= sum;
                                }
                            }
                            for (int k = 0; k < usedLayers.Count; k++) prunedLayers[k] = srcData.terrainLayers[usedLayers[k]];
                            td.terrainLayers = prunedLayers;
                            td.SetAlphamaps(0, 0, prunedAlphas);
                        }

                        // 自检：第 (0,0) 块读回控制贴图验证写入是否真的生效
                        if (gx == 0 && gz == 0)
                        {
                            var readBack = td.GetAlphamaps(0, 0, chunkAlphaRes, chunkAlphaRes);
                            var sb = new System.Text.StringBuilder($"[TerrainSplitter] (0,0)块自检: 层数={td.terrainLayers.Length} alphaRes={td.alphamapResolution} 各层最大权重=");
                            for (int l = 0; l < readBack.GetLength(2); l++)
                            {
                                float maxW = 0f;
                                for (int y = 0; y < chunkAlphaRes; y++)
                                    for (int x = 0; x < chunkAlphaRes; x++)
                                        if (readBack[y, x, l] > maxW) maxW = readBack[y, x, l];
                                sb.Append($" L{l}={maxW:F3}");
                            }
                            Debug.Log(sb.ToString());
                        }
                    }

                    // 草：逐层切片后剪枝——本块完全没长草的原型不进子地形
                    if (detailLayerCount > 0)
                    {
                        td.SetDetailResolution(chunkDetailRes, srcData.detailResolutionPerPatch);
                        var slicedDetails = new int[detailLayerCount][,];
                        var usedDetails = new List<int>();
                        for (int l = 0; l < detailLayerCount; l++)
                        {
                            int[,] srcDetail = srcData.GetDetailLayer(0, 0, srcDetailRes, srcDetailRes, l);
                            int[,] chunkDetail = new int[chunkDetailRes, chunkDetailRes];
                            int maxDensity = 0;
                            for (int y = 0; y < chunkDetailRes; y++)
                            {
                                int sy = Mathf.Min(srcDetailRes - 1, Mathf.FloorToInt((gz * chunkDetailRes + y) * srcDetailRes / (float)(Grid * chunkDetailRes)));
                                for (int x = 0; x < chunkDetailRes; x++)
                                {
                                    int sx = Mathf.Min(srcDetailRes - 1, Mathf.FloorToInt((gx * chunkDetailRes + x) * srcDetailRes / (float)(Grid * chunkDetailRes)));
                                    int v = srcDetail[sy, sx];
                                    chunkDetail[y, x] = v;
                                    if (v > maxDensity) maxDensity = v;
                                }
                            }
                            slicedDetails[l] = chunkDetail;
                            if (maxDensity > 0) usedDetails.Add(l);
                        }

                        var prunedDetailProtos = new DetailPrototype[usedDetails.Count];
                        for (int k = 0; k < usedDetails.Count; k++)
                        {
                            prunedDetailProtos[k] = CloneDetailPrototype(srcData.detailPrototypes[usedDetails[k]]);
                        }
                        td.detailPrototypes = prunedDetailProtos; // 先定原型表，再按新下标写入草层数据
                        for (int k = 0; k < usedDetails.Count; k++)
                        {
                            td.SetDetailLayer(0, 0, k, slicedDetails[usedDetails[k]]);
                        }
                    }

                    // 树：按世界位置归入本块；再剪掉本块没用到/无树时的原型（prototypeIndex 重映射）
                    var chunkTrees = new List<TreeInstance>();
                    if (srcData.treePrototypes.Length > 0 && srcTrees.Length > 0)
                    {
                        foreach (var t in srcTrees)
                        {
                            Vector3 p = new Vector3(t.position.x * srcSize.x, 0f, t.position.z * srcSize.z);
                            float minX = gx * chunkSize.x, minZ = gz * chunkSize.z;
                            bool inX = p.x >= minX && (gx == Grid - 1 ? p.x <= srcSize.x : p.x < minX + chunkSize.x);
                            bool inZ = p.z >= minZ && (gz == Grid - 1 ? p.z <= srcSize.z : p.z < minZ + chunkSize.z);
                            if (!inX || !inZ) continue;
                            var ti = t;
                            ti.position = new Vector3((p.x - minX) / chunkSize.x, t.position.y, (p.z - minZ) / chunkSize.z);
                            chunkTrees.Add(ti);
                        }
                    }
                    if (chunkTrees.Count > 0)
                    {
                        var protoMap = new Dictionary<int, int>();
                        var prunedTreeProtos = new List<TreePrototype>();
                        for (int i = 0; i < chunkTrees.Count; i++)
                        {
                            var ti = chunkTrees[i];
                            if (!protoMap.TryGetValue(ti.prototypeIndex, out int newIndex))
                            {
                                newIndex = prunedTreeProtos.Count;
                                protoMap[ti.prototypeIndex] = newIndex;
                                prunedTreeProtos.Add(CloneTreePrototype(srcData.treePrototypes[ti.prototypeIndex]));
                            }
                            ti.prototypeIndex = newIndex;
                            chunkTrees[i] = ti;
                        }
                        td.treePrototypes = prunedTreeProtos.ToArray();
                        td.treeInstances = chunkTrees.ToArray();
                    }
                    else
                    {
                        td.treePrototypes = new TreePrototype[0]; // 无树块不挂任何原型
                    }

                    // 地形参数：与原地形保持一致
                    td.wavingGrassStrength = srcData.wavingGrassStrength;
                    td.wavingGrassSpeed = srcData.wavingGrassSpeed;
                    td.wavingGrassAmount = srcData.wavingGrassAmount;
                    td.wavingGrassTint = srcData.wavingGrassTint;
                    // thickness 已被 Unity 标记为 obsolete（物理引擎不再使用），不再复制

                    EditorUtility.SetDirty(td); // 数据写完统一标脏：SaveAssets 时才把最终权重序列化进资产

                    var go = new GameObject(td.name);
                    go.transform.SetParent(parent, false);
                    // 每块按网格坐标偏移：块 (gx,gz) 的世界起点 = 地形原点 + (gx, gz) × 子块边长
                    go.transform.localPosition = new Vector3(gx * chunkSize.x, 0f, gz * chunkSize.z);
                    var terrain = go.AddComponent<Terrain>();
                    terrain.terrainData = td;
                    // Terrain 组件参数逐一复刻原地形
                    terrain.materialTemplate = src.materialTemplate;
                    terrain.drawInstanced = src.drawInstanced;
                    terrain.heightmapPixelError = src.heightmapPixelError; // 高度图像素误差（Inspector 里的 Pixel Error）
                    terrain.basemapDistance = src.basemapDistance;         // Base Map Distance（非 instanced 路径的远处 basemap 切换距离）
                    terrain.heightmapMaximumLOD = src.heightmapMaximumLOD;
                    terrain.treeBillboardDistance = src.treeBillboardDistance;
                    terrain.treeCrossFadeLength = src.treeCrossFadeLength;
                    terrain.treeMaximumFullLODCount = src.treeMaximumFullLODCount;
                    terrain.detailObjectDistance = src.detailObjectDistance;
                    terrain.detailObjectDensity = src.detailObjectDensity;
                    var col = go.AddComponent<TerrainCollider>();
                    col.terrainData = td;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 自检第二轮：落盘并强制重导入后，直接读控制贴图像素——渲染器读的就是这份序列化数据
            LogSavedControlTexture($"{dir}/{srcData.name}_0_0.asset", "子块(0,0)");
            LogSavedControlTexture(AssetDatabase.GetAssetPath(srcData), "源地形");

            Selection.activeGameObject = parent.gameObject;
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "Split Terrain 8x8");

            int totalTrees = srcTrees.Length;
            Debug.Log($"[TerrainSplitter] 完成：{dir} 下生成 {Grid * Grid} 个 TerrainData 资产，" +
                      $"每块高度图 {chunkHRes}²、alphamap {chunkAlphaRes}²、detail {chunkDetailRes}²，树共 {totalTrees} 棵已按位置分配。" +
                      "原 Terrain 未做任何修改，确认无误后可自行禁用。");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TerrainData>($"{dir}/{srcData.name}_0_0.asset"));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>读取某个 TerrainData 资产的控制贴图像素并打印各通道最大值（对照诊断用）。</summary>
    private static void LogSavedControlTexture(string assetPath, string label)
    {
        if (string.IsNullOrEmpty(assetPath)) return;
        var td = AssetDatabase.LoadAssetAtPath<TerrainData>(assetPath);
        if (td == null)
        {
            Debug.LogWarning($"[TerrainSplitter] {label}: 无法加载 {assetPath}");
            return;
        }
        var texs = td.alphamapTextures;
        var sb = new System.Text.StringBuilder(
            $"[TerrainSplitter] {label} 落盘自检: alphaRes={td.alphamapResolution} layers={td.terrainLayers.Length} 控制贴图={(texs == null ? "null" : texs.Length.ToString() + "张")}");
        try
        {
            if (texs != null && texs.Length > 0)
            {
                var t0 = texs[0];
                sb.Append($" 尺寸={t0.width}x{t0.height}");
                var pix = t0.GetPixels();
                for (int c = 0; c < 4; c++)
                {
                    float mx = 0f;
                    foreach (var p in pix) mx = Mathf.Max(mx, p[c]);
                    sb.Append($" ch{c}max={mx:F3}");
                }
            }

            // 权重数据读回：与控制贴图对照——若权重在而贴图通道为 0，则是控制贴图未重建
            if (td.alphamapResolution > 0 && td.terrainLayers.Length > 0)
            {
                var maps = td.GetAlphamaps(0, 0, td.alphamapResolution, td.alphamapResolution);
                for (int l = 0; l < maps.GetLength(2); l++)
                {
                    float mx = 0f;
                    for (int y = 0; y < maps.GetLength(0); y++)
                        for (int x = 0; x < maps.GetLength(1); x++)
                            if (maps[y, x, l] > mx) mx = maps[y, x, l];
                    sb.Append($" 权重L{l}max={mx:F3}");
                }
            }
        }
        catch (System.Exception e)
        {
            sb.Append($" 读取失败: {e.Message}");
        }
        Debug.Log(sb.ToString());
    }

    private static TreePrototype CloneTreePrototype(TreePrototype src)
    {
        // billboard / allowSelfIntersection 无公开 API（本 Unity 版本），子地形用默认值（Billboard 默认开启）
        return new TreePrototype { prefab = src.prefab, bendFactor = src.bendFactor };
    }

    private static DetailPrototype CloneDetailPrototype(DetailPrototype s)
    {
        return new DetailPrototype
        {
            prototype = s.prototype,
            prototypeTexture = s.prototypeTexture,
            usePrototypeMesh = s.usePrototypeMesh,
            renderMode = s.renderMode,
            healthyColor = s.healthyColor,
            dryColor = s.dryColor,
            minWidth = s.minWidth,
            maxWidth = s.maxWidth,
            minHeight = s.minHeight,
            maxHeight = s.maxHeight,
            noiseSeed = s.noiseSeed,
            // positionRandomness 在本 Unity 版本无公开 API，用默认值
        };
    }
}
