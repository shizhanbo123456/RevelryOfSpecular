public static class Landscape
{
    public const float chunkSize = 40f;

    public const int chunkCount = 32;

    public static float MapSize => chunkSize * chunkCount;

    public static UnityEngine.Vector3 MapCenter => new UnityEngine.Vector3(MapSize * 0.5f, 0f, MapSize * 0.5f);

    public static bool InMap(UnityEngine.Vector3 pos)
    {
        return pos.x >= 0f && pos.z >= 0f && pos.x < MapSize && pos.z < MapSize;
    }
}
