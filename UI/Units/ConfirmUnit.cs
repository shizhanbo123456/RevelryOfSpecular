using System;
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
        root = UITheme.Overlay(0.45f);

        var panel = UITheme.Card(400f, 20f);
        label = UITheme.Text("", UITheme.TextMain, 16);
        label.style.marginBottom = 22;

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        confirmButton = UITheme.StyleButton(new Button { text = "确定" }, true, 160f, 40f);
        cancelButton = UITheme.StyleButton(new Button { text = "取消" }, false, 160f, 40f);
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
