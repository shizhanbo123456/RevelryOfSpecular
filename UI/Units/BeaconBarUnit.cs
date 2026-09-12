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
        Root = new VisualElement { style = { width = 240, marginBottom = 8 } };
        Root.style.backgroundColor = UITheme.CardBg;
        UITheme.SetBorder(Root, 1f, UITheme.Border);
        UITheme.SetRadius(Root, UITheme.RadiusSmall);
        UITheme.SetPadding(Root, 8f);

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        nameLabel = UITheme.Text("守护点", UITheme.TextMain, UITheme.FontSmall, true);
        healthLabel = UITheme.Text("5000/5000", UITheme.TextDim, UITheme.FontSmall);
        row.Add(nameLabel);
        row.Add(healthLabel);
        Root.Add(row);

        var bar = UITheme.BarTrack(10f);
        bar.style.marginTop = 6;
        fill = UITheme.BarFill(UITheme.Danger, 10f);
        bar.Add(fill);
        Root.Add(bar);

        shieldLabel = UITheme.Text("", UITheme.Defense, UITheme.FontTiny);
        shieldLabel.style.marginTop = 3;
        Root.Add(shieldLabel);
    }

    /// <summary>刷新守护点数据（来源：实体表现摘要；减伤叠层按 Buff 判断）。</summary>
    public void Refresh(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        nameLabel.text = info.type == EntityType.CoreBeacon ? "中心守护点" : $"外围守护点 {info.type.value}";
        healthLabel.text = $"{info.health}/{info.maxHealth}";
        fill.style.width = Length.Percent(Mathf.Clamp01(info.maxHealth > 0 ? (float)info.health / info.maxHealth : 0f) * 100f);

        // 减伤叠层 = 「守护点减伤」Buff 的等级（守护点数量分层机制/教皇守护）
        int shieldLayer = 0;
        foreach (var buff in info.buffs)
        {
            if (buff != null && buff.type == (int)EffectType.BeaconReduce)
            {
                shieldLayer = Mathf.Max(shieldLayer, buff.level);
            }
        }
        shieldLabel.text = shieldLayer > 0 ? $"减伤叠层 ×{shieldLayer}" : "";
    }

    /// <summary>守护点被摧毁时置灰。</summary>
    public void SetDestroyed()
    {
        fill.style.width = Length.Percent(0f);
        healthLabel.text = "已摧毁";
        healthLabel.style.color = UITheme.TextFaint;
        shieldLabel.text = "";
    }
}
