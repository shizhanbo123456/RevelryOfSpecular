using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 通用加载遮罩（居中转圈 + 文案）。
/// </summary>
public class LoadingUnit
{
    private VisualElement root;
    private Label label;

    /// <summary>初始化并挂载。</summary>
    public void Init(VisualElement parent)
    {
        root = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 0, bottom = 0,
                backgroundColor = new Color(0f, 0f, 0f, 0.5f),
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                display = DisplayStyle.None,
            }
        };
        label = new Label { text = "加载中...", style = { color = Color.white, fontSize = 18 } };
        root.Add(label);
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
