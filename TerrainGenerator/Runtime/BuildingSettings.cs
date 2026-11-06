using UnityEngine;

namespace TerrainGen
{
    [CreateAssetMenu(menuName = "模块化地形/建筑生成参数", fileName = "BuildingSettings")]
    public class BuildingSettings : ScriptableObject
    {
        [Header("网格")]
        [Tooltip("格子边长，应与地面预制体的长宽一致")]
        [Min(1f)] public float cellSize = 6f;
        [Tooltip("地面预制体的高度，围墙/石桩/点位都放在这个高度上")]
        [Min(0f)] public float groundHeight = 4f;
        [Min(4)] public int mapCellsX = 64;
        [Min(4)] public int mapCellsZ = 64;

        [Header("房间划分")]
        public int seed = 20261105;
        [Tooltip("房间最大边长（格），地图越大房间自动越多")]
        [Min(4)] public int maxRoomCells = 12;
        [Tooltip("单个房间最少几格")]
        [Min(2)] public int minRoomCells = 4;
        [Tooltip("房间离切分边界留几格，留 2 格以上走廊才走得开")]
        [Min(0)] public int roomMargin = 2;
        [Tooltip("走廊最大宽度（格），每条走廊在 1~该值之间随机")]
        [Min(1)] public int corridorMaxWidth = 3;
        [Tooltip("每个房间至少连到多少个最近邻房间，2 以上才会形成环路")]
        [Range(1, 4)] public int minRoomLinks = 2;
        [Tooltip("额外随机环路的生成概率，越高路线越密")]
        [Range(0f, 1f)] public float extraLinkChance = 0.4f;
        [Tooltip("房间随机缺角概率，避免所有房间都是同一个矩形")]
        [Range(0f, 1f)] public float notchChance = 0.35f;
        [Tooltip("勾选：房间贴走廊的地方整段开洞（多路线）；取消：只有走廊端点开门")]
        public bool openAlongCorridor = true;

        [Header("点位")]
        [Tooltip("空置判定半径（格）：点位周围这么多格内不能有走廊或空地")]
        [Min(1)] public int pointOpenRadius = 2;
        [Tooltip("所有点位离地图边缘至少几格")]
        [Min(0)] public int pointEdgeMargin = 8;
        [Tooltip("副守护点数量")]
        [Min(1)] public int subGuardCount = 3;
        [Tooltip("普通点位数量下限")]
        [Min(0)] public int extraPointMin = 4;
        [Tooltip("普通点位数量上限")]
        [Min(0)] public int extraPointMax = 6;
        [Tooltip("普通点位与其他所有点位的最小间距（格）")]
        [Min(1)] public int pointSpacing = 8;

        [Tooltip("每个守护点周围生成几个防御塔点位")]
        [Min(0)] public int towerCountPerGuard = 3;
        [Tooltip("防御塔点位离所属守护点的最小距离（格）")]
        [Min(1)] public int towerMinDistance = 2;
        [Tooltip("防御塔点位离所属守护点的最大距离（格）")]
        [Min(2)] public int towerMaxDistance = 6;
        [Tooltip("同一守护点下防御塔点位之间的最小间距（格）")]
        [Min(1)] public int towerSpacing = 3;
    }
}
