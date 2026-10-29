public static class Landscape
{
    public const float chunkSize = 40f;

    // 地形边长取配表 terrain_size，区块数随之推出（加载期已校验 terrain_size 是 chunkSize 的整数倍）
    public static float MapSize => ConfigManager.GlobalVariableConfig.terrain_size;

    public static int chunkCount => (int)(MapSize / chunkSize);

    public static UnityEngine.Vector3 MapCenter => new UnityEngine.Vector3(MapSize * 0.5f, 0f, MapSize * 0.5f);

    public static bool InMap(UnityEngine.Vector3 pos)
    {
        return pos.x >= 0f && pos.z >= 0f && pos.x < MapSize && pos.z < MapSize;
    }
}
