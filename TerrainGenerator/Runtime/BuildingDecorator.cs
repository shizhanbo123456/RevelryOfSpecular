using System.Collections.Generic;
using UnityEngine;

namespace TerrainGen
{
    // 只按约定摆预制体：地面 6×6×4、原点在底面中心；围墙 6 米长、默认沿 Z 延伸
    public static class BuildingDecorator
    {
        public const string Name = "Decor";

        public static int Build(BuildingField f, BuildingSettings s, BuildingKitSet kit, Transform parent)
        {
            if (kit == null) return 0;

            var container = new GameObject(Name);
            container.transform.SetParent(parent, false);

            float c = s.cellSize;
            float ox = (f.SizeX - 1) * 0.5f * c;
            float oz = (f.SizeZ - 1) * 0.5f * c;
            float surface = s.groundHeight;
            int count = 0;

            // 地面：每格一块，底面中心对齐格子中心
            for (int x = 0; x < f.SizeX; x++)
            {
                for (int z = 0; z < f.SizeZ; z++)
                {
                    if (f.Layer[x, z] < 0) continue;
                    GameObject prefab = kit.PickGround(f.RegionGroup(x, z));
                    if (prefab == null) continue;

                    var go = (GameObject)Object.Instantiate(prefab, container.transform);
                    go.name = "Ground_" + prefab.name;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localPosition = new Vector3(x * c - ox, 0f, z * c - oz);
                    count++;
                }
            }

            // 围墙：沿边界边铺设；素材默认沿 Z 延伸，所以东西向的边转 90 度
            for (int x = 0; x < f.SizeX; x++)
            {
                for (int z = 0; z < f.SizeZ; z++)
                {
                    if (f.Layer[x, z] < 0) continue;

                    // x*c-ox 是格子中心，格子最小角要再减半格
                    float x0 = x * c - ox - c * 0.5f;
                    float x1 = x0 + c;
                    float z0 = z * c - oz - c * 0.5f;
                    float z1 = z0 + c;
                    int group = f.RegionGroup(x, z);

                    for (int d = 0; d < 4; d++)
                    {
                        if (!f.NeedWallAt(x, z, d)) continue;
                        GameObject prefab = kit.PickFence(group);
                        if (prefab == null) continue;

                        int yaw = (d % 2 == 0 ? 90 : 0) + kit.fenceYawOffset;
                        Vector3 mid;
                        switch (d)
                        {
                            case 0: mid = new Vector3((x0 + x1) * 0.5f, surface, z1); break;
                            case 1: mid = new Vector3(x1, surface, (z0 + z1) * 0.5f); break;
                            case 2: mid = new Vector3((x0 + x1) * 0.5f, surface, z0); break;
                            default: mid = new Vector3(x0, surface, (z0 + z1) * 0.5f); break;
                        }

                        var go = (GameObject)Object.Instantiate(prefab, container.transform);
                        go.name = "Fence_" + prefab.name;
                        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        go.transform.localPosition = mid;
                        count++;
                    }
                }
            }

            // 石桩：只放在两条边界边交汇的格角，重叠的角只放一个
            var corners = new HashSet<long>();
            for (int x = 0; x < f.SizeX; x++)
            {
                for (int z = 0; z < f.SizeZ; z++)
                {
                    if (f.Layer[x, z] < 0) continue;
                    int group = f.RegionGroup(x, z);
                    bool w = f.NeedWallAt(x, z, 3);
                    bool e = f.NeedWallAt(x, z, 1);
                    bool sEdge = f.NeedWallAt(x, z, 2);
                    bool n = f.NeedWallAt(x, z, 0);

                    count += TryPost(x, z, w && sEdge, group, kit, ox, oz, c, surface, container.transform, corners);
                    count += TryPost(x + 1, z, e && sEdge, group, kit, ox, oz, c, surface, container.transform, corners);
                    count += TryPost(x + 1, z + 1, e && n, group, kit, ox, oz, c, surface, container.transform, corners);
                    count += TryPost(x, z + 1, w && n, group, kit, ox, oz, c, surface, container.transform, corners);
                }
            }

            return count;
        }

        static int TryPost(int cx, int cz, bool atCorner, int group, BuildingKitSet kit,
            float ox, float oz, float cell, float surface, Transform parent, HashSet<long> seen)
        {
            if (!atCorner) return 0;
            if (!seen.Add(((long)cx << 32) | (uint)cz)) return 0;

            GameObject prefab = kit.PickPost(group);
            if (prefab == null) return 0;

            var go = (GameObject)Object.Instantiate(prefab, parent);
            go.name = "Post_" + prefab.name;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = new Vector3(cx * cell - ox - cell * 0.5f, surface, cz * cell - oz - cell * 0.5f);
            return 1;
        }
    }
}
