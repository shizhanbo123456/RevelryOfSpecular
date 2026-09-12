using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 通用加载遮罩（居中卡片 + 文案）。
/// </summary>
public class LoadingUnit
{
    private VisualElement root;
    private Label label;

    /// <summary>初始化并挂载。</summary>
    public void Init(VisualElement parent)
    {
        root = UITheme.Overlay(0.6f);

        var box = UITheme.Card(280f, 18f);
        label = UITheme.Text("加载中...", UITheme.TextMain, 18, true);
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        box.Add(label);
        root.Add(box);
        parent.Add(root);
    }

    /// <summary>显示/隐藏。</summary>
    public void Show(bool show)
    {
        if (root == null) return;
        root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>设置文案。</summary>
    public void SetText(string text)
    {
        if (label != null) label.text = text;
    }
}
