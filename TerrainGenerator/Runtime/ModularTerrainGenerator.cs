using UnityEngine;

namespace TerrainGen
{
    public class ModularTerrainGenerator : MonoBehaviour
    {
        public BuildingSettings settings;
        [Tooltip("地面/围栏/石桩素材表，留空则只生成素地面和墙")]
        public BuildingKitSet kit;
        [Tooltip("每次生成换一个随机种子")]
        public bool randomSeed;
        public int seed = 20261105;
        [Tooltip("生成到哪个节点下，留空则生成到自身")]
        public Transform container;
        public bool generateOnStart;

        BuildingField field;

        void Start()
        {
            if (generateOnStart) Generate();
        }

        public GameObject Generate()
        {
            if (settings == null) { Debug.LogError("[地形] 未指定生成参数 BuildingSettings", this); return null; }
            Clear();
            int useSeed = randomSeed ? System.Environment.TickCount : seed;
            field = new BuildingField();
            field.Generate(settings, useSeed);

            Transform parent = container != null ? container : transform;
            TerrainBuildResult result = TerrainAssembler.Build(field, settings, kit, parent);
            if (!result.Success)
            {
                Debug.LogError("[地形] " + result.error, this);
                return null;
            }
            return result.root;
        }

        public void Clear()
        {
            Transform parent = container != null ? container : transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name != TerrainAssembler.RootName) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        public BuildingField Field => field;
    }
}
