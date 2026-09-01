using Ros.Transport;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 守护点血量 UI 单元（策划案 5.1：防守方 HUD 常驻显示每个守护点血量，血量变化即偷拆预警）。
/// </summary>
public class BeaconBarUnit
{
    /// <summary>根元素。</summary>
    public VisualElement Root { get; }

    private readonly Label nameLabel;
    private readonly Label healthLabel;
    private readonly VisualElement fill;
    private readonly Label shieldLabel;

    public BeaconBarUnit()
    {
        Root = new VisualElement
        {
            style =
            {
                width = 240, height = 56,
                backgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.95f),
                padding = 6, marginBottom = 6,
            }
        };
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        nameLabel = new Label("守护点") { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } };
        healthLabel = new Label("5000/5000") { style = { color = Color.white, fontSize = 13 } };
        row.Add(nameLabel);
        row.Add(healthLabel);
        Root.Add(row);

        var bar = new VisualElement
        {
            style = { height = 12, backgroundColor = new Color(0.3f, 0f, 0f, 1f), marginTop = 4, position = Position.Relative }
        };
        fill = new VisualElement { style = { width = Length.Percent(100f), height = 12, backgroundColor = new Color(1f, 0.2f, 0.1f, 1f) } };
        fill.pickingMode = PickingMode.Ignore;
        bar.Add(fill);
        Root.Add(bar);

        shieldLabel = new Label("")
        {
            style =
            {
                color = new Color(0.4f, 0.8f, 1f, 1f), fontSize = 12, marginTop = 2,
            }
        };
        Root.Add(shieldLabel);
    }

    /// <summary>刷新守护点数据。</summary>
    public void Refresh(SCBeaconInfo info)
    {
        if (info == null) return;
        nameLabel.text = info.type == EntityType.CoreBeacon ? "中心守护点" : $"外围守护点 {info.type.value}";
        healthLabel.text = $"{info.health}/{info.maxHealth}";
        fill.style.width = Length.Percent(Mathf.Clamp01(info.maxHealth > 0 ? (float)info.health / info.maxHealth : 0f) * 100f);
        shieldLabel.text = info.shieldLayer > 0 ? $"减伤叠层 ×{info.shieldLayer}" : "";
    }

    /// <summary>守护点被摧毁时置灰。</summary>
    public void SetDestroyed()
    {
        fill.style.width = Length.Percent(0f);
        healthLabel.text = "已摧毁";
        shieldLabel.text = "";
    }
}
