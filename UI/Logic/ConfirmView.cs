using System;
using FairyGUI;
using Ros.UI.Main;
using UnityEngine;

// 确认弹窗：全局子界面（不挂任何页面，挂 GRoot）。
// 全屏拦截层已并入 ConfirmPanel 内部，弹窗本体 opaque 吞掉点击。
public class ConfirmView
{
    private UI_ConfirmPanel panel;
    private Action onConfirm;
    private Action onCancel;
    private float uiScale = 1f;

    public bool IsOpen { get; private set; }

    public void Construct()
    {
        panel = UI_ConfirmPanel.CreateInstance();
        panel.opaque = true; // 吞掉点击：底层页面/聊天/GM 面板都不再响应
        panel.touchable = true;
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

        GRoot.inst.AddChild(panel); // 每次弹出都重新置顶
        panel.visible = true;
        ApplyTransform();
        IsOpen = true;
    }

    public void Hide()
    {
        onConfirm = null;
        onCancel = null;
        IsOpen = false;
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

    // 居中：面板带全屏拦截层时尺寸不等于弹窗本身，按中心锚点定位才不会被拉偏
    private void ApplyTransform()
    {
        if (panel == null) return;
        panel.scale = new Vector2(uiScale, uiScale);
        panel.pivotAsAnchor = true;
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.SetXY(GRoot.inst.width * 0.5f, GRoot.inst.height * 0.5f);
    }
    #endregion
}
