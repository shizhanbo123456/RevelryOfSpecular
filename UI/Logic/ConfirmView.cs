using System;
using FairyGUI;
using Ros.UI.Main;
using UnityEngine;

// 确认弹窗：全局子界面（不挂任何页面，挂 GRoot）。
// 弹出时先铺满屏遮罩挡住底层点击，再把弹窗本体叠在其上。
public class ConfirmView
{
    private UI_FullScreenMask mask;
    private UI_ConfirmPanel panel;
    private Action onConfirm;
    private Action onCancel;
    private float uiScale = 1f;

    public bool IsOpen { get; private set; }

    public void Construct()
    {
        mask = UI_FullScreenMask.CreateInstance();
        mask.opaque = true; // 吞掉点击：底层页面/聊天/GM 面板都不再响应
        GRoot.inst.AddChild(mask);
        mask.visible = false;

        panel = UI_ConfirmPanel.CreateInstance();
        GRoot.inst.AddChild(panel);
        panel.visible = false;

        panel.m_btn_confirm.onClick.Add(OnConfirmClicked);
        panel.m_btn_cancel.onClick.Add(OnCancelClicked);
    }

    public void Shutdown()
    {
        onConfirm = null;
        onCancel = null;
        Hide();
    }

    // 与页面同规则的整体缩放（设计高度 1080）
    public void OnResize(float uiScale)
    {
        this.uiScale = uiScale;
        ApplyTransform();
    }

    // content = 正文；confirm = 确认回调；cancel = 取消回调（可为空，此时取消按钮只关闭）
    public void Show(string content, Action confirm, Action cancel = null)
    {
        onConfirm = confirm;
        onCancel = cancel;
        if (panel.m_content != null) panel.m_content.text = content;

        // 每次弹出都重新置顶：先遮罩后弹窗，加入顺序即层级
        GRoot.inst.AddChild(mask);
        GRoot.inst.AddChild(panel);
        mask.visible = true;
        panel.visible = true;
        ApplyTransform();
        IsOpen = true;
    }

    public void Hide()
    {
        onConfirm = null;
        onCancel = null;
        IsOpen = false;
        if (mask != null) mask.visible = false;
        if (panel != null) panel.visible = false;
    }

    #region//Local
    // 先关闭再回调：回调里可能立刻弹下一个窗或切页
    private void OnConfirmClicked()
    {
        var act = onConfirm;
        Hide();
        if (act != null) act();
    }

    private void OnCancelClicked()
    {
        var act = onCancel;
        Hide();
        if (act != null) act();
    }

    // 居中：GRoot 坐标是屏幕像素，面板按页面同规则缩放
    private void ApplyTransform()
    {
        if (panel == null) return;
        panel.scale = new Vector2(uiScale, uiScale);
        panel.SetXY(
            (GRoot.inst.width - panel.width * uiScale) * 0.5f,
            (GRoot.inst.height - panel.height * uiScale) * 0.5f);
    }
    #endregion
}
