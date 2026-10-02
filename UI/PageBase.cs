using FairyGUI;
using UnityEngine;

public abstract class PageBase
{
    protected GComponent Root { get; private set; }

    public float UiScale { get; private set; } = 1f;

    public float PanelWidth { get; private set; } = 1920f;

    protected PageBase(GComponent root)
    {
        Root = root;
        GRoot.inst.AddChild(root);
        root.visible = false;
    }

    public virtual void Construct() { }

    public virtual void Init() { }

    public virtual void Enter(ShowParam param)
    {
        IsVisible = true;
        Root.visible = true;
    }

    public virtual void Exit()
    {
        IsVisible = false;
        Root.visible = false;
    }

    public bool IsVisible { get; private set; }

    public virtual void Tick(float deltaTime) { }

    public virtual void OnResize(float width, float height)
    {
        PanelWidth = width;
        UiScale = Screen.height / 1080f;
        Root.SetSize(width, height);
        Root.scale = new Vector2(UiScale, UiScale);
        Root.SetXY(0f, 0f);
    }

    public Vector2 WorldToPanel(Vector3 worldPos, Camera camera = null)
    {
        var cam = camera != null ? camera : Camera.main;
        if (cam == null) return Vector2.zero;
        var screen = cam.WorldToScreenPoint(worldPos);
        // Unity 屏幕 y 轴向上（原点左下），FGUI 局部 y 轴向下（原点左上），需翻转
        return new Vector2(screen.x / UiScale, (Screen.height - screen.y) / UiScale);
    }
}

public class ShowParam
{
}
