using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 小地图表现：点位增删改、雷达换算、自身朝向、地形底图对位。
    // 网络事件仍由 BattlePage 订阅，这里只接收换算所需的数据。
    public partial class UI_Minimap
    {
        // 单个点位：档位由本类按实体类别与阵营换算，调用方只给原始信息
        public readonly struct Point
        {
            public readonly ushort id;
            public readonly EntityCategory category;
            public readonly EntityType type;
            public readonly EntityCamp camp;
            public readonly bool marked;
            public readonly bool self;
            public readonly Vector2 world; // 世界坐标 x/z

            public Point(ushort id, EntityCategory category, EntityType type, EntityCamp camp,
                bool marked, bool self, Vector2 world)
            {
                this.id = id;
                this.category = category;
                this.type = type;
                this.camp = camp;
                this.marked = marked;
                this.self = self;
                this.world = world;
            }
        }

        private readonly Dictionary<ushort, Point> points = new();
        private readonly Dictionary<ushort, float> lastReceived = new();
        private readonly Dictionary<ushort, UI_MinimapItem> items = new();
        private readonly List<ushort> culledIds = new(); // 剔除临时列表：遍历中不能改字典
        private readonly List<ushort> expiredIds = new();
        private ushort selfId;
        private EntityCamp myCamp = EntityCamp.None;
        private float viewRadius;
        private float itemScale = 1f;
        private GLoader mapLoader;
        private NTexture mapTexture;

        // 自身点位：固定在雷达中心且永远置顶
        private UI_MinimapItem SelfItem => selfId != 0 && items.TryGetValue(selfId, out var item) ? item : null;

        // 每局进战斗：复位半径与点位、重算图标缩放、补建地形底图
        public void Reset(float defaultRadius)
        {
            viewRadius = defaultRadius;
            myCamp = EntityCamp.None;
            DisposeItems();
            ComputeItemScale();
            EnsureMapLoader();
        }

        // F 键切档：服务器权威，客户端换用新半径做裁剪与缩放
        public void SetRadius(float radius)
        {
            if (radius > 0f) viewRadius = radius;
        }

        // 收到点位包：只记录数据，渲染统一在 Tick 里按当前圆心处理；localCamp 用于队友/敌人分档
        public void Push(in Point point, EntityCamp localCamp)
        {
            points[point.id] = point;
            lastReceived[point.id] = Time.time;
            myCamp = localCamp;
        }

        // 实体消失、超时、超半径统一走这里：显示与数据一起清
        public void Remove(ushort entityId)
        {
            if (items.TryGetValue(entityId, out var item))
            {
                if (item != null) item.Dispose();
                items.Remove(entityId);
            }
            points.Remove(entityId);
            lastReceived.Remove(entityId);
            if (selfId == entityId) selfId = 0;
        }

        // 每帧：超时剔除、自身朝向、地形底图、点位重排；圆心未知时只做前两项
        public void Tick(Vector3 center, bool hasCenter, float selfYaw, bool hasSelfYaw)
        {
            TickTimeout();

            var self = SelfItem;
            if (self != null && !self.isDisposed && hasSelfYaw)
                self.rotation = selfYaw; // 地图不转、图标转

            if (!hasCenter) return;
            TickMapImage(center);
            RenderPoints(center);
        }

        private void RenderPoints(Vector3 center)
        {
            culledIds.Clear();
            foreach (var pair in points)
            {
                var point = pair.Value;
                var item = EnsureItem(point);
                if (item == null) continue;
                item.m_type.selectedIndex = GetPage(point);
                item.m_highlight.selectedIndex = point.marked ? 1 : 0; // 白眼标记随包同步
                if (!PlaceItem(item, point, center)) culledIds.Add(point.id);
            }
            foreach (var id in culledIds) Remove(id);
            RaiseSelfToTop();
        }

        private UI_MinimapItem EnsureItem(in Point point)
        {
            if (items.TryGetValue(point.id, out var item) && item != null && !item.isDisposed) return item;
            item = UI_MinimapItem.CreateInstance();
            item.scale = new Vector2(itemScale, itemScale); // 抵消上层缩放，恢复 FGUI 设计尺寸
            AddChild(item); // GGraph 不是容器，点位挂在小地图面板上
            items[point.id] = item;
            if (point.self) selfId = point.id;
            return item;
        }

        // 返回 false = 超出雷达显示半径（由调用方剔除）
        private bool PlaceItem(UI_MinimapItem item, in Point point, Vector3 center)
        {
            var mapBase = m_mapBase;
            if (mapBase == null) return true;

            float dx = 0f, dz = 0f;
            if (!point.self)
            {
                dx = point.world.x - center.x;
                dz = point.world.y - center.z;
                if (dx * dx + dz * dz > viewRadius * viewRadius) return false;
            }

            // 雷达坐标：上方=世界Z+、右侧=世界X+，显示半径铺满 mapBase；减半宽高让图标几何居中
            float scale = mapBase.width * 0.5f / viewRadius;
            item.SetXY(mapBase.x + mapBase.width * 0.5f + dx * scale - item.width * 0.5f,
                mapBase.y + mapBase.height * 0.5f - dz * scale - item.height * 0.5f);
            return true;
        }

        // 实体类别 → 点位档位（0 自己 / 1 队友 / 2 敌人 / 3~9 场景与守卫）
        private int GetPage(in Point point)
        {
            switch (point.category)
            {
                case EntityCategory.Character_Attack:
                case EntityCategory.Character_Defense:
                    if (point.self) return 0;
                    return point.camp == myCamp ? 1 : 2;
                case EntityCategory.NeutralStatue: return 3;
                case EntityCategory.Coral: return 4;
                case EntityCategory.CrystalStatue: return 5;
                case EntityCategory.Zombie: return 6;
                case EntityCategory.EliteZombie: return 7;
                case EntityCategory.GuardStatue: return point.type == EntityType.KingStatue ? 8 : 9;
                default:
                    Debug.LogError($"[Minimap] 未处理的实体类别：{point.category}");
                    return 7;
            }
        }

        private void RaiseSelfToTop()
        {
            var self = SelfItem;
            if (self == null || self.isDisposed) return;
            SetChildIndex(self, numChildren - 1); // 新点加入可能盖住自身图标，每次校一次层级
        }

        // 超时剔除：长时间未收到点位包（离屏停传 / 夜间敌方停传）即移除
        private void TickTimeout()
        {
            if (lastReceived.Count == 0) return;
            float now = Time.time;
            expiredIds.Clear();
            foreach (var kv in lastReceived)
                if (now - kv.Value > Config.minimap_entity_timeout) expiredIds.Add(kv.Key);
            foreach (var id in expiredIds) Remove(id);
        }

        // 抵消 Minimap 上层元素（不含页面根 UiScale）的缩放
        private void ComputeItemScale()
        {
            float upper = 1f;
            for (GObject o = this; o != null && o.parent != GRoot.inst; o = o.parent)
                if (o.scale.x > 0.0001f) upper *= o.scale.x;
            itemScale = upper > 0.0001f ? 1f / upper : 1f;
        }

        // 遮罩内创建地形底图 Loader：只建一次，被重建时补建
        private void EnsureMapLoader()
        {
            if (m_mask == null) return;
            if (mapLoader != null && !mapLoader.isDisposed && mapLoader.parent == m_mask) return;
            mapLoader = new GLoader();
            mapLoader.fill = FillType.Scale; // 地形图与目标区域同为正方形，直接拉伸铺满
            m_mask.AddChild(mapLoader);
            if (mapTexture == null && Tool.AssetsManager != null && Tool.AssetsManager.MapTexture != null)
                mapTexture = new NTexture(Tool.AssetsManager.MapTexture);
            mapLoader.texture = mapTexture;
        }

        // 地形底图对位：与点位同一套雷达换算（图左下=世界(0,0)、上=+Z）
        private void TickMapImage(Vector3 center)
        {
            var mapBase = m_mapBase;
            if (mapLoader == null || mapLoader.isDisposed || mapBase == null) return;

            float ppm = mapBase.width * 0.5f / viewRadius; // 像素/米
            float size = Landscape.MapSize * ppm;
            float maskX = m_mask != null ? m_mask.x : 0f; // Loader 挂在遮罩里，需换算成遮罩局部
            float maskY = m_mask != null ? m_mask.y : 0f;
            mapLoader.SetXY(
                mapBase.x + mapBase.width * 0.5f - center.x * ppm - maskX,
                mapBase.y + mapBase.height * 0.5f - (Landscape.MapSize - center.z) * ppm - maskY);
            mapLoader.SetSize(size, size);
        }

        private void DisposeItems()
        {
            foreach (var item in items.Values)
                if (item != null) item.Dispose();
            items.Clear();
            points.Clear();
            lastReceived.Clear();
            selfId = 0;
        }
    }
}
