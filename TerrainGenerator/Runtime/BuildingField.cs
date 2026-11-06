using System.Collections.Generic;
using UnityEngine;

namespace TerrainGen
{
    // BSP 切出房间后用走廊连通：走廊从房间边缘的随机点进出，宽度随机，另加环路和去死角
    public class BuildingField
    {
        public const int RegionVoid = -2;
        public const int RegionCorridor = -1;
        public static readonly Vector2Int[] DirDelta =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        public int SizeX;
        public int SizeZ;
        public int[,] Layer;
        public int[,] Region;
        public bool[,] Door;
        public readonly List<RectInt> Rooms = new List<RectInt>();
        public readonly List<MapPoint> Points = new List<MapPoint>();

        BuildingSettings s;
        System.Random rng;
        int maxRoomCells;
        int minRoomCells;
        int[,] openDist;
        int openRadius = 1;
        readonly HashSet<long> linked = new HashSet<long>();

        class Node
        {
            public RectInt rect;
            public Node a;
            public Node b;
            public int room = -1;
        }

        public void Generate(BuildingSettings settings) => Generate(settings, settings.seed);

        public void Generate(BuildingSettings settings, int seed)
        {
            s = settings;
            rng = new System.Random(seed);
            SizeX = Mathf.Max(4, s.mapCellsX);
            SizeZ = Mathf.Max(4, s.mapCellsZ);

            Layer = new int[SizeX, SizeZ];
            Region = new int[SizeX, SizeZ];
            Door = new bool[SizeX, SizeZ];
            Rooms.Clear();
            for (int x = 0; x < SizeX; x++)
                for (int z = 0; z < SizeZ; z++)
                {
                    Layer[x, z] = -1;
                    Region[x, z] = RegionVoid;
                }

            maxRoomCells = Mathf.Max(3, s.maxRoomCells);
            minRoomCells = Mathf.Max(2, s.minRoomCells);
            if (maxRoomCells < minRoomCells)
            {
                Debug.LogError($"[地形] maxRoomCells({maxRoomCells}) 小于 minRoomCells({minRoomCells})，已按最大值切分");
                minRoomCells = Mathf.Max(2, maxRoomCells / 2);
            }
            if (minRoomCells * 2 > maxRoomCells)
            {
                int adjusted = Mathf.Max(2, maxRoomCells / 2);
                Debug.LogWarning($"[地形] minRoomCells({minRoomCells}) 的两倍已超过 maxRoomCells({maxRoomCells})，" +
                                 $"本次临时把最小房间收到 {adjusted} 格，否则房间会突破上限");
                minRoomCells = adjusted;
            }

            var root = new Node { rect = new RectInt(0, 0, SizeX, SizeZ) };
            Split(root, 64);
            AssignRooms(root);
            if (Rooms.Count == 0) return;

            NotchRooms();
            linked.Clear();
            Connect(root);
            EnsureCycles();
            AddExtraLinks();
            PunchExtraOpenings();
            PruneStubs();
            MarkDoors();
            SelectPoints();
        }

        void Split(Node node, int guard)
        {
            if (guard <= 0) return;

            int min = minRoomCells;
            bool wantV = node.rect.width > maxRoomCells && node.rect.width >= min * 2;
            bool wantH = node.rect.height > maxRoomCells && node.rect.height >= min * 2;
            if (!wantV && !wantH) return;

            bool splitVertical = wantV && (!wantH || node.rect.width >= node.rect.height);
            if (splitVertical)
            {
                int cut = PickCut(node.rect.width, min);
                if (cut <= 0) return;
                node.a = new Node { rect = new RectInt(node.rect.x, node.rect.y, cut, node.rect.height) };
                node.b = new Node { rect = new RectInt(node.rect.x + cut, node.rect.y, node.rect.width - cut, node.rect.height) };
            }
            else
            {
                int cut = PickCut(node.rect.height, min);
                if (cut <= 0) return;
                node.a = new Node { rect = new RectInt(node.rect.x, node.rect.y, node.rect.width, cut) };
                node.b = new Node { rect = new RectInt(node.rect.x, node.rect.y + cut, node.rect.width, node.rect.height - cut) };
            }

            Split(node.a, guard - 1);
            Split(node.b, guard - 1);
        }

        // 切点限制在中间三分之一，避免 2+62 这种细长条；中间放不下时才退回全区间
        int PickCut(int length, int min)
        {
            int lo = min;
            int hi = length - min;
            if (hi < lo) return -1;

            int midLo = Mathf.Max(lo, Mathf.CeilToInt(length / 3f));
            int midHi = Mathf.Min(hi, length - Mathf.CeilToInt(length / 3f));
            return midHi >= midLo ? rng.Next(midLo, midHi + 1) : rng.Next(lo, hi + 1);
        }

        void AssignRooms(Node node)
        {
            if (node.a == null)
            {
                int m = Mathf.Clamp(s.roomMargin, 0, Mathf.Max(0, (Mathf.Min(node.rect.width, node.rect.height) - 2) / 2));
                var room = new RectInt(node.rect.x + m, node.rect.y + m, node.rect.width - m * 2, node.rect.height - m * 2);
                if (room.width < 2 || room.height < 2)
                    room = new RectInt(node.rect.x, node.rect.y, node.rect.width, node.rect.height);
                if (room.width < 1 || room.height < 1) return;

                node.room = Rooms.Count;
                Rooms.Add(room);
                CarveRect(room, node.room);
                return;
            }

            AssignRooms(node.a);
            AssignRooms(node.b);
        }

        void CarveRect(RectInt room, int region)
        {
            for (int x = room.x; x < room.xMax; x++)
                for (int z = room.y; z < room.yMax; z++)
                {
                    if (x < 0 || z < 0 || x >= SizeX || z >= SizeZ) continue;
                    Layer[x, z] = 0;
                    Region[x, z] = region;
                }
        }

        // 随机在房间某个角挖掉一块，避免所有房间都是同一个矩形
        void NotchRooms()
        {
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (rng.NextDouble() > s.notchChance) continue;
                RectInt room = Rooms[i];
                int maxNotch = Mathf.Min(room.width, room.height) / 2;
                if (maxNotch < 1) continue;

                int w = rng.Next(1, maxNotch + 1);
                int h = rng.Next(1, maxNotch + 1);
                int nx = rng.Next(2) == 0 ? room.x : room.xMax - w;
                int nz = rng.Next(2) == 0 ? room.y : room.yMax - h;

                for (int x = nx; x < nx + w; x++)
                    for (int z = nz; z < nz + h; z++)
                    {
                        if (x < 0 || z < 0 || x >= SizeX || z >= SizeZ) continue;
                        Layer[x, z] = -1;
                        Region[x, z] = RegionVoid;
                    }
            }
        }

        void Connect(Node node)
        {
            if (node.a == null) return;
            Connect(node.a);
            Connect(node.b);

            int ra = RepRoom(node.a);
            int rb = RepRoom(node.b);
            if (ra >= 0 && rb >= 0) LinkRooms(ra, rb);
        }

        static long PairKey(int a, int b) => ((long)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);

        void LinkRooms(int a, int b)
        {
            if (a == b) return;
            if (!linked.Add(PairKey(a, b))) return;
            CarveCorridor(Rooms[a], Rooms[b]);
        }

        // 保证每间房至少连上若干个最近的房间：树状骨架 → 带环路的网，消掉单入口房间
        void EnsureCycles()
        {
            if (Rooms.Count < 3) return;
            int want = Mathf.Clamp(s.minRoomLinks, 1, 4);

            for (int i = 0; i < Rooms.Count; i++)
            {
                for (int k = 0; k < want; k++)
                {
                    int best = -1;
                    int bestDist = int.MaxValue;
                    Vector2Int ci = Center(Rooms[i]);
                    for (int j = 0; j < Rooms.Count; j++)
                    {
                        if (j == i) continue;
                        if (linked.Contains(PairKey(i, j))) continue;
                        Vector2Int cj = Center(Rooms[j]);
                        int d = Mathf.Abs(ci.x - cj.x) + Mathf.Abs(ci.y - cj.y);
                        if (d < bestDist) { bestDist = d; best = j; }
                    }
                    if (best < 0) break;
                    LinkRooms(i, best);
                }
            }
        }

        static int RepRoom(Node node)
        {
            if (node.a == null) return node.room;
            int a = RepRoom(node.a);
            return a >= 0 ? a : RepRoom(node.b);
        }

        // 额外连一些较近的房间，把树状走廊变成带环路的网，顺带消掉一部分尽头
        void AddExtraLinks()
        {
            if (s.extraLinkChance <= 0f || Rooms.Count < 2) return;
            int attempts = Mathf.CeilToInt(Rooms.Count * 0.8f);
            int reach = maxRoomCells * 2;
            for (int i = 0; i < attempts; i++)
            {
                if (rng.NextDouble() > s.extraLinkChance) continue;
                int a = rng.Next(Rooms.Count);
                int b = rng.Next(Rooms.Count);
                if (a == b) continue;
                Vector2Int ca = Center(Rooms[a]);
                Vector2Int cb = Center(Rooms[b]);
                if (Mathf.Abs(ca.x - cb.x) + Mathf.Abs(ca.y - cb.y) > reach) continue;
                LinkRooms(a, b);
            }
        }

        void CarveCorridor(RectInt a, RectInt b)
        {
            int width = Mathf.Clamp(rng.Next(1, Mathf.Max(1, s.corridorMaxWidth) + 1), 1, Mathf.Max(1, s.corridorMaxWidth));
            Vector2Int pa = BorderPoint(a, b);
            Vector2Int pb = BorderPoint(b, a);

            if (Mathf.Abs(pa.x - pb.x) >= Mathf.Abs(pa.y - pb.y))
            {
                CarveLine(pa.x, pb.x, pa.y, true, width);
                CarveLine(pa.y, pb.y, pb.x, false, width);
            }
            else
            {
                CarveLine(pa.y, pb.y, pa.x, false, width);
                CarveLine(pa.x, pb.x, pb.y, true, width);
            }
        }

        // 在朝向对方的房间边上随机取一点，而不是永远从正中间出发
        Vector2Int BorderPoint(RectInt room, RectInt other)
        {
            Vector2Int oc = Center(other);
            Vector2Int rc = Center(room);
            bool horizontal = Mathf.Abs(oc.x - rc.x) >= Mathf.Abs(oc.y - rc.y);

            for (int attempt = 0; attempt < 8; attempt++)
            {
                int x, z;
                if (horizontal)
                {
                    x = oc.x >= rc.x ? room.xMax - 1 : room.x;
                    z = rng.Next(room.y, room.yMax);
                }
                else
                {
                    z = oc.y >= rc.y ? room.yMax - 1 : room.y;
                    x = rng.Next(room.x, room.xMax);
                }
                if (InBounds(x, z) && Layer[x, z] >= 0) return new Vector2Int(x, z);
            }
            return rc;
        }

        void CarveLine(int from, int to, int fixedCoord, bool horizontal, int width)
        {
            int step = from <= to ? 1 : -1;
            for (int i = from; i != to + step; i += step)
            {
                int cx = horizontal ? i : fixedCoord;
                int cz = horizontal ? fixedCoord : i;
                CarveBlock(cx, cz, width);
            }
        }

        void CarveBlock(int cx, int cz, int width)
        {
            int offset = (width - 1) / 2;
            for (int dx = 0; dx < width; dx++)
                for (int dz = 0; dz < width; dz++)
                {
                    int x = cx + dx - offset;
                    int z = cz + dz - offset;
                    if (!InBounds(x, z)) continue;
                    if (Layer[x, z] >= 0) continue;
                    Layer[x, z] = 0;
                    Region[x, z] = RegionCorridor;
                }
        }

        // 去掉悬空的走廊末端，只保留真正通向房间的部分
        // 数一个房间有几个独立开口（走廊贴着的连续段）
        bool DoorAt(int x, int z, int rid)
        {
            if (!InBounds(x, z) || Region[x, z] != rid || Layer[x, z] < 0) return false;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DirDelta[d].x;
                int nz = z + DirDelta[d].y;
                if (InBounds(nx, nz) && Region[nx, nz] == RegionCorridor) return true;
            }
            return false;
        }

        int CountOpenings(RectInt room, int rid)
        {
            var ring = new List<Vector2Int>();
            for (int x = room.x; x < room.xMax; x++) ring.Add(new Vector2Int(x, room.y));
            for (int z = room.y + 1; z < room.yMax; z++) ring.Add(new Vector2Int(room.xMax - 1, z));
            for (int x = room.xMax - 2; x >= room.x; x--) ring.Add(new Vector2Int(x, room.yMax - 1));
            for (int z = room.yMax - 2; z > room.y; z--) ring.Add(new Vector2Int(room.x, z));

            int n = ring.Count;
            if (n == 0) return 0;

            var door = new bool[n];
            int doorCount = 0;
            for (int i = 0; i < n; i++)
            {
                door[i] = DoorAt(ring[i].x, ring[i].y, rid);
                if (door[i]) doorCount++;
            }
            if (doorCount == 0) return 0;
            // 整圈都贴着走廊 = 没墙，不算死胡同
            if (doorCount >= n) return 4;

            int runs = 0;
            for (int i = 0; i < n; i++)
                if (door[i] && !door[(i - 1 + n) % n]) runs++;
            return runs;
        }

        // 单开口的房间，在完全没有开口的那条边上打一条支路接到已有走廊
        void PunchExtraOpenings()
        {
            int want = Mathf.Clamp(s.minRoomLinks, 1, 4);
            if (want < 2) return;
            int reach = maxRoomCells * 2;
            var sides = new int[4];

            for (int i = 0; i < Rooms.Count; i++)
            {
                RectInt room = Rooms[i];
                if (CountOpenings(room, i) >= want) continue;

                sides[0] = 0; sides[1] = 1; sides[2] = 2; sides[3] = 3;
                for (int k = 3; k > 0; k--)
                {
                    int j = rng.Next(k + 1);
                    int tmp = sides[k];
                    sides[k] = sides[j];
                    sides[j] = tmp;
                }

                bool punched = false;
                foreach (int side in sides)
                {
                    if (punched) break;
                    if (SideHasDoor(room, i, side)) continue;

                    int len = side < 2 ? room.width : room.height;
                    int startIndex = len > 0 ? rng.Next(len) : 0;
                    for (int k = 0; k < len; k++)
                    {
                        SideCell(room, side, (startIndex + k) % len, out int x, out int z, out int dx, out int dz);
                        if (!InBounds(x, z) || Layer[x, z] < 0 || Region[x, z] != i) continue;
                        if (!TrySpur(x, z, dx, dz, reach)) continue;
                        punched = true;
                        break;
                    }
                }
            }
        }

        bool SideHasDoor(RectInt room, int rid, int side)
        {
            switch (side)
            {
                case 0:
                    for (int x = room.x; x < room.xMax; x++) if (DoorAt(x, room.y, rid)) return true;
                    break;
                case 1:
                    for (int x = room.x; x < room.xMax; x++) if (DoorAt(x, room.yMax - 1, rid)) return true;
                    break;
                case 2:
                    for (int z = room.y; z < room.yMax; z++) if (DoorAt(room.x, z, rid)) return true;
                    break;
                default:
                    for (int z = room.y; z < room.yMax; z++) if (DoorAt(room.xMax - 1, z, rid)) return true;
                    break;
            }
            return false;
        }

        void SideCell(RectInt room, int side, int index, out int x, out int z, out int dx, out int dz)
        {
            switch (side)
            {
                case 0:
                    x = room.x + index; z = room.y; dx = 0; dz = -1;
                    break;
                case 1:
                    x = room.x + index; z = room.yMax - 1; dx = 0; dz = 1;
                    break;
                case 2:
                    x = room.x; z = room.y + index; dx = -1; dz = 0;
                    break;
                default:
                    x = room.xMax - 1; z = room.y + index; dx = 1; dz = 0;
                    break;
            }
        }

        bool TrySpur(int x, int z, int dx, int dz, int maxLength)
        {
            int cx = x + dx;
            int cz = z + dz;
            int steps = 0;
            while (steps < maxLength && InBounds(cx, cz) && Layer[cx, cz] < 0)
            {
                cx += dx;
                cz += dz;
                steps++;
            }
            if (steps == 0 || !InBounds(cx, cz) || Layer[cx, cz] < 0) return false;

            cx = x + dx;
            cz = z + dz;
            for (int i = 0; i < steps; i++)
            {
                CarveBlock(cx, cz, 1);
                cx += dx;
                cz += dz;
            }
            return true;
        }

        void PruneStubs()
        {
            bool changed = true;
            int guard = 0;
            while (changed && guard++ < 64)
            {
                changed = false;
                for (int x = 0; x < SizeX; x++)
                    for (int z = 0; z < SizeZ; z++)
                    {
                        if (Region[x, z] != RegionCorridor) continue;
                        int neighbors = 0;
                        bool touchesRoom = false;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = x + DirDelta[d].x;
                            int nz = z + DirDelta[d].y;
                            if (!InBounds(nx, nz) || Layer[nx, nz] < 0) continue;
                            neighbors++;
                            if (Region[nx, nz] >= 0) touchesRoom = true;
                        }
                        if (neighbors <= 1 && !touchesRoom)
                        {
                            Layer[x, z] = -1;
                            Region[x, z] = RegionVoid;
                            changed = true;
                        }
                    }
            }
        }

        void MarkDoors()
        {
            // 默认房间贴走廊的地方整段开洞（战斗地图要多路线）；关掉则只有走廊端点算门
            for (int x = 0; x < SizeX; x++)
                for (int z = 0; z < SizeZ; z++)
                {
                    if (Region[x, z] != RegionCorridor) continue;

                    if (!s.openAlongCorridor)
                    {
                        int corridorNeighbors = 0;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = x + DirDelta[d].x;
                            int nz = z + DirDelta[d].y;
                            if (InBounds(nx, nz) && Region[nx, nz] == RegionCorridor) corridorNeighbors++;
                        }
                        if (corridorNeighbors > 1) continue;
                    }

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + DirDelta[d].x;
                        int nz = z + DirDelta[d].y;
                        if (!InBounds(nx, nz)) continue;
                        if (Region[nx, nz] >= 0) Door[nx, nz] = true;
                    }
                }
        }

        // ---------- 点位 ----------

        // 到最近的走廊/空地的格数，用来衡量"空置程度"
        int[,] BuildOpenness()
        {
            var dist = new int[SizeX, SizeZ];
            var queue = new Queue<Vector2Int>();
            for (int x = 0; x < SizeX; x++)
                for (int z = 0; z < SizeZ; z++)
                {
                    bool blocked = Layer[x, z] < 0 || Region[x, z] < 0;
                    dist[x, z] = blocked ? 0 : int.MaxValue;
                    if (blocked) queue.Enqueue(new Vector2Int(x, z));
                }

            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                int step = dist[c.x, c.y] + 1;
                for (int d = 0; d < 4; d++)
                {
                    int nx = c.x + DirDelta[d].x;
                    int nz = c.y + DirDelta[d].y;
                    if (!InBounds(nx, nz) || dist[nx, nz] <= step) continue;
                    dist[nx, nz] = step;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }
            return dist;
        }

        List<Vector2Int> CollectOpenCells(int[,] dist, int radius, int margin)
        {
            var list = new List<Vector2Int>();
            for (int x = margin; x < SizeX - margin; x++)
                for (int z = margin; z < SizeZ - margin; z++)
                    if (Layer[x, z] >= 0 && Region[x, z] >= 0 && dist[x, z] >= radius)
                        list.Add(new Vector2Int(x, z));
            return list;
        }

        void SelectPoints()
        {
            Points.Clear();

            int radius = Mathf.Max(1, s.pointOpenRadius);
            int margin = Mathf.Clamp(s.pointEdgeMargin, 0, Mathf.Min(SizeX, SizeZ) / 3);
            int[,] dist = BuildOpenness();
            openDist = dist;
            openRadius = radius;
            List<Vector2Int> pool = CollectOpenCells(dist, radius, margin);
            while (pool.Count < 8 && radius > 1)
            {
                radius--;
                pool = CollectOpenCells(dist, radius, margin);
                Debug.LogWarning($"[地形] 空置半径收窄到 {radius} 格，候选点 {pool.Count} 个");
            }
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[地形] 距边缘 {margin} 格以内没有足够空置的区域，未生成点位（可调小 pointEdgeMargin）");
                return;
            }

            // 主守护点：候选池已经排除边缘区域，这里再取最靠中心且最空旷的
            Vector2Int center = new Vector2Int(SizeX / 2, SizeZ / 2);
            var main = new Vector2Int(-1, -1);
            int bestScore = int.MinValue;
            foreach (Vector2Int c in pool)
            {
                if (c.x < margin || c.y < margin || c.x >= SizeX - margin || c.y >= SizeZ - margin) continue;
                int toCenter = Mathf.Abs(c.x - center.x) + Mathf.Abs(c.y - center.y);
                int score = -toCenter * 4 + Mathf.Min(dist[c.x, c.y], 12);
                if (score <= bestScore) continue;
                bestScore = score;
                main = c;
            }
            if (main.x < 0)
            {
                main = pool[0];
                Debug.LogWarning("[地形] 边缘限制过严，主守护点退到第一个空置格");
            }

            var chosen = new List<Vector2Int> { main };
            var usedRooms = new HashSet<int> { Region[main.x, main.y] };
            Points.Add(new MapPoint(main, PointKind.MainGuard));

            // 副守护点：硬性约束离地图中心 <= 0.25 * 边长，在圆内用最远点采样互相拉开
            int subCount = Mathf.Max(0, s.subGuardCount);
            if (subCount > 0)
            {
                float maxR = Mathf.Max(SizeX, SizeZ) * 0.25f;
                for (int i = 0; i < subCount; i++)
                {
                    Vector2Int p = new Vector2Int(-1, -1);
                    for (int pass = 0; pass < 4 && p.x < 0; pass++)
                    {
                        bool distinctRooms = pass < 2;
                        float best = -1f;
                        foreach (Vector2Int c in pool)
                        {
                            if (Vector2.Distance(c, center) > maxR) continue;
                            if (distinctRooms && usedRooms.Contains(Region[c.x, c.y])) continue;
                            float nearest = float.MaxValue;
                            foreach (Vector2Int q in chosen)
                                nearest = Mathf.Min(nearest, Vector2.Distance(c, q));
                            if (i == 0) nearest += (float)rng.NextDouble() * 0.5f;
                            if (nearest > best) { best = nearest; p = c; }
                        }
                    }
                    if (p.x < 0)
                    {
                        Debug.LogWarning($"[地形] 副守护点只放下 {i} 个 / {subCount}，中心区域内空置格不够");
                        break;
                    }
                    Points.Add(new MapPoint(p, PointKind.SubGuard));
                    chosen.Add(p);
                    usedRooms.Add(Region[p.x, p.y]);
                }
            }

            int low = Mathf.Min(s.extraPointMin, s.extraPointMax);
            int high = Mathf.Max(s.extraPointMin, s.extraPointMax);
            int extraCount = rng.Next(low, high + 1);
            int gap = Mathf.Max(1, s.pointSpacing);
            for (int i = 0; i < extraCount; i++)
            {
                Vector2Int p = PickFarthest(pool, chosen, usedRooms, gap);
                while (p.x < 0 && gap > 1)
                {
                    gap--;
                    p = PickFarthest(pool, chosen, usedRooms, gap);
                }
                if (p.x < 0)
                {
                    Debug.LogWarning($"[地形] 普通点位只放下 {i} 个 / {extraCount}，空置区域不够");
                    break;
                }
                Points.Add(new MapPoint(p, PointKind.Extra));
                chosen.Add(p);
                usedRooms.Add(Region[p.x, p.y]);
            }

            SelectTowerPoints();
        }

        // 每个守护点周围按角度均分选几个防御塔点位
        void SelectTowerPoints()
        {
            int want = Mathf.Clamp(s.towerCountPerGuard, 0, 8);
            if (want == 0 || openDist == null) return;

            int minD = Mathf.Max(1, s.towerMinDistance);
            int maxD = Mathf.Max(minD + 1, s.towerMaxDistance);
            int gap = Mathf.Max(1, s.towerSpacing);
            int margin = Mathf.Clamp(s.pointEdgeMargin, 0, Mathf.Min(SizeX, SizeZ) / 3);

            var occupied = new List<Vector2Int>();
            foreach (MapPoint p in Points) occupied.Add(p.Cell);

            var guardians = new List<MapPoint>();
            foreach (MapPoint p in Points)
                if (p.Kind == PointKind.MainGuard || p.Kind == PointKind.SubGuard) guardians.Add(p);

            foreach (MapPoint g in guardians)
            {
                var candidates = new List<Vector2Int>();
                int reach = maxD;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    candidates.Clear();
                    for (int x = margin; x < SizeX - margin; x++)
                        for (int z = margin; z < SizeZ - margin; z++)
                        {
                            if (Layer[x, z] < 0 || Region[x, z] < 0) continue;
                            if (openDist[x, z] < 1) continue;
                            int d = Mathf.Abs(x - g.Cell.x) + Mathf.Abs(z - g.Cell.y);
                            if (d < minD || d > reach) continue;
                            if (IsTooClose(new Vector2Int(x, z), occupied, gap)) continue;
                            candidates.Add(new Vector2Int(x, z));
                        }
                    if (candidates.Count >= want) break;
                    reach += 2;
                }
                if (candidates.Count == 0)
                {
                    Debug.LogWarning($"[地形] 守护点 ({g.Cell.x},{g.Cell.y}) 周围找不到防御塔点位");
                    continue;
                }

                float sector = Mathf.PI * 2f / want;
                float startAngle = (float)rng.NextDouble() * Mathf.PI * 2f;
                var picked = new List<Vector2Int>();

                for (int k = 0; k < want; k++)
                {
                    float from = startAngle + sector * k;
                    var best = new Vector2Int(-1, -1);
                    int bestDist = -1;
                    foreach (Vector2Int c in candidates)
                    {
                        float angle = Mathf.Atan2(c.y - g.Cell.y, c.x - g.Cell.x);
                        float rel = Mathf.Repeat(angle - from, Mathf.PI * 2f);
                        if (rel >= sector) continue;
                        if (IsTooClose(c, occupied, gap)) continue;
                        int d = Mathf.Abs(c.x - g.Cell.x) + Mathf.Abs(c.y - g.Cell.y);
                        if (d <= bestDist) continue;
                        bestDist = d;
                        best = c;
                    }
                    if (best.x < 0) continue;
                    picked.Add(best);
                    occupied.Add(best);
                    Points.Add(new MapPoint(best, PointKind.Tower));
                }

                // 扇区凑不满时补位：优先选离已选塔最远的
                while (picked.Count < want)
                {
                    var best = new Vector2Int(-1, -1);
                    int bestDist = -1;
                    foreach (Vector2Int c in candidates)
                    {
                        if (IsTooClose(c, occupied, gap)) continue;
                        int nearest = int.MaxValue;
                        foreach (Vector2Int o in picked)
                            nearest = Mathf.Min(nearest, Mathf.Abs(c.x - o.x) + Mathf.Abs(c.y - o.y));
                        if (nearest <= bestDist) continue;
                        bestDist = nearest;
                        best = c;
                    }
                    if (best.x < 0) break;
                    picked.Add(best);
                    occupied.Add(best);
                    Points.Add(new MapPoint(best, PointKind.Tower));
                }

                if (picked.Count < want)
                    Debug.LogWarning($"[地形] 守护点 ({g.Cell.x},{g.Cell.y}) 只放下 {picked.Count} 个防御塔点位 / {want}");
            }
        }

        static bool IsTooClose(Vector2Int cell, List<Vector2Int> others, int minDistance)
        {
            foreach (Vector2Int o in others)
                if (Mathf.Abs(cell.x - o.x) + Mathf.Abs(cell.y - o.y) < minDistance) return true;
            return false;
        }

        // 最远点采样：在满足最小间距的候选里挑离已有点最远的一个，天然均匀铺开
        Vector2Int PickFarthest(List<Vector2Int> pool, List<Vector2Int> chosen, HashSet<int> usedRooms, int minDistance)
        {
            var best = new Vector2Int(-1, -1);
            int bestDistance = -1;
            foreach (Vector2Int c in pool)
            {
                if (usedRooms.Contains(Region[c.x, c.y])) continue;
                int nearest = int.MaxValue;
                foreach (Vector2Int o in chosen)
                    nearest = Mathf.Min(nearest, Mathf.Abs(c.x - o.x) + Mathf.Abs(c.y - o.y));
                if (nearest < minDistance || nearest <= bestDistance) continue;
                bestDistance = nearest;
                best = c;
            }
            return best;
        }

        // 与网格生成共用的墙体判定，装饰层据此放围栏/石桩
        public bool NeedWallAt(int x, int z, int d)
        {
            if (!InBounds(x, z) || Layer[x, z] < 0) return false;
            int nx = x + DirDelta[d].x;
            int nz = z + DirDelta[d].y;
            bool outside = !InBounds(nx, nz);
            bool occupied = !outside && Layer[nx, nz] >= 0;
            if (!occupied) return true;
            if (Region[x, z] >= 0 && Region[nx, nz] < 0) return !Door[x, z];
            return false;
        }

        // 同一区域用同一套素材：房间按房间号分组，走廊按 8 格街区
        public int RegionGroup(int x, int z)
        {
            if (Region[x, z] >= 0) return Region[x, z];
            return 900000 + (x / 8) * 1000 + (z / 8);
        }

        bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < SizeX && z < SizeZ;

        static Vector2Int Center(RectInt r) => new Vector2Int(r.x + r.width / 2, r.y + r.height / 2);

        public int FloorCellCount()
        {
            int n = 0;
            for (int x = 0; x < SizeX; x++)
                for (int z = 0; z < SizeZ; z++)
                    if (Layer[x, z] >= 0) n++;
            return n;
        }


    }
}
