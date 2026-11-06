using System.Collections.Generic;
using UnityEngine;

namespace TerrainGen
{
    [CreateAssetMenu(menuName = "模块化地形/装饰素材表", fileName = "BuildingKitSet")]
    public class BuildingKitSet : ScriptableObject
    {
        [Header("地面（每格一块，顶面对齐格面）")]
        public List<GameObject> ground = new List<GameObject>();

        [Header("围栏（沿边界边铺设）")]
        public List<GameObject> fences = new List<GameObject>();

        [Header("石桩（放在边界拐角）")]
        public List<GameObject> posts = new List<GameObject>();

        [Tooltip("围栏素材长轴不是本地 X 轴时，用这个角度补正")]
        public int fenceYawOffset;

        [Tooltip("同一区域挑中同一素材的偏移，换个数字可以整体换一套搭配")]
        public int variantSeed;

        // 同一区域固定取同一个 index，避免整张图看起来杂乱
        public GameObject PickGround(int group) => Pick(ground, group);

        public GameObject PickFence(int group) => Pick(fences, group);

        public GameObject PickPost(int group) => Pick(posts, group);

        GameObject Pick(List<GameObject> list, int group)
        {
            if (list == null || list.Count == 0) return null;
            int mixed = group * 397 ^ variantSeed * 17;
            int index = Mathf.Abs(mixed) % list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                GameObject candidate = list[(index + i) % list.Count];
                if (candidate != null) return candidate;
            }
            return null;
        }
    }
}
