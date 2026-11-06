using UnityEngine;

namespace TerrainGen
{
    public class TerrainBuildResult
    {
        public GameObject root;
        public int points;
        public int decor;
        public string error;
        public bool Success => string.IsNullOrEmpty(error);

        public TerrainBuildResult(GameObject root, int points, int decor, string error)
        {
            this.root = root;
            this.points = points;
            this.decor = decor;
            this.error = error;
        }
    }

    public class TerrainAssembler
    {
        public const string RootName = "TerrainRoot";
        public const string PointsName = "Points";

        public static TerrainBuildResult Build(BuildingField field, BuildingSettings s, BuildingKitSet kit, Transform parent)
        {
            if (field == null || field.Layer == null) return new TerrainBuildResult(null, 0, 0, "建筑布局为空，先生成布局");
            if (s == null) return new TerrainBuildResult(null, 0, 0, "未指定生成参数 BuildingSettings");
            if (field.FloorCellCount() == 0) return new TerrainBuildResult(null, 0, 0, "布局里没有任何地板格，检查房间划分参数");

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            int decor = BuildingDecorator.Build(field, s, kit, root.transform);
            int points = CreatePoints(field, s, root.transform);
            return new TerrainBuildResult(root, points, decor, null);
        }

        static int CreatePoints(BuildingField field, BuildingSettings s, Transform parent)
        {
            if (field.Points.Count == 0) return 0;

            var container = new GameObject(PointsName);
            container.transform.SetParent(parent, false);

            float ox = (field.SizeX - 1) * 0.5f * s.cellSize;
            float oz = (field.SizeZ - 1) * 0.5f * s.cellSize;
            int subIndex = 0;
            int extraIndex = 0;
            int towerIndex = 0;

            foreach (MapPoint p in field.Points)
            {
                string name;
                Color color;
                switch (p.Kind)
                {
                    case PointKind.MainGuard:
                        name = "Guard_Main";
                        color = new Color(1f, 0.35f, 0.15f);
                        break;
                    case PointKind.SubGuard:
                        name = "Guard_Sub_" + (++subIndex);
                        color = new Color(1f, 0.75f, 0.2f);
                        break;
                    case PointKind.Tower:
                        name = "Tower_" + (++towerIndex);
                        color = new Color(0.35f, 0.95f, 0.45f);
                        break;
                    default:
                        name = "Open_" + (++extraIndex);
                        color = new Color(0.25f, 0.8f, 1f);
                        break;
                }

                var go = new GameObject(name);
                go.transform.SetParent(container.transform, false);
                go.transform.localPosition = new Vector3(p.Cell.x * s.cellSize - ox, s.groundHeight, p.Cell.y * s.cellSize - oz);

                var marker = go.AddComponent<PointMarker>();
                marker.label = name;
                marker.color = color;
                marker.radius = s.cellSize * (p.Kind == PointKind.Tower ? 0.45f : 0.6f);
            }

            return field.Points.Count;
        }
    }
}
