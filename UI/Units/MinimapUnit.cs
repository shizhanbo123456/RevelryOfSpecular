using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 小地图 UI 单元（策划案第十五章）。可见单位由服务器按阵营算好后下发（阵营共享视野），本单元只做表现，
/// 两处共用同一份数据：
///   ① HUD 小地图：**圆形**，以自己为中心，只画 Config.minimap_view_radius 米内的单位（超出不画）；
///   ② 完整地图：点击 HUD 小地图展开，显示整幅 Landscape.MapSize × Landscape.MapSize 区域，并圈出 ①的显示范围。
/// 坐标约定：世界 **X+ 向右、Z+ 向上**（地图为俯视图）；UI Toolkit 的 y 轴向下，故 Z 映射到 bottom。
/// 自己用金点，其余按阵营着色（攻红 / 守蓝 / 中立灰）；被「白眼标记」的敌人加描边；
/// 「小地图失联」（夜间进攻方）时标题变为提示文字。
/// </summary>
public class MinimapUnit
{
    /// <summary>HUD 小地图直径（像素）。</summary>
    private const float HudSize = 200f;
    /// <summary>HUD 小地图在页面上的位置（左上角，避开顶部信息条；与右侧守护点面板对称）。</summary>
    private const float HudLeft = 16f;
    private const float HudTop = 60f;
    /// <summary>完整地图边长（像素）。</summary>
    private const float FullMapSize = 560f;
    /// <summary>点位到画布边缘的留白（像素）。</summary>
    private const float DotInset = 6f;
    /// <summary>本地玩家点位尺寸（像素）。</summary>
    private const float SelfDotSize = 11f;

    /// <summary>HUD 小地图根元素（点击展开完整地图）。</summary>
    public VisualElement Root { get; }

    private readonly Label titleLabel;
    private readonly MapCanvas hudCanvas;
    private readonly VisualElement fullMapOverlay;
    private readonly MapCanvas fullCanvas;
    private readonly VisualElement viewCircle;
    /// <summary>完整地图的像素/米（画布半宽对应地图半边长）。</summary>
    private readonly float fullPixelsPerMeter;

    private SCMinimapInfo lastInfo;
    private ushort selfId;
    /// <summary>是否已知自己位置（自己不在可见列表时沿用上一次的值）。</summary>
    private bool hasSelf;
    private float selfX;
    private float selfZ;
    /// <summary>完整地图是否展开（不用读 style 判断，避免依赖样式枚举比较）。</summary>
    private bool fullMapOpen;

    public MinimapUnit()
    {
        // ① HUD 小地图：正方形框架取半边长圆角即成正圆，超范围单位直接不画（不依赖裁剪）
        Root = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = HudLeft,
                top = HudTop,
                width = HudSize,
                height = HudSize,
                overflow = Overflow.Hidden,
            }
        };
        Root.style.backgroundColor = UITheme.SunkenBg;
        UITheme.SetBorder(Root, 1f, UITheme.BorderStrong);
        UITheme.SetRadius(Root, HudSize * 0.5f);
        Root.RegisterCallback<ClickEvent>(_ => OpenFullMap());

        hudCanvas = new MapCanvas(Root);

        titleLabel = UITheme.Text("小地图 · 点击展开", UITheme.TextFaint, UITheme.FontTiny);
        titleLabel.style.position = Position.Absolute;
        titleLabel.style.left = 0;
        titleLabel.style.right = 0;
        titleLabel.style.bottom = 6;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.pickingMode = PickingMode.Ignore;
        Root.Add(titleLabel);

        // ② 完整地图：整屏遮罩 + 居中地图，点击任意处关闭
        fullPixelsPerMeter = (FullMapSize * 0.5f - DotInset) / (Landscape.MapSize * 0.5f);

        fullMapOverlay = UITheme.Overlay(0.85f);
        fullMapOverlay.RegisterCallback<ClickEvent>(_ => CloseFullMap());

        var frame = new VisualElement
        {
            style = { width = FullMapSize, height = FullMapSize, overflow = Overflow.Hidden }
        };
        frame.style.backgroundColor = UITheme.CardBg;
        UITheme.SetBorder(frame, 1f, UITheme.BorderStrong);
        UITheme.SetRadius(frame, UITheme.Radius);
        fullMapOverlay.Add(frame);

        fullCanvas = new MapCanvas(frame);

        // 显示范围圈：完整地图上标出 HUD 小地图能显示的范围（以自己为圆心、minimap_view_radius 半径），
        // 它只是画面裁剪范围，与「可见距离」（服务器可见性判定）无关
        viewCircle = new VisualElement { pickingMode = PickingMode.Ignore };
        viewCircle.style.position = Position.Absolute;
        viewCircle.style.display = DisplayStyle.None;
        UITheme.SetBorder(viewCircle, 1f, UITheme.BorderStrong);
        frame.Add(viewCircle);

        var frameTitle = UITheme.Text($"完整地图 {Mathf.RoundToInt(Landscape.MapSize)}×{Mathf.RoundToInt(Landscape.MapSize)}", UITheme.TextDim, UITheme.FontSmall);
        frameTitle.style.position = Position.Absolute;
        frameTitle.style.left = 0;
        frameTitle.style.right = 0;
        frameTitle.style.top = 6;
        frameTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        frameTitle.pickingMode = PickingMode.Ignore;
        frame.Add(frameTitle);

        var closeButton = UITheme.StyleButton(new Button(CloseFullMap) { text = "关闭（点击任意处也可）" }, false, 0f, 34f);
        closeButton.style.marginTop = 12;
        fullMapOverlay.Add(closeButton);
    }

    /// <summary>挂载到页面（同时挂上 HUD 小地图与完整地图遮罩，后者在上层）。</summary>
    public void Attach(VisualElement parent)
    {
        parent.Add(Root);
        parent.Add(fullMapOverlay);
    }

    /// <summary>刷新（来源：服务器下发的本阵营可见单位，两个视图共用这份数据）。</summary>
    public void Refresh(SCMinimapInfo info)
    {
        if (info == null) return;
        lastInfo = info;

        selfId = NetworkManager.battleInfo != null ? NetworkManager.battleInfo.playerEntityId : (ushort)0;
        if (info.entities != null)
        {
            foreach (var entity in info.entities)
            {
                if (entity == null || entity.entityId != selfId) continue;
                hasSelf = true;
                selfX = entity.posX;
                selfZ = entity.posZ;
                break;
            }
        }

        if (titleLabel != null)
        {
            titleLabel.text = info.minimapLost ? "视野失联 · 点击展开" : "小地图 · 点击展开";
            titleLabel.style.color = info.minimapLost ? UITheme.Warn : UITheme.TextFaint;
        }

        // HUD：以自己为中心，画半径内的单位；自己不在列表时（如复活等待中）退回地图中心
        float centerX = hasSelf ? selfX : Landscape.MapCenter.x;
        float centerZ = hasSelf ? selfZ : Landscape.MapCenter.z;
        hudCanvas.Draw(info, selfId, centerX, centerZ, Config.minimap_view_radius, HudSize, Config.minimap_view_radius);

        if (fullMapOpen) RedrawFullMap();
    }

    /// <summary>清点位并收起完整地图（对局结束）。</summary>
    public void Clear()
    {
        lastInfo = null;
        hasSelf = false;
        hudCanvas.Clear();
        fullCanvas.Clear();
        CloseFullMap();
    }

    /// <summary>展开完整地图。</summary>
    public void OpenFullMap()
    {
        fullMapOpen = true;
        fullMapOverlay.style.display = DisplayStyle.Flex;
        RedrawFullMap();
    }

    /// <summary>收起完整地图。</summary>
    public void CloseFullMap()
    {
        fullMapOpen = false;
        fullMapOverlay.style.display = DisplayStyle.None;
    }

    #region//Local
    /// <summary>完整地图：整幅地图范围 + 自己那圈视野圈（需要 lastInfo）。</summary>
    private void RedrawFullMap()
    {
        fullCanvas.Draw(lastInfo, selfId, Landscape.MapCenter.x, Landscape.MapCenter.z,
            Landscape.MapSize * 0.5f, FullMapSize, -1f);

        if (!hasSelf)
        {
            viewCircle.style.display = DisplayStyle.None;
            return;
        }
        float diameter = Config.minimap_view_radius * 2f * fullPixelsPerMeter;
        viewCircle.style.display = DisplayStyle.Flex;
        viewCircle.style.width = diameter;
        viewCircle.style.height = diameter;
        UITheme.SetRadius(viewCircle, diameter * 0.5f);
        viewCircle.style.left = FullMapSize * 0.5f + (selfX - Landscape.MapCenter.x) * fullPixelsPerMeter - diameter * 0.5f;
        viewCircle.style.bottom = FullMapSize * 0.5f + (selfZ - Landscape.MapCenter.z) * fullPixelsPerMeter - diameter * 0.5f;
    }

    /// <summary>
    /// 一张地图的画布：点位池 + 世界 XZ → 局部坐标换算。
    /// 缩放 = (边长/2 − 留白) / halfSpan，即 halfSpan 米的距离正好落在画布边缘内侧，故 HUD 的视野圈边界即 Config.minimap_view_radius。
    /// </summary>
    private sealed class MapCanvas
    {
        private readonly List<VisualElement> dots = new();

        public MapCanvas(VisualElement parent)
        {
            var layer = new VisualElement
            {
                style = { position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0 }
            };
            layer.pickingMode = PickingMode.Ignore;
            parent.Add(layer);
            Layer = layer;
        }

        private VisualElement Layer { get; }

        /// <param name="centerX">画布中心对应的世界 X。</param>
        /// <param name="centerZ">画布中心对应的世界 Z。</param>
        /// <param name="halfSpan">画布半宽对应的世界距离（米）。</param>
        /// <param name="size">画布边长（像素）。</param>
        /// <param name="filterRadius">只画该半径内的单位（&lt;= 0 表示全画）。</param>
        public void Draw(SCMinimapInfo info, ushort selfId, float centerX, float centerZ, float halfSpan, float size, float filterRadius)
        {
            float pixelsPerMeter = (size * 0.5f - DotInset) / Mathf.Max(0.01f, halfSpan);
            int used = 0;
            if (info != null && info.entities != null)
            {
                foreach (var entity in info.entities)
                {
                    if (entity == null) continue;
                    float dx = entity.posX - centerX;
                    float dz = entity.posZ - centerZ;
                    if (filterRadius > 0f && dx * dx + dz * dz > filterRadius * filterRadius) continue;
                    Apply(Take(used++), entity, entity.entityId == selfId, size, pixelsPerMeter, centerX, centerZ);
                }
            }
            for (int i = used; i < dots.Count; i++)
            {
                dots[i].style.display = DisplayStyle.None;
            }
        }

        public void Clear()
        {
            foreach (var dot in dots)
            {
                dot.style.display = DisplayStyle.None;
            }
        }

        private VisualElement Take(int index)
        {
            while (dots.Count <= index)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.style.position = Position.Absolute;
                Layer.Add(dot);
                dots.Add(dot);
            }
            return dots[index];
        }

        private static void Apply(VisualElement dot, SCMinimapInfo.MinimapEntity entity, bool self,
            float size, float pixelsPerMeter, float centerX, float centerZ)
        {
            float dotSize = self ? SelfDotSize : DotSizeOf(entity.type.category);
            float x = size * 0.5f + (entity.posX - centerX) * pixelsPerMeter;
            float z = size * 0.5f + (entity.posZ - centerZ) * pixelsPerMeter;

            dot.style.display = DisplayStyle.Flex;
            dot.style.width = dotSize;
            dot.style.height = dotSize;
            UITheme.SetRadius(dot, dotSize * 0.5f);
            dot.style.backgroundColor = self ? UITheme.Gold : CampColor(entity.camp);
            // 白眼标记：加描边高亮（策划案 11.3）
            UITheme.SetBorder(dot, entity.marked ? 2f : (self ? 1f : 0f), entity.marked ? UITheme.Warn : UITheme.TextMain);
            dot.style.left = x - dotSize * 0.5f;
            dot.style.bottom = z - dotSize * 0.5f;
        }

        /// <summary>阵营着色：攻红 / 守蓝 / 中立灰。</summary>
        private static Color CampColor(EntityCamp camp)
        {
            switch (camp)
            {
                case EntityCamp.Attack: return UITheme.Attack;
                case EntityCamp.Defense: return UITheme.Defense;
                default: return UITheme.TextDim;
            }
        }

        /// <summary>点位尺寸按类别区分：目标物（守护点 / 塔 / 树）大于角色，僵尸最小。</summary>
        private static float DotSizeOf(EntityCategory category)
        {
            switch (category)
            {
                case EntityCategory.Beacon: return 11f;
                case EntityCategory.Tower: return 9f;
                case EntityCategory.PlagueTree: return 9f;
                case EntityCategory.Character_Attack:
                case EntityCategory.Character_Defense: return 8f;
                case EntityCategory.Crystal: return 7f;
                case EntityCategory.Zombie:
                case EntityCategory.EliteZombie: return 6f;
                default: return 7f;
            }
        }
    }
    #endregion
}
