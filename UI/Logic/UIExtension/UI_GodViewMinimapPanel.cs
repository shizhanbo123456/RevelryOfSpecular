using System;
using System.Collections.Generic;
using FairyGUI;
using Ros.Transport;
using UnityEngine;

namespace Ros.UI.Main
{
    // 上帝视角（左 Ctrl）：显示区域 = 中心 + 边长（地形边长 / 倍率），可拖动；服务器只在收到请求时回包
    // 实体点位可点，点中后单独请求详情快照填右侧面板；珊瑚只画点、不响应点击
    public partial class UI_GodViewMinimapPanel
    {
        private const float RequestInterval = 0.2f; // 进界面期间按此频率反复拉取，本地图不动就不请求
        private const int DetailSkillCapacity = 8;

        private struct Marker
        {
            public GObject view;
            public Vector2 world;
        }

        private readonly Dictionary<ushort, Marker> entityMarkers = new();
        private readonly List<UI_MinimapItem> coralViews = new(); // 珊瑚没有 id：按序号复用图标
        private readonly HashSet<ushort> staleIds = new();
        private readonly HashSet<ushort> receivedEntityChunks = new();
        private readonly HashSet<ushort> receivedCoralChunks = new();
        private readonly List<SCMinimapEntity> entityBuffer = new();
        private readonly List<Vector2> coralBuffer = new();
        private readonly List<UI_SkillList.Slot> detailSkills = new();
        private readonly List<UI_SkillList.Slot> detailHeldSkills = new();
        private readonly List<SCGodViewEntityDetail.Buff> detailBuffs = new();

        private Action<int, GObject> buffHoverEnter;
        private Action buffHoverExit;
        private NTexture mapTexture;
        private Vector2 center;
        private int scaleIndex;
        private byte seq;
        private float requestTimer;
        private bool dragging;
        private Vector2 lastPointer;
        private int expectedEntityChunks = -1;
        private int expectedCoralChunks = -1;
        private float itemScale = 1f;
        private EntityCamp myCamp = EntityCamp.None;
        private ushort ownEntityId;

        public bool IsOpen => visible;

        // 悬浮提示回调由战斗页提供（与血条 buff 图标同一套提示面板）
        public void Init(Action<int, GObject> onBuffHoverEnter, Action onBuffHoverExit)
        {
            buffHoverEnter = onBuffHoverEnter;
            buffHoverExit = onBuffHoverExit;
            m_largeMinimapContainer.onTouchBegin.Set(OnDragBegin);
            m_largeMinimapContainer.onTouchMove.Set(OnDragMove);
            m_largeMinimapContainer.onTouchEnd.Set(OnDragEnd);
        }

        public void Open(Vector2 worldCenter)
        {
            visible = true;
            touchable = true;
            m_largeMinimapContainer.opaque = true;
            m_largeMinimapContainer.touchable = true;
            center = ClampCenter(worldCenter);
            scaleIndex = 0; // 默认取配表首档（放大最多的一档）
            seq = 0;
            requestTimer = RequestInterval;
            dragging = false;
            DisposeMarkers(entityMarkers);
            DisposeCoralViews(coralViews);
            ClearDetail();
            BuildScaleButtons();
            ComputeItemScale();
            ApplyLayout();
            SendRequest(); // 进界面立刻拉一次，不等第一个节拍
        }

        public void Close()
        {
            visible = false;
            touchable = false;
            dragging = false;
            DisposeMarkers(entityMarkers);
            DisposeCoralViews(coralViews);
            ClearDetail();
        }

        public void Tick(float deltaTime, EntityCamp localCamp, ushort ownId)
        {
            if (!visible) return;
            myCamp = localCamp;
            ownEntityId = ownId;
            requestTimer -= deltaTime;
            if (requestTimer > 0f) return;
            requestTimer = RequestInterval;
            SendRequest();
        }

        #region 拖拽
        private void OnDragBegin(EventContext context)
        {
            if (!visible) return;
            dragging = true;
            lastPointer = m_largeMinimapContainer.GlobalToLocal(context.inputEvent.position);
            context.CaptureTouch();
        }

        private void OnDragMove(EventContext context)
        {
            if (!dragging) return;
            Vector2 current = m_largeMinimapContainer.GlobalToLocal(context.inputEvent.position);
            Vector2 delta = current - lastPointer;
            lastPointer = current;
            float ppm = PixelsPerMeter;
            if (ppm <= 0f) return;
            // 内容跟着手指走：向右拖 = 视野中心左移，向下拖 = 视野中心上移（世界 Z+）
            center = ClampCenter(new Vector2(center.x - delta.x / ppm, center.y + delta.y / ppm));
            ApplyLayout();
        }

        private void OnDragEnd(EventContext context) => dragging = false;
        #endregion

        #region 数据回包
        public void OnEntities(SCGodViewEntities chunk)
        {
            if (!visible || chunk == null || chunk.seq != seq) return; // 上一轮的残留块直接丢
            if (chunk.chunkCount == 0) return;
            if (expectedEntityChunks < 0)
            {
                expectedEntityChunks = chunk.chunkCount;
                receivedEntityChunks.Clear();
                entityBuffer.Clear();
            }
            if (expectedEntityChunks != chunk.chunkCount) return;
            if (!receivedEntityChunks.Add(chunk.chunkIndex)) return; // 重复块
            entityBuffer.AddRange(chunk.entries);
            if (receivedEntityChunks.Count < expectedEntityChunks) return;
            ApplyEntities();
        }

        public void OnCorals(SCGodViewCorals chunk)
        {
            if (!visible || chunk == null || chunk.seq != seq) return;
            if (chunk.chunkCount == 0) return;
            if (expectedCoralChunks < 0)
            {
                expectedCoralChunks = chunk.chunkCount;
                receivedCoralChunks.Clear();
                coralBuffer.Clear();
            }
            if (expectedCoralChunks != chunk.chunkCount) return;
            if (!receivedCoralChunks.Add(chunk.chunkIndex)) return;
            coralBuffer.AddRange(chunk.points);
            if (receivedCoralChunks.Count < expectedCoralChunks) return;
            ApplyCorals();
        }

        public void OnDetail(SCGodViewEntityDetail info)
        {
            if (!visible) return;
            if (info == null || info.entityId == 0)
            {
                ClearDetail(); // 实体已消失：收起详情
                return;
            }
            m_show_character_detail.selectedIndex = 1;
            FillHead(info);
            FillHealth(info);
            FillBuffs(info);
            FillSkills(info);
        }

        private void SendRequest()
        {
            if (Tool.NetworkManager == null) return;
            float edge = Edge;
            float half = edge * 0.5f;
            seq++;
            expectedEntityChunks = -1;
            expectedCoralChunks = -1;
            receivedEntityChunks.Clear();
            receivedCoralChunks.Clear();
            entityBuffer.Clear();
            coralBuffer.Clear();
            Tool.NetworkManager.SendGodViewArea(new CSGodViewArea
            {
                min = new Vector2(center.x - half, center.y - half),
                max = new Vector2(center.x + half, center.y + half),
                seq = seq,
            });
        }
        #endregion

        #region 点位
        private void ApplyEntities()
        {
            staleIds.Clear();
            foreach (ushort id in entityMarkers.Keys) staleIds.Add(id);
            foreach (var entry in entityBuffer)
            {
                var view = EnsureEntityView(entry.entityId);
                if (view == null) continue;
                view.m_type.selectedIndex = UI_Minimap.PageOf(entry.type.category, entry.type, entry.camp, entry.entityId == ownEntityId, myCamp);
                entityMarkers[entry.entityId] = new Marker
                {
                    view = view,
                    world = new Vector2(entry.posX, entry.posZ),
                };
                staleIds.Remove(entry.entityId);
            }
            foreach (ushort id in staleIds) RemoveMarker(entityMarkers, id);
            PlaceMarkers();
        }

        // 珊瑚没有 id（不可点），按序号复用图标：每轮用新坐标覆盖前 N 个，多出来的销毁
        private void ApplyCorals()
        {
            while (coralViews.Count < coralBuffer.Count)
            {
                var view = UI_MinimapItem.CreateInstance();
                view.scale = new Vector2(itemScale, itemScale);
                view.touchable = false;
                view.m_type.selectedIndex = UI_Minimap.PageOf(EntityCategory.Coral, default, EntityCamp.None, false, myCamp);
                m_largeMinimapContainer.AddChild(view);
                coralViews.Add(view);
            }
            while (coralViews.Count > coralBuffer.Count)
            {
                var view = coralViews[coralViews.Count - 1];
                if (view != null) view.Dispose();
                coralViews.RemoveAt(coralViews.Count - 1);
            }
            PlaceMarkers();
        }

        private static void DisposeCoralViews(List<UI_MinimapItem> views)
        {
            foreach (var view in views)
                if (view != null) view.Dispose();
            views.Clear();
        }

        private UI_MinimapItemButton EnsureEntityView(ushort entityId)
        {
            if (entityMarkers.TryGetValue(entityId, out var marker) && marker.view != null && !marker.view.isDisposed)
                return marker.view as UI_MinimapItemButton;
            var view = UI_MinimapItemButton.CreateInstance();
            view.scale = new Vector2(itemScale, itemScale);
            view.onClick.Set(() => RequestDetail(entityId));
            m_largeMinimapContainer.AddChild(view);
            return view;
        }

        private void RequestDetail(ushort entityId)
        {
            if (!visible || Tool.NetworkManager == null) return;
            Tool.NetworkManager.SendGodViewEntityDetail(new CSGodViewEntityDetail { entityId = entityId });
        }

        private void PlaceMarkers()
        {
            if (m_largeMinimapContainer == null) return;
            float ppm = PixelsPerMeter;
            float minX = center.x - Edge * 0.5f;
            float maxZ = center.y + Edge * 0.5f;
            foreach (var marker in entityMarkers.Values) PlaceMarker(marker.view, marker.world, ppm, minX, maxZ);
            for (int i = 0; i < coralViews.Count && i < coralBuffer.Count; i++)
                PlaceMarker(coralViews[i], coralBuffer[i], ppm, minX, maxZ);
        }

        private static void PlaceMarker(GObject view, Vector2 world, float ppm, float minX, float maxZ)
        {
            if (view == null) return;
            view.SetXY((world.x - minX) * ppm - view.width * 0.5f,
                (maxZ - world.y) * ppm - view.height * 0.5f);
        }

        private static void DisposeMarkers(Dictionary<ushort, Marker> markers)
        {
            foreach (var marker in markers.Values)
                if (marker.view != null) marker.view.Dispose();
            markers.Clear();
        }

        private static void RemoveMarker(Dictionary<ushort, Marker> markers, ushort entityId)
        {
            if (!markers.TryGetValue(entityId, out var marker)) return;
            if (marker.view != null) marker.view.Dispose();
            markers.Remove(entityId);
        }
        #endregion

        #region 区域与底图
        private float Edge => Landscape.MapSize / ConfigManager.GlobalVariableConfig.godView_scale[scaleIndex];

        private float PixelsPerMeter => Edge > 0f ? m_largeMinimapContainer.width / Edge : 0f;

        private Vector2 ClampCenter(Vector2 value)
        {
            float edge = Edge;
            float size = Landscape.MapSize;
            if (edge >= size) return new Vector2(size * 0.5f, size * 0.5f); // 区域不比地图小：居中
            float half = edge * 0.5f;
            return new Vector2(Mathf.Clamp(value.x, half, size - half), Mathf.Clamp(value.y, half, size - half));
        }

        // 底图与点位同一套换算：图左下 = 世界 (0,0)、上 = 世界 Z+
        private void ApplyLayout()
        {
            if (m_largeMinimapContainer == null) return;
            EnsureMapImage();
            float ppm = PixelsPerMeter;
            if (ppm <= 0f) return;
            float size = Landscape.MapSize * ppm;
            var loader = m_largeMinimapContainer.m_loader;
            if (loader != null)
            {
                loader.SetSize(size, size);
                loader.SetXY((center.x - Edge * 0.5f) * -ppm, (center.y + Edge * 0.5f - Landscape.MapSize) * ppm);
            }
            PlaceMarkers();
        }

        private void EnsureMapImage()
        {
            var loader = m_largeMinimapContainer != null ? m_largeMinimapContainer.m_loader : null;
            if (loader == null) return;
            loader.fill = FillType.Scale;
            if (mapTexture == null && Tool.AssetsManager != null && Tool.AssetsManager.MapTexture != null)
                mapTexture = new NTexture(Tool.AssetsManager.MapTexture);
            if (loader.texture == null) loader.texture = mapTexture;
        }

        // 抵消容器上层缩放，让图标回到设计尺寸（与雷达点位同一口径）
        private void ComputeItemScale()
        {
            float upper = 1f;
            for (GObject o = m_largeMinimapContainer; o != null && o.parent != GRoot.inst; o = o.parent)
                if (o.scale.x > 0.0001f) upper *= o.scale.x;
            itemScale = upper > 0.0001f ? 1f / upper : 1f;
        }

        private void BuildScaleButtons()
        {
            var scales = ConfigManager.GlobalVariableConfig.godView_scale;
            if (m_scaleButtonList == null) return;
            m_scaleButtonList.itemRenderer = (index, obj) =>
            {
                var button = obj.asButton;
                if (button == null) return;
                button.title = scales[index].ToString("0") + "x";
                button.selected = index == scaleIndex;
                int target = index;
                button.onClick.Set(() => SelectScale(target));
            };
            m_scaleButtonList.numItems = scales.Count;
        }

        private void SelectScale(int index)
        {
            if (index == scaleIndex) return;
            scaleIndex = index;
            center = ClampCenter(center);
            for (int i = 0; i < m_scaleButtonList.numChildren; i++)
            {
                var button = m_scaleButtonList.GetChildAt(i).asButton;
                if (button != null) button.selected = i == scaleIndex;
            }
            ApplyLayout();
            SendRequest(); // 换档立即拉一次，不等节拍
        }
        #endregion

        #region 详情面板
        private void ClearDetail()
        {
            m_show_character_detail.selectedIndex = 0; // 0 = 收起 / 1 = 展开
            detailSkills.Clear();
            detailHeldSkills.Clear();
            detailBuffs.Clear();
            if (m_activeSkillList != null) m_activeSkillList.numItems = 0;
            if (m_skillList != null) m_skillList.numItems = 0;
            if (m_buff_list != null && m_buff_list.m_list != null) m_buff_list.m_list.numItems = 0;
            if (m_entity_name != null) m_entity_name.text = "";
            if (m_label_health != null) m_label_health.text = "";
        }

        private void FillHead(SCGodViewEntityDetail info)
        {
            bool isCharacter = info.type.category == EntityCategory.Character_Attack
                || info.type.category == EntityCategory.Character_Defense;
            var icons = Tool.AssetsManager == null ? null
                : info.camp == EntityCamp.Defense ? Tool.AssetsManager.DefenseCharacterIcons : Tool.AssetsManager.AttackCharacterIcons;
            int index = info.type.value;
            bool hasIcon = isCharacter && icons != null && index >= 0 && index < icons.Count && icons[index] != null;
            if (m_roleHead != null)
            {
                m_roleHead.m_headIcon.texture = hasIcon ? new NTexture(icons[index]) : null;
                m_roleHead.m_show_default_icon.selectedIndex = hasIcon ? 0 : 1;
                m_roleHead.m_quality.selectedIndex = ConfigManager.EntityConfig.GetByCategory(info.type).quality;
                m_roleHead.m_level.selectedIndex = Mathf.Clamp(info.level - 1, 0, 4); // 头像控制器只有 5 档
            }
            if (m_entity_name != null) m_entity_name.text = info.name;
            if (m_campIcon != null) m_campIcon.m_player_camp.selectedIndex = (int)ToPlayerCamp(info.camp);
        }

        private void FillHealth(SCGodViewEntityDetail info)
        {
            float ratio = info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f;
            if (m_bar_fill_health_partner != null) m_bar_fill_health_partner.fillAmount = ratio;
            if (m_bar_fill_health_enemy != null) m_bar_fill_health_enemy.fillAmount = ratio;
            if (m_label_health != null) m_label_health.text = $"{info.health}/{info.maxHealth}";
        }

        private void FillBuffs(SCGodViewEntityDetail info)
        {
            if (m_buff_list == null || m_buff_list.m_list == null) return;
            detailBuffs.Clear();
            if (info.buffs != null)
                for (int i = 0; i < info.buffs.Count; i++) detailBuffs.Add(info.buffs[i]);
            var list = m_buff_list.m_list;
            list.itemRenderer = (index, obj) =>
                ((UI_buffItem)obj).Fill(detailBuffs[index].type, detailBuffs[index].level, buffHoverEnter, buffHoverExit);
            list.numItems = detailBuffs.Count;
        }

        // 技能栏槽位进池（带冷却进度与库存）；未装备的持有技能单独一栏，只画图标
        private void FillSkills(SCGodViewEntityDetail info)
        {
            detailSkills.Clear();
            if (info.slotSkills != null)
            {
                for (int i = 0; i < info.slotSkills.Count; i++)
                {
                    var slot = info.slotSkills[i];
                    detailSkills.Add(new UI_SkillList.Slot(slot.skillId, slot.recharge01 / 100f, slot.store, slot.store < 0));
                }
            }
            if (m_activeSkillList != null)
            {
                m_activeSkillList.itemRenderer = RenderSkillSlot;
                m_activeSkillList.numItems = detailSkills.Count;
            }

            detailHeldSkills.Clear();
            if (info.heldSkills != null)
            {
                for (int i = 0; i < info.heldSkills.Count && i < DetailSkillCapacity; i++)
                    detailHeldSkills.Add(new UI_SkillList.Slot(info.heldSkills[i], 1f, 0, true));
            }
            if (m_skillList != null)
            {
                m_skillList.itemRenderer = RenderHeldSkillSlot;
                m_skillList.numItems = detailHeldSkills.Count;
            }
        }

        private void RenderSkillSlot(int index, GObject obj) => RenderSlot(obj, detailSkills, index, true);

        private void RenderHeldSkillSlot(int index, GObject obj) => RenderSlot(obj, detailHeldSkills, index, false);

        private static void RenderSlot(GObject obj, List<UI_SkillList.Slot> source, int index, bool withRecharge)
        {
            if (obj is not UI_SkillListItem slot) return;
            var data = index < source.Count ? source[index] : new UI_SkillList.Slot(-1, 1f, 0, true);
            bool empty = data.skillId < 0;
            if (slot.m_key != null) slot.m_key.text = ""; // 别人的技能没有玩家键位
            if (slot.m_empty != null) slot.m_empty.selectedIndex = empty ? 1 : 0;
            Texture icon = empty ? null : UI_SkillList.ResolveIcon(data.skillId);
            if (slot.m_loader_icon != null)
            {
                slot.m_loader_icon.texture = icon != null ? new NTexture(icon) : null;
                slot.m_loader_icon.visible = icon != null;
                slot.m_loader_icon.fillAmount = !withRecharge || data.unlimited || empty ? 1f : data.recharge01;
            }
            if (slot.m_loader_iconBase != null)
            {
                slot.m_loader_iconBase.texture = icon != null ? new NTexture(icon) : null;
                slot.m_loader_iconBase.visible = icon != null;
            }
            // 未装备的技能不下发冷却与库存，次数按 0 显示
            if (slot.m_storeList != null) slot.m_storeList.numItems = withRecharge && !data.unlimited && !empty ? data.store : 0;
        }
        #endregion

        // 实体阵营 → 玩家阵营档位（PlayerCampIcon 页序：0 进攻 / 1 防守 / 2 观战 / 3 中立）
        private static PlayerCampType ToPlayerCamp(EntityCamp camp)
        {
            if (camp == EntityCamp.Attack) return PlayerCampType.Attack;
            if (camp == EntityCamp.Defense) return PlayerCampType.Defense;
            return PlayerCampType.Neutral;
        }
    }
}

