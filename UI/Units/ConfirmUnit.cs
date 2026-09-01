using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 通用确认面板（居中弹窗：文案 + 确定/取消）。
/// </summary>
public class ConfirmUnit
{
    private VisualElement root;
    private Label label;
    private Button confirmButton;
    private Button cancelButton;
    private Action onConfirm;

    /// <summary>初始化并挂载。</summary>
    public void Init(VisualElement parent)
    {
        root = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 0, bottom = 0,
                backgroundColor = new Color(0f, 0f, 0f, 0.4f),
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                display = DisplayStyle.None,
            }
        };
        var panel = new VisualElement
        {
            style =
            {
                backgroundColor = new Color(0.15f, 0.15f, 0.18f, 0.98f),
                width = 380,
                paddingLeft = 20, paddingRight = 20, paddingTop = 16, paddingBottom = 16,
            }
        };
        label = new Label { style = { color = Color.white, fontSize = 16, whiteSpace = WhiteSpace.Normal, marginBottom = 18 } };
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        confirmButton = new Button { text = "确定", style = { width = 150 } };
        cancelButton = new Button { text = "取消", style = { width = 150 } };
        confirmButton.clicked += () =>
        {
            Hide();
            onConfirm?.Invoke();
            onConfirm = null;
        };
        cancelButton.clicked += Hide;
        row.Add(confirmButton);
        row.Add(cancelButton);
        panel.Add(label);
        panel.Add(row);
        root.Add(panel);
        parent.Add(root);
    }

    /// <summary>显示确认面板。</summary>
    public void Show(string msg, Action onConfirm)
    {
        if (root == null || label == null) return;
        label.text = msg;
        this.onConfirm = onConfirm;
        root.style.display = DisplayStyle.Flex;
    }

    /// <summary>隐藏。</summary>
    public void Hide()
    {
        if (root != null) root.style.display = DisplayStyle.None;
    }
}
