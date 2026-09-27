using FairyGUI;
using UnityEngine;

/// <summary>
/// FGUI 页面基类：持有页面根组件（生成类实例），由 UIManager 创建/切页/驱动。
/// 适配规则（全部界面按 1920×1080 设计）：OnResize 时先按当前宽高比算出「1080 高对应的宽度」应用到面板，
/// 再按「屏幕高 / 1080」对面板整体等比缩放（不改设计高度，保证竖直方向元素完整显示）。
/// </summary>
public abstract class PageBase
{
    protected GComponent Root { get; private set; }

    /// <summary>当前面板缩放（屏幕高 / 1080）。</summary>
    protected float UiScale { get; private set; } = 1f;

    /// <summary>当前宽高比下 1080 高对应的宽度（设计单位）。</summary>
    protected float PanelWidth { get; private set; } = 1920f;

    protected PageBase(GComponent root)
    {
        Root = root;
        GRoot.Instance.AddChild(root);
        root.visible = false;
    }

    public virtual void Construct() { }

    public virtual void Init() { }

    public virtual void Enter(ShowParam param)
    {
        Root.visible = true;
    }

    public virtual void Exit()
    {
        Root.visible = false;
    }

    /// <summary>每帧推进（仅当前页，由 UIManager.Update 驱动）。</summary>
    public virtual void Tick(float deltaTime) { }

    /// <summary>
    /// 分辨率变化时由 UIManager 调用：width = 1080 × 当前宽高比（设计单位），height 固定 1080。
    /// 应用面板尺寸后按「屏幕高 / 1080」整体等比缩放并贴左上角，铺满全屏。
    /// </summary>
    public virtual void OnResize(float width, float height)
    {
        PanelWidth = width;
        UiScale = Screen.height / 1080f;
        Root.SetSize(width, height);
        Root.scale = new Vector2(UiScale, UiScale);
        Root.SetXY(0f, 0f);
    }

    /// <summary>世界坐标 → 本页面板局部坐标（屏幕像素 / 缩放；用于名牌/伤害飘字跟随）。</summary>
    public Vector2 WorldToPanel(Vector3 worldPos, Camera camera = null)
    {
        var cam = camera != null ? camera : Camera.main;
        if (cam == null) return Vector2.zero;
        var screen = cam.WorldToScreenPoint(worldPos);
        return new Vector2(screen.x / UiScale, screen.y / UiScale);
    }
}
