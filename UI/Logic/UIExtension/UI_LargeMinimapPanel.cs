using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 小地图展开（左 Alt）：整张地形图铺满 loader，把雷达当前收到的点位按整图换算铺上去，纯展示不可点
    public partial class UI_LargeMinimapPanel
    {
        private readonly Dictionary<ushort, UI_MinimapItem> items = new();
        private readonly HashSet<ushort> staleIds = new();
        private NTexture mapTexture;

        public bool IsOpen => visible;

        public void Open()
        {
            EnsureMapImage();
            visible = true;
            touchable = true;
        }

        public void Close()
        {
            visible = false;
            touchable = false;
            DisposeItems();
        }

        // 点位缓存每帧重排：数量不多，只做增删与换位，不重建已有图标
        public void Refresh(IReadOnlyDictionary<ushort, UI_Minimap.Point> points, EntityCamp myCamp, float selfYaw, bool hasSelfYaw)
        {
            if (!visible) return;
            EnsureMapImage();
            if (points == null)
            {
                DisposeItems();
                return;
            }

            staleIds.Clear();
            foreach (ushort id in items.Keys) staleIds.Add(id);
            foreach (var pair in points)
            {
                var point = pair.Value;
                var item = EnsureItem(point);
                if (item == null) continue;
                item.m_type.selectedIndex = UI_Minimap.PageOf(point.category, point.type, point.camp, point.self, myCamp);
                item.m_highlight.selectedIndex = point.marked ? 1 : 0;
                if (point.self && hasSelfYaw) item.rotation = selfYaw;
                Place(item, point);
                staleIds.Remove(point.id);
            }
            foreach (ushort id in staleIds) Remove(id);
        }

        // 整图换算：图左下 = 世界 (0,0)、上 = 世界 Z+，loader 就是整张地图的显示区域
        private void Place(UI_MinimapItem item, in UI_Minimap.Point point)
        {
            if (m_loader == null) return;
            float ppmX = m_loader.width / Landscape.MapSize;
            float ppmY = m_loader.height / Landscape.MapSize;
            item.SetXY(m_loader.x + point.world.x * ppmX - item.width * 0.5f,
                m_loader.y + (Landscape.MapSize - point.world.y) * ppmY - item.height * 0.5f);
        }

        private UI_MinimapItem EnsureItem(in UI_Minimap.Point point)
        {
            if (items.TryGetValue(point.id, out var item) && item != null && !item.isDisposed) return item;
            item = UI_MinimapItem.CreateInstance();
            AddChild(item); // loader 是 1 号子物体，后加的点位天然压在底图之上
            items[point.id] = item;
            return item;
        }

        private void Remove(ushort id)
        {
            if (!items.TryGetValue(id, out var item)) return;
            if (item != null) item.Dispose();
            items.Remove(id);
        }

        private void DisposeItems()
        {
            foreach (var item in items.Values)
                if (item != null) item.Dispose();
            items.Clear();
        }

        private void EnsureMapImage()
        {
            if (m_loader == null) return;
            m_loader.fill = FillType.Scale;
            if (mapTexture == null && Tool.AssetsManager != null && Tool.AssetsManager.MapTexture != null)
                mapTexture = new NTexture(Tool.AssetsManager.MapTexture);
            if (m_loader.texture == null) m_loader.texture = mapTexture;
        }
    }
}
