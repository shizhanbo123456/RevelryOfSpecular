/// <summary>
/// 地图基础信息（静态配置）。
/// ChunkSearcher 依赖本类的区块划分参数。
/// </summary>
public static class Landscape
{
    /// <summary>单个区块边长（米）。</summary>
    public const float chunkSize = 40f;

    /// <summary>每边区块数量。地图尺寸 = chunkSize * chunkCount。</summary>
    public const int chunkCount = 32;

    /// <summary>地图边长（米）。</summary>
    public static float MapSize => chunkSize * chunkCount;

    /// <summary>地图中心点（XZ 平面）。</summary>
    public static UnityEngine.Vector3 MapCenter => new UnityEngine.Vector3(MapSize * 0.5f, 0f, MapSize * 0.5f);

    /// <summary>判断坐标是否在地图范围内。</summary>
    public static bool InMap(UnityEngine.Vector3 pos)
    {
        return pos.x >= 0f && pos.z >= 0f && pos.x < MapSize && pos.z < MapSize;
    }
}
